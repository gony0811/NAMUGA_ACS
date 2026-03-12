using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ACS.Application;
using System.Diagnostics;
using System.Reflection;
using Spring.Threading;

namespace ACS.StartUp
{
    /// <summary>
    /// AsyncLocal 기반 IThreadStorage 구현.
    /// .NET 8에서 제거된 CallContext를 대체하여 Spring.NET의 스레드 저장소로 사용.
    /// </summary>
    internal class AsyncLocalThreadStorage : IThreadStorage
    {
        private static readonly ConcurrentDictionary<string, AsyncLocal<object>> _store
            = new ConcurrentDictionary<string, AsyncLocal<object>>();

        public object GetData(string name)
        {
            return _store.TryGetValue(name, out var slot) ? slot.Value : null;
        }

        public void SetData(string name, object value)
        {
            _store.GetOrAdd(name, _ => new AsyncLocal<object>()).Value = value;
        }

        public void FreeNamedDataSlot(string name)
        {
            if (_store.TryRemove(name, out var slot))
            {
                slot.Value = null;
            }
        }
    }

    static class ServerApplication
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            Console.WriteLine("[ACS] Starting ACS Server...");

            // .NET 8 호환: Spring.NET의 CallContextStorage를 AsyncLocal 기반으로 교체
            LogicalThreadContext.SetStorage(new AsyncLocalThreadStorage());
            Console.WriteLine("[ACS] ThreadStorage initialized (AsyncLocal)");

            // .NET 8 호환: mscorlib에서 제거된 타입 해결 (Remoting, CallContext 등)
            var compatAssembly = typeof(System.Runtime.Remoting.Messaging.CallContext).Assembly;
            Console.WriteLine($"[ACS] Compat assembly loaded: {compatAssembly.FullName}");
            AppDomain.CurrentDomain.TypeResolve += (sender, args) =>
            {
                if (args.Name != null && (
                    args.Name.Contains("CallContext") ||
                    args.Name.Contains("System.Runtime.Remoting")))
                {
                    return compatAssembly;
                }
                return null;
            };

            // .NET 8: 앱 기본 디렉토리에서 어셈블리 해결 (Spring.NET 동적 타입 로딩)
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var asmName = new AssemblyName(args.Name);
                var dllPath = System.IO.Path.Combine(appDir, asmName.Name + ".dll");
                if (System.IO.File.Exists(dllPath))
                {
                    return Assembly.LoadFrom(dllPath);
                }
                return null;
            };

            // .NET 8+에서 dotnet CLI로 실행 시 프로세스 이름이 'dotnet'이므로 중복 검사 스킵
            var currentProcess = Process.GetCurrentProcess();
            var processName = currentProcess.ProcessName;
            Console.WriteLine($"[ACS] Process: {processName} (PID: {currentProcess.Id})");
            if (!processName.Equals("dotnet", StringComparison.OrdinalIgnoreCase) 
                && Process.GetProcessesByName(processName).Length > 1)
            {
                Console.WriteLine("[ACS] Duplicate process detected. Exiting.");
                return;
            }

            try
            {
                Console.WriteLine("[ACS] Initializing Executor...");
                var executor = new Executor();
                Console.WriteLine("[ACS] Executor created. Starting...");
                executor.Start();
                Console.WriteLine("[ACS] Server started successfully. Press Ctrl+C to stop.");
                Thread.Sleep(System.Threading.Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ACS] Error: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
