using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Capacity
{
    public class ResultCode : Entity
    {
        private string machineName = "";
        private string resultCode = "0";
        private string description = "";
        private string creator = "admin";
        private DateTime createTime = new DateTime();

        public ResultCode()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public ResultCode(string machineName, string resultCode)
        {
            this.machineName = machineName;
            this.resultCode = resultCode;

            this.Id = this.machineName + "-" + this.resultCode;
        }

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string ResultCodeName
        {
            get { return resultCode; }
            set { resultCode = value; }
        }

        public string Creator
        {
            get { return creator; }
            set { creator = value; }
        }

        public DateTime CreateTime
        {
            get { return createTime; }
            set { createTime = value; }
        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("permittedCapacityResultCode{");
            sb.Append("machineName=").Append(this.machineName);
            sb.Append(", resultCode=").Append(this.resultCode);
            sb.Append(", description=").Append(this.description);
            sb.Append(", creator=").Append(this.creator);
            sb.Append(", createTime=").Append(this.createTime);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
