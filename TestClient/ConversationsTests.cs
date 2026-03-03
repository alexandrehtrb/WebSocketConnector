// See https://aka.ms/new-console-template for more information
using AlexandreHtrb.WebSocketExtensions;
using TestShared;
using System.Net;
using System.Net.WebSockets;
using Xunit;

[assembly: CaptureConsole]

namespace TestClient;

// TODO: Make tests for compression.
// Currently (.NET 10) we can't whether messages arrive compressed or decompressed.

public static class ConversationsTests
{
    [Theory]
    [InlineData(1.1f, "ws://localhost:5000/test/http1websocket")]
    [InlineData(2.0f, "wss://localhost:5001/test/http2websocket")]
    public static async Task Should_run_WebSocket_full_conversation_successfully(float httpVersion, string url)
    {
        // GIVEN
        using var cws = MakeClientWebSocket((decimal)httpVersion);
        using var hc = MakeHttpClient(disableSslVerification: true);
        var wsc = new WebSocketClientSideConnector();
        var uri = new Uri(url);
        await wsc.ConnectAsync(cws, hc, uri, TestContext.Current.CancellationToken);

        // WHEN AND THEN
        Assert.Equal(WebSocketConnectionState.Connected, wsc.ConnectionState);
        var msgs = await new ClientServerFullConversation(wsc).RunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(WebSocketConnectionState.Disconnected, wsc.ConnectionState);

        // THEN
        Assert.Equal(15, msgs.Count);

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[0].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[0].Type);
        Assert.Equal("Hello!", msgs[0].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[1].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[1].Type);
        Assert.Equal("Hi!", msgs[1].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[2].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[2].Type);
        Assert.Equal("What time is it?", msgs[2].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[3].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[3].Type);
        Assert.StartsWith("Now it's ", msgs[3].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[4].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[4].Type);
        Assert.Equal("Thanks!", msgs[4].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[5].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[5].Type);
        Assert.Equal("You're welcome!", msgs[5].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[6].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[6].Type);
        Assert.Equal("Server, send me an image!", msgs[6].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[7].Direction);
        Assert.Equal(WebSocketMessageType.Binary, msgs[7].Type); // Large image

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[8].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[8].Type);
        Assert.Equal("Your turn! Client, send me an image!", msgs[8].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[9].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[9].Type);
        Assert.Equal("Server, send me a JSON!", msgs[9].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[10].Direction);
        Assert.Equal(WebSocketMessageType.Binary, msgs[10].Type);
        Assert.IsType<FileStream>(msgs[10].BytesStream);

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[11].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[11].Type);
        Assert.Equal("{\"name\":\"Phoebe\",\"age\":25,\"bloodType\":\"AB-\"}", msgs[11].ReadAsUtf8Text());
        var personReceived = msgs[11].ReadAsUtf8Json(AppJsonSrcGenContext.Default.Person);
        Assert.NotNull(personReceived);
        Assert.Equal("Phoebe", personReceived.Name);
        Assert.Equal(25, personReceived.Age);
        Assert.Equal(BloodType.ABNegative, personReceived.BloodType);

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[12].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[12].Type);
        Assert.Equal("Your turn! Client, send me a JSON!", msgs[12].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[13].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[13].Type);
        Assert.Equal("{\"name\":\"Joey\",\"age\":21,\"bloodType\":\"O+\"}", msgs[13].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[14].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[14].Type);
        Assert.Equal("I will close this connection in around 3s.", msgs[14].ReadAsUtf8Text());

        bool sentImgEquality = await CheckIfFilesContentsAreEqualAsync(
            ClientServerFullConversation.GetClientExampleFilePath("small_file.jpg"),
            ClientServerFullConversation.GetServerExampleFilePath("received_img.jpg"));

        Assert.True(sentImgEquality);
        Assert.Equal(9784, new FileInfo(ClientServerFullConversation.GetClientExampleFilePath("small_file.jpg")).Length);
        Assert.Equal(9784, new FileInfo(ClientServerFullConversation.GetServerExampleFilePath("received_img.jpg")).Length);

        bool receivedImgEquality = await CheckIfFilesContentsAreEqualAsync(
            ClientServerFullConversation.GetClientExampleFilePath("received_img.jpg"),
            ClientServerFullConversation.GetServerExampleFilePath("large_file.jpg"));

        Assert.True(receivedImgEquality);
        Assert.Equal(191316, new FileInfo(ClientServerFullConversation.GetClientExampleFilePath("received_img.jpg")).Length);
        Assert.Equal(191316, new FileInfo(ClientServerFullConversation.GetServerExampleFilePath("large_file.jpg")).Length);
    }

    [Theory]
    [InlineData(1.1f, "ws://localhost:5000/test/http1websocket")]
    [InlineData(2.0f, "wss://localhost:5001/test/http2websocket")]
    public static async Task Should_run_WebSocket_conversation_successfully_where_client_disconnects(float httpVersion, string url)
    {
        // GIVEN
        using var cws = MakeClientWebSocket((decimal)httpVersion);
        using var hc = MakeHttpClient(disableSslVerification: true);
        var wsc = new WebSocketClientSideConnector();
        var uri = new Uri(url);
        await wsc.ConnectAsync(cws, hc, uri, TestContext.Current.CancellationToken);

        // WHEN AND THEN
        Assert.Equal(WebSocketConnectionState.Connected, wsc.ConnectionState);
        var msgs = await new ClientDisconnectConversation(wsc).RunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(WebSocketConnectionState.Disconnected, wsc.ConnectionState);

        // THEN
        Assert.Equal(2, msgs.Count);

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[0].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[0].Type);
        Assert.Equal("Hello!", msgs[0].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[1].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[1].Type);
        Assert.Equal("Hi!", msgs[1].ReadAsUtf8Text());
    }

    [Theory]
    [InlineData(1.1f, "ws://localhost:5000/test/http1websocket")]
    [InlineData(2.0f, "wss://localhost:5001/test/http2websocket")]
    public static async Task Should_run_WebSocket_conversation_successfully_where_client_sends_closure_message(float httpVersion, string url)
    {
        // GIVEN
        using var cws = MakeClientWebSocket((decimal)httpVersion);
        using var hc = MakeHttpClient(disableSslVerification: true);
        var wsc = new WebSocketClientSideConnector();
        var uri = new Uri(url);
        await wsc.ConnectAsync(cws, hc, uri, TestContext.Current.CancellationToken);

        // WHEN AND THEN
        Assert.Equal(WebSocketConnectionState.Connected, wsc.ConnectionState);
        var msgs = await new ClientClosureMessageConversation(wsc).RunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(WebSocketConnectionState.Disconnected, wsc.ConnectionState);

        // THEN
        Assert.Equal(3, msgs.Count);

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[0].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[0].Type);
        Assert.Equal("Hello!", msgs[0].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[1].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[1].Type);
        Assert.Equal("Hi!", msgs[1].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[2].Direction);
        Assert.Equal(WebSocketMessageType.Close, msgs[2].Type);
        Assert.Equal("Adiós muchacho", msgs[2].ReadAsUtf8Text());
    }

    [Theory]
    [InlineData(1.1f, "ws://localhost:5000/test/http1websocket")]
    [InlineData(2.0f, "wss://localhost:5001/test/http2websocket")]
    public static async Task Should_run_WebSocket_conversation_successfully_where_server_sends_closure_message(float httpVersion, string url)
    {
        // GIVEN
        using var cws = MakeClientWebSocket((decimal)httpVersion);
        using var hc = MakeHttpClient(disableSslVerification: true);
        var wsc = new WebSocketClientSideConnector();
        var uri = new Uri(url);
        await wsc.ConnectAsync(cws, hc, uri, TestContext.Current.CancellationToken);

        // WHEN AND THEN
        Assert.Equal(WebSocketConnectionState.Connected, wsc.ConnectionState);
        var msgs = await new ServerClosureMessageConversation(wsc).RunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(WebSocketConnectionState.Disconnected, wsc.ConnectionState);

        // THEN
        Assert.Equal(4, msgs.Count);

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[0].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[0].Type);
        Assert.Equal("Hello!", msgs[0].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[1].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[1].Type);
        Assert.Equal("Hi!", msgs[1].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[2].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[2].Type);
        Assert.Equal("I can't leave without you!", msgs[2].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[3].Direction);
        Assert.Equal(WebSocketMessageType.Close, msgs[3].Type);
        Assert.Equal("Yes, you can. Bye!", msgs[3].ReadAsUtf8Text());
    }

    [Theory]
    [InlineData(1.1f, "ws://localhost:5000/test/http1websocket", null)]
    [InlineData(1.1f, "ws://localhost:5000/test/http1websocket", "gamma")]
    [InlineData(2.0f, "wss://localhost:5001/test/http2websocket", null)]
    [InlineData(2.0f, "wss://localhost:5001/test/http2websocket", "gamma")]
    public static async Task Should_run_WebSocket_conversation_successfully_with_or_without_subprotocol_also_check_server_disconnect(float httpVersion, string url, string? subprotocol)
    {
        // GIVEN
        using var cws = MakeClientWebSocket((decimal)httpVersion, subprotocol);
        using var hc = MakeHttpClient(disableSslVerification: true);
        var wsc = new WebSocketClientSideConnector();
        var uri = new Uri(url);
        await wsc.ConnectAsync(cws, hc, uri, TestContext.Current.CancellationToken);

        // WHEN AND THEN
        Assert.Equal(WebSocketConnectionState.Connected, wsc.ConnectionState);
        var msgs = await new SubprotocolAndServerDisconnectConversation(wsc).RunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(WebSocketConnectionState.Disconnected, wsc.ConnectionState);

        // THEN
        Assert.Equal(4, msgs.Count);

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[0].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[0].Type);
        Assert.Equal("Hello!", msgs[0].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromServer, msgs[1].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[1].Type);
        Assert.Equal("Hi!", msgs[1].ReadAsUtf8Text());

        Assert.Equal(WebSocketMessageDirection.FromClient, msgs[2].Direction);
        Assert.Equal(WebSocketMessageType.Text, msgs[2].Type);
        Assert.Equal("Which subprotocol are we on?", msgs[2].ReadAsUtf8Text());

        if (subprotocol == null)
        {
            Assert.Equal(WebSocketMessageDirection.FromServer, msgs[3].Direction);
            Assert.Equal(WebSocketMessageType.Text, msgs[3].Type);
            Assert.Equal("No subprotocol specified.", msgs[3].ReadAsUtf8Text());
        }
        else
        {
            Assert.Equal(WebSocketMessageDirection.FromServer, msgs[3].Direction);
            Assert.Equal(WebSocketMessageType.Text, msgs[3].Type);
            Assert.Equal("We are on subprotocol 'gamma'.", msgs[3].ReadAsUtf8Text());
        }
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