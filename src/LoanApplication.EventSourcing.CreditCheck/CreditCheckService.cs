using EventStore.Client;
using LoanApplication.EventSourcing.Shared;
using LoanApplication.EventSourcing.Shared.Events;

namespace LoanApplication.EventSourcing.CreditCheck;

public class CreditCheckService(
    EventStoreClient eventStoreClient,
    EventStorePersistentSubscriptionsClient subscriptionsClient,
    ILogger<CreditCheckService> logger) : BackgroundService
{
    private readonly EventStoreClient _eventStoreClient = eventStoreClient;
    private readonly EventStorePersistentSubscriptionsClient _subscriptionsClient = subscriptionsClient;
    private readonly ILogger<CreditCheckService> _logger = logger;

    private const string StreamName = "$et-LoanRequested";
    private const string GroupName = "credit-check";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IEnumerable<PersistentSubscriptionInfo>? subscriptions = null;

        try
        {
            subscriptions = await _subscriptionsClient.ListToStreamAsync(StreamName, TimeSpan.FromSeconds(30), cancellationToken: stoppingToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to list subscriptions for '{StreamName}' in group '{GroupName}'.", StreamName, GroupName);
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
        var @event = resolvedEvent.Deserialize<LoanRequested>();
        if (@event is null)
        {
            _logger.LogError("Failed to deserialize event from '{StreamName}' in group '{GroupName}'.", StreamName, GroupName);
            await subscription.Nack(PersistentSubscriptionNakEventAction.Park, "Failed to deserialize.", resolvedEvent);
            return;
        }

        await CheckCreditScore(subscription, resolvedEvent, @event, cancellationToken);
    }

    private async Task CheckCreditScore(PersistentSubscription subscription, ResolvedEvent resolvedEvent, LoanRequested? @event, CancellationToken cancellationToken)
    {
        try
        {
            var randomCreditScore = new Random().Next(1, 10); // Credit score between 1 and 9
            var creditChecked = new CreditChecked(@event!.Id, randomCreditScore, @event.Customer.NationalId, DateTimeOffset.UtcNow);

            _logger.LogInformation("Credit checked for '{EventId}' with score '{CreditScore}'.", creditChecked.Id, creditChecked.Score);

            await _eventStoreClient.AppendToStreamAsync(
                $"loan-request-{@event.Id:N}",
                StreamRevision.FromStreamPosition(resolvedEvent.Event.EventNumber),
                [creditChecked.Serialize()],
                cancellationToken: cancellationToken);

            await subscription.Ack(resolvedEvent);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to check credit for '{EventId}'.", @event?.Id);
            await subscription.Nack(PersistentSubscriptionNakEventAction.Park, exception.Message, resolvedEvent);
        }
        
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    private void SubscriptionDropped(PersistentSubscription subscription, SubscriptionDroppedReason reason, Exception? exception)
    {
        _logger.LogError(exception, "Subscription Dropped for '{EventStoreStream}' with reason '{SubscriptionDroppedReason}'.", subscription.SubscriptionId, reason);
    }
}
