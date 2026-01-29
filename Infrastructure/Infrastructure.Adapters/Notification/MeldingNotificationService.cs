using System.Diagnostics;
using System.Text.Json;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Notification;

internal class MeldingNotificationService : IMeldingNotificationService
{
    private readonly IDatabase _db;
    private readonly ILogger<MeldingNotificationService> _logger;

    private readonly ISubscriber _subscriber;

    private readonly ValkeyConfiguration _valkeyConfig;

    public MeldingNotificationService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<MeldingNotificationService> logger,
        IOptions<InfrastructureConfiguration> options
    )
    {
        _db = connectionMultiplexer.GetDatabase();
        _logger = logger;
        _valkeyConfig = options.Value.ValkeyConfiguration;
        _subscriber = connectionMultiplexer.GetSubscriber();
    }

    public async Task NotifyMeldingProcessed(Melding melding)
    {
        melding.AddInternalTag("rootTraceId", Activity.Current?.TraceId.ToString() ?? "");
        melding.AddInternalTag("rootTraceParent", Activity.Current?.ParentId ?? "");
        var result = await _db.StreamAddAsync(
            _valkeyConfig.StreamName,
            new NameValueEntry[]
            {
                new(_valkeyConfig.MessageKey, JsonSerializer.Serialize(melding)),
            }
        );
        _logger.LogInformation(
            "Successfully added notification with message id {MessageId} to stream {StreamName}. Activity id was {ActivityId}.",
            result,
            _valkeyConfig.StreamName,
            Activity.Current?.Id
        );
        await _subscriber.PublishAsync(
            new RedisChannel(_valkeyConfig.StreamName, RedisChannel.PatternMode.Literal),
            melding.Id.ToString()
        );
    }
}
