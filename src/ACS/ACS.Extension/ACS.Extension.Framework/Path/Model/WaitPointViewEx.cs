using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Extension.Framework.Path.Model
{
    public class WaitPointViewEx 
    {
        public static string TYPE_SWAIT_P = "S_WAIT_P";
        // only one AGV stop
        public static string TYPE_AWAIT_P = "A_WAIT_P";

        public static string TYPE_ABNORMAL = "B_WAIT_P"; //give tray to abnormal before go wait point
   
        public virtual string Id { get; set; }
        public virtual string Type { get; set; }
        public virtual string ZoneId { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("WaitPointView{");
            sb.Append("type=").Append(this.Type);
            sb.Append(", zoneId=").Append(this.ZoneId);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
