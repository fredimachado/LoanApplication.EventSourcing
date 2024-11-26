using EventStore.Client;
using LoanApplication.EventSourcing.Underwriting.WebApp;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddEventStoreClient("eventstore");

builder.Services.AddSingleton(
    new EventStorePersistentSubscriptionsClient(EventStoreClientSettings.Create(builder.Configuration.GetConnectionString("eventstore")!)));
builder.Services.AddHostedService<UnderwritingService>();
builder.Services.AddSingleton<LoanRequestRepository>();

builder.Services.AddRazorPages();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapHub<UnderwritingHub>("/underwritingHub");

app.Run();
