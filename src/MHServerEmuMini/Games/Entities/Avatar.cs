using MHServerEmu.Core.Serialization;
using MHServerEmuMini.Games.Network;
using MHServerEmuMini.Games.Properties;

namespace MHServerEmuMini.Games.Entities
{
    public class Avatar : WorldEntity
    {
        private Player _owner;

        public ReplicatedString PlayerName { get; }
        public AbilityKeyMapping[] AbilityKeyMappings { get; } = [new()];

        public Avatar(Game game) : base(game)
        {
            PlayerName = new(game.NextReplicationId);
        }

        public override void Initialize()
        {
            // Default stats for a level 1 avatar
            Properties[PropertyEnum.CharacterLevel] = 1;
            Properties[PropertyEnum.Health] = 400f;
            Properties[PropertyEnum.HealthMaxOther] = 400f;
            Properties[PropertyEnum.Endurance] = 100f;

            // Abilities
            //AbilityKeyMappings[0].SetAbilityInAbilitySlot(11537848138906866161, AbilitySlot.PrimaryAction);
            //AbilityKeyMappings[0].SetAbilityInAbilitySlot(8948152837392568972, AbilitySlot.SecondaryAction);
        }

        public void SetOwner(Player owner)
        {
            _owner = owner;
            PlayerName.Value = owner.PlayerName.Value;
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            success &= PlayerName.Serialize(archive);

            // PlayerDbId?
            ulong dummy = 0;
            success &= archive.Transfer(ref dummy);
            ulong playerDbId = 0x2000000000000001;
            success &= archive.Transfer(ref playerDbId);

            // Empty string
            string emptyString = string.Empty;
            success &= archive.Transfer(ref emptyString);

            // GuildInfo - false
            bool hasGuildInfo = false;
            success &= archive.Transfer(ref hasGuildInfo);

            // Ability key mappings
            ulong numAbilityKeyMappings = (ulong)AbilityKeyMappings.Length;
            success &= archive.Transfer(ref numAbilityKeyMappings);

            foreach (AbilityKeyMapping mapping in AbilityKeyMappings)
                success &= mapping.Serialize(archive);

            return success;
        }
    }
}
