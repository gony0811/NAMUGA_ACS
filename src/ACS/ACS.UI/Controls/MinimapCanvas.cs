using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ACS.UI.Models;
using ACS.UI.ViewModels;

namespace ACS.UI.Controls;

/// <summary>
/// 메인 맵 전체를 작은 영역에 fit-to-screen으로 그리고, 현재 viewport를 사각형(혹은 4점 polygon)으로 표시.
/// 클릭/드래그 시 MapViewModel.RequestCenterOnWorld 호출 → MapCanvas가 _pan 조정.
/// MapCanvas와 달리 자체적으로 pan/zoom/rotation을 갖지 않는다(항상 N-up 고정).
/// </summary>
public class MinimapCanvas : Control
{
    private MapViewModel? _viewModel;

    // fit-to-screen transform (자체 계산)
    private double _baseScale = 1.0;
    private double _offsetX;
    private double _offsetY;
    private const double Padding = 6;

    private bool _isDragging;

    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(22, 27, 36));
    private static readonly IBrush LinkBrush = new SolidColorBrush(Color.FromRgb(90, 100, 120));
    private static readonly IPen LinkPen = new Pen(LinkBrush, 0.8);

    private static readonly IBrush NodeBrush = new SolidColorBrush(Color.FromRgb(180, 190, 210));
    private static readonly IBrush NodeChargeBrush = new SolidColorBrush(Color.FromRgb(74, 222, 128));
    private static readonly IBrush NodeStockBrush = new SolidColorBrush(Color.FromRgb(96, 165, 250));

    private static readonly IBrush VehicleIdleBrush = new SolidColorBrush(Color.FromRgb(96, 165, 250));
    private static readonly IBrush VehicleRunBrush = new SolidColorBrush(Color.FromRgb(74, 222, 128));
    private static readonly IBrush VehicleDownBrush = new SolidColorBrush(Color.FromRgb(248, 113, 113));
    private static readonly IBrush VehicleDisconnectBrush = new SolidColorBrush(Color.FromRgb(110, 118, 137));

    private static readonly IBrush ViewportFillBrush = new SolidColorBrush(Color.FromArgb(40, 250, 204, 21));
    private static readonly IPen ViewportPen = new Pen(new SolidColorBrush(Color.FromRgb(250, 204, 21)), 1.5);
    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.FromRgb(70, 78, 95)), 1);

    public MinimapCanvas()
    {
        Focusable = false;
        ClipToBounds = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateViewModel();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.DataChanged -= OnInvalidate;
            _viewModel.ViewportChanged -= OnInvalidate;
            _viewModel = null;
        }
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UpdateViewModel();
    }

    private void UpdateViewModel()
    {
        if (_viewModel != null)
        {
            _viewModel.DataChanged -= OnInvalidate;
            _viewModel.ViewportChanged -= OnInvalidate;
        }

        _viewModel = DataContext as MapViewModel;

        if (_viewModel != null)
        {
            _viewModel.DataChanged += OnInvalidate;
            _viewModel.ViewportChanged += OnInvalidate;
        }

        QueueInvalidate();
    }

    // MapCanvas.Render → MapViewModel.UpdateViewport → ViewportChanged 경로에서
    // 호출되므로 render pass 중 InvalidateVisual 금지 규칙을 피하려면 dispatcher로 미뤄야 함.
    private void OnInvalidate() => QueueInvalidate();

    private void QueueInvalidate()
        => Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        // 배경 + 외곽선
        context.DrawRectangle(BackgroundBrush, BorderPen, new Rect(0, 0, w, h));

        if (_viewModel == null) return;

        var nodes = _viewModel.Nodes;
        var links = _viewModel.Links;
        var vehicles = _viewModel.Vehicles;

        if (nodes.Count == 0) return;

        CalculateTransform(nodes);

        // 노드 좌표 lookup
        var nodePositions = new Dictionary<string, Point>(nodes.Count);
        foreach (var node in nodes)
            nodePositions[node.Id] = TransformPoint(node.Xpos, node.Ypos);

        // Links
        foreach (var link in links)
        {
            if (!nodePositions.TryGetValue(link.FromNodeId ?? "", out var from)) continue;
            if (!nodePositions.TryGetValue(link.ToNodeId ?? "", out var to)) continue;
            context.DrawLine(LinkPen, from, to);
        }

        // Nodes (작은 사각형)
        const double nodeHalf = 1.5;
        foreach (var node in nodes)
        {
            if (!nodePositions.TryGetValue(node.Id, out var pos)) continue;
            var brush = GetNodeBrush(node.Type);
            context.DrawRectangle(brush, null,
                new Rect(pos.X - nodeHalf, pos.Y - nodeHalf, nodeHalf * 2, nodeHalf * 2));
        }

        // Vehicles (작은 원)
        const double vehicleRadius = 2.5;
        foreach (var vehicle in vehicles)
        {
            Point pos;
            if (vehicle.PoseX.HasValue && vehicle.PoseY.HasValue)
                pos = TransformPoint(vehicle.PoseX.Value, vehicle.PoseY.Value);
            else if (nodePositions.TryGetValue(vehicle.CurrentNodeId ?? "", out var nodePos))
                pos = nodePos;
            else
                continue;

            context.DrawEllipse(GetVehicleBrush(vehicle), null, pos, vehicleRadius, vehicleRadius);
        }

        // Viewport 사각형/polygon (회전 시 4점 polygon)
        if (_viewModel.HasViewport)
        {
            var v0 = TransformPoint(_viewModel.ViewportP0.X, _viewModel.ViewportP0.Y);
            var v1 = TransformPoint(_viewModel.ViewportP1.X, _viewModel.ViewportP1.Y);
            var v2 = TransformPoint(_viewModel.ViewportP2.X, _viewModel.ViewportP2.Y);
            var v3 = TransformPoint(_viewModel.ViewportP3.X, _viewModel.ViewportP3.Y);

            var geom = new StreamGeometry();
            using (var gctx = geom.Open())
            {
                gctx.BeginFigure(v0, true);
                gctx.LineTo(v1);
                gctx.LineTo(v2);
                gctx.LineTo(v3);
                gctx.EndFigure(true);
            }
            context.DrawGeometry(ViewportFillBrush, ViewportPen, geom);
        }
    }

    /// <summary>
    /// 노드 bbox 기준 fit-to-screen 스케일/오프셋 계산. MapCanvas.CalculateTransform과 같은 공식이되 회전/zoom 없음.
    /// minimap 자체는 데이터가 바뀌기 전엔 고정. viewport polygon만 메인 맵 상태에 따라 크기/위치가 변함.
    /// polygon이 minimap 영역을 넘어가면 ClipToBounds로 잘려서 표시됨.
    /// </summary>
    private void CalculateTransform(IReadOnlyList<NodeDto> nodes)
    {
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var node in nodes)
        {
            if (node.Xpos < minX) minX = node.Xpos;
            if (node.Ypos < minY) minY = node.Ypos;
            if (node.Xpos > maxX) maxX = node.Xpos;
            if (node.Ypos > maxY) maxY = node.Ypos;
        }

        double rangeX = maxX - minX;
        double rangeY = maxY - minY;
        if (rangeX < 0.001) rangeX = 1;
        if (rangeY < 0.001) rangeY = 1;

        double availableW = Math.Max(Bounds.Width - Padding * 2, 20);
        double availableH = Math.Max(Bounds.Height - Padding * 2, 20);

        _baseScale = Math.Min(availableW / rangeX, availableH / rangeY);
        _offsetX = Padding - minX * _baseScale + (availableW - rangeX * _baseScale) / 2;
        _offsetY = Padding + maxY * _baseScale + (availableH - rangeY * _baseScale) / 2;
    }

    private Point TransformPoint(double x, double y)
        => new(x * _baseScale + _offsetX, -y * _baseScale + _offsetY);

    private (double X, double Y) ScreenToWorld(Point p)
    {
        if (_baseScale <= 0) return (0, 0);
        double worldX = (p.X - _offsetX) / _baseScale;
        double worldY = (_offsetY - p.Y) / _baseScale;
        return (worldX, worldY);
    }

    private void RequestCenter(Point screenPoint)
    {
        if (_viewModel == null || _viewModel.Nodes.Count == 0) return;
        if (_baseScale <= 0) return;
        var (wx, wy) = ScreenToWorld(screenPoint);
        _viewModel.RequestCenterOnWorld(wx, wy);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed) return;

        _isDragging = true;
        e.Pointer.Capture(this);
        RequestCenter(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging) return;
        RequestCenter(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private static IBrush GetNodeBrush(string type)
    {
        return (type ?? "").ToUpperInvariant() switch
        {
            "CHARGE" => NodeChargeBrush,
            "STOCK" => NodeStockBrush,
            _ => NodeBrush
        };
    }

    private static IBrush GetVehicleBrush(VehicleDto vehicle)
    {
        if ((vehicle.ConnectionState ?? "").ToUpperInvariant() == "DISCONNECT")
            return VehicleDisconnectBrush;

        return (vehicle.State ?? "").ToUpperInvariant() switch
        {
            "IDLE" => VehicleIdleBrush,
            "RUN" => VehicleRunBrush,
            "DOWN" => VehicleDownBrush,
            _ => VehicleDisconnectBrush
        };
    }
}
