using LoanApplication.EventSourcing.Shared.Events;

namespace LoanApplication.EventSourcing.AutomatedApplicants;

public record LoanRequest(LoanRequested.CustomerInformation CustomerInformation, decimal LoanAmount);
