namespace LoanApplication.EventSourcing.Shared.Events;

public record ManualApprovalRequired(Guid Id, DecisionType DecisionType, string Responsible, string Reason, DateTimeOffset Timestamp) : IEvent;
