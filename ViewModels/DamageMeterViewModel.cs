using AlbionDpsMeter.Enums;
using AlbionDpsMeter.Models;
using AlbionDpsMeter.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AlbionDpsMeter.ViewModels;

public partial class DamageMeterViewModel : ObservableObject
{
    private readonly TrackingController _trackingController;
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    private ObservableCollection<DamageMeterFragmentViewModel> _fragments = [];

    [ObservableProperty]
    private bool _isTrackingActive;

    [ObservableProperty]
    private DamageMeterSortType _sortType = DamageMeterSortType.Damage;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private PacketProviderKind _packetProvider = PacketProviderKind.Npcap;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private long _totalDamage;

    [ObservableProperty]
    private long _totalHeal;

    [ObservableProperty]
    private long _totalTakenDamage;

    [ObservableProperty]
    private long _totalFame;

    [ObservableProperty]
    private long _totalCombatFame;

    [ObservableProperty]
    private long _totalSilver;

    [ObservableProperty]
    private string _playerName = string.Empty;

    [ObservableProperty]
    private string _guildName = string.Empty;

    [ObservableProperty]
    private int _partyMemberCount;

    public event Action? OnUiRefreshRequired;

    public DamageMeterViewModel(TrackingController trackingController, DispatcherQueue dispatcherQueue)
    {
        _trackingController = trackingController;
        _dispatcherQueue = dispatcherQueue;

        _trackingController.CombatController.OnDamageUpdate += OnDamageDataReceived;
        _trackingController.CombatController.OnFameOrSilverUpdate += OnFameOrSilverReceived;
        _trackingController.OnTrackingError += OnTrackingError;
        _trackingController.EntityController.OnProfileOrPartyChanged += OnProfileOrPartyChanged;
        _trackingController.OnTrackingStateChanged += state =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsTrackingActive = state;
                StatusText = state ? "Tracking..." : "Stopped";
            });
        };
    }

    partial void OnSortTypeChanged(DamageMeterSortType value)
    {
        RefreshSort();
    }

    [RelayCommand]
    private void StartTracking()
    {
        ErrorMessage = null;
        _trackingController.PacketProvider = PacketProvider;
        _ = _trackingController.StartTrackingAsync();
    }

    [RelayCommand]
    private void StopTracking()
    {
        _trackingController.StopTracking();
    }

    [RelayCommand]
    private void ResetDamageMeter()
    {
        _trackingController.CombatController.ResetDamageMeter();
        _dispatcherQueue.TryEnqueue(() =>
        {
            Fragments.Clear();
            TotalDamage = 0;
            TotalHeal = 0;
            TotalTakenDamage = 0;
            TotalFame = 0;
            TotalCombatFame = 0;
            TotalSilver = 0;
            OnUiRefreshRequired?.Invoke();
        });
    }

    private void OnTrackingError(string errorMessage)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            ErrorMessage = errorMessage;
            StatusText = "Error";
        });
    }

    private void OnProfileOrPartyChanged()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var local = _trackingController.EntityController.GetLocalEntity();
            Log.Information("ProfileOrPartyChanged: local={HasLocal} name={Name} guild={Guild} partyCount={Count}",
                local?.Value != null,
                local?.Value?.Name,
                local?.Value?.Guild,
                _trackingController.EntityController.GetPartyMemberCount());
            if (local?.Value != null)
            {
                PlayerName = local.Value.Value.Name ?? string.Empty;
                GuildName = local.Value.Value.Guild ?? string.Empty;
            }
            PartyMemberCount = _trackingController.EntityController.GetPartyMemberCount();
        });
    }

    private void OnDamageDataReceived(List<KeyValuePair<Guid, PlayerGameObject>> entities)
    {
        _dispatcherQueue.TryEnqueue(() => UpdateDamageMeterUi(entities));
    }

    private void OnFameOrSilverReceived(long fame, long combatFame, long silver)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            TotalFame = fame;
            TotalCombatFame = combatFame;
            TotalSilver = silver;
        });
    }

    private void UpdateDamageMeterUi(List<KeyValuePair<Guid, PlayerGameObject>> entities)
    {
        if (entities.Count == 0) return;

        var totalDamage = entities.GetCurrentTotalDamage();
        var totalHeal = entities.GetCurrentTotalHeal();
        var totalTakenDamage = entities.GetCurrentTotalTakenDamage();

        TotalDamage = totalDamage;
        TotalHeal = totalHeal;
        TotalTakenDamage = totalTakenDamage;

        // Update existing or add new fragments
        var existingGuids = new HashSet<Guid>();
        foreach (var entity in entities)
        {
            existingGuids.Add(entity.Key);

            var fragment = Fragments.FirstOrDefault(f => f.UserGuid == entity.Key);
            if (fragment == null)
            {
                fragment = new DamageMeterFragmentViewModel { UserGuid = entity.Key };
                Fragments.Add(fragment);
            }

            var damagePercentage = entities.GetDamagePercentage(entity.Value.Damage);
            var healPercentage = entities.GetHealPercentage(entity.Value.Heal);
            var takenDamagePercentage = entities.GetTakenDamagePercentage(entity.Value.TakenDamage);

            fragment.UpdateFrom(entity.Value, damagePercentage, healPercentage, takenDamagePercentage);
        }

        // Remove fragments for players no longer in the list
        for (int i = Fragments.Count - 1; i >= 0; i--)
        {
            if (!existingGuids.Contains(Fragments[i].UserGuid))
                Fragments.RemoveAt(i);
        }

        RefreshSort();
        OnUiRefreshRequired?.Invoke();
    }

    private void RefreshSort()
    {
        var sorted = SortType switch
        {
            DamageMeterSortType.Damage => Fragments.OrderByDescending(f => f.Damage).ToList(),
            DamageMeterSortType.Dps => Fragments.OrderByDescending(f => f.Dps).ToList(),
            DamageMeterSortType.Heal => Fragments.OrderByDescending(f => f.Heal).ToList(),
            DamageMeterSortType.Hps => Fragments.OrderByDescending(f => f.Hps).ToList(),
            DamageMeterSortType.Name => Fragments.OrderBy(f => f.Name).ToList(),
            DamageMeterSortType.TakenDamage => Fragments.OrderByDescending(f => f.TakenDamage).ToList(),
            _ => Fragments.OrderByDescending(f => f.Damage).ToList()
        };

        for (int i = 0; i < sorted.Count; i++)
        {
            int oldIndex = Fragments.IndexOf(sorted[i]);
            if (oldIndex != i)
                Fragments.Move(oldIndex, i);
        }
    }
}
