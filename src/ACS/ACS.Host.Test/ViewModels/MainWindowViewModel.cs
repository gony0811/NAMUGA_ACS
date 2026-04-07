using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS.Host.Test.ViewModels;

/// <summary>
/// 메인 윈도우 ViewModel — 두 탭의 ViewModel을 합성
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    public MesSimulatorViewModel MesSimulator { get; } = new();
    public HostTestViewModel HostTest { get; } = new();
}
