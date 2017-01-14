using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace ColorCounter.Interfaces
{
    using System.Fabric.Health;

    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IColorCounter : IActor
    {
        Task SetImage(Uri imageUri, CancellationToken token);

        Task CountPixels(string color, CancellationToken token);

        Task<IDictionary<string, int>> GetPixelCount(CancellationToken token);
    }
}
