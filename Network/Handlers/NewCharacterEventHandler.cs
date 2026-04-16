using AlbionDpsMeter.Enums;
using AlbionDpsMeter.Models;
using AlbionDpsMeter.Network.Events;
using AlbionDpsMeter.Services;
using StatisticsAnalysisTool.Network;
using System;
using System.Threading.Tasks;

namespace AlbionDpsMeter.Network.Handlers;

public class NewCharacterEventHandler : EventPacketHandler<NewCharacterEvent>
{
    private readonly TrackingController _trackingController;

    public NewCharacterEventHandler(TrackingController trackingController) : base((int)EventCodes.NewCharacter)
    {
        _trackingController = trackingController;
    }

    protected override async Task OnActionAsync(NewCharacterEvent value)
    {
        if (value.Guid != null && value.ObjectId != null)
        {
            _trackingController.EntityController.AddEntity(new Entity
            {
                ObjectId = value.ObjectId,
                UserGuid = value.Guid ?? Guid.Empty,
                Name = value.Name,
                Guild = value.GuildName,
                CharacterEquipment = value.CharacterEquipment,
                ObjectType = GameObjectType.Player,
                ObjectSubType = GameObjectSubType.Player
            });
        }
        await Task.CompletedTask;
    }
}
