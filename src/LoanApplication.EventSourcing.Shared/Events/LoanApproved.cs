namespace LoanApplication.EventSourcing.Shared.Events;

public record LoanApproved(Guid Id, DecisionType DecisionType, string Responsible, string Reason, DateTimeOffset Timestamp) : IEvent;
