using System.Diagnostics.Metrics;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi;

public class ApiMeters
{
    public const string MeterName = "Meldinger.Receiver.API.Meters";
    private readonly Meter _meter;

    private readonly Dictionary<string, int> _meldingRecoveryCountByApp = new();
    private readonly Counter<int> _meldingReceivedCounter;

    private readonly Counter<int> _meldingProcessedCounter;

    private readonly Histogram<double> _meldingRequestDuration;

    public ApiMeters(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        _meldingReceivedCounter = _meter.CreateCounter<int>(
            "meldinger_receiver_melding_received_count",
            "meldinger",
            "Counts the number of received meldinger"
        );

        _meldingProcessedCounter = _meter.CreateCounter<int>(
            "meldinger_receiver_melding_processed_count",
            "meldinger",
            "Counts the number of processed meldinger"
        );

        _meter.CreateObservableUpDownCounter<int>(
            "meldinger_receiver_recovery_counter_count",
            GetCachedRecoveryJobCounts,
            "meldinger",
            "Counts the number of items when running the recovery job"
        );

        _meldingRequestDuration = _meter.CreateHistogram<double>(
            "meldinger_receiver_melding_request_duration",
            "ms",
            "Request duration in milliseconds"
        );
    }

    public void MeldingReceived(MessageSource Source, string AppId)
    {
        _meldingReceivedCounter.Add(1, new("source", Source), new("appId", AppId));
    }

    public void MeldingProcessed(Melding melding)
    {
        _meldingProcessedCounter.Add(
            1,
            new("source", melding.Source),
            new("appId", melding.ApplicationId)
        );
    }

    public void RegisterMeldingDuration(Melding melding)
    {
        _meldingRequestDuration.Record(
            (DateTime.Now.ToUniversalTime() - melding.ReceivedAt).TotalMilliseconds,
            new("source", melding.Source),
            new("appId", melding.ApplicationId)
        );
    }

    private IEnumerable<Measurement<int>> GetCachedRecoveryJobCounts()
    {
        // Return cached values - these should be updated via UpdateRecoveryCounts
        foreach (var kvp in _meldingRecoveryCountByApp)
        {
            yield return new Measurement<int>(
                kvp.Value,
                new KeyValuePair<string, object?>("appId", kvp.Key)
            );
        }
    }

    // Call this periodically from a background service
    public void UpdateRecoveryCounts(int count, string appId)
    {
        _meldingRecoveryCountByApp[appId] = count;
    }
}
