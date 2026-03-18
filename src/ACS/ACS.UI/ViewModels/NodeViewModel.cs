using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Models;
using ACS.UI.Services;
using ACS.UI.Views;

namespace ACS.UI.ViewModels;

/// <summary>
/// Node View ViewModel — 노드 목록 CRUD
/// </summary>
public partial class NodeViewModel : ObservableObject
{
    private readonly IAcsApiService? _apiService;

    public ObservableCollection<NodeDto> Nodes { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditNodeCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteNodeCommand))]
    private NodeDto? _selectedNode;

    [ObservableProperty]
    private int _totalCount;

    public NodeViewModel(IAcsApiService? apiService = null)
    {
        _apiService = apiService;
    }

    /// <summary>
    /// API에서 노드 목록 로드
    /// </summary>
    [RelayCommand]
    public async Task LoadNodesAsync()
    {
        if (_apiService == null) return;

        try
        {
            var nodes = await _apiService.GetNodesAsync();
            Nodes.Clear();
            foreach (var node in nodes)
            {
                Nodes.Add(node);
            }
            TotalCount = Nodes.Count;
        }
        catch (Exception)
        {
            // 로드 실패 시 무시
        }
    }

    [RelayCommand]
    private async Task AddNodeAsync()
    {
        if (_apiService == null) return;

        var emptyNode = new NodeDto { Type = "COMMON" };
        var dialog = new NodeEditWindow(emptyNode, isEditMode: false);

        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.CreateNodeAsync(dialog.Node);
            if (success)
                await LoadNodesAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedNode))]
    private async Task EditNodeAsync()
    {
        if (_apiService == null || SelectedNode == null) return;

        var dialog = new NodeEditWindow(SelectedNode, isEditMode: true);

        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.UpdateNodeAsync(dialog.Node);
            if (success)
                await LoadNodesAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedNode))]
    private async Task DeleteNodeAsync()
    {
        if (_apiService == null || SelectedNode == null) return;

        var msgBox = new Window
        {
            Title = "Delete Node",
            Width = 320,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = CreateDeleteConfirmContent(SelectedNode.Id)
        };

        var result = await msgBox.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.DeleteNodeAsync(SelectedNode.Id);
            if (success)
                await LoadNodesAsync();
        }
    }

    private bool HasSelectedNode() => SelectedNode != null;

    private static object CreateDeleteConfirmContent(string nodeId)
    {
        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12
        };
        panel.Children.Add(new TextBlock
        {
            Text = $"Node '{nodeId}' 을(를) 삭제하시겠습니까?",
            FontSize = 12
        });

        var btnPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 12
        };

        var okBtn = new Button { Content = "OK", Width = 80, Height = 28 };
        var cancelBtn = new Button { Content = "Cancel", Width = 80, Height = 28 };

        okBtn.Click += (s, e) =>
        {
            var w = (s as Visual)?.FindAncestorOfType<Window>();
            w?.Close(true);
        };
        cancelBtn.Click += (s, e) =>
        {
            var w = (s as Visual)?.FindAncestorOfType<Window>();
            w?.Close(false);
        };

        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        panel.Children.Add(btnPanel);

        return panel;
    }

    private Window GetOwnerWindow()
    {
        // NodeView는 팝업 Window의 Content로 설정되므로, 해당 Window를 찾는다
        return Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.FirstOrDefault(w => w.Title == "Node") ?? desktop.MainWindow!
            : null!;
    }
}
