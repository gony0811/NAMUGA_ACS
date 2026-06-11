using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ACS.UI.Views;

public partial class ResetPasswordDialog : Window
{
    public string NewPassword { get; private set; } = "";

    public ResetPasswordDialog()
    {
        InitializeComponent();
    }

    public ResetPasswordDialog(string userId) : this()
    {
        HeaderText.Text = $"'{userId}' 계정의 비밀번호를 새로 설정합니다.\n" +
                          $"리셋 후 대상 사용자는 최초 로그인 시 비밀번호 변경이 강제됩니다.";
        NewTextBox.Focus();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        var n1 = NewTextBox.Text ?? "";
        var n2 = ConfirmTextBox.Text ?? "";
        if (n1.Length < 4)
        {
            ShowError("새 비밀번호는 4자 이상이어야 합니다.");
            return;
        }
        if (n1 != n2)
        {
            ShowError("비밀번호 확인이 일치하지 않습니다.");
            return;
        }
        NewPassword = n1;
        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close(false);

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.IsVisible = true;
    }
}
