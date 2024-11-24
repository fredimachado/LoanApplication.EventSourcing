using LoanApplication.EventSourcing.AutomatedApplicants;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddEventStoreClient("eventstore");
builder.Services.AddHostedService<AutomatedLoanRequests>();

var host = builder.Build();
host.Run();
