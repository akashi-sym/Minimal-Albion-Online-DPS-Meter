using AlbionDpsMeter.Network.Events;
using AlbionDpsMeter.Services;
using StatisticsAnalysisTool.Network;
using System.Threading.Tasks;

namespace AlbionDpsMeter.Network.Handlers;

public class UpdateReSpecPointsEventHandler : EventPacketHandler<UpdateReSpecPointsEvent>
{
    private readonly TrackingController _trackingController;

    public UpdateReSpecPointsEventHandler(TrackingController trackingController) : base((int)EventCodes.UpdateReSpecPoints)
    {
        _trackingController = trackingController;
    }

    protected override async Task OnActionAsync(UpdateReSpecPointsEvent value)
    {
        await _trackingController.CombatController.AddCombatFame(value.GainedReSpecPoints / 10000L);
    }
}
