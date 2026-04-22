using AlbionDpsMeter.Network.Events;
using AlbionDpsMeter.Services;
using StatisticsAnalysisTool.Network;
using System.Threading.Tasks;

namespace AlbionDpsMeter.Network.Handlers;

public class TakeSilverEventHandler : EventPacketHandler<TakeSilverEvent>
{
    private readonly TrackingController _trackingController;

    public TakeSilverEventHandler(TrackingController trackingController) : base((int)EventCodes.TakeSilver)
    {
        _trackingController = trackingController;
    }

    protected override async Task OnActionAsync(TakeSilverEvent value)
    {
        await _trackingController.CombatController.AddSilver(value.YieldAfterTax / 10000L);
    }
}
