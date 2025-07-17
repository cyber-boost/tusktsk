# frozen_string_literal: true

require 'spec_helper'
require 'tempfile'
require 'tmpdir'
require_relative '../../cli/main'

RSpec.describe TuskLang::CLI do
  let(:cli) { described_class.new }
  let(:temp_dir) { Dir.mktmpdir }

  after do
    FileUtils.remove_entry(temp_dir) if Dir.exist?(temp_dir)
  end

  describe '.run' do
    it 'shows help when no arguments provided' do
      expect { described_class.run([]) }.to output(/TuskLang Ruby SDK CLI/).to_stdout
    end

    it 'shows version with --version flag' do
      expect { described_class.run(['--version']) }.to output(/TuskLang Ruby SDK v/).to_stdout
    end

    it 'shows help with --help flag' do
      expect { described_class.run(['--help']) }.to output(/Usage: tsk/).to_stdout
    end
  end

  describe 'database commands' do
    before do
      Dir.chdir(temp_dir)
    end

    it 'initializes database' do
      expect { described_class.run(['db', 'init']) }.to output(/Database initialized/).to_stdout
      expect(File.exist?('tusklang.db')).to be true
    end

    it 'shows database status' do
      # Create a test database first
      db = SQLite3::Database.new('tusklang.db')
      db.execute('CREATE TABLE test (id INTEGER)')
      db.close

      expect { described_class.run(['db', 'status']) }.to output(/Database connected/).to_stdout
    end

    it 'runs migrations' do
      # Create test database
      db = SQLite3::Database.new('tusklang.db')
      db.close

      # Create migration file
      migration_file = File.join(temp_dir, 'test_migration.sql')
      File.write(migration_file, 'CREATE TABLE test (id INTEGER PRIMARY KEY);')

      expect { described_class.run(['db', 'migrate', migration_file]) }.to output(/Migration completed/).to_stdout
    end

    it 'creates database backup' do
      # Create test database with data
      db = SQLite3::Database.new('tusklang.db')
      db.execute('CREATE TABLE test (id INTEGER)')
      db.execute('INSERT INTO test VALUES (1)')
      db.close

      expect { described_class.run(['db', 'backup']) }.to output(/Database backed up/).to_stdout
      expect(Dir.glob('tusklang_backup_*.sql')).not_to be_empty
    end
  end

  describe 'development commands' do
    it 'compiles TSK files' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, <<~TSK)
        [app]
        name = "Test App"
        version = "1.0.0"
      TSK

      expect { described_class.run(['compile', tsk_file]) }.to output(/Compiled and optimized/).to_stdout
      expect(File.exist?(tsk_file.sub('.tsk', '_optimized.tsk'))).to be true
    end

    it 'optimizes TSK files' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, <<~TSK)
        [app]
        name = "Test App"
        version = "1.0.0"
      TSK

      expect { described_class.run(['optimize', tsk_file]) }.to output(/Optimized/).to_stdout
      expect(File.exist?(tsk_file.sub('.tsk', '_optimized.tsk'))).to be true
    end
  end

  describe 'testing commands' do
    it 'runs all tests' do
      expect { described_class.run(['test', 'all']) }.to output(/All tests completed/).to_stdout
    end

    it 'runs parser tests' do
      expect { described_class.run(['test', 'parser']) }.to output(/Parser tests passed/).to_stdout
    end

    it 'runs FUJSEN tests' do
      expect { described_class.run(['test', 'fujsen']) }.to output(/FUJSEN tests passed/).to_stdout
    end

    it 'runs SDK tests' do
      expect { described_class.run(['test', 'sdk']) }.to output(/SDK tests passed/).to_stdout
    end

    it 'runs performance tests' do
      expect { described_class.run(['test', 'performance']) }.to output(/Performance test completed/).to_stdout
    end
  end

  describe 'service commands' do
    it 'shows service status' do
      expect { described_class.run(['services', 'status']) }.to output(/Service Status/).to_stdout
    end

    it 'starts services' do
      expect { described_class.run(['services', 'start']) }.to output(/Services started/).to_stdout
    end

    it 'stops services' do
      expect { described_class.run(['services', 'stop']) }.to output(/Services stopped/).to_stdout
    end

    it 'restarts services' do
      expect { described_class.run(['services', 'restart']) }.to output(/Services restarted/).to_stdout
    end
  end

  describe 'cache commands' do
    it 'clears cache' do
      expect { described_class.run(['cache', 'clear']) }.to output(/Cache cleared/).to_stdout
    end

    it 'shows cache status' do
      expect { described_class.run(['cache', 'status']) }.to output(/Cache Status/).to_stdout
    end

    it 'warms cache' do
      expect { described_class.run(['cache', 'warm']) }.to output(/Cache warmed/).to_stdout
    end

    it 'shows distributed cache status' do
      expect { described_class.run(['cache', 'distributed']) }.to output(/Distributed Cache Status/).to_stdout
    end
  end

  describe 'memcached commands' do
    it 'shows memcached status' do
      expect { described_class.run(['cache', 'memcached', 'status']) }.to output(/Memcached Status/).to_stdout
    end

    it 'shows memcached stats' do
      expect { described_class.run(['cache', 'memcached', 'stats']) }.to output(/Memcached Statistics/).to_stdout
    end

    it 'flushes memcached' do
      expect { described_class.run(['cache', 'memcached', 'flush']) }.to output(/Memcached flushed/).to_stdout
    end

    it 'restarts memcached' do
      expect { described_class.run(['cache', 'memcached', 'restart']) }.to output(/Memcached restarted/).to_stdout
    end

    it 'tests memcached connection' do
      expect { described_class.run(['cache', 'memcached', 'test']) }.to output(/Memcached connection test passed/).to_stdout
    end
  end

  describe 'configuration commands' do
    before do
      Dir.chdir(temp_dir)
      
      # Create test configuration files
      File.write('peanu.tsk', <<~TSK)
        [server]
        host = "localhost"
        port = 8080
      TSK
    end

    it 'gets configuration values' do
      expect { described_class.run(['config', 'get', 'server.host']) }.to output(/localhost/).to_stdout
    end

    it 'checks configuration hierarchy' do
      expect { described_class.run(['config', 'check']) }.to output(/Configuration hierarchy check completed/).to_stdout
    end

    it 'validates configuration' do
      expect { described_class.run(['config', 'validate']) }.to output(/Configuration is valid/).to_stdout
    end

    it 'compiles configuration' do
      expect { described_class.run(['config', 'compile']) }.to output(/Configuration compiled/).to_stdout
      expect(File.exist?('peanu.pnt')).to be true
    end

    it 'generates configuration documentation' do
      expect { described_class.run(['config', 'docs']) }.to output(/Documentation generated/).to_stdout
    end

    it 'clears configuration cache' do
      expect { described_class.run(['config', 'clear-cache']) }.to output(/Configuration cache cleared/).to_stdout
    end

    it 'shows configuration statistics' do
      expect { described_class.run(['config', 'stats']) }.to output(/Configuration Statistics/).to_stdout
    end
  end

  describe 'binary commands' do
    it 'compiles to binary format' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, <<~TSK)
        [app]
        name = "Test App"
        version = "1.0.0"
      TSK

      expect { described_class.run(['binary', 'compile', tsk_file]) }.to output(/Compiled to binary/).to_stdout
      expect(File.exist?(tsk_file.sub('.tsk', '.tskb'))).to be true
    end

    it 'executes binary files' do
      # Create a test binary file
      binary_file = File.join(temp_dir, 'test.tskb')
      test_data = { 'test' => { 'key' => 'value' } }
      File.binwrite(binary_file, Marshal.dump(test_data))

      expect { described_class.run(['binary', 'execute', binary_file]) }.to output(/Binary executed successfully/).to_stdout
    end

    it 'benchmarks binary vs text performance' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, <<~TSK)
        [app]
        name = "Test App"
        version = "1.0.0"
      TSK

      expect { described_class.run(['binary', 'benchmark', tsk_file]) }.to output(/Performance test completed/).to_stdout
    end

    it 'optimizes binary files' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, <<~TSK)
        [app]
        name = "Test App"
        version = "1.0.0"
      TSK

      expect { described_class.run(['binary', 'optimize', tsk_file]) }.to output(/Binary optimized/).to_stdout
      expect(File.exist?(tsk_file.sub('.tsk', '_optimized.tskb'))).to be true
    end
  end

  describe 'peanuts commands' do
    it 'compiles peanuts files' do
      peanuts_file = File.join(temp_dir, 'test.peanuts')
      File.write(peanuts_file, <<~PEANUTS)
        [server]
        host = "localhost"
        port = 8080
      PEANUTS

      expect { described_class.run(['peanuts', 'compile', peanuts_file]) }.to output(/Peanuts compiled to binary/).to_stdout
    end

    it 'auto-compiles peanuts files' do
      peanuts_file = File.join(temp_dir, 'test.peanuts')
      File.write(peanuts_file, <<~PEANUTS)
        [server]
        host = "localhost"
        port = 8080
      PEANUTS

      expect { described_class.run(['peanuts', 'auto-compile']) }.to output(/Auto-compilation completed/).to_stdout
    end

    it 'loads binary peanuts files' do
      # Create a test binary peanuts file
      binary_file = File.join(temp_dir, 'test.pnt')
      test_data = { 'test' => { 'key' => 'value' } }
      File.binwrite(binary_file, Marshal.dump(test_data))

      expect { described_class.run(['peanuts', 'load', binary_file]) }.to output(/Peanuts binary loaded/).to_stdout
    end
  end

  describe 'CSS commands' do
    it 'expands CSS shortcodes' do
      css_file = File.join(temp_dir, 'test.css')
      File.write(css_file, 'div { mh: 100px; mw: 200px; }')

      expect { described_class.run(['css', 'expand', css_file]) }.to output(/max-height/).to_stdout
    end

    it 'shows CSS shortcode mappings' do
      expect { described_class.run(['css', 'map']) }.to output(/CSS Shortcode Mappings/).to_stdout
    end
  end

  describe 'AI commands' do
    it 'queries Claude' do
      expect { described_class.run(['ai', 'claude', 'Hello']) }.to output(/Querying Claude/).to_stdout
    end

    it 'queries ChatGPT' do
      expect { described_class.run(['ai', 'chatgpt', 'Hello']) }.to output(/Querying ChatGPT/).to_stdout
    end

    it 'queries custom AI API' do
      expect { described_class.run(['ai', 'custom', 'https://api.example.com', 'Hello']) }.to output(/Querying custom AI API/).to_stdout
    end

    it 'shows AI configuration' do
      expect { described_class.run(['ai', 'config']) }.to output(/AI Configuration/).to_stdout
    end

    it 'sets up AI interactively' do
      expect { described_class.run(['ai', 'setup']) }.to output(/Interactive AI API key setup/).to_stdout
    end

    it 'tests AI connections' do
      expect { described_class.run(['ai', 'test']) }.to output(/Testing AI connections/).to_stdout
    end

    it 'provides AI completion' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, '[app]')

      expect { described_class.run(['ai', 'complete', tsk_file]) }.to output(/Getting AI-powered auto-completion/).to_stdout
    end

    it 'analyzes files with AI' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, '[app]')

      expect { described_class.run(['ai', 'analyze', tsk_file]) }.to output(/Analyzing file for errors and improvements/).to_stdout
    end

    it 'optimizes files with AI' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, '[app]')

      expect { described_class.run(['ai', 'optimize', tsk_file]) }.to output(/Getting performance optimization suggestions/).to_stdout
    end

    it 'scans files for security with AI' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, '[app]')

      expect { described_class.run(['ai', 'security', tsk_file]) }.to output(/Scanning for security vulnerabilities/).to_stdout
    end
  end

  describe 'utility commands' do
    it 'parses TSK files' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, <<~TSK)
        [app]
        name = "Test App"
        version = "1.0.0"
      TSK

      expect { described_class.run(['parse', tsk_file]) }.to output(/\[app\]/).to_stdout
    end

    it 'validates TSK files' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, <<~TSK)
        [app]
        name = "Test App"
        version = "1.0.0"
      TSK

      expect { described_class.run(['validate', tsk_file]) }.to output(/File is valid TuskLang syntax/).to_stdout
    end

    it 'converts between formats' do
      input_file = File.join(temp_dir, 'input.tsk')
      output_file = File.join(temp_dir, 'output.tsk')
      File.write(input_file, <<~TSK)
        [app]
        name = "Test App"
        version = "1.0.0"
      TSK

      expect { described_class.run(['convert', '-i', input_file, '-o', output_file]) }.to output(/Converted to/).to_stdout
      expect(File.exist?(output_file)).to be true
    end

    it 'gets values from TSK files' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, <<~TSK)
        [app]
        name = "Test App"
        version = "1.0.0"
      TSK

      expect { described_class.run(['get', tsk_file, 'app.name']) }.to output(/Test App/).to_stdout
    end

    it 'sets values in TSK files' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, <<~TSK)
        [app]
        name = "Test App"
        version = "1.0.0"
      TSK

      expect { described_class.run(['set', tsk_file, 'app.name', 'Updated App']) }.to output(/Value set/).to_stdout
      
      # Verify the change was made
      content = File.read(tsk_file)
      expect(content).to include('Updated App')
    end
  end

  describe 'error handling' do
    it 'handles missing files gracefully' do
      expect { described_class.run(['parse', 'nonexistent.tsk']) }.to output(/File not found/).to_stdout
    end

    it 'handles invalid commands gracefully' do
      expect { described_class.run(['invalid-command']) }.to output(/Unknown command/).to_stdout
    end

    it 'handles missing arguments gracefully' do
      expect { described_class.run(['parse']) }.to output(/File required/).to_stdout
    end
  end

  describe 'global options' do
    it 'respects --verbose flag' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, '[app]')

      expect { described_class.run(['--verbose', 'parse', tsk_file]) }.to output(/\[app\]/).to_stdout
    end

    it 'respects --json flag' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, <<~TSK)
        [app]
        name = "Test App"
      TSK

      expect { described_class.run(['--json', 'parse', tsk_file]) }.to output(/"app"/).to_stdout
    end

    it 'respects --quiet flag' do
      tsk_file = File.join(temp_dir, 'test.tsk')
      File.write(tsk_file, '[app]')

      # Should not output anything with --quiet
      expect { described_class.run(['--quiet', 'parse', tsk_file]) }.not_to output.to_stdout
    end
  end
end 