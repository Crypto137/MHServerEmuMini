using System.Collections;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmuMini.Frontend;

namespace MHServerEmuMini.Games.Network
{
    public class PlayerConnectionManager : IEnumerable<PlayerConnection>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<FrontendClient, PlayerConnection> _clientConnectionDict = new();
        private readonly Game _game;

        // Incoming messages are asynchronously posted to a mailbox where they are deserialized and stored for later retrieval.
        // When it's time to process messages, we copy all messages stored in our mailbox to a list.
        // Although we call it a "list" to match the client, it functions more like a queue (FIFO, pop/peeks).
        private readonly CoreNetworkMailbox<FrontendClient> _mailbox = new();
        private readonly MessageList<FrontendClient> _messagesToProcessList = new();

        // We swap queues with a lock when handling async client connect / disconnect events
        private Queue<FrontendClient> _asyncAddClientQueue = new();
        private Queue<FrontendClient> _asyncRemoveClientQueue = new();
        private Queue<FrontendClient> _addClientQueue = new();
        private Queue<FrontendClient> _removeClientQueue = new();

        // Queue for pending player connections (i.e. players currently loading)
        private Queue<PlayerConnection> _pendingPlayerConnectionQueue = new();

        /// <summary>
        /// Constructs a new <see cref="PlayerConnectionManager"/> instance for the provided <see cref="Game"/>.
        /// </summary>
        public PlayerConnectionManager(Game game)
        {
            _game = game;
        }

        /// <summary>
        /// Returns the <see cref="PlayerConnection"/> bound to the provided <see cref="FrontendClient"/>.
        /// </summary>
        public PlayerConnection GetPlayerConnection(FrontendClient frontendClient)
        {
            if (_clientConnectionDict.TryGetValue(frontendClient, out PlayerConnection connection) == false)
                Logger.Warn($"GetPlayerConnection(): Client is not bound to a player connection");

            return connection;
        }

        public void Update()
        {
            // NOTE: It is important to remove disconnected client BEFORE registering new clients
            // to make sure we save data for cases such as duplicate logins.
            // markAsyncDisconnectedClients() -> we just do everything in RemoveDisconnectedClients()
            RemoveDisconnectedClients();
            ProcessAsyncAddedClients();
        }

        /// <summary>
        /// Loads pending players.
        /// </summary>
        public void ProcessPendingPlayerConnections()
        {
            while (_pendingPlayerConnectionQueue.Count > 0)
            {
                PlayerConnection playerConnection = _pendingPlayerConnectionQueue.Dequeue();
                playerConnection.EnterGame();
            }
        }

        /// <summary>
        /// Requests a player to be reloaded.
        /// </summary>
        public void SetPlayerConnectionPending(PlayerConnection playerConnection)
        {
            _pendingPlayerConnectionQueue.Enqueue(playerConnection);
        }

        /// <summary>
        /// Enqueues registration of a new <see cref="PlayerConnection"/> for the provided <see cref="FrontendClient"/> during the next update.
        /// </summary>
        public void AsyncAddClient(FrontendClient client)
        {
            lock (_asyncAddClientQueue)
                _asyncAddClientQueue.Enqueue(client);
        }

        /// <summary>
        /// Enqueues removal of the <see cref="PlayerConnection"/> for the provided <see cref="FrontendClient"/> during the next update.
        /// </summary>
        public void AsyncRemoveClient(FrontendClient client)
        {
            lock (_asyncRemoveClientQueue)
                _asyncRemoveClientQueue.Enqueue(client);
        }

        /// <summary>
        /// Handles an incoming <see cref="MessagePackage"/> asynchronously.
        /// </summary>
        public void AsyncPostMessage(FrontendClient client, MessagePackage message)
        {
            // If the message fails to deserialize it means either data got corrupted somehow or we have a hacker trying to mess things up.
            // In both cases it's better to bail out.
            if (_mailbox.Post(client, message) == false)
            {
                Logger.Error($"AsyncPostMessage(): Deserialization failed, disconnecting client from {_game}");
                client.Disconnect();
            }
        }

        /// <summary>
        /// Processes all asynchronously posted messages.
        /// </summary>
        public void ReceiveAllPendingMessages()
        {
            // We reuse the same message list every time to avoid unnecessary allocations.
            _mailbox.GetAllMessages(_messagesToProcessList);

            while (_messagesToProcessList.HasMessages)
            {
                (FrontendClient client, MailboxMessage message) = _messagesToProcessList.PopNextMessage();
                PlayerConnection playerConnection = GetPlayerConnection(client);

                if (playerConnection != null && playerConnection.CanSendOrReceiveMessages())
                    playerConnection.ReceiveMessage(message);

                // If the player connection was removed or it is currently unable to receive messages,
                // this message will be lost, like tears in rain...
            }
        }

        /// <summary>
        /// Sends the provided <see cref="IMessage"/> instance over the specified <see cref="PlayerConnection"/>.
        /// </summary>
        public void SendMessage(PlayerConnection connection, IMessage message)
        {
            connection.PostMessage(message);
        }

        /// <summary>
        /// Broadcasts an <see cref="IMessage"/> instance to all active <see cref="PlayerConnection"/> instances.
        /// </summary>
        public void BroadcastMessage(IMessage message)
        {
            foreach (PlayerConnection connection in _clientConnectionDict.Values)
                connection.PostMessage(message);
        }

        /// <summary>
        /// Posts the provided <see cref="IMessage"/> to the specified <see cref="PlayerConnection"/> and immediately flushes it.
        /// </summary>
        public void SendMessageImmediate(PlayerConnection connection, IMessage message)
        {
            connection.PostMessage(message);
            connection.FlushMessages();
        }

        /// <summary>
        /// Flushes all active <see cref="PlayerConnection"/> instances.
        /// </summary>
        public void SendAllPendingMessages()
        {
            foreach (PlayerConnection connection in this)
                connection.FlushMessages();
        }

        #region IEnumerable Implementation

        public IEnumerator<PlayerConnection> GetEnumerator() => _clientConnectionDict.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        private void ProcessAsyncAddedClients()
        {
            // Swap queues so that we can continue queueing clients while we process
            lock (_asyncAddClientQueue)
                (_asyncAddClientQueue, _addClientQueue) = (_addClientQueue, _asyncAddClientQueue);

            while (_addClientQueue.Count > 0)
            {
                FrontendClient client = _addClientQueue.Dequeue();
                AcceptAndRegisterNewClient(client);
            }
        }

        private void RemoveDisconnectedClients()
        {
            // Swap queues so that we can continue queueing clients while we process
            lock (_asyncRemoveClientQueue)
                (_asyncRemoveClientQueue, _removeClientQueue) = (_removeClientQueue, _asyncRemoveClientQueue);

            while (_removeClientQueue.Count > 0)
            {
                FrontendClient client = _removeClientQueue.Dequeue();

                if (_clientConnectionDict.Remove(client, out PlayerConnection playerConnection) == false)
                {
                    Logger.Warn($"RemoveDisconnectedClients(): Client not found");
                    continue;
                }

                // Update db models and clean up
                playerConnection.OnDisconnect();

                Logger.Info($"Removed client from game {_game}");
            }
        }

        private void AcceptAndRegisterNewClient(FrontendClient client)
        {
            PlayerConnection connection = new(_game, client);

            if (_clientConnectionDict.TryAdd(client, connection) == false)
                Logger.Warn($"AcceptAndRegisterNewClient(): Failed to add client");

            SetPlayerConnectionPending(connection);

            Logger.Info($"Accepted and registered client to game {_game}");
        }
    }
}
