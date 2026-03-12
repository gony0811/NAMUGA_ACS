using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Application.Model
{
    public class ExternalApplication : NamedEntity
    {
        public static string TYPE_MES = "MES";
        public static string TYPE_DSP = "DSP";
        private string factoryName;
        private string areaName;
        private string areaType = "SHOP";
        private string type = "MES";
        private string state = "active";
        private string communicationState;
        private DateTime? lastCommunicationTime;
        private DateTime? checkTime;
        private int communictionRetryCount;
        private string sendDestinationName;
        private string receiveDestinationName;

        public string FactoryName { get { return factoryName; } set { factoryName = value; } }
        public string AreaName { get { return areaName; } set { areaName = value; } }
        public string AreaType { get { return areaType; } set { areaType = value; } }
        public string Type { get { return type; } set { type = value; } }
        public string State { get { return state; } set { state = value; } }
        public string CommunicationState { get { return communicationState; } set { communicationState = value; } }
        public DateTime? LastCommunicationTime { get { return lastCommunicationTime; } set { lastCommunicationTime = value; } }
        public DateTime? CheckTime { get { return checkTime; } set { checkTime = value; } }
        public int CommunictionRetryCount { get { return communictionRetryCount; } set { communictionRetryCount = value; } }
        public string SendDestinationName { get { return sendDestinationName; } set { sendDestinationName = value; } }
        public string ReceiveDestinationName { get { return receiveDestinationName; } set { receiveDestinationName = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("externalApplication{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", factoryName=").Append(this.factoryName);
            sb.Append(", type=").Append(this.type);
            sb.Append(", areaType=").Append(this.areaType);
            sb.Append(", areaName=").Append(this.areaName);
            sb.Append(", state=").Append(this.state);
            sb.Append(", communicationState=").Append(this.communicationState);
            sb.Append(", communictionRetryCount=").Append(this.communictionRetryCount);
            sb.Append(", lastCommunicationTime=").Append(this.lastCommunicationTime);
            sb.Append(", sendDestinationName=").Append(this.sendDestinationName);
            sb.Append(", receiveDestinationName=").Append(this.receiveDestinationName);
            sb.Append(", creator=").Append(this.Creator);
            sb.Append(", createTime=").Append(this.CreateTime);
            sb.Append(", editor=").Append(this.Editor);
            sb.Append(", editTime=").Append(this.EditTime);
            sb.Append(", checkTime=").Append(this.checkTime);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
