#!/usr/bin/env ruby
# frozen_string_literal: true

require_relative 'lib/tusk_lang/tsk_parser_enhanced'

# Enhanced CLI for TuskLang Ruby SDK
class CLIEnhanced
  def self.run(args)
    if args.empty?
      show_help
      return
    end
    
    command = args[0]
    
    case command
    when 'parse'
      if args.length < 2
        puts 'Error: File path required'
        exit 1
      end
      
      parser = TuskLang::TSKParserEnhanced.new
      parser.parse_file(args[1])
      
      parser.keys.each do |key|
        value = parser.get(key)
        puts "#{key} = #{value}"
      end
      
    when 'get'
      if args.length < 3
        puts 'Error: File path and key required'
        exit 1
      end
      
      parser = TuskLang::TSKParserEnhanced.new
      parser.parse_file(args[1])
      
      value = parser.get(args[2])
      puts value if value
      
    when 'keys'
      if args.length < 2
        puts 'Error: File path required'
        exit 1
      end
      
      parser = TuskLang::TSKParserEnhanced.new
      parser.parse_file(args[1])
      
      parser.keys.each { |key| puts key }
      
    when 'peanut'
      parser = TuskLang::TSKParserEnhanced.load_from_peanut
      puts "Loaded #{parser.items.length} configuration items"
      
      parser.keys.each do |key|
        value = parser.get(key)
        puts "#{key} = #{value}"
      end
      
    when 'json'
      if args.length < 2
        puts 'Error: File path required'
        exit 1
      end
      
      parser = TuskLang::TSKParserEnhanced.new
      parser.parse_file(args[1])
      
      puts parser.to_json
      
    when 'validate'
      if args.length < 2
        puts 'Error: File path required'
        exit 1
      end
      
      begin
        parser = TuskLang::TSKParserEnhanced.new
        parser.parse_file(args[1])
        puts '✅ File is valid TuskLang syntax'
      rescue => e
        puts "❌ Validation failed: #{e.message}"
        exit 1
      end
      
    else
      puts "Error: Unknown command: #{command}"
      show_help
      exit 1
    end
  rescue => e
    puts "Error: #{e.message}"
    exit 1
  end
  
  def self.show_help
    puts <<~HELP
      
      TuskLang Enhanced for Ruby - The Freedom Parser
      ==============================================
      
      Usage: ruby cli_enhanced.rb [command] [options]
      
      Commands:
          parse <file>     Parse a .tsk file and show all key-value pairs
          get <file> <key> Get a specific value by key
          keys <file>      List all keys in the file
          json <file>      Convert .tsk file to JSON format
          validate <file>  Validate .tsk file syntax
          peanut           Load configuration from peanut.tsk
          
      Examples:
          ruby cli_enhanced.rb parse config.tsk
          ruby cli_enhanced.rb get config.tsk database.host
          ruby cli_enhanced.rb keys config.tsk
          ruby cli_enhanced.rb json config.tsk
          ruby cli_enhanced.rb validate config.tsk
          ruby cli_enhanced.rb peanut
      
      Features:
          - Multiple syntax styles: [], {}, <>
          - Global variables with $
          - Cross-file references: @file.tsk.get()
          - Database queries: @query()
          - Date functions: @date()
          - Environment variables: @env()
          - Conditional expressions (ternary operator)
          - Range syntax: 8000-9000
          - String concatenation with +
          - Optional semicolons
          - Ruby elegance with Rails integration ready
          - ActiveRecord configuration support
      
      Default config file: peanut.tsk
      "We don't bow to any king" - Maximum syntax flexibility
      
    HELP
  end
end

# Run CLI if this file is executed directly
CLIEnhanced.run(ARGV) if __FILE__ == $PROGRAM_NAME