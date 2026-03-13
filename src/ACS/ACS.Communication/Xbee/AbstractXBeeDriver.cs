using ACS.Framework.Resource.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using ACS.Framework.Extension.Resource.Model;
using ACS.Framework.Logging;
namespace ACS.Communication.Xbee
{
    public class AbstractXBeeDriver
    {
        public Logger logger = Logger.GetLogger(typeof(AbstractXBeeDriver));
        //protected WorkflowManager workflowManager;
        private string id;
        protected bool open;
        private Dictionary<string, string> xBeeMessageNameConverter = new Dictionary<string, string>();
        private Dictionary<string, List<VehicleEx>> Vehicles = new Dictionary<string, List<VehicleEx>>();

        public Dictionary<string, string> getxBeeMessageNameConverter()
        {
            return this.xBeeMessageNameConverter;
        }

        public void setxBeeMessageNameConverter(Dictionary<string, string> xBeeMessageNameConverter)
        {
            this.xBeeMessageNameConverter = xBeeMessageNameConverter;
        }

        public void OnMessageReceived(string vehicleId, Object message)
        {
            //logger.fine(message);
            //string messageName = "";
            //this.workflowManager.execute(messageName, message);
        }

        public string getId()
        {
            return this.id;
        }

        public void setId(string id)
        {
            this.id = id;
        }

        public bool isOpen()
        {
            return this.open;
        }

        public static explicit operator AbstractXBeeDriver(string v) //lys20180720 Please check??
        {
            throw new NotImplementedException();
        }

        public void setOpen(bool open)
        {
            this.open = open;
        }

        public Dictionary<string, List<VehicleEx>> getVehicles()
        {
            return this.Vehicles;
        }

        public void setVehicles(Dictionary<string, List<VehicleEx>> vehicles)
        {
            this.Vehicles = vehicles;
        }

        //public WorkflowManager getWorkflowManager() //lys20180719 After Workflow create
        //{
        //    return this.workflowManager;
        //}

        //public void setWorkflowManager(WorkflowManager workflowManager)
        //{
        //    this.workflowManager = workflowManager;
        //}

        public int initialize()
        {
            return -1;
        }

        public void terminate() { }

        public bool Open()
        {
            return false;
        }

        public bool close()
        {
            return false;
        }

        public void OnVehicleConnected(string vehicleId) { }

        public void OnVehicleDisconnected(string vehicleId) { }

        public bool request()
        {
            return false;
        }

        public bool reply()
        {
            return false;
        }

        public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("abstractXBeeDriver{");
            sb.Append("id=").Append(this.id);
            sb.Append(", open=").Append(this.open);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
