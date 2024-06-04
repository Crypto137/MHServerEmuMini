using MHServerEmu.Core.Serialization;

namespace MHServerEmuMini.Games.Network
{
    public class ReplicatedString : ISerialize
    {
        public ulong ReplicationId { get; }
        public string Value { get; set; }

        public ReplicatedString(ulong replicationId, string value = "")
        {
            ReplicationId = replicationId;
            Value = value;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            ulong replicationId = ReplicationId;
            string value = Value;
            success &= archive.Transfer(ref replicationId);
            success &= archive.Transfer(ref value);

            return success;
        }
    }
}
