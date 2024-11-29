using EventStore.Client;
using LoanApplication.EventSourcing.Shared;
using LoanApplication.EventSourcing.Shared.Events;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoanApplication.EventSourcing.Underwriting.WebApp;

public class UnderwritingService(
    EventStorePersistentSubscriptionsClient subscriptionsClient,
    LoanRequestRepository loanRequestRepository,
    IHubContext<UnderwritingHub> hubContext,
    ILogger<UnderwritingService> logger) : BackgroundService
{
    private readonly EventStorePersistentSubscriptionsClient _subscriptionsClient = subscriptionsClient;
    private readonly LoanRequestRepository _loanRequestRepository = loanRequestRepository;
    private readonly IHubContext<UnderwritingHub> _hubContext = hubContext;
    private readonly ILogger<UnderwritingService> _logger = logger;

    private const string StreamName = "$et-ManualApprovalRequired";
    private const string GroupName = "underwriting";

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IEnumerable<PersistentSubscriptionInfo>? subscriptions = null;

        try
        {
            subscriptions = await _subscriptionsClient.ListToStreamAsync(StreamName, TimeSpan.FromSeconds(30), cancellationToken: stoppingToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to list subscriptions for '{StreamName}' in group '{GroupName}'.", StreamName, GroupName);
        }

        if (subscriptions is null || !subscriptions.Any())
        {
            _logger.LogInformation("Creating subscription for '{StreamName}' in group '{GroupName}'.", StreamName, GroupName);

            await _subscriptionsClient.CreateToStreamAsync(
                StreamName,
                GroupName,
                new PersistentSubscriptionSettings(resolveLinkTos: true),
                cancellationToken: stoppingToken);
        }
        else
        {
            _logger.LogInformation("Subscription for '{StreamName}' in group '{GroupName}' already exists.", StreamName, GroupName);
        }

        var subscription = await _subscriptionsClient.SubscribeToStreamAsync(
            StreamName,
            GroupName,
            EventAppeared,
            SubscriptionDropped,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Subscribed to '{StreamName}' in group '{GroupName}' with subscription id '{SubscriptionId}'.",
            StreamName,
            GroupName,
            subscription.SubscriptionId);
    }

    private async Task EventAppeared(PersistentSubscription subscription, ResolvedEvent resolvedEvent, int? nullable, CancellationToken cancellationToken)
    {
        var @event = resolvedEvent.Deserialize<ManualApprovalRequired>();
        if (@event is null)
        {
            _logger.LogError("Failed to deserialize event from '{StreamName}' in group '{GroupName}'.", StreamName, GroupName);
            await subscription.Nack(PersistentSubscriptionNakEventAction.Park, "Failed to deserialize.", resolvedEvent);
            return;
        }

        try
        {
            _loanRequestRepository.AddManualApproval(@event);

            await _hubContext.Clients.All.SendAsync("ManualApproval", JsonSerializer.Serialize(@event, _serializerOptions), cancellationToken);

            await subscription.Ack(resolvedEvent);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to process Event '{EventId}'.", @event?.Id);
            await subscription.Nack(PersistentSubscriptionNakEventAction.Park, exception.Message, resolvedEvent);
        }
    }

    private void SubscriptionDropped(PersistentSubscription subscription, SubscriptionDroppedReason reason, Exception? exception)
    {
        _logger.LogError(exception, "Subscription Dropped for '{EventStoreStream}' with reason '{SubscriptionDroppedReason}'.", subscription.SubscriptionId, reason);
    }
}
