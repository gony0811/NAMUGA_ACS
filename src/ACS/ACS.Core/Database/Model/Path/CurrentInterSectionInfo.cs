using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Path;

public class CurrentInterSectionInfo : Entity
{
    public static string STATE_CHANGED = "CHANGED";
    public static string STATE_CHANGING = "CHANGING";

    public virtual string CurrentDirectionNode { get; set; }
    public virtual DateTime? ChangedTime { get; set; }
    public virtual string State { get; set; }

    public override string ToString()
    {
        return Id + ":" + CurrentDirectionNode + ":" + ChangedTime + ":" + State;
    }
}
