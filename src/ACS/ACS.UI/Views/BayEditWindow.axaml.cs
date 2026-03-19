using Avalonia.Controls;
using Avalonia.Interactivity;
using ACS.UI.Models;

namespace ACS.UI.Views;

public partial class BayEditWindow : Window
{
    public BayDto Bay { get; private set; }
    public bool IsEditMode { get; set; }

    public BayEditWindow()
    {
        InitializeComponent();
    }

    public BayEditWindow(BayDto bay, bool isEditMode) : this()
    {
        IsEditMode = isEditMode;
        Bay = new BayDto
        {
            Id = bay.Id ?? "",
            OriginalId = bay.Id ?? "",
            Floor = bay.Floor,
            Description = bay.Description ?? "",
            AgvType = bay.AgvType ?? "",
            ChargeVoltage = bay.ChargeVoltage,
            LimitVoltage = bay.LimitVoltage,
            IdleTime = bay.IdleTime,
            ZoneMove = bay.ZoneMove ?? "",
            Traffic = bay.Traffic ?? "",
            StopOut = bay.StopOut ?? ""
        };

        IdTextBox.Text = Bay.Id;
        FloorNumeric.Value = Bay.Floor;
        DescriptionTextBox.Text = Bay.Description;
        AgvTypeTextBox.Text = Bay.AgvType;
        ChargeVoltageNumeric.Value = (decimal)Bay.ChargeVoltage;
        LimitVoltageNumeric.Value = (decimal)Bay.LimitVoltage;
        IdleTimeNumeric.Value = Bay.IdleTime;
        ZoneMoveTextBox.Text = Bay.ZoneMove;
        TrafficTextBox.Text = Bay.Traffic;
        StopOutTextBox.Text = Bay.StopOut;

        Title = isEditMode ? "Modify Bay" : "Add Bay";
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Bay.Id = IdTextBox.Text ?? "";
        Bay.Floor = (int)(FloorNumeric.Value ?? 0);
        Bay.Description = DescriptionTextBox.Text ?? "";
        Bay.AgvType = AgvTypeTextBox.Text ?? "";
        Bay.ChargeVoltage = (float)(ChargeVoltageNumeric.Value ?? 0);
        Bay.LimitVoltage = (float)(LimitVoltageNumeric.Value ?? 0);
        Bay.IdleTime = (int)(IdleTimeNumeric.Value ?? 0);
        Bay.ZoneMove = ZoneMoveTextBox.Text ?? "";
        Bay.Traffic = TrafficTextBox.Text ?? "";
        Bay.StopOut = StopOutTextBox.Text ?? "";
        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
