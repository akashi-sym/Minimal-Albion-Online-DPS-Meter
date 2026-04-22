using AlbionDpsMeter.Network.Events;
using AlbionDpsMeter.Services;
using StatisticsAnalysisTool.Network;
using System.Threading.Tasks;

namespace AlbionDpsMeter.Network.Handlers;

public class CharacterEquipmentChangedEventHandler : EventPacketHandler<CharacterEquipmentChangedEvent>
{
    private readonly TrackingController _trackingController;

    public CharacterEquipmentChangedEventHandler(TrackingController trackingController) : base((int)EventCodes.CharacterEquipmentChanged)
    {
        _trackingController = trackingController;
    }

    protected override async Task OnActionAsync(CharacterEquipmentChangedEvent value)
    {
        Serilog.Log.Debug("CharacterEquipmentChanged: ObjectId={ObjectId} MainHand={MainHand}",
            value.ObjectId, value.CharacterEquipment?.MainHand);
        if (value.ObjectId != null)
        {
            _trackingController.EntityController.SetCharacterEquipment((long)value.ObjectId, value.CharacterEquipment);
        }
        await Task.CompletedTask;
    }
}
