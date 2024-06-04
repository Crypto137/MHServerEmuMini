using System.Globalization;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Logging.Targets;
using MHServerEmu.Core.Network;
using MHServerEmuMini.Auth;
using MHServerEmuMini.Frontend;
using MHServerEmuMini.Games;

namespace MHServerEmuMini
{
    public class ServerApp
    {
#if DEBUG
        public const string BuildConfiguration = "Debug";
#elif RELEASE
        public const string BuildConfiguration = "Release";
#endif

        private static readonly Logger Logger = LogManager.CreateLogger();

        private bool _isRunning = false;
        private Thread _authServerThread;
        private Thread _frontendServerThread;
        private Thread _gameThread;

        public static readonly string VersionInfo = $"Revision 1 | Client Version {Game.Version} | {AssemblyHelper.ParseAssemblyBuildTime():yyyy.MM.dd HH:mm:ss} UTC | {BuildConfiguration}";

        public static ServerApp Instance { get; } = new();

        public DateTime StartupTime { get; private set; }

        public AuthServer AuthServer { get; private set; }
        public FrontendServer FrontendServer { get; private set; }
        public Game Game { get; private set; }

        private ServerApp() { }

        public void Run()
        {
            if (_isRunning) return;
            _isRunning = true;

            StartupTime = DateTime.Now;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Console.ForegroundColor = ConsoleColor.Yellow;
            PrintBanner();
            PrintVersionInfo();
            Console.ResetColor();

            // Init loggers before anything else
            InitLoggers();

            Logger.Info("MHServerEmuMini starting...");

            // Our encoding is not going to work unless we are running on a little-endian system
            if (BitConverter.IsLittleEndian == false)
            {
                Logger.Fatal("This computer's architecture uses big-endian byte order, which is not compatible with MHServerEmu.");
                Console.ReadLine();
                return;
            }

            ProtocolDispatchTable.Instance.Initialize();

            AuthServer = new();
            _authServerThread = new(AuthServer.Run) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            _authServerThread.Start();

            FrontendServer = new();
            _frontendServerThread = new(FrontendServer.Run) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            _frontendServerThread.Start();

            Game = new();
            _gameThread = new(Game.Run) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            _gameThread.Start();

            while (true)
            {
                string input = Console.ReadLine();
            }
        }

        /// <summary>
        /// Prints a fancy ASCII banner to console.
        /// </summary>
        private void PrintBanner()
        {
            Console.WriteLine(@"  __  __ _    _  _____                          ______                             _       _ ");
            Console.WriteLine(@" |  \/  | |  | |/ ____|                        |  ____|                           (_)     (_)");
            Console.WriteLine(@" | \  / | |__| | (___   ___ _ ____   _____ _ __| |__   _ __ ___  _   _   _ __ ___  _ _ __  _ ");
            Console.WriteLine(@" | |\/| |  __  |\___ \ / _ \ '__\ \ / / _ \ '__|  __| | '_ ` _ \| | | | | '_ ` _ \| | '_ \| |");
            Console.WriteLine(@" | |  | | |  | |____) |  __/ |   \ V /  __/ |  | |____| | | | | | |_| | | | | | | | | | | | |");
            Console.WriteLine(@" |_|  |_|_|  |_|_____/ \___|_|    \_/ \___|_|  |______|_| |_| |_|\__,_| |_| |_| |_|_|_| |_|_|");
            Console.WriteLine();
        }

        /// <summary>
        /// Prints formatted version info to console.
        /// </summary>
        private void PrintVersionInfo()
        {
            Console.WriteLine($"\t{VersionInfo}");
            Console.WriteLine();
        }

        /// <summary>
        /// Handles unhandled exceptions.
        /// </summary>
        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;

            if (e.IsTerminating)
            {
                Logger.FatalException(ex, "MHServerEmu terminating because of unhandled exception.");
                ServerManager.Instance.ShutdownServices();
            }
            else
            {
                Logger.ErrorException(ex, "Caught unhandled exception.");
            }

            Console.ReadLine();
        }

        /// <summary>
        /// Initializes log targets.
        /// </summary>
        private void InitLoggers()
        {
            var config = ConfigManager.Instance.GetConfig<LoggingConfig>();

            LogManager.Enabled = config.EnableLogging;

            // Attach console log target
            if (config.EnableConsole)
            {
                ConsoleTarget target = new(config.ConsoleIncludeTimestamps, config.ConsoleMinLevel, config.ConsoleMaxLevel);
                LogManager.AttachTarget(target);
            }

            // Attach file log target
            if (config.EnableFile)
            {
                FileTarget target = new(config.FileIncludeTimestamps, config.FileMinLevel, config.FileMaxLevel,
                    $"MHServerEmu_{StartupTime:yyyy-dd-MM_HH.mm.ss}.log", false);
                LogManager.AttachTarget(target);
            }

            if (config.SynchronousMode)
                Logger.Debug($"Synchronous logging enabled");
        }
    }
}
