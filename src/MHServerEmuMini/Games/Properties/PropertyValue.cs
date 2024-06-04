using System.Runtime.InteropServices;

namespace MHServerEmuMini.Games.Properties
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct PropertyValue : IEquatable<PropertyValue>
    {
        [FieldOffset(0)]
        public readonly long RawLong;
        [FieldOffset(0)]
        public readonly float RawFloat;

        public PropertyValue(bool value) { RawLong = Convert.ToInt64(value); } 
        public PropertyValue(int value) { RawLong = value; } 
        public PropertyValue(long value) { RawLong = value; } 
        public PropertyValue(uint value) { RawLong = value; } 
        public PropertyValue(ulong value) { RawLong = (long)value; } 
        public PropertyValue(float value) { RawFloat = value; }

        public bool Equals(PropertyValue other)
        {
            return RawLong == other.RawLong;
        }

        public override bool Equals(object obj)
        {
            return obj is PropertyValue other && Equals(other); 
        }

        public override int GetHashCode()
        {
            return RawLong.GetHashCode();
        }

        public static implicit operator PropertyValue(bool value) => new(value);
        public static implicit operator PropertyValue(float value) => new(value);
        public static implicit operator PropertyValue(int value) => new(value);
        public static implicit operator PropertyValue(long value) => new(value);
        public static implicit operator PropertyValue(uint value) => new(value);
        public static implicit operator PropertyValue(ulong value) => new(value);
        public static implicit operator PropertyValue(TimeSpan value) => new((long)value.TotalMilliseconds);

        public static implicit operator bool(PropertyValue value) => value.RawLong != 0;
        public static implicit operator float(PropertyValue value) => value.RawFloat;
        public static implicit operator int(PropertyValue value) => (int)value.RawLong;
        public static implicit operator long(PropertyValue value) => value.RawLong;
        public static implicit operator uint(PropertyValue value) => (uint)value.RawLong;
        public static implicit operator ulong(PropertyValue value) => (ulong)value.RawLong;
        public static implicit operator TimeSpan(PropertyValue value) => TimeSpan.FromMilliseconds(value.RawLong);
    }
}
