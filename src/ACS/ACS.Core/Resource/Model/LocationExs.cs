using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Resource.Model;

namespace ACS.Core.Resource.Model
{
    public class LocationExs : LocationEx
    {
        public static String LOCATION_TYPE_BUFFER = "BUFFER";
        public static String LOCATION_TYPE_CHARGE = "CHARGE";
        public static String LOCATION_TYPE_VIRTUAL_BUFF = "VBUFFER";
        public static String LOCATION_TYPE_EQP = "EQP";

        public static String DIRECTION_LEFT = "LEFT";
        public static String DIRECTION_LEFT_BACK = "LEFTBACK";
        public static String DIRECTION_RIGHT = "RIGHT";
        public static String DIRECTION_RIGHT_BACK = "RIGHTBACK";
    }
}
