using ACS.Elsa.Activities;
using Elsa.Workflows.Attributes;

namespace ACS.Activity.Activities
{
    // ═══════════════════════════════════════════════════════════════
    //  AlarmServiceEx Activities
    //  Category: ACS.Alarm
    // ═══════════════════════════════════════════════════════════════

    [Activity("ACS.Alarm", "Create Alarm",
        "Creates an alarm record for the vehicle.")]
    public class CreateAlarm : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.AlarmServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CreateAlarm";
    }

    [Activity("ACS.Alarm", "Check Vehicle Has Alarm",
        "Checks if the vehicle currently has an active alarm.")]
    public class CheckVehicleHaveAlarm : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.AlarmServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CheckVehicleHaveAlarm";
    }

    [Activity("ACS.Alarm", "Check Alarm Clear Msg",
        "Checks if the message is an alarm clear message.")]
    public class CheckAlarmClearMessage : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.AlarmServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CheckAlarmClearMessage";
    }

    [Activity("ACS.Alarm", "Clear Alarm + History",
        "Clears the alarm and sets alarm time in history.")]
    public class ClearAlarmAndSetAlarmTimeHistory : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.AlarmServiceEx, ACS.Service";
        protected override string ServiceMethodName => "ClearAlarmAndSetAlarmTimeHistory";
    }

    [Activity("ACS.Alarm", "Check Heavy Alarm",
        "Checks if the alarm is a heavy (critical) alarm.")]
    public class CheckHeavyAlarm : ReflectionActivityBase
    {
        protected override string ServiceTypeAssemblyName => "ACS.Service.AlarmServiceEx, ACS.Service";
        protected override string ServiceMethodName => "CheckHeavyAlarm";
    }
}
