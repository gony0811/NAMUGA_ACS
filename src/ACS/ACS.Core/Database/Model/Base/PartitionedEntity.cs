namespace ACS.Core.Database.Model.Base;

public class PartitionedEntity : Entity
{
    public PartitionedEntity()
    {
        Id = Guid.NewGuid().ToString();
    }

    public virtual int PartitionId { get; set; }

    public virtual DateTime? Time { get; set; } = new DateTime(DateTime.UtcNow.Ticks);

    public virtual int CreatePartitionIdByMonth()
    {
        DateTime dt = DateTime.Now;
        return dt.Month;
    }

    public virtual int CreatePartitionIdByDate()
    {
        DateTime dt = DateTime.Now;
        return dt.Day;
    }
}
