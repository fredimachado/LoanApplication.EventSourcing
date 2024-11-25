using EventStore.Client;
using System.Text.Json;
using System.Text;
using LoanApplication.EventSourcing.Shared.Events;

namespace LoanApplication.EventSourcing.Shared;

public static class EventStoreExtensions
{
    public static T? Deserialize<T>(this ResolvedEvent resolvedEvent) where T : class
    {
        try
        {
            var eventClrTypeName = JsonDocument.Parse(resolvedEvent.Event.Metadata)
                .RootElement
                .GetProperty("EventClrTypeName")
                .GetString();

            return JsonSerializer.Deserialize(
                Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span),
                Type.GetType(eventClrTypeName!)!) as T;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Exception: {exception.Message}");
            Console.WriteLine($"Stack Trace: {exception.StackTrace}");
            if (exception.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {exception.InnerException.Message}");
                Console.WriteLine($"Inner Stack Trace: {exception.InnerException.StackTrace}");
            }
        }

        return null;
    }

    public static EventData Serialize<T>(this T @event) where T : class, IEvent
    {
        return new EventData(
            Uuid.NewUuid(),
            @event.GetType().Name,
            data: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event, @event.GetType())),
            metadata: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "EventClrTypeName", @event.GetType().AssemblyQualifiedName! }
                }))
        );
    }
}
