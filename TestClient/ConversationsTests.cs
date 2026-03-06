// See https://aka.ms/new-console-template for more information
using AlexandreHtrb.WebSocketExtensions;
using TestShared;
using System.Net;
using System.Net.WebSockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestClient;

// TODO: Make tests for compression.
// Currently (.NET 10) we can't verify whether 
// messages arrive compressed or decompressed.

[TestClass]
public class ConversationsTests
{
    [TestMethod]
    [DataRow(1.1d, "ws://localhost:5000/test/http1websocket", false)]
    [DataRow(1.1d, "ws://localhost:5000/test/http1websocket", true)]
    [DataRow(2.0d, "wss://localhost:5001/test/http2websocket", false)]
    [DataRow(2.0d, "wss://localhost:5001/test/http2websocket", true)]
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
            Assert.IsExactInstanceOfType<FileStream>(msgs[10].BytesStream);

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

    [TestMethod]
    [DataRow(1.1d, "ws://localhost:5000/test/http1websocket")]
    [DataRow(2.0d, "wss://localhost:5001/test/http2websocket")]
    public async Task Should_run_WebSocket_conversation_successfully_where_client_disconnects(double httpVersion, string url)
    {
        // GIVEN
        using var cws = MakeClientWebSocket((decimal)httpVersion);
        using var hc = MakeHttpClient(disableSslVerification: true);
        var wsc = new WebSocketClientSideConnector(collectOnlyServerSideMessages: false);
        var uri = new Uri(url);
        await wsc.ConnectAsync(cws, hc, uri, default);

        // WHEN AND THEN
        Assert.AreEqual(WebSocketConnectionState.Connected, wsc.ConnectionState);
        var msgs = await new ClientDisconnectConversation(wsc).RunAsync(default);
        Assert.AreEqual(WebSocketConnectionState.Disconnected, wsc.ConnectionState);

        // THEN
        Assert.HasCount(2, msgs);

        Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[0].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[0].Type);
        Assert.AreEqual("Hello!", msgs[0].ReadAsUtf8Text());

        Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[1].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[1].Type);
        Assert.AreEqual("Hi!", msgs[1].ReadAsUtf8Text());
    }

    [TestMethod]
    [DataRow(1.1d, "ws://localhost:5000/test/http1websocket")]
    [DataRow(2.0d, "wss://localhost:5001/test/http2websocket")]
    public async Task Should_run_WebSocket_conversation_successfully_where_client_sends_closure_message(double httpVersion, string url)
    {
        // GIVEN
        using var cws = MakeClientWebSocket((decimal)httpVersion);
        using var hc = MakeHttpClient(disableSslVerification: true);
        var wsc = new WebSocketClientSideConnector(collectOnlyServerSideMessages: false);
        var uri = new Uri(url);
        await wsc.ConnectAsync(cws, hc, uri, default);

        // WHEN AND THEN
        Assert.AreEqual(WebSocketConnectionState.Connected, wsc.ConnectionState);
        var msgs = await new ClientClosureMessageConversation(wsc).RunAsync(default);
        Assert.AreEqual(WebSocketConnectionState.Disconnected, wsc.ConnectionState);

        // THEN
        Assert.HasCount(3, msgs);

        Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[0].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[0].Type);
        Assert.AreEqual("Hello!", msgs[0].ReadAsUtf8Text());

        Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[1].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[1].Type);
        Assert.AreEqual("Hi!", msgs[1].ReadAsUtf8Text());

        Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[2].Direction);
        Assert.AreEqual(WebSocketMessageType.Close, msgs[2].Type);
        Assert.AreEqual("Adiós muchacho", msgs[2].ReadAsUtf8Text());
    }

    [TestMethod]
    [DataRow(1.1d, "ws://localhost:5000/test/http1websocket")]
    [DataRow(2.0d, "wss://localhost:5001/test/http2websocket")]
    public async Task Should_run_WebSocket_conversation_successfully_where_server_sends_closure_message(double httpVersion, string url)
    {
        // GIVEN
        using var cws = MakeClientWebSocket((decimal)httpVersion);
        using var hc = MakeHttpClient(disableSslVerification: true);
        var wsc = new WebSocketClientSideConnector(collectOnlyServerSideMessages: false);
        var uri = new Uri(url);
        await wsc.ConnectAsync(cws, hc, uri, default);

        // WHEN AND THEN
        Assert.AreEqual(WebSocketConnectionState.Connected, wsc.ConnectionState);
        var msgs = await new ServerClosureMessageConversation(wsc).RunAsync(default);
        Assert.AreEqual(WebSocketConnectionState.Disconnected, wsc.ConnectionState);

        // THEN
        Assert.HasCount(4, msgs);

        Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[0].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[0].Type);
        Assert.AreEqual("Hello!", msgs[0].ReadAsUtf8Text());

        Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[1].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[1].Type);
        Assert.AreEqual("Hi!", msgs[1].ReadAsUtf8Text());

        Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[2].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[2].Type);
        Assert.AreEqual("I can't live without you!", msgs[2].ReadAsUtf8Text());

        Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[3].Direction);
        Assert.AreEqual(WebSocketMessageType.Close, msgs[3].Type);
        Assert.AreEqual("Yes, you can. Bye!", msgs[3].ReadAsUtf8Text());
    }

    [TestMethod]
    [DataRow(1.1d, "ws://localhost:5000/test/http1websocket", null)]
    [DataRow(1.1d, "ws://localhost:5000/test/http1websocket", "gamma")]
    [DataRow(2.0d, "wss://localhost:5001/test/http2websocket", null)]
    [DataRow(2.0d, "wss://localhost:5001/test/http2websocket", "gamma")]
    public async Task Should_run_WebSocket_conversation_successfully_with_or_without_subprotocol_also_check_server_disconnect(double httpVersion, string url, string? subprotocol)
    {
        // GIVEN
        using var cws = MakeClientWebSocket((decimal)httpVersion, subprotocol);
        using var hc = MakeHttpClient(disableSslVerification: true);
        var wsc = new WebSocketClientSideConnector(collectOnlyServerSideMessages: false);
        var uri = new Uri(url);
        await wsc.ConnectAsync(cws, hc, uri, default);

        // WHEN AND THEN
        Assert.AreEqual(WebSocketConnectionState.Connected, wsc.ConnectionState);
        var msgs = await new SubprotocolAndServerDisconnectConversation(wsc).RunAsync(default);
        Assert.AreEqual(WebSocketConnectionState.Disconnected, wsc.ConnectionState);

        // THEN
        Assert.HasCount(4, msgs);

        Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[0].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[0].Type);
        Assert.AreEqual("Hello!", msgs[0].ReadAsUtf8Text());

        Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[1].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[1].Type);
        Assert.AreEqual("Hi!", msgs[1].ReadAsUtf8Text());

        Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[2].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[2].Type);
        Assert.AreEqual("Which subprotocol are we on?", msgs[2].ReadAsUtf8Text());

        if (subprotocol == null)
        {
            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[3].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[3].Type);
            Assert.AreEqual("No subprotocol specified.", msgs[3].ReadAsUtf8Text());
        }
        else
        {
            Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[3].Direction);
            Assert.AreEqual(WebSocketMessageType.Text, msgs[3].Type);
            Assert.AreEqual("We are on subprotocol 'gamma'.", msgs[3].ReadAsUtf8Text());
        }
    }

    [TestMethod]
    [DataRow(1.1d, "ws://localhost:5000/test/http1websocket")]
    [DataRow(2.0d, "wss://localhost:5001/test/http2websocket")]
    public async Task Should_run_WebSocket_conversation_successfully_where_server_throws_exception(double httpVersion, string url)
    {
        // GIVEN
        using var cws = MakeClientWebSocket((decimal)httpVersion);
        using var hc = MakeHttpClient(disableSslVerification: true);
        var wsc = new WebSocketClientSideConnector(collectOnlyServerSideMessages: false);
        var uri = new Uri(url);
        await wsc.ConnectAsync(cws, hc, uri, default);

        // WHEN AND THEN
        Assert.AreEqual(WebSocketConnectionState.Connected, wsc.ConnectionState);
        var msgs = await new ServerExceptionConversation(wsc).RunAsync(default);
        Assert.AreEqual(WebSocketConnectionState.Disconnected, wsc.ConnectionState);

        // THEN
        Assert.HasCount(3, msgs);

        Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[0].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[0].Type);
        Assert.AreEqual("Hello!", msgs[0].ReadAsUtf8Text());

        Assert.AreEqual(WebSocketMessageDirection.FromServer, msgs[1].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[1].Type);
        Assert.AreEqual("Hi!", msgs[1].ReadAsUtf8Text());

        Assert.AreEqual(WebSocketMessageDirection.FromClient, msgs[2].Direction);
        Assert.AreEqual(WebSocketMessageType.Text, msgs[2].Type);
        Assert.AreEqual("Throw an Exception!", msgs[2].ReadAsUtf8Text());
    }

    internal static async Task<bool> CheckIfFilesContentsAreEqualAsync(string filePath1, string filePath2)
    {
        if (!File.Exists(filePath1) || !File.Exists(filePath2))
            return false;

        using FileStream fs1 = File.OpenRead(filePath1);
        using FileStream fs2 = File.OpenRead(filePath2);
        using MemoryStream ms1 = new();
        using MemoryStream ms2 = new();
        await fs1.CopyToAsync(ms1, bufferSize: 4096);
        await fs2.CopyToAsync(ms2, bufferSize: 4096);
        return Enumerable.SequenceEqual(ms1.ToArray(), ms2.ToArray());
    }

    #region SETUP

    static ClientWebSocket MakeClientWebSocket(decimal httpVersion, string? subprotocol = null)
    {
        ClientWebSocket cws = new();
        cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        cws.Options.HttpVersion = new((int)httpVersion, (int)(httpVersion * 10) % 10);
        cws.Options.CollectHttpResponseDetails = true;
        if (subprotocol != null)
        {
            cws.Options.AddSubProtocol(subprotocol);
        }
        // if (enableCompression)
        // {
        //     cws.Options.DangerousDeflateOptions = new();
        // }
        return cws;
    }

    static HttpClient MakeHttpClient(bool disableSslVerification)
    {
        /*
        https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0#alternatives-to-ihttpclientfactory-1
        https://stackoverflow.com/questions/48778580/singleton-httpclient-vs-creating-new-httpclient-request/68495953#68495953
        */
        SocketsHttpHandler httpHandler = new()
        {
            // Sets how long a connection can be in the pool to be considered reusable (by default - infinite)
            PooledConnectionLifetime = TimeSpan.FromMinutes(20),
            AutomaticDecompression = DecompressionMethods.All
        };

        if (disableSslVerification)
        {
            httpHandler.SslOptions.RemoteCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;
        }

        HttpClient httpClient = new(httpHandler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
        return httpClient;
    }

    #endregion
}
