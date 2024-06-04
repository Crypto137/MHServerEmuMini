using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmuMini.Games.Regions
{
    public class Region
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly SortedDictionary<uint, Area> _areaDict = new();

        public ulong PrototypeDataRef { get; }
        public Vector3 Min { get; } = new(-10000);
        public Vector3 Max { get; } = new(10000);
        public Vector3 EntrancePosition { get; } = Vector3.Zero;
        public Orientation EntranceOrientation { get; } = Orientation.Zero;

        public IEnumerable<KeyValuePair<uint, Area>> Areas { get => _areaDict; }

        public Region(RegionSettings settings)
        {
            PrototypeDataRef = settings.RegionProtoRef;
            Min = settings.Min;
            Max = settings.Max;
            EntrancePosition = settings.EntrancePosition;
            EntranceOrientation = settings.EntranceOrientation;
        }

        public Area AddArea(uint id, ulong areaProtoRef, Vector3 origin)
        {
            Area area = new(areaProtoRef, origin);
            _areaDict[id] = area;
            return area;
        }

        public Area AddArea(uint id, ulong areaProtoRef)
        {
            return AddArea(id, areaProtoRef, Vector3.Zero);
        }

        public bool ImportLayoutFromFile(string fileName)
        {
            string filePath = Path.Combine(FileHelper.ServerRoot, "Data", "Regions", fileName);
            if (File.Exists(filePath) == false)
                return Logger.WarnReturn(false, $"ImportLayoutFromFile(): File {fileName} not found");

            RegionDump layout = FileHelper.DeserializeJson<RegionDump>(filePath);
            _areaDict.Clear();
            foreach (var areaKvp in layout)
            {
                Area area = AddArea(areaKvp.Key, areaKvp.Value.PrototypeDataRef, areaKvp.Value.Origin);
                foreach (var cellKvp in areaKvp.Value.Cells)
                    area.AddCell(cellKvp.Key, HashHelper.HashPath($"&{cellKvp.Value.PrototypeName}"), cellKvp.Value.Position);
            }

            return true;
        }

        private class RegionDump : Dictionary<uint, AreaDump> { }

        private class AreaDump
        {
            public ulong PrototypeDataRef { get; set; }
            public Vector3 Origin { get; set; }
            public SortedDictionary<uint, CellDump> Cells { get; set; }
        }

        private class CellDump
        {
            public string PrototypeName { get; set; }
            public Vector3 Position { get; set; }

            public CellDump(string prototypeName, Vector3 position)
            {
                PrototypeName = prototypeName;
                Position = position;
            }
        }
    }
}
