using Avalonia.Controls;
using Avalonia.Interactivity;
using ACS.UI.Services;

namespace ACS.UI.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly IAcsApiService _api;
    private readonly bool _forced;

    public ChangePasswordWindow()
    {
        InitializeComponent();
    }

    /// <param name="forced">true면 최초 로그인 시 강제 변경(취소 불가).</param>
    public ChangePasswordWindow(IAcsApiService api, bool forced) : this()
    {
        _api = api;
        _forced = forced;

        if (forced)
        {
            HeaderText.Text = "비밀번호를 변경하세요";
            SubText.Text = "최초 로그인입니다. 새 비밀번호를 설정해야 사용할 수 있습니다.";
            CancelButton.Content = "로그아웃";
        }
        else
        {
            HeaderText.Text = "비밀번호 변경";
            SubText.Text = "현재 비밀번호와 새 비밀번호를 입력하세요.";
        }
        CurrentTextBox.Focus();
    }

    private async void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (_api == null) return;

        var cur = CurrentTextBox.Text ?? "";
        var n1 = NewTextBox.Text ?? "";
        var n2 = ConfirmTextBox.Text ?? "";

        if (string.IsNullOrEmpty(cur) || string.IsNullOrEmpty(n1))
        {
            ShowError("현재/새 비밀번호를 모두 입력하세요.");
            return;
        }
        if (n1.Length < 4)
        {
            ShowError("새 비밀번호는 4자 이상이어야 합니다.");
            return;
        }
        if (n1 != n2)
        {
            ShowError("새 비밀번호 확인이 일치하지 않습니다.");
            return;
        }
        if (cur == n1)
        {
            ShowError("새 비밀번호는 현재 비밀번호와 달라야 합니다.");
            return;
        }

        OkButton.IsEnabled = false;
        try
        {
            var ok = await _api.ChangePasswordAsync(cur, n1);
            if (!ok)
            {
                ShowError("비밀번호 변경 실패. 현재 비밀번호를 확인하세요.");
                return;
            }
            Close(true);
        }
        finally
        {
            OkButton.IsEnabled = true;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close(false);

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.IsVisible = true;
    }
}
