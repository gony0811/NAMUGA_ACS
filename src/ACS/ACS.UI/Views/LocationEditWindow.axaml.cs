using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ACS.UI.Models;

namespace ACS.UI.Views;

public partial class LocationEditWindow : Window
{
    public LocationDto Location { get; private set; }
    public bool IsEditMode { get; set; }

    public LocationEditWindow()
    {
        InitializeComponent();
    }

    public LocationEditWindow(LocationDto location, List<StationDto> stations, bool isEditMode) : this()
    {
        IsEditMode = isEditMode;
        Location = new LocationDto
        {
            LocationId = location.LocationId ?? "",
            OriginalLocationId = location.LocationId ?? "",
            StationId = location.StationId ?? "",
            Type = location.Type ?? "BUFFER",
            CarrierType = location.CarrierType ?? "",
            State = location.State ?? "",
            Direction = location.Direction ?? "LEFT"
        };

        LocationIdTextBox.Text = Location.LocationId;

        // Station ID ComboBox
        var stationIds = stations.Select(s => s.Id).OrderBy(id => id).ToList();
        StationIdComboBox.ItemsSource = stationIds;
        if (!string.IsNullOrEmpty(Location.StationId) && stationIds.Contains(Location.StationId))
            StationIdComboBox.SelectedItem = Location.StationId;
        else if (stationIds.Count > 0)
            StationIdComboBox.SelectedIndex = 0;

        // Type ComboBox
        SelectComboBoxItem(TypeComboBox, Location.Type);

        CarrierTypeTextBox.Text = Location.CarrierType;
        StateTextBox.Text = Location.State;

        // Direction ComboBox
        SelectComboBoxItem(DirectionComboBox, Location.Direction);

        Title = isEditMode ? "Modify Location" : "Add Location";
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

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Location.LocationId = LocationIdTextBox.Text ?? "";
        Location.StationId = StationIdComboBox.SelectedItem?.ToString() ?? "";
        Location.Type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "BUFFER";
        Location.CarrierType = CarrierTypeTextBox.Text ?? "";
        Location.State = StateTextBox.Text ?? "";
        Location.Direction = (DirectionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "LEFT";
        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
