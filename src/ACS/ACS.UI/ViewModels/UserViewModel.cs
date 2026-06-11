using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Models;
using ACS.UI.Services;
using ACS.UI.Views;

namespace ACS.UI.ViewModels;

/// <summary>
/// NA_X_USER CRUD. Admin 전용 화면이지만, 본인 비밀번호 변경 버튼은 모든 역할에서 노출.
/// </summary>
public partial class UserViewModel : ObservableObject
{
    private readonly IAcsApiService _apiService;
    private readonly UserSession _session;

    public ObservableCollection<UserDto> Users { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditUserCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteUserCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetPasswordCommand))]
    private UserDto? _selectedUser;

    [ObservableProperty]
    private int _totalCount;

    public UserViewModel(IAcsApiService apiService, UserSession session = null)
    {
        _apiService = apiService;
        _session = session;
    }

    [RelayCommand]
    public async Task LoadUsersAsync()
    {
        if (_apiService == null) return;
        try
        {
            var users = await _apiService.GetUsersAsync();
            Users.Clear();
            foreach (var u in users) Users.Add(u);
            TotalCount = Users.Count;
        }
        catch { }
    }

    [RelayCommand]
    private async Task AddUserAsync()
    {
        if (_apiService == null) return;
        var dialog = new UserEditWindow(new UserDto { Role = UserSession.ROLE_VIEWER, IsActive = true }, isEditMode: false);
        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var ok = await _apiService.CreateUserAsync(dialog.User);
            if (ok) await LoadUsersAsync();
            else ShowMessage("사용자 생성 실패", "ID 중복 또는 입력 값을 확인하세요.");
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelected))]
    private async Task EditUserAsync()
    {
        if (_apiService == null || SelectedUser == null) return;
        var dialog = new UserEditWindow(SelectedUser, isEditMode: true);
        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var ok = await _apiService.UpdateUserAsync(dialog.User);
            if (ok) await LoadUsersAsync();
            else ShowMessage("사용자 수정 실패", "마지막 Admin은 강등/비활성화할 수 없습니다.");
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelected))]
    private async Task DeleteUserAsync()
    {
        if (_apiService == null || SelectedUser == null) return;

        var seq = SelectedUser.Seq;
        var name = SelectedUser.UserId;

        var msgBox = new Window
        {
            Title = "Delete User",
            Width = 360,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = CreateConfirmContent($"사용자 '{name}' (seq={seq}) 을(를) 삭제하시겠습니까?")
        };
        var result = await msgBox.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var ok = await _apiService.DeleteUserAsync(seq);
            if (ok) await LoadUsersAsync();
            else ShowMessage("사용자 삭제 실패", "본인 계정 또는 마지막 Admin은 삭제할 수 없습니다.");
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelected))]
    private async Task ResetPasswordAsync()
    {
        if (_apiService == null || SelectedUser == null) return;

        var dlg = new ResetPasswordDialog(SelectedUser.UserId);
        var ok = await dlg.ShowDialog<bool?>(GetOwnerWindow());
        if (ok != true) return;

        var dto = new UserDto
        {
            Seq = SelectedUser.Seq,
            UserId = SelectedUser.UserId,
            Role = SelectedUser.Role,
            IsActive = SelectedUser.IsActive,
            Description = SelectedUser.Description,
            InitialPassword = dlg.NewPassword
        };
        var saved = await _apiService.UpdateUserAsync(dto);
        if (saved) await LoadUsersAsync();
        else ShowMessage("비밀번호 리셋 실패", "백엔드 오류를 확인하세요.");
    }

    [RelayCommand]
    private async Task ChangeMyPasswordAsync()
    {
        if (_apiService == null) return;
        var dlg = new ChangePasswordWindow(_apiService, forced: false);
        await dlg.ShowDialog<bool?>(GetOwnerWindow());
    }

    private bool HasSelected() => SelectedUser != null;

    private static object CreateConfirmContent(string text)
    {
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = text, FontSize = 12, TextWrapping = Avalonia.Media.TextWrapping.Wrap });

        var btnPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 12
        };
        var okBtn = new Button { Content = "OK", Width = 80, Height = 28 };
        var cancelBtn = new Button { Content = "Cancel", Width = 80, Height = 28 };
        okBtn.Click += (s, e) => (s as Visual)?.FindAncestorOfType<Window>()?.Close(true);
        cancelBtn.Click += (s, e) => (s as Visual)?.FindAncestorOfType<Window>()?.Close(false);
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        panel.Children.Add(btnPanel);
        return panel;
    }

    private void ShowMessage(string title, string text)
    {
        var w = new Window
        {
            Title = title,
            Width = 340, Height = 120,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = text, FontSize = 12, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        var btn = new Button
        {
            Content = "OK", Width = 80, Height = 26,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        btn.Click += (s, e) => (s as Visual)?.FindAncestorOfType<Window>()?.Close();
        panel.Children.Add(btn);
        w.Content = panel;
        _ = w.ShowDialog(GetOwnerWindow());
    }

    private Window GetOwnerWindow()
    {
        return Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.FirstOrDefault(w => w.Title == "User Management" && w.IsVisible) ?? desktop.MainWindow!
            : null!;
    }
}
