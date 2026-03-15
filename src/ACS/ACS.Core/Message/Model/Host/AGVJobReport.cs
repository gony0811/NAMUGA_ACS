using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Host
{
    public class AGVJobReport : HostMessageEx
    {
        public static string JOBCANCEL_ERRCODE_HOSTCANCEL = "0";
        public static string JOBCANCEL_ERRCODE_CARRIERLOADED = "-1";
        public static string JOBCANCEL_ERRCODE_CARRIERREMOVED = "1";
        public static string JOBCANCEL_ERRCODE_DESTOCCUPIED = "-2";
        public static string JOBCANCEL_ERRCODE_SOURCEEMPTY = "4";
        public static string JOBCANCEL_ERRCODE_SOURCEPIOCONERROR = "6";
        public static string JOBCANCEL_ERRCODE_SOURCEPIOREQERROR = "7";
        public static string JOBCANCEL_ERRCODE_SOURCEPIORUNERROR = "8";
        public static string JOBCANCEL_ERRCODE_SOURCEPIOPORTCHECKERROR = "5";
        public static string JOBCANCEL_ERRCODE_DESTPIOCONERROR = "41";
        public static string JOBCANCEL_ERRCODE_DESTPIOREQERROR = "42";
        public static string JOBCANCEL_ERRCODE_DESTPIORUNERROR = "43";
        public static string JOBCANCEL_ERRCODE_DESTPIOPORTCHECKERROR = "40";     
        public static string JOBCANCEL_ERRCODE_VEHICLEOCCUPIED = "-11";
        public static string JOBCANCEL_ERRCODE_VEHICLEEMPTY = "10";
        public static string JOBCANCEL_ERRCODE_UNDEFINED = "50";
        public static string JOBCANCEL_ERRCODE_UICANCEL = "99";

    }

}
