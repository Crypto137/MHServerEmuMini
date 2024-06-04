using System.Reflection;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmuMini.Frontend;
using MHServerEmuMini.Games.Entities;
using MHServerEmuMini.Games.GameData;
using MHServerEmuMini.Games.Network;
using MHServerEmuMini.Games.Properties;

namespace MHServerEmuMini.Games.Commands
{
    /// <summary>
    /// A singleton that handles commands.
    /// </summary>
    public class CommandManagerMini : ICommandParser<PlayerConnection>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<CommandAttribute, CommandInvoker> _commandDict = new();

        private delegate string CommandInvoker(string[] @params, PlayerConnection invoker);

        public static CommandManagerMini Instance { get; } = new();

        private CommandManagerMini()
        {
            foreach (MethodInfo methodInfo in typeof(CommandManagerMini).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                var commandAttribute = methodInfo.GetCustomAttribute<CommandAttribute>();
                if (commandAttribute == null) continue;
                _commandDict.Add(commandAttribute, methodInfo.CreateDelegate<CommandInvoker>(this));
            }
        }

        public bool TryParse(string message, PlayerConnection invoker)
        {
            if (ParseInput(message, out string command, out string[] @params) == false)
                return false;

            foreach (var kvp in _commandDict)
            {
                if (kvp.Key.Name != command) continue;

                string output = kvp.Value(@params, invoker);

                if (string.IsNullOrEmpty(output) == false)
                    invoker.SendSystemChatMessage(output);

                return true;
            }

            return false;
        }

        private bool ParseInput(string input, out string command, out string[] @params)
        {
            command = string.Empty;
            @params = Array.Empty<string>();

            input = input.Trim();
            if (input.Length < 2 || input[0] != '!') return false;

            string[] tokens = input.Split(' ');
            command = tokens[0].Substring(1).ToLower();
            
            if (tokens.Length > 1)
            {
                @params = new string[tokens.Length - 1];
                Array.Copy(tokens, 1, @params, 0, @params.Length);
            }

            return true;   
        }

        #region Command Implementations

        [Command("commands", "Prints available commands.")]
        private string PrintCommandList(string[] @params, PlayerConnection invoker)
        {
            foreach (var kvp in _commandDict)
                invoker.SendSystemChatMessage($"!{kvp.Key.Name} - {kvp.Key.Description}");

            return string.Empty;
        }

        [Command("avatar", "Switches the current avatar.")]
        private string SwitchAvatar(string[] @params, PlayerConnection invoker)
        {
            if (@params.Length == 0) return "Invalid parameters.";

            if (Enum.TryParse(@params[0], true, out AvatarPrototypeRef avatarProtoRef) == false)
                return $"Invalid avatar name '{@params[0]}'.";

            ulong entityId = invoker.Player.GetAvatarEntityId(avatarProtoRef);
            invoker.SwitchAvatar(entityId);

            return string.Empty;
        }

        [Command("tower", "Return to Avengers Tower.")]
        private string ReturnToAvengersTower(string[] @params, PlayerConnection invoker)
        {
            invoker.MoveToRegion(RegionPrototypeRef.AvengersTowerHUBRegion);
            return string.Empty;
        }

        [Command("spawn", "Spawns an entity with the provided data file path (relative from the entity folder and without the file extension).")]
        private string SpawnEntity(string[] @params, PlayerConnection invoker)
        {
            if (@params.Length == 0)
                return "Invalid parameters.";

            string path = $"Entity/{@params[0]}.prototype";

            WorldEntity worldEntity = new(invoker.Game);
            worldEntity.Id = invoker.Game.NextEntityId;
            worldEntity.PrototypeDataRef = HashHelper.HashPath(path.ToCalligraphyPath());
            worldEntity.Properties[PropertyEnum.Health] = 100f;
            worldEntity.Properties[PropertyEnum.HealthMaxOther] = 100f;

            using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)AOINetworkPolicyValues.AOIChannelProximity))
            {
                worldEntity.Serialize(archive);
                byte padding = 0;
                for (int i = 0; i < 256; i++)
                    archive.Transfer(ref padding);

                invoker.SendMessage(NetMessageEntityCreate.CreateBuilder()
                    .SetIdEntity(worldEntity.Id)
                    .SetPrototypeId(worldEntity.PrototypeDataRef)
                    .SetDbId(0)
                    .SetInterestPolicies((uint)AOINetworkPolicyValues.AOIChannelProximity)
                    .SetPosition(invoker.Player.CurrentAvatar.Position.ToNetStructPoint3())
                    .SetOrientation(invoker.Player.CurrentAvatar.Orientation.ToNetStructPoint3())
                    .SetArchiveData(archive.ToByteString())
                    .Build());
            }

            return string.Empty;
        }

        [Command("position", "Prints current position and orientation.")]
        private string PrintPosition(string[] @params, PlayerConnection invoker)
        {
            return $"position={invoker.Player.CurrentAvatar.Position}, orientation={invoker.Player.CurrentAvatar.Orientation}";
        }

        [Command("tp", "Teleports to the specified coordinates (tp x:+1000 (relative to current position, tp x100 y500 z10 (absolute position)).")]
        private string Teleport(string[] @params, PlayerConnection invoker)
        {
            if (@params.Length == 0) return "Invalid arguments.";

            Avatar avatar = invoker.Player.CurrentAvatar;

            float x = 0f, y = 0f, z = 0f;
            foreach (string param in @params)
            {
                switch (param[0])
                {
                    case 'x':
                        if (float.TryParse(param.AsSpan(1), out x) == false) x = 0f;
                        break;

                    case 'y':
                        if (float.TryParse(param.AsSpan(1), out y) == false) y = 0f;
                        break;

                    case 'z':
                        if (float.TryParse(param.AsSpan(1), out z) == false) z = 0f;
                        break;

                    default:
                        return $"Invalid parameter: {param}";
                }
            }

            Vector3 teleportPoint = new(x, y, z);

            if (@params.Length < 3)
                teleportPoint += avatar.Position;

            avatar.Position = teleportPoint;
            invoker.SendMessage(NetMessageEntityPosition.CreateBuilder()
                .SetIdEntity(avatar.Id)
                .SetFlags(1u << 6)
                .SetPosition(teleportPoint.ToNetStructPoint3())
                .Build());

            return $"Teleporting to {teleportPoint.ToStringNames()}.";
            }

        [Command("startraftride", "Starts the funicular scripted sequence in the Raft.")]
        private string StartRaftFunicularRide(string[] @params, PlayerConnection invoker)
        {
            if (invoker.Region.PrototypeDataRef != (ulong)RegionPrototypeRef.RaftRegion)
                return "You must be in the Raft to invoke this command.";

            invoker.PlayKismetSeq(9329157849119332306);

            return string.Empty;
        }

        #endregion

        [AttributeUsage(AttributeTargets.Method)]
        private class CommandAttribute : Attribute
        {
            public string Name { get; }
            public string Description { get; }

            public CommandAttribute(string name, string description)
            {
                Name = name;
                Description = description;
            }
        }
    }
}
