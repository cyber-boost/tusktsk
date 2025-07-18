use std::collections::HashMap;
use std::io::{self, Read, Write, Seek, SeekFrom};
use std::time::{SystemTime, UNIX_EPOCH, Duration};
use byteorder::{LittleEndian, ReadBytesExt, WriteBytesExt};

/// Binary data types
#[repr(u8)]
#[derive(Debug, Clone, Copy, PartialEq)]
pub enum BinaryType {
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
    Decimal = 0x13,
}

impl From<u8> for BinaryType {
    fn from(byte: u8) -> Self {
        match byte {
            0x00 => BinaryType::Null,
            0x01 => BinaryType::Bool,
            0x02 => BinaryType::Int8,
            0x03 => BinaryType::Int16,
            0x04 => BinaryType::Int32,
            0x05 => BinaryType::Int64,
            0x06 => BinaryType::UInt8,
            0x07 => BinaryType::UInt16,
            0x08 => BinaryType::UInt32,
            0x09 => BinaryType::UInt64,
            0x0A => BinaryType::Float32,
            0x0B => BinaryType::Float64,
            0x0C => BinaryType::String,
            0x0D => BinaryType::Bytes,
            0x0E => BinaryType::Array,
            0x0F => BinaryType::Object,
            0x10 => BinaryType::Timestamp,
            0x11 => BinaryType::Duration,
            0x12 => BinaryType::Reference,
            0x13 => BinaryType::Decimal,
            _ => panic!("Unknown binary type: {}", byte),
        }
    }
}

/// Binary file header structure
#[derive(Debug, Clone)]
pub struct BinaryHeader {
    pub version: (u8, u8, u8),
    pub flags: u32,
    pub data_offset: u64,
    pub index_offset: u64,
    pub data_size: u64,
    pub index_size: u64,
    pub header_checksum: u32,
}

/// Binary value types
#[derive(Debug, Clone)]
pub enum BinaryValue {
    Null,
    Bool(bool),
    Int8(i8),
    Int16(i16),
    Int32(i32),
    Int64(i64),
    UInt8(u8),
    UInt16(u16),
    UInt32(u32),
    UInt64(u64),
    Float32(f32),
    Float64(f64),
    String(String),
    Bytes(Vec<u8>),
    Array(Vec<BinaryValue>),
    Object(HashMap<String, BinaryValue>),
    Timestamp(SystemTime),
    Duration(Duration),
    Reference(u64),
    Decimal(f64), // Simplified decimal representation
}

/// Binary format reader
pub struct BinaryFormatReader<R> {
    reader: R,
}

impl<R: Read + Seek> BinaryFormatReader<R> {
    pub fn new(reader: R) -> Self {
        Self { reader }
    }

    /// Reads the file header and validates format
    pub fn read_header(&mut self) -> io::Result<BinaryHeader> {
        let mut header_bytes = [0u8; 64];
        self.reader.read_exact(&mut header_bytes)?;

        // Validate magic bytes
        if header_bytes[0..4] != [0x50, 0x4E, 0x54, 0x00] {
            return Err(io::Error::new(
                io::ErrorKind::InvalidData,
                "Invalid magic bytes in header",
            ));
        }

        let version = (header_bytes[4], header_bytes[5], header_bytes[6]);
        let flags = u32::from_le_bytes([header_bytes[7], header_bytes[8], header_bytes[9], header_bytes[10]]);
        let data_offset = u64::from_le_bytes([
            header_bytes[11], header_bytes[12], header_bytes[13], header_bytes[14],
            header_bytes[15], header_bytes[16], header_bytes[17], header_bytes[18],
        ]);
        let index_offset = u64::from_le_bytes([
            header_bytes[19], header_bytes[20], header_bytes[21], header_bytes[22],
            header_bytes[23], header_bytes[24], header_bytes[25], header_bytes[26],
        ]);
        let data_size = u64::from_le_bytes([
            header_bytes[27], header_bytes[28], header_bytes[29], header_bytes[30],
            header_bytes[31], header_bytes[32], header_bytes[33], header_bytes[34],
        ]);
        let index_size = u64::from_le_bytes([
            header_bytes[35], header_bytes[36], header_bytes[37], header_bytes[38],
            header_bytes[39], header_bytes[40], header_bytes[41], header_bytes[42],
        ]);
        let header_checksum = u32::from_le_bytes([header_bytes[43], header_bytes[44], header_bytes[45], header_bytes[46]]);

        // Validate header checksum
        let calculated_checksum = crc32(&header_bytes[0..43]);
        if header_checksum != calculated_checksum {
            return Err(io::Error::new(
                io::ErrorKind::InvalidData,
                "Header checksum validation failed",
            ));
        }

        Ok(BinaryHeader {
            version,
            flags,
            data_offset,
            index_offset,
            data_size,
            index_size,
            header_checksum,
        })
    }

    /// Reads a value from the data section
    pub fn read_value(&mut self) -> io::Result<BinaryValue> {
        let type_byte = self.reader.read_u8()?;
        let binary_type = BinaryType::from(type_byte);

        match binary_type {
            BinaryType::Null => Ok(BinaryValue::Null),
            BinaryType::Bool => {
                let value = self.reader.read_u8()? != 0;
                Ok(BinaryValue::Bool(value))
            }
            BinaryType::Int8 => {
                let value = self.reader.read_i8()?;
                Ok(BinaryValue::Int8(value))
            }
            BinaryType::Int16 => {
                let value = self.reader.read_i16::<LittleEndian>()?;
                Ok(BinaryValue::Int16(value))
            }
            BinaryType::Int32 => {
                let value = self.reader.read_i32::<LittleEndian>()?;
                Ok(BinaryValue::Int32(value))
            }
            BinaryType::Int64 => {
                let value = self.reader.read_i64::<LittleEndian>()?;
                Ok(BinaryValue::Int64(value))
            }
            BinaryType::UInt8 => {
                let value = self.reader.read_u8()?;
                Ok(BinaryValue::UInt8(value))
            }
            BinaryType::UInt16 => {
                let value = self.reader.read_u16::<LittleEndian>()?;
                Ok(BinaryValue::UInt16(value))
            }
            BinaryType::UInt32 => {
                let value = self.reader.read_u32::<LittleEndian>()?;
                Ok(BinaryValue::UInt32(value))
            }
            BinaryType::UInt64 => {
                let value = self.reader.read_u64::<LittleEndian>()?;
                Ok(BinaryValue::UInt64(value))
            }
            BinaryType::Float32 => {
                let value = self.reader.read_f32::<LittleEndian>()?;
                Ok(BinaryValue::Float32(value))
            }
            BinaryType::Float64 => {
                let value = self.reader.read_f64::<LittleEndian>()?;
                Ok(BinaryValue::Float64(value))
            }
            BinaryType::String => {
                let length = self.read_length()?;
                let mut bytes = vec![0u8; length];
                self.reader.read_exact(&mut bytes)?;
                let string = String::from_utf8(bytes)
                    .map_err(|e| io::Error::new(io::ErrorKind::InvalidData, e))?;
                Ok(BinaryValue::String(string))
            }
            BinaryType::Bytes => {
                let length = self.read_length()?;
                let mut bytes = vec![0u8; length];
                self.reader.read_exact(&mut bytes)?;
                Ok(BinaryValue::Bytes(bytes))
            }
            BinaryType::Array => {
                let length = self.read_length()?;
                let mut array = Vec::with_capacity(length);
                for _ in 0..length {
                    array.push(self.read_value()?);
                }
                Ok(BinaryValue::Array(array))
            }
            BinaryType::Object => {
                let count = self.read_length()?;
                let mut object = HashMap::new();
                for _ in 0..count {
                    let key = match self.read_value()? {
                        BinaryValue::String(s) => s,
                        _ => return Err(io::Error::new(io::ErrorKind::InvalidData, "Object key must be string")),
                    };
                    let value = self.read_value()?;
                    object.insert(key, value);
                }
                Ok(BinaryValue::Object(object))
            }
            BinaryType::Timestamp => {
                let ticks = self.reader.read_i64::<LittleEndian>()?;
                let timestamp = UNIX_EPOCH + Duration::from_nanos(ticks as u64 * 100);
                Ok(BinaryValue::Timestamp(timestamp))
            }
            BinaryType::Duration => {
                let ticks = self.reader.read_i64::<LittleEndian>()?;
                let duration = Duration::from_nanos(ticks as u64 * 100);
                Ok(BinaryValue::Duration(duration))
            }
            BinaryType::Reference => {
                let value = self.reader.read_u64::<LittleEndian>()?;
                Ok(BinaryValue::Reference(value))
            }
            BinaryType::Decimal => {
                let mut bytes = [0u8; 16];
                self.reader.read_exact(&mut bytes)?;
                // Simplified decimal representation
                let value = f64::from_le_bytes([
                    bytes[0], bytes[1], bytes[2], bytes[3],
                    bytes[4], bytes[5], bytes[6], bytes[7],
                ]);
                Ok(BinaryValue::Decimal(value))
            }
        }
    }

    fn read_length(&mut self) -> io::Result<usize> {
        let first_byte = self.reader.read_u8()?;
        if (first_byte & 0x80) == 0 {
            return Ok(first_byte as usize);
        }

        let mut length = (first_byte & 0x7F) as usize;
        let mut shift = 7;
        loop {
            let byte = self.reader.read_u8()?;
            length |= ((byte & 0x7F) as usize) << shift;
            if (byte & 0x80) == 0 {
                break;
            }
            shift += 7;
        }
        Ok(length)
    }
}

/// Binary format writer
pub struct BinaryFormatWriter<W> {
    writer: W,
}

impl<W: Write + Seek> BinaryFormatWriter<W> {
    pub fn new(writer: W) -> Self {
        Self { writer }
    }

    /// Writes the file header
    pub fn write_header(&mut self, header: &BinaryHeader) -> io::Result<()> {
        let mut header_bytes = [0u8; 64];

        // Magic bytes
        header_bytes[0..4].copy_from_slice(&[0x50, 0x4E, 0x54, 0x00]);

        // Version
        header_bytes[4] = header.version.0;
        header_bytes[5] = header.version.1;
        header_bytes[6] = header.version.2;

        // Flags
        header_bytes[7..11].copy_from_slice(&header.flags.to_le_bytes());

        // Offsets and sizes
        header_bytes[11..19].copy_from_slice(&header.data_offset.to_le_bytes());
        header_bytes[19..27].copy_from_slice(&header.index_offset.to_le_bytes());
        header_bytes[27..35].copy_from_slice(&header.data_size.to_le_bytes());
        header_bytes[35..43].copy_from_slice(&header.index_size.to_le_bytes());

        // Calculate and write checksum
        let checksum = crc32(&header_bytes[0..43]);
        header_bytes[43..47].copy_from_slice(&checksum.to_le_bytes());

        self.writer.write_all(&header_bytes)?;
        Ok(())
    }

    /// Writes a value to the data section
    pub fn write_value(&mut self, value: &BinaryValue) -> io::Result<()> {
        match value {
            BinaryValue::Null => {
                self.writer.write_u8(BinaryType::Null as u8)?;
            }
            BinaryValue::Bool(b) => {
                self.writer.write_u8(BinaryType::Bool as u8)?;
                self.writer.write_u8(if *b { 1 } else { 0 })?;
            }
            BinaryValue::Int8(i) => {
                self.writer.write_u8(BinaryType::Int8 as u8)?;
                self.writer.write_i8(*i)?;
            }
            BinaryValue::Int16(i) => {
                self.writer.write_u8(BinaryType::Int16 as u8)?;
                self.writer.write_i16::<LittleEndian>(*i)?;
            }
            BinaryValue::Int32(i) => {
                self.writer.write_u8(BinaryType::Int32 as u8)?;
                self.writer.write_i32::<LittleEndian>(*i)?;
            }
            BinaryValue::Int64(i) => {
                self.writer.write_u8(BinaryType::Int64 as u8)?;
                self.writer.write_i64::<LittleEndian>(*i)?;
            }
            BinaryValue::UInt8(u) => {
                self.writer.write_u8(BinaryType::UInt8 as u8)?;
                self.writer.write_u8(*u)?;
            }
            BinaryValue::UInt16(u) => {
                self.writer.write_u8(BinaryType::UInt16 as u8)?;
                self.writer.write_u16::<LittleEndian>(*u)?;
            }
            BinaryValue::UInt32(u) => {
                self.writer.write_u8(BinaryType::UInt32 as u8)?;
                self.writer.write_u32::<LittleEndian>(*u)?;
            }
            BinaryValue::UInt64(u) => {
                self.writer.write_u8(BinaryType::UInt64 as u8)?;
                self.writer.write_u64::<LittleEndian>(*u)?;
            }
            BinaryValue::Float32(f) => {
                self.writer.write_u8(BinaryType::Float32 as u8)?;
                self.writer.write_f32::<LittleEndian>(*f)?;
            }
            BinaryValue::Float64(f) => {
                self.writer.write_u8(BinaryType::Float64 as u8)?;
                self.writer.write_f64::<LittleEndian>(*f)?;
            }
            BinaryValue::String(s) => {
                self.writer.write_u8(BinaryType::String as u8)?;
                self.write_length(s.len())?;
                self.writer.write_all(s.as_bytes())?;
            }
            BinaryValue::Bytes(b) => {
                self.writer.write_u8(BinaryType::Bytes as u8)?;
                self.write_length(b.len())?;
                self.writer.write_all(b)?;
            }
            BinaryValue::Array(a) => {
                self.writer.write_u8(BinaryType::Array as u8)?;
                self.write_length(a.len())?;
                for item in a {
                    self.write_value(item)?;
                }
            }
            BinaryValue::Object(o) => {
                self.writer.write_u8(BinaryType::Object as u8)?;
                self.write_length(o.len())?;
                for (key, value) in o {
                    self.write_value(&BinaryValue::String(key.clone()))?;
                    self.write_value(value)?;
                }
            }
            BinaryValue::Timestamp(t) => {
                self.writer.write_u8(BinaryType::Timestamp as u8)?;
                let duration = t.duration_since(UNIX_EPOCH)
                    .map_err(|e| io::Error::new(io::ErrorKind::InvalidData, e))?;
                let ticks = duration.as_nanos() / 100;
                self.writer.write_i64::<LittleEndian>(ticks as i64)?;
            }
            BinaryValue::Duration(d) => {
                self.writer.write_u8(BinaryType::Duration as u8)?;
                let ticks = d.as_nanos() / 100;
                self.writer.write_i64::<LittleEndian>(ticks as i64)?;
            }
            BinaryValue::Reference(r) => {
                self.writer.write_u8(BinaryType::Reference as u8)?;
                self.writer.write_u64::<LittleEndian>(*r)?;
            }
            BinaryValue::Decimal(d) => {
                self.writer.write_u8(BinaryType::Decimal as u8)?;
                let bytes = d.to_le_bytes();
                self.writer.write_all(&bytes)?;
                // Pad to 16 bytes
                let padding = [0u8; 8];
                self.writer.write_all(&padding)?;
            }
        }
        Ok(())
    }

    fn write_length(&mut self, length: usize) -> io::Result<()> {
        if length < 0x80 {
            self.writer.write_u8(length as u8)?;
            return Ok(());
        }

        let mut remaining = length;
        while remaining >= 0x80 {
            self.writer.write_u8(((remaining & 0x7F) | 0x80) as u8)?;
            remaining >>= 7;
        }
        self.writer.write_u8(remaining as u8)?;
        Ok(())
    }

    pub fn flush(&mut self) -> io::Result<()> {
        self.writer.flush()
    }
}

/// CRC32 implementation for checksum calculation
fn crc32(data: &[u8]) -> u32 {
    let mut crc = 0xFFFFFFFFu32;
    for &byte in data {
        crc = CRC32_TABLE[((crc ^ byte as u32) & 0xFF) as usize] ^ (crc >> 8);
    }
    !crc
}

static CRC32_TABLE: [u32; 256] = {
    let mut table = [0u32; 256];
    let mut i = 0;
    while i < 256 {
        let mut crc = i as u32;
        let mut j = 0;
        while j < 8 {
            if (crc & 1) != 0 {
                crc = (crc >> 1) ^ 0xEDB88320;
            } else {
                crc >>= 1;
            }
            j += 1;
        }
        table[i] = crc;
        i += 1;
    }
    table
};

/// Convenience functions for reading and writing .pnt files
pub struct BinaryFormat;

impl BinaryFormat {
    pub fn read_file<R: Read + Seek>(mut reader: R) -> io::Result<(BinaryHeader, BinaryValue)> {
        let mut binary_reader = BinaryFormatReader::new(reader);
        let header = binary_reader.read_header()?;
        let data = binary_reader.read_value()?;
        Ok((header, data))
    }

    pub fn write_file<W: Write + Seek>(mut writer: W, data: &BinaryValue, header: Option<BinaryHeader>) -> io::Result<()> {
        let header = header.unwrap_or(BinaryHeader {
            version: (1, 0, 0),
            flags: 0,
            data_offset: 64,
            index_offset: 0,
            data_size: 0,
            index_size: 0,
            header_checksum: 0,
        });

        let mut binary_writer = BinaryFormatWriter::new(writer);
        binary_writer.write_header(&header)?;
        binary_writer.write_value(data)?;
        binary_writer.flush()?;
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    #[test]
    fn test_binary_format_roundtrip() {
        let test_data = BinaryValue::Object({
            let mut map = HashMap::new();
            map.insert("string".to_string(), BinaryValue::String("hello world".to_string()));
            map.insert("number".to_string(), BinaryValue::Int32(42));
            map.insert("array".to_string(), BinaryValue::Array(vec![
                BinaryValue::Bool(true),
                BinaryValue::Float64(3.14),
            ]));
            map
        });

        let mut buffer = Cursor::new(Vec::new());
        BinaryFormat::write_file(&mut buffer, &test_data, None).unwrap();
        buffer.set_position(0);

        let (header, result) = BinaryFormat::read_file(buffer).unwrap();
        assert_eq!(header.version, (1, 0, 0));
        assert_eq!(result, test_data);
    }

    #[test]
    fn test_header_validation() {
        let mut buffer = Cursor::new(Vec::new());
        let header = BinaryHeader {
            version: (1, 0, 0),
            flags: 0,
            data_offset: 64,
            index_offset: 0,
            data_size: 0,
            index_size: 0,
            header_checksum: 0,
        };

        let mut writer = BinaryFormatWriter::new(&mut buffer);
        writer.write_header(&header).unwrap();
        buffer.set_position(0);

        let mut reader = BinaryFormatReader::new(buffer);
        let read_header = reader.read_header().unwrap();
        assert_eq!(read_header.version, header.version);
    }
} 