using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Application
{
    public class Settings
    {
        public static string SYSTEM_DATABASE_TYPE = "sdc.acs.database.type";
        public static string SYSTEM_PROPERTY_SECTION_NAME = "application_properties";
        public static string SYSTEM_PROPERTY_PROCESS_ID = "process.id";
        public static string SYSTEM_STARTUP_PATH = "sdc.acs.startup.path";
        public static string SYSTEM_PROPERTY_KEY_CONFIG = "sdc.acs.process.config.key";
        public static string SYSTEM_PROPERTY_KEY_SITE_VALUE = "sdc.acs.site.name";
        public static string SYSTEM_PROPERTY_KEY_ID_VALUE = "sdc.acs.application.name";
        public static string SYSTEM_ENV_KEY_HARDWARE_TYPE = "sdc.acs.hardware.type";
        public static string SYSTEM_ENV_OPERATION_SYSTEM = "sdc.acs.operation.system";
        public static string DEFAULT_CONFIGURATION_FILE = "config/@{site}/startup/startup.xml";
        public static string DEFAULT_LOGPROPERTY_FILE = "config/@{site}/startup/log-template.xml";
        public static string DEFAULT_DATABASEPROPERTY_FILE = "config/@{site}/startup/database.xml";
        public static string CHAR_BLACKSQUARE = "��";
        public static string CHAR_WHITESQUARE = "��";
        public static string CHAR_BLACKCIRCLE = "��";
        public static string CHAR_WHITECIRCLE = "��";
        public static string MSB_TIBRV = "tibrv";
        public static string MSB_JMS = "jms";
        public static string MSB_HIGHWAY101 = "highway101";
        public static string MSB_IBMMQ = "ibmmq";
        public static string MSB_MSMQ = "msmq";
        public static string MSB_NONE = "none";
        public static string MSB_ANY = "any";
        public static string XPATH_ID = "//id";
        public static string DB_ORACLE = "oracle";
        public static string DB_MSSQL = "mssql";
        public static string DB_SQLITE = "sqlite";
    }
}
