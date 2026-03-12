using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ACS.Application;
using System.Diagnostics;
using System.Reflection;

namespace ACS.StartUp
{
    static class ServerApplication
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            Console.WriteLine("[ACS] Starting ACS Server...");
            
            // .NET 8 compatibility: resolve types removed from mscorlib (e.g., CallContext)
            var compatAssembly = typeof(System.Runtime.Remoting.Messaging.CallContext).Assembly;
            Console.WriteLine($"[ACS] Compat assembly loaded: {compatAssembly.FullName}");
            AppDomain.CurrentDomain.TypeResolve += (sender, args) =>
            {
                if (args.Name != null && args.Name.Contains("CallContext"))
                {
                    return compatAssembly;
                }
                return null;
            };

            // .NET 8: resolve assemblies from app base directory (Spring.NET loads types dynamically)
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var asmName = new AssemblyName(args.Name);
                var dllPath = System.IO.Path.Combine(appDir, asmName.Name + ".dll");
                if (System.IO.File.Exists(dllPath))
                {
                    Console.WriteLine($"[ACS] AssemblyResolve: {asmName.Name} -> {dllPath}");
                    return Assembly.LoadFrom(dllPath);
                }
                return null;
            };

            // On .NET 8+, process name is 'dotnet' when run via dotnet CLI, so skip duplicate check
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
