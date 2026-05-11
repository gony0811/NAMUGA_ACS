using System;
using System.Collections;
using System.Collections.Generic;

using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ACS.Core.Base.Interface;
using ACS.Core.Logging;

namespace ACS.Database
{
    /// <summary>
    /// EF Core 기반 IPersistentDao 구현.
    ///
    /// DbContext 수명 정책:
    ///   각 public 메서드 호출마다 `using var db = new AcsDbContext()` 로 fresh DbContext 를 생성.
    ///   이전의 `[ThreadStatic] _threadDb` 패턴은 long-lived 워커 스레드(Quartz, RabbitMQ 컨슈머, Elsa)에서
    ///   ChangeTracker snapshot 이 누적/손상되어 UPDATE 가 silent drop 되는 버그(JOB013/JOB016 사례)를
    ///   유발했으므로 폐기. EF Core 와 Npgsql 은 per-operation DbContext + connection pooling 을 위해
    ///   설계되었으므로 성능 영향 없음.
    /// </summary>
    public class EfCorePersistentDao : IPersistentDao
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(EfCorePersistentDao));

        private int maxResults = 1000;
        private int maxUpdateCounts = 1000;
        private int dataAccessRetryCount = 3;
        private long dataAccessRetrySleep = 300L;

        // NHibernate 엔티티 이름 → CLR Type 매핑 (런타임 캐싱). DbContext.Model 은
        // 모든 인스턴스에서 동일한 메타데이터를 노출하므로 string→Type 캐시는 안전하게 공유 가능.
        private static readonly Dictionary<string, Type> _entityTypeCache = new Dictionary<string, Type>();
        private static readonly object _cacheLock = new object();

        public EfCorePersistentDao(AcsDbContext db)
        {
            // DI 호환을 위해 생성자 시그너처는 유지하지만 인스턴스는 사용하지 않는다.
            // 각 메서드가 자체 DbContext 를 생성/해제한다.
            _ = db;
        }

        /// <summary>
        /// 새 DbContext 를 생성한다. AcsDbContext 의 매개변수 없는 생성자는 앱 시작 시 캐싱된
        /// connection string 을 사용하므로 DI 외부에서도 동일 DB 에 연결된다.
        /// </summary>
        private static AcsDbContext NewDb() => new AcsDbContext();

        #region Type Resolution

        /// <summary>
        /// 클래스 이름(단순명 또는 정규명)을 CLR Type 으로 변환.
        /// </summary>
        private Type ResolveType(string className, AcsDbContext db)
        {
            if (string.IsNullOrEmpty(className)) return null;

            lock (_cacheLock)
            {
                if (_entityTypeCache.TryGetValue(className, out var cached))
                    return cached;
            }

            var entityType = db.Model.GetEntityTypes()
                .FirstOrDefault(et =>
                    et.ClrType.FullName == className ||
                    et.ClrType.Name == className);

            if (entityType == null)
            {
                var requestedType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(className))
                    .FirstOrDefault(t => t != null);

                if (requestedType != null)
                {
                    entityType = db.Model.GetEntityTypes()
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

        private IQueryable<object> GetQueryable(Type clazz, AcsDbContext db)
        {
            var setMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes);
            var genericSet = setMethod.MakeGenericMethod(clazz);
            var dbSet = genericSet.Invoke(db, null);

            var castMethod = typeof(Queryable).GetMethod(nameof(Queryable.Cast)).MakeGenericMethod(typeof(object));
            return (IQueryable<object>)castMethod.Invoke(null, new[] { dbSet });
        }

        private object GetPropertyValue(object entity, string propertyName)
        {
            return entity?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(entity);
        }

        /// <summary>
        /// 엔티티의 프로퍼티 값을 리플렉션으로 설정하고 EF 트래커에 IsModified 를 명시한다.
        /// per-operation DbContext 에서도 방어적으로 IsModified 를 셋팅하여 어떤 경우에도
        /// UPDATE 에서 누락되지 않도록 한다.
        /// </summary>
        private void SetPropertyValue(object entity, string propertyName, object value, AcsDbContext db)
        {
            var prop = entity?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanWrite)
            {
                if (value is DateTime dt && dt.Kind == DateTimeKind.Local)
                    value = dt.ToUniversalTime();

                prop.SetValue(entity, value);

                try
                {
                    var entry = db.Entry(entity);
                    if (entry.State != EntityState.Detached)
                    {
                        var efProp = entry.Property(prop.Name);
                        if (efProp != null) efProp.IsModified = true;
                    }
                }
                catch
                {
                    // EF 모델에 매핑되지 않은 속성이거나 추적 비활성 — 안전 무시
                }
            }
        }

        private string GetKeyPropertyName(Type clazz, AcsDbContext db)
        {
            var entityType = db.Model.FindEntityType(clazz);
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
            using var db = NewDb();
            return db.Model.FindEntityType(clazz) != null;
        }

        public bool Use(string className)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            return type != null && db.Model.FindEntityType(type) != null;
        }

        public object Exist(Type clazz, ISerializable id)
        {
            return Exist(clazz.FullName, id);
        }

        public object Exist(string className, ISerializable id)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return null;

            object idValue = NormalizeId(id);
            return db.Find(type, idValue);
        }

        public object ExistByName(Type clazz, object value)
        {
            return ExistByName(clazz.FullName, value);
        }

        public object ExistByName(string className, object value)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return null;

            var query = GetQueryable(type, db);
            return query.AsEnumerable().FirstOrDefault(e => Equals(GetPropertyValue(e, "Name"), value));
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
                    using var db = NewDb();
                    db.Add(obj);
                    db.SaveChanges();
                    return;
                }
                catch (Exception)
                {
                    tryCount++;
                    if (tryCount > dataAccessRetryCount) throw;
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
                    using var db = NewDb();
                    var type = obj.GetType();
                    var keyName = GetKeyPropertyName(type, db);
                    var keyValue = GetPropertyValue(obj, keyName);

                    var existing = keyValue != null ? db.Find(type, keyValue) : null;
                    if (existing != null)
                    {
                        db.Entry(existing).CurrentValues.SetValues(obj);
                    }
                    else
                    {
                        db.Add(obj);
                    }
                    db.SaveChanges();
                    return;
                }
                catch (Exception)
                {
                    tryCount++;
                    if (tryCount > dataAccessRetryCount) throw;
                    System.Threading.Thread.Sleep((int)dataAccessRetrySleep);
                }
            } while (tryCount <= dataAccessRetryCount);
        }

        public void Flush()
        {
            // per-operation DbContext 모델에서는 외부에서 호출되는 명시적 Flush 가 의미 없다.
            // 모든 변경은 각 메서드 내부에서 SaveChanges 로 즉시 커밋된다.
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
            using var db = NewDb();
            return db.Set<T>().ToList();
        }

        public object Find(Type clazz, ISerializable id)
        {
            using var db = NewDb();
            object idValue = NormalizeId(id);
            return db.Find(clazz, idValue);
        }

        public object Find(string className, ISerializable id)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return null;
            object idValue = NormalizeId(id);
            return db.Find(type, idValue);
        }

        public object Find(Type clazz, ISerializable id, bool throwExceptionIfNotFound)
        {
            return Find(clazz, id);
        }

        public object Find(string className, ISerializable id, bool throwExceptionIfNotFound)
        {
            return Find(className, id);
        }

        public object FindByName(Type clazz, object value)
        {
            return FindByName(clazz.FullName, value);
        }

        public object FindByName(string className, object value)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return null;

            var query = GetQueryable(type, db);
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
                using var db = NewDb();
                var type = ResolveType(className, db);
                if (type == null) return new ArrayList();

                var query = GetQueryable(type, db);
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
                using var db = NewDb();
                var type = ResolveType(className, db);
                if (type == null) return new ArrayList();

                var query = GetQueryable(type, db);
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
                using var db = NewDb();
                var type = ResolveType(className, db);
                if (type == null) return new ArrayList();

                var query = GetQueryable(type, db);
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type, db);
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type, db);
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type, db);
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type, db);
            return new ArrayList(query.ToList());
        }

        public IList FindAllOrderBy(Type clazz, string order)
        {
            return FindAllOrderBy(clazz.FullName, order);
        }

        public IList FindAllOrderBy(string className, string order)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type, db);
            var result = query.AsEnumerable().OrderBy(e => GetPropertyValue(e, order)).ToList();
            return new ArrayList(result);
        }

        public IList FindAllOrderByDesc(Type clazz, string order)
        {
            return FindAllOrderByDesc(clazz.FullName, order);
        }

        public IList FindAllOrderByDesc(string className, string order)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type, db);
            var result = query.AsEnumerable().OrderByDescending(e => GetPropertyValue(e, order)).ToList();
            return new ArrayList(result);
        }

        public IList FindProperty(Type clazz, string property)
        {
            return FindProperty(clazz.FullName, property);
        }

        public IList FindProperty(string className, string property)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type, db);
            var result = query.AsEnumerable().Select(e => GetPropertyValue(e, property)).ToList();
            return new ArrayList(result);
        }

        public IList FindPropertyOrderBy(Type clazz, string property, string order)
        {
            return FindPropertyOrderBy(clazz.FullName, property, order);
        }

        public IList FindPropertyOrderBy(string className, string property, string order)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type, db);
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type, db);
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type, db);
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type, db);
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            var query = GetQueryable(type, db);
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            string pattern = value?.ToString() ?? "";
            var query = GetQueryable(type, db);
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            string pattern = value?.ToString() ?? "";
            var query = GetQueryable(type, db);
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return new ArrayList();

            string pattern = value?.ToString() ?? "";
            var query = GetQueryable(type, db);
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
            return new List<T>();
        }

        #endregion

        #region Update

        public void Update(object obj)
        {
            Update(obj, false);
        }

        /// <summary>
        /// 단일 엔티티 업데이트.
        /// per-operation DbContext 에서 항상 Detached 상태이므로 Attach 후 State=Modified 로 마킹.
        /// EF Core 가 모든 non-key 속성을 UPDATE 문에 포함시키므로 silent drop 불가.
        /// </summary>
        public void Update(object obj, bool ignoreException)
        {
            try
            {
                NormalizeDateTimeProperties(obj);
                using var db = NewDb();

                db.Attach(obj);
                db.Entry(obj).State = EntityState.Modified;

                int saved = db.SaveChanges();
                if (saved == 0)
                {
                    var keyName = GetKeyPropertyName(obj.GetType(), db);
                    var keyValue = GetPropertyValue(obj, keyName);
                    logger.Warn($"PersistentDao.Update: 0 rows affected. type={obj.GetType().Name}, {keyName}={keyValue}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"PersistentDao.Update: {ex.Message}", ex);
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            var entity = db.Find(type, id);
            if (entity == null) return 0;

            foreach (var kv in setAttributes)
            {
                SetPropertyValue(entity, kv.Key, kv.Value, db);
            }
            int saved = db.SaveChanges();
            if (saved == 0)
                logger.Warn($"PersistentDao.Update(dict): 0 rows affected. type={type.Name}, id={id}");
            return saved > 0 ? 1 : 0;
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            var query = GetQueryable(type, db);
            var entities = query.AsEnumerable()
                .Where(e => Equals(GetPropertyValue(e, "Name"), value))
                .ToList();

            foreach (var entity in entities)
            {
                foreach (var kv in setAttributes)
                {
                    SetPropertyValue(entity, kv.Key, kv.Value, db);
                }
            }

            int saved = db.SaveChanges();
            if (saved == 0 && entities.Count > 0)
                logger.Warn($"PersistentDao.UpdateByName: 0 rows affected. type={type.Name}, matched={entities.Count}, value={value}");
            return entities.Count;
        }

        public int UpdateByAttribute(Type clazz, string setName, object setValue, string conditionName, object conditionValue)
        {
            return UpdateByAttribute(clazz.FullName, setName, setValue, conditionName, conditionValue);
        }

        public int UpdateByAttribute(string className, string setName, object setValue, string conditionName, object conditionValue)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            var query = GetQueryable(type, db);
            var entities = query.AsEnumerable()
                .Where(e => Equals(GetPropertyValue(e, conditionName), conditionValue))
                .ToList();

            foreach (var entity in entities)
            {
                SetPropertyValue(entity, setName, setValue, db);
            }

            int saved = db.SaveChanges();
            if (saved == 0 && entities.Count > 0)
                logger.Warn($"PersistentDao.UpdateByAttribute: 0 rows affected. type={type.Name}, matched={entities.Count}, {conditionName}={conditionValue}, set {setName}={setValue}");
            return entities.Count;
        }

        public int UpdateByAttributes(Type clazz, string setName, object setValue, Dictionary<string, object> conditionAttributes)
        {
            return UpdateByAttributes(clazz.FullName, setName, setValue, conditionAttributes);
        }

        public int UpdateByAttributes(string className, string setName, object setValue, Dictionary<string, object> conditionAttributes)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            var query = GetQueryable(type, db);
            var entities = query.AsEnumerable()
                .Where(e => conditionAttributes.All(kv => Equals(GetPropertyValue(e, kv.Key), kv.Value)))
                .ToList();

            foreach (var entity in entities)
            {
                SetPropertyValue(entity, setName, setValue, db);
            }

            int saved = db.SaveChanges();
            if (saved == 0 && entities.Count > 0)
                logger.Warn($"PersistentDao.UpdateByAttributes: 0 rows affected. type={type.Name}, matched={entities.Count}");
            return entities.Count;
        }

        public int UpdateByAttributes(Type clazz, string setName, object setValue, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            return UpdateByAttributes(clazz.FullName, setName, setValue, conditionAttributes, operators);
        }

        public int UpdateByAttributes(string className, string setName, object setValue, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            var query = GetQueryable(type, db);
            var entities = query.AsEnumerable()
                .Where(e => MatchByOperators(e, conditionAttributes, operators))
                .ToList();

            foreach (var entity in entities)
            {
                SetPropertyValue(entity, setName, setValue, db);
            }

            int saved = db.SaveChanges();
            if (saved == 0 && entities.Count > 0)
                logger.Warn($"PersistentDao.UpdateByAttributes(op): 0 rows affected. type={type.Name}, matched={entities.Count}");
            return entities.Count;
        }

        public int UpdateByAttributes(Type clazz, Dictionary<string, object> setAttributes, string conditionName, string conditionValue)
        {
            return UpdateByAttributes(clazz.FullName, setAttributes, conditionName, conditionValue);
        }

        public int UpdateByAttributes(string className, Dictionary<string, object> setAttributes, string conditionName, string conditionValue)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            var query = GetQueryable(type, db);
            var entities = query.AsEnumerable()
                .Where(e => Equals(GetPropertyValue(e, conditionName), conditionValue))
                .ToList();

            foreach (var entity in entities)
            {
                foreach (var kv in setAttributes)
                {
                    SetPropertyValue(entity, kv.Key, kv.Value, db);
                }
            }

            int saved = db.SaveChanges();
            if (saved == 0 && entities.Count > 0)
                logger.Warn($"PersistentDao.UpdateByAttributes(dict-single): 0 rows affected. type={type.Name}, matched={entities.Count}");
            return entities.Count;
        }

        public int UpdateByAttributes(Type clazz, Dictionary<string, object> setAttributes, Dictionary<string, object> conditionAttributes)
        {
            return UpdateByAttributes(clazz.FullName, setAttributes, conditionAttributes);
        }

        public int UpdateByAttributes(string className, Dictionary<string, object> setAttributes, Dictionary<string, object> conditionAttributes)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            var query = GetQueryable(type, db);
            var entities = query.AsEnumerable()
                .Where(e => conditionAttributes.All(kv => Equals(GetPropertyValue(e, kv.Key), kv.Value)))
                .ToList();

            foreach (var entity in entities)
            {
                foreach (var kv in setAttributes)
                {
                    SetPropertyValue(entity, kv.Key, kv.Value, db);
                }
            }

            int saved = db.SaveChanges();
            if (saved == 0 && entities.Count > 0)
                logger.Warn($"PersistentDao.UpdateByAttributes(dict-dict): 0 rows affected. type={type.Name}, matched={entities.Count}");
            return entities.Count;
        }

        public int UpdateByAttributes(Type clazz, Dictionary<string, object> setAttributes, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            return UpdateByAttributes(clazz.FullName, setAttributes, conditionAttributes, operators);
        }

        public int UpdateByAttributes(string className, Dictionary<string, object> setAttributes, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            var query = GetQueryable(type, db);
            var entities = query.AsEnumerable()
                .Where(e => MatchByOperators(e, conditionAttributes, operators))
                .ToList();

            foreach (var entity in entities)
            {
                foreach (var kv in setAttributes)
                {
                    SetPropertyValue(entity, kv.Key, kv.Value, db);
                }
            }

            int saved = db.SaveChanges();
            if (saved == 0 && entities.Count > 0)
                logger.Warn($"PersistentDao.UpdateByAttributes(dict-dict-op): 0 rows affected. type={type.Name}, matched={entities.Count}");
            return entities.Count;
        }

        public int UpdateByAttributes(Type clazz, Dictionary<string, object> setAttributes, string conditionName, object conditionValue)
        {
            return UpdateByAttributes(clazz.FullName, setAttributes, conditionName, conditionValue);
        }

        public int UpdateByAttributes(string className, Dictionary<string, object> setAttributes, string conditionName, object conditionValue)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            var query = GetQueryable(type, db);
            var entities = query.AsEnumerable()
                .Where(e => Equals(GetPropertyValue(e, conditionName), conditionValue))
                .ToList();

            foreach (var entity in entities)
            {
                foreach (var kv in setAttributes)
                {
                    SetPropertyValue(entity, kv.Key, kv.Value, db);
                }
            }

            int saved = db.SaveChanges();
            if (saved == 0 && entities.Count > 0)
                logger.Warn($"PersistentDao.UpdateByAttributes(dict-name-obj): 0 rows affected. type={type.Name}, matched={entities.Count}");
            return entities.Count;
        }

        public int UpdateByListAttributes(string className, string setName, object setValue, ArrayList conditionList)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            var query = GetQueryable(type, db);
            int count = 0;

            foreach (var conditionValue in conditionList)
            {
                var entities = query.AsEnumerable()
                    .Where(e => Equals(GetPropertyValue(e, "Id"), conditionValue))
                    .ToList();

                foreach (var entity in entities)
                {
                    SetPropertyValue(entity, setName, setValue, db);
                    count++;
                }
            }

            int saved = db.SaveChanges();
            if (saved == 0 && count > 0)
                logger.Warn($"PersistentDao.UpdateByListAttributes: 0 rows affected. type={type.Name}, matched={count}");
            return count;
        }

        public int UpdateByHql(string hql)
        {
            using var db = NewDb();
            return db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql, db));
        }

        public int UpdateByHql(string hql, string value)
        {
            using var db = NewDb();
            return db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql, db), value);
        }

        public int UpdateByHql(string hql, string[] values)
        {
            using var db = NewDb();
            return db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql, db), values.Cast<object>().ToArray());
        }

        public int UpdateByHql(string hql, ArrayList values)
        {
            using var db = NewDb();
            return db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql, db), values.Cast<object>().ToArray());
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
                using var db = NewDb();
                db.Attach(obj);
                db.Remove(obj);
                int saved = db.SaveChanges();
                if (saved == 0)
                    logger.Warn($"PersistentDao.Delete: 0 rows affected. type={obj.GetType().Name}");
            }
            catch (Exception ex)
            {
                logger.Error($"PersistentDao.Delete: {ex.Message}", ex);
                if (!ignoreException) throw;
            }
        }

        public int Delete(Type clazz, ISerializable id)
        {
            return Delete(clazz.FullName, id);
        }

        public int Delete(string className, ISerializable id)
        {
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            object idValue = NormalizeId(id);
            var entity = db.Find(type, idValue);
            if (entity == null) return 0;

            db.Remove(entity);
            int saved = db.SaveChanges();
            return saved > 0 ? 1 : 0;
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
                using var db = NewDb();
                var type = ResolveType(className, db);
                if (type == null) return 0;

                var query = GetQueryable(type, db);
                var entities = query.AsEnumerable()
                    .Where(e => Equals(GetPropertyValue(e, name), value))
                    .ToList();

                db.RemoveRange(entities);
                db.SaveChanges();
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
                using var db = NewDb();
                var type = ResolveType(className, db);
                if (type == null) return 0;

                var query = GetQueryable(type, db);
                var entities = query.AsEnumerable()
                    .Where(e => attributes.All(kv => Equals(GetPropertyValue(e, kv.Key), kv.Value)))
                    .ToList();

                db.RemoveRange(entities);
                db.SaveChanges();
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            var query = GetQueryable(type, db);
            var entities = query.AsEnumerable()
                .Where(e => MatchByOperators(e, attributes, operators))
                .ToList();

            db.RemoveRange(entities);
            db.SaveChanges();
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
            using var db = NewDb();
            var type = ResolveType(className, db);
            if (type == null) return 0;

            var query = GetQueryable(type, db);
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

            db.RemoveRange(entities);
            db.SaveChanges();
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
                using var db = NewDb();
                var type = ResolveType(className, db);
                if (type == null) return 0;

                var query = GetQueryable(type, db);
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

                db.RemoveRange(entities);
                db.SaveChanges();
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
                using var db = NewDb();
                var type = ResolveType(className, db);
                if (type == null) return 0;

                var query = GetQueryable(type, db);
                var entities = query.ToList();

                db.RemoveRange(entities);
                db.SaveChanges();
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
            using var db = NewDb();
            foreach (var entity in collection)
            {
                db.Attach(entity);
                db.Remove(entity);
            }
            db.SaveChanges();
        }

        public int DeleteByHql(string hql)
        {
            using var db = NewDb();
            return db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql, db));
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
            using var db = NewDb();
            return db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql, db), value);
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
            using var db = NewDb();
            return db.Database.ExecuteSqlRaw(ConvertHqlToSql(hql, db), values.Cast<object>().ToArray());
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
            // per-operation DbContext 모델에서 호출자 측 detach 는 의미 없다 (이미 외부 객체).
            // no-op 로 안전하게 보존.
        }

        public int ExecuteUpdate(string sql)
        {
            using var db = NewDb();
            return db.Database.ExecuteSqlRaw(sql);
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

        /// <summary>
        /// 엔티티의 모든 DateTime/DateTime? 프로퍼티를 UTC 로 정규화.
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

        private string ConvertHqlToSql(string hql, AcsDbContext db)
        {
            string sql = hql;
            int paramIndex = 0;
            while (sql.Contains("?"))
            {
                int idx = sql.IndexOf("?");
                sql = sql.Substring(0, idx) + "{" + paramIndex + "}" + sql.Substring(idx + 1);
                paramIndex++;
            }

            foreach (var entityType in db.Model.GetEntityTypes())
            {
                string entityName = entityType.ClrType.Name;
                string fullName = entityType.ClrType.FullName;
                string tableName = entityType.GetTableName() ?? entityName;

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
