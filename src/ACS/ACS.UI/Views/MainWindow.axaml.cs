using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ACS.UI.ViewModels;

namespace ACS.UI.Views;

public partial class MainWindow : Window
{
    public static readonly IValueConverter ConnectionStatusToColorConverter =
        new FuncValueConverter<string, IBrush>(status =>
        {
            if (status != null && status.StartsWith("Connected"))
                return Brushes.LimeGreen;
            if (status != null && status.StartsWith("Error"))
                return Brushes.Red;
            return Brushes.Gray;
        });

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.StartPollingAsync();
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.StopPolling();
        }
        base.OnClosing(e);
    }
}
