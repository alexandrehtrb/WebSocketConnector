using AlexandreHtrb.WebSocketExtensions;
using System.Net.WebSockets;
using TestClient.Conversations;

namespace TestClient;

[TestClass]
public class SubprotocolConversationTest : BaseConversationTest
{
    [TestMethod]
    [DataRow(1.1d, wsHttp1Url, null)]
    [DataRow(1.1d, wsHttp1Url, "gamma")]
    [DataRow(2.0d, wsHttp2Url, null)]
    [DataRow(2.0d, wsHttp2Url, "gamma")]
    public async Task Should_run_WebSocket_conversation_successfully_with_or_without_subprotocol(double httpVersion, string url, string? subprotocol)
    {
        // GIVEN
        using var cws = MakeClientWebSocket((decimal)httpVersion, subprotocol);
        using var hc = MakeHttpClient(disableSslVerification: true);
        var wsc = new WebSocketClientSideConnector(collectOnlyServerSideMessages: false);
        var uri = new Uri(url);
        await wsc.ConnectAsync(cws, hc, uri, default);

        // WHEN AND THEN
        Assert.AreEqual(WebSocketConnectionState.Connected, wsc.ConnectionState);
        var msgs = await new SubprotocolConversation(wsc).RunAsync(default);
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
}