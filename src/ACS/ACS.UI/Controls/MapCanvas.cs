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

    // 히트테스트용 캐시 (Render 시 갱신)
    private Dictionary<string, Point> _cachedNodeScreenPositions = new();

    // Station 히트테스트용 캐시
    private Dictionary<string, Point> _cachedStationScreenPositions = new();
    private string? _hoveredStationId;

    // Node 드래그 이동
    private string? _draggingNodeId;
    private bool _isDraggingNode;

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

    // Node 배치 모드
    private static readonly IBrush PendingNodeBrush = new SolidColorBrush(Color.FromRgb(255, 80, 80));
    private static readonly IPen PendingNodePen = new Pen(PendingNodeBrush, 2);
    private static readonly IBrush PlacementBannerBrush = new SolidColorBrush(Color.FromArgb(200, 30, 30, 30));
    private static readonly IBrush PlacementBannerTextBrush = Brushes.White;

    // Node 사각형 기본 스타일
    private static readonly IPen NodeDefaultPen = new Pen(new SolidColorBrush(Color.FromRgb(120, 120, 130)), 1.5);
    private static readonly IBrush StationBrush = new SolidColorBrush(Color.FromRgb(255, 160, 40));
    private static readonly IPen StationPen = new Pen(new SolidColorBrush(Color.FromRgb(200, 120, 20)), 1.5);

    // Link 선택 모드
    private static readonly IPen NodeHoverPen = new Pen(Brushes.White, 3);
    private static readonly IPen NodeSelectedFromPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 60, 60)), 3);

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
        var point = e.GetCurrentPoint(this);

        if (_viewModel?.IsLinkSelectionMode == true)
        {
            if (point.Properties.IsLeftButtonPressed)
            {
                var nodeId = FindNodeAtScreen(e.GetPosition(this));
                if (nodeId != null)
                    _viewModel.SelectNode(nodeId);
                e.Handled = true;
            }
            else if (point.Properties.IsRightButtonPressed)
            {
                _viewModel.CancelLinkSelection();
                Cursor = null;
                e.Handled = true;
            }
            return;
        }

        if (_viewModel?.IsNodePlacementMode == true)
        {
            if (point.Properties.IsLeftButtonPressed)
            {
                // 기존 노드 클릭 체크 → 드래그 시작
                var existingNodeId = FindNodeAtScreen(e.GetPosition(this));
                if (existingNodeId != null)
                {
                    _draggingNodeId = existingNodeId;
                    _isDraggingNode = true;
                    Cursor = new Cursor(StandardCursorType.Hand);
                }
                else
                {
                    // 빈 공간 클릭: 새 노드 위치 추가
                    var (wx, wy) = ScreenToWorld(e.GetPosition(this));
                    _viewModel.AddPendingNode(wx, wy);
                }
                e.Handled = true;
            }
            else if (point.Properties.IsRightButtonPressed)
            {
                // 우클릭: 배치된 노드가 있으면 완료, 없으면 취소
                if (_viewModel.PendingPlacementNodes.Count > 0)
                    _viewModel.FinishNodePlacement();
                else
                    _viewModel.CancelNodePlacement();
                Cursor = null;
                e.Handled = true;
            }
            return;
        }

        if (point.Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastMousePos = e.GetPosition(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        // Node 드래그 이동
        if (_isDraggingNode && _draggingNodeId != null && _viewModel != null)
        {
            var (wx, wy) = ScreenToWorld(e.GetPosition(this));
            _viewModel.UpdateNodePosition(_draggingNodeId, wx, wy);
            e.Handled = true;
            return;
        }

        // Link 선택 모드: hover 처리
        if (_viewModel?.IsLinkSelectionMode == true)
        {
            var nodeId = FindNodeAtScreen(e.GetPosition(this));
            _viewModel.SetHoveredNode(nodeId);
            Cursor = nodeId != null ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Arrow);
            return;
        }

        // 배치 모드: 기존 노드 hover 시 커서 변경
        if (_viewModel?.IsNodePlacementMode == true)
        {
            var nodeId = FindNodeAtScreen(e.GetPosition(this));
            Cursor = nodeId != null ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Cross);
            return;
        }

        if (_isPanning)
        {
            var pos = e.GetPosition(this);
            _pan = new Point(
                _pan.X + (pos.X - _lastMousePos.X),
                _pan.Y + (pos.Y - _lastMousePos.Y));
            _lastMousePos = pos;
            InvalidateVisual();
            return;
        }

        // 일반 모드: Station hover 체크
        var stationId = FindStationAtScreen(e.GetPosition(this));
        if (stationId != _hoveredStationId)
        {
            _hoveredStationId = stationId;
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isDraggingNode && _draggingNodeId != null && _viewModel != null)
        {
            var (wx, wy) = ScreenToWorld(e.GetPosition(this));
            _viewModel.UpdateNodePosition(_draggingNodeId, wx, wy);
            _viewModel.CommitNodePosition(_draggingNodeId, wx, wy);
            _isDraggingNode = false;
            _draggingNodeId = null;
            Cursor = new Cursor(StandardCursorType.Cross);
            e.Handled = true;
            return;
        }

        _isPanning = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            if (_viewModel?.IsLinkSelectionMode == true)
            {
                _viewModel.CancelLinkSelection();
                Cursor = null;
                e.Handled = true;
            }
            else if (_viewModel?.IsNodePlacementMode == true)
            {
                _viewModel.CancelNodePlacement();
                Cursor = null;
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Delete)
        {
            if (_viewModel?.IsNodePlacementMode == true)
            {
                _viewModel.RemoveLastPendingNode();
                e.Handled = true;
            }
        }
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

        // 상호작용 모드 커서/포커스 설정
        if (_viewModel.IsNodePlacementMode)
        {
            Cursor = new Cursor(StandardCursorType.Cross);
            Focusable = true;
            Focus();
        }
        else if (_viewModel.IsLinkSelectionMode)
        {
            Focusable = true;
            Focus();
        }

        var nodes = _viewModel.Nodes;
        var links = _viewModel.Links;
        var vehicles = _viewModel.Vehicles;
        var stations = _viewModel.Stations;
        var locations = _viewModel.Locations;

        // Calculate bounding box and scale
        if (nodes.Count > 0)
            CalculateTransform(nodes);
        else
            CalculateDefaultTransform();

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

            // 히트테스트용 화면 좌표 캐싱 (pan/zoom 적용)
            _cachedNodeScreenPositions.Clear();
            foreach (var (id, pos) in nodePositions)
            {
                _cachedNodeScreenPositions[id] = new Point(
                    pos.X * _zoom + _pan.X,
                    pos.Y * _zoom + _pan.Y);
            }

            // Link lookup 사전 계산
            var outgoingLinks = new Dictionary<string, List<LinkDto>>();
            var incomingLinks = new Dictionary<string, List<LinkDto>>();
            foreach (var link in links)
            {
                if (!string.IsNullOrEmpty(link.FromNodeId))
                {
                    if (!outgoingLinks.ContainsKey(link.FromNodeId))
                        outgoingLinks[link.FromNodeId] = new List<LinkDto>();
                    outgoingLinks[link.FromNodeId].Add(link);
                }
                if (!string.IsNullOrEmpty(link.ToNodeId))
                {
                    if (!incomingLinks.ContainsKey(link.ToNodeId))
                        incomingLinks[link.ToNodeId] = new List<LinkDto>();
                    incomingLinks[link.ToNodeId].Add(link);
                }
            }

            // Station lookup (LinkId → List<StationDto>)
            var stationsByLink = new Dictionary<string, List<StationDto>>();
            foreach (var st in stations)
            {
                if (!string.IsNullOrEmpty(st.LinkId))
                {
                    if (!stationsByLink.ContainsKey(st.LinkId))
                        stationsByLink[st.LinkId] = new List<StationDto>();
                    stationsByLink[st.LinkId].Add(st);
                }
            }

            // Location lookup (StationId → List<LocationId>)
            var locationsByStation = new Dictionary<string, List<string>>();
            foreach (var loc in locations)
            {
                if (!string.IsNullOrEmpty(loc.StationId))
                {
                    if (!locationsByStation.ContainsKey(loc.StationId))
                        locationsByStation[loc.StationId] = new List<string>();
                    if (!string.IsNullOrEmpty(loc.LocationId))
                        locationsByStation[loc.StationId].Add(loc.LocationId);
                }
            }

            // Draw links
            DrawLinks(context, links, nodePositions);

            // Draw nodes (사각형 + 내부 방향 화살표)
            DrawNodes(context, nodes, nodePositions, outgoingLinks, incomingLinks);

            // Draw stations (진행방향 사각형 마커)
            DrawStations(context, links, nodePositions, stationsByLink, locationsByStation);

            // Draw vehicles
            DrawVehicles(context, vehicles, nodePositions);

            // Draw pending placement nodes (임시 마커)
            if (_viewModel.IsNodePlacementMode)
            {
                foreach (var (px, py) in _viewModel.PendingPlacementNodes)
                {
                    var pos = TransformPoint(px, py);
                    double s = 8;
                    // 십자 마커
                    context.DrawLine(PendingNodePen, new Point(pos.X - s, pos.Y), new Point(pos.X + s, pos.Y));
                    context.DrawLine(PendingNodePen, new Point(pos.X, pos.Y - s), new Point(pos.X, pos.Y + s));
                    // 좌표 라벨
                    var label = new FormattedText($"({px},{py})",
                        System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight, DefaultTypeface, 9, PendingNodeBrush);
                    context.DrawText(label, new Point(pos.X + 6, pos.Y - 14));
                }
            }
        }

        // 안내 배너 (transform 밖에서 그리기)
        if (_viewModel.IsNodePlacementMode)
        {
            var bannerText = new FormattedText(
                $"  클릭: 노드 추가 / 기존 노드 드래그 이동  |  우클릭: 완료 / ESC: 취소 / DEL: 마지막 삭제  |  선택됨: {_viewModel.PendingPlacementNodes.Count}개",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, DefaultTypeface, 13, PlacementBannerTextBrush);
            double bannerH = bannerText.Height + 10;
            context.DrawRectangle(PlacementBannerBrush, null, new Rect(0, 0, Bounds.Width, bannerH));
            context.DrawText(bannerText, new Point(10, 5));
        }
        else if (_viewModel.IsLinkSelectionMode)
        {
            string msg = _viewModel.SelectedFromNodeId == null
                ? "  From Node를 선택하세요  (ESC로 취소)"
                : $"  To Node를 선택하세요  (From: {_viewModel.SelectedFromNodeId})  |  ESC로 취소";
            var bannerText = new FormattedText(msg,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, DefaultTypeface, 13, PlacementBannerTextBrush);
            double bannerH = bannerText.Height + 10;
            context.DrawRectangle(PlacementBannerBrush, null, new Rect(0, 0, Bounds.Width, bannerH));
            context.DrawText(bannerText, new Point(10, 5));
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

    /// <summary>
    /// 노드가 없을 때 기본 transform 설정 (1cm = 1px, 원점 중앙)
    /// </summary>
    private void CalculateDefaultTransform()
    {
        _scaleX = 1;
        _scaleY = 1;
        _offsetX = Bounds.Width / 2;
        _offsetY = Bounds.Height / 2;
    }

    private Point TransformPoint(double x, double y)
    {
        return new Point(x * _scaleX + _offsetX, y * _scaleY + _offsetY);
    }

    /// <summary>
    /// 화면 좌표에서 가장 가까운 Node ID를 찾음 (반경 15px 이내)
    /// </summary>
    private string? FindNodeAtScreen(Point screenPos)
    {
        const double hitRadius = 15;
        string? closest = null;
        double closestDist = double.MaxValue;

        foreach (var (nodeId, nodeScreenPos) in _cachedNodeScreenPositions)
        {
            double dx = screenPos.X - nodeScreenPos.X;
            double dy = screenPos.Y - nodeScreenPos.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < hitRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = nodeId;
            }
        }
        return closest;
    }

    private string? FindStationAtScreen(Point screenPos)
    {
        const double hitRadius = 12;
        string? closest = null;
        double closestDist = double.MaxValue;

        foreach (var (stationId, stationScreenPos) in _cachedStationScreenPositions)
        {
            double dx = screenPos.X - stationScreenPos.X;
            double dy = screenPos.Y - stationScreenPos.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < hitRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = stationId;
            }
        }
        return closest;
    }

    /// <summary>
    /// 화면 좌표를 월드 좌표(cm)로 역변환. zoom/pan 보정 포함.
    /// </summary>
    private (int X, int Y) ScreenToWorld(Point screenPoint)
    {
        // pan과 zoom 역변환
        double x = (screenPoint.X - _pan.X) / _zoom;
        double y = (screenPoint.Y - _pan.Y) / _zoom;
        // offset과 scale 역변환
        double worldX = (x - _offsetX) / _scaleX;
        double worldY = (y - _offsetY) / _scaleY;
        return ((int)Math.Round(worldX), (int)Math.Round(worldY));
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

    private void DrawNodes(DrawingContext context, IReadOnlyList<NodeDto> nodes,
        Dictionary<string, Point> nodePositions,
        Dictionary<string, List<LinkDto>> outgoingLinks,
        Dictionary<string, List<LinkDto>> incomingLinks)
    {
        bool isLinkMode = _viewModel?.IsLinkSelectionMode == true;
        string? hoveredId = _viewModel?.HoveredNodeId;
        string? fromId = _viewModel?.SelectedFromNodeId;

        const double size = 7;

        foreach (var node in nodes)
        {
            if (!nodePositions.TryGetValue(node.Id, out var pos)) continue;

            bool isHovered = isLinkMode && node.Id == hoveredId;
            bool isSelectedFrom = isLinkMode && node.Id == fromId;

            // border 색상: 타입별 색상 또는 하이라이트
            IPen borderPen = isSelectedFrom ? NodeSelectedFromPen
                           : isHovered ? NodeHoverPen
                           : new Pen(GetNodeBrush(node.Type), 1.5);

            // 사각형: 흰색 채우기 + 타입별 border
            context.DrawRectangle(Brushes.White, borderPen,
                new Rect(pos.X - size, pos.Y - size, size * 2, size * 2));

            // 내부 방향 화살표 (나가는 Link 기준)
            if (outgoingLinks.TryGetValue(node.Id, out var outLinks) && outLinks.Count > 0)
            {
                // 직진 방향 결정: 들어오는 Link의 연장선에 가장 가까운 나가는 Link
                LinkDto primaryLink = outLinks[0];

                if (outLinks.Count > 1 && incomingLinks.TryGetValue(node.Id, out var inLinks) && inLinks.Count > 0)
                {
                    // 들어오는 방향 (마지막 incoming의 from→to 방향)
                    var inLink = inLinks[0];
                    if (nodePositions.TryGetValue(inLink.FromNodeId ?? "", out var inFrom))
                    {
                        double inDx = pos.X - inFrom.X;
                        double inDy = pos.Y - inFrom.Y;
                        double inLen = Math.Sqrt(inDx * inDx + inDy * inDy);
                        if (inLen > 0.1)
                        {
                            double inUx = inDx / inLen;
                            double inUy = inDy / inLen;

                            // 각 나가는 Link에 대해 직진도 계산 (dot product)
                            double bestDot = double.MinValue;
                            foreach (var ol in outLinks)
                            {
                                if (!nodePositions.TryGetValue(ol.ToNodeId ?? "", out var outTo)) continue;
                                double oDx = outTo.X - pos.X;
                                double oDy = outTo.Y - pos.Y;
                                double oLen = Math.Sqrt(oDx * oDx + oDy * oDy);
                                if (oLen < 0.1) continue;
                                double dot = (oDx / oLen) * inUx + (oDy / oLen) * inUy;
                                if (dot > bestDot)
                                {
                                    bestDot = dot;
                                    primaryLink = ol;
                                }
                            }
                        }
                    }
                }

                // 화살표 방향 계산
                if (nodePositions.TryGetValue(primaryLink.ToNodeId ?? "", out var toPos))
                {
                    double dx = toPos.X - pos.X;
                    double dy = toPos.Y - pos.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 0.1)
                    {
                        double ux = dx / len;
                        double uy = dy / len;

                        // 사각형 내부에 작은 삼각형 화살표
                        double arrowSize = size * 0.65;
                        double tipX = pos.X + ux * arrowSize;
                        double tipY = pos.Y + uy * arrowSize;
                        double bx = pos.X - ux * arrowSize;
                        double by = pos.Y - uy * arrowSize;
                        double px = -uy * arrowSize * 0.7;
                        double py = ux * arrowSize * 0.7;

                        IBrush arrowBrush = primaryLink.Availability switch
                        {
                            "1" => LinkUnavailableBrush,
                            "2" => LinkBannedBrush,
                            _ => LinkAvailableBrush
                        };

                        var arrowGeom = new StreamGeometry();
                        using (var ctx = arrowGeom.Open())
                        {
                            ctx.BeginFigure(new Point(tipX, tipY), true);
                            ctx.LineTo(new Point(bx + px, by + py));
                            ctx.LineTo(new Point(bx - px, by - py));
                            ctx.EndFigure(true);
                        }
                        context.DrawGeometry(arrowBrush, null, arrowGeom);
                    }
                }
            }

            // Node ID 라벨 표시
            double screenSize = size * _zoom;
            if (screenSize >= 4 || isHovered || isSelectedFrom)
            {
                var labelBrush = isSelectedFrom ? NodeSelectedFromPen.Brush : GetNodeBrush(node.Type);
                var label = new FormattedText(node.Id ?? "",
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, DefaultTypeface, 9, labelBrush);
                context.DrawText(label, new Point(pos.X + size + 3, pos.Y - 6));
            }
        }
    }

    /// <summary>
    /// Station 마커: 해당 Link의 FromNode 위치에서 Direction에 따른 방향에 사각형 표시
    /// </summary>
    private void DrawStations(DrawingContext context, IReadOnlyList<LinkDto> links,
        Dictionary<string, Point> nodePositions,
        Dictionary<string, List<StationDto>> stationsByLink,
        Dictionary<string, List<string>> locationsByStation)
    {
        const double size = 7;
        const double offset = size * 2;

        _cachedStationScreenPositions.Clear();

        foreach (var link in links)
        {
            if (!stationsByLink.TryGetValue(link.Id, out var stList)) continue;
            if (!nodePositions.TryGetValue(link.FromNodeId ?? "", out var from)) continue;
            if (!nodePositions.TryGetValue(link.ToNodeId ?? "", out var to)) continue;

            // 진행 방향 단위 벡터
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.1) continue;

            double ux = dx / len;  // 진행 방향
            double uy = dy / len;

            // 방향별 인덱스 (같은 방향에 여러 Station이 있으면 순차 배치)
            var directionIndex = new Dictionary<string, int>();

            foreach (var station in stList)
            {
                // Direction에 따른 offset 벡터 결정
                var dir = (station.Direction ?? "").ToUpperInvariant();
                double ox, oy;
                switch (dir)
                {
                    case "RIGHT":
                        ox = -uy;
                        oy = ux;
                        break;
                    case "LEFTBACK":
                        ox = (uy - ux) * 0.707;
                        oy = (-ux - uy) * 0.707;
                        break;
                    case "RIGHTBACK":
                        ox = (-uy - ux) * 0.707;
                        oy = (ux - uy) * 0.707;
                        break;
                    default:
                        ox = uy;
                        oy = -ux;
                        break;
                }

                if (!directionIndex.TryGetValue(dir, out int idx))
                    idx = 0;
                directionIndex[dir] = idx + 1;

                double stX = from.X + ox * (offset + idx * size * 2.2);
                double stY = from.Y + oy * (offset + idx * size * 2.2);

                // 화면 좌표 캐싱 (히트테스트용)
                _cachedStationScreenPositions[station.Id] = new Point(
                    stX * _zoom + _pan.X,
                    stY * _zoom + _pan.Y);

                bool isHovered = station.Id == _hoveredStationId;

                // 사각형 마커
                context.DrawRectangle(Brushes.White,
                    isHovered ? new Pen(StationBrush, 2.5) : StationPen,
                    new Rect(stX - size, stY - size, size * 2, size * 2));

                // Hover 시 Port ID (LocationId) 라벨 표시
                if (isHovered)
                {
                    string portLabel = "";
                    if (locationsByStation.TryGetValue(station.Id, out var locIds) && locIds.Count > 0)
                        portLabel = string.Join(", ", locIds);
                    else
                        portLabel = station.Id;

                    var label = new FormattedText(portLabel,
                        System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight, DefaultTypeface, 9, StationBrush);
                    context.DrawText(label, new Point(stX + size + 3, stY - 6));
                }
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
                vehicle.VehicleId ?? "?",
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
