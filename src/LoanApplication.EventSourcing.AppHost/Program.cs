var builder = DistributedApplication.CreateBuilder(args);

var eventstore = builder.AddEventStore("eventstore", 32113)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", "true"); // https://github.com/EventStore/EventStore/issues/2640

builder.AddProject<Projects.LoanApplication_EventSourcing_AutomatedApplicants>("automated-applicants")
    .WithReference(eventstore)
    .WaitFor(eventstore);

builder.AddProject<Projects.LoanApplication_EventSourcing_CreditCheck>("credit-check")
    .WithReference(eventstore)
    .WaitFor(eventstore);

builder.AddProject<Projects.LoanApplication_EventSourcing_DecisionEngine>("decision-engine")
    .WithReference(eventstore)
    .WaitFor(eventstore);

builder.AddProject<Projects.LoanApplication_EventSourcing_Underwriting_WebApp>("underwriting-webapp")
    .WithReference(eventstore)
    .WaitFor(eventstore);

builder.Build().Run();
