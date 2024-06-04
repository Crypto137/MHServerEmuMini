using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmuMini.Games.Entities
{
    public class WorldEntity : Entity
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Orientation Orientation { get; set; } = Orientation.Zero;

        public WorldEntity(Game game) : base(game)
        {
            
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            ulong repId = 0;
            uint dummy32 = 0;
            ulong dummy64 = 0;
            Vector3 dummyVector3 = Vector3.Zero;

            // Five RepVars - Locomotor?
            archive.Transfer(ref repId);            // RepVar<UInt64>
            archive.Transfer(ref dummy64);
            //repId++;

            archive.Transfer(ref repId);            // RepVar<UInt32>
            archive.Transfer(ref dummy32);
            //repId++;

            archive.Transfer(ref repId);            // RepVar<UInt32>
            archive.Transfer(ref dummy32);
            //repId++;

            archive.Transfer(ref repId);            // RepVar<Vector3> - position?
            archive.Transfer(ref dummyVector3);
            //repId++;

            archive.Transfer(ref repId);            // RepVar - orientation?
            archive.Transfer(ref dummyVector3);
            //repId++;

            archive.Transfer(ref repId);            // RepId for locomotor?

            // ConditionCollection
            archive.Transfer(ref repId);            // RepId for the ConditionCollection itself
            //repId++;
            archive.Transfer(ref repId);            // RepId for RepMap<RepVar<ulong>, Condition>
            //repId++;
            archive.Transfer(ref dummy64);          // NumConditions

            // Bool to indicate if there is a power collection
            bool hasPowerCollection = false;
            success &= archive.Transfer(ref hasPowerCollection);

            return success;
        }
    }
}
