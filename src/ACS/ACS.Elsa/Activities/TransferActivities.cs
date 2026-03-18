
using Elsa.Workflows.Attributes;

namespace ACS.Elsa.Activities
{
    // ═══════════════════════════════════════════════════════════════
    //  TransferServiceEx Activities
    //  Category: ACS.Transfer
    // ═══════════════════════════════════════════════════════════════

    [Activity("ACS.Transfer", "Create Transport Command",
        "Creates a new transport command from the transfer message.")]
    public class CreateTransportCommand : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CreateTransportCommand";
    }

    [Activity("ACS.Transfer", "Create Recharge TC",
        "Creates a recharge transport command (CHARGEMOVE) for the vehicle.")]
    public class CreateRechargeTransportCommand : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CreateRechargeTransportCommand";
    }

    [Activity("ACS.Transfer", "Delete Transport Command",
        "Deletes a transport command.")]
    public class DeleteTransportCommand : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "DeleteTransportCommand";
    }

    [Activity("ACS.Transfer", "Change TC Vehicle ID",
        "Updates transport command with the assigned vehicle ID.")]
    public class ChangeTransferCommandVehicleId : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeTransferCommandVehicleId";
    }

    [Activity("ACS.Transfer", "Clear TC Vehicle ID",
        "Clears the vehicle ID from the transport command.")]
    public class ChangeTransferCommandVehicleIdEmpty : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeTransferCommandVehicleIdEmpty";
    }

    [Activity("ACS.Transfer", "TC State → Pre-Assigned",
        "Changes transport command state to PRE_ASSIGNED.")]
    public class ChangeTransportCommandStateToPreAssigned : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeTransportCommandStateToPreAssigned";
    }

    [Activity("ACS.Transfer", "TC State → Assigned",
        "Changes transport command state to ASSIGNED and sets AssignedTime.")]
    public class ChangeTransportCommandStateToAssigned : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeTransportCommandStateToAssigned";
    }

    [Activity("ACS.Transfer", "TC State → Queued",
        "Changes transport command state to QUEUED.")]
    public class ChangeTransportCommandStateToQueued : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeTransportCommandStateToQueued";
    }

    [Activity("ACS.Transfer", "TC State → Transferring",
        "Changes transport command state to TRANSFERRING.")]
    public class ChangeTransportCommandStateToTransferring : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeTransportCommandStateToTransferring";
    }

    [Activity("ACS.Transfer", "TC State → Complete",
        "Changes transport command state to COMPLETE.")]
    public class ChangeTransportCommandStateToComplete : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeTransportCommandStateToComplete";
    }

    [Activity("ACS.Transfer", "Update Vehicle TC",
        "Updates vehicle record with current transport command info.")]
    public class UpdateVehicleTransportCommand : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "UpdateVehicleTransportCommand";
    }

    [Activity("ACS.Transfer", "Update Vehicle Dest Node",
        "Updates vehicle's ACS destination node ID from the transport command.")]
    public class UpdateVehicleAcsDestNodeId : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "UpdateVehicleAcsDestNodeId";
    }

    [Activity("ACS.Transfer", "Update TC Path",
        "Updates the path of the transport command.")]
    public class UpdateTransportCommandPath : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "UpdateTransportCommandPath";
    }

    [Activity("ACS.Transfer", "Update Vehicle Path",
        "Updates the vehicle's path.")]
    public class UpdateVehiclePath : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "UpdateVehiclePath";
    }

    [Activity("ACS.Transfer", "Update TC Vehicle Event",
        "Updates transport command with vehicle event info.")]
    public class UpdateTransportCommandVehicleEvent : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "UpdateTransportCommandVehicleEvent";
    }

    [Activity("ACS.Transfer", "Create TC Request",
        "Creates a transport command request for path assignment.")]
    public class CreateTransportCommandRequest : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CreateTransportCommandRequest";
    }

    [Activity("ACS.Transfer", "Check TC Request Replied",
        "Checks if the transport command request has been replied.")]
    public class IsTransportCommandRequestReplied : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "IsTransportCommandRequestReplied";
    }

    [Activity("ACS.Transfer", "Search Paths",
        "Searches for paths for the transfer message.")]
    public class SearchPaths : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "SearchPaths";
    }

    [Activity("ACS.Transfer", "Send Dest To 0000",
        "Updates vehicle destination node to 0000 (stop).")]
    public class UpdateVehicleAcsDestNodeIdTo0000 : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "UpdateVehicleAcsDestNodeIdTo0000";
    }

    [Activity("ACS.Transfer", "Dest → Wait Point",
        "Updates vehicle destination to the wait point.")]
    public class UpdateVehicleAcsDestNodeIdToWaitPoint : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.TransferServiceEx, ACS.Service";
        protected override string ServiceMethodName => "UpdateVehicleAcsDestNodeIdToWaitPoint";
    }
}
