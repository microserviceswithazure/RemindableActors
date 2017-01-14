using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using ColorCounter.Interfaces;

namespace ColorCounter
{
    using System.Drawing;
    using System.Text;

    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class ColorCounter : Actor, IColorCounter, IRemindable
    {
        /// <summary>
        /// Initializes a new instance of ColorCounter
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public ColorCounter(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");
            this.StateManager.TryAddStateAsync("sourceImage", new Uri(string.Empty));
            return this.StateManager.TryAddStateAsync("colorCounter", new Dictionary<string, long>());
        }

        public Task SetImage(Uri imageUri, CancellationToken token)
        {
            return this.StateManager.AddOrUpdateStateAsync("sourceImage", imageUri, (key, value) => value, token);
        }

        public async Task CountPixels(string color, CancellationToken token)
        {
            var actorReminder = await this.RegisterReminderAsync(
                "countRequest",
                Encoding.ASCII.GetBytes(color),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromDays(1));
        }

        public Task<IDictionary<string, int>> GetPixelCount(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            if (reminderName.Equals("countRequest"))
            {
                var imageUri = await this.StateManager.TryGetStateAsync<Uri>("sourceImage");
                var colorToInspect = Encoding.ASCII.GetString(context).ToLowerInvariant();
                if (imageUri.HasValue)
                {
                    var image = new Bitmap(imageUri.Value.ToString(), true);
                    for (var widthCounter = 0; widthCounter < image.Width; ++widthCounter)
                    {
                        for (var heightCounter = 0; heightCounter < image.Height; ++heightCounter)
                        {
                            var pixelColor = image.GetPixel(widthCounter, heightCounter);
                            if (pixelColor.Name.ToLowerInvariant() == colorToInspect)
                            {
                                var colorDictionaryState = await this.StateManager.TryGetStateAsync<Dictionary<string, long>>("colorCounter");
                                if (colorDictionaryState.HasValue)
                                {
                                    var colorDictionary = colorDictionaryState.Value;
                                    var colorValue = colorDictionary[colorToInspect];
                                    colorDictionary[colorToInspect] = colorValue + 1;
                                    await this.StateManager.AddOrUpdateStateAsync(
                                        "colorCounter",
                                        colorDictionary,
                                        (key, value) => colorDictionary);
                                }
                            }
                        }
                    }
                }

                var reminder = this.GetReminder("countRequest");
                await this.UnregisterReminderAsync(reminder);
            }
        }
    }
}
