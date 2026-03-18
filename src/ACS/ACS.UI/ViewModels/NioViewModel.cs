using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Models;

namespace ACS.UI.ViewModels;

/// <summary>
/// NIO View 화면 ViewModel
/// AGV(Vehicle) Interface 조회/등록/수정/삭제
/// </summary>
public partial class NioViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<NioItemModel> _nioItems = new();

    [ObservableProperty]
    private NioItemModel? _selectedNio;

    public NioViewModel()
    {
        LoadSampleData();
    }

    [RelayCommand]
    private void AddNio()
    {
        // TODO: NIO 추가 다이얼로그
    }

    [RelayCommand]
    private void Refresh()
    {
        // TODO: 실제 API 호출로 NIO 목록 갱신
    }

    /// <summary>
    /// 샘플 데이터 로드 (매뉴얼 기반)
    /// </summary>
    private void LoadSampleData()
    {
        var interfaceClass = "kr.co.samsung.sds.acs.communication.socket.nio.SocketClient";

        for (int i = 1; i <= 20; i++)
        {
            NioItems.Add(new NioItemModel
            {
                Id = i.ToString("D3"),
                Name = "",
                InterfaceClassName = interfaceClass,
                WorkflowManagerName = "workflowManager",
                ApplicationName = "ES01_P",
                Port = 4001,
                RemoteIp = $"127.0.{i / 10}.{i % 10 + 1}",
                MachineName = "WIFI",
                State = i <= 15 ? "CONNECTED" : "DISCONNECTED",
                Description = "",
                CreateTime = i <= 5 ? DateTime.Parse("2017-08-08 09:44:09") : null
            });
        }
    }
}
