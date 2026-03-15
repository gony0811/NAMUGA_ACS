using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Logging;

public class LogMessage : PartitionedEntity
{
    public virtual string TransactionId { get; set; }
    public virtual string ThreadName { get; set; }
    public virtual string OperationName { get; set; }
    public virtual string ProcessName { get; set; }
    public virtual string MessageName { get; set; }
    public virtual string CommunicationMessageName { get; set; }
    public virtual string TransportCommandId { get; set; }
    public virtual string CarrierName { get; set; }
    public virtual string MachineName { get; set; }
    public virtual string UnitName { get; set; }
    public virtual string Text { get; set; }
    public virtual string LogLevel { get; set; }
    public virtual bool WorkflowLog { get; set; }
    public virtual bool SaveToDatabase { get; set; }

    public LogMessage()
    {
        this.PartitionId = base.CreatePartitionIdByDate();
        SaveToDatabase = true;
    }

    public LogMessage(int partitionId)
    {
        this.PartitionId = partitionId;
        SaveToDatabase = true;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(this.Text);
        sb.Append(", messageName=").Append(this.MessageName);
        sb.Append(", communicationMessageNam=").Append(this.CommunicationMessageName);
        if (!this.WorkflowLog)
        {
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", machineName=").Append(this.MachineName);
            sb.Append(", unitName=").Append(this.UnitName);
        }
        return sb.ToString();
    }
}
