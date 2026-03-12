using ACS.Communication.Msb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Msb.Tibrv
{
    //public abstract class AbstractTibrvFtListener : AbstractTibrvListener, TibrvMsgCallback, MsbControllable
    public abstract class AbstractTibrvFtListener : AbstractTibrvListener, IMsbControllable
    {
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("tibrvFtListener{");
            sb.Append("destination=").Append(this.destination);
            sb.Append(", transport=").Append(this.transport);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", useDataField=").Append(this.useDataField);
            sb.Append(", dataFieldName=").Append(this.dataFieldName);
            sb.Append(", open=").Append(this.open);
            sb.Append(", dataFormat=").Append(this.DataFormat).Append("{").Append(GetDataFormatTostring()).Append("}");
            sb.Append(", msbConverter=").Append(this.msbConverter);
            sb.Append("}");

            return sb.ToString();
        }
    }

}
