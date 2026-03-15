namespace ACS.Core.Database.Model.Base;

public class TimedEntity : Entity
{
    public virtual string Description { get; set; }
    public virtual DateTime? CreateTime { get; set; }
    public virtual DateTime? EditTime { get; set; }
    public virtual string Creator { get; set; }
    public virtual string Editor { get; set; }

    public TimedEntity()
    {
        Creator = "admin";
        Editor = "admin";
        CreateTime = new DateTime();
        EditTime = new DateTime();
    }
}
