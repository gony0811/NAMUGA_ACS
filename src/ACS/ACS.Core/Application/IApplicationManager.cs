using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Application.Model;
using ACS.Core.Resource.Model.Factory.Machine;
using ACS.Core.Resource.Model.Factory.Unit;

namespace ACS.Core.Application
{
    public interface IApplicationManager
    {
        bool IsWifiMode(string paramString);
        string GetNioConnectMode(string paramString);
        string GetApplicationName();

        void CreateApplication(Model.Application paramApplication);

        Model.Application GetApplication(String paramString);

        IList GetApplicationsByType(String paramString);

        IList GetApplicationNamesByType(String paramString);

        IList GetApplicationNamesByType(String paramString1, String paramString2);

        IList GetApplicationNamesByState(String paramString1, String paramString2);

        IList GetApplicationNamesByState(String paramString1, String paramString2, String paramString3);

        IList GetApplicationNamesByInitialHardware(String paramString);

        IList GetApplicationNamesByRunningHardware(String paramString);

        IList GetApplicationNamesByRunningHardware(String paramString, bool paramBoolean);

        IList GetApplicationNamesByRunningHardware(String paramString, bool paramBoolean1, bool paramBoolean2);
        IList GetApplicationNamesByRunningHardware(string type, string state, string runningHardware);
        IList GetApplications(String paramString1, String paramString2);

        IList GetApplicationsByState(String paramString);

        IList GetApplications();

        IList GetQueuedUiApplicationManagers();
        int UpdateApplicationManagersState(String paramString1, String paramString2);
        int UpdateApplicationManagersReply(String paramString1, String paramString2, String paramString3);
        void DeleteApplication(Model.Application paramApplication);

        int DeleteApplications();

        void UpdateApplication(Model.Application paramApplication);

        int UpdateApplication(String paramString1, Object paramObject1, String paramString2, Object paramObject2);

        int UpdateApplicationState(String paramString1, String paramString2);

        int UpdateApplicationState(String paramString1, String paramString2, DateTime paramDate);

        int UpdateApplicationCheckTime(String paramString, DateTime paramDate);

        String GetTransportRuleWhenPhysicalFull(MassStorageMachine paramMassStorageMachine);

        String GetTransportRuleWhenPhysicalFull(String paramString);

    
        String GetGarbageCarrierControlRuleForMassStroage(String paramString);

        String GetUnknownCarrierControlRuleForMassStorage(MassStorageMachine paramMassStorageMachine);

        String GetUnknownCarrierControlRuleForMassStorage(String paramString);

        String GetUnknownCarrierControlRuleForMassStorage(String paramString1, String paramString2);

        String GetDuplicateCarrierControlRuleForMassStorage(MassStorageMachine paramMassStorageMachine);

        String GetDuplicateCarrierControlRuleForMassStorage(String paramString);

        String GetMismatchCarrierControlRuleForMassStorage(MassStorageMachine paramMassStorageMachine);

        String GetMismatchCarrierControlRuleForMassStorage(String paramString);

        String GetGarbageCarrierControlRuleForRailStorage(RailStorageMachine paramRailStorageMachine);

        String GetGarbageCarrierControlRuleForRailStorage(String paramString);

        bool ForeTransferWhenAwake(Unit paramUnit);

        bool ForeTransferWhenAwake(String paramString1, String paramString2);

        bool ExecuteBatchJobInDueOrder();

        bool CheckRailStorageMachineTransferState();

        bool CheckRailStorageMachineTransferState(RailStorageMachine paramRailStorageMachine);

        bool CheckRailStorageMachineTransferState(String paramString);

        bool CheckRailStorageMachinePortOccupied();

        bool CheckProcessMachineTransferState();

        bool CheckProcessMachineTransferState(ProcessMachine paramProcessMachine);

        bool CheckProcessMachineTransferState(String paramString);

        bool UseWaitOnProcessMachineForUnloading();

        bool UseWaitOnProcessMachineForUnloading(ProcessMachine paramProcessMachine);

        bool UseWaitOnProcessMachineForUnloading(String paramString);

        bool CheckProcessMachinePortOccupied();

        bool CheckProcessMachinePortOccupied(Port paramPort);

        bool CheckProcessMachinePortOccupied(String paramString1, String paramString2);

        bool CheckProcessMachinePortSubState();

        bool CheckProcessMachinePortSubState(Port paramPort);

        bool CheckProcessMachinePortSubState(String paramString1, String paramString2);

        bool UseTransportFailWhenFirstTransport();

        bool UseCarrierProcessType();

        bool UseBidirectionalNode();

        bool ConsiderAlternatedJobWhenTransferring();

        bool AcceptTransportRequest();

        bool PermitMaterialMovement();

        bool AcceptDestChangeRequest();

        void CreateDefaultOptions();

        Option CreateDefaultOptionUseBalanceLoad();

        Option CreateDefaultOptionUseDynamicLoad();

        Option CreateDefaultOptionUseHeuristicDelay();

        Option CreateDefaultOptionUseInternalRoute();

        Option CreateDefaultOptionSelectToByStocker();

        Option CreateDefaultOptionUseBidirectionalNode();

        void CreateOption(Option option);
    }
}
