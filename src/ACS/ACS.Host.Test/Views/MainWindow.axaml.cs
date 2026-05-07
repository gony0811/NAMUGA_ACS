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

    private MainWindowViewModel _subscribedVm;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // 이전 DataContext의 PropertyChanged 구독 정리 (다중 구독 방지)
        if (_subscribedVm != null)
        {
            _subscribedVm.MesSimulator.PropertyChanged -= OnMesPropertyChanged;
            _subscribedVm.HostTest.PropertyChanged -= OnHostTestPropertyChanged;
            _subscribedVm = null;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            // DataContext는 XAML의 {Binding MesSimulator} / {Binding HostTest}로 전파된다.
            // 여기서는 인디케이터/로그 스크롤을 위해 PropertyChanged만 구독한다.
            vm.MesSimulator.PropertyChanged += OnMesPropertyChanged;
            vm.HostTest.PropertyChanged += OnHostTestPropertyChanged;
            _subscribedVm = vm;

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
