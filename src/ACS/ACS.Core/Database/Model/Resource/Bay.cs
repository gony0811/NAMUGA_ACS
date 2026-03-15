using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Resource;

public class Bay : Entity
{
    public virtual int Floor { get; set; }
    public virtual string Description { get; set; }
    public virtual string AgvType { get; set; }
    public virtual float ChargeVoltage { get; set; }
    public virtual float LimitVoltage { get; set; }
    public virtual int IdleTime { get; set; }
    public virtual string ZoneMove { get; set; }
    public virtual string Traffic { get; set; }
    public virtual string StopOut { get; set; }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("bay{");
        sb.Append("id=").Append(this.Id);
        sb.Append(", description=").Append(this.Description);
        sb.Append(", floor=").Append(this.Floor);
        sb.Append(", agvType=").Append(this.AgvType);
        sb.Append(", chargeVoltage=").Append(this.ChargeVoltage);
        sb.Append(", limitVoltage=").Append(this.LimitVoltage);
        sb.Append(", idleTime=").Append(this.IdleTime);
        sb.Append("}");
        return sb.ToString();
    }
}
