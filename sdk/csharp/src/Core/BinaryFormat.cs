using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TuskLang
{
    /// <summary>
    /// Binary format reader for .pnt files
    /// </summary>
    public class BinaryFormatReader : IDisposable
    {
        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly byte[] _buffer;
        private int _bufferPosition;
        private int _bufferLength;

        public BinaryFormatReader(Stream stream, bool leaveOpen = false)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _leaveOpen = leaveOpen;
            _buffer = ArrayPool<byte>.Shared.Rent(8192);
            _bufferPosition = 0;
            _bufferLength = 0;
        }

        /// <summary>
        /// Reads the file header and validates format
        /// </summary>
        public async Task<BinaryHeader> ReadHeaderAsync()
        {
            var headerBytes = new byte[64];
            await ReadExactAsync(headerBytes, 0, 64);

            // Validate magic bytes
            if (headerBytes[0] != 0x50 || headerBytes[1] != 0x4E || headerBytes[2] != 0x54 || headerBytes[3] != 0x00)
                throw new InvalidDataException("Invalid magic bytes in header");

            var header = new BinaryHeader
            {
                Version = new Version(headerBytes[4], headerBytes[5], headerBytes[6]),
                Flags = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes.AsSpan(7, 4)),
                DataOffset = BinaryPrimitives.ReadUInt64LittleEndian(headerBytes.AsSpan(11, 8)),
                IndexOffset = BinaryPrimitives.ReadUInt64LittleEndian(headerBytes.AsSpan(19, 8)),
                DataSize = BinaryPrimitives.ReadUInt64LittleEndian(headerBytes.AsSpan(27, 8)),
                IndexSize = BinaryPrimitives.ReadUInt64LittleEndian(headerBytes.AsSpan(35, 8)),
                HeaderChecksum = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes.AsSpan(43, 4))
            };

            // Validate header checksum
            var calculatedChecksum = Crc32.Calculate(headerBytes.AsSpan(0, 43));
            if (header.HeaderChecksum != calculatedChecksum)
                throw new InvalidDataException("Header checksum validation failed");

            return header;
        }

        /// <summary>
        /// Reads a value from the data section
        /// </summary>
        public async Task<object?> ReadValueAsync()
        {
            var typeByte = await ReadByteAsync();
            var type = (BinaryType)typeByte;

            return type switch
            {
                BinaryType.Null => null,
                BinaryType.Bool => await ReadBoolAsync(),
                BinaryType.Int8 => await ReadInt8Async(),
                BinaryType.Int16 => await ReadInt16Async(),
                BinaryType.Int32 => await ReadInt32Async(),
                BinaryType.Int64 => await ReadInt64Async(),
                BinaryType.UInt8 => await ReadUInt8Async(),
                BinaryType.UInt16 => await ReadUInt16Async(),
                BinaryType.UInt32 => await ReadUInt32Async(),
                BinaryType.UInt64 => await ReadUInt64Async(),
                BinaryType.Float32 => await ReadFloat32Async(),
                BinaryType.Float64 => await ReadFloat64Async(),
                BinaryType.String => await ReadStringAsync(),
                BinaryType.Bytes => await ReadBytesAsync(),
                BinaryType.Array => await ReadArrayAsync(),
                BinaryType.Object => await ReadObjectAsync(),
                BinaryType.Timestamp => await ReadTimestampAsync(),
                BinaryType.Duration => await ReadDurationAsync(),
                BinaryType.Reference => await ReadReferenceAsync(),
                BinaryType.Decimal => await ReadDecimalAsync(),
                _ => throw new InvalidDataException($"Unknown binary type: {type}")
            };
        }

        private async Task<byte> ReadByteAsync()
        {
            if (_bufferPosition >= _bufferLength)
            {
                await FillBufferAsync();
            }
            return _buffer[_bufferPosition++];
        }

        private async Task FillBufferAsync()
        {
            _bufferLength = await _stream.ReadAsync(_buffer, 0, _buffer.Length);
            _bufferPosition = 0;
            if (_bufferLength == 0)
                throw new EndOfStreamException();
        }

        private async Task ReadExactAsync(byte[] buffer, int offset, int count)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var read = await _stream.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                    throw new EndOfStreamException();
                totalRead += read;
            }
        }

        private async Task<bool> ReadBoolAsync() => await ReadByteAsync() != 0;
        private async Task<sbyte> ReadInt8Async() => (sbyte)await ReadByteAsync();
        private async Task<short> ReadInt16Async() => BinaryPrimitives.ReadInt16LittleEndian(await ReadBytesAsync(2));
        private async Task<int> ReadInt32Async() => BinaryPrimitives.ReadInt32LittleEndian(await ReadBytesAsync(4));
        private async Task<long> ReadInt64Async() => BinaryPrimitives.ReadInt64LittleEndian(await ReadBytesAsync(8));
        private async Task<byte> ReadUInt8Async() => await ReadByteAsync();
        private async Task<ushort> ReadUInt16Async() => BinaryPrimitives.ReadUInt16LittleEndian(await ReadBytesAsync(2));
        private async Task<uint> ReadUInt32Async() => BinaryPrimitives.ReadUInt32LittleEndian(await ReadBytesAsync(4));
        private async Task<ulong> ReadUInt64Async() => BinaryPrimitives.ReadUInt64LittleEndian(await ReadBytesAsync(8));
        private async Task<float> ReadFloat32Async() => BitConverter.ToSingle(await ReadBytesAsync(4), 0);
        private async Task<double> ReadFloat64Async() => BitConverter.ToDouble(await ReadBytesAsync(8), 0);

        private async Task<string> ReadStringAsync()
        {
            var length = await ReadLengthAsync();
            var bytes = await ReadBytesAsync(length);
            return Encoding.UTF8.GetString(bytes);
        }

        private async Task<byte[]> ReadBytesAsync(int length)
        {
            var bytes = new byte[length];
            await ReadExactAsync(bytes, 0, length);
            return bytes;
        }

        private async Task<byte[]> ReadBytesAsync()
        {
            var length = await ReadLengthAsync();
            return await ReadBytesAsync(length);
        }

        private async Task<object[]> ReadArrayAsync()
        {
            var length = await ReadLengthAsync();
            var array = new object[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = await ReadValueAsync() ?? throw new InvalidDataException("Array element cannot be null");
            }
            return array;
        }

        private async Task<Dictionary<string, object>> ReadObjectAsync()
        {
            var count = await ReadLengthAsync();
            var obj = new Dictionary<string, object>();
            for (int i = 0; i < count; i++)
            {
                var key = await ReadStringAsync();
                var value = await ReadValueAsync();
                obj[key] = value ?? throw new InvalidDataException($"Object value for key '{key}' cannot be null");
            }
            return obj;
        }

        private async Task<DateTime> ReadTimestampAsync()
        {
            var ticks = await ReadInt64Async();
            return DateTime.UnixEpoch.AddTicks(ticks);
        }

        private async Task<TimeSpan> ReadDurationAsync()
        {
            var ticks = await ReadInt64Async();
            return TimeSpan.FromTicks(ticks);
        }

        private async Task<ulong> ReadReferenceAsync() => await ReadUInt64Async();

        private async Task<decimal> ReadDecimalAsync()
        {
            var bytes = await ReadBytesAsync(16);
            var bits = new int[4];
            for (int i = 0; i < 4; i++)
            {
                bits[i] = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(i * 4, 4));
            }
            return new decimal(bits);
        }

        private async Task<int> ReadLengthAsync()
        {
            var firstByte = await ReadByteAsync();
            if ((firstByte & 0x80) == 0)
                return firstByte;

            var length = firstByte & 0x7F;
            var shift = 7;
            while (true)
            {
                var nextByte = await ReadByteAsync();
                length |= (nextByte & 0x7F) << shift;
                if ((nextByte & 0x80) == 0)
                    break;
                shift += 7;
            }
            return length;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            if (!_leaveOpen)
                _stream?.Dispose();
        }
    }

    /// <summary>
    /// Binary format writer for .pnt files
    /// </summary>
    public class BinaryFormatWriter : IDisposable
    {
        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly byte[] _buffer;
        private int _bufferPosition;

        public BinaryFormatWriter(Stream stream, bool leaveOpen = false)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _leaveOpen = leaveOpen;
            _buffer = ArrayPool<byte>.Shared.Rent(8192);
            _bufferPosition = 0;
        }

        /// <summary>
        /// Writes the file header
        /// </summary>
        public async Task WriteHeaderAsync(BinaryHeader header)
        {
            var headerBytes = new byte[64];
            
            // Magic bytes
            headerBytes[0] = 0x50; // P
            headerBytes[1] = 0x4E; // N
            headerBytes[2] = 0x54; // T
            headerBytes[3] = 0x00; // null

            // Version
            headerBytes[4] = (byte)header.Version.Major;
            headerBytes[5] = (byte)header.Version.Minor;
            headerBytes[6] = (byte)header.Version.Build;

            // Flags
            BinaryPrimitives.WriteUInt32LittleEndian(headerBytes.AsSpan(7, 4), header.Flags);

            // Offsets and sizes
            BinaryPrimitives.WriteUInt64LittleEndian(headerBytes.AsSpan(11, 8), header.DataOffset);
            BinaryPrimitives.WriteUInt64LittleEndian(headerBytes.AsSpan(19, 8), header.IndexOffset);
            BinaryPrimitives.WriteUInt64LittleEndian(headerBytes.AsSpan(27, 8), header.DataSize);
            BinaryPrimitives.WriteUInt64LittleEndian(headerBytes.AsSpan(35, 8), header.IndexSize);

            // Calculate and write checksum
            var checksum = Crc32.Calculate(headerBytes.AsSpan(0, 43));
            BinaryPrimitives.WriteUInt32LittleEndian(headerBytes.AsSpan(43, 4), checksum);

            await _stream.WriteAsync(headerBytes, 0, 64);
        }

        /// <summary>
        /// Writes a value to the data section
        /// </summary>
        public async Task WriteValueAsync(object? value)
        {
            if (value == null)
            {
                await WriteByteAsync((byte)BinaryType.Null);
                return;
            }

            switch (value)
            {
                case bool b:
                    await WriteByteAsync((byte)BinaryType.Bool);
                    await WriteBoolAsync(b);
                    break;
                case sbyte i8:
                    await WriteByteAsync((byte)BinaryType.Int8);
                    await WriteInt8Async(i8);
                    break;
                case short i16:
                    await WriteByteAsync((byte)BinaryType.Int16);
                    await WriteInt16Async(i16);
                    break;
                case int i32:
                    await WriteByteAsync((byte)BinaryType.Int32);
                    await WriteInt32Async(i32);
                    break;
                case long i64:
                    await WriteByteAsync((byte)BinaryType.Int64);
                    await WriteInt64Async(i64);
                    break;
                case byte u8:
                    await WriteByteAsync((byte)BinaryType.UInt8);
                    await WriteUInt8Async(u8);
                    break;
                case ushort u16:
                    await WriteByteAsync((byte)BinaryType.UInt16);
                    await WriteUInt16Async(u16);
                    break;
                case uint u32:
                    await WriteByteAsync((byte)BinaryType.UInt32);
                    await WriteUInt32Async(u32);
                    break;
                case ulong u64:
                    await WriteByteAsync((byte)BinaryType.UInt64);
                    await WriteUInt64Async(u64);
                    break;
                case float f32:
                    await WriteByteAsync((byte)BinaryType.Float32);
                    await WriteFloat32Async(f32);
                    break;
                case double f64:
                    await WriteByteAsync((byte)BinaryType.Float64);
                    await WriteFloat64Async(f64);
                    break;
                case string s:
                    await WriteByteAsync((byte)BinaryType.String);
                    await WriteStringAsync(s);
                    break;
                case byte[] bytes:
                    await WriteByteAsync((byte)BinaryType.Bytes);
                    await WriteBytesAsync(bytes);
                    break;
                case object[] array:
                    await WriteByteAsync((byte)BinaryType.Array);
                    await WriteArrayAsync(array);
                    break;
                case Dictionary<string, object> obj:
                    await WriteByteAsync((byte)BinaryType.Object);
                    await WriteObjectAsync(obj);
                    break;
                case DateTime dt:
                    await WriteByteAsync((byte)BinaryType.Timestamp);
                    await WriteTimestampAsync(dt);
                    break;
                case TimeSpan ts:
                    await WriteByteAsync((byte)BinaryType.Duration);
                    await WriteDurationAsync(ts);
                    break;
                case decimal dec:
                    await WriteByteAsync((byte)BinaryType.Decimal);
                    await WriteDecimalAsync(dec);
                    break;
                default:
                    throw new ArgumentException($"Unsupported type: {value.GetType()}");
            }
        }

        private async Task WriteByteAsync(byte value)
        {
            if (_bufferPosition >= _buffer.Length)
                await FlushBufferAsync();
            _buffer[_bufferPosition++] = value;
        }

        private async Task FlushBufferAsync()
        {
            if (_bufferPosition > 0)
            {
                await _stream.WriteAsync(_buffer, 0, _bufferPosition);
                _bufferPosition = 0;
            }
        }

        private async Task WriteBoolAsync(bool value) => await WriteByteAsync(value ? (byte)1 : (byte)0);
        private async Task WriteInt8Async(sbyte value) => await WriteByteAsync((byte)value);
        private async Task WriteInt16Async(short value) => await WriteBytesAsync(BitConverter.GetBytes(value));
        private async Task WriteInt32Async(int value) => await WriteBytesAsync(BitConverter.GetBytes(value));
        private async Task WriteInt64Async(long value) => await WriteBytesAsync(BitConverter.GetBytes(value));
        private async Task WriteUInt8Async(byte value) => await WriteByteAsync(value);
        private async Task WriteUInt16Async(ushort value) => await WriteBytesAsync(BitConverter.GetBytes(value));
        private async Task WriteUInt32Async(uint value) => await WriteBytesAsync(BitConverter.GetBytes(value));
        private async Task WriteUInt64Async(ulong value) => await WriteBytesAsync(BitConverter.GetBytes(value));
        private async Task WriteFloat32Async(float value) => await WriteBytesAsync(BitConverter.GetBytes(value));
        private async Task WriteFloat64Async(double value) => await WriteBytesAsync(BitConverter.GetBytes(value));

        private async Task WriteStringAsync(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            await WriteLengthAsync(bytes.Length);
            await WriteBytesAsync(bytes);
        }

        private async Task WriteBytesAsync(byte[] bytes)
        {
            await WriteLengthAsync(bytes.Length);
            await _stream.WriteAsync(bytes, 0, bytes.Length);
        }

        private async Task WriteArrayAsync(object[] array)
        {
            await WriteLengthAsync(array.Length);
            foreach (var item in array)
            {
                await WriteValueAsync(item);
            }
        }

        private async Task WriteObjectAsync(Dictionary<string, object> obj)
        {
            await WriteLengthAsync(obj.Count);
            foreach (var kvp in obj)
            {
                await WriteStringAsync(kvp.Key);
                await WriteValueAsync(kvp.Value);
            }
        }

        private async Task WriteTimestampAsync(DateTime value)
        {
            var ticks = (value - DateTime.UnixEpoch).Ticks;
            await WriteInt64Async(ticks);
        }

        private async Task WriteDurationAsync(TimeSpan value)
        {
            await WriteInt64Async(value.Ticks);
        }

        private async Task WriteDecimalAsync(decimal value)
        {
            var bits = decimal.GetBits(value);
            var bytes = new byte[16];
            for (int i = 0; i < 4; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 4, 4), bits[i]);
            }
            await WriteBytesAsync(bytes);
        }

        private async Task WriteLengthAsync(int length)
        {
            if (length < 0x80)
            {
                await WriteByteAsync((byte)length);
                return;
            }

            while (length >= 0x80)
            {
                await WriteByteAsync((byte)((length & 0x7F) | 0x80));
                length >>= 7;
            }
            await WriteByteAsync((byte)length);
        }

        public async Task FlushAsync()
        {
            await FlushBufferAsync();
            await _stream.FlushAsync();
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            if (!_leaveOpen)
                _stream?.Dispose();
        }
    }

    /// <summary>
    /// Binary file header structure
    /// </summary>
    public struct BinaryHeader
    {
        public Version Version;
        public uint Flags;
        public ulong DataOffset;
        public ulong IndexOffset;
        public ulong DataSize;
        public ulong IndexSize;
        public uint HeaderChecksum;
    }

    /// <summary>
    /// Binary data types
    /// </summary>
    public enum BinaryType : byte
    {
        Null = 0x00,
        Bool = 0x01,
        Int8 = 0x02,
        Int16 = 0x03,
        Int32 = 0x04,
        Int64 = 0x05,
        UInt8 = 0x06,
        UInt16 = 0x07,
        UInt32 = 0x08,
        UInt64 = 0x09,
        Float32 = 0x0A,
        Float64 = 0x0B,
        String = 0x0C,
        Bytes = 0x0D,
        Array = 0x0E,
        Object = 0x0F,
        Timestamp = 0x10,
        Duration = 0x11,
        Reference = 0x12,
        Decimal = 0x13
    }

    /// <summary>
    /// CRC32 implementation for checksum calculation
    /// </summary>
    public static class Crc32
    {
        private static readonly uint[] Table = new uint[256];

        static Crc32()
        {
            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                        crc = (crc >> 1) ^ 0xEDB88320;
                    else
                        crc >>= 1;
                }
                Table[i] = crc;
            }
        }

        public static uint Calculate(ReadOnlySpan<byte> data)
        {
            uint crc = 0xFFFFFFFF;
            foreach (byte b in data)
            {
                crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }
            return ~crc;
        }
    }
} 