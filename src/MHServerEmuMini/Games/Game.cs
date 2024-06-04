using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;
using MHServerEmuMini.Frontend;
using MHServerEmuMini.Games.Network;
using MHServerEmuMini.Games.Regions;

namespace MHServerEmuMini.Games
{
    public class Game
    {
        public const string Version = "1.10.0.643";

        private static readonly Logger Logger = LogManager.CreateLogger();

        private ulong _nextEntityId = 1;
        private ulong _nextReplicationId = 1000;

        public ulong Id { get; } = 1;

        public PlayerConnectionManager NetworkManager { get; }
        public RegionManager RegionManager { get; }

        public ulong NextEntityId { get => _nextEntityId++; }
        public ulong NextReplicationId { get => _nextReplicationId++; }

        public Game()
        {
            NetworkManager = new(this);
            RegionManager = new();
        }

        public void Run()
        {
            while (true)
            {
                NetworkManager.Update();
                NetworkManager.ReceiveAllPendingMessages();
                NetworkManager.ProcessPendingPlayerConnections();
                NetworkManager.SendAllPendingMessages();
            }
        }

        public void AddClient(FrontendClient client)
        {
            NetworkManager.AsyncAddClient(client);
        }

        public void RemoveClient(FrontendClient client)
        {
            NetworkManager.AsyncRemoveClient(client);
        }

        public void SendMessage(PlayerConnection connection, IMessage message)
        {
            NetworkManager.SendMessage(connection, message);
        }

        public bool ReceiveMessage(FrontendClient client, MessagePackage message)
        {
            // manual handling for this
            if (message.Id == (uint)ClientToGameServerMessage.NetMessageReadyForGameJoin)
            {
                NetMessageReadyForGameJoin readyForGameJoin = NetMessageReadyForGameJoin.CreateBuilder().MergeFrom(message.Payload).BuildPartial();
                Logger.Info($"Received NetMessageReadyForGameJoin");
                Logger.Trace(readyForGameJoin.ToString());

                client.SendMessage(1, NetMessageReadyAndLoggedIn.DefaultInstance);
                client.SendMessage(1, NetMessageReadyForTimeSync.DefaultInstance);

                return true;
            }
            else if (message.Id == (uint)ClientToGameServerMessage.NetMessageSyncTimeRequest)
            {
                // Handle sync time request here because occasionally the first time sync arrives before
                // we are able to create a PlayerConnection for the client due to how simplified everything is.
                message.Protocol = typeof(ClientToGameServerMessage);
                var request = message.Deserialize() as NetMessageSyncTimeRequest;
                if (request == null) return Logger.WarnReturn(false, $"ReceiveMessage(): Failed to retrieve NetMessageSyncTimeRequest");

                //Logger.Debug($"NetMessageSyncTimeRequest:\n{request}");

                var reply = NetMessageSyncTimeReply.CreateBuilder()
                    .SetGameTimeClientSent(request.GameTimeClientSent)
                    .SetGameTimeServerReceived(Clock.GameTime.Ticks / 10)
                    .SetGameTimeServerSent(Clock.GameTime.Ticks / 10)
                    .SetDateTimeClientSent(request.DateTimeClientSent)
                    .SetDateTimeServerReceived(Clock.GameTime.Ticks / 10)
                    .SetDateTimeServerSent(Clock.UnixTime.Ticks / 10)
                    .SetDialation(1.0f)
                    .SetGametimeDialationStarted(0)
                    .SetDatetimeDialationStarted(0)
                    .Build();

                //Logger.Debug($"NetMessageSyncTimeReply:\n{reply}");

                client.SendMessage(1, reply);
                return true;
            }

            message.Protocol = typeof(ClientToGameServerMessage);
            NetworkManager.AsyncPostMessage(client, message);
            return true;
        }

        public void ReceiveMessages(FrontendClient client, IEnumerable<MessagePackage> messages)
        {
            foreach (MessagePackage message in messages)
                ReceiveMessage(client, message);
        }

        public override string ToString()
        {
            return $"id=0x{Id:X}";
        }
    }
}
