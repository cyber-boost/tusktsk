# frozen_string_literal: true

# TSK Parser for Ruby
# Handles TOML-like TSK format parsing and generation

module TuskLang
  # Parser for TSK (TuskLang Configuration) format
  class TSKParser
    # Parse TSK content into hash
    def self.parse(content)
      data, _ = parse_with_comments(content)
      data
    end

    # Parse TSK content with comments preserved
    def self.parse_with_comments(content)
      lines = content.split("\n")
      result = {}
      comments = {}
      current_section = nil
      in_multiline_string = false
      multiline_key = nil
      multiline_content = []

      lines.each_with_index do |line, i|
        trimmed_line = line.strip

        # Handle multiline strings
        if in_multiline_string
          if trimmed_line == '"""'
            if current_section && multiline_key
              result[current_section][multiline_key] = multiline_content.join("\n")
            end
            in_multiline_string = false
            multiline_key = nil
            multiline_content = []
            next
          end
          multiline_content << line
          next
        end

        # Capture comments
        if trimmed_line.start_with?('#')
          comments[i] = trimmed_line
          next
        end

        # Skip empty lines
        next if trimmed_line.empty?

        # Section header
        section_match = trimmed_line.match(/^\[(.+)\]$/)
        if section_match
          current_section = section_match[1]
          result[current_section] = {}
          next
        end

        # Key-value pair
        if current_section && trimmed_line.include?('=')
          separator_index = trimmed_line.index('=')
          key = trimmed_line[0...separator_index].strip
          value_str = trimmed_line[(separator_index + 1)..-1].strip

          # Check for multiline string start
          if value_str == '"""'
            in_multiline_string = true
            multiline_key = key
            next
          end

          value = parse_value(value_str)
          result[current_section][key] = value
        end
      end

      [result, comments]
    end

    # Parse a TSK value string into appropriate Ruby type
    private_class_method def self.parse_value(value_str)
      # Null
      return nil if value_str == 'null'

      # Boolean
      return true if value_str == 'true'
      return false if value_str == 'false'

      # Number
      return value_str.to_i if value_str.match?(/^-?\d+$/)
      return value_str.to_f if value_str.match?(/^-?\d+\.\d+$/)

      # String
      if value_str.start_with?('"') && value_str.end_with?('"')
        return value_str[1...-1].gsub('\\"', '"').gsub('\\\\', '\\')
      end

      # Array
      if value_str.start_with?('[') && value_str.end_with?(']')
        array_content = value_str[1...-1].strip
        return [] if array_content.empty?

        items = split_array_items(array_content)
        return items.map { |item| parse_value(item.strip) }
      end

      # Object/Hash
      if value_str.start_with?('{') && value_str.end_with?('}')
        obj_content = value_str[1...-1].strip
        return {} if obj_content.empty?

        pairs = split_object_pairs(obj_content)
        obj = {}

        pairs.each do |pair|
          if pair.include?('=')
            eq_index = pair.index('=')
            key = pair[0...eq_index].strip
            value = pair[(eq_index + 1)..-1].strip
            # Remove quotes from key if present
            clean_key = key.start_with?('"') && key.end_with?('"') ? key[1...-1] : key
            obj[clean_key] = parse_value(value)
          end
        end

        return obj
      end

      # Return as string if no other type matches
      value_str
    end

    # Split array items considering nested structures
    private_class_method def self.split_array_items(content)
      items = []
      current = ''
      depth = 0
      in_string = false

      content.each_char.with_index do |ch, i|
        if ch == '"' && (i == 0 || content[i - 1] != '\\')
          in_string = !in_string
        end

        unless in_string
          depth += 1 if ch == '[' || ch == '{'
          depth -= 1 if ch == ']' || ch == '}'

          if ch == ',' && depth == 0
            items << current.strip
            current = ''
            next
          end
        end

        current += ch
      end

      items << current.strip unless current.strip.empty?
      items
    end

    # Split object pairs considering nested structures
    private_class_method def self.split_object_pairs(content)
      pairs = []
      current = ''
      depth = 0
      in_string = false

      content.each_char.with_index do |ch, i|
        if ch == '"' && (i == 0 || content[i - 1] != '\\')
          in_string = !in_string
        end

        unless in_string
          depth += 1 if ch == '[' || ch == '{'
          depth -= 1 if ch == ']' || ch == '}'

          if ch == ',' && depth == 0
            pairs << current.strip
            current = ''
            next
          end
        end

        current += ch
      end

      pairs << current.strip unless current.strip.empty?
      pairs
    end

    # Convert hash back to TSK string format
    def self.stringify(data)
      result = []

      data.each do |section, section_data|
        result << "[#{section}]"

        if section_data.is_a?(Hash)
          section_data.each do |key, value|
            result << "#{key} = #{format_value(value)}"
          end
        else
          result << "value = #{format_value(section_data)}"
        end

        result << ''
      end

      result.join("\n").strip
    end

    # Format a value for TSK string representation
    private_class_method def self.format_value(value)
      case value
      when nil then 'null'
      when true then 'true'
      when false then 'false'
      when String then "\"#{value.gsub('"', '\\"').gsub('\\', '\\\\')}\""
      when Numeric then value.to_s
      when Hash
        pairs = value.map { |k, v| "\"#{k}\" = #{format_value(v)}" }
        "{#{pairs.join(', ')}}"
      when Array
        items = value.map { |v| format_value(v) }
        "[#{items.join(', ')}]"
      else
        "\"#{value}\""
      end
    end
  end
end 