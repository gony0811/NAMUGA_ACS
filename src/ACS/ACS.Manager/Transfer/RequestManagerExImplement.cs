using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using ACS.Framework.Base;
using ACS.Framework.Base.Interface;
using ACS.Framework.Transfer.Model;
using ACS.Framework.Transfer;

namespace ACS.Manager.Transfer
{
    public class RequestManagerExImplement : AbstractManager, IRequestManagerEx
    {
        public void CreateTransportCommandRequest(TransportCommandRequestEx transportCommandRequestEx)
        {
            this.PersistentDao.Save(transportCommandRequestEx);
        }

        public TransportCommandRequestEx CreateTransportCommandRequest(String messageName, String jobId, String vehicleId, String dest)
        {
            TransportCommandRequestEx transportCommandRequestEx = new TransportCommandRequestEx();
            transportCommandRequestEx.MessageName = messageName;
            transportCommandRequestEx.JobId = jobId;
            transportCommandRequestEx.VehicleId = vehicleId;
            transportCommandRequestEx.Dest = dest;
            transportCommandRequestEx.CreateTime = DateTime.Now;

            CreateTransportCommandRequest(transportCommandRequestEx);
            return transportCommandRequestEx;
        }

        public TransportCommandRequestEx CreateTransportCommandRequest(String messageName, String jobId, String vehicleId)
        {
            TransportCommandRequestEx transportCommandRequestEx = new TransportCommandRequestEx();
            transportCommandRequestEx.MessageName = messageName;
            transportCommandRequestEx.JobId = jobId;
            transportCommandRequestEx.VehicleId = vehicleId;
            transportCommandRequestEx.CreateTime = DateTime.Now;

            CreateTransportCommandRequest(transportCommandRequestEx);
            return transportCommandRequestEx;
        }

        public TransportCommandRequestEx CreateTransportCommandRequest(String jobId, String vehicleId)
        {
            TransportCommandRequestEx transportCommandRequestEx = new TransportCommandRequestEx();
            transportCommandRequestEx.JobId = jobId;
            transportCommandRequestEx.VehicleId = vehicleId;
            transportCommandRequestEx.CreateTime = DateTime.Now;

            CreateTransportCommandRequest(transportCommandRequestEx);
            return transportCommandRequestEx;
        }

        public void DeleteTransportCommandRequest(TransportCommandRequestEx transportCommandRequestEx)
        {
            this.PersistentDao.Delete(transportCommandRequestEx);
        }

        public int DeleteTransportCommandRequest(String jobId, String vehicleId, String dest)
        {
            Dictionary<string, object> conditionAttributes = new Dictionary<string, object>();

            conditionAttributes.Add("JobId", jobId);
            conditionAttributes.Add("VehicleId", vehicleId);
            conditionAttributes.Add("Dest", dest);

            return this.PersistentDao.DeleteByAttributes(typeof(TransportCommandRequestEx), conditionAttributes);
        }

        public int DeleteTransportCommandRequest(String jobId, String vehicleId)
        {
            Dictionary<string, object> conditionAttributes = new Dictionary<string, object>();

            conditionAttributes.Add("VehicleId", vehicleId);

            return this.PersistentDao.DeleteByAttributes(typeof(TransportCommandRequestEx), conditionAttributes);
        }

        public int DeleteTransportCommandRequest(String vehicleId)
        {
            return this.PersistentDao.DeleteByAttribute(typeof(TransportCommandRequestEx), "VehicleId", vehicleId);
        }
  
        public int DeleteTransportCommandRequests()
        {
            return this.PersistentDao.DeleteAll(typeof(TransportCommandRequestEx));
        }
  
        public TransportCommandRequestEx GetTransportCommandRequest(String jobId, String vehicleId, String dest)
        {
            var attributes = new Dictionary<string, object>
            {
                { "JobId", jobId },
                { "VehicleId", vehicleId },
                { "Dest", dest }
            };
            IList transportCommandRequests = this.PersistentDao.FindByAttributes(typeof(TransportCommandRequestEx), attributes);
            if (transportCommandRequests.Count > 0)
            {
                return (TransportCommandRequestEx)transportCommandRequests[0];
            }
            return null;
         }
  
        public TransportCommandRequestEx GetTransportCommandRequest(String jobId, String vehicleId)
        {
            var attributes = new Dictionary<string, object>
            {
                { "JobId", jobId },
                { "VehicleId", vehicleId }
            };
            IList transportCommandRequests = this.PersistentDao.FindByAttributes(typeof(TransportCommandRequestEx), attributes);
            if (transportCommandRequests.Count > 0)
            {
                return (TransportCommandRequestEx)transportCommandRequests[0];
            }
            return null;
        }
  
        public TransportCommandRequestEx GetTransportCommandRequest(String vehicleId)
        {
            var attributes = new Dictionary<string, object> { { "VehicleId", vehicleId } };
            IList transportCommandRequests = this.PersistentDao.FindByAttributes(typeof(TransportCommandRequestEx), attributes);
            if (transportCommandRequests.Count > 0)
            {
                return (TransportCommandRequestEx)transportCommandRequests[0];
            }
            return null;
        }

        public IList GetTransportCommandRequests()
        {
            return this.PersistentDao.FindAll(typeof(TransportCommandRequestEx));
        }
    }
}
