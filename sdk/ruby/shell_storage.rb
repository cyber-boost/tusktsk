# frozen_string_literal: true

# Shell Storage for Ruby
# Handles binary data compression and storage

module TuskLang
  # Shell format storage for binary data
  class ShellStorage
    # Magic bytes for Shell format identification
    MAGIC = 'FLEX'.bytes
    VERSION = 1

    # Pack data into Shell binary format
    def self.pack(data)
      # Compress data if it's not already compressed
      compressed_data = if data[:data].is_a?(String)
                          Zlib::Deflate.deflate(data[:data])
                        elsif data[:data].is_a?(Array)
                          Zlib::Deflate.deflate(data[:data].pack('C*'))
                        else
                          Zlib::Deflate.deflate(data[:data].to_s)
                        end

      id_bytes = data[:id].bytes

      # Build binary format
      # Magic (4) + Version (1) + ID Length (4) + ID + Data Length (4) + Data
      result = []
      result.concat(MAGIC)
      result << VERSION
      result.concat([id_bytes.length].pack('>N').bytes)
      result.concat(id_bytes)
      result.concat([compressed_data.length].pack('>N').bytes)
      result.concat(compressed_data.bytes)

      result.pack('C*')
    end

    # Unpack Shell binary format
    def self.unpack(shell_data)
      offset = 0

      # Check magic bytes
      magic = shell_data.bytes[offset, 4]
      raise ArgumentError, 'Invalid shell format' unless magic == MAGIC
      offset += 4

      # Read version
      version = shell_data.bytes[offset]
      raise ArgumentError, "Unsupported shell version: #{version}" unless version == VERSION
      offset += 1

      # Read ID
      id_length = shell_data.bytes[offset, 4].pack('C*').unpack('>N')[0]
      offset += 4
      storage_id = shell_data.bytes[offset, id_length].pack('C*').force_encoding('UTF-8')
      offset += id_length

      # Read data
      data_length = shell_data.bytes[offset, 4].pack('C*').unpack('>N')[0]
      offset += 4
      compressed_data = shell_data.bytes[offset, data_length].pack('C*')

      # Decompress
      data = Zlib::Inflate.inflate(compressed_data).force_encoding('UTF-8')

      {
        version: version,
        id: storage_id,
        data: data
      }
    end

    # Detect the type of data stored in Shell format
    def self.detect_type(shell_data)
      data = unpack(shell_data)
      content = data[:data].to_s

      # Try to detect JSON
      return 'json' if content.start_with?('{') || content.start_with?('[')

      # Try to detect TSK format
      return 'tsk' if content.include?('[') && content.include?(']') && content.include?('=')

      # Try to detect XML
      return 'xml' if content.start_with?('<') && content.include?('>')

      # Default to text
      'text'
    rescue
      'unknown'
    end

    # Create Shell data with metadata
    def self.create_shell_data(data, id, metadata = nil)
      {
        version: VERSION,
        id: id,
        data: data,
        metadata: metadata || {}
      }
    end
  end
end 