using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.History;

public class NioHistory : PartitionedEntity
{
    public NioHistory()
    {
        this.PartitionId = this.CreatePartitionIdByDate();
    }

    public virtual string Name { get; set; }
    public virtual string State { get; set; }
    public virtual string MachineName { get; set; }
    public virtual string RemoteIp { get; set; }
    public virtual int Port { get; set; }
    public virtual string ApplicationName { get; set; }
    public virtual string Location { get; set; }

    public override string ToString()
    {
        string port = string.Format("{0}", Port);
        return "NioHistory [Id=" + Id + ", Name="
                + Name + ", State=" + State + ", MachineName=" + MachineName + ", RemoteIp=" + RemoteIp + ", Port=" + port + ", ApplicationName=" + ApplicationName + ", Location=" + Location
                + "]";
    }
}
