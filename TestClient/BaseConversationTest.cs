using System.Net;
using System.Net.WebSockets;

namespace TestClient;

// TODO: Make tests for compression.
// Currently (.NET 10) we can't verify whether 
// messages arrive compressed or decompressed.

public abstract class BaseConversationTest
{
    protected const string wsHttp1Url = "ws://localhost:5000/test/http1websocket";
    protected const string wsHttp2Url = "wss://localhost:5001/test/http2websocket";

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

    protected static ClientWebSocket MakeClientWebSocket(decimal httpVersion, string? subprotocol = null)
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

    protected static HttpClient MakeHttpClient(bool disableSslVerification)
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