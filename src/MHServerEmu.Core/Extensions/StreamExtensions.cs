﻿namespace MHServerEmu.Core.Extensions
{
    public static class StreamExtensions
    {
        public static bool ReadByteAt(this MemoryStream stream, long position, out byte value)
        {
            try
            {
                value = 0;

                long previousPosition = stream.Position;
                stream.Position = position;
                int result = stream.ReadByte();
                stream.Position = previousPosition;

                if (result == -1)
                    return false;

                value = (byte)result;
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        public static bool WriteByteAt(this MemoryStream stream, long position, byte value)
        {
            try
            {
                long previousPosition = stream.Position;
                stream.Position = position;
                stream.WriteByte(value);
                stream.Position = previousPosition;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
