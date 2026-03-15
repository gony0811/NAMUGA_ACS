using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Transfer;

public class UiCommand : NamedEntity
{
    public virtual string ID { get; set; }
    public virtual string MESSAGENAME { get; set; }
    public virtual string APPLICATIONNAME { get; set; }
    public virtual string APPLICATIONTYPE { get; set; }
    public virtual string USERID { get; set; }
    public virtual string CAUSE { get; set; }
    public virtual string DESCRIPTION { get; set; }
    public virtual DateTime? TIME { get; set; }

    public UiCommand()
    {
        TIME = DateTime.Now;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("uinio{");
        sb.Append("ID=").Append(this.ID);
        sb.Append(", MESSAGENAME=").Append(this.MESSAGENAME);
        sb.Append(", APPLICATIONNAME=").Append(this.APPLICATIONNAME);
        sb.Append(", APPLICATIONTYPE=").Append(this.APPLICATIONTYPE);
        sb.Append(", USERID=").Append(this.USERID);
        sb.Append(", CAUSE=").Append(this.CAUSE);
        sb.Append(", DESCRIPTION=").Append(this.DESCRIPTION);
        sb.Append(", TIME=").Append(this.TIME);
        sb.Append("}");
        return sb.ToString();
    }
}
