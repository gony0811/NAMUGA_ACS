using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Xml;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ACS.App.Web.Hubs;
using ACS.Core.Host;
using Serilog;

namespace ACS.App;

static class Program
{
    static int Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        Console.WriteLine("[ACS] Starting ACS Server...");

        var appDir = AppDomain.CurrentDomain.BaseDirectory;

        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            var asmName = new AssemblyName(resolveArgs.Name);
            var dllPath = System.IO.Path.Combine(appDir, asmName.Name + ".dll");
            if (System.IO.File.Exists(dllPath))
                return Assembly.LoadFrom(dllPath);
            return null;
        };

        var configuration = new ConfigurationBuilder()
            .SetBasePath(appDir)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var processId = configuration["Acs:Process:Name"];
        if (string.IsNullOrEmpty(processId))
        {
            Console.WriteLine("[ACS] Acs:Process:Name is not configured. Exiting.");
            return 1;
        }

        using var mutex = new Mutex(true, $"Global\\ACS_{processId}", out bool createdNew);
        if (!createdNew)
        {
            Console.WriteLine($"[ACS] Process '{processId}' is already running. Exiting.");
            return 1;
        }

        Console.Title = processId;
        Console.WriteLine($"[ACS] Process: {processId} (PID: {Process.GetCurrentProcess().Id})");

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.File(
                path: $"logs/{processId}-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var processType = configuration["Acs:Process:Type"];
        try
        {
            // control 프로세스가 UI 백엔드(REST API + SignalR)를 겸하므로 웹 호스트로 기동한다.
            // (기존 ui 프로세스는 폐지되어 control로 통합됨.)
            if (string.Equals(processType, "control", StringComparison.OrdinalIgnoreCase))
            {
                return RunWebHost(args);
            }
            return RunConsoleHost();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[ACS] Server terminated unexpectedly");
            return 2;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// 콘솔 프로세스(host/trans/ei/daemon/query/report) 실행 경로.
    /// 기존 Executor 콘솔 흐름 그대로 유지한다. (control은 웹 호스트로 분리됨.)
    /// </summary>
    private static int RunConsoleHost()
    {
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
        finally
        {
            try { executor?.Stop(); } catch { }
        }

        return 0;
    }

    /// <summary>
    /// 웹 백엔드(REST API + SignalR) 호스팅 실행 경로. ASP.NET Core(Kestrel) + Autofac 통합.
    /// control 프로세스가 UI 백엔드를 겸하며, REST 엔드포인트는 ACS.App.Web.Controllers의
    /// 컨트롤러로, 차량 POSE 텔레메트리는 VehicleHub를 통해 SignalR로 노출된다.
    /// 프로세스별 서비스 등록은 Executor.RegisterModules가 process type(control)에 맞춰 수행한다.
    /// </summary>
    private static int RunWebHost(string[] args)
    {
        var executor = new Executor();
        var configuration = Executor.LoadConfiguration();
        executor.ApplyProcessSettings(configuration);

        string listenIp = configuration["Acs:Api:ListenIP"] ?? "any";
        string listenPort = configuration["Acs:Api:ListenPort"] ?? "5100";

        var builder = WebApplication.CreateBuilder(args);

        // appsettings.json은 Executor가 이미 로드한 동일 IConfiguration을 사용하도록 추가
        builder.Configuration.AddConfiguration(configuration);

        // Kestrel 바인딩
        builder.WebHost.ConfigureKestrel(options =>
        {
            int port = int.TryParse(listenPort, out var p) ? p : 5100;
            if (string.Equals(listenIp, "any", StringComparison.OrdinalIgnoreCase))
            {
                options.ListenAnyIP(port);
            }
            else if (System.Net.IPAddress.TryParse(listenIp, out var ip))
            {
                options.Listen(ip, port);
            }
            else
            {
                options.ListenAnyIP(port);
            }
        });

        // CORS — UI 클라이언트가 다른 호스트에서 접근하더라도 허용
        builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
            p.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));

        // MVC + Newtonsoft.Json — 기존 ApiRequestHandler 직렬화 동작과 호환 유지
        builder.Services.AddControllers().AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        });

        builder.Services.AddSignalR();

        // 로그인 세션 보관소 — 메모리 기반 토큰 스토어 (싱글톤)
        builder.Services.AddSingleton<ACS.App.Web.Auth.SessionStore>();

        // Autofac을 ASP.NET Core DI에 통합
        // (PoseTelemetrySubscriber/HostCommSubscriber는 ControlModule에서 IHostedService로 등록되어 Generic Host가 자동 기동)
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
            executor.RegisterModules(containerBuilder, configuration);
        });

        var app = builder.Build();

        // Autofac 컨테이너 핸들 획득 (AutofacServiceProvider → IContainer)
        var container = app.Services.GetAutofacRoot() as IContainer
            ?? throw new InvalidOperationException("Autofac container resolution failed.");

        // DB 마이그레이션, ApplicationInitializer, 스케줄러 시작 (IHostedService는 Generic Host가 관리)
        executor.OnContainerBuilt(container, startHostedServices: false);

        app.UseCors();

        // ACS.UI Velopack 릴리스 피드 정적 서빙 — 클라이언트 PC가 이 경로에서
        // Setup.exe 다운로드(최초 설치) 및 자동 업데이트 체크를 수행한다.
        // 피드 파일(.nupkg, RELEASES, releases.win.json)은 기본 MIME 매핑에 없으므로 ServeUnknownFileTypes 필수.
        string releasePath = configuration["Acs:Api:ClientReleasePath"];
        // 비 Windows(개발용 macOS/Linux)에서는 Windows 절대경로(C:\...)가 유효하지 않으므로 로컬 기본값으로 대체.
        // PhysicalFileProvider는 반드시 절대경로를 요구하므로 마지막에 GetFullPath로 정규화한다.
        if (string.IsNullOrWhiteSpace(releasePath)
            || (!OperatingSystem.IsWindows()
                && System.Text.RegularExpressions.Regex.IsMatch(releasePath, @"^[A-Za-z]:\\")))
        {
            releasePath = OperatingSystem.IsWindows()
                ? @"C:\acs\releases\ui"
                : System.IO.Path.Combine(AppContext.BaseDirectory, "releases", "ui");
        }
        releasePath = System.IO.Path.GetFullPath(releasePath);
        System.IO.Directory.CreateDirectory(releasePath);   // PhysicalFileProvider는 폴더 부재 시 예외
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(releasePath),
            RequestPath = "/releases/ui",
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream"
        });

        app.MapControllers();
        app.MapHub<VehicleHub>("/hubs/vehicle");
        app.MapHub<ACS.App.Web.Hubs.HostCommHub>("/hubs/hostcomm");

        // 종료 시 Executor.Stop() 호출
        var lifetime = app.Lifetime;
        lifetime.ApplicationStopping.Register(() =>
        {
            try { executor.Stop(); } catch { }
        });

        Log.Information("[ACS] Web backend started on {Ip}:{Port}. Press Ctrl+C to stop.", listenIp, listenPort);
        app.Run();
        return 0;
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
