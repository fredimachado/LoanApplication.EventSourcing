using LoanApplication.EventSourcing.Shared.Events;

namespace LoanApplication.EventSourcing.Underwriting.WebApp;

public class LoanRequestRepository
{
    private readonly List<ManualApprovalRequired> manualApprovals = new();

    public void AddManualApproval(ManualApprovalRequired manualApproval)
    {
        manualApprovals.Add(manualApproval);
    }

    public List<ManualApprovalRequired> GetManualApprovals()
    {
        return manualApprovals;
    }
}
