using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using ACS.Host.Test.ViewModels;

namespace ACS.Host.Test.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainWindowViewModel vm)
        {
            // XAML 바인딩 대신 코드에서 직접 DataContext 설정
            // (Avalonia 11 XAML 컴파일러의 TabItem DataContext 타입 추론 문제 우회)
            var mesGrid = this.FindControl<Grid>("MesGrid");
            var hostGrid = this.FindControl<Grid>("HostGrid");
            if (mesGrid != null) mesGrid.DataContext = vm.MesSimulator;
            if (hostGrid != null) hostGrid.DataContext = vm.HostTest;

            vm.MesSimulator.PropertyChanged += OnMesPropertyChanged;
            vm.HostTest.PropertyChanged += OnHostTestPropertyChanged;

            UpdateIndicator("MesStatusIndicator", vm.MesSimulator.IsConnected);
            UpdateIndicator("HostTestStatusIndicator", vm.HostTest.IsConnected);
        }
    }

    private void OnMesPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MesSimulatorViewModel.IsConnected) && sender is MesSimulatorViewModel vm)
            UpdateIndicator("MesStatusIndicator", vm.IsConnected);
        else if (e.PropertyName == nameof(MesSimulatorViewModel.LogText))
            ScrollToEnd("MesLogTextBox");
    }

    private void OnHostTestPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HostTestViewModel.IsConnected) && sender is HostTestViewModel vm)
            UpdateIndicator("HostTestStatusIndicator", vm.IsConnected);
        else if (e.PropertyName == nameof(HostTestViewModel.LogText))
            ScrollToEnd("HostTestLogTextBox");
    }

    private void ScrollToEnd(string textBoxName)
    {
        var textBox = this.FindControl<TextBox>(textBoxName);
        if (textBox?.Text != null)
            textBox.CaretIndex = textBox.Text.Length;
    }

    private void UpdateIndicator(string indicatorName, bool isConnected)
    {
        var indicator = this.FindControl<Ellipse>(indicatorName);
        if (indicator != null)
        {
            indicator.Fill = isConnected
                ? new SolidColorBrush(Color.Parse("#43a047"))
                : new SolidColorBrush(Color.Parse("#999999"));
        }
    }
}
