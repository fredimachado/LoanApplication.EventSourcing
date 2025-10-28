using KurrentDB.Client;
using LoanApplication.EventSourcing.Shared;
using LoanApplication.EventSourcing.Shared.Events;

namespace LoanApplication.EventSourcing.AutomatedApplicants;

public class AutomatedLoanRequests(KurrentDBClient kurrentDBClient, IServiceScopeFactory serviceScopeFactory, IHostApplicationLifetime applicationLifetime, ILogger<AutomatedLoanRequests> logger) : BackgroundService
{
    private readonly KurrentDBClient _kurrentDBClient = kurrentDBClient;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime;
    private readonly ILogger<AutomatedLoanRequests> _logger = logger;

    private readonly List<LoanRequest> _loanRequests =
    [
        new(new LoanRequested.CustomerInformation(
                "Alice Spencer",
                "1234567890",
                new LoanRequested.CustomerAddress(
                    "123 Some Street",
                    "Some City",
                    "Queensland")),
            10000),
        new(new LoanRequested.CustomerInformation(
                "Bob Johnson",
                "0987654321",
                new LoanRequested.CustomerAddress(
                    "456 Another Ave",
                    "Another City",
                    "New South Wales")),
            15000),
        new(new LoanRequested.CustomerInformation(
                "Charlie Brown",
                "1122334455",
                new LoanRequested.CustomerAddress(
                    "789 Different Blvd",
                    "Different City",
                    "Victoria")),
            20000),
        new(new LoanRequested.CustomerInformation(
                "Diana Prince",
                "6677889900",
                new LoanRequested.CustomerAddress(
                    "101 Unique Rd",
                    "Unique City",
                    "Tasmania")),
            25000)
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var loanRequest in _loanRequests)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            var eventToAppend = new LoanRequested(
                Guid.NewGuid(),
                loanRequest.CustomerInformation,
                loanRequest.LoanAmount,
                DateTimeOffset.UtcNow);

            _logger.LogInformation("Appending event {EventId} to stream. {@LoanRequested}", eventToAppend.Id, eventToAppend);

            await _kurrentDBClient.AppendToStreamAsync(
                $"loanRequest-{eventToAppend.Id:N}",
                StreamState.Any,
                [eventToAppend.Serialize()],
                cancellationToken: stoppingToken);

            _logger.LogInformation("Event {EventId} appended to stream.", eventToAppend.Id);

            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("All loan requests have been appended to the stream.");

        _applicationLifetime.StopApplication();
    }
}
