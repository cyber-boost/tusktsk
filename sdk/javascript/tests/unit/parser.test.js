#!/usr/bin/env node
/**
 * Unit Tests for TuskLang Parser
 * ===============================
 * Comprehensive test coverage for parser functionality
 */

const TuskLang = require('../../tsk.js');
const TuskLangEnhanced = require('../../tsk-enhanced.js');

describe('TuskLang Parser Unit Tests', () => {
  let tusk;
  let tuskEnhanced;

  beforeEach(() => {
    tusk = new TuskLang();
    tuskEnhanced = new TuskLangEnhanced();
  });

  describe('Basic Parsing', () => {
    test('should parse simple key-value pairs', () => {
      const config = tusk.parse(`
        name: "TestApp"
        version: "1.0.0"
        debug: true
        port: 8080
      `);

      expect(config.name).toBe('TestApp');
      expect(config.version).toBe('1.0.0');
      expect(config.debug).toBe(true);
      expect(config.port).toBe(8080);
    });

    test('should parse different data types', () => {
      const config = tusk.parse(`
        string_value: "hello world"
        number_value: 42
        float_value: 3.14
        boolean_true: true
        boolean_false: false
        null_value: null
        array_value: [1, 2, 3, "test"]
        object_value: {
          nested: "value"
          number: 123
        }
      `);

      expect(config.string_value).toBe('hello world');
      expect(config.number_value).toBe(42);
      expect(config.float_value).toBe(3.14);
      expect(config.boolean_true).toBe(true);
      expect(config.boolean_false).toBe(false);
      expect(config.null_value).toBe(null);
      expect(config.array_value).toEqual([1, 2, 3, 'test']);
      expect(config.object_value).toEqual({
        nested: 'value',
        number: 123
      });
    });

    test('should handle empty configuration', () => {
      const config = tusk.parse('');
      expect(config).toEqual({});
    });

    test('should handle whitespace and comments', () => {
      const config = tusk.parse(`
        # This is a comment
        name: "TestApp"  # Inline comment
        
        # Another comment
        version: "1.0.0"
      `);

      expect(config.name).toBe('TestApp');
      expect(config.version).toBe('1.0.0');
    });
  });

  describe('Section Parsing', () => {
    test('should parse bracket sections', () => {
      const config = tusk.parse(`
        [database]
        host: "localhost"
        port: 5432
        
        [server]
        host: "0.0.0.0"
        port: 8080
      `);

      expect(config.database.host).toBe('localhost');
      expect(config.database.port).toBe(5432);
      expect(config.server.host).toBe('0.0.0.0');
      expect(config.server.port).toBe(8080);
    });

    test('should parse brace sections', () => {
      const config = tusk.parse(`
        database {
          host: "localhost"
          port: 5432
        }
        
        server {
          host: "0.0.0.0"
          port: 8080
        }
      `);

      expect(config.database.host).toBe('localhost');
      expect(config.database.port).toBe(5432);
      expect(config.server.host).toBe('0.0.0.0');
      expect(config.server.port).toBe(8080);
    });

    test('should parse angle bracket sections', () => {
      const config = tusk.parse(`
        database >
          host: "localhost"
          port: 5432
        <
        
        server >
          host: "0.0.0.0"
          port: 8080
        <
      `);

      expect(config.database.host).toBe('localhost');
      expect(config.database.port).toBe(5432);
      expect(config.server.host).toBe('0.0.0.0');
      expect(config.server.port).toBe(8080);
    });

    test('should parse mixed syntax styles', () => {
      const config = tusk.parse(`
        [database]
        host: "localhost"
        
        server {
          port: 8080
        }
        
        cache >
          ttl: 300
        <
      `);

      expect(config.database.host).toBe('localhost');
      expect(config.server.port).toBe(8080);
      expect(config.cache.ttl).toBe(300);
    });

    test('should handle nested sections', () => {
      const config = tusk.parse(`
        [app]
        name: "TestApp"
        
        [app.database]
        host: "localhost"
        
        [app.database.pool]
        max: 10
        min: 2
      `);

      expect(config.app.name).toBe('TestApp');
      expect(config.app.database.host).toBe('localhost');
      expect(config.app.database.pool.max).toBe(10);
      expect(config.app.database.pool.min).toBe(2);
    });
  });

  describe('Enhanced Features', () => {
    test('should parse variables', () => {
      const config = tuskEnhanced.parse(`
        $app_name: "TestApp"
        $port: 8080
        
        name: $app_name
        server_port: $port
      `);

      expect(config.name).toBe('TestApp');
      expect(config.server_port).toBe(8080);
    });

    test('should handle variable scoping', () => {
      const config = tuskEnhanced.parse(`
        $global_var: "global"
        
        [section1]
        $local_var: "local1"
        value1: $local_var
        global_value: $global_var
        
        [section2]
        $local_var: "local2"
        value2: $local_var
        global_value: $global_var
      `);

      expect(config.section1.value1).toBe('local1');
      expect(config.section1.global_value).toBe('global');
      expect(config.section2.value2).toBe('local2');
      expect(config.section2.global_value).toBe('global');
    });

    test('should parse environment variables', () => {
      process.env.TEST_VAR = 'test_value';
      process.env.TEST_PORT = '3000';

      const config = tuskEnhanced.parse(`
        env_var: @env("TEST_VAR")
        port: @env("TEST_PORT", "8080")
        missing: @env("MISSING_VAR", "default")
      `);

      expect(config.env_var).toBe('test_value');
      expect(config.port).toBe('3000');
      expect(config.missing).toBe('default');
    });

    test('should parse date functions', () => {
      const config = tuskEnhanced.parse(`
        timestamp: @date("Y-m-d H:i:s")
        year: @date("Y")
        month: @date("m")
        iso_date: @date("c")
        unix_time: @date("U")
      `);

      expect(config.timestamp).toMatch(/^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$/);
      expect(config.year).toMatch(/^\d{4}$/);
      expect(config.month).toMatch(/^\d{2}$/);
      expect(config.iso_date).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
      expect(config.unix_time).toMatch(/^\d+$/);
    });

    test('should parse ranges', () => {
      const config = tuskEnhanced.parse(`
        port_range: 8000-9000
        worker_range: 1-10
        version_range: "1.0-2.0"
      `);

      expect(config.port_range).toEqual({ start: 8000, end: 9000 });
      expect(config.worker_range).toEqual({ start: 1, end: 10 });
      expect(config.version_range).toEqual({ start: '1.0', end: '2.0' });
    });

    test('should parse conditional expressions', () => {
      const config = tuskEnhanced.parse(`
        $env: "production"
        
        debug: $env == "development" ? true : false
        workers: $env == "production" ? 8 : 2
        log_level: $env == "production" ? "error" : "debug"
      `);

      expect(config.debug).toBe(false);
      expect(config.workers).toBe(8);
      expect(config.log_level).toBe('error');
    });

    test('should parse string concatenation', () => {
      const config = tuskEnhanced.parse(`
        $base: "TuskLang"
        $version: "2.0"
        
        full_name: $base + " v" + $version
        api_url: "https://api.example.com/" + $base
      `);

      expect(config.full_name).toBe('TuskLang v2.0');
      expect(config.api_url).toBe('https://api.example.com/TuskLang');
    });
  });

  describe('Database Integration', () => {
    test('should execute database queries', async () => {
      const mockAdapter = {
        async query(sql, params = []) {
          if (sql.includes('COUNT')) {
            return [{ count: 42 }];
          }
          if (sql.includes('rate_limit')) {
            return [{ rate_limit: 1000 }];
          }
          return [];
        }
      };

      tuskEnhanced.setDatabaseAdapter(mockAdapter);

      const config = await tuskEnhanced.parse(`
        [database]
        user_count: @query("SELECT COUNT(*) as count FROM users")
        rate_limit: @query("SELECT rate_limit FROM plans WHERE id = ?", 1)
      `);

      expect(config.database.user_count).toBe(42);
      expect(config.database.rate_limit).toBe(1000);
    });

    test('should handle database errors gracefully', async () => {
      const mockAdapter = {
        async query(sql, params = []) {
          throw new Error('Database connection failed');
        }
      };

      tuskEnhanced.setDatabaseAdapter(mockAdapter);

      await expect(tuskEnhanced.parse(`
        [database]
        user_count: @query("SELECT COUNT(*) FROM users")
      `)).rejects.toThrow('Database connection failed');
    });
  });

  describe('Error Handling', () => {
    test('should throw error for invalid syntax', () => {
      expect(() => {
        tusk.parse(`
          name: "TestApp"
          invalid syntax here
        `);
      }).toThrow();
    });

    test('should throw error for unclosed sections', () => {
      expect(() => {
        tusk.parse(`
          [database]
          host: "localhost"
          # Missing closing bracket
        `);
      }).toThrow();
    });

    test('should throw error for undefined variables', () => {
      expect(() => {
        tuskEnhanced.parse(`
          value: $undefined_variable
        `);
      }).toThrow();
    });

    test('should throw error for invalid date format', () => {
      expect(() => {
        tuskEnhanced.parse(`
          date: @date("invalid_format")
        `);
      }).toThrow();
    });

    test('should throw error for invalid range format', () => {
      expect(() => {
        tuskEnhanced.parse(`
          range: invalid-range
        `);
      }).toThrow();
    });
  });

  describe('Validation', () => {
    test('should validate configuration against schema', () => {
      const schema = {
        required: ['name', 'version'],
        properties: {
          name: { type: 'string' },
          version: { type: 'string' },
          port: { type: 'number', minimum: 1, maximum: 65535 }
        }
      };

      const config = {
        name: 'TestApp',
        version: '1.0.0',
        port: 8080
      };

      const errors = tusk.validate(config, schema);
      expect(errors).toHaveLength(0);
    });

    test('should return validation errors for invalid config', () => {
      const schema = {
        required: ['name', 'version'],
        properties: {
          name: { type: 'string' },
          version: { type: 'string' },
          port: { type: 'number', minimum: 1, maximum: 65535 }
        }
      };

      const config = {
        name: 'TestApp',
        // Missing required version
        port: 99999 // Invalid port
      };

      const errors = tusk.validate(config, schema);
      expect(errors.length).toBeGreaterThan(0);
      expect(errors.some(e => e.path === 'version')).toBe(true);
      expect(errors.some(e => e.path === 'port')).toBe(true);
    });
  });

  describe('Stringification', () => {
    test('should convert config object back to TuskLang format', () => {
      const originalConfig = {
        name: 'TestApp',
        version: '1.0.0',
        database: {
          host: 'localhost',
          port: 5432
        },
        server: {
          host: '0.0.0.0',
          port: 8080
        }
      };

      const tskString = tusk.stringify(originalConfig, {
        format: 'bracket',
        indent: 2
      });

      const parsedConfig = tusk.parse(tskString);
      expect(parsedConfig).toEqual(originalConfig);
    });

    test('should handle different output formats', () => {
      const config = {
        name: 'TestApp',
        database: {
          host: 'localhost'
        }
      };

      const bracketFormat = tusk.stringify(config, { format: 'bracket' });
      const braceFormat = tusk.stringify(config, { format: 'brace' });
      const angleFormat = tusk.stringify(config, { format: 'angle' });

      expect(bracketFormat).toContain('[database]');
      expect(braceFormat).toContain('database {');
      expect(angleFormat).toContain('database >');
    });
  });

  describe('Performance', () => {
    test('should parse large configuration efficiently', () => {
      const largeConfig = generateLargeConfig(1000);
      
      const start = process.hrtime.bigint();
      const config = tusk.parse(largeConfig);
      const end = process.hrtime.bigint();
      
      const duration = Number(end - start) / 1000000; // milliseconds
      expect(duration).toBeLessThan(100); // Should parse in under 100ms
      expect(Object.keys(config).length).toBeGreaterThan(100);
    });

    test('should handle deep nesting efficiently', () => {
      const deepConfig = generateDeepConfig(20);
      
      const start = process.hrtime.bigint();
      const config = tusk.parse(deepConfig);
      const end = process.hrtime.bigint();
      
      const duration = Number(end - start) / 1000000;
      expect(duration).toBeLessThan(50); // Should parse in under 50ms
    });
  });

  describe('Edge Cases', () => {
    test('should handle special characters in strings', () => {
      const config = tusk.parse(`
        special_string: "Hello\nWorld\tWith\"Quotes\"And\\Backslashes"
        unicode_string: "Hello 世界 🌍"
        empty_string: ""
      `);

      expect(config.special_string).toBe('Hello\nWorld\tWith"Quotes"And\\Backslashes');
      expect(config.unicode_string).toBe('Hello 世界 🌍');
      expect(config.empty_string).toBe('');
    });

    test('should handle very large numbers', () => {
      const config = tusk.parse(`
        large_number: 999999999999999999
        negative_number: -123456789
        zero: 0
      `);

      expect(config.large_number).toBe(999999999999999999);
      expect(config.negative_number).toBe(-123456789);
      expect(config.zero).toBe(0);
    });

    test('should handle empty sections', () => {
      const config = tusk.parse(`
        [empty_section]
        
        [section_with_values]
        key: "value"
      `);

      expect(config.empty_section).toEqual({});
      expect(config.section_with_values.key).toBe('value');
    });

    test('should handle duplicate keys (last wins)', () => {
      const config = tusk.parse(`
        name: "First"
        name: "Second"
        name: "Third"
      `);

      expect(config.name).toBe('Third');
    });
  });
});

// Helper functions for generating test data
function generateLargeConfig(count) {
  let config = '';
  for (let i = 0; i < count; i++) {
    config += `key_${i}: "value_${i}"\n`;
  }
  return config;
}

function generateDeepConfig(depth) {
  let config = '';
  let currentDepth = '';
  
  for (let i = 0; i < depth; i++) {
    currentDepth += `level_${i} {\n`;
    config += currentDepth + `  value: ${i}\n`;
  }
  
  for (let i = 0; i < depth; i++) {
    config += '}\n';
  }
  
  return config;
}

// Export for use in other test files
module.exports = {
  TuskLang,
  TuskLangEnhanced,
  generateLargeConfig,
  generateDeepConfig
}; 