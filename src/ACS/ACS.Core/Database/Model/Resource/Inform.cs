using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Resource;

public class Inform : Entity
{
    public static string INFORM_TYPE_EMERGENCY = "EMERGENCY";
    public static string INFORM_TYPE_NOTICE = "NOTICE";
    public static string INFORM_TYPE_IMPORTANT = "IMPORTANT";

    public virtual DateTime Time { get; set; }
    public virtual string Type { get; set; }
    public virtual string Message { get; set; }
    public virtual string Source { get; set; }
    public virtual string Description { get; set; }
}
