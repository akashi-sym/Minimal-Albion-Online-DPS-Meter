using AlbionDpsMeter.Network.Events;
using AlbionDpsMeter.Services;
using StatisticsAnalysisTool.Network;
using System.Threading.Tasks;

namespace AlbionDpsMeter.Network.Handlers;

public class PartyDisbandedEventHandler : EventPacketHandler<PartyDisbandedEvent>
{
    private readonly TrackingController _trackingController;

    public PartyDisbandedEventHandler(TrackingController trackingController) : base((int)EventCodes.PartyDisbanded)
    {
        _trackingController = trackingController;
    }

    protected override async Task OnActionAsync(PartyDisbandedEvent value)
    {
        _trackingController.EntityController.ResetPartyMember();
        _trackingController.EntityController.AddLocalEntityToParty();
        await Task.CompletedTask;
    }
}
