using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ACS.UI.Models;

namespace ACS.UI.Views;

public partial class MqttEditWindow : Window
{
    public MqttConfigDto Config { get; private set; } = new();
    public bool IsEditMode { get; set; }

    public MqttEditWindow()
    {
        InitializeComponent();
    }

    public MqttEditWindow(MqttConfigDto config, bool isEditMode) : this()
    {
        IsEditMode = isEditMode;
        // Seq/timestamps/creator/editor 는 그대로 보존(서버가 자동 갱신).
        Config = new MqttConfigDto
        {
            Seq = config.Seq,
            Name = config.Name ?? "",
            ApplicationName = config.ApplicationName ?? "",
            WorkflowManagerName = config.WorkflowManagerName ?? "",
            BrokerIp = config.BrokerIp ?? "",
            BrokerPort = config.BrokerPort == 0 ? 1883 : config.BrokerPort,
            TopicPrefix = string.IsNullOrEmpty(config.TopicPrefix) ? "amr/" : config.TopicPrefix,
            ClientId = config.ClientId ?? "",
            UserName = config.UserName ?? "",
            Password = config.Password ?? "",
            KeepAliveSeconds = config.KeepAliveSeconds == 0 ? 30 : config.KeepAliveSeconds,
            ReconnectDelayMs = config.ReconnectDelayMs == 0 ? 5000 : config.ReconnectDelayMs,
            State = string.IsNullOrEmpty(config.State) ? "LOADED" : config.State,
            Description = config.Description ?? "",
            CreateTime = config.CreateTime,
            EditTime = config.EditTime,
            Creator = config.Creator ?? "",
            Editor = config.Editor ?? ""
        };

        NameTextBox.Text = Config.Name;
        ApplicationTextBox.Text = Config.ApplicationName;
        WorkflowMgrTextBox.Text = Config.WorkflowManagerName;
        BrokerIpTextBox.Text = Config.BrokerIp;
        BrokerPortNumeric.Value = Config.BrokerPort;
        TopicPrefixTextBox.Text = Config.TopicPrefix;
        ClientIdTextBox.Text = Config.ClientId;
        UserNameTextBox.Text = Config.UserName;
        PasswordTextBox.Text = Config.Password;
        KeepAliveNumeric.Value = Config.KeepAliveSeconds;
        ReconnectNumeric.Value = Config.ReconnectDelayMs;
        DescriptionTextBox.Text = Config.Description;

        var match = StateCombo.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(i => string.Equals(i.Content as string, Config.State, System.StringComparison.OrdinalIgnoreCase));
        StateCombo.SelectedItem = match ?? StateCombo.Items.OfType<ComboBoxItem>().FirstOrDefault();

        Title = isEditMode ? "Modify MQTT Config" : "Add MQTT Config";
        NameTextBox.IsReadOnly = isEditMode;   // Name 은 unique key 성격 — 편집 시 변경 금지
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Config.Name = NameTextBox.Text ?? "";
        if (string.IsNullOrWhiteSpace(Config.Name))
        {
            NameTextBox.Focus();
            return; // Name 은 필수
        }

        Config.ApplicationName = ApplicationTextBox.Text ?? "";
        Config.WorkflowManagerName = WorkflowMgrTextBox.Text ?? "";
        Config.BrokerIp = BrokerIpTextBox.Text ?? "";
        Config.BrokerPort = (int)(BrokerPortNumeric.Value ?? 1883);
        Config.TopicPrefix = TopicPrefixTextBox.Text ?? "";
        Config.ClientId = ClientIdTextBox.Text ?? "";
        Config.UserName = UserNameTextBox.Text ?? "";
        Config.Password = PasswordTextBox.Text ?? "";
        Config.KeepAliveSeconds = (int)(KeepAliveNumeric.Value ?? 30);
        Config.ReconnectDelayMs = (int)(ReconnectNumeric.Value ?? 5000);
        Config.State = (StateCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "LOADED";
        Config.Description = DescriptionTextBox.Text ?? "";

        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
