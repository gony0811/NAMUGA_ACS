using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ACS.UI.Models;
using ACS.UI.Services;

namespace ACS.UI.Views;

public partial class LoginWindow : Window
{
    private readonly IAcsApiService _api;
    private readonly UserSession _session;

    public LoginResult Result { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
    }

    public LoginWindow(IAcsApiService api, UserSession session) : this()
    {
        _api = api;
        _session = session;
        UserIdTextBox.Focus();
    }

    private async void OnLoginClick(object sender, RoutedEventArgs e)
    {
        if (_api == null || _session == null) return;

        var userId = UserIdTextBox.Text?.Trim() ?? "";
        var password = PasswordTextBox.Text ?? "";
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            ShowError("ID/비밀번호를 입력하세요.");
            return;
        }

        LoginButton.IsEnabled = false;
        try
        {
            var result = await _api.LoginAsync(userId, password);
            if (result == null || string.IsNullOrEmpty(result.Token))
            {
                ShowError("ID 또는 비밀번호가 올바르지 않거나 서버에 연결할 수 없습니다.");
                return;
            }

            _session.Set(result.Token, result.UserId, result.Role);
            Result = result;
            Close(true);
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close(false);

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.IsVisible = true;
    }
}
