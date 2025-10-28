using KurrentDB.Client;
using LoanApplication.EventSourcing.CreditCheck;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddKurrentDBClient("eventstore");
builder.Services.AddSingleton(
    new KurrentDBPersistentSubscriptionsClient(KurrentDBClientSettings.Create(builder.Configuration.GetConnectionString("eventstore")!)));
builder.Services.AddHostedService<CreditCheckService>();

var host = builder.Build();
host.Run();
