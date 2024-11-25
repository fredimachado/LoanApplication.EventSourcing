namespace LoanApplication.EventSourcing.Shared.Events;

public record LoanDenied(Guid Id, DecisionType DecisionType, string Responsible, string Reason, DateTimeOffset Timestamp) : IEvent;
