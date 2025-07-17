# frozen_string_literal: true

require 'spec_helper'
require 'tempfile'
require 'tmpdir'
require_relative '../lib/peanut_config'

RSpec.describe PeanutConfig do
  let(:config) { described_class.new }

  describe '#initialize' do
    it 'sets default options' do
      expect(config.auto_compile).to be true
      expect(config.watch).to be true
    end

    it 'accepts custom options' do
      custom_config = described_class.new(auto_compile: false, watch: false)
      expect(custom_config.auto_compile).to be false
      expect(custom_config.watch).to be false
    end
  end

  describe '#parse_text_config' do
    it 'parses basic configuration' do
      content = <<~CONFIG
        [server]
        host: "localhost"
        port: 8080
        enabled: true

        [database]
        connections: 10
        timeout: 30.5
      CONFIG

      result = config.parse_text_config(content)

      expect(result).to eq({
        'server' => {
          'host' => 'localhost',
          'port' => 8080,
          'enabled' => true
        },
        'database' => {
          'connections' => 10,
          'timeout' => 30.5
        }
      })
    end

    it 'skips comments and empty lines' do
      content = <<~CONFIG
        # This is a comment
        [server]
        host: "localhost"

        # Another comment
        port: 8080
      CONFIG

      result = config.parse_text_config(content)
      expect(result['server']).to eq({
        'host' => 'localhost',
        'port' => 8080
      })
    end
  end

  describe '#parse_value' do
    it 'parses different value types' do
      expect(config.parse_value('"string value"')).to eq('string value')
      expect(config.parse_value("'single quoted'")).to eq('single quoted')
      expect(config.parse_value('true')).to be true
      expect(config.parse_value('false')).to be false
      expect(config.parse_value('null')).to be_nil
      expect(config.parse_value('42')).to eq(42)
      expect(config.parse_value('3.14')).to eq(3.14)
      expect(config.parse_value('one,two,three')).to eq(['one', 'two', 'three'])
      expect(config.parse_value('1,2,3')).to eq([1, 2, 3])
    end
  end

  describe '#compile_to_binary and #load_binary' do
    it 'performs binary roundtrip correctly' do
      test_config = {
        'string_key' => 'test value',
        'number_key' => 42,
        'bool_key' => true,
        'section' => {
          'nested_key' => 'nested value',
          'nested_num' => 3.14
        }
      }

      Dir.mktmpdir do |tmpdir|
        binary_path = File.join(tmpdir, 'test.pnt')
        
        config.compile_to_binary(test_config, binary_path)
        expect(File).to exist(binary_path)

        # Check that shell file was also created
        shell_path = File.join(tmpdir, 'test.shell')
        expect(File).to exist(shell_path)

        loaded = config.load_binary(binary_path)
        expect(loaded).to eq(test_config)
      end
    end

    it 'validates binary file format' do
      Dir.mktmpdir do |tmpdir|
        invalid_path = File.join(tmpdir, 'invalid.pnt')
        File.write(invalid_path, 'invalid content')

        expect { config.load_binary(invalid_path) }.to raise_error(/Invalid peanut binary file/)
      end
    end
  end

  describe '#find_config_hierarchy' do
    it 'finds configuration files in hierarchy' do
      Dir.mktmpdir do |tmpdir|
        # Create nested directory structure
        project_dir = File.join(tmpdir, 'project')
        config_dir = File.join(project_dir, 'config')
        FileUtils.mkdir_p(config_dir)

        # Create config files
        root_config = File.join(tmpdir, 'peanu.tsk')
        project_config = File.join(project_dir, 'peanu.peanuts')
        config_config = File.join(config_dir, 'peanu.pnt')

        File.write(root_config, 'root: true')
        File.write(project_config, '[project]\nname: test')
        File.write(config_config, 'PNUT')  # Dummy binary file

        hierarchy = config.find_config_hierarchy(config_dir)

        expect(hierarchy.length).to eq(3)
        expect(hierarchy.map(&:path)).to eq([root_config, project_config, config_config])
        expect(hierarchy.map(&:type)).to eq([:tsk, :text, :binary])
      end
    end

    it 'handles missing config files gracefully' do
      Dir.mktmpdir do |tmpdir|
        hierarchy = config.find_config_hierarchy(tmpdir)
        expect(hierarchy).to be_empty
      end
    end
  end

  describe '#deep_merge' do
    it 'merges configurations with CSS-like cascading' do
      target = {
        'key1' => 'value1',
        'section1' => {
          'nested1' => 'original',
          'nested2' => 'keep_this'
        }
      }

      source = {
        'key2' => 'value2',
        'section1' => {
          'nested1' => 'overridden',
          'nested3' => 'new_value'
        },
        'section2' => {
          'new_section' => true
        }
      }

      result = config.deep_merge(target, source)

      expect(result).to eq({
        'key1' => 'value1',
        'key2' => 'value2',
        'section1' => {
          'nested1' => 'overridden',
          'nested2' => 'keep_this',
          'nested3' => 'new_value'
        },
        'section2' => {
          'new_section' => true
        }
      })
    end
  end

  describe '#load' do
    it 'loads and merges configuration hierarchy' do
      Dir.mktmpdir do |tmpdir|
        # Create config files
        root_config = File.join(tmpdir, 'peanu.tsk')
        File.write(root_config, <<~CONFIG)
          [server]
          host: "localhost"
          port: 8080
        CONFIG

        subdir = File.join(tmpdir, 'project')
        FileUtils.mkdir_p(subdir)
        project_config = File.join(subdir, 'peanu.peanuts')
        File.write(project_config, <<~CONFIG)
          [server]
          port: 9000
          debug: true

          [database]
          host: "db.example.com"
        CONFIG

        result = config.load(subdir)

        expect(result).to eq({
          'server' => {
            'host' => 'localhost',  # From root config
            'port' => 9000,         # Overridden by project config
            'debug' => true         # From project config
          },
          'database' => {
            'host' => 'db.example.com'
          }
        })
      end
    end

    it 'caches loaded configurations' do
      Dir.mktmpdir do |tmpdir|
        config_file = File.join(tmpdir, 'peanu.tsk')
        File.write(config_file, 'key: value')

        # First load
        result1 = config.load(tmpdir)
        
        # Modify file
        File.write(config_file, 'key: modified')
        
        # Second load should return cached result
        result2 = config.load(tmpdir)
        expect(result1).to eq(result2)
        expect(result1['key']).to eq('value')  # Not modified
      end
    end
  end

  describe '#get' do
    it 'retrieves nested configuration values' do
      Dir.mktmpdir do |tmpdir|
        config_file = File.join(tmpdir, 'peanu.peanuts')
        File.write(config_file, <<~CONFIG)
          [server]
          host: "localhost"
          port: 8080

          [database]
          host: "db.example.com"
          pool_size: 10
        CONFIG

        expect(config.get('server.host', 'default', tmpdir)).to eq('localhost')
        expect(config.get('server.port', 0, tmpdir)).to eq(8080)
        expect(config.get('database.pool_size', 0, tmpdir)).to eq(10)
        expect(config.get('nonexistent.key', 'default', tmpdir)).to eq('default')
      end
    end
  end

  describe '#clear_cache' do
    it 'clears the configuration cache' do
      Dir.mktmpdir do |tmpdir|
        config_file = File.join(tmpdir, 'peanu.tsk')
        File.write(config_file, 'key: value')

        # Load and cache
        config.load(tmpdir)
        
        # Modify file
        File.write(config_file, 'key: modified')
        
        # Clear cache and reload
        config.clear_cache
        result = config.load(tmpdir)
        
        expect(result['key']).to eq('modified')
      end
    end
  end

  describe '.benchmark' do
    it 'runs performance benchmark' do
      expect { described_class.benchmark }.to output(/faster than text parsing/).to_stdout
    end

    it 'returns benchmark results' do
      result = described_class.benchmark
      
      expect(result).to have_key(:text_time)
      expect(result).to have_key(:binary_time)
      expect(result).to have_key(:improvement)
      expect(result[:improvement]).to be > 0
    end
  end
end

RSpec.describe PeanutConfig::CLI do
  describe '.run' do
    it 'shows usage when no arguments provided' do
      expect { described_class.run([]) }.to output(/PeanutConfig - TuskLang/).to_stdout
    end

    it 'handles unknown commands' do
      expect { described_class.run(['unknown']) }.to output(/Unknown command/).to_stdout.and raise_error(SystemExit)
    end
  end

  describe '.compile_file' do
    it 'compiles configuration file' do
      Dir.mktmpdir do |tmpdir|
        input_file = File.join(tmpdir, 'test.peanuts')
        File.write(input_file, <<~CONFIG)
          [server]
          host: "localhost"
          port: 8080
        CONFIG

        config = PeanutConfig.new
        expect { described_class.compile_file(config, input_file) }.to output(/Compiled to/).to_stdout

        output_file = File.join(tmpdir, 'test.pnt')
        expect(File).to exist(output_file)
      end
    end

    it 'handles missing input file' do
      config = PeanutConfig.new
      expect { described_class.compile_file(config, 'nonexistent.peanuts') }
        .to output(/File.*not found/).to_stdout.and raise_error(SystemExit)
    end
  end

  describe '.load_config' do
    it 'loads and displays configuration' do
      Dir.mktmpdir do |tmpdir|
        config_file = File.join(tmpdir, 'peanu.tsk')
        File.write(config_file, 'key: "value"')

        expect { described_class.load_config(PeanutConfig.new, tmpdir) }
          .to output(/"key": "value"/).to_stdout
      end
    end
  end

  describe '.show_hierarchy' do
    it 'displays configuration hierarchy' do
      Dir.mktmpdir do |tmpdir|
        config_file = File.join(tmpdir, 'peanu.tsk')
        File.write(config_file, 'key: value')

        expect { described_class.show_hierarchy(PeanutConfig.new, tmpdir) }
          .to output(/Configuration hierarchy/).to_stdout
      end
    end
  end
end