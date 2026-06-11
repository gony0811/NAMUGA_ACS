using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ACS.UI.Models;

namespace ACS.UI.Views;

public partial class UserEditWindow : Window
{
    public UserDto User { get; private set; } = new();
    public bool IsEditMode { get; private set; }

    public UserEditWindow()
    {
        InitializeComponent();
    }

    public UserEditWindow(UserDto src, bool isEditMode) : this()
    {
        IsEditMode = isEditMode;
        User = new UserDto
        {
            Seq = src.Seq,
            UserId = src.UserId ?? "",
            Role = string.IsNullOrEmpty(src.Role) ? "Viewer" : src.Role,
            IsActive = src.IsActive,
            MustChangePassword = src.MustChangePassword,
            Description = src.Description ?? "",
            CreateTime = src.CreateTime,
            EditTime = src.EditTime,
            Creator = src.Creator ?? "",
            Editor = src.Editor ?? "",
            LastLoginTime = src.LastLoginTime
        };

        UserIdTextBox.Text = User.UserId;
        ActiveCheck.IsChecked = User.IsActive;
        DescriptionTextBox.Text = User.Description;

        var roleMatch = RoleCombo.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(i => string.Equals(i.Content as string, User.Role, System.StringComparison.OrdinalIgnoreCase));
        RoleCombo.SelectedItem = roleMatch ?? RoleCombo.Items.OfType<ComboBoxItem>().FirstOrDefault();

        Title = isEditMode ? "Edit User" : "Add User";

        if (isEditMode)
        {
            // 편집 모드: UserId 고정, 비밀번호 입력란 숨김 (리셋은 별도 ResetPasswordDialog 사용)
            UserIdTextBox.IsReadOnly = true;
            PasswordLabel.IsVisible = false;
            PasswordTextBox.IsVisible = false;
            HintText.Text = "비밀번호 변경은 'Reset PW' 버튼을 사용하세요.";
        }
        else
        {
            UserIdTextBox.Focus();
        }
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        User.UserId = (UserIdTextBox.Text ?? "").Trim();
        if (string.IsNullOrEmpty(User.UserId))
        {
            UserIdTextBox.Focus();
            return;
        }

        User.Role = (RoleCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "Viewer";
        User.IsActive = ActiveCheck.IsChecked ?? true;
        User.Description = DescriptionTextBox.Text ?? "";

        if (!IsEditMode)
        {
            var pw = PasswordTextBox.Text ?? "";
            if (string.IsNullOrEmpty(pw))
            {
                PasswordTextBox.Focus();
                return;
            }
            if (pw.Length < 4)
            {
                PasswordTextBox.Focus();
                return;
            }
            User.InitialPassword = pw;
        }

        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close(false);
}
