

using System.Collections.Generic;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RSK.Audit;
using RSK.Audit.EF;
using Rsk.DuendeIdentityServer.AuditEventSink;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Services;
using AuditProviderFactory = RSK.Audit.EF.AuditProviderFactory;

namespace Rsk.Samples.IdentityServer.AdminUiIntegration.Extensions;

public static class EventSinkExtExtensions
{
    public static IServiceCollection AddAuditProviderFactory(this IServiceCollection services, DbContextOptionsBuilder<AuditDatabaseContext> auditDbBuilder)
    {
        RSK.Audit.AuditProviderFactory auditFactory = new AuditProviderFactory(auditDbBuilder.Options);
        services.AddSingleton(auditFactory.CreateAuditSource("IdentityServer"));
        services.AddSingleton<IEventSink, AuditSink>();
        return services;
    }
    
    public static IServiceCollection AddEventSinks(this IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();

        var eventSinkAgLogger = sp.GetService<ILogger<EventSinkAggregator>>();
        var defaultEventLogger = sp.GetService<ILogger<DefaultEventService>>();

        var eventStore = sp.GetService<IEventStore>();
        var memoryCache = sp.GetService<IMemoryCache>();

        IRecordAuditableActions recordAuditableActions = sp.GetService<IRecordAuditableActions>();
        
        
        services.AddSingleton(provider => new EventSinkAggregator(eventSinkAgLogger)  
        {
            EventSinks = new List<IEventSink>()
            {
                new CustomEventSink(defaultEventLogger, eventStore),
                new AuditSink(recordAuditableActions),
            }
        });

        return services;
    }
}