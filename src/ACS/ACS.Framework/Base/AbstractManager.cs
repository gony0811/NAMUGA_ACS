using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Logging;
using ACS.Framework.Base.Interface;

namespace ACS.Framework.Base
{
    public class AbstractManager
    {
        protected Logger logger = Logger.GetLogger(typeof(AbstractManager));
        public static Tuple<string, string> ID_RESULT_CANNOTEXECUTE = new Tuple<string, string>("39", "CANNOTEXECUTE");
        public static Tuple<string, string> ID_RESULT_NONSTANDARD = new Tuple<string, string>("38", "NONSTANDARD");        
        public static Tuple<string, string> ID_RESULT_SOURCEMACHINE_NAMEEMPTY = new Tuple<string, string>("109", "SOURCEMACHINENAMEISEMPTY");
        public static Tuple<string, string> ID_RESULT_SOURCEMACHINE_NOTFOUND = new Tuple<string, string>("25", "SOURCEMACHINENOTFOUND");
        public static Tuple<string, string> ID_RESULT_SOURCEUNIT_NAMEEMPTY = new Tuple<string, string>("105", "SOURCEUNITNAMEISEMPTY");
        public static Tuple<string, string> ID_RESULT_SOURCEUNIT_NOTFOUND = new Tuple<string, string>("24", "SOURCEUNITNOTFOUND");
        public static Tuple<string, string> ID_RESULT_CARRIER_NAMEEMPTY = new Tuple<string, string>("-3", "CARRIERNAMEISEMPTY");
        public static Tuple<string, string> ID_RESULT_SOURCEDESTMACHINE_DUPLICATE = new Tuple<string, string>("106", "SOURCEDESTMACHINEDUPLICATE");
        public static Tuple<string, string> ID_RESULT_DESTMACHINE_NOTFOUND = new Tuple<string, string>("21", "DESTMACHINENOTFOUND");
        public static Tuple<string, string> ID_RESULT_DESTMACHINE_NAMEEMPTY = new Tuple<string, string>("103", "DESTMACHINENAMEISEMPTY");
        public static Tuple<string, string> ID_RESULT_DESTUNIT_NAMEEMPTY = new Tuple<string, string>("104", "DESTUNITNAMEISEMPTY");
        public static Tuple<string, string> ID_RESULT_TRANSPORTCOMMAND_IDEMPTY = new Tuple<string, string>("107", "TRANSPORTCOMMANDIDISEMPTY");
        public static Tuple<string, string> ID_RESULT_TRANSPORTCOMMAND_NOTFOUND = new Tuple<string, string>("108", "TRANSPORTCOMMANDNOTFOUND");
        public static Tuple<string, string> ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE = new Tuple<string, string>("-4", "COMMANDNOTABLETOEXECUTE");
        public static Tuple<string, string> ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED = new Tuple<string, string>("102", "COMMANDALREADYREQUESTED");
        public static Tuple<string, string> ID_RESULT_CARRIER_HAS_ANOTHER_TRANSPORTJOB = new Tuple<string, string>("101", "CARRIERHASANOTHERTRANSPORTJOB");
        public static Tuple<string, string> ID_RESULT_NOTSAMEBAY = new Tuple<string, string>("22", "NOTSAMEBAY");
        public static Tuple<string, string> ID_RESULT_CURRENTUNIT_NAMEEMPTY = new Tuple<string, string>("-6", "CURRENTUNITNAMEEMPTY");
        public static Tuple<string, string> ID_RESULT_RESULT_CURRENTUNIT_NOTFOUND = new Tuple<string, string>("-7", "CURRENTUNITNOTFOUND");

        public static String RESULT_CANNOTEXECUTE = "CANNOTEXECUTE";
        public static String RESULT_NONSTANDARD = "NONSTANDARD";
        public static String RESULT_SOURCEMACHINE_NAMEEMPTY = "SOURCEMACHINENAMEISEMPTY";
        public static String RESULT_SOURCEMACHINE_NOTFOUND = "SOURCEMACHINENOTFOUND";
        public static String RESULT_CURRENTLOC_NOTAPPLICABLE_TEMPORARILY = "CURRENTLOCNOTAPPLICABLETEMPORARILY";
        public static String RESULT_SOURCEMACHINE_MISMATCH = "SOURCEMACHINEMISMATCH";
        public static String RESULT_SOURCEUNIT_NAMEEMPTY = "SOURCEUNITNAMEISEMPTY";
        public static String RESULT_SOURCEUNIT_NOTFOUND = "SOURCEUNITNOTFOUND";
        public static String RESULT_CARRIER_NAMEEMPTY = "CARRIERNAMEISEMPTY";
        public static String RESULT_CARRIER_NOTFOUND = "CARRIERNOTFOUND";
        public static String RESULT_MACHINE_NAMEEMPTY = "MACHINENAMEEMPTY";
        public static String RESULT_MACHINE_NOTFOUND = "MACHINENOTFOUND";
        public static String RESULT_DESTMACHINE_NAMEEMPTY = "DESTMACHINENAMEISEMPTY";
        public static String RESULT_DESTMACHINE_NOTFOUND = "DESTMACHINENOTFOUND";
        public static String RESULT_DESTMACHINE_NOTSUITABLE = "DESTMACHINENOTSUITABLE";
        public static String RESULT_CURRENTMACHINE_NAMEEMPTY = "CURRENTMACHINENAMEISEMPTY";
        public static String RESULT_CURRENTMACHINE_NOTFOUND = "CURRENTMACHINENOTFOUND";
        public static String RESULT_DESTUNIT_NAMEEMPTY = "DESTUNITNAMEISEMPTY";
        public static String RESULT_DESTUNIT_NOTFOUND = "DESTUNITNOTFOUND";
        public static String RESULT_CURRENTUNIT_NAMEEMPTY = "CURRENTUNITNAMEISEMPTY";
        public static String RESULT_CURRENTUNIT_NOTFOUND = "CURRENTUNITNOTFOUND";
        public static String RESULT_CURRENTZONE_NOTFOUND = "CURRENTZONENOTFOUND";
        public static String RESULT_MULTIPOSITION_NAMEEMPTY = "MULTIPOSITIONNAMEISEMPTY";
        public static String RESULT_MULTIPOSITION_NOTFOUND = "MULTIPOSITIONNOTFOUND";
        public static String RESULT_BATCHTRANSPORTJOB_NOTFOUND = "BATCHTRANSPORTJOBNOTFOUND";
        public static String RESULT_BATCHTRANSPORTJOB_ALREADYEXIST = "BATCHTRANSPORTJOBALREADYEXIST";
        public static String RESULT_TRANSPORTJOB_NOTFOUND = "TRANSPORTJOBNOTFOUND";
        public static String RESULT_TRANSPORTJOB_ALREADYEXIST = "TRANSPORTJOBALREADYEXIST";
        public static String RESULT_TRANSPORTCOMMAND_IDEMPTY = "TRANSPORTCOMMANDIDISEMPTY";
        public static String RESULT_TRANSPORTCOMMAND_NOTFOUND = "TRANSPORTCOMMANDNOTFOUND";
        public static String RESULT_ROUTE_NOTEXIST = "ROUTENOTEXIST";
        public static String RESULT_ROUTE_NOTFOUND = "ROUTENOTFOUND";
        public static String RESULT_PROCESSTYPE_NOTMATCHED = "PROCESSTYPENOTMATCHED";
        public static String RESULT_DEST_EXCEEDHIGHWATERMARK = "EXCEEDHIGHWATERMARK";
        public static String RESULT_DEST_NOTEXISTSUITABLESHELF = "NOTEXISTSUITABLESHELF";
        public static String RESULT_NOT_CONTROLSTATE_ONLINEREMOTE = "NOTONLINEREMOTE";
        public static String RESULT_NOT_STATE_AUTO = "NOTSTATEAUTO";
        public static String RESULT_NOT_STATE_INSERVICE = "NOTSTATEINSERVICE";
        public static String RESULT_CARRIER_UNKNOWN = "CARRIERUNKNOWN";
        public static String RESULT_CARRIER_DUPLICATED = "CARRIERDUPLICATED";
        public static String RESULT_CARRIER_MISMATCHED = "CARRIERMISMATCHED";
        public static String RESULT_CARRIER_FAILTOREAD = "CARRIERFAILTOREAD";
        public static String RESULT_CARRIER_LOCATIONMISMATCHED = "CARRIERLOCATIONMISMATCHED";
        public static String RESULT_TOUNIT_NAMEEMPTY = "TOUNITNAMEISEMPTY";
        public static String RESULT_TOUNIT_NOTFOUND = "TOUNITNOTFOUND";
        public static String RESULT_ZONE_FULL = "ZONEISFULL";
        public static String RESULT_TRANSPORTCOMMAND_NOTEXIST = "COMMANDNOTEXIST";
        public static String RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE = "COMMANDNOTABLETOEXECUTE";
        public static String RESULT_TRANSPORTCOMMAND_PARAMETERNOTVALID = "COMMANDPARAMETERNOTVALID";
        public static String RESULT_TRANSPORTCOMMAND_CONFIRMED = "COMMANDCONFIRMED";
        public static String RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED = "COMMANDALREADYREQUESTED";
        public static String RESULT_TRANSPORTCOMMAND_OBJECTNOTEXIST = "OBJECTNOTEXIST";
        public static String RESULT_TRANSPORTCOMMAND_SOURCEINTERLOCKERROR = "SOURCEINTERLOCKERROR";
        public static String RESULT_TRANSPORTCOMMAND_DESTINTERLOCKERROR = "DESTINTERLOCKERROR";
        public static String RESULT_LINKEDAUTOPORT_NOTEXIST = "LINKEDAUTOPORTNOTEXIST";
        public static String RESULT_SUITABLEAUTOPORT_NOTEXIST = "SUITABLEAUTOPORTNOTEXIST";
        public static String RESULT_SUITABLEBIDIRECTIONALAUTOPORT_NOTEXIST = "SUITABLEBIDIRECTIONALAUTOPORTNOTEXIST";
        public static String RESULT_NEEDTOPORTINOUTTYPECHANGECOMMAND = "NEEDTOPORTINOUTTYPECHANGECOMMAND";
        public static String RESULT_EXCEED_BIDIRECTIONAL_COMMAND_CAPACITY = "EXCEEDBIDIRECTIONALCOMMANDCAPACITY";
        public static String RESULT_EXCEED_PORT_CAPACITY = "EXCEEDPORTCAPACITY";
        public static String RESULT_PORT_INOUTTYPE_ALREADY_INPUT = "PORTINOUTTYPEISALREADYINPUT";
        public static String RESULT_PORT_INOUTTYPE_ALREADY_OUTPUT = "PORTINOUTTYPEISALREADYOUTPUT";
        public static String RESULT_PORT_COMMANDMACHINE_NOT_AVAILABLE = "COMMANDMACHINEISNOTAVAILABLE";
        public static String RESULT_CARRIER_HAS_ANOTHER_TRANSPORTJOB = "CARRIERHASANOTHERTRANSPORTJOB";
        public static String RESULT_REJECT_TRANSPORT_REQUEST = "REJECTTRANSPORTREQUEST";
        public static String RESULT_CURRENTLOC_ISDESTGROUP = "CURRENTLOCISDESTGROUP";
        public static String RESULT_CURRENTLOC_ISDEST = "CURRENTLOCISDEST";
        public static String RESULT_INVALID_DESTGROUPRULE = "INVALIDDESTGROUPRULE";
        public static String RESULT_NOT_EXISTSUITABLEDESTMACHINE_INGROUP = "NOTEXISTSUITABLEDESTMACHINEINGROUP";
        public static String RESULT_NOT_EXISTSUITABLEDESTPORT_INGROUP = "NOTEXISTSUITABLEDESTPORTINGROUP";
        public static String RESULT_NOT_NEEDTOCHANGE_DEST = "NOTNEEDTOCHANGEDEST";
        public static String RESULT_CURRENTLOC_IS_ALTERNATE_STORAGE = "CURRENTLOCISALTERNATESTORAGE";
        public static String RESULT_INVALID_MACHINEALTERNATESTORAGEGROUPRULE = "INVALIDMACHINEALTERNATESTORAGEGROUPRULE";
        public static String RESULT_INVALID_ZONEALTERNATESTORAGEGROUPRULE = "INVALIDZONEALTERNATESTORAGEGROUPRULE";
        public static String RESULT_INVALID_UNITALTERNATESTORAGEGROUPRULE = "INVALIDUNITALTERNATESTORAGEGROUPRULE";
        public static String RESULT_INVALID_MACHINERECOVERYDESTGROUPRULE = "INVALIDMACHINERECOVERYDESTGROUPRULE";
        public static String RESULT_INVALID_UNITRECOVERYDESTGROUPRULE = "INVALIDUNITRECOVERYDESTGROUPRULE";
        public static String RESULT_SOURCEDESTMACHINE_DUPLICATE = "SOURCEDESTMACHINEDUPLICATE";
        public static String RESULT_NOTSAMEBAY = "NOTSAMEBAY";
        public static string LINE_SEPARATOR;
        public static string KEY_DELIMITER = "|";

        string Name { get; set; }

        public IPersistentDao PersistentDao { get; set; }

        public ILogManager LogManager { get; set; }

        public AbstractManager()
        {
        }
        public virtual void Init()
        {
            
        }

        public virtual bool IsNameValid(string name)
        {
            bool result = false;

            if(!string.IsNullOrEmpty(name) && !name.Equals("NA"))
            {
                return true;
            }

            return result;
        }

        public virtual bool IsIdValid(string id)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(id))
            {
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }

        //public virtual void fireWaitedJob(TransportJob transportJob) { }

    }
}
