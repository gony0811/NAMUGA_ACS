using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACS.UI.Models;

namespace ACS.UI.ViewModels;

/// <summary>
/// Application Management 화면 ViewModel
/// Primary/Secondary 서버의 프로세스 트리 + Properties 패널
/// </summary>
public partial class AppManagementViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ProcessNodeModel> _primaryProcesses = new();

    [ObservableProperty]
    private ObservableCollection<ProcessNodeModel> _secondaryProcesses = new();

    [ObservableProperty]
    private ProcessNodeModel? _selectedProcess;

    [ObservableProperty]
    private ObservableCollection<PropertyItem> _selectedProperties = new();

    [ObservableProperty]
    private bool _autoRefreshEnabled;

    public AppManagementViewModel()
    {
        LoadSampleData();
    }

    partial void OnSelectedProcessChanged(ProcessNodeModel? value)
    {
        SelectedProperties.Clear();
        if (value?.Properties != null)
        {
            foreach (var kvp in value.Properties)
            {
                SelectedProperties.Add(new PropertyItem { Property = kvp.Key, Value = kvp.Value });
            }
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        // TODO: 실제 API 호출로 프로세스 목록 갱신
    }

    [RelayCommand]
    private void Delete()
    {
        // TODO: 선택된 inactive 프로세스 삭제
    }

    [RelayCommand]
    private void ToggleAutoRefresh()
    {
        AutoRefreshEnabled = !AutoRefreshEnabled;
    }

    /// <summary>
    /// 샘플 데이터 로드 (매뉴얼 기반)
    /// </summary>
    private void LoadSampleData()
    {
        // Primary 서버
        var primaryCs = new ProcessNodeModel
        {
            Name = "CS01_P", Type = "CS", State = "CS_ACTIVE",
            Properties = new Dictionary<string, string>
            {
                ["ID"] = "CS01_P", ["TYPE"] = "cs",
                ["STARTTIME"] = "2017-04-29 14:02:27",
                ["RUNNINGHARDWARE"] = "PRIMARY"
            }
        };

        var trans = new ProcessNodeModel { Name = "TRANS", Type = "TRANS_TYPE", State = "CS_ACTIVE" };
        for (int i = 1; i <= 7; i++)
        {
            trans.Children.Add(new ProcessNodeModel
            {
                Name = $"TS0{i}_P", Type = "ts", State = "STATE_ACTIVE",
                Properties = new Dictionary<string, string>
                {
                    ["ID"] = $"TS0{i}_P", ["TYPE"] = "ts",
                    ["STARTTIME"] = "2017-04-29 14:02:27",
                    ["RUNNINGHARDWARE"] = "PRIMARY",
                    ["APPLICATIONNAME"] = $"ACS/SDV/TS/TS0{i}_P",
                    ["NAME"] = $"TS0{i}_P"
                }
            });
        }

        var ei = new ProcessNodeModel { Name = "EI", Type = "EI_TYPE", State = "CS_ACTIVE" };
        for (int i = 1; i <= 7; i++)
        {
            var esNode = new ProcessNodeModel
            {
                Name = $"ES0{i}_P", Type = "es", State = i == 1 ? "STATE_INACTIVE" : "STATE_ACTIVE",
                Properties = new Dictionary<string, string>
                {
                    ["ID"] = $"ES0{i}_P", ["TYPE"] = "es",
                    ["STARTTIME"] = "2017-04-29 14:02:27",
                    ["RUNNINGHARDWARE"] = "PRIMARY",
                    ["APPLICATIONNAME"] = $"ACS/SDV/ES/ES0{i}_P",
                    ["NAME"] = $"ES0{i}_P"
                }
            };
            // EI 하위에 NIO 노드 추가
            if (i <= 3)
            {
                for (int n = 1; n <= 3; n++)
                {
                    esNode.Children.Add(new ProcessNodeModel
                    {
                        Name = $"{100 + (i - 1) * 3 + n}",
                        Type = "NIO", State = "NIO_CONNECTED",
                        Properties = new Dictionary<string, string>
                        {
                            ["ID"] = $"{100 + (i - 1) * 3 + n}",
                            ["TYPE"] = "NIO",
                            ["STATE"] = "CONNECTED",
                            ["MACHINENAME"] = "WIFI"
                        }
                    });
                }
            }
            ei.Children.Add(esNode);
        }

        var daemon = new ProcessNodeModel { Name = "DAEMON", Type = "DAEMON_TYPE", State = "CS_ACTIVE" };
        daemon.Children.Add(new ProcessNodeModel
        {
            Name = "DS01_P", Type = "daemon", State = "STATE_ACTIVE",
            Properties = new Dictionary<string, string>
            {
                ["ID"] = "DS01_P", ["TYPE"] = "daemon",
                ["STARTTIME"] = "2017-04-29 14:02:27",
                ["RUNNINGHARDWARE"] = "PRIMARY"
            }
        });

        primaryCs.Children.Add(trans);
        primaryCs.Children.Add(ei);
        primaryCs.Children.Add(daemon);
        PrimaryProcesses.Add(primaryCs);

        // Secondary 서버
        var secondaryCs = new ProcessNodeModel
        {
            Name = "CS01_S", Type = "CS", State = "CS_STANDBY",
            Properties = new Dictionary<string, string>
            {
                ["ID"] = "CS01_S", ["TYPE"] = "cs",
                ["RUNNINGHARDWARE"] = "SECONDARY"
            }
        };

        var sTrans = new ProcessNodeModel { Name = "TRANS", Type = "TRANS_TYPE", State = "CS_STANDBY" };
        var sEi = new ProcessNodeModel { Name = "EI", Type = "EI_TYPE", State = "CS_STANDBY" };
        secondaryCs.Children.Add(sTrans);
        secondaryCs.Children.Add(sEi);
        SecondaryProcesses.Add(secondaryCs);
    }
}

/// <summary>
/// Properties DataGrid 바인딩용 아이템
/// </summary>
public class PropertyItem
{
    public string Property { get; set; } = "";
    public string Value { get; set; } = "";
}
