using AlbionDpsMeter.Models;
using AlbionDpsMeter.Network.Events;
using AlbionDpsMeter.Services;
using StatisticsAnalysisTool.Network;
using System.Threading.Tasks;

namespace AlbionDpsMeter.Network.Handlers;

public class HealthUpdatesEventHandler : EventPacketHandler<HealthUpdatesEvent>
{
    private readonly TrackingController _trackingController;

    public HealthUpdatesEventHandler(TrackingController trackingController) : base((int)EventCodes.HealthUpdates)
    {
        _trackingController = trackingController;
    }

    protected override async Task OnActionAsync(HealthUpdatesEvent value)
    {
        foreach (HealthUpdate healthUpdate in value.HealthUpdates)
        {
            await _trackingController.CombatController.AddDamage(healthUpdate.AffectedObjectId, healthUpdate.CauserId, healthUpdate.HealthChange, healthUpdate.NewHealthValue, healthUpdate.CausingSpellIndex);
            await _trackingController.CombatController.AddTakenDamage(healthUpdate.AffectedObjectId, healthUpdate.CauserId, healthUpdate.HealthChange, healthUpdate.NewHealthValue, healthUpdate.CausingSpellIndex);
        }
    }
}
