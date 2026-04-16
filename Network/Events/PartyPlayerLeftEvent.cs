using AlbionDpsMeter.Models;
using Serilog;
using System;
using System.Collections.Generic;

namespace AlbionDpsMeter.Network.Events;

public class PartyPlayerLeftEvent
{
    public Guid? UserGuid;

    public PartyPlayerLeftEvent(Dictionary<byte, object> parameters)
    {
        try
        {
            if (parameters.ContainsKey(1))
                UserGuid = parameters[1].ObjectToGuid();
        }
        catch (Exception e) { Log.Error(e, "PartyPlayerLeftEvent parse error"); }
    }
}
