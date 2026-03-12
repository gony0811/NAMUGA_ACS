using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.History.Model
{
    public class NioHistory : PartitionedEntity
    {
        public NioHistory()
        {
            this.PartitionId = this.CreatePartitionIdByDate();
        }

        public virtual String Name { get; set; }

        public virtual String State { get; set; }

        public virtual String MachineName { get; set; }

        public virtual String RemoteIp { get; set; }

        public virtual int Port { get; set; }

        public virtual String ApplicationName { get; set; }

        public virtual String Location { get; set; }

        public override String ToString()
        {
            string port = string.Format("{0}", Port);
            return "NioHistory [Id=" + Id + ", Name="
                    + Name + ", State=" + State + ", MachineName=" + MachineName + ", RemoteIp=" + RemoteIp + ", Port=" + port + ", ApplicationName=" + ApplicationName + ", Location=" + Location
                    + "]";
        }
    }
}
