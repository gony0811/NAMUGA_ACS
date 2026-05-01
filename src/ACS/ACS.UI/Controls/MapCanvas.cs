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

    // Node 드래그 이동
    private string? _draggingNodeId;
    private bool _isDraggingNode;

    // Coordinate transform: 월드(m) → 화면(px)
    // 기본 스케일: 1px = 1m. zoom으로 확대/축소 (최대 1px=1mm, 최소 1px=1m)
    private double _baseScale = 1.0; // fit-to-screen 기본 스케일 (px per meter)
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
    private static readonly IBrush VehicleDisconnectOutlineBrush = new SolidColorBrush(Color.FromRgb(220, 50, 50));

    // ProcessingState 라벨용 색상 (차량 원의 어두운 배경 위에서 가독성 확보)
    private static readonly IBrush ProcessingIdleBrush = new SolidColorBrush(Color.FromRgb(180, 220, 255));
    private static readonly IBrush ProcessingRunBrush = new SolidColorBrush(Color.FromRgb(180, 255, 180));
    private static readonly IBrush ProcessingChargeBrush = new SolidColorBrush(Color.FromRgb(255, 240, 160));
    private static readonly IBrush ProcessingParkBrush = new SolidColorBrush(Color.FromRgb(220, 200, 255));

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
            double dx = pos.X - _lastMousePos.X;
            double dy = pos.Y - _lastMousePos.Y;

            // _pan은 Render의 변환 스택에서 rotation 이전 단계에 적용되므로,
            // 화면 delta를 -_rotation 으로 역회전하여 누적해야 마우스 드래그 방향과
            // 콘텐츠 이동 방향이 일치한다.
            double cos = Math.Cos(-_rotation);
            double sin = Math.Sin(-_rotation);
            double rdx = dx * cos - dy * sin;
            double rdy = dx * sin + dy * cos;

            _pan = new Point(_pan.X + rdx, _pan.Y + rdy);
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

        _isPanning = false;
        _isRotating = false;
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

            // Draw links
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
                    var textPos = new Point(pos.X + s * 0.8, pos.Y - fontSize * 1.5);
                    using (context.PushTransform(
                        Matrix.CreateTranslation(-textPos.X, -textPos.Y)
                        * Matrix.CreateRotation(-_rotation)
                        * Matrix.CreateTranslation(textPos.X, textPos.Y)))
                    {
                        context.DrawText(label, textPos);
                    }
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
        _offsetY = Padding - minY * _baseScale + (availableH - rangeY * _baseScale) / 2;
    }

    /// <summary>
    /// 노드가 없을 때 기본 transform 설정 (1px = 0.1m = 10px/m, 원점 중앙)
    /// </summary>
    private void CalculateDefaultTransform()
    {
        _baseScale = 10; // 10px per meter → 1px = 0.1m
        _offsetX = Bounds.Width / 2;
        _offsetY = Bounds.Height / 2;
    }

    private Point TransformPoint(double x, double y)
    {
        return new Point(x * _baseScale + _offsetX, y * _baseScale + _offsetY);
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
        // offset과 scale 역변환
        double worldX = (x - _offsetX) / _baseScale;
        double worldY = (y - _offsetY) / _baseScale;
        return (Math.Round(worldX, 3), Math.Round(worldY, 3));
    }

    private void DrawLinks(DrawingContext context, IReadOnlyList<LinkDto> links,
        Dictionary<string, Point> nodePositions, double linkWidth)
    {
        foreach (var link in links)
        {
            if (!nodePositions.TryGetValue(link.FromNodeId ?? "", out var from)) continue;
            if (!nodePositions.TryGetValue(link.ToNodeId ?? "", out var to)) continue;

            IBrush brush = link.Availability switch
            {
                "1" => LinkUnavailableBrush,
                "2" => LinkBannedBrush,
                _ => LinkAvailableBrush
            };
            var pen = new Pen(brush, linkWidth);

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
        double penWidth = Math.Clamp(1.5 / _zoom, 0.05, 100);

        foreach (var node in nodes)
        {
            if (!nodePositions.TryGetValue(node.Id, out var pos)) continue;

            bool isHovered = isLinkMode && node.Id == hoveredId;
            bool isSelectedFrom = isLinkMode && node.Id == fromId;

            // border 색상: 타입별 색상 또는 하이라이트
            IPen borderPen = isSelectedFrom ? new Pen(NodeSelectedFromPen.Brush, penWidth * 2)
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
                var textPos = new Point(pos.X + size + size * 0.3, pos.Y - fontSize * 0.7);
                using (context.PushTransform(
                    Matrix.CreateTranslation(-textPos.X, -textPos.Y)
                    * Matrix.CreateRotation(-_rotation)
                    * Matrix.CreateTranslation(textPos.X, textPos.Y)))
                {
                    context.DrawText(label, textPos);
                }
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

                // 사각형 마커
                context.DrawRectangle(Brushes.White,
                    isHovered ? new Pen(StationBrush, penWidth * 1.7) : new Pen(StationPen.Brush, penWidth),
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
                        FlowDirection.LeftToRight, DefaultTypeface, fontSize, StationBrush);
                    var textPos = new Point(stX + size + size * 0.3, stY - fontSize * 0.7);
                    using (context.PushTransform(
                        Matrix.CreateTranslation(-textPos.X, -textPos.Y)
                        * Matrix.CreateRotation(-_rotation)
                        * Matrix.CreateTranslation(textPos.X, textPos.Y)))
                    {
                        context.DrawText(label, textPos);
                    }
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

            // Vehicle circle - ConnectionState 가 DISCONNECT 면 빨간 점선 외곽선
            bool isDisconnected = (vehicle.ConnectionState ?? "").ToUpperInvariant() == "DISCONNECT";
            IPen circlePen = isDisconnected
                ? new Pen(VehicleDisconnectOutlineBrush, penWidth * 1.5)
                {
                    DashStyle = new DashStyle(new[] { 2.0, 2.0 }, 0)
                }
                : outlinePen;
            context.DrawEllipse(brush, circlePen, pos, radius, radius);

            // 헤딩 표시: 월드 프레임 (cos θ, sin θ) 방향, 길이는 radius와 함께 줌 보정됨
            if (vehicle.PoseAngle.HasValue)
            {
                double a = vehicle.PoseAngle.Value;
                double headingLen = radius * 1.6;
                var tip = new Point(pos.X + Math.Cos(a) * headingLen, pos.Y + Math.Sin(a) * headingLen);
                context.DrawLine(new Pen(Brushes.White, penWidth * 1.5), pos, tip);
            }

            // Vehicle ID label + ProcessingState 2단 표시 (차량 원 안 중앙)
            double labelSize = fontSize * 1.1;
            var idText = new FormattedText(
                vehicle.VehicleId ?? "?",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                DefaultTypeface,
                labelSize,
                Brushes.White);

            string psStr = (vehicle.ProcessingState ?? "").ToUpperInvariant();
            FormattedText? psText = null;
            if (!string.IsNullOrEmpty(psStr))
            {
                psText = new FormattedText(
                    psStr,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    DefaultTypeface,
                    fontSize * 0.85,
                    GetProcessingStateBrush(psStr));
            }

            double totalH = idText.Height + (psText?.Height ?? 0);
            double startY = pos.Y - totalH / 2;
            var idPos = new Point(pos.X - idText.Width / 2, startY);

            // 차량 중심(pos) 기준으로 -_rotation 만큼 역회전하여 라벨 글자가 항상 화면 기준으로 똑바로 보이도록.
            using (context.PushTransform(
                Matrix.CreateTranslation(-pos.X, -pos.Y)
                * Matrix.CreateRotation(-_rotation)
                * Matrix.CreateTranslation(pos.X, pos.Y)))
            {
                context.DrawText(idText, idPos);
                if (psText != null)
                    context.DrawText(psText, new Point(pos.X - psText.Width / 2, startY + idText.Height));
            }

            // Battery indicator bar below vehicle
            double barWidth = radius * 1.4;
            double barHeight = radius * 0.2;
            double barY = pos.Y + radius + radius * 0.2;
            double fillWidth = barWidth * vehicle.BatteryRate / 100.0;

            context.DrawRectangle(new SolidColorBrush(Color.FromRgb(200, 200, 200)), null,
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
            new SolidColorBrush(Color.FromRgb(100, 100, 100)));
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

    private static IBrush GetProcessingStateBrush(string processingState) => processingState switch
    {
        "IDLE" => ProcessingIdleBrush,
        "RUN" => ProcessingRunBrush,
        "CHARGE" => ProcessingChargeBrush,
        "PARK" => ProcessingParkBrush,
        _ => Brushes.LightGray
    };
}
