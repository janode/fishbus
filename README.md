# fishbus

Tiny tiny library for receiving azure service bus messages

### Install

dotnet add package fishbus

### Note!

release v0.2.8 has a breaking change.
Interface IHandleMessage was changed again.
Handle should return HandlerResult to mark success, fail or abort
If abort is returned from a handler the message will immediately be moved to Dead letter queue,
and not retried.

release v0.2.7 has a breaking change.
Interface IHandleMessage was changed.
Handle no longer takes a delegate argument and it should return true/false to mark success or not

### Declare a message type (an Event or a Command)

```c#
public class SomethingExcitingJustHappened
{
    public string Somedata { get; set; }
}
```

### Write a handler for the message

```c#
public class DeleteUserHandler : IHandleMessage<SomethingExcitingJustHappened>
{
    public async Task<HandlerResult> Handle(SomethingExcitingJustHappened message)
    {
        Log.Information("Received SomethingExcitingJustHappened");
        return HandlerResult.Success();
    }
}
```

### Configure the host for your handlers

Here we just have a console app, with a Program.cs, but this could just as well be a web app.
We just need a host that can host a Microsoft.Extensions.Hosting.IHostedService implementation.

```c#
public static async Task<int> Main(string[] args)
{
    try
    {
        await new HostBuilder()
            .ConfigureHostConfiguration(configHost =>
            {
                configHost.SetBasePath(Directory.GetCurrentDirectory());
                configHost.AddEnvironmentVariables(prefix: "ASPNETCORE_");
            })
            .ConfigureServices((hostContext, services) =>
            {
                services
                    .ConfigureMessaging() //will register messagehandlers from current assembly
                    .Configure<MessageSources>(configuration.GetSection("MessageSources")); //register the MessageSources
            })
            .RunConsoleAsync();

        return 0;
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Host terminated unexpectedly");
        return 1;
    }
}
```

### Configuration - appsettings.json

The following appsettings.json file could be used to configure the messagsources that the application will listen to

```json
{
  "MessageSources": {
    "Subscriptions": [
      {
        "ConnectionString": "",
        "Name": "<name of topic>"
      }
    ],
    "Queues": [
      {
        "ConnectionString": ""
      }
    ]
  }
}
```

## Sending Messages

```csharp
[MessageLabel("My.Message.Label")]
public class MyMessage
{
    public string A { get; set; }
    public string B { get; set; }
}

...

var publisher = new MessagePublisher(connectionString);

var myMessage = new MyMessage
{
    A = "a",
    B = "b"
};

await publisher.SendAsync(myMessage);

```

## Sending Message with custom Message Id

To leverage Azure Service Bus duplicate detection the MessageId should be set to an identifier based on your internal business logic. With Fishbus, Azure Service Bus MessageId logic can be overriden by adding the `[MessageId]` attribute to your custom message id property.

```csharp
[MessageLabel("My.Message.With.Custom.MessageId")]
public class MyMessage
{
    [MessageId]
    public string MyId { get; set; }
    public string A { get; set; }
    public string B { get; set; }
}

...

var publisher = new MessagePublisher(connectionString);

var myMessage = new MyMessage
{
    MyId = "id",
    A = "a",
    B = "b"
};

var duplicateMessage = new MyMessage
{
    MyId = "id",
    A = "a",
    B = "b"
};

await publisher.SendAsync(myMessage);
await publisher.SendAsync(duplicateMessage); // Will be discarded by Azure Service Bus if duplicate detection activated and message sent within the duplicate detection history window.
```

## Sending Message with TimeToLive Attribute

To override Azure Service Bus messages TimeToLive property you can, in your message, set a TimeToLive (the property name is not important) property of type `TimeSpan` which is tagged by a `[TimeToLive]` attribute. Only one property can be tagged as a `[TimeToLive]` attribute. The property will be used as the messages TimeToLive if the time span is shorter than the queues DefaultTimeToLive. Otherwise it will silently be adjusted to the default value.

```csharp
[MessageLabel("My.Message.With.Custom.TimeToLive.Attribute")]
public class MyMessage
{
    [TimeToLive]
    public TimeSpan TimeToLive { get; set; }

    public string A { get; set; }
}

...

var publisher = new MessagePublisher(connectionString);

var myMessage = new MyMessage
{
    TimeToLive = new TimeSpan(1, 0, 0)
    A = "a",
};

await publisher.SendAsync(myMessage); // TimeToLive for the message will now be set to 1 hour
```
