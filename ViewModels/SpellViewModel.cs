using AlbionDpsMeter.Enums;
using AlbionDpsMeter.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AlbionDpsMeter.ViewModels;

public partial class SpellViewModel : ObservableObject
{
    [ObservableProperty]
    private string _spellName = string.Empty;

    [ObservableProperty]
    private int _spellIndex;

    [ObservableProperty]
    private long _damageHealValue;

    [ObservableProperty]
    private int _ticks;

    [ObservableProperty]
    private HealthChangeType _healthChangeType;

    [ObservableProperty]
    private double _percentage;

    public static ObservableCollection<SpellViewModel> FromSpells(IEnumerable<UsedSpell> spells, long totalDamage, long totalHeal)
    {
        var result = new ObservableCollection<SpellViewModel>();
        foreach (var spell in spells)
        {
            long total = spell.HealthChangeType == HealthChangeType.Damage ? totalDamage : totalHeal;
            result.Add(new SpellViewModel
            {
                SpellName = !string.IsNullOrEmpty(spell.UniqueName) ? spell.UniqueName : $"Spell #{spell.SpellIndex}",
                SpellIndex = spell.SpellIndex,
                DamageHealValue = spell.DamageHealValue,
                Ticks = spell.Ticks,
                HealthChangeType = spell.HealthChangeType,
                Percentage = total > 0 ? 100.0 / total * spell.DamageHealValue : 0
            });
        }
        return result;
    }
}
