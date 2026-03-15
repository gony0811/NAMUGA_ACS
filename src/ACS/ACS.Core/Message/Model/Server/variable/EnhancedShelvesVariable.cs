using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server.variable
{
    public class EnhancedShelvesVariable
    {
        private string shelfName;
        private string shelfState;
        private string shelfStatus;

        public string ShelfName { get { return shelfName; } set { shelfName = value; } }
        public string ShelfState { get { return shelfState; } set { shelfState = value; } }
        public string ShelfStatus { get { return shelfStatus; } set { shelfStatus = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("enhancedShelvesVariable{");
            sb.Append("shelfName=").Append(this.shelfName);
            sb.Append(", shelfState=").Append(this.shelfState);
            sb.Append(", shelfStatus=").Append(this.shelfStatus);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
