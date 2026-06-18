using System.Diagnostics.Metrics;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.Receiver.Ports;

internal class ApiMeters
{
    public const string MeterName = "Meldinger.Consumer.API.Meters";
    internal readonly Meter _meter;
    private readonly Counter<int> _meldingConsumedCounter;

    private readonly Counter<int> _meldingAcknowledgedCounter;

    private readonly Histogram<double> _meldingRequestDurationFromStart;

    private readonly Histogram<double> _meldingRequestDurationFromConsumerHook;

    public ApiMeters(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        _meldingConsumedCounter = _meter.CreateCounter<int>(
            "meldinger_receiver_melding_consumed_count",
            "meldinger",
            "Counts the number of consumed meldinger"
        );

        _meldingAcknowledgedCounter = _meter.CreateCounter<int>(
            "meldinger_receiver_melding_acknowledged_count",
            "meldinger",
            "Counts the number of acknowledged meldinger"
        );

        _meldingRequestDurationFromConsumerHook = _meter.CreateHistogram<double>(
            "meldinger_consumer_melding_request_duration_from_consumed",
            "ms",
            "Request duration in milliseconds"
        );

        _meldingRequestDurationFromStart = _meter.CreateHistogram<double>(
            "meldinger_consumer_melding_request_duration_from_received",
            "ms",
            "Request duration in milliseconds"
        );
    }

    public void MeldingConsumed(Melding melding, bool fromRedrive)
    {
        _meldingConsumedCounter.Add(
            1,
            new KeyValuePair<string, object?>("source", melding.Source),
            new KeyValuePair<string, object?>("appId", melding.ApplicationId),
            new KeyValuePair<string, object?>("triggeredFromRedrive", fromRedrive)
        );
    }

    public void MeldingAcknowledged(Melding melding, bool fromRedrive)
    {
        _meldingAcknowledgedCounter.Add(
            1,
            new KeyValuePair<string, object?>("source", melding.Source),
            new KeyValuePair<string, object?>("appId", melding.ApplicationId),
            new KeyValuePair<string, object?>("triggeredFromRedrive", fromRedrive)
        );
    }

    public void RegisterMeldingDurationFromConsumerHook(
        Melding melding,
        DateTime consumedAt,
        bool fromRedrive
    )
    {
        _meldingRequestDurationFromConsumerHook.Record(
            (DateTime.Now.ToUniversalTime() - consumedAt).TotalMilliseconds,
            new KeyValuePair<string, object?>("source", melding.Source),
            new KeyValuePair<string, object?>("appId", melding.ApplicationId),
            new KeyValuePair<string, object?>("triggeredFromRedrive", fromRedrive)
        );
    }

    public void RegisterMeldingDurationFromStart(Melding melding, bool fromRedrive)
    {
        _meldingRequestDurationFromStart.Record(
            (DateTime.Now.ToUniversalTime() - melding.ReceivedAt).TotalMilliseconds,
            new KeyValuePair<string, object?>("source", melding.Source),
            new KeyValuePair<string, object?>("appId", melding.ApplicationId),
            new KeyValuePair<string, object?>("triggeredFromRedrive", fromRedrive)
        );
    }
}
