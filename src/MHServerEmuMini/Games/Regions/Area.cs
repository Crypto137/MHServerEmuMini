using MHServerEmu.Core.VectorMath;

namespace MHServerEmuMini.Games.Regions
{
    public class Area
    {
        private readonly SortedDictionary<uint, Cell> _cellDict = new();

        public ulong PrototypeDataRef { get; }
        public Vector3 Origin { get; }
        public IEnumerable<KeyValuePair<uint, Cell>> Cells { get => _cellDict; }

        public Area(ulong areaProtoRef, Vector3 origin)
        {
            PrototypeDataRef = areaProtoRef;
            Origin = origin;
        }

        public Cell AddCell(uint id, ulong cellProtoRef, Vector3 position)
        {
            Cell cell = new(cellProtoRef, position);
            _cellDict[id] = cell;
            return cell;
        }

        public Cell AddCell(uint id, ulong cellProtoRef)
        {
            return AddCell(id, cellProtoRef, Vector3.Zero);
        }
    }
}
