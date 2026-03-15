using System.Text;

namespace ACS.Core.Database.Model.History;

public class VehicleBatteryHistory : AbstractHistory
{
    public virtual string VehicleId { get; set; }
    public virtual int BatteryRate { get; set; }
    public virtual float BatteryVoltage { get; set; }
    public virtual string ProcessingState { get; set; }

    public VehicleBatteryHistory()
    {
        this.PartitionId = CreatePartitionIdByDate();
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("vehicleHistory{");
        sb.Append("id=").Append(this.Id);
        sb.Append(", vehicleId=").Append(this.VehicleId);
        sb.Append(", runState=").Append(this.BatteryRate);
        sb.Append(", fullState=").Append(this.BatteryVoltage);
        sb.Append(", processingState=").Append(this.ProcessingState);
        sb.Append(", time=").Append(this.Time);
        sb.Append("}");
        return sb.ToString();
    }
}
