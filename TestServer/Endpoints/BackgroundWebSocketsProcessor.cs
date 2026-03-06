using AlexandreHtrb.WebSocketExtensions;
using TestShared;
using System.Net.WebSockets;
using static TestShared.BloodTypeExtensions;

namespace TestServer.Endpoints;

public static class BackgroundWebSocketsProcessor
{
    private static readonly TimeSpan maximumLifetimePeriod = TimeSpan.FromSeconds(6);

    public static async Task RegisterAndProcessAsync(ILogger<WebSocketServerSideConnector> logger, WebSocket ws, string? subprotocol, TaskCompletionSource<object> socketFinishedTcs)
    {
        WebSocketServerSideConnector wsc = new(ws, collectOnlyClientSideMessages: false);

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            await wsc.SendMessageAsync(WebSocketMessageType.Text, "I will close this connection in around 3s.", false);
        });

        _ = Task.Run(async () =>
        {
            await Task.Delay(maximumLifetimePeriod);
            await wsc.DisconnectAsync();
        });

        int msgCount = 0;
        await foreach (var msg in wsc.ExchangedMessagesCollector!.ReadAllAsync())
        {
            msgCount++;
            string msgText = msg.Type switch
            {
                WebSocketMessageType.Text or WebSocketMessageType.Close => msg.ReadAsUtf8Text()!,
                WebSocketMessageType.Binary when msg.BytesStream is MemoryStream ms => $"(binary, {ms.Length} bytes)",
                WebSocketMessageType.Binary when msg.BytesStream is not MemoryStream => $"(binary, ? bytes)",
                _ => "(unknown)"
            };
            logger.LogInformation("Message {msgCount}, {direction}: {msgText}", msgCount, msg.Direction, msgText);

            if (msg.Direction == WebSocketMessageDirection.FromServer)
            {
                continue;
            }

            switch (msg.Type, msgText)
            {
                case (WebSocketMessageType.Text, "Hello!"):
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, "Hi!", false);
                    break;
                case (WebSocketMessageType.Text, "What time is it?"):
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, "Now it's " + DateTime.Now.TimeOfDay, false);
                    break;
                case (WebSocketMessageType.Text, "Thanks!"):
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, "You're welcome!", false);
                    break;
                case (WebSocketMessageType.Text, "Server, send me an image!"):
                    string largeFilePath = GetServerExampleFilePath("large_file.jpg");
                    FileStream fs = new(largeFilePath, FileMode.Open);
                    await wsc.SendMessageAsync(WebSocketMessageType.Binary, fs, false);
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, "Your turn! Client, send me an image!", false);
                    break;
                case (WebSocketMessageType.Binary, _) when msg.Direction == WebSocketMessageDirection.FromClient:
                    string receivedFilePath = GetServerExampleFilePath("received_img.jpg");
                    using (FileStream fs2 = new(receivedFilePath, FileMode.Create))
                    {
                        await msg.BytesStream.CopyToAsync(fs2);
                    }
                    break;
                case (WebSocketMessageType.Text, "Server, send me a JSON!"):
                    Person personSent = new("Phoebe", 25, BloodType.ABNegative);
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, personSent, AppJsonSrcGenContext.Default.Person, false);
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, "Your turn! Client, send me a JSON!", false);
                    break;
                case (WebSocketMessageType.Text, _) when msgText.StartsWith('{'): // JSON
                    var personReceived = msg.ReadAsUtf8Json(AppJsonSrcGenContext.Default.Person)!;
                    logger.LogInformation("Received Person JSON:\nName: {name}\nAge: {age}\nBlood type: {bloodType}", personReceived.Name, personReceived.Age, ConvertToString(personReceived.BloodType));
                    break;
                case (WebSocketMessageType.Text, "I can't live without you!"):
                    await wsc.SendMessageAsync(WebSocketMessageType.Close, "Yes, you can. Bye!", false);
                    break;
                case (WebSocketMessageType.Text, "Throw an Exception!"):
                    throw new Exception("AI MORREU");
                case (WebSocketMessageType.Text, "Make me throw an Exception!"):
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, "Throw an Exception!", false);
                    break;
                case (WebSocketMessageType.Text, "Which subprotocol are we on?"):
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, subprotocol switch
                    {
                        null or "" => "No subprotocol specified.",
                        _ => $"We are on subprotocol '{subprotocol}'."
                    }, false);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await wsc.DisconnectAsync();
                    break;
                case (WebSocketMessageType.Text, _) when msgText == "I don't understand this message!":
                case (WebSocketMessageType.Text, _) when msgText.StartsWith("I will close this connection in around"):
                case (WebSocketMessageType.Close, _):
                default:
                    break;
                case (_, _):
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, "I don't understand this message!", false);
                    break;
            }
        }
        socketFinishedTcs.SetResult(true);
    }
    private static DirectoryInfo GetProjectDir() =>
        new DirectoryInfo(Directory.GetCurrentDirectory());

    private static string GetServerExampleFilePath(string testFileName)
    {
        string rootPath = GetProjectDir().FullName;
        return Path.Combine(rootPath, testFileName);
    }
}
