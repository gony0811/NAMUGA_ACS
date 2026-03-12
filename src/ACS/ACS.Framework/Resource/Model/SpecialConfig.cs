using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model
{
    public class SpecialConfig : Entity
    {
        public static char VALUE_DELIMITER = ',';
	
	    public static String SPECIAL_CHARGE_BAY = "SPECIAL_CHARGE_BAY";
	    public static String SPECIAL_INTERSECTION_BAY = "INTERSECTION_BAY";
	    public static String SPECIAL_IDLE_OVER_TIME = "IDLEOVERTIME";
	    public static String SPECIAL_ODER_BAY = "ORDERBAY";
	    public static String SPECIAL_STOP_BAY = "STOPBAY";
	    public static String SPECIAL_LONGPREASSIGNED_BAY = "LONGPREASSIGN";
	    public static String SPECIAL_DIVIDEPATH = "DIVIDEPATH";
	    public static String SPECIAL_SPEEDCONTROL = "SPEEDCONTROL";
        public static String SPECIAL_MISMATCHANDFLY = "MISMATCHANDFLY";
        public static String SPECIAL_CROSSWAITHISTORY = "CROSSWAITHISTORY";

        public virtual String Name { get; set; }
        public virtual String Values { get; set; }       

        public override string ToString()
        {
            return "SpecialConfig [c_name=" + Name + ", c_values=" + Values
                    + ", id=" + Id + "]";
        }

        public virtual bool ContainsValue(string value)
        {
            bool contains = false;

            if (!string.IsNullOrEmpty(value))
            {
                if (Values.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (Values.Contains(VALUE_DELIMITER))
                {
                    String[] valueArr = Values.Split(VALUE_DELIMITER);

                    if (valueArr != null & valueArr.Length > 0)
                    {
                        foreach (string tempValue in valueArr)
                        {
                            if (value.Equals(tempValue, StringComparison.OrdinalIgnoreCase))
                            {
                                contains = true;
                                break;
                            }

                        }
                    }
                }
                else
                {
                    if (value.Equals(Values, StringComparison.OrdinalIgnoreCase))
                    {
                        contains = true;
                    }
                }
            }
            return contains;
        }
    }
}
