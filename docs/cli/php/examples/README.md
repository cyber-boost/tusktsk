# Examples - PHP CLI

Complete examples and workflows for the TuskLang PHP CLI.

## Quick Examples

### Basic Configuration Management

```bash
# Create a simple configuration
cat > app.tsk << 'EOF'
[app]
name: "My PHP App"
version: "1.0.0"
debug: true

[server]
host: "localhost"
port: 8080
workers: 4

[database]
host: "db.example.com"
port: 3306
name: "myapp"
EOF

# Parse and validate
tsk parse app.tsk
tsk validate app.tsk

# Get specific values
tsk config get app.name
tsk config get server.port

# Compile to binary
tsk config compile app.tsk
```

### Database Operations

```bash
# Check database status
tsk db status

# Run migrations
tsk db migrate

# Create backup
tsk db backup myapp_$(date +%Y%m%d).sql

# Restore from backup
tsk db restore myapp_20240115.sql
```

### Development Workflow

```bash
# Start development server
tsk serve 3000

# Run tests
tsk test all

# Clear cache
tsk cache clear

# Check services
tsk services status
```

## Complete Workflows

### 1. New Project Setup

```bash
#!/bin/bash
# new-project.sh

echo "Setting up new TuskLang PHP project..."

# Create project structure
mkdir -p {config,src,tests,docs}

# Create base configuration
cat > config/app.tsk << 'EOF'
[app]
name: "New PHP Project"
version: "1.0.0"
environment: "development"

[server]
host: "0.0.0.0"
port: 8080
workers: 2

[database]
driver: "mysql"
host: "localhost"
port: 3306
name: "newproject"
user: "root"
password: ""

[features]
debug: true
logging: true
caching: false
EOF

# Initialize database
tsk db init

# Run initial migrations
tsk db migrate

# Compile configuration
tsk config compile config/app.tsk

# Start development server
echo "Starting development server..."
tsk serve 8080
```

### 2. Production Deployment

```bash
#!/bin/bash
# deploy.sh

echo "Deploying to production..."

# Check system status
tsk status

# Backup current database
tsk db backup pre_deployment_$(date +%Y%m%d_%H%M%S).sql

# Run migrations
tsk db migrate

# Compile configurations
tsk config compile config/app.tsk
tsk peanuts compile

# Warm cache
tsk cache warm

# Restart services
tsk services restart

# Verify deployment
tsk services status
tsk db status

echo "Deployment complete!"
```

### 3. Testing Workflow

```bash
#!/bin/bash
# test-workflow.sh

echo "Running test workflow..."

# Clear cache
tsk cache clear

# Run all tests
tsk test all

# Run specific test categories
tsk test parser
tsk test sdk
tsk test performance

# Generate test report
tsk test all --json > test_report.json

echo "Test workflow complete!"
```

## PHP Integration Examples

### 1. Configuration Loading in PHP

```php
<?php
// config-loader.php

class ConfigLoader
{
    private $config = [];
    
    public function __construct()
    {
        $this->loadConfig();
    }
    
    private function loadConfig()
    {
        // Execute CLI command to get configuration
        $output = [];
        $returnCode = 0;
        
        exec('tsk config load . --json 2>&1', $output, $returnCode);
        
        if ($returnCode === 0) {
            $jsonOutput = implode("\n", $output);
            $this->config = json_decode($jsonOutput, true);
        } else {
            throw new Exception("Failed to load configuration: " . implode("\n", $output));
        }
    }
    
    public function get($key, $default = null)
    {
        $keys = explode('.', $key);
        $value = $this->config;
        
        foreach ($keys as $k) {
            if (isset($value[$k])) {
                $value = $value[$k];
            } else {
                return $default;
            }
        }
        
        return $value;
    }
    
    public function getAll()
    {
        return $this->config;
    }
}

// Usage
$config = new ConfigLoader();
echo "App name: " . $config->get('app.name') . "\n";
echo "Server port: " . $config->get('server.port') . "\n";
```

### 2. Database Operations in PHP

```php
<?php
// database-manager.php

class DatabaseManager
{
    public function checkStatus()
    {
        $output = [];
        $returnCode = 0;
        
        exec('tsk db status --json 2>&1', $output, $returnCode);
        
        if ($returnCode === 0) {
            $jsonOutput = implode("\n", $output);
            return json_decode($jsonOutput, true);
        } else {
            throw new Exception("Database status check failed: " . implode("\n", $output));
        }
    }
    
    public function runMigrations()
    {
        $output = [];
        $returnCode = 0;
        
        exec('tsk db migrate --json 2>&1', $output, $returnCode);
        
        if ($returnCode === 0) {
            $jsonOutput = implode("\n", $output);
            return json_decode($jsonOutput, true);
        } else {
            throw new Exception("Migration failed: " . implode("\n", $output));
        }
    }
    
    public function createBackup($filename)
    {
        $output = [];
        $returnCode = 0;
        
        exec("tsk db backup $filename --json 2>&1", $output, $returnCode);
        
        if ($returnCode === 0) {
            $jsonOutput = implode("\n", $output);
            return json_decode($jsonOutput, true);
        } else {
            throw new Exception("Backup failed: " . implode("\n", $output));
        }
    }
}

// Usage
$db = new DatabaseManager();

try {
    $status = $db->checkStatus();
    echo "Database: " . ($status['connected'] ? 'OK' : 'Error') . "\n";
    
    $migrations = $db->runMigrations();
    echo "Migrations applied: " . count($migrations['applied']) . "\n";
    
    $backup = $db->createBackup('backup_' . date('Y-m-d_H-i-s') . '.sql');
    echo "Backup created: " . $backup['file'] . "\n";
} catch (Exception $e) {
    echo "Error: " . $e->getMessage() . "\n";
}
```

### 3. Service Management in PHP

```php
<?php
// service-manager.php

class ServiceManager
{
    public function getStatus()
    {
        $output = [];
        $returnCode = 0;
        
        exec('tsk services status --json 2>&1', $output, $returnCode);
        
        if ($returnCode === 0) {
            $jsonOutput = implode("\n", $output);
            return json_decode($jsonOutput, true);
        } else {
            throw new Exception("Service status check failed: " . implode("\n", $output));
        }
    }
    
    public function startServices()
    {
        $output = [];
        $returnCode = 0;
        
        exec('tsk services start --json 2>&1', $output, $returnCode);
        
        if ($returnCode === 0) {
            $jsonOutput = implode("\n", $output);
            return json_decode($jsonOutput, true);
        } else {
            throw new Exception("Service start failed: " . implode("\n", $output));
        }
    }
    
    public function stopServices()
    {
        $output = [];
        $returnCode = 0;
        
        exec('tsk services stop --json 2>&1', $output, $returnCode);
        
        if ($returnCode === 0) {
            $jsonOutput = implode("\n", $output);
            return json_decode($jsonOutput, true);
        } else {
            throw new Exception("Service stop failed: " . implode("\n", $output));
        }
    }
    
    public function restartServices()
    {
        $output = [];
        $returnCode = 0;
        
        exec('tsk services restart --json 2>&1', $output, $returnCode);
        
        if ($returnCode === 0) {
            $jsonOutput = implode("\n", $output);
            return json_decode($jsonOutput, true);
        } else {
            throw new Exception("Service restart failed: " . implode("\n", $output));
        }
    }
}

// Usage
$services = new ServiceManager();

try {
    $status = $services->getStatus();
    echo "Services status:\n";
    foreach ($status['services'] as $service => $info) {
        echo "  $service: " . ($info['running'] ? 'Running' : 'Stopped') . "\n";
    }
    
    $result = $services->restartServices();
    echo "Services restarted: " . count($result['restarted']) . "\n";
} catch (Exception $e) {
    echo "Error: " . $e->getMessage() . "\n";
}
```

## Framework Integration Examples

### 1. Laravel Integration

```php
<?php
// app/Console/Commands/TuskLangCommand.php

namespace App\Console\Commands;

use Illuminate\Console\Command;

class TuskLangCommand extends Command
{
    protected $signature = 'tusk:config {action} {--key=} {--value=}';
    protected $description = 'Execute TuskLang CLI commands';

    public function handle()
    {
        $action = $this->argument('action');
        $key = $this->option('key');
        $value = $this->option('value');

        switch ($action) {
            case 'get':
                $this->getConfig($key);
                break;
            case 'set':
                $this->setConfig($key, $value);
                break;
            case 'compile':
                $this->compileConfig();
                break;
            case 'status':
                $this->checkStatus();
                break;
            default:
                $this->error("Unknown action: $action");
                return 1;
        }

        return 0;
    }

    private function getConfig($key)
    {
        $output = [];
        $returnCode = 0;
        
        exec("tsk config get $key --json 2>&1", $output, $returnCode);
        
        if ($returnCode === 0) {
            $jsonOutput = implode("\n", $output);
            $result = json_decode($jsonOutput, true);
            $this->info("$key = " . $result['value']);
        } else {
            $this->error("Failed to get config: " . implode("\n", $output));
        }
    }

    private function setConfig($key, $value)
    {
        $output = [];
        $returnCode = 0;
        
        exec("tsk config set $key '$value' --json 2>&1", $output, $returnCode);
        
        if ($returnCode === 0) {
            $this->info("Configuration updated: $key = $value");
        } else {
            $this->error("Failed to set config: " . implode("\n", $output));
        }
    }

    private function compileConfig()
    {
        $output = [];
        $returnCode = 0;
        
        exec('tsk config compile --json 2>&1', $output, $returnCode);
        
        if ($returnCode === 0) {
            $this->info("Configuration compiled successfully");
        } else {
            $this->error("Compilation failed: " . implode("\n", $output));
        }
    }

    private function checkStatus()
    {
        $output = [];
        $returnCode = 0;
        
        exec('tsk status --json 2>&1', $output, $returnCode);
        
        if ($returnCode === 0) {
            $jsonOutput = implode("\n", $output);
            $result = json_decode($jsonOutput, true);
            $this->info("System Status: " . $result['status']);
        } else {
            $this->error("Status check failed: " . implode("\n", $output));
        }
    }
}
```

### 2. Symfony Integration

```php
<?php
// src/Command/TuskLangCommand.php

namespace App\Command;

use Symfony\Component\Console\Command\Command;
use Symfony\Component\Console\Input\InputInterface;
use Symfony\Component\Console\Output\OutputInterface;
use Symfony\Component\Console\Input\InputArgument;
use Symfony\Component\Console\Input\InputOption;

class TuskLangCommand extends Command
{
    protected static $defaultName = 'tusk:execute';

    protected function configure()
    {
        $this
            ->setDescription('Execute TuskLang CLI commands')
            ->addArgument('command', InputArgument::REQUIRED, 'TuskLang command to execute')
            ->addOption('json', 'j', InputOption::VALUE_NONE, 'Output in JSON format')
            ->addOption('verbose', 'v', InputOption::VALUE_NONE, 'Enable verbose output');
    }

    protected function execute(InputInterface $input, OutputInterface $output): int
    {
        $command = $input->getArgument('command');
        $json = $input->getOption('json');
        $verbose = $input->getOption('verbose');

        $cmd = "tsk $command";
        
        if ($json) {
            $cmd .= " --json";
        }
        
        if ($verbose) {
            $cmd .= " --verbose";
        }

        $output->writeln("Executing: $cmd");

        $process = proc_open($cmd, [
            1 => ['pipe', 'w'],
            2 => ['pipe', 'w'],
        ], $pipes);

        if (is_resource($process)) {
            $stdout = stream_get_contents($pipes[1]);
            $stderr = stream_get_contents($pipes[2]);
            
            fclose($pipes[1]);
            fclose($pipes[2]);
            
            $returnCode = proc_close($process);

            if ($returnCode === 0) {
                $output->writeln($stdout);
                return Command::SUCCESS;
            } else {
                $output->writeln("<error>$stderr</error>");
                return Command::FAILURE;
            }
        }

        return Command::FAILURE;
    }
}
```

## CI/CD Integration Examples

### 1. GitHub Actions

```yaml
# .github/workflows/tusklang.yml

name: TuskLang CI

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup PHP
      uses: shivammathur/setup-php@v2
      with:
        php-version: '8.2'
        extensions: pdo, json, mbstring, msgpack
    
    - name: Install TuskLang
      run: |
        curl -sSL tusklang.org/tsk.sh | sudo bash
    
    - name: Check system status
      run: tsk status
    
    - name: Parse configuration
      run: tsk parse config/app.tsk
    
    - name: Validate configuration
      run: tsk validate config/app.tsk
    
    - name: Compile configuration
      run: tsk config compile config/app.tsk
    
    - name: Run tests
      run: tsk test all
    
    - name: Check database
      run: tsk db status
    
    - name: Run migrations
      run: tsk db migrate
    
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: test_report.json
```

### 2. GitLab CI

```yaml
# .gitlab-ci.yml

stages:
  - test
  - deploy

variables:
  TUSKLANG_VERSION: "2.0.0"

before_script:
  - curl -sSL tusklang.org/tsk.sh | sudo bash

test:
  stage: test
  image: php:8.2-cli
  script:
    - apt-get update && apt-get install -y curl
    - curl -sSL tusklang.org/tsk.sh | bash
    - tsk status
    - tsk parse config/app.tsk
    - tsk validate config/app.tsk
    - tsk test all
  artifacts:
    reports:
      junit: test-results.xml
    paths:
      - test_report.json

deploy:
  stage: deploy
  image: php:8.2-cli
  script:
    - curl -sSL tusklang.org/tsk.sh | bash
    - tsk db backup pre_deployment.sql
    - tsk db migrate
    - tsk config compile config/app.tsk
    - tsk services restart
  only:
    - main
```

## See Also

- [Basic Usage](../quickstart.md)
- [Command Reference](../commands/README.md)
- [Troubleshooting](../troubleshooting.md)

**Strong. Secure. Scalable.** 🐘 