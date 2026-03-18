
using Elsa.Workflows.Attributes;

namespace ACS.Elsa.Activities
{
    // ═══════════════════════════════════════════════════════════════
    //  InterfaceServiceEx Activities
    //  Category: ACS.Interface
    // ═══════════════════════════════════════════════════════════════

    [Activity("ACS.Interface", "Send Transport Dest",
        "Sends transport command destination message to the vehicle/ES.")]
    public class SendTransportMessageDest : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "SendTransportMessageDest";
    }

    [Activity("ACS.Interface", "Send Transport Source",
        "Sends transport command source message to the vehicle.")]
    public class SendTransportMessageSource : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "SendTransportMessageSource";
    }

    [Activity("ACS.Interface", "Send Go Waitpoint",
        "Sends go-to-waitpoint command to the vehicle.")]
    public class SendGoWaitpoint : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "SendGoWaitpoint";
    }

    [Activity("ACS.Interface", "Send Go 0000",
        "Sends go-to-0000 (stop) command to the vehicle.")]
    public class SendGoWaitpoint0000 : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "SendGoWaitpoint0000";
    }

    [Activity("ACS.Interface", "Reply MoveCmd",
        "Sends reply to a MOVECMD request.")]
    public class ReplyMoveCmd : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ReplyMoveCmd";
    }

    [Activity("ACS.Interface", "Report AGV Location",
        "Reports AGV location to the host system.")]
    public class ReportAGVLocation : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ReportAGVLocation";
    }

    [Activity("ACS.Interface", "Report AGV State",
        "Reports AGV state to the host system.")]
    public class ReportAGVState : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ReportAGVState";
    }

    [Activity("ACS.Interface", "Report Alarm",
        "Reports an alarm event to the host system.")]
    public class ReportAlarmReport : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ReportAlarmReport";
    }

    [Activity("ACS.Interface", "Report Alarm Clear",
        "Reports alarm clear to the host system.")]
    public class ReportAlarmClearReport : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ReportAlarmClearReport";
    }

    [Activity("ACS.Interface", "Check Transfer Message",
        "Validates a transfer message.")]
    public class CheckTransferMessage : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CheckTransferMessage";
    }

    [Activity("ACS.Interface", "Check TC Source/Dest",
        "Checks transfer command source and destination validity.")]
    public class CheckTransferCommandSourceDest : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CheckTransferCommandSourceDest";
    }

    [Activity("ACS.Interface", "Send Vehicle Permit",
        "Sends permission message to the vehicle.")]
    public class SendVehiclePermitMessage : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "SendVehiclePermitMessage";
    }

    [Activity("ACS.Interface", "Request AGV Job Cancel",
        "Requests cancellation of an AGV job.")]
    public class RequestAGVJobCancel : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "requestAGVJobCancel";
    }

    [Activity("ACS.Interface", "Report Load Complete",
        "Reports AGV load complete to the host.")]
    public class ReportAGVloadComplete : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ReportAGVloadComplete";
    }

    [Activity("ACS.Interface", "Report Unload Complete",
        "Reports AGV unload complete to the host.")]
    public class ReportAGVUnloadComplete : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.InterfaceServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ReportAGVUnloadComplete";
    }
}
