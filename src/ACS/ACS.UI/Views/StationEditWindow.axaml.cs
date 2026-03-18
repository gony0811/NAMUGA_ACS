using Avalonia.Controls;
using Avalonia.Interactivity;
using ACS.UI.Models;

namespace ACS.UI.Views;

public partial class StationEditWindow : Window
{
    public StationDto Station { get; private set; }
    public bool IsEditMode { get; set; }

    public StationEditWindow()
    {
        InitializeComponent();
    }

    public StationEditWindow(StationDto station, bool isEditMode) : this()
    {
        IsEditMode = isEditMode;
        Station = new StationDto
        {
            Id = station.Id ?? "",
            LinkId = station.LinkId ?? "",
            Type = station.Type ?? "ACQUIRE",
            Distance = station.Distance,
            Direction = station.Direction ?? ""
        };

        IdTextBox.Text = Station.Id;
        IdTextBox.IsReadOnly = isEditMode;

        LinkIdTextBox.Text = Station.LinkId;

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
        DirectionTextBox.Text = Station.Direction;

        Title = isEditMode ? "Modify Station" : "Add Station";
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Station.Id = IdTextBox.Text ?? "";
        Station.LinkId = LinkIdTextBox.Text ?? "";
        Station.Type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "ACQUIRE";
        Station.Distance = (int)(DistanceNumeric.Value ?? 0);
        Station.Direction = DirectionTextBox.Text ?? "";
        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
