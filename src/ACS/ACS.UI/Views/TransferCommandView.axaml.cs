using System.Linq;
using Avalonia.Controls;
using ACS.UI.Models;
using ACS.UI.ViewModels;

namespace ACS.UI.Views;

public partial class TransferCommandView : UserControl
{
    public TransferCommandView()
    {
        InitializeComponent();

        var grid = this.FindControl<DataGrid>("TransferCommandDataGrid");
        if (grid != null)
        {
            grid.SelectionChanged += (_, _) =>
            {
                if (DataContext is TransferCommandViewModel vm)
                {
                    vm.SelectedCommands = grid.SelectedItems.OfType<TransportCommandDto>().ToList();
                }
            };
        }
    }
}
