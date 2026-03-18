
using Elsa.Workflows.Attributes;

namespace ACS.Elsa.Activities
{
    // ═══════════════════════════════════════════════════════════════
    //  MaterialServiceEx Activities
    //  Category: ACS.Material
    // ═══════════════════════════════════════════════════════════════

    [Activity("ACS.Material", "Create Carrier",
        "Creates a carrier record.")]
    public class CreateCarrier : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.MaterialServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CreateCarrier";
    }

    [Activity("ACS.Material", "Carrier → Vehicle",
        "Changes carrier location to the vehicle.")]
    public class ChangeCarrierLocationToVehicle : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.MaterialServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeCarrierLocationToVehicle";
    }

    [Activity("ACS.Material", "Carrier → Dest Port",
        "Changes carrier location to the destination port.")]
    public class ChangeCarrierLocationToDest : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.MaterialServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ChangeCarrierLocationToDest";
    }

    [Activity("ACS.Material", "Delete Carrier",
        "Deletes a carrier record.")]
    public class DeleteCarrier : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.MaterialServiceEx, ACS.Service";
        protected override string ServiceMethodName => "DeleteCarrier";
    }
}
