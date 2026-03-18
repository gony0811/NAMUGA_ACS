using System;
using System.Collections.Generic;
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
/// Link View ViewModel — 링크 목록 CRUD
/// </summary>
public partial class LinkViewModel : ObservableObject
{
    private readonly IAcsApiService? _apiService;

    public ObservableCollection<LinkDto> Links { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditLinkCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteLinkCommand))]
    private LinkDto? _selectedLink;

    [ObservableProperty]
    private int _totalCount;

    public LinkViewModel(IAcsApiService? apiService = null)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadLinksAsync()
    {
        if (_apiService == null) return;

        try
        {
            var links = await _apiService.GetLinksAsync();
            Links.Clear();
            foreach (var link in links)
            {
                Links.Add(link);
            }
            TotalCount = Links.Count;
        }
        catch (Exception)
        {
            // 로드 실패 시 무시
        }
    }

    [RelayCommand]
    private async Task AddLinkAsync()
    {
        if (_apiService == null) return;

        var emptyLink = new LinkDto { Availability = "0" };
        var zones = await _apiService.GetZonesAsync();
        var emptyLinkZones = new List<LinkZoneDto>();
        var dialog = new LinkEditWindow(emptyLink, zones, emptyLinkZones, isEditMode: false);

        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.CreateLinkAsync(dialog.Link);
            if (success)
            {
                // 새로 추가된 LinkZone 저장
                foreach (var lz in dialog.LinkZones)
                {
                    await _apiService.CreateLinkZoneAsync(lz);
                }
                await LoadLinksAsync();
            }
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLink))]
    private async Task EditLinkAsync()
    {
        if (_apiService == null || SelectedLink == null) return;

        var zones = await _apiService.GetZonesAsync();
        var existingLinkZones = await _apiService.GetLinkZonesByLinkIdAsync(SelectedLink.Id);
        var dialog = new LinkEditWindow(SelectedLink, zones, existingLinkZones, isEditMode: true);

        var result = await dialog.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.UpdateLinkAsync(dialog.Link);
            if (success)
            {
                // LinkZone 차이 비교: 추가/삭제
                var originalIds = dialog.OriginalLinkZones.Select(lz => lz.Id).ToHashSet();
                var currentIds = dialog.LinkZones.Select(lz => lz.Id).ToHashSet();

                // 삭제된 항목
                foreach (var orig in dialog.OriginalLinkZones)
                {
                    if (!currentIds.Contains(orig.Id))
                        await _apiService.DeleteLinkZoneAsync(orig.Id);
                }

                // 추가된 항목
                foreach (var curr in dialog.LinkZones)
                {
                    if (!originalIds.Contains(curr.Id))
                        await _apiService.CreateLinkZoneAsync(curr);
                }

                await LoadLinksAsync();
            }
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLink))]
    private async Task DeleteLinkAsync()
    {
        if (_apiService == null || SelectedLink == null) return;

        var msgBox = new Window
        {
            Title = "Delete Link",
            Width = 320,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = CreateDeleteConfirmContent(SelectedLink.Id)
        };

        var result = await msgBox.ShowDialog<bool?>(GetOwnerWindow());
        if (result == true)
        {
            var success = await _apiService.DeleteLinkAsync(SelectedLink.Id);
            if (success)
                await LoadLinksAsync();
        }
    }

    private bool HasSelectedLink() => SelectedLink != null;

    private static object CreateDeleteConfirmContent(string linkId)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12
        };
        panel.Children.Add(new TextBlock
        {
            Text = $"Link '{linkId}' 을(를) 삭제하시겠습니까?",
            FontSize = 12
        });

        var btnPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 12
        };

        var okBtn = new Button { Content = "OK", Width = 80, Height = 28 };
        var cancelBtn = new Button { Content = "Cancel", Width = 80, Height = 28 };

        okBtn.Click += (s, e) =>
        {
            var w = (s as Visual)?.FindAncestorOfType<Window>();
            w?.Close(true);
        };
        cancelBtn.Click += (s, e) =>
        {
            var w = (s as Visual)?.FindAncestorOfType<Window>();
            w?.Close(false);
        };

        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        panel.Children.Add(btnPanel);

        return panel;
    }

    private Window GetOwnerWindow()
    {
        return Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.FirstOrDefault(w => w.Title == "Link") ?? desktop.MainWindow!
            : null!;
    }
}
