using MHServerEmu.Core.VectorMath;

namespace MHServerEmuMini.Games.Regions
{
    public class RegionSettings
    {
        public ulong RegionProtoRef { get; set; } = 0;
        public Vector3 Min { get; set; } = new(-10000f);
        public Vector3 Max { get; set; } = new(10000f);
        public Vector3 EntrancePosition { get; set; } = Vector3.Zero;
        public Orientation EntranceOrientation { get; set; } = Orientation.Zero;

        public SortedDictionary<uint, Area> AreaDict { get; set; } = new();
    }
}
