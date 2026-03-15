using System.Text;

namespace ACS.Core.Database.Model.History;

public class AlarmReportHistory : AbstractHistory
{
    public AlarmReportHistory()
    {
        this.PartitionId = base.CreatePartitionIdByDate();
    }

    public virtual string VehicleId { get; set; }
    public virtual string AlarmId { get; set; }
    public virtual string AlarmCode { get; set; }
    public virtual string AlarmText { get; set; }
    public virtual string State { get; set; }
    public virtual string TransportCommandId { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("alarmReportHistory{");
        sb.Append("id=").Append(this.Id);
        sb.Append(", partitionId=").Append(this.PartitionId);
        sb.Append(", vehicleId=").Append(this.VehicleId);
        sb.Append(", alarmId=").Append(this.AlarmId);
        sb.Append(", alarmCode=").Append(this.AlarmCode);
        sb.Append(", alarmText=").Append(this.AlarmText);
        sb.Append(", state=").Append(this.State);
        sb.Append(", time=").Append(this.Time);
        sb.Append("}");
        return sb.ToString();
    }
}
