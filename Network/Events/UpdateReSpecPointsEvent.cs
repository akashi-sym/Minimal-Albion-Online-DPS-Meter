using AlbionDpsMeter.Models;
using Serilog;
using System;
using System.Collections.Generic;

namespace AlbionDpsMeter.Network.Events;

public class UpdateReSpecPointsEvent
{
    public long GainedReSpecPoints;

    public UpdateReSpecPointsEvent(Dictionary<byte, object> parameters)
    {
        try
        {
            if (parameters.TryGetValue(2, out object? gainedRaw))
                GainedReSpecPoints = gainedRaw.ObjectToLong() ?? 0;
        }
        catch (Exception e)
        {
            Log.Error(e, "UpdateReSpecPointsEvent parse error");
        }
    }
}
