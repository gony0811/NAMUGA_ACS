using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Path.Model
{
    public class NodeEx
    {
        public static string TYPE_COMMON = "COMMON";
        public static string TYPE_CROSS_S = "CROSS_S";
        public static string TYPE_CROSS_E = "CROSS_E";
        public static string TYPE_CHARGE = "CHARGE";
        public static string TYPE_WAIT_P = "WAIT_P";
        public static string TYPE_STOCK_STATION = "STOCK";
        public static string TYPE_MONITOR = "MONITOR";
       
        public virtual string Id {get;set;}
        public virtual string Type{get;set;}
        public virtual int Xpos{get;set;}
        public virtual int Ypos{get;set;}
        public virtual int Zpos { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("node{");
            sb.Append("nodeId=").Append(this.Id);
            sb.Append(", type=").Append(this.Type);
            sb.Append(", xPos=").Append(this.Xpos);
            sb.Append(", yPos=").Append(this.Ypos);
            sb.Append(", zPos=").Append(this.Zpos);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
