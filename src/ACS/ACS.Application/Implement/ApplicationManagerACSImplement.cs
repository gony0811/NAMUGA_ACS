using ACS.Framework.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Criterion;
using ACS.Framework.Base;

namespace ACS.Application.Implement
{
    public class ApplicationManagerACSImplement : ApplicationManagerImplement, IApplicationManagerACS
    {

        public const string NIO_CONNECT_MODE_WIFI = "WIFI";
        public const string NIO_CONNECT_MODE_REPEATER = "REPEATER";

        public virtual string GetNioConnectMode(string applicationName)
        {
            if (applicationName.StartsWith("ES09", StringComparison.Ordinal) || applicationName.StartsWith("ES10", StringComparison.Ordinal))
            {
                return "REPEATER";
            }
            return "WIFI";
        }

        public virtual bool IsWifiMode(string applicationName)
        {
            //181213 WIFI RF ZIGBEE available
            if (applicationName.StartsWith("ES09", StringComparison.Ordinal) || applicationName.StartsWith("ES10", StringComparison.Ordinal))
            {
                //ES09_P, ES10_P RF
                return false;
            }
            return true;
        }

        public virtual System.Collections.IList GetApplicationNamesByStateAndRunHW(string type, string state, string runningHardware)
        {
            DetachedCriteria criteria = DetachedCriteria.For<ACS.Framework.Application.Model.Application>();
            criteria.SetProjection(Projections.Property("Name"));

            criteria.Add(Restrictions.Eq("Type", type));
            criteria.Add(Restrictions.Eq("State", state));
            criteria.Add(Restrictions.Eq("RunningHardware", runningHardware));
            criteria.AddOrder(Order.Asc("Name"));

            return this.PersistentDao.FindByCriteria(criteria);
        }

    }
}
