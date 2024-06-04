using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmuMini.Frontend
{
    public class FrontendServer : TcpServer
    {
        private new static readonly Logger Logger = LogManager.CreateLogger();  // Hide the Server.Logger so that this logger can show the actual server as log source.

        public override void Run()
        {
            var config = ConfigManager.Instance.GetConfig<FrontendConfig>();

            if (Start(config.BindIP, int.Parse(config.Port)) == false) return;
            Logger.Info($"FrontendServer is listening on {config.BindIP}:{config.Port}...");
        }

        // Shutdown implemented by TcpServer

        public void Handle(ITcpClient tcpClient, MessagePackage message)
        {
            var client = (FrontendClient)tcpClient;
            message.Protocol = typeof(FrontendProtocolMessage);

            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.ClientCredentials: OnClientCredentials(client, message); break;
                case FrontendProtocolMessage.InitialClientHandshake: OnInitialClientHandshake(client, message); break;

                default: Logger.Warn($"Handle(): Unhandled {(FrontendProtocolMessage)message.Id} [{message.Id}]"); break;
            }
        }

        public void Handle(ITcpClient tcpClient, IEnumerable<MessagePackage> messages)
        {
            foreach (MessagePackage message in messages)
                Handle(tcpClient, message);
        }

        protected override void OnClientConnected(TcpClientConnection connection)
        {
            Logger.Info($"Client connected from {connection}");
            connection.Client = new FrontendClient(connection);
        }

        protected override void OnClientDisconnected(TcpClientConnection connection)
        {
            var client = (FrontendClient)connection.Client;

            ServerApp.Instance.Game.RemoveClient(client);
            Logger.Info($"Client disconnected");
        }

        protected override void OnDataReceived(TcpClientConnection connection, byte[] data)
        {
            ((FrontendClient)connection.Client).Parse(data);
        }

        /// <summary>
        /// Handles <see cref="ClientCredentials"/>.
        /// </summary>
        private bool OnClientCredentials(FrontendClient client, MessagePackage message)
        {
            Logger.Info("Responding with SessionEncryptionChanged message");
            client.SendMessage(1, SessionEncryptionChanged.CreateBuilder()
                .SetRandomNumberIndex(0)
                .SetEncryptedRandomNumber(ByteString.Empty)
                .Build());

            return true;
        }

        /// <summary>
        /// Handles <see cref="InitialClientHandshake"/>.
        /// </summary>
        private bool OnInitialClientHandshake(FrontendClient client, MessagePackage message)
        {
            var handshake = message.Deserialize() as InitialClientHandshake;
            if (handshake == null) return Logger.WarnReturn(false, $"OnInitialClientHandshake(): Failed to retrieve message");

            Logger.Info($"Received initial client handshake for {handshake.ServerType}");

            if (handshake.ServerType == PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND && client.FinishedPlayerManagerHandshake == false)
                client.FinishedPlayerManagerHandshake = true;
            else if (handshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND && client.FinishedGroupingManagerHandshake == false)
                client.FinishedGroupingManagerHandshake = true;

            if (client.FinishedPlayerManagerHandshake && client.FinishedGroupingManagerHandshake)
                ServerApp.Instance.Game.AddClient(client);

            return true;
        }
    }
}
