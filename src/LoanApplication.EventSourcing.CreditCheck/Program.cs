using EventStore.Client;
using LoanApplication.EventSourcing.CreditCheck;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddEventStoreClient("eventstore");
builder.Services.AddSingleton(
    new EventStorePersistentSubscriptionsClient(EventStoreClientSettings.Create(builder.Configuration.GetConnectionString("eventstore")!)));
builder.Services.AddHostedService<CreditCheckService>();

var host = builder.Build();
host.Run();
