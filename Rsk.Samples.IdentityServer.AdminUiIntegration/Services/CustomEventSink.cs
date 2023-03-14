using System;
using System.Text.Json;
using System.Threading.Tasks;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Rsk.Samples.IdentityServer.AdminUiIntegration.Services
{
    /// <summary>
    /// Implements the DefaultEventSink functionality, but in addition, when event types are error or failure, will add the event to the custom event store cache
    /// </summary>
    public sealed class CustomEventSink : IEventSink
    {
        private readonly ILogger logger;
        private readonly IEventStore eventStore;

        public CustomEventSink(ILogger<DefaultEventService> logger, IEventStore eventStore)
        {
            this.logger = logger;
            this.eventStore = eventStore;
        }

        public Task PersistAsync(Event evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            logger.LogInformation("{@event}", evt);
            
            if (evt.EventType == EventTypes.Error || evt.EventType == EventTypes.Failure) eventStore.AddEvent(evt); 
            
            return Task.CompletedTask;
        }
    }

    public interface IEventStore
    {
        string GetEventByTraceID(string traceID);
        void AddEvent(Event evt);
    }

    /// <summary>
    /// Stores IdentityServer error and failure events in a memory cache a capacity of holding 10 events
    /// </summary>
    public class ErrorEventStore : IEventStore
    {
        private readonly IMemoryCache cache;

        public ErrorEventStore(IMemoryCache cache)
        {
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public string GetEventByTraceID(string traceID)
        {
            cache.TryGetValue(traceID, out string eventDetails);
            return eventDetails;
        }

        public void AddEvent(Event evt)
        {
            cache.Set(evt.ActivityId, JsonSerializer.Serialize(evt), new MemoryCacheEntryOptions
            {
                Size = 1
            });
        }
    }
}