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
            vm.PropertyChanged += OnViewModelPropertyChanged;
            UpdateStatusIndicator(vm.IsConnected);
        }
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsConnected) && sender is MainWindowViewModel vm)
        {
            UpdateStatusIndicator(vm.IsConnected);
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.LogText))
        {
            ScrollLogToEnd();
        }
    }

    private void ScrollLogToEnd()
    {
        var logTextBox = this.FindControl<TextBox>("LogTextBox");
        if (logTextBox != null && logTextBox.Text != null)
        {
            logTextBox.CaretIndex = logTextBox.Text.Length;
        }
    }

    private void UpdateStatusIndicator(bool isConnected)
    {
        var indicator = this.FindControl<Ellipse>("StatusIndicator");
        if (indicator != null)
        {
            indicator.Fill = isConnected
                ? new SolidColorBrush(Color.Parse("#43a047"))
                : new SolidColorBrush(Color.Parse("#999999"));
        }
    }
}
