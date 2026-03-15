using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server.variable
{
    public class EnhancedPortVariable
    {
        private string name;
        private string state;
        private string subState;
        private string accessMode;
        private string inOutType;
        private string processingState;
        private string transferState;
        private string useRfReader;

        protected internal string Name { get { return name; } set { name = value; } }
        protected internal string State { get { return state; } set { state = value; } }
        protected internal string SubState { get { return subState; } set { subState = value; } }
        protected internal string AccessMode { get { return accessMode; } set { accessMode = value; } }
        protected internal string InOutType { get { return inOutType; } set { inOutType = value; } }
        protected internal string ProcessingState { get { return processingState; } set { processingState = value; } }
        protected internal string TransferState { get { return transferState; } set { transferState = value; } }
        protected internal string UseRfReader { get { return useRfReader; } set { useRfReader = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("enhancedPortVariable{");
            sb.Append("name=").Append(this.name);
            sb.Append(", state=").Append(this.state);
            sb.Append(", subState=").Append(this.subState);
            sb.Append(", accessMode=").Append(this.accessMode);
            sb.Append(", inOutType=").Append(this.inOutType);
            sb.Append(", processingState=").Append(this.processingState);
            sb.Append(", transferState=").Append(this.transferState);
            sb.Append(", useRfReader=").Append(this.useRfReader);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
