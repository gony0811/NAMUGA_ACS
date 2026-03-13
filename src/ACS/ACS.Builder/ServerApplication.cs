using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using ACS.Application;
using Serilog;

namespace ACS.Builder
{
    static class ServerApplication
    {
        /// <summary>
        /// The main entry point for the application.
        /// Spring.NET 의존성이 완전히 제거된 Autofac 기반 진입점.
        /// </summary>
        static void Main()
        {
            Console.WriteLine("[ACS] Starting ACS Server...");

            // .NET 8: 앱 기본 디렉토리에서 어셈블리 해결 (동적 DLL 로딩용)
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

            // Serilog 글로벌 로거 초기화
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    System.IO.Path.Combine(appDir, "logs", "acs-.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Console.WriteLine("[ACS] Initializing Executor...");
                var executor = new Executor();
                Console.WriteLine("[ACS] Executor created. Starting...");
                executor.Start();
                Console.WriteLine("[ACS] Server started successfully. Press Ctrl+C to stop.");
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ACS] Error: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
