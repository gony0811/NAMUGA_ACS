using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Application.Model
{
    public class Application : NamedEntity
    {
        public static string NAME_ALL = "all";
        public static string TYPE_ALL = "all";
        public static string TYPE_TRANS = "trans";
        public static string TYPE_EI = "ei";
        public static string TYPE_CONTROL = "control";
        public static string TYPE_DAEMON = "daemon";
        public static string TYPE_EMULATOR = "emulator";
        public static string TYPE_REPORT = "report";
        public static string TYPE_QUERY = "query";
        public static string TYPE_UI = "ui";
        public static string TYPE_HOST = "host";
        public static string STATE_ACTIVE = "active";
        public static string STATE_STANBY = "stanby";
        public static string STATE_INACTIVE = "inactive";
        public static string STATE_HANG = "hang";
        public static string CREATOR_ADMIN = "admin";
        public static string EDITOR_ADMIN = "admin";
        public static string VERSION = "4.0";
        public static string HARDWARE_TYPE_PRIMARY = "PRIMARY";
        public static string HARDWARE_TYPE_SECONDARY = "SECONDARY";

        public virtual string Type { get; set; }
        public virtual string State { get; set; }
        public virtual DateTime? StartTime { get; set; }
        public virtual DateTime? CheckTime { get; set; }
        public virtual string InitialHardware { get; set; }
        public virtual string RunningHardware { get; set; }
        public virtual string RunningHardwareAddress { get; set; }
        public virtual string Msb { get; set; }
        public virtual string Memory { get; set; }
        public virtual string Jmx { get; set; }
        public virtual string DestinationName { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("application{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", type=").Append(this.Type);
            sb.Append(", creator=").Append(this.Creator);
            sb.Append(", createTime=").Append(this.CreateTime);
            sb.Append(", editor=").Append(this.Editor);
            sb.Append(", editTime=").Append(this.EditTime);
            sb.Append(", startTime=").Append(this.StartTime);
            sb.Append(", checkTime=").Append(this.CheckTime);
            sb.Append(", state=").Append(this.State);
            sb.Append(", initialHardware=").Append(this.InitialHardware);
            sb.Append(", runningHardware=").Append(this.RunningHardware);
            sb.Append(", runningHardwareAddress=").Append(this.RunningHardwareAddress);
            sb.Append(", msb=").Append(this.Msb);
            sb.Append(", memory=").Append(this.Memory);
            sb.Append(", jmx=").Append(this.Jmx);
            sb.Append(", destinationName=").Append(this.DestinationName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
