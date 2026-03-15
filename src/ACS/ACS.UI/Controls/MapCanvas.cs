using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ACS.UI.Models;
using ACS.UI.ViewModels;

namespace ACS.UI.Controls;

public class MapCanvas : Control
{
    private MapViewModel _viewModel;
    private double _zoom = 1.0;
    private Point _pan = new(0, 0);
    private Point _lastMousePos;
    private bool _isPanning;

    // Coordinate transform
    private double _scaleX = 1;
    private double _scaleY = 1;
    private double _offsetX;
    private double _offsetY;
    private const double Padding = 40;

    // Light theme brushes
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(245, 247, 250));

    private static readonly IBrush LinkAvailableBrush = new SolidColorBrush(Color.FromRgb(60, 160, 60));
    private static readonly IBrush LinkUnavailableBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));
    private static readonly IBrush LinkBannedBrush = new SolidColorBrush(Color.FromRgb(200, 60, 60));

    private static readonly IPen LinkAvailablePen = new Pen(LinkAvailableBrush, 1.5);
    private static readonly IPen LinkUnavailablePen = new Pen(LinkUnavailableBrush, 1.5);
    private static readonly IPen LinkBannedPen = new Pen(LinkBannedBrush, 1.5);

    private static readonly IBrush NodeCommonBrush = new SolidColorBrush(Color.FromRgb(140, 140, 140));
    private static readonly IBrush NodeChargeBrush = new SolidColorBrush(Color.FromRgb(40, 170, 40));
    private static readonly IBrush NodeCrossBrush = new SolidColorBrush(Color.FromRgb(200, 180, 30));
    private static readonly IBrush NodeStockBrush = new SolidColorBrush(Color.FromRgb(50, 110, 200));
    private static readonly IBrush NodeMonitorBrush = new SolidColorBrush(Color.FromRgb(160, 80, 200));

    private static readonly IBrush VehicleIdleBrush = new SolidColorBrush(Color.FromRgb(30, 120, 230));
    private static readonly IBrush VehicleRunBrush = new SolidColorBrush(Color.FromRgb(40, 180, 40));
    private static readonly IBrush VehicleChargeBrush = new SolidColorBrush(Color.FromRgb(230, 190, 0));
    private static readonly IBrush VehicleDownBrush = new SolidColorBrush(Color.FromRgb(220, 50, 50));
    private static readonly IBrush VehicleDisconnectBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));

    private static readonly IPen VehicleOutlinePen = new Pen(new SolidColorBrush(Color.FromRgb(50, 50, 60)), 2);
    private static readonly Typeface DefaultTypeface = new("Inter", FontStyle.Normal, FontWeight.Bold);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateViewModel();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UpdateViewModel();
    }

    private void UpdateViewModel()
    {
        if (_viewModel != null)
            _viewModel.DataChanged -= OnDataChanged;

        _viewModel = DataContext as MapViewModel;

        if (_viewModel != null)
            _viewModel.DataChanged += OnDataChanged;
    }

    private void OnDataChanged()
    {
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastMousePos = e.GetPosition(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isPanning)
        {
            var pos = e.GetPosition(this);
            _pan = new Point(
                _pan.X + (pos.X - _lastMousePos.X),
                _pan.Y + (pos.Y - _lastMousePos.Y));
            _lastMousePos = pos;
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPanning = false;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var delta = e.Delta.Y > 0 ? 1.1 : 0.9;
        var pos = e.GetPosition(this);

        // Zoom toward cursor
        _pan = new Point(
            pos.X - (pos.X - _pan.X) * delta,
            pos.Y - (pos.Y - _pan.Y) * delta);
        _zoom *= delta;
        _zoom = Math.Clamp(_zoom, 0.1, 20);

        InvalidateVisual();
        e.Handled = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Light background
        context.DrawRectangle(BackgroundBrush, null,
            new Rect(0, 0, Bounds.Width, Bounds.Height));

        if (_viewModel == null) return;

        var nodes = _viewModel.Nodes;
        var links = _viewModel.Links;
        var vehicles = _viewModel.Vehicles;

        if (nodes.Count == 0) return;

        // Calculate bounding box and scale
        CalculateTransform(nodes);

        // Apply pan and zoom transform
        using (context.PushTransform(Matrix.CreateTranslation(_pan.X, _pan.Y)))
        using (context.PushTransform(Matrix.CreateScale(_zoom, _zoom)))
        {
            // Build node position lookup
            var nodePositions = new Dictionary<string, Point>();
            foreach (var node in nodes)
            {
                var pos = TransformPoint(node.Xpos, node.Ypos);
                nodePositions[node.Id] = pos;
            }

            // Draw links
            DrawLinks(context, links, nodePositions);

            // Draw nodes
            DrawNodes(context, nodes, nodePositions);

            // Draw vehicles
            DrawVehicles(context, vehicles, nodePositions);
        }
    }

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

        if (rangeX < 1) rangeX = 1;
        if (rangeY < 1) rangeY = 1;

        double availableW = Math.Max(Bounds.Width - Padding * 2, 100);
        double availableH = Math.Max(Bounds.Height - Padding * 2, 100);

        _scaleX = availableW / rangeX;
        _scaleY = availableH / rangeY;

        // Uniform scale
        double scale = Math.Min(_scaleX, _scaleY);
        _scaleX = scale;
        _scaleY = scale;

        _offsetX = Padding - minX * _scaleX + (availableW - rangeX * scale) / 2;
        _offsetY = Padding - minY * _scaleY + (availableH - rangeY * scale) / 2;
    }

    private Point TransformPoint(double x, double y)
    {
        return new Point(x * _scaleX + _offsetX, y * _scaleY + _offsetY);
    }

    private void DrawLinks(DrawingContext context, IReadOnlyList<LinkDto> links, Dictionary<string, Point> nodePositions)
    {
        foreach (var link in links)
        {
            if (!nodePositions.TryGetValue(link.FromNodeId ?? "", out var from)) continue;
            if (!nodePositions.TryGetValue(link.ToNodeId ?? "", out var to)) continue;

            IPen pen = link.Availability switch
            {
                "1" => LinkUnavailablePen,
                "2" => LinkBannedPen,
                _ => LinkAvailablePen
            };

            context.DrawLine(pen, from, to);
        }
    }

    private void DrawNodes(DrawingContext context, IReadOnlyList<NodeDto> nodes, Dictionary<string, Point> nodePositions)
    {
        foreach (var node in nodes)
        {
            if (!nodePositions.TryGetValue(node.Id, out var pos)) continue;

            double size = 6;
            IBrush brush = GetNodeBrush(node.Type);

            var nodeType = (node.Type ?? "").ToUpperInvariant();
            if (nodeType == "CHARGE" || nodeType == "STOCK")
            {
                // Draw rectangle for charge/stock
                context.DrawRectangle(brush, null,
                    new Rect(pos.X - size, pos.Y - size, size * 2, size * 2));
            }
            else if (nodeType == "CROSS_S" || nodeType == "CROSS_E")
            {
                // Draw diamond for cross nodes
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(new Point(pos.X, pos.Y - size), true);
                    ctx.LineTo(new Point(pos.X + size, pos.Y));
                    ctx.LineTo(new Point(pos.X, pos.Y + size));
                    ctx.LineTo(new Point(pos.X - size, pos.Y));
                    ctx.EndFigure(true);
                }
                context.DrawGeometry(brush, null, geometry);
            }
            else
            {
                // Draw circle for common/other nodes
                context.DrawEllipse(brush, null, pos, size, size);
            }
        }
    }

    private void DrawVehicles(DrawingContext context, IReadOnlyList<VehicleDto> vehicles, Dictionary<string, Point> nodePositions)
    {
        foreach (var vehicle in vehicles)
        {
            if (!nodePositions.TryGetValue(vehicle.CurrentNodeId ?? "", out var pos)) continue;

            double radius = 14;
            IBrush brush = GetVehicleBrush(vehicle);

            // Vehicle circle
            context.DrawEllipse(brush, VehicleOutlinePen, pos, radius, radius);

            // Vehicle ID label (dark text for light theme)
            var text = new FormattedText(
                vehicle.Id ?? "?",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                DefaultTypeface,
                10,
                Brushes.White);

            context.DrawText(text, new Point(pos.X - text.Width / 2, pos.Y - text.Height / 2));

            // Battery indicator bar below vehicle
            double barWidth = 20;
            double barHeight = 3;
            double barY = pos.Y + radius + 3;
            double fillWidth = barWidth * vehicle.BatteryRate / 100.0;

            context.DrawRectangle(new SolidColorBrush(Color.FromRgb(200, 200, 200)), null,
                new Rect(pos.X - barWidth / 2, barY, barWidth, barHeight));

            IBrush batteryBrush = vehicle.BatteryRate >= 70 ? Brushes.LimeGreen :
                                  vehicle.BatteryRate >= 30 ? Brushes.Gold : Brushes.Red;
            context.DrawRectangle(batteryBrush, null,
                new Rect(pos.X - barWidth / 2, barY, fillWidth, barHeight));
        }
    }

    private static IBrush GetNodeBrush(string type)
    {
        return (type ?? "").ToUpperInvariant() switch
        {
            "CHARGE" => NodeChargeBrush,
            "CROSS_S" or "CROSS_E" => NodeCrossBrush,
            "STOCK" => NodeStockBrush,
            "MONITOR" => NodeMonitorBrush,
            _ => NodeCommonBrush
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
            "CHARGE" => VehicleChargeBrush,
            "DOWN" => VehicleDownBrush,
            "MANUAL" => VehicleChargeBrush,
            _ => VehicleDisconnectBrush
        };
    }
}
