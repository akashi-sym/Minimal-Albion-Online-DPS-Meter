using AlbionDpsMeter.Enums;
using AlbionDpsMeter.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace AlbionDpsMeter.ViewModels;

public partial class DamageMeterFragmentViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _guildName = string.Empty;

    [ObservableProperty]
    private long _damage;

    [ObservableProperty]
    private long _heal;

    [ObservableProperty]
    private long _takenDamage;

    [ObservableProperty]
    private long _overhealed;

    [ObservableProperty]
    private double _dps;

    [ObservableProperty]
    private double _hps;

    [ObservableProperty]
    private double _damagePercentage;

    [ObservableProperty]
    private double _healPercentage;

    [ObservableProperty]
    private double _takenDamagePercentage;

    [ObservableProperty]
    private string _combatTimeString = "00:00";

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private ObservableCollection<SpellViewModel> _damageSpells = [];

    [ObservableProperty]
    private ObservableCollection<SpellViewModel> _healSpells = [];

    [ObservableProperty]
    private int _mainHandIndex;

    public Guid UserGuid { get; set; }

    public void UpdateFrom(PlayerGameObject player, double damagePercentage, double healPercentage, double takenDamagePercentage)
    {
        Name = player.Name ?? "Unknown";
        GuildName = player.Guild ?? string.Empty;
        MainHandIndex = player.CharacterEquipment?.MainHand ?? 0;
        Damage = player.Damage;
        Heal = player.Heal;
        TakenDamage = player.TakenDamage;
        Overhealed = player.Overhealed;
        Dps = player.Dps;
        Hps = player.Hps;
        DamagePercentage = damagePercentage;
        HealPercentage = healPercentage;
        TakenDamagePercentage = takenDamagePercentage;
        CombatTimeString = player.CombatTime.ToTimerString();
        DamageSpells = SpellViewModel.FromSpells(
            player.Spells.Where(s => s.HealthChangeType == HealthChangeType.Damage).OrderByDescending(s => s.DamageHealValue),
            player.Damage, player.Heal);
        HealSpells = SpellViewModel.FromSpells(
            player.Spells.Where(s => s.HealthChangeType == HealthChangeType.Heal).OrderByDescending(s => s.DamageHealValue),
            player.Damage, player.Heal);
    }
}
