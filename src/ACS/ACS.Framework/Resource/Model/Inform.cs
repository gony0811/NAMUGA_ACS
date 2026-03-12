using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model
{
    public class Inform : Entity
    {
        public static string INFORM_TYPE_EMERGENCY = "EMERGENCY";
        public static string INFORM_TYPE_NOTICE = "NOTICE";
        public static string INFORM_TYPE_IMPORTANT = "IMPORTANT";

        public virtual DateTime Time { get; set; }
        public virtual string Type { get; set; }
        public virtual string Message { get; set; }
        public virtual string Source { get; set; }
        public virtual string Description { get; set; }
    }
}
