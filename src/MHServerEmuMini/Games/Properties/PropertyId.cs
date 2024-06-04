namespace MHServerEmuMini.Games.Properties
{
    public readonly struct PropertyId
    {
        private const int ParamBitCount = 53;

        public ulong Raw { get; }
        public PropertyEnum Enum { get => (PropertyEnum)(Raw >> ParamBitCount); }

        public PropertyId()
        {
            Raw = (ulong)PropertyEnum.Invalid << ParamBitCount;
        }

        public PropertyId(PropertyEnum propertyEnum)
        {
            Raw = (ulong)propertyEnum << ParamBitCount;
        }

        public PropertyId(PropertyEnum propertyEnum, ulong paramValue, int numBits)
        {
            Raw = (ulong)propertyEnum << ParamBitCount;
            Raw |= paramValue << (ParamBitCount - numBits);
        }

        public static implicit operator PropertyId(PropertyEnum propertyEnum) => new(propertyEnum);
    }
}
