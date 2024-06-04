using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;

namespace MHServerEmuMini.Games.Properties
{
    public class PropertyCollection : ISerialize
    {
        private readonly Dictionary<PropertyId, PropertyValue> _propertyList = new();

        public PropertyValue this[PropertyId propertyId]
        {
            get => _propertyList[propertyId];
            set => _propertyList[propertyId] = value;
        }

        public void Clear()
        {
            _propertyList.Clear();
        }

        public virtual bool Serialize(Archive archive)
        {
            bool success = true;

            success &= archive.WriteUnencodedStream((uint)_propertyList.Count);
            foreach (var kvp in _propertyList)
            {
                ulong propertyId = kvp.Key.Raw.ReverseBytes();
                long propertyValue = kvp.Value;
                success &= archive.Transfer(ref propertyId);
                success &= archive.Transfer(ref propertyValue);
            }

            return success;
        }
    }
}
