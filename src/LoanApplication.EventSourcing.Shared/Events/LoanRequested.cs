namespace LoanApplication.EventSourcing.Shared.Events;

public sealed record LoanRequested(Guid Id, LoanRequested.CustomerInformation Customer, decimal LoanAmount, DateTimeOffset Timestamp) : IEvent
{
    public sealed record CustomerInformation(string FullName, string NationalId, CustomerAddress Address);
    public sealed record CustomerAddress(string Street, string City, string State);
}


