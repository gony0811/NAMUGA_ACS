using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ACS.Core.Resource.Model;

namespace ACS.AMR.Simulator.Views;

public partial class AddVehicleWindow : Window
{
    public VehicleExs Result { get; private set; }

    public AddVehicleWindow()
    {
        InitializeComponent();

        var addButton = this.FindControl<Button>("AddButton");
        var cancelButton = this.FindControl<Button>("CancelButton");

        addButton.Click += OnAddClick;
        cancelButton.Click += OnCancelClick;
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var vehicleId = this.FindControl<TextBox>("VehicleIdTextBox").Text?.Trim();
        if (string.IsNullOrEmpty(vehicleId))
        {
            // Vehicle ID 필수
            this.FindControl<TextBox>("VehicleIdTextBox").Focus();
            return;
        }

        var connectionCombo = this.FindControl<ComboBox>("ConnectionStateComboBox");
        var processingCombo = this.FindControl<ComboBox>("ProcessingStateComboBox");

        Result = new VehicleExs
        {
            VehicleId = vehicleId,
            BayId = this.FindControl<TextBox>("BayIdTextBox").Text?.Trim() ?? "",
            CurrentNodeId = this.FindControl<TextBox>("CurrentNodeIdTextBox").Text?.Trim() ?? "",
            ConnectionState = (connectionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "DISCONNECT",
            ProcessingState = (processingCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "IDLE",
            BatteryRate = (int)(this.FindControl<NumericUpDown>("BatteryRateUpDown").Value ?? 100),
            // 기본값 설정
            AlarmState = VehicleEx.ALARMSTATE_NOALARM,
            RunState = VehicleEx.RUNSTATE_STOP,
            FullState = VehicleEx.FULLSTATE_EMPTY,
            State = VehicleEx.STATE_INSTALLED,
            Installed = VehicleEx.INSTALL_INSTALLED,
            TransferState = VehicleEx.TRANSFERSTATE_NOTASSIGNED,
            BatteryVoltage = 25.0f,
            EventTime = DateTime.UtcNow,
            NodeCheckTime = DateTime.UtcNow,
            LastChargeTime = DateTime.UtcNow,
        };

        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
