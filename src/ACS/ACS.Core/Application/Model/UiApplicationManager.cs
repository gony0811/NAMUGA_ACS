using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Application.Model
{
    public class UiApplicationManager
    {
        public virtual string ID { get; set; }
        public virtual string TYPE { get; set; }
        public virtual string COMMAND { get; set; }
        public virtual string REPLY { get; set; }
        public virtual string STATE { get; set; }
        public virtual string USERID { get; set; }
        public virtual string IPADDRESS { get; set; }
        public virtual DateTime? EVENTTIME { get; set; }
        public virtual DateTime? REQUESTTIME { get; set; }
        public UiApplicationManager()
        {

        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("uiapplicationmanager{");  //여기는 무엇? 
            sb.Append("ID=").Append(this.ID);
            sb.Append(", TYPE=").Append(this.TYPE);
            sb.Append(", COMMAND=").Append(this.COMMAND);
            sb.Append(", REPLY=").Append(this.REPLY);
            sb.Append(", STATE=").Append(this.STATE);
            sb.Append(", USERID=").Append(this.USERID);
            sb.Append(", IPADDRESS=").Append(this.IPADDRESS);
            sb.Append(", EVENTTIME=").Append(this.EVENTTIME);
            sb.Append(", REQUESTTIME=").Append(this.REQUESTTIME);          
            sb.Append("}");
            return sb.ToString();
        }
    }
}
