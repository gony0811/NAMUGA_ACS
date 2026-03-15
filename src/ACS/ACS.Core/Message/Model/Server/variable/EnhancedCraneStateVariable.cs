using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server.variable
{
    public class EnhancedCraneStateVariable
    {
        private string stockerCraneId;
        private string craneState;

        public string StockerCraneId { get { return stockerCraneId; } set { stockerCraneId = value; } }
        public string CraneState { get { return craneState; } set { craneState = value; } }  

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("enhancedCraneStateVariable{");
            sb.Append("stockerCraneId=").Append(this.stockerCraneId);
            sb.Append(", craneState=").Append(this.craneState);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
