using MHServerEmu.Core.Serialization;

namespace MHServerEmuMini.Games.Properties
{
    public class ReplicatedPropertyCollection : PropertyCollection
    {
        public ulong ReplicationId { get; }

        public ReplicatedPropertyCollection(ulong replicationId)
        {
            ReplicationId = replicationId;
        }

        public override bool Serialize(Archive archive)
        {
            bool success = true;

            ulong replicationId = ReplicationId;
            success &= archive.Transfer(ref replicationId);

            success &= base.Serialize(archive);
            return success;
        }
    }
}
