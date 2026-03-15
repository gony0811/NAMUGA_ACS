using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace ACS.App;

static class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("[ACS] Starting ACS Server...");

        var appDir = AppDomain.CurrentDomain.BaseDirectory;

        // Dynamic assembly resolution
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            var asmName = new AssemblyName(resolveArgs.Name);
            var dllPath = System.IO.Path.Combine(appDir, asmName.Name + ".dll");
            if (System.IO.File.Exists(dllPath))
                return Assembly.LoadFrom(dllPath);
            return null;
        };

        // Duplicate process detection
        var currentProcess = Process.GetCurrentProcess();
        var processName = currentProcess.ProcessName;
        Console.WriteLine($"[ACS] Process: {processName} (PID: {currentProcess.Id})");
        if (!processName.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            && Process.GetProcessesByName(processName).Length > 1)
        {
            Console.WriteLine("[ACS] Duplicate process detected. Exiting.");
            return;
        }

        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(appDir)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Initialize Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                System.IO.Path.Combine(appDir, "logs", "acs-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Executor executor = null;
        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            executor = new Executor();
            var container = executor.Start();

            Log.Information("[ACS] Server started. Press Ctrl+C to stop.");
            cts.Token.WaitHandle.WaitOne();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[ACS] Server terminated unexpectedly");
        }
        finally
        {
            try { executor?.Stop(); } catch { }
            Log.CloseAndFlush();
        }
    }
}
