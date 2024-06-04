using MHServerEmu.Core.VectorMath;

namespace MHServerEmuMini.Games.Regions
{
    public class Cell
    {
        public ulong PrototypeDataRef { get; }
        public Vector3 Position { get; }

        public Cell(ulong prototypeDataRef)
        {
            PrototypeDataRef = prototypeDataRef;
            Position = Vector3.Zero;
        }

        public Cell(ulong prototypeDataRef, Vector3 position)
        {
            PrototypeDataRef = prototypeDataRef;
            Position = position;
        }

        public static implicit operator Cell(ulong prototypeDataRef) => new(prototypeDataRef);
    }
}
