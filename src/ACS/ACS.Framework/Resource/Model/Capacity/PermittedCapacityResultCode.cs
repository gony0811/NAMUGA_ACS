using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model.Capacity
{
    public class PermittedCapacityResultCode : Entity
    {
        private string permittedCapacityId = "";
        private string resultCodeId = "";
        private string creator = "admin";
        private DateTime createTime = new DateTime();

        public PermittedCapacityResultCode()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public string PermittedCapacityId
        {
            get { return permittedCapacityId; }
            set { permittedCapacityId = value; }
        }

        public string ResultCodeId
        {
            get { return resultCodeId; }
            set { resultCodeId = value; }
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
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("permittedCapacityResultCode{");
            sb.Append("permittedCapacityId=").Append(this.permittedCapacityId);
            sb.Append(", resultCodeId=").Append(this.resultCodeId);
            sb.Append(", creator=").Append(this.creator);
            sb.Append(", createTime=").Append(this.createTime);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
