using Spring.Data.NHibernate.Generic.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Runtime.Serialization;
using System.Reflection;
using ACS.Framework.Base.Interface;
using Spring.Data.NHibernate;
using Spring.Transaction.Support;
using NHibernate.Criterion;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using System.Configuration;
using System.Globalization;

namespace ACS.Database
{
    public class HibernateDaoImplement : HibernateDaoSupport, IPersistentDao //0728
    {

        // private static SimpleDateFormat deleteDateFormat = new SimpleDateFormat("yyyyMMddHHmmss");
        public static int ORA_25408 = 25408;
        private int maxResults;
        private int maxUpdateCounts;
        private int dataAccessRetryCount;
        private long dataAccessRetrySleep;
        private string databaseType;

        public HibernateDaoImplement()
        {
            //190329 Oracle 
            //Add 1Row
            databaseType = ConfigurationManager.AppSettings[ACS.Framework.Application.Settings.SYSTEM_DATABASE_TYPE];

            maxResults = 1000;
            maxUpdateCounts = 1000;
            dataAccessRetryCount = 3;
            dataAccessRetrySleep = 300L;

            var cfg = new NHibernate.Cfg.Configuration();
            cfg.DataBaseIntegration(x =>
            {
                x.ConnectionString = ConfigurationManager.AppSettings["HibernateConnectionString"];
                var dbType = ConfigurationManager.AppSettings[ACS.Framework.Application.Settings.SYSTEM_DATABASE_TYPE];
                if (string.Equals(dbType, "ORACLE", StringComparison.CurrentCultureIgnoreCase))
                {
                    x.Driver<OracleClientDriver>();
                    x.Dialect<Oracle10gDialect>();                    
                }
                else if (string.Equals(dbType, "SQLITE", StringComparison.CurrentCultureIgnoreCase))
                {
                    x.Driver<MicrosoftDataSqliteDriver>();
                    x.Dialect<NHibernate.Dialect.SQLiteDialect>();
                }
                else
                {
                    x.Driver<SqlClientDriver>();
                    x.Dialect<MsSql2008Dialect>();
                }
            });
            cfg.AddAssembly(Assembly.GetExecutingAssembly());
                        
            this.SessionFactory = cfg.BuildSessionFactory();
            SetTransaction(this.SessionFactory);
        }


        private void SetTransaction(ISessionFactory sessionFactory)
        {
            HibernateTransactionManager tx = new HibernateTransactionManager(sessionFactory);
            TransactionTemplate transactionTemplate = new TransactionTemplate(tx);
            transactionTemplate.TransactionIsolationLevel = System.Data.IsolationLevel.Unspecified;
            transactionTemplate.PropagationBehavior = Spring.Transaction.TransactionPropagation.Required;
        }

        public int GetMaxResults()
        {
            return maxResults;
        }

        public void SetMaxResults(int maxResults)
        {
            this.maxResults = maxResults;
        }

        public int GetMaxUpdateCounts()
        {
            return maxUpdateCounts;
        }

        public void SetMaxUpdateCounts(int maxUpdateCounts)
        {
            this.maxUpdateCounts = maxUpdateCounts;
        }

        public int GetDataAccessRetryCount()
        {
            return dataAccessRetryCount;
        }

        public void SetDataAccessRetryCount(int dataAccessRetryCount)
        {
            this.dataAccessRetryCount = dataAccessRetryCount;
        }

        public long GetDataAccessRetrySleep()
        {
            return dataAccessRetrySleep;
        }

        public void SetDataAccessRetrySleep(long dataAccessRetrySleep)
        {
            this.dataAccessRetrySleep = dataAccessRetrySleep;
        }

        public bool Use(Type clazz)
        {
            return Use(clazz.FullName);
        }

        public bool Use(string className)
        {          
            return SessionFactory.GetAllClassMetadata().ContainsKey(className);
        }

        public void Save(object entity)
        {
            int tryCount = 0;
            do
                try
                {
                    HibernateTemplate.Save(entity);
                    return;
                }
                //catch (UncategorizedSQLException e)
                //{
                //    logger.warn("failed to save it, please check it out", e);
                //    if (e.getSQLException().getErrorCode() == ORA_25408)
                //    {
                //        logger.info((new StringBuilder("ORA-25408: can not safely replay call is occured. query will be executed again after ")).append(dataAccessRetrySleep).append("ms later").toString());
                //        tryCount++;
                //        sleep(dataAccessRetrySleep);
                //    }
                //    else
                //    {
                //        throw new DataManipulateException(entity, e);
                //    }
                //}
                catch (Exception e)
                {
                    tryCount++;
                    //logger.warn("failed to save it, please check it out", e);
                    //throw new DataManipulateException(entity, e);
                }
            while (tryCount <= dataAccessRetryCount);
        }

        public bool Save(object entity, bool ignoreException)
        {
            bool result = false;
            try
            {
                Save(entity);
                result = true;
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(entity);
            }
            return result;
        }

        public void UpdateAll(ICollection entities)
        {
            foreach (var ent in entities)
            {
                HibernateTemplate.SaveOrUpdate(ent);
            }
        }

        public void Flush()
        {
            HibernateTemplate.Flush();
        }

        public void SaveOrUpdate(object entity)
        {
            int tryCount = 0;
            do
                try
                {
                    HibernateTemplate.SaveOrUpdate(entity);
                    return;
                }
                //catch (UncategorizedSQLException e)
                //{
                //    logger.warn("failed to saveOrUpdate it, please check it out", e);
                //    if (e.getSQLException().getErrorCode() == ORA_25408)
                //    {
                //        logger.info((new StringBuilder("ORA-25408: can not safely replay call is occured. query will be executed again after ")).append(dataAccessRetrySleep).append("ms later").toString());
                //        tryCount++;
                //        sleep(dataAccessRetrySleep);
                //    }
                //    else
                //    {
                //        throw new DataManipulateException(entity, e);
                //    }
                //}
                catch (Exception e)
                {
                    tryCount++;
                    //logger.warn("failed to saveOrUpdate it, please check it out", e);
                    //throw new DataManipulateException(entity, e);
                }
            while (tryCount <= dataAccessRetryCount);
        }

        public IList<T> Find<T>(string hql)
        {
            return HibernateTemplate.Find<T>(hql);
        }

        public object Find(Type clazz, ISerializable id)
        {
            object Id = id;
            if(id is StringBuilder)
            {
                Id = id.ToString(); 
            }
            else if(id is DateTime)
            {
                Id = Convert.ToDateTime(id);
            }
           
            object obj = Session.Get(clazz, Id);

            if (obj != null)
                return obj;
            else
                // throw new DataNotFoundException("id", id.toString(), clazz.getName());
                return obj; //suji
        }

        public object Find(string className, ISerializable id)
        {
            object Id = id;
            if (id is StringBuilder)
            {
                Id = id.ToString();
            }
            else if (id is DateTime)
            {
                Id = Convert.ToDateTime(id);
            }
            object obj = Session.Get(className, Id);
            //object object = HibernateTemplate.Get(className, id);

            if (obj != null)
                return obj;
            else
                //throw new DataNotFoundException("id", id.toString(), className);
                return obj; //suji
        }

        public object Find(Type clazz, ISerializable id, bool throwExceptionIfNotFound)
        {
            object Id = id;
            if (id is StringBuilder)
            {
                Id = id.ToString();
            }
            else if (id is DateTime)
            {
                Id = Convert.ToDateTime(id);
            }
            if (throwExceptionIfNotFound)
                return Find(clazz, id);
            else
                return Session.Get(clazz, Id);
        }

        public object Find(String className, ISerializable id, bool throwExceptionIfNotFound)
        {
            object Id = id;
            if (id is StringBuilder)
            {
                Id = id.ToString();
            }
            else if (id is DateTime)
            {
                Id = Convert.ToDateTime(id);
            }
            if (throwExceptionIfNotFound)
                return Find(className, id);
            else
                return Session.Get(className, Id);
        }

        public object Exist(Type clazz, ISerializable id)
        {
            return Exist(clazz.FullName, id);
        }

        public object Exist(string className, ISerializable id)
        {
            object Id = id;
            if (id is StringBuilder)
            {
                Id = id.ToString();
            }
            else if (id is DateTime)
            {
                Id = Convert.ToDateTime(id);
            }
            return Session.Get(className, Id);
        }

        public object ExistByName(Type clazz, object value)
        {
            ICriteria criteria = Session.CreateCriteria(clazz).Add(Restrictions.Eq("Name", value));
            HibernateTemplate.PrepareCriteria(criteria);
            IList result = criteria.List();

            if (result.Count != 0)
            {
                return result[0];
            }
            else
                return null; 
        }

        //edit suji
        public object ExistByName(string className, object value)
        {    
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).Add(Restrictions.Eq("Name", value));
            IList result = FindByCriteria(criteria);

            if (result.Count != 0) return result[0];
            else return null;
        }

        public object FindByName(Type clazz, object value)
        {
            object obj = FindByName(clazz.FullName, value);
            if (obj != null)
                return obj;
            else
                //  throw new DataNotFoundException("name", value.toString(), clazz.getName());
                return obj;
        }


        // edit suji
        public object FindByName(String className, object value)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).Add(Restrictions.Eq("Name", value));
            IList result = FindByCriteria(criteria);
            if (result.Count != 0)
                return result[0];
            else return null;
          
            //else
            //    throw new DataNotFoundException("name", value.toString(), className);       
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

        public IList FindByExample(object obejct)
        {
            return FindByExample(obejct, false);
        }

        //pass suji
        public IList FindByExample(object obejct, bool ignoreException)
        {
            ArrayList result = null;
            try
            {
                result = new ArrayList();
                //result = HibernateTemplate.findByExample(obejct);
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(obejct);
                //result = new ArrayList();
            }
            return result;
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

        //edit suji
        public IList FindByAttribute(string className, string name, object value, bool ignoreException)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).Add(Restrictions.Eq(name, value));
            IList result = null;
            try
            {
                result = FindByCriteria(criteria);              
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(criteria);
                result = new ArrayList();
            }
            return result;
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

        //pass suji
        public IList FindByAttributeOrderBy(string className, string name, object value, string order, bool ignoreException)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).Add(Restrictions.Eq(name, value)).AddOrder(Order.Asc(order));
            ArrayList result = null;
            try
            {
                //result = HibernateTemplate.findByCriteria(criteria);
                result = new ArrayList(); //suji
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(criteria);
                result = new ArrayList();
            }
            return result;
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
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).Add(Restrictions.Eq(name, value)).AddOrder(Order.Desc(order));
            IList result = null;
            try
            {
                result = FindByCriteria(criteria);               
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(criteria);
                result = new ArrayList();
            }
            return result;
        }

        public IList FindByAttributes(Type clazz, Dictionary<string, object> attributes)
        {
            return FindByAttributes(clazz.FullName, attributes);
        }

        public IList FindByAttributes(string className, Dictionary<string, object> attributes)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).Add(Restrictions.AllEq(attributes));
            return FindByCriteria(criteria);
        }

        public IList FindByAttributesOrderBy(Type clazz, Dictionary<string, object> attributes, string order)
        {
            return FindByAttributesOrderBy(clazz.FullName, attributes, order);
        }

        public IList FindByAttributesOrderBy(string className, Dictionary<string, object> attributes, string order)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).Add(Restrictions.AllEq(attributes)).AddOrder(Order.Asc(order));
            return FindByCriteria(criteria);
        }

        public IList FindByAttributesOrderByDesc(Type clazz, Dictionary<string, object> attributes, string order)
        {
            return FindByAttributesOrderByDesc(clazz.FullName, attributes, order);
        }

        public IList FindByAttributesOrderByDesc(string className, Dictionary<string, object> attributes, string order)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).Add(Restrictions.AllEq(attributes)).AddOrder(Order.Desc(order));
            return FindByCriteria(criteria);
        }

        public IList FindAll(Type clazz)
        {
            return FindAll(clazz.FullName);
        }

        public IList FindAll(string className)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className);
            return FindByCriteria(criteria);
        }

        public IList FindAllOrderBy(Type clazz, string order)
        {
            return FindAllOrderBy(clazz.FullName, order);
        }

        public IList FindAllOrderBy(string className, string order)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).AddOrder(Order.Asc(order));
            return FindByCriteria(criteria);
        }

        public IList FindAllOrderByDesc(Type clazz, string order)
        {
            return FindAllOrderByDesc(clazz.FullName, order);
        }

        public IList FindAllOrderByDesc(string className, string order)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).AddOrder(Order.Desc(order));
            return FindByCriteria(criteria);
        }

        public IList FindProperty(Type clazz, string property)
        {
            return FindProperty(clazz.FullName, property);
        }

        public IList FindProperty(string className, string property)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).SetProjection(Projections.Property(property));
            return FindByCriteria(criteria);
        }

        public IList FindPropertyOrderBy(Type clazz, string property, string order)
        {
            return FindPropertyOrderBy(clazz.FullName, property, order);
        }

        public IList FindPropertyOrderBy(string className, string property, string order)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).SetProjection(Projections.Property(property)).AddOrder(Order.Asc(order));
            IList result = FindByCriteria(criteria);
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("find{")).append(result.size()).append("}, ").append(criteria).toString());
            return result;
        }

        public DetachedCriteria CreateDetachedCriteria()
        {
            return DetachedCriteria.For<object>();
        }

        public ICriteria CreateCriteria()
        {
            return Session.CreateCriteria<object>();
        }

        public IList FindByCriteria(DetachedCriteria criteria)
        {
            return FindByCriteria(criteria, -1, -1);
        }

        public IList FindByCriteria(DetachedCriteria criteria, int firstResult, int maxResults)
        {
            return FindByCriteria(criteria, firstResult, maxResults, false);
        }

        public IList FindByCriteria(DetachedCriteria criteria, bool ignoreException)
        {
            return FindByCriteria(criteria, -1, -1, ignoreException);
        }

        public IList FindByCriteria(DetachedCriteria criteria, int firstResult, int maxResults, bool ignoreException)
        {
            criteria.SetFirstResult(firstResult).SetMaxResults(maxResults);
            ICriteria iCriteria = criteria.GetExecutableCriteria(Session);
            return iCriteria.List();

            //ArrayList result = null;
            //try
            //{
            //    result = HibernateTemplate.findByCriteria(criteria, firstResult, maxResults);
            //    //if (logger.isDebugEnabled())
            //    //    logger.debug((new StringBuilder("find{")).append(result.size()).append("}, ").append(criteria).toString());
            //}
            //catch (Exception e)
            //{
            //    //logger.warn("failed to manipulate it, please check it out", e);
            //    //if (!ignoreException)
            //    //    throw new DataManipulateException(criteria);
            //    result = new ArrayList();
            //}
            //return result;
        }

        public int Count(DetachedCriteria criteria)
        {
            return Count(criteria, false);
        }

        public int Count(DetachedCriteria criteria, bool ignoreException)
        {
            int count = 0;
            try
            {
                IList list = FindByCriteria(criteria.SetProjection(Projections.RowCount()));
                if (list.Count != 0)
                    count = (int)list[0];
                //if (logger.isDebugEnabled())
                //    logger.debug((new StringBuilder("count{")).append(count.intValue()).append("}, ").append(criteria).toString());
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(criteria);
            }
            return count;
        }

        public int Sum(DetachedCriteria criteria, string propertySum)
        {
            return Sum(criteria, propertySum, false);
        }

        public int Sum(DetachedCriteria criteria, string propertySum, bool ignoreException)
        {
            int count = 0;
            try
            {
                IList list = FindByCriteria(criteria.SetProjection(Projections.Sum(propertySum)));

                if (list.Count != 0)
                    count = (int)list[0];
                //if (logger.isDebugEnabled())
                //    logger.debug((new StringBuilder("count{")).append(count.intValue()).append("}, ").append(criteria).toString());
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(criteria);
            }
            return count;
        }

        public int CountDistinct(DetachedCriteria criteria, string propertyDistincted)
        {
            int countDistinct = (int)FindByCriteria(criteria.SetProjection(Projections.Distinct(Projections.CountDistinct(propertyDistincted))))[0];
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("count{")).append(countDistinct.intValue()).append("}, ").append(criteria).toString());
            return countDistinct;
        }

        public IList Distinct(DetachedCriteria criteria, string propertyDistincted)
        {
            IList values = FindByCriteria(criteria.SetProjection(Projections.Distinct(Projections.Property(propertyDistincted))));
            return values;
        }

        public int UpdateByHql(string hql)
        {
            int count = BulkUpdate(hql);
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("updated{")).append(count).append("}, HQL{").append(hql).append("}").toString());
            return count;
        }

        public int UpdateByHql(string hql, string value)
        {
            int count = BulkUpdate(hql, value);
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("updated{")).append(count).append("}, HQL{").append(hql).append("}, ").append(value).toString());
            return count;
        }

        public int UpdateByHql(string hql, ArrayList values)
        {
            int count = BulkUpdate(hql, values.ToArray());
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("updated{")).append(count).append("}, HQL{").append(hql).append("}, ").append(values).toString());
            return count;
        }

        public int UpdateByHql(string hql, string[] values)
        {
            int count = BulkUpdate(hql, values);
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("updated{")).append(count).append("}, HQL{").append(hql).append("}, ").append(values).toString());
            return count;
        }

        public void Update(object entity)
        {
            Update(entity, false);
        }

        public void Update(object entity, bool ignoreException)
        {
            try
            {
                HibernateTemplate.Update(entity);
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(entity);
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

        public int UpdateByName(Type clazz, string setName, object setValue, string name)
        {
            return UpdateByName(clazz.FullName, setName, setValue, name);
        }

        public int UpdateByName(string className, string setName, object setValue, string value)
        {
            return UpdateByAttribute(className, setName, setValue, "name", value);
        }

        public int UpdateByAttribute(Type clazz, string setName, object setValue, string conditionName, object conditionValue)
        {
            return UpdateByAttribute(clazz.FullName, setName, setValue, conditionName, conditionValue);
        }

        public int UpdateByAttribute(string className, string setName, object setValue, string conditionName, object conditionValue)
        {
            string hql = (new StringBuilder("UPDATE ")).Append(className).Append(" ").ToString();
            hql = (new StringBuilder(hql)).Append("SET ").Append(setName).Append(" = ? ").ToString();
            hql = (new StringBuilder(hql)).Append("WHERE ").Append(conditionName).Append(" = ?").ToString();
            //if (logger.isDebugEnabled())
            //    logger.debug(hql);
            object[] values = {
            setValue, conditionValue
             };
            int count = BulkUpdate(hql, values);
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("updated{")).append(count).append("}, HQL{").append(hql).append("}, ").append(setValue).append(", ").append(conditionValue).toString());
            return count;
        }

        public int UpdateByAttributes(Type clazz, string setName, object setValue, Dictionary<string, object> conditionAttributes)
        {
            return UpdateByAttributes(clazz.FullName, setName, setValue, conditionAttributes);
        }

        public int UpdateByAttributes(string className, string setName, object setValue, Dictionary<string, object> conditionAttributes)
        {
            ArrayList values = new ArrayList();
            values.Add(setValue);
            string hql = (new StringBuilder("UPDATE ")).Append(className).Append(" ").ToString();
            hql = (new StringBuilder(hql)).Append("SET ").Append(setName).Append(" = ? ").ToString();
            hql = (new StringBuilder(hql)).Append("WHERE ").ToString();
            object[] keys = conditionAttributes.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
            {
                string name = (string)keys[index];
                {
                    object obj = conditionAttributes[name];
                    if (obj is DateTime)
                    { values.Add(Convert.ToDateTime(obj)); }
                    else if (obj is string || obj is StringBuilder)
                    { values.Add(obj.ToString()); }
                    else if (obj is int)
                    { values.Add(Convert.ToInt32(obj)); }
                }  

                hql = index + 1 >= keys.Length ? (new StringBuilder(hql)).Append(name).Append(" = ?").ToString() : (new StringBuilder(hql)).Append(name).Append(" = ? AND ").ToString();
            }

            //if (logger.isDebugEnabled())
            //    logger.debug(hql);
            int count = BulkUpdate(hql, values.ToArray());
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("updated{")).append(count).append("}, HQL{").append(hql).append("}, ").append(conditionAttributes.values()).toString());
            return count;
        }

        public int UpdateListByAttributes(string className, string setName, object setValue, Dictionary<string, object> conditionAttributes)
        {
            ArrayList values = new ArrayList();
            values.Add(setValue);
            string hql = (new StringBuilder("UPDATE ")).Append(className).Append(" ").ToString();
            hql = (new StringBuilder(hql)).Append("SET ").Append(setName).Append(" = ? ").ToString();
            hql = (new StringBuilder(hql)).Append("WHERE ").ToString();
            object[] keys = conditionAttributes.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
            {
                string name = (string)keys[index];
                {
                    object obj = conditionAttributes[name];
                    if (obj is DateTime)
                    { values.Add(Convert.ToDateTime(obj)); }
                    else if (obj is string || obj is StringBuilder)
                    { values.Add(obj.ToString()); }
                    else if (obj is int)
                    { values.Add(Convert.ToInt32(obj)); }
                }                
                hql = index + 1 >= keys.Length ? (new StringBuilder(hql)).Append(name).Append(" = ?").ToString() : (new StringBuilder(hql)).Append(name).Append(" = ? OR ").ToString();
            }

            //if (logger.isDebugEnabled())
            //    logger.debug(hql);
            int count = BulkUpdate(hql, values.ToArray());
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("updated{")).append(count).append("}, HQL{").append(hql).append("}, ").append(conditionAttributes.values()).toString());
            return count;
        }

        public int UpdateByAttributes(Type clazz, string setName, object setValue, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            return UpdateByAttributes(clazz.FullName, setName, setValue, conditionAttributes, operators);
        }

        public int UpdateByAttributes(string className, string setName, object setValue, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            ArrayList values = new ArrayList();
            values.Add(setValue);
            string hql = (new StringBuilder("UPDATE ")).Append(className).Append(" ").ToString();
            hql = (new StringBuilder(hql)).Append("SET ").Append(setName).Append(" = ? ").ToString();
            hql = (new StringBuilder(hql)).Append("WHERE ").ToString();
            object[] keys = conditionAttributes.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
            {
                string name = (string)keys[index];
                {
                    object obj = conditionAttributes[name];
                    if (obj is DateTime)
                    { values.Add(Convert.ToDateTime(obj)); }
                    else if (obj is string || obj is StringBuilder)
                    { values.Add(obj.ToString()); }
                    else if (obj is int)
                    { values.Add(Convert.ToInt32(obj)); }
                }  

                hql = index + 1 >= keys.Length ? (new StringBuilder(hql)).Append(name).Append(" = ?").ToString() : (new StringBuilder(hql)).Append(name).Append(" = ? ").Append(operators[index]).Append(" ").ToString();
            }

            //if (logger.isDebugEnabled())
            //    logger.debug(hql);
            int count = BulkUpdate(hql, values.ToArray());
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("updated{")).append(count).append("}, HQL{").append(hql).append("}, ").append(conditionAttributes.values()).toString());
            return count;
        }

        public int UpdateByAttributes(Type clazz, Dictionary<string, object> setAttributes, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            return UpdateByAttributes(clazz.FullName, setAttributes, conditionAttributes, operators);
        }

        public int UpdateByAttributes(string className, Dictionary<string, object> setAttributes, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            ArrayList values = new ArrayList();
            string hql = (new StringBuilder("UPDATE ")).Append(className).Append(" ").ToString();
            hql = (new StringBuilder(hql)).Append("SET ").ToString();
            object[] setKeys = setAttributes.Keys.ToArray();
            for (int index = 0; index < setKeys.Length; index++)
            {
                string name = (string)setKeys[index];
                {
                    object obj = setAttributes[name];
                    if (obj is DateTime)
                    { values.Add(Convert.ToDateTime(obj)); }
                    else if (obj is string || obj is StringBuilder)
                    { values.Add(obj.ToString()); }
                    else if (obj is int)
                    { values.Add(Convert.ToInt32(obj)); }
                }                  
                hql = index + 1 >= setKeys.Length ? (new StringBuilder(hql)).Append(name).Append(" = ?").ToString() : (new StringBuilder(hql)).Append(name).Append(" = ?, ").ToString();
            }

            hql = (new StringBuilder(hql)).Append(" WHERE ").ToString();
            object[] keys = conditionAttributes.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
            {              
                string name = (string)keys[index];
                {
                    object obj = conditionAttributes[name];
                    if (obj is DateTime)
                    { values.Add(Convert.ToDateTime(obj)); }
                    else if (obj is string || obj is StringBuilder)
                    { values.Add(obj.ToString()); }
                    else if (obj is int)
                    { values.Add(Convert.ToInt32(obj)); }
                }  
                hql = index + 1 >= keys.Length ? (new StringBuilder(hql)).Append(name).Append(" = ?").ToString() : (new StringBuilder(hql)).Append(name).Append(" = ? ").Append(operators[index]).Append(" ").ToString();
            }

            //if (logger.isDebugEnabled())
            //    logger.debug(hql);
            int count = BulkUpdate(hql, values.ToArray());
            //if (logger.isDebugEnabled())
            //    logger.info((new StringBuilder("updated{")).append(count).append("}, HQL{").append(hql).append("}, ").append(setAttributes.values()).toString());
            return count;
        }

        public int UpdateByAttributes(Type clazz, Dictionary<string, object> setAttributes, Dictionary<string, object> conditionAttributes)
        {
            return UpdateByAttributes(clazz.FullName, setAttributes, conditionAttributes);
        }

        public int UpdateByAttributes(string className, Dictionary<string, object> setAttributes, Dictionary<string, object> conditionAttributes)
        {
            ArrayList values = new ArrayList();
            string hql = (new StringBuilder("UPDATE ")).Append(className).Append(" ").ToString();
            hql = (new StringBuilder(hql)).Append("SET ").ToString();
            object[] setKeys = setAttributes.Keys.ToArray(); ;
            for (int index = 0; index < setKeys.Length; index++)
            {
                string name = (string)setKeys[index];
                {
                    object obj = setAttributes[name];
                    if (obj is DateTime)
                    { values.Add(Convert.ToDateTime(obj)); }
                    else if (obj is string || obj is StringBuilder)
                    { values.Add(obj.ToString()); }
                    else if (obj is int)
                    { values.Add(Convert.ToInt32(obj)); }
                    else if (obj is float)
                    {
                        float fVal = (float)obj;                       
                        values.Add(fVal);
                    }
                }   
                hql = index + 1 >= setKeys.Length ? (new StringBuilder(hql)).Append(name).Append(" = ?").ToString() : (new StringBuilder(hql)).Append(name).Append(" = ?, ").ToString();
            }

            hql = (new StringBuilder(hql)).Append(" WHERE ").ToString();
            object[] keys = conditionAttributes.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
            {
                string name = (string)keys[index];
                {
                    object obj = conditionAttributes[name];
                    if (obj is DateTime)
                    { values.Add(Convert.ToDateTime(obj)); }
                    else if (obj is string || obj is StringBuilder)
                    { values.Add(obj.ToString()); }
                    else if (obj is int)
                    { values.Add(Convert.ToInt32(obj)); }
                }  
                hql = index + 1 >= keys.Length ? (new StringBuilder(hql)).Append(name).Append(" = ?").ToString() : (new StringBuilder(hql)).Append(name).Append(" = ? AND ").ToString();
            }

            //if (logger.isDebugEnabled())
            //    logger.debug(hql);
            int count = BulkUpdate(hql, values.ToArray());
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("updated{")).append(count).append("}, HQL{").append(hql).append("}, ").append(setAttributes.values()).toString());
            return count;
        }

        public int UpdateByListAttributes(string className, string setName, object setValue, ArrayList conditionList)
        {
            string hql = "";

            if (conditionList.Count > maxUpdateCounts)
            {
                int loopCount = (int)Math.Ceiling(Double.Parse(conditionList.Count.ToString()) / Double.Parse(maxUpdateCounts.ToString()));
                for (int index = 0; index < loopCount; index++)
                {
                    hql = "";
                    hql = (new StringBuilder("UPDATE ")).Append(className).Append(" ").ToString();
                    hql = (new StringBuilder(hql)).Append("SET ").Append(setName).Append(" = '").Append(setValue).Append("' ").ToString();
                    hql = (new StringBuilder(hql)).Append("WHERE NAME IN ('").ToString();
                    int startIndex = index != 0 ? index * maxUpdateCounts : 0;
                    int endIndex = index != 0 ? index + 1 >= loopCount ? conditionList.Count : (index + 1) * maxUpdateCounts : maxUpdateCounts;
                    for (int j = startIndex; j < endIndex; j++)
                    {
                        string conditionVal = (string)conditionList[j];
                        hql = j + 1 >= endIndex ? (new StringBuilder(hql)).Append(conditionVal).Append("')").ToString() : (new StringBuilder(hql)).Append(conditionVal).Append("', '").ToString();
                    }

                    UpdateByHql(hql);
                }

                return conditionList.Count;
            }
            hql = "";
            hql = (new StringBuilder("UPDATE ")).Append(className).Append(" ").ToString();
            hql = (new StringBuilder(hql)).Append("SET ").Append(setName).Append(" = '").Append(setValue).Append("' ").ToString();
            hql = (new StringBuilder(hql)).Append("WHERE NAME IN ('").ToString();
            for (int index = 0; index < conditionList.Count; index++)
            {
                string conditionVal = (string)conditionList[index];
                hql = index + 1 >= conditionList.Count ? (new StringBuilder(hql)).Append(conditionVal).Append("')").ToString() : (new StringBuilder(hql)).Append(conditionVal).Append("', '").ToString();
            }

            return UpdateByHql(hql);
        }

        public int UpdateByName(Type clazz, Dictionary<string, object> setAttributes, string value)
        {
            return UpdateByName(clazz.FullName, setAttributes, value);
        }

        public int UpdateByName(string className, Dictionary<string, object> setAttributes, string value)
        {
            return UpdateByAttributes(className, setAttributes, "Name", value);
        }

        public int Update(Type clazz, Dictionary<string, object> setAttributes, string id)
        {
            return Update(clazz.FullName, setAttributes, id);
        }

        public int Update(string className, Dictionary<string, object> setAttributes, string id)
        {
            return UpdateByAttributes(className, setAttributes, "Id", id);
        }

        public int UpdateByAttributes(Type clazz, Dictionary<string, object> setAttributes, string whereName, string whereValue)
        {
            return UpdateByAttributes(clazz.FullName, setAttributes, whereName, whereValue);
        }

        public int UpdateByAttributes(string className, Dictionary<string, object> setAttributes, string conditionName, string conditionValue)
        {
            ArrayList values = new ArrayList();
            int count = 0;
            if (setAttributes.Count > 0)
            {
                string hql = (new StringBuilder("UPDATE ")).Append(className).Append(" ").ToString();
                hql = (new StringBuilder(hql)).Append("SET ").ToString();
                object[] setKeys = setAttributes.Keys.ToArray();
                for (int index = 0; index < setKeys.Length; index++)
                {
                    string name = (string)setKeys[index];
                    {
                        object obj = setAttributes[name];
                        if (obj is DateTime)
                        { values.Add(Convert.ToDateTime(obj)); }
                        else if(obj is string ||obj is StringBuilder)
                        { values.Add(obj.ToString()); }
                        else if(obj is int)
                        { values.Add(Convert.ToInt32(obj)); }
                    }                   
                    hql = index + 1 >= setKeys.Length ? (new StringBuilder(hql)).Append(name).Append(" = ?").ToString() : (new StringBuilder(hql)).Append(name).Append(" = ?, ").ToString();
                }

                hql = (new StringBuilder(hql)).Append(" WHERE ").Append(conditionName).Append(" = ?").ToString();
                //if (logger.isDebugEnabled())
                //    logger.debug(hql);
                values.Add(conditionValue);
                count = BulkUpdate(hql, values.ToArray());
                //if (logger.isDebugEnabled())
                //    logger.debug((new StringBuilder("updated{")).append(count).append("}, HQL{").append(hql).append("}, ").append(setAttributes.values()).toString());
            }
            else { } //suji
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("can not update {")).append(className).append("}, set value is empty").toString());
            return count;
        }

        public int DeleteByHql(string hql)
        {
            return DeleteByHql(hql, false);
        }

        public int DeleteByHql(string hql, bool ignoreException)
        {
            int count = 0;
            try
            {
                count = BulkUpdate(hql);
                //if (logger.isDebugEnabled())
                //    logger.debug((new StringBuilder("deleted{")).append(count).append("}, HQL{").append(hql).append("}").toString());
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(hql);
            }
            return count;
        }

        public int DeleteByHql(string hql, string value)
        {
            return DeleteByHql(hql, value, false);
        }

        public int DeleteByHql(string hql, string value, bool ignoreException)
        {
            int count = 0;
            try
            {
                count = BulkUpdate(hql, value);
                //if (logger.isDebugEnabled())
                //    logger.debug((new StringBuilder("deleted{")).append(count).append("}, HQL{").append(hql).append("}, value{").append(value).append("}").toString());
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(hql);
            }
            return count;
        }

        public int DeleteByHql(string hql, ArrayList values)
        {
            return DeleteByHql(hql, values, false);
        }

        public int DeleteByHql(string hql, ArrayList values, bool ignoreException)
        {
            int count = 0;
            try
            {
                count = BulkUpdate(hql, values.ToArray());
                //if (logger.isDebugEnabled())
                //    logger.debug((new StringBuilder("deleted{")).append(count).append("}, HQL{").append(hql).append("}, ").append(values).toString());
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(hql);
            }
            return count;
        }

        public void Delete(object entity)
        {
            Delete(entity, false);
        }

        public void Delete(object entity, bool ignoreException)
        {
            int tryCount = 0;
            do
                try
                {
                    HibernateTemplate.Delete(entity);
                    return;
                }
                //catch (UncategorizedSQLException e)
                //{
                //    logger.warn("failed to delete it, please check it out", e);
                //    if (e.getSQLException().getErrorCode() == ORA_25408)
                //    {
                //        logger.info((new StringBuilder("ORA-25408: can not safely replay call is occured. query will be executed again after ")).append(dataAccessRetrySleep).append("ms later").toString());
                //        tryCount++;
                //        sleep(dataAccessRetrySleep);
                //    }
                //    else
                //    {
                //        throw new DataManipulateException(entity, e);
                //    }
                //}
                catch (Exception e)
                {
                    tryCount++;
                    //logger.warn("failed to delete it, please check it out", e);
                    //throw new DataManipulateException(entity);
                }
            while (tryCount <= dataAccessRetryCount);
            if (!ignoreException) return;//suji 
            //throw new DataManipulateException(entity);
            else
                return;
        }

        public int Delete(Type clazz, ISerializable id)
        {
            return Delete(clazz.FullName, id);
        }

        public int Delete(string className, ISerializable id)
        {
            object Id = id;
            if (id is StringBuilder)
            {
                Id = id.ToString();
            }
            else if (id is DateTime)
            {
                Id = Convert.ToDateTime(id);
            }
            string hql = (new StringBuilder("DELETE ")).Append(className).Append(" WHERE id = ?").ToString();
            int count = BulkUpdate(hql, Id);
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("deleted{")).append(count).append("}, HQL{").append(hql).append("}").toString());
            return count;
        }

        public int DeleteByTime(Type clazz, DateTime fromDate, DateTime toDate)
        {
            return DeleteByTime(clazz.FullName, fromDate, toDate);
        }

        public int DeleteByTime(string className, DateTime fromDate, DateTime toDate)
        {
            //190329 Oracle
            ////string from = fromDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
            ////string to = toDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
            //string from = fromDate.ToString("dd-MMM-yy HH.mm.ss.ffff tt");
            //string to = toDate.ToString("dd-MMM-yy HH.mm.ss.ffff tt");
            //string hql = (new StringBuilder("delete ")).Append(className).Append(" where time between '").Append(from).Append("' and '").Append(to).Append("'").ToString();
            //return DeleteByHql(hql);

            //KSB 대소문자 구분해서.오류.. (Oracle != oracle)
            //if (databaseType == ACS.Framework.Application.Settings.DB_ORACLE)
            if (string.Equals(databaseType, ACS.Framework.Application.Settings.DB_ORACLE, StringComparison.CurrentCultureIgnoreCase))
            {
                string from = fromDate.ToString("dd-MMM-yy hh.mm.ss.ffff tt", new CultureInfo("en-US"));
                string to = toDate.ToString("dd-MMM-yy hh.mm.ss.ffff tt", new CultureInfo("en-US")); 
                string hql = (new StringBuilder("delete ")).Append(className).Append(" where time between '").Append(from).Append("' and '").Append(to).Append("'").ToString();
                return DeleteByHql(hql);
            }
            else
            {
                string from = fromDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
                string to = toDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
                string hql = (new StringBuilder("delete ")).Append(className).Append(" where time between '").Append(from).Append("' and '").Append(to).Append("'").ToString();
                return DeleteByHql(hql);
            }
        }

        public int DeleteByTime(Type clazz, DateTime fromDate, DateTime toDate, int count)
        {
            return DeleteByTime(clazz.FullName, fromDate, toDate, count);
        }

        public int DeleteByTime(string className, DateTime fromDate, DateTime toDate, int count)
        {
            //string from = fromDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
            //string to = toDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
            string from = fromDate.ToString("dd-MMM-yy HH.mm.ss.ffff tt", new CultureInfo("en-US"));
            string to = toDate.ToString("dd-MMM-yy HH.mm.ss.ffff tt", new CultureInfo("en-US"));

            string hql = (new StringBuilder("delete ")).Append(className).Append(" where time between '").Append(from).Append("' and '").Append(to).Append("' and rownum <= ").Append(count).ToString();
            return DeleteByHql(hql);
        }

        public int DeleteByTime(Type clazz, DateTime toDate)
        {
            return DeleteByTime(clazz.FullName, toDate);
        }

        public int DeleteByTime(string className, DateTime toDate)
        {
            //190329 Oracle
            ////181117 
            ////ORACLE DB의 Time format이 MSSQL과 다름. 오라클 DB 적용 시 아래 주석코드 적용 필요.
            ////08-NOV-18 01.13.45.00000000 PM
            //string date = toDate.ToString("dd-MMM-yy HH.mm.ss.ffff tt");

            ////string date = toDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
            //string hql = (new StringBuilder("delete ")).Append(className).Append(" where time < '").Append(date).Append("'").ToString();
                       
            //return DeleteByHql(hql);

            //181117 
            //ORACLE DB의 Time format이 MSSQL과 다름. 오라클 DB 적용 시 아래 주석코드 적용 필요.
            //08-NOV-18 01.13.45.00000000 PM
            string date = toDate.ToString("dd-MMM-yy hh.mm.ss.ffff tt", new CultureInfo("en-US"));

            //string date = toDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
            string hql = (new StringBuilder("delete ")).Append(className).Append(" where time < '").Append(date).Append("'").ToString();

            return DeleteByHql(hql);
        }

        public int DeleteByTime(Type clazz, DateTime toDate, int count)
        {
            return DeleteByTime(clazz.FullName, toDate, count);
        }

        public int DeleteByTime(string className, DateTime toDate, int count)
        {
            return DeleteByTime(className, toDate, count, false);
        }

        public int DeleteByTime(Type clazz, DateTime toDate, int count, bool ignoreException)
        {
            return DeleteByTime(clazz.FullName, toDate, count, ignoreException);
        }

        public int DeleteByTime(string className, DateTime toDate, int count, bool ignoreException)
        {
            //190329 Oracle
            ////string date = toDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
            //string date = toDate.ToString("dd-MMM-yy HH.mm.ss.ffff tt");
            //string hql = (new StringBuilder("delete ")).Append(className).Append(" where time < '").Append(date).Append("' and rownum <= ").Append(count).ToString();
            //int countDeleted = 0;
            //try
            //{
            //    countDeleted = DeleteByHql(hql);
            //}
            //catch (Exception e)
            //{
            //    //logger.warn("failed to manipulate it, please check it out", e);
            //    //if (!ignoreException)
            //    //    throw new DataManipulateException(className);
            //}
            //return countDeleted;

            //string date = toDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
            //string hql = (new StringBuilder("delete ")).Append(className).Append(" where time < '").Append(date).Append("' and rownum <= ").Append(count).ToString();
            //int countDeleted = 0;
            //try
            //{
            //    countDeleted = DeleteByHql(hql);
            //}
            //catch (Exception e)
            //{
            //    //logger.warn("failed to manipulate it, please check it out", e);
            //    //if (!ignoreException)
            //    //    throw new DataManipulateException(className);
            //}
            //return countDeleted;
            try
            {
                //KSB 대소문자 구분해서.오류.. (Oracle != oracle)
                //if (databaseType == ACS.Framework.Application.Settings.DB_ORACLE)
                if (string.Equals(databaseType, ACS.Framework.Application.Settings.DB_ORACLE, StringComparison.CurrentCultureIgnoreCase))
                {
                    string date = toDate.ToString("dd-MMM-yy hh.mm.ss.ffff tt", new CultureInfo("en-US"));
                    string hql = (new StringBuilder("delete ")).Append(className).Append(" where time < '").Append(date).Append("' and rownum <= ").Append(count).ToString();
                    return DeleteByHql(hql);
                }
                else
                {
                    string date = toDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
                    string hql = (new StringBuilder("delete ")).Append(className).Append(" where time < '").Append(date).Append("' and rownum <= ").Append(count).ToString();
                    return DeleteByHql(hql);
                }
            }
            catch (Exception e)
            {
                //    //logger.warn("failed to manipulate it, please check it out", e);
                //    //if (!ignoreException)
                //    //    throw new DataManipulateException(className);
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
            string hql = (new StringBuilder("DELETE ")).Append(className).ToString();
            int count = 0;
            try
            {
                count = BulkUpdate(hql);
                //if (logger.isDebugEnabled())
                //    logger.debug((new StringBuilder("deleted{")).append(count).append("}, HQL{").append(hql).append("}").toString());
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(hql);
            }
            return count;
        }

        public void DeleteAll(ICollection entities)
        {
            int tryCount = 0;
            do
                try
                {
                    foreach (var entitie in entities)
                    {
                        HibernateTemplate.Delete(entitie);
                    }
                    return;
                }
                //catch (UncategorizedSQLException e)
                //{
                //    logger.warn("failed to deleteAll it, please check it out", e);
                //    if (e.getSQLException().getErrorCode() == ORA_25408)
                //    {
                //        logger.info((new StringBuilder("ORA-25408: can not safely replay call is occured. query will be executed again after ")).append(dataAccessRetrySleep).append("ms later").toString());
                //        tryCount++;
                //        sleep(dataAccessRetrySleep);
                //    }
                //    else
                //    {
                //        throw new DataManipulateException(entities, e);
                //    }
                //}
                catch (Exception e)
                {
                    tryCount++;
                    //logger.warn("failed to deleteAll it, please check it out", e);
                    //throw new DataManipulateException(entities, e);
                }
            while (tryCount <= dataAccessRetryCount);
        }

        public int DeleteByName(Type clazz, string value)
        {
            return DeleteByName(clazz.FullName, value);
        }

        public int DeleteByName(string className, string value)
        {
            string hql = (new StringBuilder("DELETE ")).Append(className).Append(" WHERE name = ?").ToString();
            int count = BulkUpdate(hql, value);
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("deleted{")).append(count).append("}, HQL{").append(hql).append("}").toString());
            return count;
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
            string hql = (new StringBuilder("DELETE ")).Append(className).Append(" WHERE ").Append(name).Append(" = ?").ToString();
            int count = 0;
            try
            {
                count = BulkUpdate(hql, value);
                //    if (logger.isDebugEnabled())
                //        logger.debug((new StringBuilder("deleted{")).append(count).append("}, HQL{").append(hql).append("}, ").append(value).toString());
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(hql);
            }
            return count;
        }

        public int DeleteByAttributes(Type clazz, Dictionary<string, object> conditionAttributes)
        {
            return DeleteByAttributes(clazz.FullName, conditionAttributes);
        }

        public int DeleteByAttributes(string className, Dictionary<string, object> conditionAttributes)
        {
            return DeleteByAttributes(className, conditionAttributes, false);
        }

        public int DeleteByAttributes(Type clazz, Dictionary<string, object> conditionAttributes, bool ignoreException)
        {
            return DeleteByAttributes(clazz.FullName, conditionAttributes, ignoreException);
        }

        public int DeleteByAttributes(string className, Dictionary<string, object> conditionAttributes, bool ignoreException)
        {
            ArrayList values = new ArrayList();
            string hql = (new StringBuilder("DELETE ")).Append(className).Append(" ").ToString();
            hql = (new StringBuilder(hql)).Append("WHERE ").ToString();
            object[] keys = conditionAttributes.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
            {
                string name = (string)keys[index];
                {
                    object obj = conditionAttributes[name];
                    if (obj is DateTime)
                    { values.Add(Convert.ToDateTime(obj)); }
                    else if (obj is string || obj is StringBuilder)
                    { values.Add(obj.ToString()); }
                    else if (obj is int)
                    { values.Add(Convert.ToInt32(obj)); }
                }  
                hql = index + 1 >= keys.Length ? (new StringBuilder(hql)).Append(name).Append(" = ?").ToString() : (new StringBuilder(hql)).Append(name).Append(" = ? AND ").ToString();
            }

            //if (logger.isDebugEnabled())
            //    logger.debug(hql);
            int count = 0;
            try
            {
                count = BulkUpdate(hql, values.ToArray());
                //if (logger.isDebugEnabled())
                //    logger.debug((new StringBuilder("deleted{")).append(count).append("}, HQL{").append(hql).append("}, ").append(conditionAttributes.values()).toString());
            }
            catch (Exception e)
            {
                //logger.warn("failed to manipulate it, please check it out", e);
                //if (!ignoreException)
                //    throw new DataManipulateException(hql);
            }
            return count;
        }

        public int DeleteByAttributes(Type clazz, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            return DeleteByAttributes(clazz.FullName, conditionAttributes, operators);
        }

        public int DeleteByAttributes(string className, Dictionary<string, object> conditionAttributes, string[] operators)
        {
            ArrayList values = new ArrayList();

            string hql = (new StringBuilder("DELETE ")).Append(className).Append(" ").ToString();
            hql = (new StringBuilder(hql)).Append("WHERE ").ToString();
            object[] keys = conditionAttributes.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
            {
                string name = (string)keys[index];
                {
                    object obj = conditionAttributes[name];
                    if (obj is DateTime)
                    { values.Add(Convert.ToDateTime(obj)); }
                    else if (obj is string || obj is StringBuilder)
                    { values.Add(obj.ToString()); }
                    else if (obj is int)
                    { values.Add(Convert.ToInt32(obj)); }
                }  
                hql = index + 1 >= keys.Length ? (new StringBuilder(hql)).Append(name).Append(" = ?").ToString() : (new StringBuilder(hql)).Append(name).Append(" = ? ").Append(operators[index]).Append(" ").ToString();
            }

            //if (logger.isDebugEnabled())
            //    logger.debug(hql);
            int count = BulkUpdate(hql, values.ToArray());
            //if (logger.isDebugEnabled())
            //    logger.debug((new StringBuilder("deleted{")).append(count).append("}, HQL{").append(hql).append("}, ").append(conditionAttributes.values()).toString());
            return count;
        }

        /**
         * @deprecated Method deleteByAttributes is deprecated
         */

        public int DeleteByAttributes(Type clazz, Type[] types, string[] names, object[] values, string[] operators)
        {
            return DeleteByAttributes(clazz.FullName, types, names, values, operators);
        }

        /**
         * @deprecated Method deleteByAttributes is deprecated
         */

        public int DeleteByAttributes(string className, Type[] types, string[] names, object[] values, string[] operators)
        {
            string where = " where";
            for (int index = 0; index < names.Length; index++)
            {
                where = (new StringBuilder(where)).Append(" ").Append(names[index]).Append(" = ?").ToString();
                if (index != names.Length - 1)
                    where = (new StringBuilder(where)).Append(" ").Append(operators[index]).ToString();
            }

            string hql = (new StringBuilder("delete ")).Append(className).Append(where).ToString();
            return -1;
        }

        public object FindByNameWithoutException(Type clazz, object value)
        {
            object obj = FindByNameWithoutException(clazz.FullName, value);
            return obj;
        }

        public object FindByNameWithoutException(string className, object value)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).Add(Restrictions.Eq("Name", value));
            IList result = FindByCriteria(criteria);
            if (result.Count != 0)
            {
                return result[0];
            }
            else
            {
                //logger.warn((new StringBuilder("value{")).append(value).append("}").append(" does not exist in repository, type{").append(className).append("}").toString());
                return null;
            }
        }

        public IList FindByLike(Type clazz, string name, object value)
        {
            return FindByLike(clazz.FullName, name, value);
        }

        public IList FindByLike(string className, string name, object value)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).Add(Restrictions.Like(name, value));
            return FindByCriteria(criteria);
        }

        public IList FindByLikeOrderByDesc(Type clazz, string name, object value, string orderPropertyName)
        {
            return FindByLikeOrderByDesc(clazz.FullName, name, value, orderPropertyName);
        }

        public IList FindByLikeOrderByDesc(string className, string name, object value, string orderPropertyName)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).Add(Restrictions.Like(name, value)).AddOrder(Order.Desc(orderPropertyName));
            return FindByCriteria(criteria);
        }

        public IList FindByLikeOrderByAsc(Type clazz, string name, object value, string orderPropertyName)
        {
            return FindByLikeOrderByAsc(clazz.FullName, name, value, orderPropertyName);
        }

        public IList FindByLikeOrderByAsc(string className, string name, object value, string orderPropertyName)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).Add(Restrictions.Like(name, value)).AddOrder(Order.Asc(orderPropertyName));
            return FindByCriteria(criteria);
        }

        public IList FindPropertyByAttributes(Type clazz, string property, string name, object value)
        {
            return FindPropertyByAttributes(clazz.FullName, property, name, value);
        }

        public IList FindPropertyByAttributes(string className, string property, string name, object value)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).SetProjection(Projections.Property(property)).Add(Restrictions.Eq(name, value));
            return FindByCriteria(criteria);
        }

        public IList FindPropertyByAttributesOrderBy(Type clazz, string property, string name, object value, string order)
        {
            return FindPropertyByAttributesOrderBy(clazz.FullName, property, name, value, order);
        }

        public IList FindPropertyByAttributesOrderBy(string className, string property, string name, object value, string order)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className).SetProjection(Projections.Property(property)).Add(Restrictions.Eq(name, value)).AddOrder(Order.Asc(order));
            return FindByCriteria(criteria);
        }

        public IList FindByAttributesOR(Type clazz, Dictionary<string, object> attributes)
        {
            return FindByAttributesOR(clazz.FullName, attributes);
        }

        public IList FindByAttributesOR(string className, Dictionary<string, object> attributes)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className);
            if (attributes.Count > 0)
            {
                Disjunction orJunc = Restrictions.Disjunction();
                orJunc.Add(Restrictions.AllEq(attributes));
                criteria.Add(orJunc);
            }
            return FindByCriteria(criteria);
        }

        public IList FindPropertyByAttributesOR(Type clazz, string property, Dictionary<string, object> attributes)
        {
            return FindPropertyByAttributesOR(clazz.FullName, property, attributes);
        }

        public IList FindPropertyByAttributesOR(string className, string property, Dictionary<string, object> attributes)
        {
            DetachedCriteria criteria = DetachedCriteria.ForEntityName(className);
            criteria.SetProjection(Projections.Property(property));
            if (attributes.Count > 0)
            {
                Disjunction orJunc = Restrictions.Disjunction();
                orJunc.Add(Restrictions.AllEq(attributes));
                criteria.Add(orJunc);
            }
            return FindByCriteria(criteria);
        }

        public int UpdateByAttributes(Type clazz, Dictionary<string, object> attributes, string conditionName, object conditionValue)
        {
            return UpdateByAttributes(clazz.FullName, attributes, conditionName, conditionValue);
        }

        public int UpdateByAttributes(string className, Dictionary<string, object> attributes, string conditionName, object conditionValue)
        {
            Dictionary<string, object> conditions = new Dictionary<string, object>();
            conditions.Add(conditionName, conditionValue.ToString());
            return UpdateByAttributes(className, attributes, conditions, new string[0]);
        }

        public IList<T> FindByBindingQuery<T>(string hql, ArrayList values)
        {
            if (values.Count == 0)
                return null; //suji
            else
                return HibernateTemplate.Find<T>(hql, values.ToArray());
        }

        public void Evict(object entity)
        {
            int tryCount = 0;
            do
                try
                {
                    HibernateTemplate.Evict(entity);
                    return;
                }
                //catch (UncategorizedSQLException e)
                //{
                //    logger.warn("failed to evict it, please check it out", e);
                //    if (e.getSQLException().getErrorCode() == ORA_25408)
                //    {
                //        logger.info((new StringBuilder("ORA-25408: can not safely replay call is occured. query will be executed again after ")).append(dataAccessRetrySleep).append("ms later").toString());
                //        tryCount++;
                //        sleep(dataAccessRetrySleep);
                //    }
                //    else
                //    {
                //        throw new DataManipulateException(entity, e);
                //    }
                //}
                catch (Exception e)
                {
                    tryCount++;
                    //logger.warn("failed to evict it, please check it out", e);
                    //throw new DataManipulateException(entity, e);
                }
            while (tryCount <= dataAccessRetryCount);
        }



        protected int BulkUpdate(string hql)
        {
            int count = -1;
            int tryCount = 0;
            do
                try
                {
                    count = HiberBulkUpdate(hql);
                    break;
                }
                //catch (UncategorizedSQLException e)
                //{
                //    logger.warn("failed to bulkUpdate it, please check it out", e);
                //    if (e.getSQLException().getErrorCode() == ORA_25408)
                //    {
                //        logger.info((new StringBuilder("ORA-25408: can not safely replay call is occured. query will be executed again after ")).append(dataAccessRetrySleep).append("ms later").toString());
                //        tryCount++;
                //        sleep(dataAccessRetrySleep);
                //    }
                //    else
                //    {
                //        throw new DataManipulateException(hql, e);
                //    }
                //}
                catch (Exception e)
                {
                    tryCount++;
                    //logger.warn("failed to bulkUpdate it, please check it out", e);
                    //throw new DataManipulateException(hql, e);
                }
            while (tryCount <= dataAccessRetryCount);
            return count;
        }

        protected int BulkUpdate(string hql, object value)
        {
            int count = -1;
            int tryCount = 0;
            do
                try
                {
                    count = HiberBulkUpdate(hql, value);
                    break;
                }
                //catch (UncategorizedSQLException e)
                //{
                //    logger.warn("failed to bulkUpdate it, please check it out", e);
                //    if (e.getSQLException().getErrorCode() == ORA_25408)
                //    {
                //        logger.info((new StringBuilder("ORA-25408: can not safely replay call is occured. query will be executed again after ")).append(dataAccessRetrySleep).append("ms later").toString());
                //        tryCount++;
                //        sleep(dataAccessRetrySleep);
                //    }
                //    else
                //    {
                //        throw new DataManipulateException(hql, e);
                //    }
                //}
                catch (Exception e)
                {
                    tryCount++;
                    //logger.warn("failed to bulkUpdate it, please check it out", e);
                    //throw new DataManipulateException(hql, e);
                }
            while (tryCount <= dataAccessRetryCount);
            return count;
        }

        protected int BulkUpdate(String hql, object[] values)
        {
            int count = -1;
            int tryCount = 0;
            do
                try
                {
                    count = HiberBulkUpdate(hql, values);
                    break;
                }
                //catch (UncategorizedSQLException e)
                //{
                //    logger.warn("failed to bulkUpdate it, please check it out", e);
                //    if (e.getSQLException().getErrorCode() == ORA_25408)
                //    {
                //        logger.info((new StringBuilder("ORA-25408: can not safely replay call is occured. query will be executed again after ")).append(dataAccessRetrySleep).append("ms later").toString());
                //        tryCount++;
                //        sleep(dataAccessRetrySleep);
                //    }
                //    else
                //    {
                //        throw new DataManipulateException(hql, e);
                //    }
                //}
                catch (Exception e)
                {
                    tryCount++;
                    //logger.warn("failed to bulkUpdate it, please check it out", e);
                    //throw new DataManipulateException(hql, e);
                }
            while (tryCount <= dataAccessRetryCount);
            return count;
        }

        #region suji made
        public int HiberBulkUpdate(String hql)
        {
            return HiberBulkUpdate(hql, (Object[])null);
        }

        public int HiberBulkUpdate(String hql, object value)
        {
            return HiberBulkUpdate(hql, new Object[] {
            value
        });
        }

        public int HiberBulkUpdate(String hql, object[] values) //필요하다면 다시 만들어야함 임시구현
        {
            int returncount = HibernateTemplate.Execute<Int32>((Session) =>
            {
                IQuery queryObject = Session.CreateQuery(hql);
                HibernateTemplate.PrepareQuery(queryObject);
                if (values != null)
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (values[i] == null) { queryObject.SetParameter(i, null, NHibernateUtil.Int32); }
                        else queryObject.SetParameter(i, values[i]);
                    }
                }
                return queryObject.ExecuteUpdate();
            });
            return returncount;
        }
        #endregion

        /*
        protected void sleep(long millis)
        {
            Thread.sleep(millis);
            //try
            //{
            //    Thread.sleep(millis);
            //}
            //catch (InterruptedException e)
            //{
            //    logger.info("", e);
            //}
        }*/


        public int ExecuteUpdate(String sql)
        {
            int count = -1;
            int tryCount = 0;
            do
                try
                {
                    HibernateTemplate.Execute<Int32>((Session) =>
                    {
                        IQuery sqlQuery = Session.CreateSQLQuery(sql);
                        return sqlQuery.ExecuteUpdate();
                    });
                    return 0;
                }
                catch
                {
                    tryCount++;
                    //예외처리 필요
                }

            while (tryCount <= dataAccessRetryCount);
            return -1;
        }
   
    }
}
