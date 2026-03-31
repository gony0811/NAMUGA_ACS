using Avalonia.Controls;
using Avalonia.Interactivity;
using ACS.UI.Models;

namespace ACS.UI.Views;

public partial class NodeEditWindow : Window
{
    public NodeDto Node { get; private set; }
    public bool IsEditMode { get; set; }

    public NodeEditWindow()
    {
        InitializeComponent();
    }

    public NodeEditWindow(NodeDto node, bool isEditMode) : this()
    {
        IsEditMode = isEditMode;
        Node = new NodeDto
        {
            Id = node.Id ?? "",
            Type = node.Type ?? "COMMON",
            Xpos = node.Xpos,
            Ypos = node.Ypos,
            Zpos = node.Zpos
        };

        IdTextBox.Text = Node.Id;
        IdTextBox.IsReadOnly = isEditMode;

        // Type ComboBox 선택
        var typeItems = TypeComboBox.Items;
        for (int i = 0; i < typeItems.Count; i++)
        {
            if (typeItems[i] is ComboBoxItem item && item.Content?.ToString() == Node.Type)
            {
                TypeComboBox.SelectedIndex = i;
                break;
            }
        }
        if (TypeComboBox.SelectedIndex < 0)
            TypeComboBox.SelectedIndex = 0;

        XPosNumeric.Value = (decimal)Node.Xpos;
        YPosNumeric.Value = (decimal)Node.Ypos;

        Title = isEditMode ? "Modify Node" : "Add Node";
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Node.Id = IdTextBox.Text ?? "";
        Node.Type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "COMMON";
        Node.Xpos = (double)(XPosNumeric.Value ?? 0);
        Node.Ypos = (double)(YPosNumeric.Value ?? 0);
        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
