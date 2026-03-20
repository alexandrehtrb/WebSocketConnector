using AlexandreHtrb.WebSocketExtensions;
using System.Net.WebSockets;
using TestClient.Conversations;
using TestShared;

namespace TestClient;

[TestClass]
public class FullConversationsTest : BaseConversationTest
{
    [TestMethod]
    [DataRow(1.1d, wsHttp1Url, false)]
    [DataRow(1.1d, wsHttp1Url, true)]
    [DataRow(2.0d, wsHttp2Url, false)]
    [DataRow(2.0d, wsHttp2Url, true)]
    public async Task Should_run_WebSocket_full_conversation_successfully(double httpVersion, string url, bool collectOnlyServerSideMessages)
    {
        // GIVEN
        using var cws = MakeClientWebSocket((decimal)httpVersion);
        using var hc = MakeHttpClient(disableSslVerification: true);
        var wsc = new WebSocketClientSideConnector(collectOnlyServerSideMessages);
        var uri = new Uri(url);
        await wsc.ConnectAsync(cws, hc, uri, default);

        // WHEN AND THEN
        Assert.AreEqual(WebSocketConnectionState.Connected, wsc.ConnectionState);
        var msgs = await new ClientServerFullConversation(wsc).RunAsync(default);
        await Task.Delay(500);
        Assert.AreEqual(WebSocketConnectionState.Disconnected, wsc.ConnectionState);

        // THEN

        if (!collectOnlyServerSideMessages)
        {
            Assert.HasCount(15, msgs);

            Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[0].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[0].Type);
            Assert.AreEqual("Hello!", msgs[0].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[1].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[1].Type);
            Assert.AreEqual("Hi!", msgs[1].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[2].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[2].Type);
            Assert.AreEqual("What time is it?", msgs[2].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[3].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[3].Type);
            Assert.StartsWith("Now it's ", msgs[3].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[4].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[4].Type);
            Assert.AreEqual("Thanks!", msgs[4].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[5].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[5].Type);
            Assert.AreEqual("You're welcome!", msgs[5].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[6].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[6].Type);
            Assert.AreEqual("Server, send me an image!", msgs[6].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[7].Direction);
            Assert.AreEqual(WebSocketMessageType.Binary, msgs[7].Type); // Large image

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[8].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[8].Type);
            Assert.AreEqual("Your turn! Client, send me an image!", msgs[8].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[9].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[9].Type);
            Assert.AreEqual("Server, send me a JSON!", msgs[9].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[10].Direction);
            Assert.AreEqual(WebSocketMessageType.Binary, msgs[10].Type);
            Assert.IsExactInstanceOfType<FileStream>(msgs[10].ReadAsStream());

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[11].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[11].Type);
            Assert.AreEqual("{\"name\":\"Phoebe\",\"age\":25,\"bloodType\":\"AB-\"}", msgs[11].ReadAsUtf8Text());
            var personReceived = msgs[11].ReadAsUtf8Json(AppJsonSrcGenContext.Default.Person);
            Assert.IsNotNull(personReceived);
            Assert.AreEqual("Phoebe", personReceived.Name);
            Assert.AreEqual(25, personReceived.Age);
            Assert.AreEqual(BloodType.ABNegative, personReceived.BloodType);

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[12].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[12].Type);
            Assert.AreEqual("Your turn! Client, send me a JSON!", msgs[12].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[13].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[13].Type);
            Assert.AreEqual("{\"name\":\"Joey\",\"age\":21,\"bloodType\":\"O+\"}", msgs[13].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[14].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[14].Type);
            Assert.AreEqual("I will close this connection in around 3s.", msgs[14].ReadAsUtf8Text());
        }
        else
        {
            Assert.HasCount(8, msgs);

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[0].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[0].Type);
            Assert.AreEqual("Hi!", msgs[0].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[1].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[1].Type);
            Assert.StartsWith("Now it's ", msgs[1].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[2].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[2].Type);
            Assert.AreEqual("You're welcome!", msgs[2].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[3].Direction);
            Assert.AreEqual(WebSocketMessageType.Binary, msgs[3].Type); // Large image

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[4].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[4].Type);
            Assert.AreEqual("Your turn! Client, send me an image!", msgs[4].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[5].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[5].Type);
            Assert.AreEqual("{\"name\":\"Phoebe\",\"age\":25,\"bloodType\":\"AB-\"}", msgs[5].ReadAsUtf8Text());
            var personReceived = msgs[5].ReadAsUtf8Json(AppJsonSrcGenContext.Default.Person);
            Assert.IsNotNull(personReceived);
            Assert.AreEqual("Phoebe", personReceived.Name);
            Assert.AreEqual(25, personReceived.Age);
            Assert.AreEqual(BloodType.ABNegative, personReceived.BloodType);

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[6].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[6].Type);
            Assert.AreEqual("Your turn! Client, send me a JSON!", msgs[6].ReadAsUtf8Text());

            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[7].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[7].Type);
            Assert.AreEqual("I will close this connection in around 3s.", msgs[7].ReadAsUtf8Text());
        }

        string smallImgFilePath = ClientServerFullConversation.GetClientExampleFilePath("small_file.jpg");
        string largeImgFilePath = ClientServerFullConversation.GetServerExampleFilePath("large_file.jpg");
        string sentImgFilePath = ClientServerFullConversation.GetServerExampleFilePath("received_img.jpg");
        string receivedImgFilePath = ClientServerFullConversation.GetClientExampleFilePath("received_img.jpg");

        Console.WriteLine("Small img file path: " + smallImgFilePath);
        Console.WriteLine("Sent img file path: " + sentImgFilePath);
        Console.WriteLine("Large img file path: " + largeImgFilePath);
        Console.WriteLine("Received img file path: " + receivedImgFilePath);

        bool sentImgEquality = await CheckIfFilesContentsAreEqualAsync(smallImgFilePath, sentImgFilePath);

        Assert.AreEqual(9784, new FileInfo(smallImgFilePath).Length);
        Assert.AreEqual(9784, new FileInfo(sentImgFilePath).Length);
        Assert.IsTrue(sentImgEquality);

        bool receivedImgEquality = await CheckIfFilesContentsAreEqualAsync(largeImgFilePath, receivedImgFilePath);

        Assert.AreEqual(191316, new FileInfo(largeImgFilePath).Length);
        Assert.AreEqual(191316, new FileInfo(receivedImgFilePath).Length);
        Assert.IsTrue(receivedImgEquality);
    }
}