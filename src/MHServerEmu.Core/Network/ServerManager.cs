﻿using System.Globalization;
using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Core.Network
{
    public enum ServerType
    {
        FrontendServer,
        AuthServer,
        PlayerManager,
        GroupingManager,
        GameInstanceServer,
        Billing,
        Leaderboard,
        NumServerTypes
    }

    /// <summary>
    /// Manages <see cref="IGameService"/> instances and routes <see cref="MessagePackage"/> instances between them.
    /// </summary>
    public class ServerManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IGameService[] _services = new IGameService[(int)ServerType.NumServerTypes];
        private readonly Thread[] _serviceThreads = new Thread[(int)ServerType.NumServerTypes];

        public static ServerManager Instance { get; } = new();

        public DateTime StartupTime { get; private set; }

        private ServerManager() { }

        /// <summary>
        /// Initializes the <see cref="ServerManager"/> instance.
        /// </summary>
        public void Initialize()
        {
            StartupTime = DateTime.Now;
        }

        /// <summary>
        /// Registers an <see cref="IGameService"/> for the specified <see cref="ServerType"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RegisterGameService(IGameService gameService, ServerType serverType)
        {
            int index = (int)serverType;

            if (index < 0 || index >= _services.Length)
                return Logger.WarnReturn(false, $"RegisterGameService(): Invalid server type {serverType}");

            if (_services[index] != null)
                return Logger.WarnReturn(false, $"RegisterGameService(): Service type {serverType} is already registered");

            if (gameService == null)
                return Logger.WarnReturn(false, $"RegisterGameService(): gameService == null");

            _services[index] = gameService;
            Logger.Info($"Registered game service for server type {serverType}");
            return true;
        }

        /// <summary>
        /// Unregisters the current <see cref="IGameService"/> for the specified <see cref="ServerType"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool UnregisterGameService(ServerType serverType)
        {
            int index = (int)serverType;

            if (index < 0 || index >= _services.Length)
                return Logger.WarnReturn(false, $"UnregisterGameService(): Invalid server type {serverType}");

            if (_services[index] == null)
                return Logger.WarnReturn(false, $"UnregisterGameService(): No registered service for server type {serverType}");

            _services[index] = null;
            Logger.Info($"Unregistered server type {serverType}");
            return true;
        }

        /// <summary>
        /// Returns the registered <see cref="IGameService"/> for the specified <see cref="ServerType"/>. Returns <see langword="null"/> if not registered.
        /// </summary>
        public IGameService GetGameService(ServerType serverType)
        {
            int index = (int)serverType;

            if (index < 0 || index >= _services.Length)
                return Logger.WarnReturn<IGameService>(null, $"GetGameService(): Invalid server type {serverType}");

            return _services[index];
        }

        /// <summary>
        /// Routes the provided <see cref="MessagePackage"/> instance to the <see cref="IGameService"/> registered as the specified <see cref="ServerType"/>.
        /// </summary>
        public bool RouteMessage(ITcpClient client, MessagePackage message, ServerType serverType)
        {
            int index = (int)serverType;

            if (index < 0 || index >= _services.Length)
                return Logger.WarnReturn(false, $"RouteMessage(): Invalid server type {serverType}");

            if (_services[index] == null)
                return Logger.WarnReturn(false, $"RouteMessage(): No service is registered for server type {serverType}");

            _services[index].Handle(client, message);
            return true;
        }

        /// <summary>
        /// Routes the provided <see cref="IEnumerable{T}"/> of <see cref="MessagePackage"/> instances to the <see cref="IGameService"/> registered as the specified <see cref="ServerType"/>.
        /// </summary>
        public bool RouteMessages(ITcpClient client, IEnumerable<MessagePackage> messages, ServerType serverType)
        {
            int index = (int)serverType;

            if (index < 0 || index >= _services.Length)
                return Logger.WarnReturn(false, $"RouteMessages(): Invalid server type {serverType}");

            if (_services[index] == null)
                return Logger.WarnReturn(false, $"RouteMessages(): No service is registered for server type {serverType}");

            _services[index].Handle(client, messages);
            return true;
        }

        /// <summary>
        /// Routes the provided <see cref="MailboxMessage"/> instance to the <see cref="IGameService"/> registered as the specified <see cref="ServerType"/>.
        /// </summary>
        public bool RouteMessage(ITcpClient client, MailboxMessage message, ServerType serverType)
        {
            int index = (int)serverType;

            if (index < 0 || index >= _services.Length)
                return Logger.WarnReturn(false, $"RouteMessage(): Invalid server type {serverType}");

            if (_services[index] == null)
                return Logger.WarnReturn(false, $"RouteMessage(): No service is registered for server type {serverType}");

            _services[index].Handle(client, message);
            return true;
        }

        /// <summary>
        /// Runs all registered <see cref="IGameService"/> instances.
        /// </summary>
        public void RunServices()
        {
            for (int i = 0; i < _services.Length; i++)
            {
                if (_services[i] == null) continue;

                if (_serviceThreads[i] != null)
                    Logger.Warn($"RunServices(): {(ServerType)i} service is already running");

                _serviceThreads[i] = new(_services[i].Run) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
                _serviceThreads[i].Start();
            }
        }

        /// <summary>
        /// Shuts down all registered <see cref="IGameService"/> instances.
        /// </summary>
        public void ShutdownServices()
        {
            for (int i = 0; i < _services.Length; i++)
            {
                if (_services[i] == null) continue;
                Logger.Info($"Shutting down {(ServerType)i}...");
                _services[i].Shutdown();
                _serviceThreads[i] = null;
            }
            Logger.Info("Shutdown finished");
        }

        /// <summary>
        /// Returns a <see cref="string"/> representing the current status of all running <see cref="IGameService"/> instances.
        /// </summary>
        public string GetServerStatus()
        {
            StringBuilder sb = new();

            sb.AppendLine($"Uptime: {DateTime.Now - StartupTime:hh\\:mm\\:ss}");

            for (int i = 0; i < _services.Length; i++)
            {
                if (_services[i] == null) continue;
                sb.Append($"[{(ServerType)i}] ");

                if (_serviceThreads[i] != null)
                    sb.AppendLine($"{_services[i].GetStatus()}");
                else
                    sb.AppendLine("Not running");
            }

            return sb.ToString();
        }
    }
}
