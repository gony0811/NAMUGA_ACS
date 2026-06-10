using System;
using Avalonia;
using Velopack;

namespace ACS.UI;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack 설치/업데이트 훅 — 반드시 Avalonia 초기화보다 먼저 실행
        // (설치/제거/업데이트 시 이 안에서 프로세스가 즉시 종료될 수 있음)
        VelopackApp.Build().Run();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
