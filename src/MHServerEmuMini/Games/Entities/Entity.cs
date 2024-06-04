using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmuMini.Games.Network;
using MHServerEmuMini.Games.Properties;

namespace MHServerEmuMini.Games.Entities
{
    public class Entity : ISerialize
    {
        public Game Game { get; }

        public ulong Id { get; set; }
        public ulong PrototypeDataRef { get; set; }

        public ReplicatedPropertyCollection Properties { get; }

        public Entity(Game game)
        {
            Game = game;
            Properties = new(Game.NextReplicationId);
        }

        public virtual void Initialize()
        {

        }

        public virtual bool Serialize(Archive archive)
        {
            return Properties.Serialize(archive);
        }

        public ByteString ToByteString(AOINetworkPolicyValues replicationPolicy = AOINetworkPolicyValues.AOIChannelOwner)
        {
            using (Archive archive = new(ArchiveSerializeType.Replication, (uint)replicationPolicy))
            {
                Serialize(archive);
                return archive.ToByteString();
            }
        }
    }
}
