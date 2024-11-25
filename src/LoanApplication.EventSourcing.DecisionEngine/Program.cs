using EventStore.Client;
using LoanApplication.EventSourcing.DecisionEngine;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddEventStoreClient("eventstore");
builder.Services.AddSingleton(
    new EventStorePersistentSubscriptionsClient(EventStoreClientSettings.Create(builder.Configuration.GetConnectionString("eventstore")!)));
builder.Services.AddHostedService<DecisionEngineService>();

var host = builder.Build();
host.Run();
