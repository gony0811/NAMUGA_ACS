using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Base
{
    [Serializable]
    public abstract class Entity
    {
        private static long serialVersionUID = 1L;
        public static string TRUE = "T";
        public static string FALSE = "F";
        public static string NOTAPPLICABLE = "NA";
        public static string NOTDESIGNATED = "NOTDESIGNATED";
        public static string ZERO = "0";
        public static string ORIGINATEDTYPE_HOST = "H";
        public static string ORIGINATEDTYPE_MACHINE = "M";
        public static string ORIGINATEDTYPE_USER = "U";
        public static string ORIGINATEDTYPE_NANOTRANS = "N";
        public static string ORIGINATEDTYPE_CIRCULAR = "C";
        public static string CREATOR_CIRCULAR = "CIRCULAR";
        public static string CREATOR_RECOVERY = "RECOVERY";
        public static string CREATOR_MES = "MES";
        public static string NANOTRANS = "NANOTRANS";
   
        public static int PRIORITY_WORST = 0;
        public virtual string Id { get; set; }
    }
}
