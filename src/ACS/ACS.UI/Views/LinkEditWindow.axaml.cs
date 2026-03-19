using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ACS.UI.Models;

namespace ACS.UI.Views;

public partial class LinkEditWindow : Window
{
    public LinkDto Link { get; private set; }
    public bool IsEditMode { get; set; }

    /// <summary>
    /// 편집 결과 LinkZone 목록 (OK 클릭 시 이 목록 기준으로 서버 동기화)
    /// </summary>
    public ObservableCollection<LinkZoneDto> LinkZones { get; private set; } = new();

    /// <summary>
    /// 원본 LinkZone 목록 (차이 비교용)
    /// </summary>
    public List<LinkZoneDto> OriginalLinkZones { get; private set; } = new();

    public LinkEditWindow()
    {
        InitializeComponent();
    }

    public LinkEditWindow(LinkDto link, List<ZoneDto> zones, List<LinkZoneDto> linkZones, bool isEditMode) : this()
    {
        IsEditMode = isEditMode;
        Link = new LinkDto
        {
            Id = link.Id ?? "",
            FromNodeId = link.FromNodeId ?? "",
            ToNodeId = link.ToNodeId ?? "",
            Availability = link.Availability ?? "0",
            Length = link.Length,
            Speed = link.Speed,
            LeftBranch = link.LeftBranch,
            Load = link.Load
        };

        IdTextBox.Text = Link.Id;
        IdTextBox.IsReadOnly = isEditMode;
        FromNodeIdTextBox.Text = Link.FromNodeId;
        ToNodeIdTextBox.Text = Link.ToNodeId;
        // Map에서 선택된 경우 FromNodeId/ToNodeId가 이미 채워져 있으면 읽기전용
        if (!string.IsNullOrEmpty(Link.FromNodeId))
            FromNodeIdTextBox.IsReadOnly = true;
        if (!string.IsNullOrEmpty(Link.ToNodeId))
            ToNodeIdTextBox.IsReadOnly = true;
        LeftBranchNumeric.Value = Link.LeftBranch;
        SpeedNumeric.Value = Link.Speed;
        DistanceNumeric.Value = Link.Length;
        LoadNumeric.Value = Link.Load;

        // Availability ComboBox 선택
        var avail = Link.Availability ?? "0";
        AvailabilityComboBox.SelectedIndex = avail switch
        {
            "1" => 1,
            "2" => 2,
            _ => 0
        };

        // Zone DataGrid
        ZoneDataGrid.ItemsSource = zones;

        // LinkZone DataGrid — 기존 항목 로드
        OriginalLinkZones = linkZones.Select(lz => new LinkZoneDto
        {
            Id = lz.Id, LinkId = lz.LinkId, ZoneId = lz.ZoneId, TransferFlag = lz.TransferFlag
        }).ToList();

        foreach (var lz in linkZones)
        {
            LinkZones.Add(new LinkZoneDto
            {
                Id = lz.Id, LinkId = lz.LinkId, ZoneId = lz.ZoneId, TransferFlag = lz.TransferFlag
            });
        }
        LinkZoneDataGrid.ItemsSource = LinkZones;

        Title = isEditMode ? "Modify Link" : "Add Link";
    }

    private void OnAddLinkZoneClick(object sender, RoutedEventArgs e)
    {
        var selectedZone = ZoneDataGrid.SelectedItem as ZoneDto;
        if (selectedZone == null) return;

        var linkId = IdTextBox.Text ?? "";
        if (string.IsNullOrEmpty(linkId)) return;

        // 이미 추가된 Zone인지 확인
        if (LinkZones.Any(lz => lz.ZoneId == selectedZone.Id))
            return;

        var newLinkZone = new LinkZoneDto
        {
            Id = $"{linkId}_{selectedZone.Id}",
            LinkId = linkId,
            ZoneId = selectedZone.Id,
            TransferFlag = "Y"
        };
        LinkZones.Add(newLinkZone);
    }

    private void OnRemoveLinkZoneClick(object sender, RoutedEventArgs e)
    {
        var selectedLinkZone = LinkZoneDataGrid.SelectedItem as LinkZoneDto;
        if (selectedLinkZone == null) return;

        LinkZones.Remove(selectedLinkZone);
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Link.Id = IdTextBox.Text ?? "";
        Link.FromNodeId = FromNodeIdTextBox.Text ?? "";
        Link.ToNodeId = ToNodeIdTextBox.Text ?? "";
        Link.LeftBranch = (int)(LeftBranchNumeric.Value ?? 0);
        Link.Speed = (int)(SpeedNumeric.Value ?? 0);
        Link.Length = (int)(DistanceNumeric.Value ?? 0);
        Link.Load = (int)(LoadNumeric.Value ?? 0);

        Link.Availability = AvailabilityComboBox.SelectedIndex switch
        {
            1 => "1",
            2 => "2",
            _ => "0"
        };

        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
