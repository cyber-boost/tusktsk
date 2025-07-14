#!/usr/bin/env php
<?php
/**
 * Simple Test for TuskLang Enhanced Concepts
 * ==========================================
 * Proof of concept for the new features
 */

echo "🐘 TuskLang Enhanced - Simple Proof of Concept\n";
echo "==============================================\n\n";

// Simulate the enhanced parser behavior
class SimpleEnhancedParser {
    private $globalVars = [];
    private $sectionVars = [];
    private $currentSection = null;
    
    public function parse($content) {
        $lines = explode("\n", $content);
        $result = [];
        
        foreach ($lines as $line) {
            $line = trim($line);
            if (empty($line) || str_starts_with($line, '#')) continue;
            
            // Remove optional semicolon
            $line = rtrim($line, ';');
            
            // Section detection
            if (preg_match('/^\[([a-zA-Z_]\w*)\]$/', $line, $m)) {
                $this->currentSection = $m[1];
                $result[$this->currentSection] = [];
                $this->sectionVars[$this->currentSection] = [];
                continue;
            }
            
            // Key-value parsing
            if (preg_match('/^([$]?[a-zA-Z_]\w*)\s*[:=]\s*(.+)$/', $line, $m)) {
                $key = $m[1];
                $value = $this->parseValue($m[2]);
                
                // Store based on context
                if ($this->currentSection) {
                    $result[$this->currentSection][$key] = $value;
                    if (!str_starts_with($key, '$')) {
                        $this->sectionVars[$this->currentSection][$key] = $value;
                    }
                } else {
                    $result[$key] = $value;
                }
                
                // Store global vars
                if (str_starts_with($key, '$')) {
                    $this->globalVars[substr($key, 1)] = $value;
                }
            }
        }
        
        return $result;
    }
    
    private function parseValue($value) {
        $value = trim($value, ' "\'');
        
        // Handle $variable references
        if (preg_match('/^\$(\w+)$/', $value, $m)) {
            return $this->globalVars[$m[1]] ?? $value;
        }
        
        // Handle section variable references
        if ($this->currentSection && isset($this->sectionVars[$this->currentSection][$value])) {
            return $this->sectionVars[$this->currentSection][$value];
        }
        
        // Handle @date()
        if (preg_match('/^@date\([\'"]([^\'"]+)[\'"]\)$/', $value, $m)) {
            return date($m[1]);
        }
        
        // Handle ranges
        if (preg_match('/^(\d+)-(\d+)$/', $value, $m)) {
            return ['min' => (int)$m[1], 'max' => (int)$m[2], 'type' => 'range'];
        }
        
        // Basic type conversion
        if ($value === 'true') return true;
        if ($value === 'false') return false;
        if (is_numeric($value)) return strpos($value, '.') !== false ? (float)$value : (int)$value;
        
        return $value;
    }
}

// Test the concepts
$testConfig = <<<TSK
# Global variables with $
\$app_name: "TuskPHP Enhanced"
\$global_port: 8080

# Top level
name: \$app_name
port: \$global_port
year: @date('Y')

# Optional semicolons
timeout: 30;
debug: true

# Range
port_range: 8000-9000

[database]
host: "localhost"
port: 5432
# Section variable (no $)
driver: "postgres"
# Reference section var
connection: driver

[api]
# Use global
app: \$app_name
# Section local
version: "v1"
endpoint: version
TSK;

$parser = new SimpleEnhancedParser();
$result = $parser->parse($testConfig);

echo "Parsed Configuration:\n";
echo "====================\n";
print_r($result);

echo "\nKey Features Demonstrated:\n";
echo "- Global variables: \$app_name = " . $result['name'] . "\n";
echo "- Section variables: database.connection = " . $result['database']['connection'] . "\n";
echo "- Date function: year = " . $result['year'] . "\n";
echo "- Range: port_range = " . json_encode($result['port_range']) . "\n";
echo "- Optional semicolons: timeout = " . $result['timeout'] . "\n";

echo "\n✅ Core concepts proven! Now we can build the full implementation.\n";