using AlexandreHtrb.WebSocketExtensions;
using System.Net.WebSockets;
using TestClient.Conversations;

namespace TestClient;

[TestClass]
public class ServerExceptionConversationTest : BaseConversationTest
{
    [TestMethod]
    [DataRow(1.1d, wsHttp1Url)]
    [DataRow(2.0d, wsHttp2Url)]
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
}