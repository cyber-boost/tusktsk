<?php

namespace TuskLang\Tests;

use PHPUnit\Framework\TestCase;
use TuskLang\PeanutConfig;

class PeanutConfigTest extends TestCase
{
    private $testDir;
    private $config;
    
    protected function setUp(): void
    {
        $this->testDir = sys_get_temp_dir() . '/peanut_test_' . uniqid();
        mkdir($this->testDir);
        $this->config = PeanutConfig::getInstance();
        $this->config->invalidateCache();
    }
    
    protected function tearDown(): void
    {
        // Clean up test directory
        $this->recursiveRemoveDir($this->testDir);
    }
    
    private function recursiveRemoveDir($dir)
    {
        if (is_dir($dir)) {
            $objects = scandir($dir);
            foreach ($objects as $object) {
                if ($object != "." && $object != "..") {
                    if (is_dir($dir . "/" . $object)) {
                        $this->recursiveRemoveDir($dir . "/" . $object);
                    } else {
                        unlink($dir . "/" . $object);
                    }
                }
            }
            rmdir($dir);
        }
    }
    
    public function testParseTextConfig()
    {
        $configContent = <<<EOT
[server]
host: "localhost"
port: 8080
debug: true

[database]
driver: postgresql
host: db.example.com
pool_size: 10
EOT;
        
        file_put_contents($this->testDir . '/peanu.peanuts', $configContent);
        
        $config = $this->config->load($this->testDir);
        
        $this->assertEquals('localhost', $config['server']['host']);
        $this->assertEquals(8080, $config['server']['port']);
        $this->assertTrue($config['server']['debug']);
        $this->assertEquals('postgresql', $config['database']['driver']);
        $this->assertEquals(10, $config['database']['pool_size']);
    }
    
    public function testHierarchicalLoading()
    {
        // Create root config
        $rootConfig = <<<EOT
[app]
name: "MyApp"
version: "1.0.0"

[server]
port: 8080
EOT;
        file_put_contents($this->testDir . '/peanu.peanuts', $rootConfig);
        
        // Create subdirectory with override
        $subDir = $this->testDir . '/subdir';
        mkdir($subDir);
        
        $subConfig = <<<EOT
[server]
port: 9090
debug: true
EOT;
        file_put_contents($subDir . '/peanu.peanuts', $subConfig);
        
        // Load from subdirectory
        $config = $this->config->load($subDir);
        
        // Should have merged values
        $this->assertEquals('MyApp', $config['app']['name']);
        $this->assertEquals('1.0.0', $config['app']['version']);
        $this->assertEquals(9090, $config['server']['port']); // Overridden
        $this->assertTrue($config['server']['debug']); // New value
    }
    
    public function testGetWithDotNotation()
    {
        $configContent = <<<EOT
[server]
host: localhost
port: 8080

[database]
primary:
    host: db1.example.com
    port: 5432
EOT;
        
        file_put_contents($this->testDir . '/peanu.peanuts', $configContent);
        
        $this->assertEquals('localhost', $this->config->get('server.host', null, $this->testDir));
        $this->assertEquals(8080, $this->config->get('server.port', null, $this->testDir));
        $this->assertEquals('default', $this->config->get('server.missing', 'default', $this->testDir));
    }
    
    public function testValueParsing()
    {
        $configContent = <<<EOT
string_value: "hello world"
int_value: 42
float_value: 3.14
bool_true: true
bool_false: false
null_value: null
array_value: one, two, three
EOT;
        
        file_put_contents($this->testDir . '/peanu.peanuts', $configContent);
        
        $config = $this->config->load($this->testDir);
        
        $this->assertEquals('hello world', $config['string_value']);
        $this->assertEquals(42, $config['int_value']);
        $this->assertEquals(3.14, $config['float_value']);
        $this->assertTrue($config['bool_true']);
        $this->assertFalse($config['bool_false']);
        $this->assertNull($config['null_value']);
        $this->assertEquals(['one', 'two', 'three'], $config['array_value']);
    }
    
    public function testBinaryCompilation()
    {
        $configContent = <<<EOT
[test]
value: "test data"
number: 123
EOT;
        
        $textPath = $this->testDir . '/test.peanuts';
        $binaryPath = $this->testDir . '/test.pnt';
        
        file_put_contents($textPath, $configContent);
        
        // Compile to binary
        $this->config->compileBinary($textPath, $binaryPath);
        
        $this->assertFileExists($binaryPath);
        $this->assertFileExists($this->testDir . '/test.shell');
        
        // Verify binary file starts with magic
        $handle = fopen($binaryPath, 'rb');
        $magic = fread($handle, 4);
        fclose($handle);
        
        $this->assertEquals('PNUT', $magic);
    }
    
    public function testCacheInvalidation()
    {
        file_put_contents($this->testDir . '/peanu.peanuts', '[test]' . PHP_EOL . 'value: 1');
        
        // First load
        $config1 = $this->config->load($this->testDir);
        $this->assertEquals(1, $config1['test']['value']);
        
        // Update file
        sleep(1); // Ensure mtime changes
        file_put_contents($this->testDir . '/peanu.peanuts', '[test]' . PHP_EOL . 'value: 2');
        
        // Check for changes and reload
        $changed = $this->config->checkForChanges();
        $this->assertTrue($changed);
        
        $config2 = $this->config->load($this->testDir);
        $this->assertEquals(2, $config2['test']['value']);
    }
}