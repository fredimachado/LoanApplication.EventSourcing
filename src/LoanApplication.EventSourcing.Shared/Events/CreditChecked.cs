namespace LoanApplication.EventSourcing.Shared.Events;

public record CreditChecked(Guid Id, int Score, string NationalId, DateTimeOffset Timestamp) : IEvent;
