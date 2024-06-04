﻿using System.Text;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Extensions
{
    public static class BinaryReaderExtensions
    {
        /// <summary>
        /// Reads an unsigned 24-bit integer and returns it as <see cref="int"/>.
        /// </summary>
        public static int ReadUInt24(this BinaryReader reader)
        {
            // C# does not have native support for UInt24, so we store the value in an Int32.
            // We are using signed instead of unsigned because this is used only for packet sizes,
            // and we can avoid casting it to signed later. This implementation assumes little endian.
            int value = reader.ReadByte();
            value |= reader.ReadByte() << 8;
            value |= reader.ReadByte() << 16;
            return value;
        }

        /// <summary>
        /// Reads a fixed-length string preceded by its length as a 16-bit unsigned integer.
        /// </summary>
        public static string ReadFixedString16(this BinaryReader reader)
        {
            return Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
        }

        /// <summary>
        /// Reads a fixed-length string preceded by its length as a 32-bit signed integer.
        /// </summary>
        public static string ReadFixedString32(this BinaryReader reader)
        {
            return Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
        }

        /// <summary>
        /// Read a null-terminated string at the current position.
        /// </summary>
        public static string ReadNullTerminatedString(this BinaryReader reader)
        {
            List<byte> byteList = new();

            while (true)
            {
                byte b = reader.ReadByte();
                if (b == 0x00) break;
                byteList.Add(b);
            }

            return Encoding.UTF8.GetString(byteList.ToArray());
        }

        /// <summary>
        /// Read a null-terminated string at the specified offset.
        /// </summary>
        public static string ReadNullTerminatedString(this BinaryReader reader, long offset)
        {
            long pos = reader.BaseStream.Position;              // Remember the current position
            reader.BaseStream.Seek(offset, 0);                  // Move to the offset
            string result = reader.ReadNullTerminatedString();  // Read the string
            reader.BaseStream.Seek(pos, 0);                     // Return to the original position
            return result;
        }

        public static Vector2 ReadVector2(this BinaryReader reader)
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static Orientation ReadOrientation(this BinaryReader reader)
        {
            return new Orientation(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
