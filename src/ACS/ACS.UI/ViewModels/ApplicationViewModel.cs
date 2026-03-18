using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACS.UI.ViewModels;

public partial class ApplicationViewModel : ObservableObject
{
    // 현재 선택된 메뉴 항목
    [ObservableProperty]
    private string _selectedMenu = "";

    [ObservableProperty]
    private string _selectedCategory = "";

    /// <summary>
    /// 뷰 전환 요청 콜백 (MainWindowViewModel에서 설정)
    /// </summary>
    public Action<string>? OnViewChangeRequested { get; set; }

    [RelayCommand]
    private void SelectMenu(string menu)
    {
        SelectedMenu = menu;

        // 카테고리 자동 설정
        SelectedCategory = menu switch
        {
            "Application" => "Application",
            "NIO" => "NIO",
            _ => ""
        };

        // 메인 영역 뷰 전환 요청
        var viewName = menu switch
        {
            "Application" => "AppManagement",
            "NIO" => "Nio",
            _ => "Map"
        };
        OnViewChangeRequested?.Invoke(viewName);
    }
}
