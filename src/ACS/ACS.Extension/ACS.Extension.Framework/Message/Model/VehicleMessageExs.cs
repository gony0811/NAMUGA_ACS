using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Message.Model;

namespace ACS.Extension.Framework.Message.Model
{
    public class VehicleMessageExs : VehicleMessageEx
    {
        public static String C_CODE_TYPE_KEY = "CCODETYPE";
        public static String C_CODE_TYPE_LEFTLOAD_BACK = "11";
        public static String C_CODE_TYPE_LEFTUNLOAD_BACK = "12";
        public static String C_CODE_TYPE_RIGHTLOAD_BACK = "13";
        public static String C_CODE_TYPE_RIGHTUNLOAD_BACK = "14";
        public static String C_CODE_TYPE_UNDEFINED = "05";
        public static String C_CODE_TYPE_CHARGE = "06";
        public static String C_CODE_TYPE_WAITPOINT = "05";
        public static String C_CODE_TYPE_BOTH = "99";
        public static String C_CODE_TYPE_ROBOT_LEFTUNLOAD = "2";
        public static String C_CODE_TYPE_ROBOT_RIGHUNLOAD = "4";
        public static String C_CODE_TYPE_ROBOT_UNDEFINED = "5";
        public static String COMMAND_CODE_B = "B";


        public static String TYPE_ORDER = "ORDER";
        public static String ORDER_NODE_TURN_LEFT_RUN = "ORDER_LF"; // 07
        public static String ORDER_NODE_TURN_RIGHT_RUN = "ORDER_RF"; // 06
        public static String ORDER_NODE_TURN_LEFT_BACK = "ORDER_LB"; // 09
        public static String ORDER_NODE_TURN_RIGHT_BACK = "ORDER_RB";// 08
        public static String ORDER_NODE_CHANGE_LINE_LEFT = "ORDER_CL"; // 10
        public static String ORDER_NODE_CHANGE_LINE_RIGHT = "ORDER_CR";// 11

        //20230307 ORDER_VISION
        public static String ORDER_NODE_VISION = "ORDER_VISION";// 50
        //


        public static String VEHICLETRANSACTIONHISTORY_TYPE_C_CODE = "C_CODE";
        public static String VEHICLETRANSACTIONHISTORY_TYPE_H_CODE = "H_CODE";
    }
}
