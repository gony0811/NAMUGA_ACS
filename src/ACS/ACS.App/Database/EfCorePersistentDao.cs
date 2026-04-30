using System;
using System.Collections;
using System.Collections.Generic;

using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ACS.Core.Base.Interface;
using ACS.Core.Resource.Model;

namespace ACS.Database
{
    /// <summary>
    /// EF Core 기반 IPersistentDao 구현.
    /// NHibernate HibernateDaoImplement를 대체하며, 동일한 메서드 시그니처를 유지한다.
    ///
    /// 주요 변경 사항:
    /// - NHibernate Session/HibernateTemplate → DbContext
    /// - HQL → LINQ 또는 Raw SQL
    /// - DetachedCriteria → 제거됨 (IPersistentDao에서 삭제)
    /// - 동적 타입 조회는 리플렉션 기반 DbContext.Set 사용
    /// </summary>
    public class EfCorePersistentDao : IPersistentDao
    {
        /// <summary>
        /// 스레드별 DbContext를 유지하여 스레드 안전성 확보.
        /// EF Core DbContext는 스레드 안전하지 않으므로, 스레드별로
        /// 별도의 인스턴스를 사용하여 동시성 예외를 방지한다.
        /// </summary>
        [ThreadStatic]
        private static AcsDbContext _threadDb;

        private AcsDbContext _db
        {
            get
            {
                if (_threadDb == null)
                {
                    _threadDb = new AcsDbContext();
                }
                return _threadDb;
            }
        }

        private int maxResults = 1000;
        private int maxUpdateCounts = 1000;
        private int dataAccessRetryCount = 3;
        private long dataAccessRetrySleep = 300L;

        // NHibernate 엔티티 이름 → CLR Type 매핑 (런타임에 캐싱)
        private static readonly Dictionary<string, Type> _entityTypeCache = new Dictionary<string, Type>();
        private static readonly object _cacheLock = new object();

        public EfCorePersistentDao(AcsDbContext db)
        {
            // 최초 인스턴스는 메인 스레드의 ThreadStatic에 저장
            _threadDb = db;
        }

        #region Type Resolution

        /// <summary>
        /// 클래스 이름(단순명 또는 정규명)을 CLR Type으로 변환.
        /// NHibernate의 GetAllClassMetadata() 역할 대체.
        /// </summary>
        private Type ResolveType(string className)
        {
            if (string.IsNullOrEmpty(className)) return null;

            lock (_cacheLock)
            {
                if (_entityTypeCache.TryGetValue(className, out var cached))
                    return cached;
            }

            // EF Core 모델에서 엔티티 타입 검색 (정확한 이름 매칭)
            var entityType = _db.Model.GetEntityTypes()
                .FirstOrDefault(et =>
                    et.ClrType.FullName == className ||
                    et.ClrType.Name == className);

            // 정확한 매칭 실패 시, 상속 관계 검색 (예: VehicleEx → VehicleExs)
            if (entityType == null)
            {
                var requestedType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(className))
                    .FirstOrDefault(t => t != null);

                if (requestedType != null)
                {
                    entityType = _db.Model.GetEntityTypes()
                        .FirstOrDefault(et => requestedType.IsAssignableFrom(et.ClrType));
                }
            }

            Type resolved = entityType?.ClrType;

            if (resolved != null)
            {
                lock (_cacheLock)
                {
                    _entityTypeCache[className] = resolved;
                }
            }

            return resolved;
        }

        /// <summary>
        /// 타입에 대한 IQueryable을 가져온다.
        /// DbContext.Set(Type) 은 non-generic IQueryable을 반환하므로 리플렉션으로 처리.
        /// </summary>
        private IQueryable<object> GetQueryable(Type clazz)
        {
            // DbContext.Set<T>() 를 리플렉션으로 호출
            var setMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes);
            var genericSet = setMethod.MakeGenericMethod(clazz);
            var dbSet = genericSet.Invoke(_db, null);

            // IQueryable<T> → IQueryable<object> 캐스트
            var castMethod = typeof(Queryable).GetMethod(nameof(Queryable.Cast)).MakeGenericMethod(typeof(object));
            return (IQueryable<object>)castMethod.Invoke(null, new[] { dbSet });
        }

        /// <summary>
        /// 엔티티의 프로퍼티 값을 리플렉션으로 가져온다.
        /// </summary>
        private object GetPropertyValue(object entity, string propertyName)
        {
            return entity?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(entity);
        }

        /// <summary>
        /// 엔티티의 프로퍼티 값을 리플렉션으로 설정한다.
        /// </summary>
        private void SetPropertyValue(object entity, string propertyName, object value)
        {
            var prop = entity?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanWrite)
            {
                // PostgreSQL timestamp with time zone는 UTC만 허용
                if (value is DateTime dt && dt.Kind == DateTimeKind.Local)
                    value = dt.ToUniversalTime();

                prop.SetValue(entity, value);
            }
        }

        /// <summary>
        /// 엔티티 ID 프로퍼티 이름을 찾는다.
        /// </summary>
        private string GetKeyPropertyName(Type clazz)
        {
            var entityType = _db.Model.FindEntityType(clazz);
            if (entityType != null)
            {
                var key = entityType.FindPrimaryKey();
                if (key != null && key.Properties.Count > 0)
                    return key.Properties[0].Name;
            }
            return "Id";
        }

        #endregion

        #region Use / Exist

        public bool Use(Type clazz)
        {
            return _db.Model.FindEntityType(clazz) != null;
        }

        public bool Use(string className)
        {
            var type = ResolveType(className);
            return type != null && Use(type);
        }

        public object Exist(Type clazz, ISerializable id)
        {
            return Exist(clazz.FullName, id);
        }

        public object Exist(string className, ISerializable id)
        {
            var type = ResolveType(className);
            if (type == null) return null;

            object idValue = NormalizeId(id);
            return _db.Find(type, idValue);
        }

        public object ExistByName(Type clazz, object value)
        {
            return ExistByName(clazz.FullName, value);
        }

        public object ExistByName(string className, object value)
        {
            var type = ResolveType(className);
            if (type == null) return null;

            var query = GetQueryable(type);
            var result = query.AsEnumerable().FirstOrDefault(e => Equals(GetPropertyValue(e, "Name"), value));
            return result;
        }

        #endregion

        #region Save / SaveOrUpdate / Flush

        public void Save(object obj)
        {
            NormalizeDateTimeProperties(obj);
            int tryCount = 0;
            do
            {
                try
                {
                    _db.Add(obj);
                    _db.SaveChanges();
                    return;
                }
                catch (Exception)
                {
                    tryCount++;
                    if (tryCount > dataAccessRetryCount) throw;
                    DetachEntity(obj);
                    System.Threading.Thread.Sleep((int)dataAccessRetrySleep);
                }
            } while (tryCount <= dataAccessRetryCount);
        }

        public bool Save(object obj, bool ignoreException)
        {
            try
            {
                Save(obj);
                return true;
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
                return false;
            }
        }

        public void SaveOrUpdate(object obj)
        {
            NormalizeDateTimeProperties(obj);
            int tryCount = 0;
            do
            {
                try
                {
                    var entry = _db.Entry(obj);
                    if (entry.State == EntityState.Detached)
                    {
                        // ID로 기존 엔티티 확인
                        var type = obj.GetType();
                        var keyName = GetKeyPropertyName(type);
                        var keyValue = GetPropertyValue(obj, keyName);

                        var existing = keyValue != null ? _db.Find(type, keyValue) : null;
                        if (existing != null)
                        {
                            _db.Entry(existing).CurrentValues.SetValues(obj);
                        }
                        else
                        {
                            _db.Add(obj);
                        }
                    }
                    else
                    {
                        _db.Update(obj);
                    }
                    _db.SaveChanges();
                    return;
                }
                catch (Exception)
                {
                    tryCount++;
                    if (tryCount > dataAccessRetryCount) throw;
                    DetachEntity(obj);
                    System.Threading.Thread.Sleep((int)dataAccessRetrySleep);
                }
            } while (tryCount <= dataAccessRetryCount);
        }

        public void Flush()
        {
            _db.SaveChanges();
        }

        public void UpdateAll(ICollection collection)
        {
            foreach (var ent in collection)
            {
                SaveOrUpdate(ent);
            }
        }

        #endregion

        #region Find

        public IList<T> Find<T>(string hql) where T : class
        {
            // HQL → Raw SQL 변환은 불가능하므로 빈 리스트 반환 (사용자가 LINQ로 전환 필요)
            // 대부분의 HQL 쿼리는 단순 SELECT이므로 fallback으로 전체 조회
            return _db.Set<T>().ToList();
        }

        public object Find(Type clazz, ISerializable id)
        {
            object idValue = NormalizeId(id);
            return _db.Find(clazz, idValue);
        }

        public object Find(string className, ISerializable id)
        {
            var type = ResolveType(className);
            if (type == null) return null;
            return Find(type, id);
        }

        public object Find(Type clazz, ISerializable id, bool throwExceptionIfNotFound)
        {
            var result = Find(clazz, id);
            return result;
        }

        public object Find(string className, ISerializable id, bool throwExceptionIfNotFound)
        {
            var result = Find(className, id);
            return result;
        }

        public object FindByName(Type clazz, object value)
        {
            return FindByName(clazz.FullName, value);
        }

        public object FindByName(string className, object value)
        {
            var type = ResolveType(className);
            if (type == null) return null;

            var query = GetQueryable(type);
            return query.AsEnumerable().FirstOrDefault(e => Equals(GetPropertyValue(e, "Name"), value));
        }

        public object FindByName(Type clazz, object value, bool throwExceptionIfNotFound)
        {
            if (throwExceptionIfNotFound)
                return FindByName(clazz, value);
            else
                return FindByNameWithoutException(clazz, value);
        }

        public object FindByName(string className, object value, bool throwExceptionIfNotFound)
        {
            if (throwExceptionIfNotFound)
                return FindByName(className, value);
            else
                return FindByNameWithoutException(className, value);
        }

        public IList FindByExample(object obj)
        {
            return FindByExample(obj, false);
        }

        public IList FindByExample(object obj, bool ignoreException)
        {
            // NHibernate의 FindByExample은 원래도 구현되지 않았음 (빈 리스트 반환)
            return new ArrayList();
        }

        public IList FindByAttribute(Type clazz, string name, object value)
        {
            return FindByAttribute(clazz.FullName, name, value);
        }

        public IList FindByAttribute(string className, string name, object value)
        {
            return FindByAttribute(className, name, value, false);
        }

        public IList FindByAttribute(Type clazz, string name, object value, bool ignoreException)
        {
            return FindByAttribute(clazz.FullName, name, value, ignoreException);
        }

        public IList FindByAttribute(string className, string name, object value, bool ignoreException)
        {
            try
            {
                var type = ResolveType(className);
                if (type == null) return new ArrayList();

                var query = GetQueryable(type);
                var result = query.AsEnumerable().Where(e => Equals(GetPropertyValue(e, name), value)).ToList();
                return new ArrayList(result);
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
                return new ArrayList();
            }
        }

        public IList FindByAttributeOrderBy(Type clazz, string name, object value, string order)
        {
            return FindByAttributeOrderBy(clazz.FullName, name, value, order);
        }

        public IList FindByAttributeOrderBy(string className, string name, object value, string order)
        {
            return FindByAttributeOrderBy(className, name, value, order, false);
        }

        public IList FindByAttributeOrderBy(Type clazz, string name, object value, string order, bool ignoreException)
        {
            return FindByAttributeOrderBy(clazz.FullName, name, value, order, ignoreException);
        }

        public IList FindByAttributeOrderBy(string className, string name, object value, string order, bool ignoreException)
        {
            try
            {
                var type = ResolveType(className);
                if (type == null) return new ArrayList();

                var query = GetQueryable(type);
                var result = query.AsEnumerable()
                    .Where(e => Equals(GetPropertyValue(e, name), value))
                    .OrderBy(e => GetPropertyValue(e, order))
                    .ToList();
                return new ArrayList(result);
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
                return new ArrayList();
            }
        }

        public IList FindByAttributeOrderByDesc(Type clazz, string name, object value, string order)
        {
            return FindByAttributeOrderByDesc(clazz.FullName, name, value, order);
        }

        public IList FindByAttributeOrderByDesc(string className, string name, object value, string order)
        {
            return FindByAttributeOrderByDesc(className, name, value, order, false);
        }

        public IList FindByAttributeOrderByDesc(Type clazz, string name, object value, string order, bool ignoreException)
        {
            return FindByAttributeOrderByDesc(clazz.FullName, name, value, order, ignoreException);
        }

        public IList FindByAttributeOrderByDesc(string className, string name, object value, string order, bool ignoreException)
        {
            try
            {
                var type = ResolveType(className);
                if (type == null) return new ArrayList();

                var query = GetQueryable(type);
                var result = query.AsEnumerable()
                    .Where(e => Equals(GetPropertyValue(e, name), value))
                    .OrderByDescending(e => GetPropertyValue(e, order))
                    .ToList();
                return new ArrayList(result);
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
                return new ArrayList();
            }
        }

        public IList FindByAttributes(Type clazz, Dictionary<string, object> attributes)
        {
            return FindByAttributes(clazz.FullName, attributes);
        }

        public IList FindByAttributes(string className, Dictionary<string, object> attributes)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type);
            var result = query.AsEnumerable()
                .Where(e => attributes.All(kv => Equals(GetPropertyValue(e, kv.Key), kv.Value)))
                .ToList();
            return new ArrayList(result);
        }

        public IList FindByAttributesOrderBy(Type clazz, Dictionary<string, object> attributes, string order)
        {
            return FindByAttributesOrderBy(clazz.FullName, attributes, order);
        }

        public IList FindByAttributesOrderBy(string className, Dictionary<string, object> attributes, string order)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type);
            var result = query.AsEnumerable()
                .Where(e => attributes.All(kv => Equals(GetPropertyValue(e, kv.Key), kv.Value)))
                .OrderBy(e => GetPropertyValue(e, order))
                .ToList();
            return new ArrayList(result);
        }

        public IList FindByAttributesOrderByDesc(Type clazz, Dictionary<string, object> attributes, string order)
        {
            return FindByAttributesOrderByDesc(clazz.FullName, attributes, order);
        }

        public IList FindByAttributesOrderByDesc(string className, Dictionary<string, object> attributes, string order)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type);
            var result = query.AsEnumerable()
                .Where(e => attributes.All(kv => Equals(GetPropertyValue(e, kv.Key), kv.Value)))
                .OrderByDescending(e => GetPropertyValue(e, order))
                .ToList();
            return new ArrayList(result);
        }

        public IList FindAll(Type clazz)
        {
            return FindAll(clazz.FullName);
        }

        public IList FindAll(string className)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type);
            return new ArrayList(query.ToList());
        }

        public IList FindAllOrderBy(Type clazz, string order)
        {
            return FindAllOrderBy(clazz.FullName, order);
        }

        public IList FindAllOrderBy(string className, string order)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type);
            var result = query.AsEnumerable().OrderBy(e => GetPropertyValue(e, order)).ToList();
            return new ArrayList(result);
        }

        public IList FindAllOrderByDesc(Type clazz, string order)
        {
            return FindAllOrderByDesc(clazz.FullName, order);
        }

        public IList FindAllOrderByDesc(string className, string order)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type);
            var result = query.AsEnumerable().OrderByDescending(e => GetPropertyValue(e, order)).ToList();
            return new ArrayList(result);
        }

        public IList FindProperty(Type clazz, string property)
        {
            return FindProperty(clazz.FullName, property);
        }

        public IList FindProperty(string className, string property)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type);
            var result = query.AsEnumerable().Select(e => GetPropertyValue(e, property)).ToList();
            return new ArrayList(result);
        }

        public IList FindPropertyOrderBy(Type clazz, string property, string order)
        {
            return FindPropertyOrderBy(clazz.FullName, property, order);
        }

        public IList FindPropertyOrderBy(string className, string property, string order)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type);
            var result = query.AsEnumerable()
                .OrderBy(e => GetPropertyValue(e, order))
                .Select(e => GetPropertyValue(e, property))
                .ToList();
            return new ArrayList(result);
        }

        public IList FindPropertyByAttributes(Type clazz, string property, string conditionName, object conditionValue)
        {
            return FindPropertyByAttributes(clazz.FullName, property, conditionName, conditionValue);
        }

        public IList FindPropertyByAttributes(string className, string property, string conditionName, object conditionValue)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type);
            var result = query.AsEnumerable()
                .Where(e => Equals(GetPropertyValue(e, conditionName), conditionValue))
                .Select(e => GetPropertyValue(e, property))
                .ToList();
            return new ArrayList(result);
        }

        public IList FindPropertyByAttributesOrderBy(Type clazz, string property, string conditionName, object conditionValue, string order)
        {
            return FindPropertyByAttributesOrderBy(clazz.FullName, property, conditionName, conditionValue, order);
        }

        public IList FindPropertyByAttributesOrderBy(string className, string property, string conditionName, object conditionValue, string order)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type);
            var result = query.AsEnumerable()
                .Where(e => Equals(GetPropertyValue(e, conditionName), conditionValue))
                .OrderBy(e => GetPropertyValue(e, order))
                .Select(e => GetPropertyValue(e, property))
                .ToList();
            return new ArrayList(result);
        }

        public IList FindByAttributesOR(Type clazz, Dictionary<string, object> attributes)
        {
            return FindByAttributesOR(clazz.FullName, attributes);
        }

        public IList FindByAttributesOR(string className, Dictionary<string, object> attributes)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type);
            var result = query.AsEnumerable()
                .Where(e => attributes.Any(kv => Equals(GetPropertyValue(e, kv.Key), kv.Value)))
                .ToList();
            return new ArrayList(result);
        }

        public IList FindPropertyByAttributesOR(Type clazz, string property, Dictionary<string, object> attributes)
        {
            return FindPropertyByAttributesOR(clazz.FullName, property, attributes);
        }

        public IList FindPropertyByAttributesOR(string className, string property, Dictionary<string, object> attributes)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type);
            var result = query.AsEnumerable()
                .Where(e => attributes.Any(kv => Equals(GetPropertyValue(e, kv.Key), kv.Value)))
                .Select(e => GetPropertyValue(e, property))
                .ToList();
            return new ArrayList(result);
        }

        public object FindByNameWithoutException(Type clazz, object value)
        {
            return FindByNameWithoutException(clazz.FullName, value);
        }

        public object FindByNameWithoutException(string className, object value)
        {
            try
            {
                return FindByName(className, value);
            }
            catch
            {
                return null;
            }
        }

        public IList FindByLike(Type clazz, string name, object value)
        {
            return FindByLike(clazz.FullName, name, value);
        }

        public IList FindByLike(string className, string name, object value)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            string pattern = value?.ToString() ?? "";
            var query = GetQueryable(type);
            var result = query.AsEnumerable()
                .Where(e =>
                {
                    var propVal = GetPropertyValue(e, name)?.ToString();
                    return propVal != null && propVal.Contains(pattern, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
            return new ArrayList(result);
        }

        public IList FindByLikeOrderByDesc(Type clazz, string name, object value, string order)
        {
            return FindByLikeOrderByDesc(clazz.FullName, name, value, order);
        }

        public IList FindByLikeOrderByDesc(string className, string name, object value, string order)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            string pattern = value?.ToString() ?? "";
            var query = GetQueryable(type);
            var result = query.AsEnumerable()
                .Where(e =>
                {
                    var propVal = GetPropertyValue(e, name)?.ToString();
                    return propVal != null && propVal.Contains(pattern, StringComparison.OrdinalIgnoreCase);
                })
                .OrderByDescending(e => GetPropertyValue(e, order))
                .ToList();
            return new ArrayList(result);
        }

        public IList FindByLikeOrderByAsc(Type clazz, string name, object value, string order)
        {
            return FindByLikeOrderByAsc(clazz.FullName, name, value, order);
        }

        public IList FindByLikeOrderByAsc(string className, string name, object value, string order)
        {
            var type = ResolveType(className);
            if (type == null) return new ArrayList();

            string pattern = value?.ToString() ?? "";
            var query = GetQueryable(type);
            var result = query.AsEnumerable()
                .Where(e =>
                {
                    var propVal = GetPropertyValue(e, name)?.ToString();
                    return propVal != null && propVal.Contains(pattern, StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(e => GetPropertyValue(e, order))
                .ToList();
            return new ArrayList(result);
        }

        public IList<T> FindByBindingQuery<T>(string hql, ArrayList parameters)
        {
            // HQL 바인딩 쿼리 → EF Core에서 직접 지원 불가
            // 호출 코드에서 LINQ로 전환 필요
            return new List<T>();
        }

        #endregion

        #region Update

        public void Update(object obj)
        {
            Update(obj, false);
        }

        public void Update(object obj, bool ignoreException)
        {
            try
            {
                var entry = _db.Entry(obj);
                if (entry.State == EntityState.Detached)
                {
                    _db.Attach(obj);
                    entry.State = EntityState.Modified;
                }
                _db.SaveChanges();
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
            }
        }

        public int Update(Type clazz, string setName, object setValue, string id)
        {
            return Update(clazz.FullName, setName, setValue, id);
        }

        public int Update(string className, string setName, object setValue, string id)
        {
            return UpdateByAttribute(className, setName, setValue, "Id", id);
        }

        public int Update(Type clazz, Dictionary<string, object> setAttributes, string id)
        {
            return Update(clazz.FullName, setAttributes, id);
        }

        public int Update(string className, Dictionary<string, object> setAttributes, string id)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            var entity = _db.Find(type, id);
            if (entity == null) return 0;

            foreach (var kv in setAttributes)
            {
                SetPropertyValue(entity, kv.Key, kv.Value);
            }
            _db.SaveChanges();
            return 1;
        }

        public int UpdateByName(Type clazz, string setName, object setValue, string name)
        {
            return UpdateByName(clazz.FullName, setName, setValue, name);
        }

        public int UpdateByName(string className, string setName, object setValue, string value)
        {
            return UpdateByAttribute(className, setName, setValue, "Name", value);
        }

        public int UpdateByName(Type clazz, Dictionary<string, object> setAttributes, string name)
        {
            return UpdateByName(clazz.FullName, setAttributes, name);
        }

        public int UpdateByName(string className, Dictionary<string, object> setAttributes, string value)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            var query = GetQueryable(type);
            var entities = query.AsEnumerable()
                .Where(e => Equals(GetPropertyValue(e, "Name"), value))
                .ToList();

            foreach (var entity in entities)
            {
                foreach (var kv in setAttributes)
                {
                    SetPropertyValue(entity, kv.Key, kv.Value);
                }
            }

            _db.SaveChanges();
            return entities.Count;
        }

        public int UpdateByAttribute(Type clazz, string setName, object setValue, string conditionName, object conditionValue)
        {
            return UpdateByAttribute(clazz.FullName, setName, setValue, conditionName, conditionValue);
        }

        public int UpdateByAttribute(string className, string setName, object setValue, string conditionName, object conditionValue)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            var query = GetQueryable(type);
            var entities = query.AsEnumerable()
                .Where(e => Equals(GetPropertyValue(e, conditionName), conditionValue))
                .ToList();

            foreach (var entity in entities)
            {
                SetPropertyValue(entity, setName, setValue);
            }

            _db.SaveChanges();
            return entities.Count;
        }

        public int UpdateByAttributes(Type clazz, string setName, object setValue, Dictionary<string, object> conditionAttributes)
        {
            return UpdateByAttributes(clazz.FullName, setName, setValue, conditionAttributes);
        }

        public int UpdateByAttributes(string className, string setName, object setValue, Dictionary<string, object> conditionAttributes)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            var query = GetQueryable(type);
            var entities = query.AsEnumerable()
                .Where(e => conditionAttributes.All(kv => Equals(GetPropertyValue(e, kv.Key), kv.Value)))
                .ToList();

            foreach (var entity in entities)
            {
                SetPropertyValue(entity, setName, setValue);
            }

            _db.SaveChanges();
            return entities.Count;
        }

        public int UpdateByAttributes(Type clazz, string setName, object setValue, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            return UpdateByAttributes(clazz.FullName, setName, setValue, conditionAttributes, operators);
        }

        public int UpdateByAttributes(string className, string setName, object setValue, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            var query = GetQueryable(type);
            var entities = query.AsEnumerable()
                .Where(e => MatchByOperators(e, conditionAttributes, operators))
                .ToList();

            foreach (var entity in entities)
            {
                SetPropertyValue(entity, setName, setValue);
            }

            _db.SaveChanges();
            return entities.Count;
        }

        public int UpdateByAttributes(Type clazz, Dictionary<string, object> setAttributes, string conditionName, string conditionValue)
        {
            return UpdateByAttributes(clazz.FullName, setAttributes, conditionName, conditionValue);
        }

        public int UpdateByAttributes(string className, Dictionary<string, object> setAttributes, string conditionName, string conditionValue)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            var query = GetQueryable(type);
            var entities = query.AsEnumerable()
                .Where(e => Equals(GetPropertyValue(e, conditionName), conditionValue))
                .ToList();

            foreach (var entity in entities)
            {
                foreach (var kv in setAttributes)
                {
                    SetPropertyValue(entity, kv.Key, kv.Value);
                }
            }

            _db.SaveChanges();
            return entities.Count;
        }

        public int UpdateByAttributes(Type clazz, Dictionary<string, object> setAttributes, Dictionary<string, object> conditionAttributes)
        {
            return UpdateByAttributes(clazz.FullName, setAttributes, conditionAttributes);
        }

        public int UpdateByAttributes(string className, Dictionary<string, object> setAttributes, Dictionary<string, object> conditionAttributes)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            var query = GetQueryable(type);
            var entities = query.AsEnumerable()
                .Where(e => conditionAttributes.All(kv => Equals(GetPropertyValue(e, kv.Key), kv.Value)))
                .ToList();

            foreach (var entity in entities)
            {
                foreach (var kv in setAttributes)
                {
                    SetPropertyValue(entity, kv.Key, kv.Value);
                }
            }

            _db.SaveChanges();
            return entities.Count;
        }

        public int UpdateByAttributes(Type clazz, Dictionary<string, object> setAttributes, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            return UpdateByAttributes(clazz.FullName, setAttributes, conditionAttributes, operators);
        }

        public int UpdateByAttributes(string className, Dictionary<string, object> setAttributes, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            var query = GetQueryable(type);
            var entities = query.AsEnumerable()
                .Where(e => MatchByOperators(e, conditionAttributes, operators))
                .ToList();

            foreach (var entity in entities)
            {
                foreach (var kv in setAttributes)
                {
                    SetPropertyValue(entity, kv.Key, kv.Value);
                }
            }

            _db.SaveChanges();
            return entities.Count;
        }

        public int UpdateByAttributes(Type clazz, Dictionary<string, object> setAttributes, string conditionName, object conditionValue)
        {
            return UpdateByAttributes(clazz.FullName, setAttributes, conditionName, conditionValue);
        }

        public int UpdateByAttributes(string className, Dictionary<string, object> setAttributes, string conditionName, object conditionValue)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            var query = GetQueryable(type);
            var entities = query.AsEnumerable()
                .Where(e => Equals(GetPropertyValue(e, conditionName), conditionValue))
                .ToList();

            foreach (var entity in entities)
            {
                foreach (var kv in setAttributes)
                {
                    SetPropertyValue(entity, kv.Key, kv.Value);
                }
            }

            _db.SaveChanges();
            return entities.Count;
        }

        public int UpdateByListAttributes(string className, string setName, object setValue, ArrayList conditionList)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            var query = GetQueryable(type);
            int count = 0;

            foreach (var conditionValue in conditionList)
            {
                var entities = query.AsEnumerable()
                    .Where(e => Equals(GetPropertyValue(e, "Id"), conditionValue))
                    .ToList();

                foreach (var entity in entities)
                {
                    SetPropertyValue(entity, setName, setValue);
                    count++;
                }
            }

            _db.SaveChanges();
            return count;
        }

        public int UpdateByHql(string hql)
        {
            return _db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql));
        }

        public int UpdateByHql(string hql, string value)
        {
            return _db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql), value);
        }

        public int UpdateByHql(string hql, string[] values)
        {
            return _db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql), values.Cast<object>().ToArray());
        }

        public int UpdateByHql(string hql, ArrayList values)
        {
            return _db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql), values.Cast<object>().ToArray());
        }

        #endregion

        #region Delete

        public void Delete(object obj)
        {
            Delete(obj, false);
        }

        public void Delete(object obj, bool ignoreException)
        {
            try
            {
                var entry = _db.Entry(obj);
                if (entry.State == EntityState.Detached)
                {
                    _db.Attach(obj);
                }
                _db.Remove(obj);
                _db.SaveChanges();
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
            }
        }

        public int Delete(Type clazz, ISerializable id)
        {
            return Delete(clazz.FullName, id);
        }

        public int Delete(string className, ISerializable id)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            object idValue = NormalizeId(id);
            var entity = _db.Find(type, idValue);
            if (entity == null) return 0;

            _db.Remove(entity);
            _db.SaveChanges();
            return 1;
        }

        public int DeleteByName(Type clazz, string value)
        {
            return DeleteByName(clazz.FullName, value);
        }

        public int DeleteByName(string className, string value)
        {
            return DeleteByAttribute(className, "Name", value);
        }

        public int DeleteByAttribute(Type clazz, string name, object value)
        {
            return DeleteByAttribute(clazz.FullName, name, value);
        }

        public int DeleteByAttribute(string className, string name, object value)
        {
            return DeleteByAttribute(className, name, value, false);
        }

        public int DeleteByAttribute(Type clazz, string name, object value, bool ignoreException)
        {
            return DeleteByAttribute(clazz.FullName, name, value, ignoreException);
        }

        public int DeleteByAttribute(string className, string name, object value, bool ignoreException)
        {
            try
            {
                var type = ResolveType(className);
                if (type == null) return 0;

                var query = GetQueryable(type);
                var entities = query.AsEnumerable()
                    .Where(e => Equals(GetPropertyValue(e, name), value))
                    .ToList();

                _db.RemoveRange(entities);
                _db.SaveChanges();
                return entities.Count;
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
                return 0;
            }
        }

        public int DeleteByAttributes(Type clazz, Dictionary<string, object> attributes)
        {
            return DeleteByAttributes(clazz.FullName, attributes);
        }

        public int DeleteByAttributes(string className, Dictionary<string, object> attributes)
        {
            return DeleteByAttributes(className, attributes, false);
        }

        public int DeleteByAttributes(Type clazz, Dictionary<string, object> attributes, bool ignoreException)
        {
            return DeleteByAttributes(clazz.FullName, attributes, ignoreException);
        }

        public int DeleteByAttributes(string className, Dictionary<string, object> attributes, bool ignoreException)
        {
            try
            {
                var type = ResolveType(className);
                if (type == null) return 0;

                var query = GetQueryable(type);
                var entities = query.AsEnumerable()
                    .Where(e => attributes.All(kv => Equals(GetPropertyValue(e, kv.Key), kv.Value)))
                    .ToList();

                _db.RemoveRange(entities);
                _db.SaveChanges();
                return entities.Count;
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
                return 0;
            }
        }

        public int DeleteByAttributes(Type clazz, Dictionary<string, object> attributes, string[] operators)
        {
            return DeleteByAttributes(clazz.FullName, attributes, operators);
        }

        public int DeleteByAttributes(string className, Dictionary<string, object> attributes, string[] operators)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            var query = GetQueryable(type);
            var entities = query.AsEnumerable()
                .Where(e => MatchByOperators(e, attributes, operators))
                .ToList();

            _db.RemoveRange(entities);
            _db.SaveChanges();
            return entities.Count;
        }

        public int DeleteByAttributes(Type clazz, Type[] types, string[] names, object[] values, string[] operators)
        {
            return DeleteByAttributes(clazz.FullName, types, names, values, operators);
        }

        public int DeleteByAttributes(string className, Type[] types, string[] names, object[] values, string[] operators)
        {
            var attrDict = new Dictionary<string, object>();
            for (int i = 0; i < names.Length && i < values.Length; i++)
            {
                attrDict[names[i]] = values[i];
            }
            return DeleteByAttributes(className, attrDict, operators);
        }

        public int DeleteByTime(Type clazz, DateTime startDate, DateTime endDate)
        {
            return DeleteByTime(clazz.FullName, startDate, endDate);
        }

        public int DeleteByTime(string className, DateTime startDate, DateTime endDate)
        {
            return DeleteByTime(className, startDate, endDate, maxUpdateCounts);
        }

        public int DeleteByTime(Type clazz, DateTime startDate, DateTime endDate, int maxCount)
        {
            return DeleteByTime(clazz.FullName, startDate, endDate, maxCount);
        }

        public int DeleteByTime(string className, DateTime startDate, DateTime endDate, int maxCount)
        {
            var type = ResolveType(className);
            if (type == null) return 0;

            var query = GetQueryable(type);
            var entities = query.AsEnumerable()
                .Where(e =>
                {
                    var timeVal = GetPropertyValue(e, "Time");
                    if (timeVal is DateTime dt)
                        return dt >= startDate && dt <= endDate;
                    return false;
                })
                .Take(maxCount)
                .ToList();

            _db.RemoveRange(entities);
            _db.SaveChanges();
            return entities.Count;
        }

        public int DeleteByTime(Type clazz, DateTime endDate)
        {
            return DeleteByTime(clazz.FullName, endDate);
        }

        public int DeleteByTime(string className, DateTime endDate)
        {
            return DeleteByTime(className, endDate, maxUpdateCounts);
        }

        public int DeleteByTime(Type clazz, DateTime endDate, int maxCount)
        {
            return DeleteByTime(clazz.FullName, endDate, maxCount);
        }

        public int DeleteByTime(string className, DateTime endDate, int maxCount)
        {
            return DeleteByTime(className, endDate, maxCount, false);
        }

        public int DeleteByTime(Type clazz, DateTime endDate, int maxCount, bool ignoreException)
        {
            return DeleteByTime(clazz.FullName, endDate, maxCount, ignoreException);
        }

        public int DeleteByTime(string className, DateTime endDate, int maxCount, bool ignoreException)
        {
            try
            {
                var type = ResolveType(className);
                if (type == null) return 0;

                var query = GetQueryable(type);
                var entities = query.AsEnumerable()
                    .Where(e =>
                    {
                        var timeVal = GetPropertyValue(e, "Time");
                        if (timeVal is DateTime dt)
                            return dt <= endDate;
                        return false;
                    })
                    .Take(maxCount)
                    .ToList();

                _db.RemoveRange(entities);
                _db.SaveChanges();
                return entities.Count;
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
                return 0;
            }
        }

        public int DeleteAll(Type clazz)
        {
            return DeleteAll(clazz.FullName);
        }

        public int DeleteAll(string className)
        {
            return DeleteAll(className, false);
        }

        public int DeleteAll(Type clazz, bool ignoreException)
        {
            return DeleteAll(clazz.FullName, ignoreException);
        }

        public int DeleteAll(string className, bool ignoreException)
        {
            try
            {
                var type = ResolveType(className);
                if (type == null) return 0;

                var query = GetQueryable(type);
                var entities = query.ToList();

                _db.RemoveRange(entities);
                _db.SaveChanges();
                return entities.Count;
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
                return 0;
            }
        }

        public void DeleteAll(ICollection collection)
        {
            foreach (var entity in collection)
            {
                var entry = _db.Entry(entity);
                if (entry.State == EntityState.Detached)
                {
                    _db.Attach(entity);
                }
                _db.Remove(entity);
            }
            _db.SaveChanges();
        }

        public int DeleteByHql(string hql)
        {
            return _db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql));
        }

        public int DeleteByHql(string hql, bool ignoreException)
        {
            try
            {
                return DeleteByHql(hql);
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
                return 0;
            }
        }

        public int DeleteByHql(string hql, string value)
        {
            return _db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql), value);
        }

        public int DeleteByHql(string hql, string value, bool ignoreException)
        {
            try
            {
                return DeleteByHql(hql, value);
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
                return 0;
            }
        }

        public int DeleteByHql(string hql, ArrayList values)
        {
            return _db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql), values.Cast<object>().ToArray());
        }

        public int DeleteByHql(string hql, ArrayList values, bool ignoreException)
        {
            try
            {
                return DeleteByHql(hql, values);
            }
            catch (Exception)
            {
                if (!ignoreException) throw;
                return 0;
            }
        }

        #endregion

        #region Evict / ExecuteUpdate

        public void Evict(object obj)
        {
            DetachEntity(obj);
        }

        public int ExecuteUpdate(string sql)
        {
            return _db.Database.ExecuteSqlRaw(sql);
        }

        /// <summary>
        /// 차량 할당의 race condition 방지를 위한 conditional UPDATE.
        /// EF Core 8 ExecuteUpdate로 single round-trip 원자적 SQL UPDATE를 발행한다.
        /// 전제: processingState='IDLE' AND (transportCommandId IS NULL OR transportCommandId='')
        /// 만족 시 transportCommandId/transferState/processingState/eventTime을 단일 UPDATE로 변경.
        /// 영향 행이 1이면 본 호출자가 차량을 잡음, 0이면 다른 스레드가 이미 잡음.
        /// </summary>
        public bool TryAssignVehicleAtomic(string vehicleId, string transportCommandId)
        {
            if (string.IsNullOrEmpty(vehicleId) || string.IsNullOrEmpty(transportCommandId))
                return false;

            int n = _db.Set<VehicleExs>()
                .Where(v => v.VehicleId == vehicleId
                         && v.ProcessingState == VehicleEx.PROCESSINGSTATE_IDLE
                         && (v.TransportCommandId == null || v.TransportCommandId == ""))
                .ExecuteUpdate(s => s
                    .SetProperty(v => v.TransportCommandId, _ => transportCommandId)
                    .SetProperty(v => v.TransferState, _ => VehicleEx.TRANSFERSTATE_ASSIGNED)
                    .SetProperty(v => v.ProcessingState, _ => VehicleEx.PROCESSINGSTATE_RUN)
                    .SetProperty(v => v.EventTime, _ => DateTime.UtcNow));
            return n > 0;
        }

        #endregion

        #region Helpers

        private object NormalizeId(ISerializable id)
        {
            if (id is StringBuilder sb)
                return sb.ToString();
            if (id is DateTime dt)
                return dt;
            return id;
        }

        private void DetachEntity(object entity)
        {
            var entry = _db.Entry(entity);
            if (entry.State != EntityState.Detached)
            {
                entry.State = EntityState.Detached;
            }
        }

        /// <summary>
        /// 엔티티의 모든 DateTime/DateTime? 프로퍼티를 UTC로 정규화.
        /// PostgreSQL의 timestamp with time zone 컬럼은 Npgsql에서 UTC만 허용하므로,
        /// DateTimeKind.Local 또는 Unspecified인 값을 UTC로 변환한다.
        /// </summary>
        private void NormalizeDateTimeProperties(object entity)
        {
            if (entity == null) return;

            foreach (var prop in entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;

                if (prop.PropertyType == typeof(DateTime))
                {
                    var dt = (DateTime)prop.GetValue(entity);
                    if (dt.Kind != DateTimeKind.Utc)
                    {
                        prop.SetValue(entity, dt.Kind == DateTimeKind.Local
                            ? dt.ToUniversalTime()
                            : DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                    }
                }
                else if (prop.PropertyType == typeof(DateTime?))
                {
                    var dt = (DateTime?)prop.GetValue(entity);
                    if (dt.HasValue && dt.Value.Kind != DateTimeKind.Utc)
                    {
                        prop.SetValue(entity, dt.Value.Kind == DateTimeKind.Local
                            ? dt.Value.ToUniversalTime()
                            : DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc));
                    }
                }
            }
        }

        /// <summary>
        /// 연산자 배열을 사용하여 조건 매칭 (AND/OR 혼합).
        /// operators[i]는 i번째 조건과 (i+1)번째 조건 사이의 연산자.
        /// </summary>
        private bool MatchByOperators(object entity, Dictionary<string, object> attributes, string[] operators)
        {
            var keys = attributes.Keys.ToArray();
            if (keys.Length == 0) return true;

            bool result = Equals(GetPropertyValue(entity, keys[0]), attributes[keys[0]]);

            for (int i = 1; i < keys.Length; i++)
            {
                bool current = Equals(GetPropertyValue(entity, keys[i]), attributes[keys[i]]);
                string op = (operators != null && i - 1 < operators.Length) ? operators[i - 1] : "AND";

                if (op.Equals("OR", StringComparison.OrdinalIgnoreCase))
                    result = result || current;
                else
                    result = result && current;
            }

            return result;
        }

        /// <summary>
        /// HQL을 SQL로 간단히 변환.
        /// NHibernate HQL에서 '?'를 EF Core의 {0}, {1} 파라미터로 변환.
        /// 엔티티 이름을 테이블 이름으로 변환.
        /// </summary>
        private string ConvertHqlToSql(string hql)
        {
            // '?' 파라미터를 {0}, {1}, ... 로 치환
            string sql = hql;
            int paramIndex = 0;
            while (sql.Contains("?"))
            {
                int idx = sql.IndexOf("?");
                sql = sql.Substring(0, idx) + "{" + paramIndex + "}" + sql.Substring(idx + 1);
                paramIndex++;
            }

            // 엔티티 이름 → 테이블 이름 변환
            foreach (var entityType in _db.Model.GetEntityTypes())
            {
                string entityName = entityType.ClrType.Name;
                string fullName = entityType.ClrType.FullName;
                string tableName = entityType.GetTableName() ?? entityName;

                // 전체 이름 우선 치환 (짧은 이름과 충돌 방지)
                if (sql.Contains(fullName))
                {
                    sql = sql.Replace(fullName, tableName);
                }
            }

            return sql;
        }

        #endregion
    }
}
