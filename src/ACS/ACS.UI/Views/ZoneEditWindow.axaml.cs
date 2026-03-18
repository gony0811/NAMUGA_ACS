using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ACS.UI.Models;

namespace ACS.UI.Views;

public partial class ZoneEditWindow : Window
{
    public ZoneDto Zone { get; private set; }
    public bool IsEditMode { get; set; }

    public ZoneEditWindow()
    {
        InitializeComponent();
    }

    public ZoneEditWindow(ZoneDto zone, List<BayDto> bays, bool isEditMode) : this()
    {
        IsEditMode = isEditMode;
        Zone = new ZoneDto
        {
            Id = zone.Id ?? "",
            BayId = zone.BayId ?? "",
            Description = zone.Description ?? ""
        };

        IdTextBox.Text = Zone.Id;
        IdTextBox.IsReadOnly = isEditMode;
        DescriptionTextBox.Text = Zone.Description;

        // Bay 목록을 ComboBox에 설정
        var bayIds = bays.Select(b => b.Id).ToList();
        BayIdComboBox.ItemsSource = bayIds;

        // 기존 BayId 선택
        if (!string.IsNullOrEmpty(Zone.BayId) && bayIds.Contains(Zone.BayId))
        {
            BayIdComboBox.SelectedItem = Zone.BayId;
        }
        else if (bayIds.Count > 0)
        {
            BayIdComboBox.SelectedIndex = 0;
        }

        Title = isEditMode ? "Modify Zone" : "Add Zone";
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Zone.Id = IdTextBox.Text ?? "";
        Zone.BayId = BayIdComboBox.SelectedItem?.ToString() ?? "";
        Zone.Description = DescriptionTextBox.Text ?? "";
        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
