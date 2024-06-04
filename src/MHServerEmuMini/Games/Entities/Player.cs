using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmuMini.Games.GameData;
using MHServerEmuMini.Games.Network;
using MHServerEmuMini.Games.Properties;

namespace MHServerEmuMini.Games.Entities
{
    public class Player : Entity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public ReplicatedPropertyCollection AvatarPropertyCollection { get; }
        public ReplicatedString PlayerName { get; }

        public List<Avatar> AvatarLibrary { get; } = new();
        public Avatar CurrentAvatar { get; private set; }

        public Player(Game game) : base(game)
        {
            PrototypeDataRef = 18307315963852687724;    // Player.defaults

            AvatarPropertyCollection = new(Game.NextReplicationId);
            PlayerName = new(Game.NextReplicationId);
        }

        public override void Initialize()
        {
            base.Initialize();

            for (ulong i = 1; i < 123; i++)     // 123 - Number of waypoint enums
                Properties[new(PropertyEnum.Waypoint, i, 7)] = true;
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            // MissionManager
            ulong numMissions = 0;
            success &= archive.Transfer(ref numMissions);

            // AvatarPropertyCollection
            success &= AvatarPropertyCollection.Serialize(archive);

            ulong shardId = 3;
            success &= archive.Transfer(ref shardId);

            success &= PlayerName.Serialize(archive);

            byte dummy = 0;
            for (int i = 0; i < 256; i++)
                success &= archive.Transfer(ref dummy);

            return true;
        }

        public bool CreateAvatar(AvatarPrototypeRef avatarProtoRef)
        {
            if (CurrentAvatar?.PrototypeDataRef == (ulong)avatarProtoRef || AvatarLibrary.Find(avatar => avatar.PrototypeDataRef == (ulong)avatarProtoRef) != null)
                return Logger.WarnReturn(false, $"CreateAvatar(): avatarProtoRef {avatarProtoRef} already exists for player {Id}");

            Avatar avatar = new(Game);
            avatar.Id = Game.NextEntityId;
            avatar.PrototypeDataRef = (ulong)avatarProtoRef;
            avatar.SetOwner(this);
            avatar.Initialize();
            AvatarLibrary.Add(avatar);

            return true;
        }

        public bool SwitchAvatar(ulong entityId, out Avatar prevAvatar, out int prevAvatarSlot)
        {
            prevAvatarSlot = -1;
            prevAvatar = null;

            Avatar avatar = AvatarLibrary.Find(avatar => avatar.Id == entityId);
            if (avatar == null) return Logger.WarnReturn(false, $"SetCurrentAvatar(): avatar entityId {entityId} not found in the avatar library");

            if (CurrentAvatar == null)
            {
                // Remove from the library if no current avatar
                AvatarLibrary.Remove(avatar);
                CurrentAvatar = avatar;
            }
            else
            {
                // Swap if there is a current avatar
                prevAvatar = CurrentAvatar;
                prevAvatarSlot = AvatarLibrary.IndexOf(avatar);
                (AvatarLibrary[prevAvatarSlot], CurrentAvatar) = (CurrentAvatar, AvatarLibrary[prevAvatarSlot]);

                avatar.Position = prevAvatar.Position;
                avatar.Orientation = prevAvatar.Orientation;
            }

            return true;
        }

        public ulong GetAvatarEntityId(AvatarPrototypeRef avatarProtoRef)
        {
            Avatar avatar = AvatarLibrary.Find(avatar => avatar.PrototypeDataRef == (ulong)avatarProtoRef);
            if (avatar == null) return Logger.WarnReturn(0ul, $"GetAvatarEntityId(): avatarProtoRef {avatarProtoRef} not found in the avatar library");

            return avatar.Id;
        }
    }
}
