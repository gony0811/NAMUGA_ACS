using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Application.Model
{
    public class SystemUsage : Entity
    {
        private string hardwareType;
        private string hardwareAddress;
        private string cpuUsage;
        private string memoryUsage;
        private string diskUsage;
        private string networkUsage;
        private DateTime? time = new DateTime();

        public string HardwareType { get { return hardwareType; } set { hardwareType = value; } }
        public string HardwareAddress { get { return hardwareAddress; } set { hardwareAddress = value; } }
        public string CpuUsage { get { return cpuUsage; } set { cpuUsage = value; } }
        public string MemoryUsage { get { return memoryUsage; } set { memoryUsage = value; } }
        public string DiskUsage { get { return diskUsage; } set { diskUsage = value; } }
        public string NetworkUsage { get { return networkUsage; } set { networkUsage = value; } }
        protected DateTime? Time { get { return time; } set { time = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("systemUsage{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", hardwareType=").Append(this.hardwareType);
            sb.Append(", hardwareAddress=").Append(this.hardwareAddress);
            sb.Append(", cpuUsage=").Append(this.cpuUsage);
            sb.Append(", memoryUsage=").Append(this.memoryUsage);
            sb.Append(", diskUsage=").Append(this.diskUsage);
            sb.Append(", networkUsage=").Append(this.networkUsage);
            sb.Append(", time=").Append(this.time);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
