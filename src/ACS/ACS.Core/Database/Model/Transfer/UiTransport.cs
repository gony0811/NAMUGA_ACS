using System.Text;
using ACS.Core.Database.Model.Base;

namespace ACS.Core.Database.Model.Transfer;

public class UiTransport : Entity
{
    public virtual string MESSAGENAME { get; set; }
    public virtual string TRANSPORTCOMMANDID { get; set; }
    public virtual string SOURCEPORTID { get; set; }
    public virtual string DESTPORTID { get; set; }
    public virtual string VEHICLEID { get; set; }
    public virtual string DESTNODEID { get; set; }
    public virtual string REQUESTID { get; set; }
    public virtual string USERID { get; set; }
    public virtual string CAUSE { get; set; }
    public virtual string DESCRIPTION { get; set; }
    public virtual DateTime? TIME { get; set; }

    public UiTransport()
    {
        TIME = DateTime.Now;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("uitransport{");
        sb.Append("ID=").Append(this.Id);
        sb.Append(", MESSAGENAME=").Append(this.MESSAGENAME);
        sb.Append(", TRANSPORTCOMMANDID=").Append(this.TRANSPORTCOMMANDID);
        sb.Append(", SOURCEPORTID=").Append(this.SOURCEPORTID);
        sb.Append(", DESTPORTID=").Append(this.DESTPORTID);
        sb.Append(", VEHICLEID=").Append(this.VEHICLEID);
        sb.Append(", DESTNODEID=").Append(this.DESTNODEID);
        sb.Append(", REQUESTID=").Append(this.REQUESTID);
        sb.Append(", USERID=").Append(this.USERID);
        sb.Append(", CAUSE=").Append(this.CAUSE);
        sb.Append(", DESCRIPTION=").Append(this.DESCRIPTION);
        sb.Append(", TIME=").Append(this.TIME);
        sb.Append("}");
        return sb.ToString();
    }
}
