using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Path;

public class InterSectionControl : Entity
{
    public static string CROSS_WAIT_INTERSECTION = "INTERS";

    public virtual string InterSectionId { get; set; }
    public virtual string CheckPreviousNode { get; set; }
    public virtual string StartNodeId { get; set; }
    public virtual string EndNodeId { get; set; }
    public virtual int Interval { get; set; }
    public virtual int Sequence { get; set; }
    public virtual string CheckNodeIds { get; set; }
    public virtual string PreviousNodeIds { get; set; }
}
