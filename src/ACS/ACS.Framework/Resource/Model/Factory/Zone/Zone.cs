using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;


namespace ACS.Framework.Resource.Model.Factory.Zone
{
    public class Zone : NamedEntity
    {
        private String machineName = "";
        private String type = "SHELF";
        private int maxCapacity = -1;
        private int zoneCapacity;
        private int priority = 7;
        private String autoInBanned = "F";
        private String autoOutBanned = "F";
        private String storedType = "STORED";
        public static string TYPE_SHELF = "SHELF";
        public static string TYPE_PORT = "PORT";
        public static string TYPE_OTHER = "OTHER";
        public static string TYPE_SHELF_NUM = "1";
        public static string TYPE_PORT_NUM = "2";
        public static string TYPE_OTHER_NUM = "3";
        public static string STOREDTYPE_STORED = "STORED";
        public static string STOREDTYPE_SHARED = "SHARED";
        public static string STOREDTYPE_HANDOFF = "HANDOFF";
        public static string STOREDTYPE_INTERIM = "INTERIM";
        public static string STOREDTYPE_IDREAD = "IDREAD";

        public int MaxCapacity
        {
            get { return maxCapacity; }
            set { maxCapacity = value; }
        }

        public int ZoneCapacity
        {
            get { return zoneCapacity; }
            set { zoneCapacity = value; }
        }

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        public int Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        public string StoredType
        {
            get { return storedType; }
            set { storedType = value; }
        }

        public string AutoInBanned
        {
            get { return autoInBanned; }
            set { autoInBanned = value; }
        }

        public string AutoOutBanned
        {
            get { return autoOutBanned; }
            set { autoOutBanned = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("zone{");
            sb.Append("name=").Append(this.Name);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", type=").Append(this.type);
            sb.Append(", storedType=").Append(this.storedType);
            sb.Append(", priority=").Append(this.priority);

            sb.Append(", maxCapacity=").Append(this.maxCapacity);
            sb.Append(", zoneCapacity=").Append(this.zoneCapacity);
            sb.Append(", description=").Append(this.Description);
            sb.Append("}");
            return sb.ToString();
        }


    }
}
