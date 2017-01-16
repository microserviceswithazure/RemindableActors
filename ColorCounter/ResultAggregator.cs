﻿namespace ColorCounter
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using global::ColorCounter.Interfaces;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    /// <remarks>
    ///     This class represents an actor.
    ///     Every ActorID maps to an instance of this class.
    ///     The StatePersistence attribute determines persistence and replication of actor state:
    ///     - Persisted: State is written to disk and replicated.
    ///     - Volatile: State is kept in memory only and replicated.
    ///     - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class ResultAggregator : Actor, IResultAggregator
    {
        /// <summary>
        ///     Initializes a new instance of ColorCounter
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public ResultAggregator(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public async Task AggregateResult(string colorName, long count)
        {
            var colorDictionaryState = await this.StateManager.TryGetStateAsync<Dictionary<string, long>>(
                "colorCounter");
            if (colorDictionaryState.HasValue)
            {
                var colorDictionary = colorDictionaryState.Value;
                if (colorDictionary.ContainsKey(colorName))
                {
                    colorDictionary[colorName] = colorDictionary[colorName] + count;
                }
                else
                {
                    colorDictionary.Add(colorName, count);
                }

                await this.StateManager.AddOrUpdateStateAsync(
                    "colorCounter",
                    colorDictionary,
                    (key, value) => colorDictionary);
            }
        }

        public async Task<Dictionary<string, long>> Result(CancellationToken token)
        {
            var result = await this.StateManager.TryGetStateAsync<Dictionary<string, long>>("colorCounter", token);
            return result.HasValue ? result.Value : new Dictionary<string, long>();
        }

        /// <summary>
        ///     This method is called whenever an actor is activated.
        ///     An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");
            return this.StateManager.TryAddStateAsync("colorCounter", new Dictionary<string, long>());
        }
    }
}