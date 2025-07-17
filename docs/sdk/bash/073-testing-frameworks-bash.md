# Testing Frameworks in TuskLang - Bash Guide

## 🧪 **Revolutionary Testing Configuration**

Testing frameworks in TuskLang transform your configuration files into intelligent testing systems. No more separate testing frameworks or complex test configurations - everything lives in your TuskLang configuration with dynamic test generation, automatic test execution, and intelligent result analysis.

> **"We don't bow to any king"** - TuskLang testing frameworks break free from traditional testing constraints and bring modern test automation capabilities to your Bash applications.

## 🚀 **Core Testing Directives**

### **Basic Testing Setup**
```bash
#test: unit                      # Test type
#test-type: unit                 # Alternative syntax
#test-framework: bats            # Testing framework
#test-coverage: 80               # Coverage threshold
#test-timeout: 30                # Test timeout (seconds)
#test-parallel: true             # Parallel execution
```

### **Advanced Testing Configuration**
```bash
#test-enabled: true              # Enable testing
#test-strategy: tdd              # Testing strategy
#test-reporting: html            # Test reporting format
#test-notifications: slack       # Test notifications
#test-ci: true                   # CI/CD integration
#test-mocking: true              # Enable mocking
```

## 🔧 **Bash Testing Implementation**

### **Basic Test Manager**
```bash
#!/bin/bash

# Load testing configuration
source <(tsk load testing.tsk)

# Testing configuration
TEST_ENABLED="${test_enabled:-true}"
TEST_FRAMEWORK="${test_framework:-bats}"
TEST_COVERAGE="${test_coverage:-80}"
TEST_TIMEOUT="${test_timeout:-30}"
TEST_PARALLEL="${test_parallel:-true}"

# Test manager
class TestManager {
    constructor() {
        this.enabled = TEST_ENABLED
        this.framework = TEST_FRAMEWORK
        this.coverage = TEST_COVERAGE
        this.timeout = TEST_TIMEOUT
        this.parallel = TEST_PARALLEL
        this.results = new Map()
        this.stats = {
            total: 0,
            passed: 0,
            failed: 0,
            skipped: 0,
            coverage: 0
        }
    }
    
    runTests(testSuite) {
        if (!this.enabled) {
            console.log("Testing disabled")
            return { success: true, results: [] }
        }
        
        console.log(`Running tests with ${this.framework} framework...`)
        
        const startTime = Date.now()
        const results = []
        
        for (const test of testSuite) {
            const result = this.runTest(test)
            results.push(result)
            this.updateStats(result)
        }
        
        const endTime = Date.now()
        const duration = endTime - startTime
        
        const summary = this.generateSummary(results, duration)
        this.saveResults(summary)
        
        return summary
    }
    
    runTest(test) {
        const testStart = Date.now()
        
        try {
            let result
            
            switch (this.framework) {
                case 'bats':
                    result = this.runBatsTest(test)
                    break
                case 'shunit2':
                    result = this.runShunit2Test(test)
                    break
                case 'bashunit':
                    result = this.runBashunitTest(test)
                    break
                default:
                    result = this.runCustomTest(test)
            }
            
            const testEnd = Date.now()
            result.duration = testEnd - testStart
            
            return result
        } catch (error) {
            return {
                name: test.name,
                status: 'error',
                error: error.message,
                duration: Date.now() - testStart
            }
        }
    }
    
    runBatsTest(test) {
        const batsCmd = `bats --tap "${test.file}"`
        const output = this.executeCommand(batsCmd, this.timeout)
        
        return this.parseBatsOutput(output, test)
    }
    
    runShunit2Test(test) {
        const shunit2Cmd = `shunit2 "${test.file}"`
        const output = this.executeCommand(shunit2Cmd, this.timeout)
        
        return this.parseShunit2Output(output, test)
    }
    
    runBashunitTest(test) {
        const bashunitCmd = `bashunit "${test.file}"`
        const output = this.executeCommand(bashunitCmd, this.timeout)
        
        return this.parseBashunitOutput(output, test)
    }
    
    runCustomTest(test) {
        const customCmd = test.command || `bash "${test.file}"`
        const output = this.executeCommand(customCmd, this.timeout)
        
        return {
            name: test.name,
            status: output.exitCode === 0 ? 'passed' : 'failed',
            output: output.stdout,
            error: output.stderr,
            duration: 0
        }
    }
    
    executeCommand(command, timeout) {
        const { exec } = require('child_process')
        
        return new Promise((resolve) => {
            const child = exec(command, { timeout: timeout * 1000 })
            
            let stdout = ''
            let stderr = ''
            
            child.stdout.on('data', (data) => {
                stdout += data
            })
            
            child.stderr.on('data', (data) => {
                stderr += data
            })
            
            child.on('close', (exitCode) => {
                resolve({
                    exitCode,
                    stdout,
                    stderr
                })
            })
        })
    }
    
    parseBatsOutput(output, test) {
        // Parse BATS TAP output
        const lines = output.stdout.split('\n')
        let status = 'unknown'
        let message = ''
        
        for (const line of lines) {
            if (line.startsWith('ok')) {
                status = 'passed'
                message = line.substring(3).trim()
            } else if (line.startsWith('not ok')) {
                status = 'failed'
                message = line.substring(6).trim()
            }
        }
        
        return {
            name: test.name,
            status,
            message,
            output: output.stdout,
            error: output.stderr
        }
    }
    
    parseShunit2Output(output, test) {
        // Parse shunit2 output
        const lines = output.stdout.split('\n')
        let status = 'unknown'
        let message = ''
        
        for (const line of lines) {
            if (line.includes('ASSERT:') && line.includes('PASSED')) {
                status = 'passed'
                message = line
            } else if (line.includes('ASSERT:') && line.includes('FAILED')) {
                status = 'failed'
                message = line
            }
        }
        
        return {
            name: test.name,
            status,
            message,
            output: output.stdout,
            error: output.stderr
        }
    }
    
    parseBashunitOutput(output, test) {
        // Parse bashunit output
        const lines = output.stdout.split('\n')
        let status = 'unknown'
        let message = ''
        
        for (const line of lines) {
            if (line.includes('✓')) {
                status = 'passed'
                message = line
            } else if (line.includes('✗')) {
                status = 'failed'
                message = line
            }
        }
        
        return {
            name: test.name,
            status,
            message,
            output: output.stdout,
            error: output.stderr
        }
    }
    
    updateStats(result) {
        this.stats.total++
        
        switch (result.status) {
            case 'passed':
                this.stats.passed++
                break
            case 'failed':
                this.stats.failed++
                break
            case 'skipped':
                this.stats.skipped++
                break
        }
    }
    
    generateSummary(results, duration) {
        const passed = results.filter(r => r.status === 'passed').length
        const failed = results.filter(r => r.status === 'failed').length
        const skipped = results.filter(r => r.status === 'skipped').length
        const total = results.length
        
        const coverage = this.calculateCoverage(results)
        
        return {
            total,
            passed,
            failed,
            skipped,
            coverage,
            duration,
            results,
            success: failed === 0
        }
    }
    
    calculateCoverage(results) {
        if (results.length === 0) return 0
        
        const covered = results.filter(r => r.status === 'passed').length
        return Math.round((covered / results.length) * 100)
    }
    
    saveResults(summary) {
        const timestamp = new Date().toISOString()
        const resultKey = `test_results_${timestamp}`
        
        this.results.set(resultKey, summary)
        
        // Save to file
        const fs = require('fs')
        fs.writeFileSync(`test-results-${timestamp}.json`, JSON.stringify(summary, null, 2))
    }
    
    getStats() {
        return { ...this.stats }
    }
}

# Initialize test manager
const testManager = new TestManager()
```

### **BATS Framework Integration**
```bash
#!/bin/bash

# BATS testing framework integration
bats_integration() {
    local operation="$1"
    local test_file="$2"
    local options="$3"
    
    case "$operation" in
        "run")
            bats_run_test "$test_file" "$options"
            ;;
        "install")
            bats_install
            ;;
        "setup")
            bats_setup_environment
            ;;
        "teardown")
            bats_teardown_environment
            ;;
        *)
            echo "Unknown BATS operation: $operation"
            return 1
            ;;
    esac
}

bats_install() {
    echo "Installing BATS testing framework..."
    
    # Check if BATS is already installed
    if command -v bats >/dev/null 2>&1; then
        echo "✓ BATS already installed"
        bats --version
        return 0
    fi
    
    # Install BATS
    if command -v npm >/dev/null 2>&1; then
        npm install -g bats
    elif command -v git >/dev/null 2>&1; then
        git clone https://github.com/bats-core/bats-core.git /tmp/bats
        sudo /tmp/bats/install.sh /usr/local
    else
        echo "✗ Cannot install BATS: npm or git not available"
        return 1
    fi
    
    if command -v bats >/dev/null 2>&1; then
        echo "✓ BATS installed successfully"
        bats --version
    else
        echo "✗ BATS installation failed"
        return 1
    fi
}

bats_run_test() {
    local test_file="$1"
    local options="$2"
    
    if [[ ! -f "$test_file" ]]; then
        echo "Test file not found: $test_file"
        return 1
    fi
    
    # Build BATS command
    local bats_cmd="bats"
    
    if [[ -n "$options" ]]; then
        bats_cmd="$bats_cmd $options"
    fi
    
    # Add common options
    bats_cmd="$bats_cmd --tap --timing"
    
    if [[ "${test_parallel}" == "true" ]]; then
        bats_cmd="$bats_cmd --jobs 4"
    fi
    
    # Run test
    echo "Running BATS test: $test_file"
    $bats_cmd "$test_file"
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "✓ BATS test passed"
    else
        echo "✗ BATS test failed"
    fi
    
    return $exit_code
}

bats_setup_environment() {
    echo "Setting up BATS test environment..."
    
    # Create test directories
    mkdir -p /tmp/bats-test/{bin,lib,test}
    
    # Set up PATH
    export PATH="/tmp/bats-test/bin:$PATH"
    
    # Create test helper functions
    cat > /tmp/bats-test/lib/test_helper.bash << 'EOF'
#!/usr/bin/env bash

# Test helper functions
setup() {
    # Setup before each test
    export TEST_TMPDIR=$(mktemp -d)
    cd "$TEST_TMPDIR"
}

teardown() {
    # Cleanup after each test
    rm -rf "$TEST_TMPDIR"
}

# Assertion functions
assert_success() {
    if [[ $status -ne 0 ]]; then
        echo "Expected success, got exit code $status"
        echo "Output: $output"
        return 1
    fi
}

assert_failure() {
    if [[ $status -eq 0 ]]; then
        echo "Expected failure, got exit code $status"
        echo "Output: $output"
        return 1
    fi
}

assert_output() {
    local expected="$1"
    if [[ "$output" != "$expected" ]]; then
        echo "Expected output: '$expected'"
        echo "Actual output: '$output'"
        return 1
    fi
}

assert_line() {
    local expected="$1"
    local found=false
    
    while IFS= read -r line; do
        if [[ "$line" == "$expected" ]]; then
            found=true
            break
        fi
    done <<< "$output"
    
    if [[ "$found" != "true" ]]; then
        echo "Expected line: '$expected'"
        echo "Actual output: '$output'"
        return 1
    fi
}
EOF
    
    echo "✓ BATS test environment setup complete"
}

bats_teardown_environment() {
    echo "Tearing down BATS test environment..."
    
    # Clean up test directories
    rm -rf /tmp/bats-test
    
    echo "✓ BATS test environment cleaned up"
}
```

### **shunit2 Framework Integration**
```bash
#!/bin/bash

# shunit2 testing framework integration
shunit2_integration() {
    local operation="$1"
    local test_file="$2"
    local options="$3"
    
    case "$operation" in
        "run")
            shunit2_run_test "$test_file" "$options"
            ;;
        "install")
            shunit2_install
            ;;
        "setup")
            shunit2_setup_environment
            ;;
        "teardown")
            shunit2_teardown_environment
            ;;
        *)
            echo "Unknown shunit2 operation: $operation"
            return 1
            ;;
    esac
}

shunit2_install() {
    echo "Installing shunit2 testing framework..."
    
    # Check if shunit2 is already installed
    if command -v shunit2 >/dev/null 2>&1; then
        echo "✓ shunit2 already installed"
        shunit2 --version 2>/dev/null || echo "shunit2 installed"
        return 0
    fi
    
    # Install shunit2
    local shunit2_url="https://github.com/kward/shunit2/archive/refs/tags/v2.1.8.tar.gz"
    local temp_dir=$(mktemp -d)
    
    cd "$temp_dir"
    curl -L "$shunit2_url" | tar -xz
    cd shunit2-*
    
    sudo cp shunit2 /usr/local/bin/
    sudo chmod +x /usr/local/bin/shunit2
    
    cd /
    rm -rf "$temp_dir"
    
    if command -v shunit2 >/dev/null 2>&1; then
        echo "✓ shunit2 installed successfully"
    else
        echo "✗ shunit2 installation failed"
        return 1
    fi
}

shunit2_run_test() {
    local test_file="$1"
    local options="$2"
    
    if [[ ! -f "$test_file" ]]; then
        echo "Test file not found: $test_file"
        return 1
    fi
    
    # Build shunit2 command
    local shunit2_cmd="shunit2"
    
    if [[ -n "$options" ]]; then
        shunit2_cmd="$shunit2_cmd $options"
    fi
    
    # Run test
    echo "Running shunit2 test: $test_file"
    $shunit2_cmd "$test_file"
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "✓ shunit2 test passed"
    else
        echo "✗ shunit2 test failed"
    fi
    
    return $exit_code
}

shunit2_setup_environment() {
    echo "Setting up shunit2 test environment..."
    
    # Create test directories
    mkdir -p /tmp/shunit2-test/{bin,lib,test}
    
    # Set up PATH
    export PATH="/tmp/shunit2-test/bin:$PATH"
    
    # Create test helper functions
    cat > /tmp/shunit2-test/lib/test_helper.sh << 'EOF'
#!/usr/bin/env bash

# Test helper functions for shunit2

# Setup function called before each test
setUp() {
    # Setup before each test
    export TEST_TMPDIR=$(mktemp -d)
    cd "$TEST_TMPDIR"
}

# Teardown function called after each test
tearDown() {
    # Cleanup after each test
    rm -rf "$TEST_TMPDIR"
}

# Custom assertion functions
assertCommandSuccess() {
    local command="$1"
    local output
    local exit_code
    
    output=$(eval "$command" 2>&1)
    exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        return 0
    else
        echo "Command failed with exit code $exit_code: $command"
        echo "Output: $output"
        return 1
    fi
}

assertCommandFailure() {
    local command="$1"
    local output
    local exit_code
    
    output=$(eval "$command" 2>&1)
    exit_code=$?
    
    if [[ $exit_code -ne 0 ]]; then
        return 0
    else
        echo "Command succeeded but expected failure: $command"
        echo "Output: $output"
        return 1
    fi
}

assertFileExists() {
    local file="$1"
    
    if [[ -f "$file" ]]; then
        return 0
    else
        echo "File does not exist: $file"
        return 1
    fi
}

assertFileNotExists() {
    local file="$1"
    
    if [[ ! -f "$file" ]]; then
        return 0
    else
        echo "File exists but should not: $file"
        return 1
    fi
}

assertDirectoryExists() {
    local directory="$1"
    
    if [[ -d "$directory" ]]; then
        return 0
    else
        echo "Directory does not exist: $directory"
        return 1
    fi
}

assertStringEquals() {
    local expected="$1"
    local actual="$2"
    
    if [[ "$expected" == "$actual" ]]; then
        return 0
    else
        echo "Expected: '$expected'"
        echo "Actual: '$actual'"
        return 1
    fi
}

assertStringContains() {
    local haystack="$1"
    local needle="$2"
    
    if [[ "$haystack" == *"$needle"* ]]; then
        return 0
    else
        echo "String '$haystack' does not contain '$needle'"
        return 1
    fi
}
EOF
    
    echo "✓ shunit2 test environment setup complete"
}

shunit2_teardown_environment() {
    echo "Tearing down shunit2 test environment..."
    
    # Clean up test directories
    rm -rf /tmp/shunit2-test
    
    echo "✓ shunit2 test environment cleaned up"
}
```

### **Test Coverage Analysis**
```bash
#!/bin/bash

# Test coverage analysis
coverage_analysis() {
    local test_results="$1"
    local source_files="$2"
    
    echo "Analyzing test coverage..."
    
    # Calculate line coverage
    local total_lines=0
    local covered_lines=0
    
    while IFS= read -r file; do
        if [[ -f "$file" ]]; then
            local file_lines=$(wc -l < "$file")
            local file_coverage=$(calculate_file_coverage "$file" "$test_results")
            
            total_lines=$((total_lines + file_lines))
            covered_lines=$((covered_lines + file_coverage))
            
            echo "  $file: $file_coverage/$file_lines lines covered"
        fi
    done <<< "$source_files"
    
    # Calculate overall coverage
    local coverage_percentage=0
    if [[ $total_lines -gt 0 ]]; then
        coverage_percentage=$((covered_lines * 100 / total_lines))
    fi
    
    echo "Overall coverage: $coverage_percentage%"
    
    # Check against threshold
    if [[ $coverage_percentage -ge $TEST_COVERAGE ]]; then
        echo "✓ Coverage threshold met ($TEST_COVERAGE%)"
        return 0
    else
        echo "✗ Coverage threshold not met (required: $TEST_COVERAGE%, actual: $coverage_percentage%)"
        return 1
    fi
}

calculate_file_coverage() {
    local file="$1"
    local test_results="$2"
    
    # Simple line coverage calculation
    # In a real implementation, you would use tools like gcov, lcov, or custom coverage analysis
    
    local total_lines=$(wc -l < "$file")
    local covered_lines=0
    
    # This is a simplified calculation
    # In practice, you would track which lines were executed during tests
    if [[ -n "$test_results" ]]; then
        # Assume some coverage based on test results
        covered_lines=$((total_lines * 80 / 100))  # Simplified assumption
    fi
    
    echo "$covered_lines"
}
```

## 🎯 **Real-World Configuration Examples**

### **Complete Testing Configuration**
```bash
# testing-config.tsk
testing_config:
  enabled: true
  framework: bats
  coverage: 80
  timeout: 30
  parallel: true

#test: unit
#test-type: unit
#test-framework: bats
#test-coverage: 80
#test-timeout: 30
#test-parallel: true

#test-enabled: true
#test-strategy: tdd
#test-reporting: html
#test-notifications: slack
#test-ci: true
#test-mocking: true

#test-config:
#  unit:
#    framework: bats
#    coverage: 80
#    timeout: 30
#    parallel: true
#    files: ["tests/unit/*.bats"]
#  integration:
#    framework: shunit2
#    coverage: 70
#    timeout: 60
#    parallel: false
#    files: ["tests/integration/*.sh"]
#  e2e:
#    framework: custom
#    coverage: 50
#    timeout: 120
#    parallel: false
#    files: ["tests/e2e/*.sh"]
#  reporting:
#    format: html
#    output: test-reports/
#    include_coverage: true
#    include_timing: true
#  notifications:
#    slack:
#      webhook: "${SLACK_WEBHOOK}"
#      channel: "#testing"
#    email:
#      recipients: ["team@example.com"]
#      smtp_server: "smtp.example.com"
#  ci:
#    enabled: true
#    parallel_jobs: 4
#    artifacts: true
#    coverage_badge: true
```

### **Multi-Framework Testing**
```bash
# multi-framework-testing.tsk
multi_framework_config:
  strategies:
    unit: bats
    integration: shunit2
    e2e: custom
    performance: k6

#test-unit: bats
#test-integration: shunit2
#test-e2e: custom
#test-performance: k6

#test-config:
#  frameworks:
#    bats:
#      version: "1.8.0"
#      options: ["--tap", "--timing"]
#      parallel: true
#      timeout: 30
#    shunit2:
#      version: "2.1.8"
#      options: ["--verbose"]
#      parallel: false
#      timeout: 60
#    custom:
#      command: "bash"
#      options: ["-e"]
#      parallel: false
#      timeout: 120
#    k6:
#      version: "0.45.0"
#      options: ["--out", "json=results.json"]
#      parallel: true
#      timeout: 300
#  suites:
#    unit:
#      framework: bats
#      files: ["tests/unit/**/*.bats"]
#      coverage: 80
#    integration:
#      framework: shunit2
#      files: ["tests/integration/**/*.sh"]
#      coverage: 70
#    e2e:
#      framework: custom
#      files: ["tests/e2e/**/*.sh"]
#      coverage: 50
#    performance:
#      framework: k6
#      files: ["tests/performance/**/*.js"]
#      thresholds:
#        - "http_req_duration < 1000ms"
#        - "http_req_failed < 1%"
```

### **CI/CD Testing Pipeline**
```bash
# ci-cd-testing.tsk
ci_cd_config:
  pipeline:
    - stage: test
      jobs:
        - unit_tests
        - integration_tests
        - e2e_tests
    - stage: report
      jobs:
        - coverage_report
        - test_report

#test-ci: true
#test-pipeline: true
#test-artifacts: true

#test-config:
#  ci:
#    enabled: true
#    stages:
#      - name: test
#        jobs:
#          - name: unit_tests
#            framework: bats
#            parallel: true
#            timeout: 30
#            artifacts: ["test-results/"]
#          - name: integration_tests
#            framework: shunit2
#            parallel: false
#            timeout: 60
#            artifacts: ["test-results/"]
#          - name: e2e_tests
#            framework: custom
#            parallel: false
#            timeout: 120
#            artifacts: ["test-results/", "screenshots/"]
#      - name: report
#        jobs:
#          - name: coverage_report
#            type: coverage
#            format: html
#            artifacts: ["coverage-report/"]
#          - name: test_report
#            type: summary
#            format: html
#            artifacts: ["test-report/"]
#    artifacts:
#      paths:
#        - "test-results/"
#        - "coverage-report/"
#        - "test-report/"
#      expire_in: "30 days"
#    notifications:
#      slack:
#        webhook: "${SLACK_WEBHOOK}"
#        channel: "#ci-cd"
#        on_success: change
#        on_failure: always
```

## 🚨 **Troubleshooting Testing Frameworks**

### **Common Issues and Solutions**

**1. Framework Installation Issues**
```bash
# Debug framework installation
debug_framework_installation() {
    local framework="$1"
    
    echo "Debugging framework installation for: $framework"
    
    case "$framework" in
        "bats")
            debug_bats_installation
            ;;
        "shunit2")
            debug_shunit2_installation
            ;;
        "bashunit")
            debug_bashunit_installation
            ;;
        *)
            echo "Unknown framework: $framework"
            ;;
    esac
}

debug_bats_installation() {
    echo "Checking BATS installation..."
    
    if command -v bats >/dev/null 2>&1; then
        echo "✓ BATS is installed"
        bats --version
        
        # Check BATS core
        if bats --help | grep -q "Bats"; then
            echo "✓ BATS core is working"
        else
            echo "✗ BATS core is not working properly"
        fi
    else
        echo "✗ BATS is not installed"
        
        # Check installation methods
        if command -v npm >/dev/null 2>&1; then
            echo "✓ npm is available for installation"
        else
            echo "✗ npm is not available"
        fi
        
        if command -v git >/dev/null 2>&1; then
            echo "✓ git is available for installation"
        else
            echo "✗ git is not available"
        fi
    fi
}

debug_shunit2_installation() {
    echo "Checking shunit2 installation..."
    
    if command -v shunit2 >/dev/null 2>&1; then
        echo "✓ shunit2 is installed"
        
        # Test shunit2 functionality
        local test_file=$(mktemp)
        cat > "$test_file" << 'EOF'
#!/usr/bin/env bash

testExample() {
    assertEquals "Expected equals actual" "expected" "expected"
}

. shunit2
EOF
        
        if bash "$test_file" >/dev/null 2>&1; then
            echo "✓ shunit2 is working properly"
        else
            echo "✗ shunit2 is not working properly"
        fi
        
        rm -f "$test_file"
    else
        echo "✗ shunit2 is not installed"
    fi
}
```

**2. Test Execution Issues**
```bash
# Debug test execution
debug_test_execution() {
    local framework="$1"
    local test_file="$2"
    
    echo "Debugging test execution for: $framework"
    
    if [[ ! -f "$test_file" ]]; then
        echo "✗ Test file not found: $test_file"
        return 1
    fi
    
    echo "✓ Test file exists: $test_file"
    
    # Check file permissions
    if [[ -x "$test_file" ]]; then
        echo "✓ Test file is executable"
    else
        echo "⚠ Test file is not executable"
        chmod +x "$test_file"
        echo "✓ Made test file executable"
    fi
    
    # Check file syntax
    if bash -n "$test_file" 2>/dev/null; then
        echo "✓ Test file syntax is valid"
    else
        echo "✗ Test file has syntax errors"
        bash -n "$test_file"
        return 1
    fi
    
    # Run test with debug output
    case "$framework" in
        "bats")
            echo "Running BATS test with debug output..."
            bats --verbose "$test_file"
            ;;
        "shunit2")
            echo "Running shunit2 test with debug output..."
            bash "$test_file"
            ;;
        *)
            echo "Running custom test..."
            bash "$test_file"
            ;;
    esac
}
```

## 🔒 **Security Best Practices**

### **Testing Security Checklist**
```bash
# Security validation
validate_testing_security() {
    echo "Validating testing security configuration..."
    
    # Check test isolation
    if [[ "${test_isolation}" == "true" ]]; then
        echo "✓ Test isolation enabled"
    else
        echo "⚠ Test isolation not enabled"
    fi
    
    # Check test data security
    if [[ "${test_data_encryption}" == "true" ]]; then
        echo "✓ Test data encryption enabled"
    else
        echo "⚠ Test data encryption not enabled"
    fi
    
    # Check test environment security
    if [[ "${test_environment_cleanup}" == "true" ]]; then
        echo "✓ Test environment cleanup enabled"
    else
        echo "⚠ Test environment cleanup not enabled"
    fi
    
    # Check test credentials
    if [[ -n "${test_credentials}" ]]; then
        echo "✓ Test credentials configured"
        
        # Check if credentials are secure
        if [[ "${test_credentials}" == *"test"* ]] || [[ "${test_credentials}" == *"demo"* ]]; then
            echo "✓ Test credentials appear to be safe"
        else
            echo "⚠ Test credentials may contain production data"
        fi
    else
        echo "⚠ Test credentials not configured"
    fi
}
```

## 📈 **Performance Optimization Tips**

### **Testing Performance Checklist**
```bash
# Performance validation
validate_testing_performance() {
    echo "Validating testing performance configuration..."
    
    # Check parallel execution
    if [[ "${test_parallel}" == "true" ]]; then
        echo "✓ Parallel test execution enabled"
        
        local max_jobs="${test_max_jobs:-4}"
        echo "  Max parallel jobs: $max_jobs"
    else
        echo "⚠ Parallel test execution not enabled"
    fi
    
    # Check test timeout
    if [[ -n "${test_timeout}" ]]; then
        echo "✓ Test timeout configured: ${test_timeout}s"
        
        if [[ "${test_timeout}" -gt 300 ]]; then
            echo "⚠ Long test timeout may impact CI/CD performance"
        fi
    else
        echo "⚠ Test timeout not configured"
    fi
    
    # Check test caching
    if [[ "${test_caching}" == "true" ]]; then
        echo "✓ Test caching enabled"
    else
        echo "⚠ Test caching not enabled"
    fi
    
    # Check test reporting
    if [[ "${test_reporting}" == "html" ]]; then
        echo "✓ HTML test reporting enabled"
    elif [[ "${test_reporting}" == "json" ]]; then
        echo "✓ JSON test reporting enabled"
    else
        echo "⚠ Test reporting not configured"
    fi
}
```

## 🎯 **Next Steps**

- **Performance Tuning**: Learn about testing performance optimization
- **Plugin Integration**: Explore testing plugins
- **Advanced Patterns**: Understand complex testing patterns
- **Continuous Testing**: Implement continuous testing strategies
- **Test Automation**: Automate test execution and reporting

---

**Testing frameworks transform your TuskLang configuration into a powerful testing system. They bring modern test automation capabilities to your Bash applications with intelligent test execution, comprehensive coverage analysis, and seamless CI/CD integration!** 