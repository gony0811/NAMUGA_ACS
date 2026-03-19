using System.Linq;
using Avalonia.Controls;
using ACS.UI.Models;
using ACS.UI.ViewModels;

namespace ACS.UI.Views;

public partial class NodeView : UserControl
{
    public NodeView()
    {
        InitializeComponent();

        var grid = this.FindControl<DataGrid>("NodeDataGrid");
        if (grid != null)
        {
            grid.SelectionChanged += (_, _) =>
            {
                if (DataContext is NodeViewModel vm)
                {
                    vm.SelectedNodes = grid.SelectedItems.OfType<NodeDto>().ToList();
                }
            };
        }
    }
}
