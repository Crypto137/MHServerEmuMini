using System.Net;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;
using MHServerEmuMini.Frontend;

namespace MHServerEmuMini.Auth
{
    public class AuthServer
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IdGenerator _sessionIdGenerator = new(IdType.Session);
        private readonly string _url;

        private CancellationTokenSource _cts;
        private HttpListener _listener;

        public AuthServer()
        {
            var config = ConfigManager.Instance.GetConfig<AuthConfig>();

            _url = $"http://{config.Address}:{config.Port}/";
        }

        public async void Run()
        {
            // Reset CTS
            _cts?.Dispose();
            _cts = new();

            // Create an http server and start listening for incoming connections
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _listener.Start();

            Logger.Info($"AuthServer is listening on {_url}...");

            while (true)
            {
                try
                {
                    // Wait for a connection, and handle the request
                    HttpListenerContext context = await _listener.GetContextAsync().WaitAsync(_cts.Token);
                    await HandleMessageAsync(context.Request, context.Response);
                    context.Response.Close();
                }
                catch (TaskCanceledException) { return; }       // Stop handling connections
                catch (Exception e)
                {
                    Logger.Error($"Run(): Unhandled exception: {e}");
                }
            }
        }

        private async Task HandleMessageAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            MessagePackage message = new(CodedInputStream.CreateInstance(request.InputStream));
            message.Protocol = typeof(FrontendProtocolMessage);
            await OnLoginDataPB(request, response, message);
        }

        private async Task SendMessageAsync(IMessage message, HttpListenerResponse response, int statusCode = 200)
        {
            byte[] buffer = new MessagePackage(message).Serialize();

            response.StatusCode = statusCode;
            response.KeepAlive = false;
            response.ContentType = "application/octet-stream";
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer);
        }

        private async Task<bool> OnLoginDataPB(HttpListenerRequest request, HttpListenerResponse response, MessagePackage message)
        {
            var loginDataPB = message.Deserialize() as LoginDataPB;
            if (loginDataPB == null) return Logger.WarnReturn(false, $"OnLoginDataPB(): Failed to retrieve message");

            Logger.Info($"Sending AuthTicket to the game client on {request.RemoteEndPoint}");

            var frontendConfig = ConfigManager.Instance.GetConfig<FrontendConfig>();

            AuthTicket ticket = AuthTicket.CreateBuilder()
                .SetSessionId(_sessionIdGenerator.Generate())
                .SetSessionToken(ByteString.Unsafe.FromBytes(CryptographyHelper.GenerateToken()))
                .SetSessionKey(ByteString.Unsafe.FromBytes(CryptographyHelper.GenerateAesKey()))
                .SetSuccess(true)
                .SetFrontendServer(frontendConfig.PublicAddress)
                .SetFrontendPort(frontendConfig.Port)
                .Build();

            await SendMessageAsync(ticket, response);
            return true;
        }
    }
}
