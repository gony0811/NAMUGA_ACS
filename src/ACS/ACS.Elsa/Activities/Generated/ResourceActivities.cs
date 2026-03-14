using Elsa.Workflows.Attributes;

namespace ACS.Elsa.Activities.Generated
{
    // ═══════════════════════════════════════════════════════════════
    //  ResourceServiceEx Activities
    //  Category: ACS.Resource
    // ═══════════════════════════════════════════════════════════════

    [Activity("ACS.Resource", "Search Recharge Station",
        "Searches for a suitable recharge station for the vehicle based on its bay.")]
    public class SearchSuitableRechargeStation : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "SearchSuitableRechargeStation";
    }

    [Activity("ACS.Resource", "Search Suitable Vehicle",
        "Searches for a suitable idle vehicle to assign to a transport command.")]
    public class SearchSuitableVehicle : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "SearchSuitableVehicle";
    }

    [Activity("ACS.Resource", "Search Wait Point",
        "Searches for an available wait point for the vehicle.")]
    public class SearchWaitPoint : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "SearchWaitPoint";
    }

    [Activity("ACS.Resource", "Search Stock Station",
        "Searches for a stock station for the vehicle.")]
    public class SearchStockStation : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "SearchStockStation";
    }

    [Activity("ACS.Resource", "Get Queued TC by Bay",
        "Gets a queued transport command by bay ID.")]
    public class GetQueuedTransportCommandbyBayId : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "GetQueuedTransportCommandbyBayId";
    }

    [Activity("ACS.Resource", "Check Vehicle",
        "Checks if the vehicle exists and is valid.")]
    public class CheckVehicle_Resource : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CheckVehicle";
    }

    [Activity("ACS.Resource", "Change Vehicle TC ID",
        "Updates the vehicle record with the transport command ID.")]
    public class ChangeVehicleTransportCommandId : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeVehicleTransportCommandId";
    }

    [Activity("ACS.Resource", "Clear Vehicle TC ID",
        "Clears the vehicle's transport command ID (sets to empty).")]
    public class ChangeVehicleTransportCommandIdEmpty : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeVehicleTransportCommandIdEmpty";
    }

    [Activity("ACS.Resource", "Vehicle State → Run",
        "Changes vehicle process state to RUN.")]
    public class ChangeVehicleProcessStateToRun : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeVehicleProcessStateToRun";
    }

    [Activity("ACS.Resource", "Vehicle State → Idle",
        "Changes vehicle process state to IDLE.")]
    public class ChangeVehicleProcessStateToIdle : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeVehicleProcessStateToIdle";
    }

    [Activity("ACS.Resource", "Vehicle State → Charge",
        "Changes vehicle process state to CHARGE.")]
    public class ChangeVehicleProcessStateToCharge : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeVehicleProcessStateToCharge";
    }

    [Activity("ACS.Resource", "Vehicle Connect",
        "Changes vehicle connection state to CONNECTED.")]
    public class ChangeVehicleConnectionStateToConnect : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeVehicleConnectionStateToConnect";
    }

    [Activity("ACS.Resource", "Vehicle Disconnect",
        "Changes vehicle connection state to DISCONNECTED.")]
    public class ChangeVehicleConnectionStateToDisconnect : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeVehicleConnectionStateToDisconnect";
    }

    [Activity("ACS.Resource", "Vehicle Transfer → Assigned",
        "Changes vehicle transfer state to ASSIGNED.")]
    public class ChangeVehicleTransferStateToAssigned : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeVehicleTransferStateToAssigned";
    }

    [Activity("ACS.Resource", "Vehicle Transfer → Not Assigned",
        "Changes vehicle transfer state to NOT_ASSIGNED.")]
    public class ChangeVehicleTransferStateToNotAssigned : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeVehicleTransferStateToNotAssigned";
    }

    [Activity("ACS.Resource", "Vehicle Transfer → Acquire Complete",
        "Changes vehicle transfer state to ACQUIRE_COMPLETE.")]
    public class ChangeVehicleTransferStateToAcquireComplete : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeVehicleTransferStateToAcquireComplete";
    }

    [Activity("ACS.Resource", "Vehicle Transfer → Deposit Complete",
        "Changes vehicle transfer state to DEPOSIT_COMPLETE.")]
    public class ChangeVehicleTransferStateToDepositComplete : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeVehicleTransferStateToDepositComplete";
    }

    [Activity("ACS.Resource", "Update Vehicle Event Time",
        "Updates the vehicle's last event timestamp.")]
    public class UpdateVehicleEventTime : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "UpdateVehicleEventTime";
    }

    [Activity("ACS.Resource", "Create Vehicle History",
        "Creates a vehicle history record.")]
    public class CreateVehicleHistory : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CreateVehicleHistory";
    }

    [Activity("ACS.Resource", "Delete UI Inform",
        "Deletes UI inform records for the vehicle.")]
    public class DeleteUIInform : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "DeleteUIInform";
    }

    [Activity("ACS.Resource", "Check Battery Voltage",
        "Checks if vehicle battery voltage is above threshold.")]
    public class CheckVehicleBatteryVoltage : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CheckVehicleBatteryVoltage";
    }

    [Activity("ACS.Resource", "Change Vehicle Location",
        "Updates vehicle's current node location.")]
    public class ChangeVehicleLocation : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.ResourceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeVehicleLocation";
    }
}
