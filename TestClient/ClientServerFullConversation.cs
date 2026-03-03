using AlexandreHtrb.WebSocketExtensions;
using TestShared;
using System.Net.WebSockets;
using static TestShared.BloodTypeExtensions;

namespace TestClient
{
    internal sealed class ClientServerFullConversation : BaseConversation
    {
        internal ClientServerFullConversation(WebSocketClientSideConnector wsc) : base(wsc)
        {
        }

        protected override async Task ReplyMessageAsync(WebSocketMessage msg, string msgText)
        {
            // Expected conversation:
            //  1) Client: Hello!
            //  2) Server: Hi!
            //  3) Client: What time is it?
            //  4) Server: Now it's 14:53.13
            //  5) Client: Thanks!
            //  6) Client: Server, send me an image!
            //  7) Server: <large_file.jpg>
            //  8) Server: Your turn! Client, send me an image!
            //  9) Client: <small_file.jpg>
            // 10) Server: I will close this connection in around 8s.

            switch (msg.Type, msgText)
            {
                case (WebSocketMessageType.Text, "Hi!"):
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, "What time is it?", false);
                    break;
                case (WebSocketMessageType.Text, _) when msgText.StartsWith("Now it's"):
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, "Thanks!", false);
                    //await wsc.DisconnectAsync();
                    break;
                case (WebSocketMessageType.Text, "You're welcome!"):
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, "Server, send me an image!", false);
                    break;
                case (WebSocketMessageType.Text, "Your turn! Client, send me an image!"):
                    string smallFilePath = GetClientExampleFilePath("small_file.jpg");
                    FileStream fs = new(smallFilePath, FileMode.Open);
                    await wsc.SendMessageAsync(WebSocketMessageType.Binary, fs, false);
                    break;
                case (WebSocketMessageType.Binary, _) when msg.Direction == WebSocketMessageDirection.FromServer:
                    string receivedFilePath = GetClientExampleFilePath("received_img.jpg");
                    using (FileStream fs2 = new(receivedFilePath, FileMode.Create))
                    {
                        await msg.BytesStream.CopyToAsync(fs2);
                    }
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, "Server, send me a JSON!", false);
                    break;
                case (WebSocketMessageType.Text, _) when msgText.StartsWith('{'): // JSON
                    var personReceived = msg.ReadAsUtf8Json(AppJsonSrcGenContext.Default.Person)!;
                    Console.WriteLine($"Received Person JSON:\nName: {personReceived.Name}\nAge: {personReceived.Age}\nBlood type: {ConvertToString(personReceived.BloodType)}");
                    break;
                case (WebSocketMessageType.Text, "Your turn! Client, send me a JSON!"):
                    Person personSent = new("Joey", 21, BloodType.OPositive);
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, personSent, AppJsonSrcGenContext.Default.Person, false);
                    break;
                case (WebSocketMessageType.Text, _) when msgText == "I don't understand this message!":
                case (WebSocketMessageType.Text, _) when msgText.StartsWith("I will close this connection in around"):
                default:
                    break;
                case (_, _):
                    await wsc.SendMessageAsync(WebSocketMessageType.Text, "I don't understand this message!", false);
                    break;
            }
        }

        internal static DirectoryInfo GetProjectDir() =>
#if DEBUG
            new DirectoryInfo(Directory.GetCurrentDirectory()).Parent!.Parent!.Parent!;
#else
            new DirectoryInfo(Directory.GetCurrentDirectory())!;
#endif

        internal static string GetClientExampleFilePath(string testFileName)
        {
            string rootPath = GetProjectDir().FullName;
            return Path.Combine(rootPath, testFileName);
        }

        internal static string GetServerExampleFilePath(string testFileName)
        {
            string rootPath = GetProjectDir().Parent!.FullName;
            return Path.Combine(rootPath, "TestServer", testFileName);
        }
    }
}
