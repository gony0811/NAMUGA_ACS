using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.History.Model
{
    public class ProcessUsageHistory: AbstractHistory
    {
        private string applicationName;
        private string processId;
        private string cpuUsage;
        private string memoryUsage;
        private string networkUsage;
        private string threadCount;

        public string ApplicationName { get {return applicationName; } set { applicationName = value; } }
        public string ProcessId { get {return processId; } set { processId = value; } }
        public string CpuUsage { get {return cpuUsage; } set { cpuUsage = value; } }
        public string MemoryUsage { get {return memoryUsage; } set { memoryUsage = value; } }
        public string NetworkUsage { get {return networkUsage; } set { networkUsage = value; } }
        public string ThreadCount { get { return threadCount; } set { threadCount = value; } }
    }
}
