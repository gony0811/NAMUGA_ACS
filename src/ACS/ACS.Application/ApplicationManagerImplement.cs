using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using ACS.Framework.Base;
using ACS.Framework.Application;
using ACS.Framework.Application.Model;
using ACS.Framework.Resource.Model.Factory.Machine;
using ACS.Framework.Resource.Model.Factory.Unit;

namespace ACS.Application
{
    public class ApplicationManagerImplement : AbstractManager, IApplicationManager
    {

        public string GetApplicationName()
        {           
            return ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_ID_VALUE];
        }

        public void CreateApplication(ACS.Framework.Application.Model.Application application)
        {
            this.PersistentDao.Save(application);
        }

        public ACS.Framework.Application.Model.Application GetApplication(string name)
        {
            return (ACS.Framework.Application.Model.Application)this.PersistentDao.FindByName(typeof(ACS.Framework.Application.Model.Application), name, false);
        }

        public System.Collections.IList GetApplicationsByType(string type)
        {
            return this.PersistentDao.FindByAttribute(typeof(ACS.Framework.Application.Model.Application), "Type", type);
        }

        public System.Collections.IList GetApplicationNamesByType(string type)
        {
            return this.PersistentDao.FindPropertyByAttributesOrderBy(typeof(ACS.Framework.Application.Model.Application), "Name", "Type", type, "Name");
        }

        public System.Collections.IList GetApplicationNamesByType(string type, string runningHardware)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("Type", type);

            //200630 REFRESHCACHE
            attributes.Add("RunningHardware", runningHardware);
            //

            System.Collections.IList applications = this.PersistentDao.FindByAttributes(typeof(ACS.Framework.Application.Model.Application), attributes);
            System.Collections.ArrayList names = new System.Collections.ArrayList();
            foreach (ACS.Framework.Application.Model.Application app in applications)
            {
                names.Add(app.Name);
            }
            return names;
        }

        public System.Collections.IList GetApplicationNamesByState(string type, string state)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("Type", type);
            attributes.Add("State", state);

            return this.PersistentDao.FindByAttributesOrderBy(typeof(ACS.Framework.Application.Model.Application), attributes, "Name")
                .Cast<ACS.Framework.Application.Model.Application>()
                .Select(app => app.Name)
                .ToList();
        }     

        public System.Collections.IList GetApplicationNamesByState(string type, string state, string excludeApplicationName)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("Type", type);
            attributes.Add("State", state);

            //[2018.08.03] ksg : Need to check function of the Restrictions.NotEqProperty
            return this.PersistentDao.FindByAttributesOrderBy(typeof(ACS.Framework.Application.Model.Application), attributes, "Name")
                .Cast<ACS.Framework.Application.Model.Application>()
                .Where(app => !app.Name.Equals(excludeApplicationName))
                .Select(app => app.Name)
                .ToList();
        }

        public System.Collections.IList GetApplicationNamesByInitialHardware(string initialHardware)
        {
            return this.PersistentDao.FindPropertyByAttributes(typeof(ACS.Framework.Application.Model.Application), "Name", "InitialHardware", initialHardware);
        }

        public System.Collections.IList GetApplicationNamesByRunningHardware(string runningHardware)
        {
            return GetApplicationNamesByRunningHardware(runningHardware, false);
        }

        public System.Collections.IList GetApplicationNamesByRunningHardware(string runningHardware, bool excludeControlServer)
        {
            if (!excludeControlServer)
            {
                return this.PersistentDao.FindPropertyByAttributes(typeof(ACS.Framework.Application.Model.Application), "Name", "RunningHardware", runningHardware);
            }

            return this.PersistentDao.FindByAttribute(typeof(ACS.Framework.Application.Model.Application), "RunningHardware", runningHardware)
                .Cast<ACS.Framework.Application.Model.Application>()
                .Where(app => !app.Type.Equals("control"))
                .Select(app => app.Name)
                .ToList();
        }

        public System.Collections.IList GetApplicationNamesByRunningHardware(string runningHardware, bool excludeInvalidState, bool excludeControlServer)
        {
            System.Collections.IList applications = this.PersistentDao.FindByAttribute(typeof(ACS.Framework.Application.Model.Application), "RunningHardware", runningHardware);

            IEnumerable<ACS.Framework.Application.Model.Application> filtered = applications.Cast<ACS.Framework.Application.Model.Application>();

            if (excludeInvalidState)
            {
                filtered = filtered.Where(app => !app.State.Equals("inactive"));
            }

            if (excludeControlServer)
            {
                filtered = filtered.Where(app => !app.Type.Equals("control"));
            }

            return filtered.Select(app => app.Name).ToList();
        }

        public System.Collections.IList GetApplications(string type, string state)
        {
           Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("Type", type);
            attributes.Add("State", state);

            return this.PersistentDao.FindByAttributes(typeof(ACS.Framework.Application.Model.Application), attributes);
        }

        public System.Collections.IList GetApplicationsByState(string state)
        {
            return this.PersistentDao.FindByAttribute(typeof(ACS.Framework.Application.Model.Application), "state", state);
        }

        public System.Collections.IList GetApplications()
        {
            return this.PersistentDao.FindAllOrderBy(typeof(ACS.Framework.Application.Model.Application), "name");
        }

        public System.Collections.IList GetQueuedUiApplicationManagers()
        {
            return this.PersistentDao.FindByAttribute(typeof(ACS.Framework.Application.Model.UiApplicationManager), "STATE", "REQUEST");
        }
        public int UpdateApplicationManagersState(string id, string state)
        {
            Dictionary<string, object> setAttributes = new Dictionary<string, object>();

            setAttributes.Add("STATE", state);
            setAttributes.Add("EVENTTIME", DateTime.Now);

            return this.PersistentDao.UpdateByAttributes(typeof(ACS.Framework.Application.Model.UiApplicationManager), setAttributes, "ID", id);
        }
        public int UpdateApplicationManagersReply(string id, string reply, string state)
        {
            Dictionary<string, object> setAttributes = new Dictionary<string, object>();

            setAttributes.Add("REPLY", reply);
            setAttributes.Add("STATE", state);
            setAttributes.Add("EVENTTIME", DateTime.Now);

            return this.PersistentDao.UpdateByAttributes(typeof(ACS.Framework.Application.Model.UiApplicationManager), setAttributes, "ID", id);
        }
        public void DeleteApplication(ACS.Framework.Application.Model.Application application)
        {
            this.PersistentDao.Delete(application);
        }

        public int DeleteApplications()
        {
            return this.PersistentDao.DeleteAll(typeof(ACS.Framework.Application.Model.Application));
        }

        public void UpdateApplication(ACS.Framework.Application.Model.Application application)
        {
            this.PersistentDao.Update(application);
        }

        public int UpdateApplication(string setName, object setValue, string conditionName, object conditionValue)
        {
            return this.PersistentDao.UpdateByAttribute(typeof(ACS.Framework.Application.Model.Application), setName, setValue, conditionName, conditionValue);
        }

        public int UpdateApplicationState(string name, string state)
        {
            Dictionary<string, object> setAttributes = new Dictionary<string, object>();

            setAttributes.Add("State", state);
            setAttributes.Add("EditTime", DateTime.Now);

            return this.PersistentDao.UpdateByName(typeof(ACS.Framework.Application.Model.Application), setAttributes, name);
        }

        public int UpdateApplicationState(string name, string state, DateTime date)
        {
            Dictionary<string, object> setAttributes = new Dictionary<string, object>();

            setAttributes.Add("State", state);
            setAttributes.Add("EditTime", date);

            return this.PersistentDao.UpdateByName(typeof(ACS.Framework.Application.Model.Application), setAttributes, name);
        }

        public int UpdateApplicationCheckTime(string name, DateTime date)
        {
            return this.PersistentDao.UpdateByName(typeof(ACS.Framework.Application.Model.Application), "checkTime", date, name);
        }

        public string GetTransportRuleWhenPhysicalFull(MassStorageMachine massStorageMachine)
        {
            throw new NotImplementedException();
        }

        public string GetTransportRuleWhenPhysicalFull(string paramString)
        {
            throw new NotImplementedException();
        }

        public string GetGarbageCarrierControlRuleForMassStroage(string paramString)
        {
            throw new NotImplementedException();
        }

        public string GetUnknownCarrierControlRuleForMassStorage(MassStorageMachine paramMassStorageMachine)
        {
            throw new NotImplementedException();
        }

        public string GetUnknownCarrierControlRuleForMassStorage(string paramString)
        {
            throw new NotImplementedException();
        }

        public string GetUnknownCarrierControlRuleForMassStorage(string paramString1, string paramString2)
        {
            throw new NotImplementedException();
        }

        public string GetDuplicateCarrierControlRuleForMassStorage(MassStorageMachine paramMassStorageMachine)
        {
            throw new NotImplementedException();
        }

        public string GetDuplicateCarrierControlRuleForMassStorage(string paramString)
        {
            throw new NotImplementedException();
        }

        public string GetMismatchCarrierControlRuleForMassStorage(MassStorageMachine paramMassStorageMachine)
        {
            throw new NotImplementedException();
        }

        public string GetMismatchCarrierControlRuleForMassStorage(string paramString)
        {
            throw new NotImplementedException();
        }

        public string GetGarbageCarrierControlRuleForRailStorage(RailStorageMachine paramRailStorageMachine)
        {
            throw new NotImplementedException();
        }

        public string GetGarbageCarrierControlRuleForRailStorage(string paramString)
        {
            throw new NotImplementedException();
        }

        public bool ForeTransferWhenAwake(Unit paramUnit)
        {
            throw new NotImplementedException();
        }

        public bool ForeTransferWhenAwake(string paramString1, string paramString2)
        {
            throw new NotImplementedException();
        }

        public bool ExecuteBatchJobInDueOrder()
        {
            throw new NotImplementedException();
        }

        public bool CheckRailStorageMachineTransferState()
        {
            throw new NotImplementedException();
        }

        public bool CheckRailStorageMachineTransferState(RailStorageMachine paramRailStorageMachine)
        {
            throw new NotImplementedException();
        }

        public bool CheckRailStorageMachineTransferState(string paramString)
        {
            throw new NotImplementedException();
        }

        public bool CheckRailStorageMachinePortOccupied()
        {
            throw new NotImplementedException();
        }

        public bool CheckProcessMachineTransferState()
        {
            throw new NotImplementedException();
        }

        public bool CheckProcessMachineTransferState(ProcessMachine paramProcessMachine)
        {
            throw new NotImplementedException();
        }

        public bool CheckProcessMachineTransferState(string paramString)
        {
            throw new NotImplementedException();
        }

        public bool UseWaitOnProcessMachineForUnloading()
        {
            throw new NotImplementedException();
        }

        public bool UseWaitOnProcessMachineForUnloading(ProcessMachine paramProcessMachine)
        {
            throw new NotImplementedException();
        }

        public bool UseWaitOnProcessMachineForUnloading(string paramString)
        {
            throw new NotImplementedException();
        }

        public bool CheckProcessMachinePortOccupied()
        {
            throw new NotImplementedException();
        }

        public bool CheckProcessMachinePortOccupied(Port paramPort)
        {
            throw new NotImplementedException();
        }

        public bool CheckProcessMachinePortOccupied(string paramString1, string paramString2)
        {
            throw new NotImplementedException();
        }

        public bool CheckProcessMachinePortSubState()
        {
            throw new NotImplementedException();
        }

        public bool CheckProcessMachinePortSubState(Port paramPort)
        {
            throw new NotImplementedException();
        }

        public bool CheckProcessMachinePortSubState(string paramString1, string paramString2)
        {
            throw new NotImplementedException();
        }

        public bool UseTransportFailWhenFirstTransport()
        {
            throw new NotImplementedException();
        }

        public bool UseCarrierProcessType()
        {
            throw new NotImplementedException();
        }

        public bool UseBidirectionalNode()
        {
            throw new NotImplementedException();
        }

        public bool ConsiderAlternatedJobWhenTransferring()
        {
            throw new NotImplementedException();
        }

        public bool AcceptTransportRequest()
        {
            throw new NotImplementedException();
        }

        public bool PermitMaterialMovement()
        {
            throw new NotImplementedException();
        }

        public bool AcceptDestChangeRequest()
        {
            throw new NotImplementedException();
        }


        public void CreateDefaultOptions()
        {
            Option option = GetOption("1001");

            if(option == null)
            {
                CreateDefaultOptionUseBalanceLoad();
            }

            option = GetOption("1002");

            if (option == null)
            {
                CreateDefaultOptionUseDynamicLoad();
            }

            option = GetOption("1003");

            if (option == null)
            {
                CreateDefaultOptionUseHeuristicDelay();
            }
            option = GetOption("1004");
            if (option == null)
            {
                CreateDefaultOptionUseBidirectionalNode();
            }
            option = GetOption("1102");
            if (option == null)
            {
                CreateDefaultOptionRuleForSuitableMachineInGroup("F");
            }
            option = GetOption("1103");
            if (option == null)
            {
                CreateDefaultOptionUseInternalRoute();
            }
            option = GetOption("2001");
            if (option == null)
            {
                CreateDefaultOptionForeTransferWhenDestUnavailable();
            }
            option = GetOption("2002");
            if (option == null)
            {
                CreateDefaultOptionUseTransportFailWhenFirstTransport();
            }
            option = GetOption("2003");
            if (option == null)
            {
                CreateDefaultOptionConsiderAlternatedJobWhenTransferring();
            }
            option = GetOption("2005");
            if (option == null)
            {
                CreateDefaultOptionConditionForDestRequest();
            }
            option = GetOption("2006");
            if (option == null)
            {
                CreateDefaultOptionExecuteBatchJobInDueOrder();
            }
            option = GetOption("2007");
            if (option == null)
            {
                CreateDefaultOptionAwakeLimitCountForAlternatedJob();
            }
            option = GetOption("2008");
            if (option == null)
            {
                CreateDefaultOptionForeTransferWhenAwake();
            }
            option = GetOption("3001");
            if (option == null)
            {
                CreateDefaultOptionSelectToByStocker("F");
            }
            option = GetOption("3002");
            if (option == null)
            {
                CreateDefaultOptionUseStageCommand();
            }
            option = GetOption("3003");
            if (option == null)
            {
                CreateDefaultOptionUseScanCommand();
            }
            option = GetOption("3004");
            if (option == null)
            {
                CreateDefaultOptionUsePriorityChangeCommand();
            }
            option = GetOption("3005");
            if (option == null)
            {
                CreateDefaultOptionUsePortInOutTypeChangeCommand();
            }
            option = GetOption("3006");
            if (option == null)
            {
                CreateDefaultOptionUseUpdateCommand();
            }
            option = GetOption("3007");
            if (option == null)
            {
                CreateDefaultOptionUsePermittedCapacity();
            }
            option = GetOption("3101");
            if (option == null)
            {
                CreateDefaultOptionUsePortIncreasingPriority();
            }
            option = GetOption("3102");
            if (option == null)
            {
                CreateDefaultOptionUseScanCommandForGarbageCarrier();
            }
            option = GetOption("3103");
            if (option == null)
            {
                CreateDefaultOptionUsePriorityBoostUp();
            }
            option = GetOption("4001");
            if (option == null)
            {
                CreateDefaultOptionUseCarrierProcessType();
            }
            option = GetOption("5101");
            if (option == null)
            {
                CreateDefaultOptionControlMultiCraneByMcs("F");
            }
            option = GetOption("5102");
            if (option == null)
            {
                CreateDefaultOptionGarbageCarrierControlRuleForMassStorage();
            }
            option = GetOption("5106");
            if (option == null)
            {
                CreateDefaultOptionUnknownCarrierControlRuleForMassStorage();
            }
            option = GetOption("5105");
            if (option == null)
            {
                CreateDefaultOptionTransportRuleWhenPhysicalFull();
            }
            option = GetOption("5201");
            if (option == null)
            {
                CreateDefaultOptionCheckProcessMachineTransferState();
            }
            option = GetOption("5301");
            if (option == null)
            {
                CreateDefaultOptionCheckRailStorageMachineTransferState();
            }
            option = GetOption("5206");
            if (option == null)
            {
                CreateDefaultOptionUseWaitOnProcessMachineForUnloading();
            }
            option = GetOption("5109");
            if (option == null)
            {
                CreateDefaultOptionSendInstallCommandWhenEnhancedCarrierListZero();
            }
            option = GetOption("5302");
            if (option == null)
            {
                CreateDefaultOptionCheckRailStorageMachinePortOccupied();
            }
            option = GetOption("6101");
            if (option == null)
            {
                CreateDefaultOptionCheckProcessMachinePortOccupied();
            }
            option = GetOption("6102");
            if (option == null)
            {
                CreateDefaultOptionCheckProcessMachinePortSubState();
            }
            option = GetOption("6103");
            if (option == null)
            {
                CreateDefaultOptionUseRecoveryTransportOnProcessMachinePort();
            }
            option = GetOption("6104");
            if (option == null)
            {
                CreateDefaultOptionUseChangePreviousCarrierLocationNotApplicableOnPort();
            }
            option = GetOption("7001");
            if (option == null)
            {
                CreateDefaultOptionAcceptTransportRequest();
            }
            option = GetOption("7002");
            if (option == null)
            {
                CreateDefaultOptionPermitMaterialMovement();
            }
            option = GetOption("7003");
            if (option == null)
            {
                CreateDefaultOptionAcceptDestChangeRequest();
            }


        }

        private Option CreateDefaultOptionAcceptDestChangeRequest()
        {
            Option option = new Option();
            option.Id = "7003";
            option.Name = "7003";
            option.NameDescription = "ACCEPTDESTCHANGEREQUEST";
            option.Value = "01";
            option.ValueDescription = "TRUE";

            CreateOption(option);

            return option;
        }

        private Option CreateDefaultOptionPermitMaterialMovement()
        {
            Option option = new Option();
            option.Id = "7002";
            option.Name = "7002";
            option.NameDescription = "PERMITMATERIALMOVEMENT";
            option.Value = "01";
            option.ValueDescription = "TRUE";

            CreateOption(option);

            return option;
        }

        private Option CreateDefaultOptionAcceptTransportRequest()
        {
            Option option = new Option();
            option.Id = "7001";
            option.Name = "7001";
            option.NameDescription = "ACCEPTTRANSPORTREQUEST";
            option.Value = "01";
            option.ValueDescription = "TRUE";

            CreateOption(option);

            return option;
        }

        private Option CreateDefaultOptionUseChangePreviousCarrierLocationNotApplicableOnPort()
        {
            Option option = new Option();
            option.Id = "6104";
            option.Name = "6104";
            option.NameDescription = "USECHANGEPREVIOUSCARRIERLOCATIONTONOTAPPLICABLEONPORT";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        private Option CreateDefaultOptionUseRecoveryTransportOnProcessMachinePort()
        {
            Option option = new Option();
            option.Id = "6103";
            option.Name = "6103";
            option.NameDescription = "USERECOVERYTRANSPORTONPROCESSMACHINEPORT";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionCheckProcessMachinePortSubState()
        {
            Option option = new Option();
            option.Id = "6102";
            option.Name = "6102";
            option.NameDescription = "CHECKPROCESSMACHINEPORTSUBSTATE";
            option.Value = "01";
            option.ValueDescription = "TRUE";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionCheckProcessMachinePortOccupied()
        {
            Option option = new Option();
            option.Id = "6101";
            option.Name = "6101";
            option.NameDescription = "CHECKPROCESSMACHINEPORTOCCUPIED";
            option.Value = "01";
            option.ValueDescription = "TRUE";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionCheckRailStorageMachinePortOccupied()
        {
            Option option = new Option();
            option.Id = "5302";
            option.Name = "5302";
            option.NameDescription = "CHECKRAILSTORAGEMACHINEPORTOCCUPIED";
            option.Value = "01";
            option.ValueDescription = "TRUE";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionSendInstallCommandWhenEnhancedCarrierListZero()
        {
            Option option = new Option();
            option.Id = "5109";
            option.Name = "5109";
            option.NameDescription = "INSTALLCOMMANDWHENENHANCEDCARRIERLISTZERO";
            option.Value = "01";
            option.ValueDescription = "USED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUseWaitOnProcessMachineForUnloading()
        {
            Option option = new Option();
            option.Id = "5206";
            option.Name = "5206";
            option.NameDescription = "USEWAITONPROCESSMACHINEFORUNLOADING";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionCheckRailStorageMachineTransferState()
        {
            Option option = new Option();
            option.Id = "5301";
            option.Name = "5301";
            option.NameDescription = "CHECKRAILSTORAGEMACHINETRANSFERSTATE";
            option.Value = "01";
            option.ValueDescription = "TRUE";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionCheckProcessMachineTransferState()
        {
            Option option = new Option();
            option.Id = "5201";
            option.Name = "5201";
            option.NameDescription = "CHECKPROCESSMACHINETRANSFERSTATE";
            option.Value = "01";
            option.ValueDescription = "TRUE";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionTransportRuleWhenPhysicalFull()
        {
            Option option = new Option();
            option.Id = "5105";
            option.Name = "5105";
            option.NameDescription = "TRANSPORTRULEWHENPHYSICALFULL";
            option.Value = "01";
            option.ValueDescription = "TOALTERNATESTORAGE";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUnknownCarrierControlRuleForMassStorage()
        {
            Option option = new Option();
            option.Id = "5106";
            option.Name = "5106";
            option.NameDescription = "UNKNOWNCARRIERCONTROLRULEFORSTORAGE";
            option.Value = "01";
            option.ValueDescription = "APPLICATIONSELECT";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionGarbageCarrierControlRuleForMassStorage()
        {
            Option option = new Option();
            option.Id = "5102";
            option.Name = "5102";
            option.NameDescription = "GARBAGECARRIERCONTROLRULEFORSTORAGE";
            option.Value = "01";
            option.ValueDescription = "APPLICATIONSELECT";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionControlMultiCraneByMcs(string used)
        {
            Option option = new Option();
            option.Id = "5101";
            option.Name = "5101";
            option.NameDescription = "CONTROLMULTICRANEBYMCS";
            option.Value = "02";
            option.ValueDescription = "FALSE";
            option.Used = used;
            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUseCarrierProcessType()
        {
            Option option = new Option();
            option.Id = "4001";
            option.Name = "4001";
            option.NameDescription = "USECARRIERPROCESSTYPE";
            option.Value = "01";
            option.ValueDescription = "USED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUsePriorityBoostUp()
        {
            Option option = new Option();
            option.Id = "3103";
            option.Name = "3103";
            option.NameDescription = "USEPRIORITYBOOSTUP";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUseScanCommandForGarbageCarrier()
        {
            Option option = new Option();
            option.Id = "3102";
            option.Name = "3102";
            option.NameDescription = "USESCANCOMMANDFORGARBAGECARRIER";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUsePortIncreasingPriority()
        {
            Option option = new Option();
            option.Id = "3101";
            option.Name = "3101";
            option.NameDescription = "USEPORTINCREASINGPRIORITY";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUsePermittedCapacity()
        {
            Option option = new Option();
            option.Id = "3007";
            option.Name = "3007";
            option.NameDescription = "USEPERMITTEDCAPACITY";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUseUpdateCommand()
        {
            Option option = new Option();
            option.Id = "3006";
            option.Name = "3006";
            option.NameDescription = "USEUPDATECOMMAND";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUsePortInOutTypeChangeCommand()
        {
            Option option = new Option();
            option.Id = "3005";
            option.Name = "3005";
            option.NameDescription = "USEPORTINOUTTYPECHANGECOMMAND";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUsePriorityChangeCommand()
        {
            Option option = new Option();
            option.Id = "3004";
            option.Name = "3004";
            option.NameDescription = "USEPRIORITYCHANGECOMMAND";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUseScanCommand()
        {
            Option option = new Option();
            option.Id = "3003";
            option.Name = "3003";
            option.NameDescription = "USESCANCOMMAND";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUseStageCommand()
        {
            Option option = new Option();
            option.Id = "3002";
            option.Name = "3002";
            option.NameDescription = "USESTAGECOMMAND";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionSelectToByStocker(string used)
        {
            Option option = new Option();
            option.Id = "3001";
            option.Name = "3001";
            option.NameDescription = "SELECTTOBYSTOCKER";
            option.Value = "01";
            option.ValueDescription = "TRUE";
            option.Used = used;

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionForeTransferWhenAwake()
        {
            Option option = new Option();
            option.Id = "2008";
            option.Name = "2008";
            option.NameDescription = "FORETRANSFERWHENAWAKE";
            option.Value = "01";
            option.ValueDescription = "NOTUSED";
            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionAwakeLimitCountForAlternatedJob()
        {
            Option option = new Option();
            option.Id = "2007";
            option.Name = "2007";
            option.NameDescription = "AWAKELIMITCOUNTFORALTERNATEDJOB";
            option.Value = "0";
            option.ValueDescription = "NOTUSED";
            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionExecuteBatchJobInDueOrder()
        {
            Option option = new Option();
            option.Id = "2006";
            option.Name = "2006";
            option.NameDescription = "EXECUTEBATCHJOBINDUEORDER";
            option.Value = "01";
            option.ValueDescription = "TRUE";
            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionConditionForDestRequest()
        {
            Option option = new Option();
            option.Id = "2005";
            option.Name = "2005";
            option.NameDescription = "CONSIDERALTERNATEDJOBWHENTRANSFERRING";
            option.Value = "01";
            option.ValueDescription = "USED";
            option.SubValue = "-1";
            option.SubValueDescription = "TIME";
            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionConsiderAlternatedJobWhenTransferring()
        {
            Option option = new Option();
            option.Id = "2003";
            option.Name = "2003";
            option.NameDescription = "CONSIDERALTERNATEDJOBWHENTRANSFERRING";
            option.Value = "01";
            option.ValueDescription = "USED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUseTransportFailWhenFirstTransport()
        {
            Option option = new Option();
            option.Id = "2002";
            option.Name = "2002";
            option.NameDescription = "USETRANSPORTFAILWHENFIRSTTRANSPORT";
            option.Value = "02";
            option.ValueDescription = "FALSE";
            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionForeTransferWhenDestUnavailable()
        {
            Option option = new Option();
            option.Id = "2001";
            option.Name = "2001";
            option.NameDescription = "FORETRANSFERWHENDESTUNAVAILABLE";
            option.Value = "01";
            option.ValueDescription = "TRUE";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionRuleForSuitableMachineInGroup(string used)
        {
            Option option = new Option();
            option.Id = "1102";
            option.Name = "1102";
            option.NameDescription = "RULEFORSUITABLEMACHINEINGROUP";
            option.Value = "01";
            option.ValueDescription = "FULLRATE";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUseBidirectionalNode()
        {
            Option option = new Option();
            option.Id = "1004";
            option.Name = "1004";
            option.NameDescription = "USEBIDIRECTIONALNODE";
            option.Value = "01";
            option.ValueDescription = "USED";

            CreateOption(option);

            return option;
        }

        private Option GetOption(string optionId)
        {
            return (Option)this.PersistentDao.FindByName(typeof(Option), optionId, false);
        }

        private Option GetOptionByName(string name)
        {
            return (Option)this.PersistentDao.FindByName(typeof(Option), name, false);
        }

        public Option CreateDefaultOptionUseBalanceLoad()
        {
            Option option = new Option();

            option.Id = "1001";
            option.Name = "1001";
            option.NameDescription = "USEBALANCELOAD";
            option.Value = "01";
            option.ValueDescription = "USED";

            CreateOption(option);

            return option;
        }

        public void CreateOption(Option option)
        {
            this.PersistentDao.Save(option);
        }

        public Option CreateDefaultOptionUseDynamicLoad()
        {
            Option option = new Option();

            option.Id = "1002";
            option.Name = "1002";
            option.NameDescription = "USEDYNAMICLOAD";
            option.Value = "01";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUseHeuristicDelay()
        {
            Option option = new Option();

            option.Id = "1003";
            option.Name = "1003";
            option.NameDescription = "USEHEURISTICDELAY";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionUseInternalRoute()
        {
            Option option = new Option();

            option.Id = "1103";
            option.Name = "1103";
            option.NameDescription = "USEINTERNALROUTE";
            option.Value = "02";
            option.ValueDescription = "NOTUSED";

            CreateOption(option);

            return option;
        }

        public Option CreateDefaultOptionSelectToByStocker()
        {
            Option option = new Option();

            option.Id = "3001";
            option.Name = "3001";
            option.NameDescription = "SELECTTOBYSTOCKER";
            option.Value = "01";
            option.ValueDescription = "TRUE";

            CreateOption(option);

            return option;
        }
    }
}
