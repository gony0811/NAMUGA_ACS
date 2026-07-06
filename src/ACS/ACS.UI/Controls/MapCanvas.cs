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
    private double _rotation = 0; // 회전 각도 (radians)
    private Point _lastMousePos;
    private bool _isPanning;
    private bool _isRotating;

    // 히트테스트용 캐시 (Render 시 갱신)
    private Dictionary<string, Point> _cachedNodeScreenPositions = new();

    // Station 히트테스트용 캐시
    private Dictionary<string, Point> _cachedStationScreenPositions = new();
    private string? _hoveredStationId;

    // Vehicle 히트테스트용 캐시
    private Dictionary<string, Point> _cachedVehicleScreenPositions = new();
    private string? _hoveredVehicleId;

    // Link 히트테스트용 캐시 (화면 좌표 선분, 회전 포함) — Render 시 갱신
    private readonly List<(string Id, Point From, Point To)> _cachedLinkScreenSegments = new();

    // Port(Location) 히트테스트용 캐시 (Edit 모드에서만 채워짐)
    private Dictionary<string, Point> _cachedPortScreenPositions = new();

    // Edit 모드 좌클릭: 드래그(팬)인지 클릭(선택)인지 구분용
    private Point _pressScreenPos;
    private bool _editClickCandidate;

    // Node 드래그 이동
    private string? _draggingNodeId;
    private bool _isDraggingNode;

    // Coordinate transform: 월드(m) → 화면(px)
    // 기본 스케일: 1px = 1m. zoom으로 확대/축소 (최대 1px=1mm, 최소 1px=1m)
    private double _baseScale = 1.0; // fit-to-screen 기본 스케일 (px per meter)
    private double _offsetX;
    private double _offsetY;
    private const double Padding = 40;

    // Dark theme brushes
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(28, 34, 44));

    private static readonly IBrush LinkAvailableBrush = new SolidColorBrush(Color.FromRgb(74, 222, 128));
    private static readonly IBrush LinkUnavailableBrush = new SolidColorBrush(Color.FromRgb(110, 118, 137));
    private static readonly IBrush LinkBannedBrush = new SolidColorBrush(Color.FromRgb(248, 113, 113));

    private static readonly IPen LinkAvailablePen = new Pen(LinkAvailableBrush, 1.5);
    private static readonly IPen LinkUnavailablePen = new Pen(LinkUnavailableBrush, 1.5);
    private static readonly IPen LinkBannedPen = new Pen(LinkBannedBrush, 1.5);

    private static readonly IBrush NodeCommonBrush = new SolidColorBrush(Color.FromRgb(180, 190, 210));
    private static readonly IBrush NodeChargeBrush = new SolidColorBrush(Color.FromRgb(74, 222, 128));
    private static readonly IBrush NodeCrossBrush = new SolidColorBrush(Color.FromRgb(250, 204, 21));
    private static readonly IBrush NodeStockBrush = new SolidColorBrush(Color.FromRgb(96, 165, 250));
    private static readonly IBrush NodeMonitorBrush = new SolidColorBrush(Color.FromRgb(192, 132, 252));

    private static readonly IBrush VehicleIdleBrush = new SolidColorBrush(Color.FromRgb(96, 165, 250));
    private static readonly IBrush VehicleRunBrush = new SolidColorBrush(Color.FromRgb(74, 222, 128));
    private static readonly IBrush VehicleChargeBrush = new SolidColorBrush(Color.FromRgb(250, 204, 21));
    private static readonly IBrush VehicleDownBrush = new SolidColorBrush(Color.FromRgb(248, 113, 113));
    private static readonly IBrush VehicleDisconnectBrush = new SolidColorBrush(Color.FromRgb(110, 118, 137));

    private static readonly IPen VehicleOutlinePen = new Pen(new SolidColorBrush(Color.FromRgb(220, 225, 235)), 2);
    private static readonly Typeface DefaultTypeface = new("Inter", FontStyle.Normal, FontWeight.Bold);

    // Node 배치 모드
    private static readonly IBrush PendingNodeBrush = new SolidColorBrush(Color.FromRgb(252, 165, 165));
    private static readonly IPen PendingNodePen = new Pen(PendingNodeBrush, 2);
    private static readonly IBrush PlacementBannerBrush = new SolidColorBrush(Color.FromArgb(220, 15, 20, 30));
    private static readonly IBrush PlacementBannerTextBrush = Brushes.White;

    // Node 사각형 기본 스타일
    private static readonly IPen NodeDefaultPen = new Pen(new SolidColorBrush(Color.FromRgb(60, 70, 85)), 1.5);
    private static readonly IBrush StationBrush = new SolidColorBrush(Color.FromRgb(251, 146, 60));
    private static readonly IPen StationPen = new Pen(new SolidColorBrush(Color.FromRgb(234, 88, 12)), 1.5);

    // Link 선택 모드
    private static readonly IPen NodeHoverPen = new Pen(Brushes.White, 3);
    private static readonly IPen NodeSelectedFromPen = new Pen(new SolidColorBrush(Color.FromRgb(248, 113, 113)), 3);

    // Edit 모드: 선택 하이라이트 + Port 마커
    private static readonly IBrush SelectionBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // sky-400
    private static readonly IBrush PortFillBrush = new SolidColorBrush(Color.FromRgb(45, 212, 191));  // teal-400
    private static readonly IPen PortPen = new Pen(new SolidColorBrush(Color.FromRgb(13, 148, 136)), 1.2);

    /// <summary>
    /// 현재 유효 스케일 (px per meter). baseScale * zoom.
    /// zoom=1일 때 fit-to-screen, 최대 확대 시 1px=1mm(1000px/m), 최소 축소 시 1px/m.
    /// </summary>
    private double EffectiveScale => _baseScale * _zoom;

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
        {
            _viewModel.DataChanged -= OnDataChanged;
            _viewModel.CenterOnWorldRequested -= OnCenterOnWorldRequested;
        }

        _viewModel = DataContext as MapViewModel;

        if (_viewModel != null)
        {
            _viewModel.DataChanged += OnDataChanged;
            _viewModel.CenterOnWorldRequested += OnCenterOnWorldRequested;
        }
    }

    private void OnDataChanged()
    {
        InvalidateVisual();
    }

    /// <summary>
    /// Minimap에서 "이 월드 좌표를 화면 중심으로" 요청을 받았을 때 _pan을 조정한다.
    /// 회전은 화면 중심을 기준으로 적용되므로 점이 중심에 있으면 그대로 유지됨 → 회전 보정 불필요.
    /// </summary>
    private void OnCenterOnWorldRequested(double worldX, double worldY)
    {
        // _baseScale/_offsetX/_offsetY 는 CalculateTransform에서 갱신.
        // 노드가 없는 초기 상태에서는 의미 있는 이동 불가 → skip.
        if (_baseScale <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0) return;

        double cx = Bounds.Width / 2;
        double cy = Bounds.Height / 2;
        double bx = worldX * _baseScale + _offsetX;
        double by = -worldY * _baseScale + _offsetY;
        _pan = new Point(cx - bx * _zoom, cy - by * _zoom);
        InvalidateVisual();
    }

    /// <summary>
    /// 현재 화면 4모서리에 해당하는 월드 좌표를 계산해 MapViewModel에 publish.
    /// ScreenToWorld가 rotation/pan/zoom 모두 역변환하므로 4개 코너 그대로 사용.
    /// </summary>
    private void PublishViewport()
    {
        if (_viewModel == null) return;
        if (_baseScale <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0) return;

        double w = Bounds.Width;
        double h = Bounds.Height;
        var p0 = ScreenToWorld(new Point(0, 0));
        var p1 = ScreenToWorld(new Point(w, 0));
        var p2 = ScreenToWorld(new Point(w, h));
        var p3 = ScreenToWorld(new Point(0, h));
        _viewModel.UpdateViewport(p0, p1, p2, p3);
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

        if (_viewModel?.IsEditMode == true && point.Properties.IsLeftButtonPressed)
        {
            // 좌클릭: 우선 팬 후보로 두고, release 시 이동량이 작으면 "클릭=선택"으로 처리.
            _isPanning = true;
            _lastMousePos = e.GetPosition(this);
            _pressScreenPos = _lastMousePos;
            _editClickCandidate = true;
            e.Handled = true;
            return;
        }

        if (point.Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastMousePos = e.GetPosition(this);
            e.Handled = true;
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            _isRotating = true;
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

        if (_isRotating)
        {
            var pos = e.GetPosition(this);
            double centerX = Bounds.Width / 2;
            double centerY = Bounds.Height / 2;
            double prevAngle = Math.Atan2(_lastMousePos.Y - centerY, _lastMousePos.X - centerX);
            double currAngle = Math.Atan2(pos.Y - centerY, pos.X - centerX);
            _rotation += currAngle - prevAngle;
            _lastMousePos = pos;
            InvalidateVisual();
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

        // 일반 모드: Vehicle/Station hover 체크 (vehicle 우선)
        var screenPos = e.GetPosition(this);
        var vehicleId = FindVehicleAtScreen(screenPos);
        var stationId = vehicleId == null ? FindStationAtScreen(screenPos) : null;
        bool changed = false;
        if (vehicleId != _hoveredVehicleId)
        {
            _hoveredVehicleId = vehicleId;
            changed = true;
        }
        if (stationId != _hoveredStationId)
        {
            _hoveredStationId = stationId;
            changed = true;
        }
        if (changed)
            InvalidateVisual();
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

        // Edit 모드 좌클릭 선택: 이동량이 임계값 미만이면 클릭으로 간주, 이상이면 팬으로 처리.
        if (_editClickCandidate && _viewModel?.IsEditMode == true)
        {
            _editClickCandidate = false;
            var relPos = e.GetPosition(this);
            double moved = Math.Sqrt(
                Math.Pow(relPos.X - _pressScreenPos.X, 2) +
                Math.Pow(relPos.Y - _pressScreenPos.Y, 2));
            if (moved < 4)
                SelectAtScreen(relPos);
        }

        _isPanning = false;
        _isRotating = false;
    }

    /// <summary>
    /// Edit 모드 히트테스트: Port → Station → Node → Link 우선순위로 최초 히트 엔티티를 선택.
    /// (작은 마커/구체적 대상 우선. 아무것도 없으면 선택 해제.)
    /// </summary>
    private void SelectAtScreen(Point screenPos)
    {
        if (_viewModel == null) return;

        var portId = FindPortAtScreen(screenPos);
        if (portId != null) { _viewModel.SelectEntity("Port", portId); return; }

        var stationId = FindStationAtScreen(screenPos);
        if (stationId != null) { _viewModel.SelectEntity("Station", stationId); return; }

        var nodeId = FindNodeAtScreen(screenPos);
        if (nodeId != null) { _viewModel.SelectEntity("Node", nodeId); return; }

        var linkId = FindLinkAtScreen(screenPos);
        if (linkId != null) { _viewModel.SelectEntity("Link", linkId); return; }

        _viewModel.ClearSelection();
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
            if (_viewModel?.IsEditMode == true && _viewModel.SelectedEntityId != null)
            {
                // 실제 삭제/확인은 MainWindowViewModel(DeleteEntityRequested 구독) 가 처리.
                _viewModel.RequestDeleteSelected();
                e.Handled = true;
            }
            else if (_viewModel?.IsNodePlacementMode == true)
            {
                _viewModel.RemoveLastPendingNode();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.R)
        {
            // R 키: 회전 초기화
            _rotation = 0;
            InvalidateVisual();
            e.Handled = true;
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

        // zoom 범위 제한: 기본 1px=0.1m(10px/m), 최대 확대 1px=1mm(1000px/m), 최소 축소 1px=1m(1px/m)
        double minZoom = Math.Max(0.05, 1.0 / Math.Max(_baseScale, 1));
        double maxZoom = Math.Max(20, 1000.0 / Math.Max(_baseScale, 1));
        _zoom = Math.Clamp(_zoom, minZoom, maxZoom);

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
        else if (_viewModel.IsEditMode)
        {
            // Del 키 수신을 위해 포커스 확보
            Focusable = true;
            Focus();
        }

        var nodes = _viewModel.Nodes;
        var links = _viewModel.Links;
        var vehicles = _viewModel.Vehicles;
        var stations = _viewModel.Stations;
        var locations = _viewModel.Locations;

        // Calculate base transform (fit-to-screen)
        if (nodes.Count > 0)
            CalculateTransform(nodes);
        else
            CalculateDefaultTransform();

        // Apply pan, zoom, rotation transform
        // 순서: 화면 중심 이동 → 회전 → 되돌리기 → zoom → pan
        double cx = Bounds.Width / 2;
        double cy = Bounds.Height / 2;
        var rotationMatrix = Matrix.CreateTranslation(-cx, -cy)
            * Matrix.CreateRotation(_rotation)
            * Matrix.CreateTranslation(cx, cy);

        using (context.PushTransform(rotationMatrix))
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

            // 히트테스트용 화면 좌표 캐싱 (pan/zoom/rotation 적용)
            _cachedNodeScreenPositions.Clear();
            foreach (var (id, pos) in nodePositions)
            {
                // zoom + pan 적용
                double sx = pos.X * _zoom + _pan.X;
                double sy = pos.Y * _zoom + _pan.Y;
                // 화면 중심 기준 회전 적용
                double cos = Math.Cos(_rotation);
                double sin = Math.Sin(_rotation);
                double rx = (sx - cx) * cos - (sy - cy) * sin + cx;
                double ry = (sx - cx) * sin + (sy - cy) * cos + cy;
                _cachedNodeScreenPositions[id] = new Point(rx, ry);
            }

            // Link 화면 선분 캐싱 (히트테스트용) — 노드 화면 좌표(회전 포함) 기준
            _cachedLinkScreenSegments.Clear();
            foreach (var link in links)
            {
                if (string.IsNullOrEmpty(link.Id)) continue;
                if (_cachedNodeScreenPositions.TryGetValue(link.FromNodeId ?? "", out var lf) &&
                    _cachedNodeScreenPositions.TryGetValue(link.ToNodeId ?? "", out var lt))
                    _cachedLinkScreenSegments.Add((link.Id, lf, lt));
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

            // 줌만 보정하여 화면 고정 크기 계산 (base-screen 좌표 단위)
            // TransformPoint가 이미 _baseScale을 적용하므로, _zoom만 역보정해야 화면 px 고정
            double nodeSize = Math.Clamp(7.0 / _zoom, 0.3, 500);           // 노드 사각형 반 크기
            double vehicleRadius = Math.Clamp(14.0 / _zoom, 0.5, 1000);    // 차량 원 반지름
            double fontSize = Math.Clamp(9.0 / _zoom, 0.3, 500);           // 라벨 폰트 크기
            double linkWidth = Math.Clamp(1.5 / _zoom, 0.05, 100);         // 링크 선 굵기

            // Draw links (Option 탭의 ShowLinks 토글로 제어)
            if (_viewModel.ShowLinks)
                DrawLinks(context, links, nodePositions, linkWidth);

            // Draw nodes (사각형 + 내부 방향 화살표)
            DrawNodes(context, nodes, nodePositions, outgoingLinks, incomingLinks, nodeSize, fontSize);

            // Draw stations (진행방향 사각형 마커)
            DrawStations(context, links, nodePositions, stationsByLink, locationsByStation, nodeSize, fontSize);

            // Draw vehicles
            DrawVehicles(context, vehicles, nodePositions, vehicleRadius, fontSize);

            // Draw pending placement nodes (임시 마커)
            if (_viewModel.IsNodePlacementMode)
            {
                foreach (var (px, py) in _viewModel.PendingPlacementNodes)
                {
                    var pos = TransformPoint(px, py);
                    double s = nodeSize;
                    double penWidth = Math.Clamp(2.0 / _zoom, 0.1, 100);
                    var pendingPen = new Pen(PendingNodeBrush, penWidth);
                    // 십자 마커
                    context.DrawLine(pendingPen, new Point(pos.X - s, pos.Y), new Point(pos.X + s, pos.Y));
                    context.DrawLine(pendingPen, new Point(pos.X, pos.Y - s), new Point(pos.X, pos.Y + s));
                    // 좌표 라벨
                    var label = new FormattedText($"({px:F1},{py:F1})",
                        System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight, DefaultTypeface, fontSize, PendingNodeBrush);
                    context.DrawText(label, new Point(pos.X + s * 0.8, pos.Y - fontSize * 1.5));
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

        // Vehicle hover 팝업 (스케일 표시 위에)
        DrawVehicleHoverPopup(context, vehicles);

        // 스케일 표시 (우하단)
        DrawScaleIndicator(context);

        // Minimap 동기화: 현재 viewport(월드 4모서리) publish
        PublishViewport();
    }

    private void DrawVehicleHoverPopup(DrawingContext context, IReadOnlyList<VehicleDto> vehicles)
    {
        if (string.IsNullOrEmpty(_hoveredVehicleId)) return;
        if (!_cachedVehicleScreenPositions.TryGetValue(_hoveredVehicleId, out var anchor)) return;
        var v = vehicles.FirstOrDefault(x => x.VehicleId == _hoveredVehicleId);
        if (v == null) return;

        string poseStr = v.PoseX.HasValue && v.PoseY.HasValue
            ? $"{v.PoseX.Value:F2}, {v.PoseY.Value:F2} m"
            : "N/A";
        string angleStr = v.PoseAngle.HasValue
            ? $"{v.PoseAngle.Value * 180.0 / Math.PI:F1}°"
            : "N/A";

        var rows = new (string Label, string Value)[]
        {
            ("Vehicle ID",   v.VehicleId ?? "?"),
            ("Connection",   v.ConnectionState ?? "-"),
            ("State",        v.State ?? "-"),
            ("Processing",   v.ProcessingState ?? "-"),
            ("Run",          v.RunState ?? "-"),
            ("Alarm",        v.AlarmState ?? "-"),
            ("Transfer",     v.TransferState ?? "-"),
            ("Battery",      $"{v.BatteryRate}%  ({v.BatteryVoltage:F1}V)"),
            ("Position",     poseStr),
            ("Heading",      angleStr),
            ("Current Node", v.CurrentNodeId ?? "-"),
            ("ACS Dest",     v.AcsDestNodeId ?? "-"),
            ("Vehicle Dest", v.VehicleDestNodeId ?? "-"),
            ("Carrier",      string.IsNullOrEmpty(v.CarrierType) ? "-" : v.CarrierType),
            ("Cmd ID",       string.IsNullOrEmpty(v.TransportCommandId) ? "-" : v.TransportCommandId),
        };

        const double fontSizePopup = 11.5;
        var labelTypeface = new Typeface("Inter", FontStyle.Normal, FontWeight.Normal);
        var labelBrush = new SolidColorBrush(Color.FromRgb(180, 190, 210));
        var valueBrush = Brushes.White;

        var labelTexts = new FormattedText[rows.Length];
        var valueTexts = new FormattedText[rows.Length];
        double maxLabelW = 0, maxValueW = 0, lineH = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            labelTexts[i] = new FormattedText(rows[i].Label,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, labelTypeface, fontSizePopup, labelBrush);
            valueTexts[i] = new FormattedText(rows[i].Value,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, DefaultTypeface, fontSizePopup, valueBrush);
            if (labelTexts[i].Width > maxLabelW) maxLabelW = labelTexts[i].Width;
            if (valueTexts[i].Width > maxValueW) maxValueW = valueTexts[i].Width;
            if (labelTexts[i].Height > lineH) lineH = labelTexts[i].Height;
        }

        const double padding = 8;
        const double colGap = 12;
        double popupW = padding * 2 + maxLabelW + colGap + maxValueW;
        double popupH = padding * 2 + lineH * rows.Length;

        // 차량 우하단으로 약간 offset, 화면 안에 들어오도록 클램프
        const double offX = 22, offY = 12;
        double x = anchor.X + offX;
        double y = anchor.Y + offY;
        if (x + popupW > Bounds.Width - 4) x = anchor.X - offX - popupW;
        if (y + popupH > Bounds.Height - 4) y = anchor.Y - offY - popupH;
        if (x < 4) x = 4;
        if (y < 4) y = 4;

        var bgBrush = new SolidColorBrush(Color.FromArgb(235, 30, 35, 45));
        var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 100, 110, 130)), 1);
        context.DrawRectangle(bgBrush, borderPen, new Rect(x, y, popupW, popupH), 4, 4);

        double labelX = x + padding;
        double valueX = x + padding + maxLabelW + colGap;
        double textY = y + padding;
        for (int i = 0; i < rows.Length; i++)
        {
            context.DrawText(labelTexts[i], new Point(labelX, textY));
            context.DrawText(valueTexts[i], new Point(valueX, textY));
            textY += lineH;
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

        if (rangeX < 0.001) rangeX = 1;
        if (rangeY < 0.001) rangeY = 1;

        double availableW = Math.Max(Bounds.Width - Padding * 2, 100);
        double availableH = Math.Max(Bounds.Height - Padding * 2, 100);

        double scaleX = availableW / rangeX;
        double scaleY = availableH / rangeY;

        // Uniform scale (px per meter at zoom=1)
        _baseScale = Math.Min(scaleX, scaleY);

        _offsetX = Padding - minX * _baseScale + (availableW - rangeX * _baseScale) / 2;
        // Y축 반전: 월드 +Y는 위쪽(화면 -Y)이므로 maxY가 화면 상단에 위치
        _offsetY = Padding + maxY * _baseScale + (availableH - rangeY * _baseScale) / 2;
    }

    /// <summary>
    /// 노드가 없을 때 기본 transform 설정 (1px = 0.1m = 10px/m, 원점은 좌측 하단)
    /// </summary>
    private void CalculateDefaultTransform()
    {
        _baseScale = 10; // 10px per meter → 1px = 0.1m
        _offsetX = Padding;
        _offsetY = Bounds.Height - Padding;
    }

    /// <summary>
    /// 월드 좌표 → 화면 좌표 변환.
    /// 월드 좌표계: 우측 +X, 좌측 -X, 위 +Y, 아래 -Y (좌측 하단 기준).
    /// Avalonia 화면 Y축은 아래로 증가하므로 Y는 반전.
    /// </summary>
    private Point TransformPoint(double x, double y)
    {
        return new Point(x * _baseScale + _offsetX, -y * _baseScale + _offsetY);
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

    private string? FindVehicleAtScreen(Point screenPos)
    {
        const double hitRadius = 18;
        string? closest = null;
        double closestDist = double.MaxValue;

        foreach (var (vehicleId, vehicleScreenPos) in _cachedVehicleScreenPositions)
        {
            double dx = screenPos.X - vehicleScreenPos.X;
            double dy = screenPos.Y - vehicleScreenPos.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < hitRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = vehicleId;
            }
        }
        return closest;
    }

    /// <summary>
    /// 화면 좌표에서 가장 가까운 Link ID를 찾음 (점-선분 거리 6px 이내).
    /// </summary>
    private string? FindLinkAtScreen(Point screenPos)
    {
        const double hitThreshold = 6;
        string? closest = null;
        double closestDist = double.MaxValue;

        foreach (var (id, from, to) in _cachedLinkScreenSegments)
        {
            double dist = DistancePointToSegment(screenPos, from, to);
            if (dist < hitThreshold && dist < closestDist)
            {
                closestDist = dist;
                closest = id;
            }
        }
        return closest;
    }

    /// <summary>
    /// 화면 좌표에서 가장 가까운 Port(LocationId)를 찾음 (반경 10px 이내, Edit 모드에서만 캐시됨).
    /// </summary>
    private string? FindPortAtScreen(Point screenPos)
    {
        const double hitRadius = 10;
        string? closest = null;
        double closestDist = double.MaxValue;

        foreach (var (portId, portScreenPos) in _cachedPortScreenPositions)
        {
            double dx = screenPos.X - portScreenPos.X;
            double dy = screenPos.Y - portScreenPos.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < hitRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = portId;
            }
        }
        return closest;
    }

    /// <summary>점 p 와 선분 a-b 사이 최단 거리.</summary>
    private static double DistancePointToSegment(Point p, Point a, Point b)
    {
        double vx = b.X - a.X, vy = b.Y - a.Y;
        double wx = p.X - a.X, wy = p.Y - a.Y;
        double c1 = vx * wx + vy * wy;
        if (c1 <= 0) return Math.Sqrt(wx * wx + wy * wy);
        double c2 = vx * vx + vy * vy;
        if (c2 <= c1)
        {
            double dxb = p.X - b.X, dyb = p.Y - b.Y;
            return Math.Sqrt(dxb * dxb + dyb * dyb);
        }
        double t = c1 / c2;
        double projX = a.X + t * vx, projY = a.Y + t * vy;
        double ddx = p.X - projX, ddy = p.Y - projY;
        return Math.Sqrt(ddx * ddx + ddy * ddy);
    }

    /// <summary>
    /// 화면 좌표를 월드 좌표(m)로 역변환. rotation/zoom/pan 보정 포함.
    /// </summary>
    private (double X, double Y) ScreenToWorld(Point screenPoint)
    {
        // 회전 역변환 (화면 중심 기준)
        double cx = Bounds.Width / 2;
        double cy = Bounds.Height / 2;
        double cos = Math.Cos(-_rotation);
        double sin = Math.Sin(-_rotation);
        double rx = (screenPoint.X - cx) * cos - (screenPoint.Y - cy) * sin + cx;
        double ry = (screenPoint.X - cx) * sin + (screenPoint.Y - cy) * cos + cy;
        // pan과 zoom 역변환
        double x = (rx - _pan.X) / _zoom;
        double y = (ry - _pan.Y) / _zoom;
        // offset과 scale 역변환 (Y축은 반전: 월드 +Y는 위쪽)
        double worldX = (x - _offsetX) / _baseScale;
        double worldY = (_offsetY - y) / _baseScale;
        return (Math.Round(worldX, 3), Math.Round(worldY, 3));
    }

    private void DrawLinks(DrawingContext context, IReadOnlyList<LinkDto> links,
        Dictionary<string, Point> nodePositions, double linkWidth)
    {
        bool editSel = _viewModel?.IsEditMode == true && _viewModel.SelectedEntityType == "Link";
        string? selId = _viewModel?.SelectedEntityId;

        foreach (var link in links)
        {
            if (!nodePositions.TryGetValue(link.FromNodeId ?? "", out var from)) continue;
            if (!nodePositions.TryGetValue(link.ToNodeId ?? "", out var to)) continue;

            bool isSelected = editSel && link.Id == selId;
            IBrush brush = link.Availability switch
            {
                "1" => LinkUnavailableBrush,
                "2" => LinkBannedBrush,
                _ => LinkAvailableBrush
            };
            var pen = isSelected
                ? new Pen(SelectionBrush, linkWidth * 3)
                : new Pen(brush, linkWidth);

            context.DrawLine(pen, from, to);
        }
    }

    private void DrawNodes(DrawingContext context, IReadOnlyList<NodeDto> nodes,
        Dictionary<string, Point> nodePositions,
        Dictionary<string, List<LinkDto>> outgoingLinks,
        Dictionary<string, List<LinkDto>> incomingLinks,
        double size, double fontSize)
    {
        bool isLinkMode = _viewModel?.IsLinkSelectionMode == true;
        string? hoveredId = _viewModel?.HoveredNodeId;
        string? fromId = _viewModel?.SelectedFromNodeId;
        bool editNodeSel = _viewModel?.IsEditMode == true && _viewModel.SelectedEntityType == "Node";
        string? editSelId = _viewModel?.SelectedEntityId;
        double penWidth = Math.Clamp(1.5 / _zoom, 0.05, 100);

        foreach (var node in nodes)
        {
            if (!nodePositions.TryGetValue(node.Id, out var pos)) continue;

            bool isHovered = isLinkMode && node.Id == hoveredId;
            bool isSelectedFrom = isLinkMode && node.Id == fromId;
            bool isEditSelected = editNodeSel && node.Id == editSelId;

            // border 색상: Edit 선택 > Link 모드 하이라이트 > 타입별 색상
            IPen borderPen = isEditSelected ? new Pen(SelectionBrush, penWidth * 2.5)
                           : isSelectedFrom ? new Pen(NodeSelectedFromPen.Brush, penWidth * 2)
                           : isHovered ? new Pen(Brushes.White, penWidth * 2)
                           : new Pen(GetNodeBrush(node.Type), penWidth);

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

                        // 사각형 내부 정삼각형 화살표
                        double h = size * 1.3;                      // 삼각형 높이
                        double halfBase = h / Math.Sqrt(3);         // 정삼각형: 밑변/2 = h/√3
                        double tipDist = h * 2.0 / 3.0;            // 무게중심→꼭짓점
                        double baseDist = h / 3.0;                  // 무게중심→밑변
                        double tipX = pos.X + ux * tipDist;
                        double tipY = pos.Y + uy * tipDist;
                        double bx = pos.X - ux * baseDist;
                        double by = pos.Y - uy * baseDist;
                        double px = -uy * halfBase;
                        double py = ux * halfBase;

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

            // Node ID 라벨 표시 (화면에서 충분히 클 때만)
            double screenSize = size * _zoom;
            if (screenSize >= 4 || isHovered || isSelectedFrom)
            {
                var labelBrush = isSelectedFrom ? NodeSelectedFromPen.Brush : GetNodeBrush(node.Type);
                var label = new FormattedText(node.Id ?? "",
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, DefaultTypeface, fontSize, labelBrush);
                context.DrawText(label, new Point(pos.X + size + size * 0.3, pos.Y - fontSize * 0.7));
            }
        }
    }

    /// <summary>
    /// Station 마커: 해당 Link의 FromNode 위치에서 Direction에 따른 방향에 사각형 표시
    /// </summary>
    private void DrawStations(DrawingContext context, IReadOnlyList<LinkDto> links,
        Dictionary<string, Point> nodePositions,
        Dictionary<string, List<StationDto>> stationsByLink,
        Dictionary<string, List<string>> locationsByStation,
        double nodeSize, double fontSize)
    {
        double size = nodeSize;
        double penWidth = Math.Clamp(1.5 / _zoom, 0.05, 100);
        double gap = penWidth;

        bool editMode = _viewModel?.IsEditMode == true;
        string? selType = _viewModel?.SelectedEntityType;
        string? selId = _viewModel?.SelectedEntityId;

        _cachedStationScreenPositions.Clear();
        _cachedPortScreenPositions.Clear();

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

            double ux = dx / len;
            double uy = dy / len;

            var directionIndex = new Dictionary<string, int>();

            foreach (var station in stList)
            {
                var dir = (station.Direction ?? "").ToUpperInvariant();

                // 링크 진행 방향 기준 offset 방향 벡터 (원시)
                double rawOx, rawOy;
                bool isDiagonal = false;
                switch (dir)
                {
                    case "RIGHT":
                        rawOx = -uy;
                        rawOy = ux;
                        break;
                    case "LEFTBACK":
                        rawOx = (uy - ux) * 0.707;
                        rawOy = (-ux - uy) * 0.707;
                        isDiagonal = true;
                        break;
                    case "RIGHTBACK":
                        rawOx = (-uy - ux) * 0.707;
                        rawOy = (ux - uy) * 0.707;
                        isDiagonal = true;
                        break;
                    default: // LEFT
                        rawOx = uy;
                        rawOy = -ux;
                        break;
                }

                if (!directionIndex.TryGetValue(dir, out int idx))
                    idx = 0;
                directionIndex[dir] = idx + 1;

                // 축 정렬 사각형끼리 edge-flush 배치
                // 원시 방향을 cardinal (상/하/좌/우) 또는 diagonal로 snap
                double unitOffset = size * 2 + gap;
                double stX, stY;
                if (isDiagonal)
                {
                    // LEFTBACK/RIGHTBACK: 양축 모두 offset (대각선 코너)
                    double sx = rawOx >= 0 ? 1 : -1;
                    double sy = rawOy >= 0 ? 1 : -1;
                    stX = from.X + sx * unitOffset * (1 + idx);
                    stY = from.Y + sy * unitOffset * (1 + idx);
                }
                else
                {
                    // LEFT/RIGHT: 주 축 방향으로 snap하여 edge 정렬
                    if (Math.Abs(rawOx) > Math.Abs(rawOy))
                    {
                        stX = from.X + (rawOx >= 0 ? 1 : -1) * unitOffset * (1 + idx);
                        stY = from.Y;
                    }
                    else
                    {
                        stX = from.X;
                        stY = from.Y + (rawOy >= 0 ? 1 : -1) * unitOffset * (1 + idx);
                    }
                }

                // 화면 좌표 캐싱 (히트테스트용, 회전 포함)
                {
                    double ssx = stX * _zoom + _pan.X;
                    double ssy = stY * _zoom + _pan.Y;
                    double cos = Math.Cos(_rotation);
                    double sin = Math.Sin(_rotation);
                    double scx = Bounds.Width / 2;
                    double scy = Bounds.Height / 2;
                    _cachedStationScreenPositions[station.Id] = new Point(
                        (ssx - scx) * cos - (ssy - scy) * sin + scx,
                        (ssx - scx) * sin + (ssy - scy) * cos + scy);
                }

                bool isHovered = station.Id == _hoveredStationId;
                bool isEditSelected = editMode && selType == "Station" && station.Id == selId;

                // 사각형 마커 (Edit 선택 > hover > 기본)
                IPen stationPen = isEditSelected ? new Pen(SelectionBrush, penWidth * 2.5)
                                : isHovered ? new Pen(StationBrush, penWidth * 1.7)
                                : new Pen(StationPen.Brush, penWidth);
                context.DrawRectangle(Brushes.White, stationPen,
                    new Rect(stX - size, stY - size, size * 2, size * 2));

                // Edit 모드: Station 하위 Port(Location) 를 개별 마커로 렌더 + 히트테스트 캐시
                if (editMode &&
                    locationsByStation.TryGetValue(station.Id, out var portIds) && portIds.Count > 0)
                {
                    double portSize = size * 0.62;
                    double portGap = portSize * 2 + gap;
                    for (int pi = 0; pi < portIds.Count; pi++)
                    {
                        // Station 아래쪽에 가로로 나란히 배치 (중앙 정렬)
                        double pX = stX + (pi - (portIds.Count - 1) / 2.0) * portGap;
                        double pY = stY + size + portSize + gap;

                        // 화면 좌표 캐싱 (회전 포함) — Station 캐싱과 동일 변환
                        {
                            double psx = pX * _zoom + _pan.X;
                            double psy = pY * _zoom + _pan.Y;
                            double cos = Math.Cos(_rotation);
                            double sin = Math.Sin(_rotation);
                            double scx = Bounds.Width / 2;
                            double scy = Bounds.Height / 2;
                            _cachedPortScreenPositions[portIds[pi]] = new Point(
                                (psx - scx) * cos - (psy - scy) * sin + scx,
                                (psx - scx) * sin + (psy - scy) * cos + scy);
                        }

                        bool portSelected = selType == "Port" && portIds[pi] == selId;
                        IPen portMarkerPen = portSelected
                            ? new Pen(SelectionBrush, penWidth * 2.5)
                            : new Pen(PortPen.Brush, penWidth);
                        context.DrawRectangle(PortFillBrush, portMarkerPen,
                            new Rect(pX - portSize, pY - portSize, portSize * 2, portSize * 2));
                    }
                }

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
                        FlowDirection.LeftToRight, DefaultTypeface, fontSize, StationBrush);
                    context.DrawText(label, new Point(stX + size + size * 0.3, stY - fontSize * 0.7));
                }
            }
        }
    }

    private void DrawVehicles(DrawingContext context, IReadOnlyList<VehicleDto> vehicles,
        Dictionary<string, Point> nodePositions, double radius, double fontSize)
    {
        double penWidth = Math.Clamp(2.0 / _zoom, 0.1, 100);
        var outlinePen = new Pen(VehicleOutlinePen.Brush, penWidth);

        _cachedVehicleScreenPositions.Clear();

        foreach (var vehicle in vehicles)
        {
            // SignalR로 수신한 실시간 POSE가 있으면 우선 사용, 없으면 CurrentNodeId 위치로 폴백
            Point pos;
            if (vehicle.PoseX.HasValue && vehicle.PoseY.HasValue)
                pos = TransformPoint(vehicle.PoseX.Value, vehicle.PoseY.Value);
            else if (nodePositions.TryGetValue(vehicle.CurrentNodeId ?? "", out var nodePos))
                pos = nodePos;
            else
                continue;

            // 히트테스트용 화면 좌표 캐싱 (회전/줌/팬 모두 반영)
            if (!string.IsNullOrEmpty(vehicle.VehicleId))
            {
                double ssx = pos.X * _zoom + _pan.X;
                double ssy = pos.Y * _zoom + _pan.Y;
                double cosR = Math.Cos(_rotation);
                double sinR = Math.Sin(_rotation);
                double scx = Bounds.Width / 2;
                double scy = Bounds.Height / 2;
                _cachedVehicleScreenPositions[vehicle.VehicleId] = new Point(
                    (ssx - scx) * cosR - (ssy - scy) * sinR + scx,
                    (ssx - scx) * sinR + (ssy - scy) * cosR + scy);
            }

            IBrush brush = GetVehicleBrush(vehicle);

            // Vehicle circle
            context.DrawEllipse(brush, outlinePen, pos, radius, radius);

            // 헤딩 표시: 월드 프레임 (cos θ, sin θ) 방향, 길이는 radius와 함께 줌 보정됨
            // 월드 +Y는 위쪽이므로 화면 Y는 반전 (- sin θ)
            if (vehicle.PoseAngle.HasValue)
            {
                double a = vehicle.PoseAngle.Value;
                double headingLen = radius * 1.6;
                var tip = new Point(pos.X + Math.Cos(a) * headingLen, pos.Y - Math.Sin(a) * headingLen);
                context.DrawLine(new Pen(Brushes.White, penWidth * 1.5), pos, tip);
            }

            // Vehicle ID label
            double labelSize = fontSize * 1.1;
            var text = new FormattedText(
                vehicle.VehicleId ?? "?",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                DefaultTypeface,
                labelSize,
                Brushes.White);

            context.DrawText(text, new Point(pos.X - text.Width / 2, pos.Y - text.Height / 2));

            // Battery indicator bar below vehicle
            double barWidth = radius * 1.4;
            double barHeight = radius * 0.2;
            double barY = pos.Y + radius + radius * 0.2;
            double fillWidth = barWidth * vehicle.BatteryRate / 100.0;

            context.DrawRectangle(new SolidColorBrush(Color.FromRgb(60, 70, 85)), null,
                new Rect(pos.X - barWidth / 2, barY, barWidth, barHeight));

            IBrush batteryBrush = vehicle.BatteryRate >= 70 ? Brushes.LimeGreen :
                                  vehicle.BatteryRate >= 30 ? Brushes.Gold : Brushes.Red;
            context.DrawRectangle(batteryBrush, null,
                new Rect(pos.X - barWidth / 2, barY, fillWidth, barHeight));
        }
    }

    /// <summary>
    /// 우하단에 현재 스케일 비율 표시
    /// </summary>
    private void DrawScaleIndicator(DrawingContext context)
    {
        double es = EffectiveScale;
        string scaleText;
        if (es >= 500)
            scaleText = $"1px = {1000.0 / es:F1}mm";
        else if (es >= 1)
            scaleText = $"1px = {1.0 / es:F2}m";
        else
            scaleText = $"1px = {1.0 / es:F0}m";

        double degrees = _rotation * 180.0 / Math.PI;
        degrees = ((degrees % 360) + 360) % 360;
        scaleText += $"  |  {degrees:F1}°";

        var ft = new FormattedText(scaleText,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, DefaultTypeface, 11,
            new SolidColorBrush(Color.FromRgb(150, 158, 175)));
        context.DrawText(ft, new Point(Bounds.Width - ft.Width - 10, Bounds.Height - ft.Height - 8));
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
