using LoanApplication.EventSourcing.Shared.Events;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LoanApplication.EventSourcing.Underwriting.WebApp.Pages;

public class IndexModel(LoanRequestRepository loanRequestRepository, ILogger<IndexModel> logger) : PageModel
{
    private readonly LoanRequestRepository _loanRequestRepository = loanRequestRepository;
    private readonly ILogger<IndexModel> _logger = logger;

    public IList<ManualApprovalRequired>? ManualApprovals { get; set; }

    public void OnGet()
    {
        ManualApprovals = _loanRequestRepository.GetManualApprovals();
    }
}
