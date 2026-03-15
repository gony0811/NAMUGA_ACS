using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Alarm;

public class AlarmSpec : TimedEntity
{
    public static string STATE_ALARM_SET = "SET";
    public static string STATE_ALARM_CLEARED = "CLEARED";
    public static string CODE_ALARMSET = "128";
    public static string SEVERITY_LIGHT = "LIGHT";
    public static string SEVERITY_HEAVY = "HEAVY";
    public static string SEVERITY_FATAL = "FATAL";

    public virtual string AlarmId { get; set; }
    public virtual string AlarmText { get; set; }
    public virtual string Severity { get; set; }
    public virtual string Description1 { get; set; }

    public AlarmSpec()
    {
        this.Id = Guid.NewGuid().ToString();
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("alarmSpec{");
        sb.Append(", alarmId=").Append(this.AlarmId);
        sb.Append(", alarmText=").Append(this.AlarmText);
        sb.Append(", severity=").Append(this.Severity);
        sb.Append(", description=").Append(this.Description1);
        sb.Append("}");
        return sb.ToString();
    }
}
