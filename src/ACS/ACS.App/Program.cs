using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Xml;
using Autofac;
using Microsoft.Extensions.Configuration;
using ACS.Core.Host;
using Serilog;

namespace ACS.App;

static class Program
{
    static void Main(string[] args)
    {
        // Npgsql 6+ DateTime UTC 제약 해제 — 기존 DateTime.Now(Local) 사용 코드와 호환
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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

        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(appDir)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Named Mutex 기반 중복 프로세스 검출
        var processId = configuration["Acs:Process:Name"];
        if (string.IsNullOrEmpty(processId))
        {
            Console.WriteLine("[ACS] Acs:Process:Name is not configured. Exiting.");
            return;
        }

        using var mutex = new Mutex(true, $"Global\\ACS_{processId}", out bool createdNew);
        if (!createdNew)
        {
            Console.WriteLine($"[ACS] Process '{processId}' is already running. Exiting.");
            return;
        }

        Console.Title = processId;
        Console.WriteLine($"[ACS] Process: {processId} (PID: {Process.GetCurrentProcess().Id})");

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

            if (executor.Type == "host")
            {
                if (Console.IsInputRedirected)
                {
                    Log.Information("[ACS] Host server started (non-interactive mode). Press Ctrl+C to stop.");
                    cts.Token.WaitHandle.WaitOne();
                }
                else
                {
                    RunHostConsoleMenu(container, executor.Id, cts);
                }
            }
            else
            {
                Log.Information("[ACS] Server started. Press Ctrl+C to stop.");
                cts.Token.WaitHandle.WaitOne();
            }
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

    /// <summary>
    /// Host 프로세스 전용 콘솔 메뉴.
    /// JOBREPORT (RECEIVE/START/ARRIVED/COMPLETED)를 직접 송신할 수 있다.
    /// </summary>
    static void RunHostConsoleMenu(IContainer container, string processId, CancellationTokenSource cts)
    {
        var hostMessageService = container.Resolve<IHostMessageService>();

        Log.Information("[ACS] Host 프로세스 시작됨. 콘솔 메뉴를 표시합니다.");

        string lastJobId = "";

        while (!cts.Token.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.WriteLine($"[{processId}] Host 프로세스 콘솔 메뉴");
            Console.WriteLine("==========================================");
            Console.WriteLine("  1. JOBREPORT - RECEIVE   송신");
            Console.WriteLine("  2. JOBREPORT - START     송신");
            Console.WriteLine("  3. JOBREPORT - ARRIVED   송신");
            Console.WriteLine("  4. JOBREPORT - COMPLETED 송신");
            Console.WriteLine("  Q. 종료");
            Console.WriteLine("==========================================");
            Console.Write("선택> ");

            string input = null;
            try
            {
                input = Console.ReadLine();
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (input == null || cts.Token.IsCancellationRequested)
                break;

            input = input.Trim().ToUpperInvariant();

            if (input == "Q")
            {
                cts.Cancel();
                break;
            }

            string reportType = input switch
            {
                "1" => "RECEIVE",
                "2" => "START",
                "3" => "ARRIVED",
                "4" => "COMPLETED",
                _ => null
            };

            if (reportType == null)
            {
                Console.WriteLine("  잘못된 입력입니다. 1~4 또는 Q를 입력하세요.");
                continue;
            }

            // JobID 입력
            string defaultJobId = string.IsNullOrEmpty(lastJobId)
                ? $"JOB{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(100, 999):D3}"
                : lastJobId;

            Console.Write($"  JobID [{defaultJobId}]> ");
            string jobIdInput = Console.ReadLine()?.Trim();
            string jobId = string.IsNullOrEmpty(jobIdInput) ? defaultJobId : jobIdInput;
            lastJobId = jobId;

            try
            {
                var doc = hostMessageService.BuildJobReport(reportType, jobId);
                hostMessageService.SendToHost("JOBREPORT", doc);

                // 송신 XML 원문 표시
                var sw = new System.IO.StringWriter();
                var xw = new XmlTextWriter(sw) { Formatting = Formatting.Indented };
                doc.WriteTo(xw);
                xw.Flush();

                Console.WriteLine();
                Console.WriteLine($"  >> JOBREPORT({reportType}) 송신 완료");
                Console.WriteLine("  ---- SENT XML ----");
                Console.WriteLine(sw.ToString());
                Console.WriteLine("  ---- END XML ----");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  >> 송신 실패: {ex.Message}");
                Log.Error(ex, "[HostConsole] JOBREPORT 송신 오류");
            }
        }

        Console.WriteLine("[ACS] Host 콘솔 메뉴 종료.");
    }
}
