using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS.UI.Services;

/// <summary>
/// 로그인한 사용자 정보 + Bearer 토큰을 앱 전역에서 보관하는 싱글톤.
/// AcsApiService(헤더 부착), MainWindowViewModel(권한 게이트), XAML 바인딩 양쪽에서 참조.
/// 모든 권한 플래그는 INPC 발생 — 로그인/로그아웃 시 UI 즉시 갱신.
/// </summary>
public partial class UserSession : ObservableObject
{
    public const string ROLE_ADMIN = "Admin";
    public const string ROLE_OPERATOR = "Operator";
    public const string ROLE_VIEWER = "Viewer";

    /// <summary>앱 전역에서 공유하는 단일 인스턴스 — XAML에서 x:Static 으로 바인딩 가능.
    /// 인스턴스 자체는 재할당되지 않고 Set/Clear 로 상태만 갱신되므로 바인딩이 끊어지지 않는다.</summary>
    public static UserSession Current { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAuthenticated))]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private string _token;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private string _userId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAdmin))]
    [NotifyPropertyChangedFor(nameof(IsOperator))]
    [NotifyPropertyChangedFor(nameof(IsViewer))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyPropertyChangedFor(nameof(CanManageUsers))]
    [NotifyPropertyChangedFor(nameof(CanUpdateUi))]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private string _role;

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
    public bool IsAdmin => string.Equals(Role, ROLE_ADMIN, StringComparison.OrdinalIgnoreCase);
    public bool IsOperator => string.Equals(Role, ROLE_OPERATOR, StringComparison.OrdinalIgnoreCase);
    public bool IsViewer => string.Equals(Role, ROLE_VIEWER, StringComparison.OrdinalIgnoreCase);

    // 데이터 CRUD: Admin / Operator
    public bool CanEdit => IsAdmin || IsOperator;
    // 사용자 관리: Admin
    public bool CanManageUsers => IsAdmin;
    // ACS.UI 자동 업데이트: Admin / Operator
    public bool CanUpdateUi => IsAdmin || IsOperator;

    public string DisplayName => string.IsNullOrEmpty(UserId) ? "(not logged in)" : $"{UserId} ({Role})";

    public void Set(string token, string userId, string role)
    {
        Token = token;
        UserId = userId;
        Role = role;
    }

    public void Clear()
    {
        Token = null;
        UserId = null;
        Role = null;
    }
}
