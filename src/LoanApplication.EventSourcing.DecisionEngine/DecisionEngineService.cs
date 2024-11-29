using EventStore.Client;
using LoanApplication.EventSourcing.Shared;
using LoanApplication.EventSourcing.Shared.Events;

namespace LoanApplication.EventSourcing.DecisionEngine;

public class DecisionEngineService(
    EventStoreClient eventStoreClient,
    EventStorePersistentSubscriptionsClient subscriptionsClient,
    ILogger<DecisionEngineService> logger) : BackgroundService
{
    private readonly EventStoreClient _eventStoreClient = eventStoreClient;
    private readonly EventStorePersistentSubscriptionsClient _subscriptionsClient = subscriptionsClient;
    private readonly ILogger<DecisionEngineService> _logger = logger;

    private const string StreamName = "$et-CreditChecked";
    private const string GroupName = "decision-engine";

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
        var @event = resolvedEvent.Deserialize<CreditChecked>();
        if (@event is null)
        {
            _logger.LogError("Failed to deserialize event from '{StreamName}' in group '{GroupName}'.", StreamName, GroupName);
            await subscription.Nack(PersistentSubscriptionNakEventAction.Park, "Failed to deserialize.", resolvedEvent);
            return;
        }

        await DecideOnCreditScore(subscription, resolvedEvent, @event, cancellationToken);
    }

    private async Task DecideOnCreditScore(PersistentSubscription subscription, ResolvedEvent resolvedEvent, CreditChecked @event, CancellationToken cancellationToken)
    {
        try
        {
            IEvent? decisionEvent = null;

            if (@event.Score >= 7)
            {
                decisionEvent = new LoanApproved(@event.Id, DecisionType.Automatic, "DECISION_ENGINE", $"Score of {@event.Score} is high enough.", DateTimeOffset.UtcNow);
            }
            else if (@event.Score >= 5)
            {
                decisionEvent = new ManualApprovalRequired(@event.Id, DecisionType.Automatic, "DECISION_ENGINE", $"Score of {@event.Score} requires manual approval.", DateTimeOffset.UtcNow);
            }
            else
            {
                decisionEvent = new LoanDenied(@event.Id, DecisionType.Automatic, "DECISION_ENGINE", $"Score of {@event.Score} is too low.", DateTimeOffset.UtcNow);
            }

            await _eventStoreClient.AppendToStreamAsync(
                $"loanRequest-{@event.Id:N}",
                StreamRevision.FromStreamPosition(resolvedEvent.Event.EventNumber),
                [decisionEvent.Serialize()],
                cancellationToken: cancellationToken);

            await subscription.Ack(resolvedEvent);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to decide for '{EventId}'.", @event?.Id);
            await subscription.Nack(PersistentSubscriptionNakEventAction.Park, exception.Message, resolvedEvent);
        }

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    private void SubscriptionDropped(PersistentSubscription subscription, SubscriptionDroppedReason reason, Exception? exception)
    {
        _logger.LogError(exception, "Subscription Dropped for '{EventStoreStream}' with reason '{SubscriptionDroppedReason}'.", subscription.SubscriptionId, reason);
    }
}
