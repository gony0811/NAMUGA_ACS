using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ACS.UI.Models;

namespace ACS.UI.Views;

public partial class StationEditWindow : Window
{
    public StationDto Station { get; private set; }
    public bool IsEditMode { get; set; }

    private readonly List<LinkDto> _allLinks;

    public StationEditWindow()
    {
        InitializeComponent();
        _allLinks = new List<LinkDto>();
    }

    public StationEditWindow(StationDto station, List<NodeDto> nodes, List<LinkDto> links, bool isEditMode) : this()
    {
        IsEditMode = isEditMode;
        _allLinks = links ?? new List<LinkDto>();

        Station = new StationDto
        {
            Id = station.Id ?? "",
            LinkId = station.LinkId ?? "",
            Type = station.Type ?? "ACQUIRE",
            Distance = station.Distance,
            Direction = station.Direction ?? ""
        };

        // Node ID ComboBox 설정
        var nodeIds = nodes.Select(n => n.Id).OrderBy(id => id).ToList();
        IdComboBox.ItemsSource = nodeIds;

        if (isEditMode)
        {
            // 수정 모드: 기존 ID 선택, 변경 불가
            IdComboBox.SelectedItem = Station.Id;
            IdComboBox.IsEnabled = false;
        }
        else
        {
            // 추가 모드: 기존 ID가 있으면 선택
            if (!string.IsNullOrEmpty(Station.Id) && nodeIds.Contains(Station.Id))
                IdComboBox.SelectedItem = Station.Id;
            else if (nodeIds.Count > 0)
                IdComboBox.SelectedIndex = 0;
        }

        // Node 선택 변경 시 LinkId 필터링
        IdComboBox.SelectionChanged += OnNodeSelectionChanged;

        // 초기 Link 목록 필터링
        UpdateLinkComboBox(IdComboBox.SelectedItem?.ToString());

        // Type ComboBox 선택
        var typeItems = TypeComboBox.Items;
        for (int i = 0; i < typeItems.Count; i++)
        {
            if (typeItems[i] is ComboBoxItem item && item.Content?.ToString() == Station.Type)
            {
                TypeComboBox.SelectedIndex = i;
                break;
            }
        }
        if (TypeComboBox.SelectedIndex < 0)
            TypeComboBox.SelectedIndex = 0;

        DistanceNumeric.Value = Station.Distance;
        SelectComboBoxItem(DirectionComboBox, Station.Direction);

        Title = isEditMode ? "Modify Station" : "Add Station";
    }

    private void OnNodeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selectedNodeId = IdComboBox.SelectedItem?.ToString();
        UpdateLinkComboBox(selectedNodeId);
    }

    private void UpdateLinkComboBox(string? nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            LinkIdComboBox.ItemsSource = null;
            return;
        }

        // FromNodeId가 선택한 nodeId인 Link만 필터링
        var filteredLinkIds = _allLinks
            .Where(l => l.FromNodeId == nodeId)
            .Select(l => l.Id)
            .OrderBy(id => id)
            .ToList();

        LinkIdComboBox.ItemsSource = filteredLinkIds;

        // 기존 LinkId가 필터 목록에 있으면 선택
        if (!string.IsNullOrEmpty(Station.LinkId) && filteredLinkIds.Contains(Station.LinkId))
            LinkIdComboBox.SelectedItem = Station.LinkId;
        else if (filteredLinkIds.Count > 0)
            LinkIdComboBox.SelectedIndex = 0;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Station.Id = IdComboBox.SelectedItem?.ToString() ?? "";
        Station.LinkId = LinkIdComboBox.SelectedItem?.ToString() ?? "";
        Station.Type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "ACQUIRE";
        Station.Distance = (int)(DistanceNumeric.Value ?? 0);
        Station.Direction = (DirectionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "LEFT";
        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private static void SelectComboBoxItem(ComboBox comboBox, string value)
    {
        var items = comboBox.Items;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is ComboBoxItem item && item.Content?.ToString() == value)
            {
                comboBox.SelectedIndex = i;
                return;
            }
        }
        if (comboBox.SelectedIndex < 0 && items.Count > 0)
            comboBox.SelectedIndex = 0;
    }
}
