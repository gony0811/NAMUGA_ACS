using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    private MapViewModel? _mapViewModel;

    public ObservableCollection<NodeDto> Nodes { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditNodeCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteNodeCommand))]
    private NodeDto? _selectedNode;

    [ObservableProperty]
    private int _totalCount;

    /// <summary>
    /// DataGrid 다중 선택된 노드 목록 (code-behind에서 동기화)
    /// </summary>
    public List<NodeDto> SelectedNodes { get; set; } = new();

    public MapViewModel? MapViewModel
    {
        get => _mapViewModel;
        set => _mapViewModel = value;
    }

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
            {
                await LoadNodesAsync();
                await RefreshMapNodesAsync();
            }
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
            {
                await LoadNodesAsync();
                await RefreshMapNodesAsync();
            }
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedNode))]
    private async Task DeleteNodeAsync()
    {
        if (_apiService == null || SelectedNode == null) return;

        // 다중 선택된 노드 목록 (없으면 단일 선택 사용)
        var targets = SelectedNodes.Count > 1
            ? SelectedNodes.ToList()
            : new List<NodeDto> { SelectedNode };

        string message = targets.Count == 1
            ? $"Node '{targets[0].Id}' 을(를) 삭제하시겠습니까?\n(관련 Link, LinkZone도 함께 삭제됩니다)"
            : $"선택된 {targets.Count}개의 Node를 삭제하시겠습니까?\n(관련 Link, LinkZone도 함께 삭제됩니다)";

        var msgBox = new Window
        {
            Title = "Delete Node",
            Width = 360,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = CreateDeleteConfirmContent(message)
        };

        var result = await msgBox.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            foreach (var node in targets)
            {
                await _apiService.DeleteNodeAsync(node.Id);
            }
            await LoadNodesAsync();
            await RefreshMapNodesAsync();
        }
    }

    [RelayCommand]
    private void SelectFromMap()
    {
        if (_apiService == null || _mapViewModel == null) return;

        // 팝업 창 숨기기
        var ownerWindow = GetOwnerWindow();
        ownerWindow?.Hide();

        void Unsubscribe()
        {
            _mapViewModel.NodePlacementCompleted -= OnPlacementCompleted;
            _mapViewModel.NodePlacementCancelled -= OnPlacementCancelled;
            _mapViewModel.NodePositionChanged -= OnNodePositionChanged;
        }

        // 배치 완료 이벤트
        void OnPlacementCompleted(List<(double X, double Y)> positions)
        {
            Unsubscribe();

            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                // 자동 ID 생성 + 일괄 Node 생성
                int existingMax = GetNextNodeNumber();
                int created = 0;

                foreach (var (x, y) in positions)
                {
                    string nodeId = $"N{(existingMax + created + 1):D03}";
                    var node = new NodeDto
                    {
                        Id = nodeId,
                        Type = "COMMON",
                        Xpos = x,
                        Ypos = y
                    };

                    var success = await _apiService.CreateNodeAsync(node);
                    if (success) created++;
                }

                // 목록 갱신 + 맵 갱신 + 팝업 다시 표시
                await LoadNodesAsync();
                await RefreshMapNodesAsync();
                ownerWindow?.Show();
                ownerWindow?.Activate();
            });
        }

        // 배치 취소 이벤트
        void OnPlacementCancelled()
        {
            Unsubscribe();

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ownerWindow?.Show();
                ownerWindow?.Activate();
            });
        }

        // 기존 노드 드래그 이동 완료 이벤트
        void OnNodePositionChanged(string nodeId, double x, double y)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                var node = new NodeDto { Id = nodeId, Xpos = x, Ypos = y };
                // 기존 노드 정보 보존을 위해 현재 목록에서 찾아서 업데이트
                var existing = Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (existing != null)
                {
                    node.Type = existing.Type;
                    node.Zpos = existing.Zpos;
                }
                await _apiService!.UpdateNodeAsync(node);
                await LoadNodesAsync();
                await RefreshMapNodesAsync();
            });
        }

        _mapViewModel.NodePlacementCompleted += OnPlacementCompleted;
        _mapViewModel.NodePlacementCancelled += OnPlacementCancelled;
        _mapViewModel.NodePositionChanged += OnNodePositionChanged;
        _mapViewModel.EnterNodePlacementMode();
    }

    /// <summary>
    /// 맵 캔버스의 노드 데이터를 최신 상태로 갱신
    /// </summary>
    private async Task RefreshMapNodesAsync()
    {
        if (_apiService == null || _mapViewModel == null) return;
        try
        {
            var nodes = await _apiService.GetNodesAsync();
            _mapViewModel.UpdateNodes(nodes);
        }
        catch { }
    }

    /// <summary>
    /// 현재 노드 목록에서 N### 패턴의 최대 번호 추출
    /// </summary>
    private int GetNextNodeNumber()
    {
        int max = 0;
        foreach (var node in Nodes)
        {
            if (node.Id != null && node.Id.StartsWith("N") &&
                int.TryParse(node.Id.Substring(1), out int num) && num > max)
                max = num;
        }
        return max;
    }

    private bool HasSelectedNode() => SelectedNode != null;

    private static object CreateDeleteConfirmContent(string message)
    {
        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12
        };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 12,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
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
