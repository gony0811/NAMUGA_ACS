using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACS.UI.ViewModels;

public partial class DataViewViewModel : ObservableObject
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
            "TrCmd View" => "Transfer",
            "Node View" or "Station View" or "Port View" or "Link View" => "Layout",
            "Bay View" or "LinkZone View" or "Zone View" => "Area",
            "Vehicle View" or "Vehicle CrossWait View" or "Alarm View" or "Alarm Spec View" => "Device",
            "Assign View" or "Route View" => "Assign/Route",
            "TCP" => "Host",
            _ => ""
        };

        // 메인 영역 뷰 전환 요청
        var viewName = menu switch
        {
            "TCP" => "HostCommunication",
            "Node View" => "Node",
            "Station View" => "Station",
            "Link View" => "Link",
            "Bay View" => "Bay",
            "Zone View" => "Zone",
            _ => (string?)null
        };
        if (viewName != null)
            OnViewChangeRequested?.Invoke(viewName);
    }
}
