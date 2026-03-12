using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using Spring.Data.NHibernate;
using Spring.Transaction.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base.Interface;

namespace ACS.Database
{
    public class HibernateSet
    { 

        public HibernateSet()
        {
            
        }
        public IPersistentDao Init()
        {
            var cfg = new Configuration();
            cfg.Configure();
            HibernateDaoImplement persistentDao = new HibernateDaoImplement();

            persistentDao.SessionFactory = cfg.BuildSessionFactory();
            SetTransaction(persistentDao.SessionFactory);

            return persistentDao;
        }

        private void SetTransaction(ISessionFactory sessionFactory)
        {
            HibernateTransactionManager tx = new HibernateTransactionManager(sessionFactory);
            TransactionTemplate transactionTemplate = new TransactionTemplate(tx);
            transactionTemplate.TransactionIsolationLevel = System.Data.IsolationLevel.Unspecified;
            transactionTemplate.PropagationBehavior = Spring.Transaction.TransactionPropagation.Required;
        }
    }
}
