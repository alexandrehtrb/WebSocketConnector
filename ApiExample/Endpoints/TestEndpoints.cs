using System.Net;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace WebSocketApiExample.Endpoints;

public static class TestEndpoints
{
    public static WebApplication MapTestEndpoints(this WebApplication app)
    {
        app.MapGet("test/http1websocket", (Delegate)TestHttp1WebSocket);
        app.MapConnect("test/http2websocket", (Delegate)TestHttp2WebSocket);
        return app;
    }

    #region WEBSOCKETS

    private static async Task TestHttp1WebSocket(HttpContext httpCtx)
    {
        if (!httpCtx.WebSockets.IsWebSocketRequest)
        {
            byte[] txtBytes = Encoding.UTF8.GetBytes("Only WebSockets requests are accepted here!");
            httpCtx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await httpCtx.Response.BodyWriter.WriteAsync(txtBytes);
        }
        else
        {
            using var webSocket = await httpCtx.WebSockets.AcceptWebSocketAsync();
            TaskCompletionSource<object> socketFinishedTcs = new();
            string? subprotocol = webSocket.SubProtocol ?? httpCtx.WebSockets.WebSocketRequestedProtocols.FirstOrDefault();

            await BackgroundWebSocketsProcessor.RegisterAndProcessAsync(webSocket, subprotocol, socketFinishedTcs);
            await socketFinishedTcs.Task;
        }
    }

    private static async Task TestHttp2WebSocket(HttpContext httpCtx)
    {
        if (httpCtx.Request.Protocol != "HTTP/2" || !httpCtx.WebSockets.IsWebSocketRequest)
        {
            byte[] txtBytes = Encoding.UTF8.GetBytes("Only HTTP/2 websocket requests are accepted here!");
            httpCtx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await httpCtx.Response.BodyWriter.WriteAsync(txtBytes);
        }
        else
        {
            using var webSocket = await httpCtx.WebSockets.AcceptWebSocketAsync();
            TaskCompletionSource<object> socketFinishedTcs = new();
            string? subprotocol = webSocket.SubProtocol ?? httpCtx.WebSockets.WebSocketRequestedProtocols.FirstOrDefault();

            await BackgroundWebSocketsProcessor.RegisterAndProcessAsync(webSocket, subprotocol, socketFinishedTcs);
            await socketFinishedTcs.Task;
        }
    }

    #endregion
}