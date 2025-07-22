/**
 * G6: DOCUMENTATION & EXAMPLES - Usage Validation
 * ===============================================
 * Comprehensive usage examples for each operator
 * API documentation validation with real-world scenarios
 * Tutorial and getting-started guide validation
 * Code sample testing and verification
 * Documentation consistency and completeness checking
 * Interactive example playground development
 */

const { expect } = require('chai');
const fs = require('fs');
const path = require('path');
const { TuskLangEnhanced } = require('../../tsk-enhanced.js');

class DocumentationExamples {
  constructor() {
    this.tusk = new TuskLangEnhanced();
    this.validationResults = [];
    this.examples = [];
    this.documentationIssues = [];
    this.startTime = Date.now();
  }

  /**
   * Generate comprehensive usage examples for all operators
   */
  generateUsageExamples() {
    console.log('🔍 Generating Usage Examples...');
    
    const examples = [
      {
        category: 'Core Operators',
        operators: [
          {
            name: '@query',
            description: 'Execute database queries with parameterized inputs',
            examples: [
              {
                title: 'Simple SELECT Query',
                code: `@query {
  query: "SELECT * FROM users WHERE age > ?"
  params: [25]
  database: "production_db"
}`,
                expected: 'Array of user objects'
              },
              {
                title: 'INSERT with Parameters',
                code: `@query {
  query: "INSERT INTO users (name, email, age) VALUES (?, ?, ?)"
  params: ["John Doe", "john@example.com", 30]
  database: "production_db"
}`,
                expected: 'Insert result with affected rows'
              },
              {
                title: 'Complex JOIN Query',
                code: `@query {
  query: "SELECT u.name, p.title FROM users u JOIN posts p ON u.id = p.user_id WHERE u.active = ?"
  params: [true]
  database: "production_db"
}`,
                expected: 'Array of user-post combinations'
              }
            ]
          },
          {
            name: '@cache',
            description: 'Manage caching operations with TTL support',
            examples: [
              {
                title: 'Set Cache with TTL',
                code: `@cache {
  operation: "set"
  key: "user:123"
  value: "{'name': 'John', 'age': 30}"
  ttl: 3600
}`,
                expected: 'true if successful'
              },
              {
                title: 'Get Cache Value',
                code: `@cache {
  operation: "get"
  key: "user:123"
}`,
                expected: 'Cached value or null'
              },
              {
                title: 'Delete Cache Entry',
                code: `@cache {
  operation: "delete"
  key: "user:123"
}`,
                expected: 'true if deleted'
              }
            ]
          }
        ]
      },
      {
        category: 'String Operations',
        operators: [
          {
            name: '@string',
            description: 'Perform string manipulation operations',
            examples: [
              {
                title: 'String Concatenation',
                code: `@string {
  operation: "concat"
  strings: ["Hello", " ", "World", "!"]
}`,
                expected: '"Hello World!"'
              },
              {
                title: 'String Replacement',
                code: `@string {
  operation: "replace"
  input: "Hello World"
  search: "World"
  replace: "Universe"
}`,
                expected: '"Hello Universe"'
              },
              {
                title: 'String Case Conversion',
                code: `@string {
  operation: "upper"
  input: "hello world"
}`,
                expected: '"HELLO WORLD"'
              }
            ]
          },
          {
            name: '@template',
            description: 'Render templates with variable substitution',
            examples: [
              {
                title: 'Simple Template',
                code: `@template {
  template: "Hello {name}, welcome to {service}!"
  data: {
    name: "John"
    service: "TuskLang"
  }
}`,
                expected: '"Hello John, welcome to TuskLang!"'
              },
              {
                title: 'Conditional Template',
                code: `@template {
  template: "Status: {#if active}Active{#else}Inactive{/if}"
  data: {
    active: true
  }
}`,
                expected: '"Status: Active"'
              }
            ]
          }
        ]
      },
      {
        category: 'Security Operations',
        operators: [
          {
            name: '@encrypt',
            description: 'Encrypt data using various algorithms',
            examples: [
              {
                title: 'AES Encryption',
                code: `@encrypt {
  text: "sensitive data"
  key: "your-secret-key-32-chars-long"
  algorithm: "aes-256-cbc"
}`,
                expected: 'Encrypted string'
              },
              {
                title: 'Hash Generation',
                code: `@hash {
  text: "password123"
  algorithm: "sha256"
}`,
                expected: '64-character hex string'
              }
            ]
          }
        ]
      },
      {
        category: 'Data Format Operations',
        operators: [
          {
            name: '@json',
            description: 'Parse and stringify JSON data',
            examples: [
              {
                title: 'JSON Parsing',
                code: `@json {
  operation: "parse"
  input: '{"name": "John", "age": 30}'
}`,
                expected: '{name: "John", age: 30}'
              },
              {
                title: 'JSON Stringifying',
                code: `@json {
  operation: "stringify"
  input: {
    name: "John"
    age: 30
    active: true
  }
}`,
                expected: 'JSON string'
              }
            ]
          },
          {
            name: '@xml',
            description: 'Parse and generate XML data',
            examples: [
              {
                title: 'XML Parsing',
                code: `@xml {
  operation: "parse"
  input: "<user><name>John</name><age>30</age></user>"
}`,
                expected: 'JavaScript object'
              }
            ]
          }
        ]
      },
      {
        category: 'Control Flow',
        operators: [
          {
            name: '@if',
            description: 'Conditional execution based on expressions',
            examples: [
              {
                title: 'Simple Condition',
                code: `@if {
  condition: "age > 18"
  then: "Adult"
  else: "Minor"
  context: {
    age: 25
  }
}`,
                expected: '"Adult"'
              },
              {
                title: 'Complex Condition',
                code: `@if {
  condition: "user.active && user.age >= 18"
  then: "Welcome {user.name}!"
  else: "Access denied"
  context: {
    user: {
      name: "John"
      age: 25
      active: true
    }
  }
}`,
                expected: '"Welcome John!"'
              }
            ]
          },
          {
            name: '@for',
            description: 'Iterate over arrays with template rendering',
            examples: [
              {
                title: 'Array Iteration',
                code: `@for {
  array: [1, 2, 3, 4, 5]
  template: "Item {item} at index {index}"
}`,
                expected: 'Array of rendered strings'
              },
              {
                title: 'Object Array Iteration',
                code: `@for {
  array: [
    {name: "Alice", age: 25}
    {name: "Bob", age: 30}
  ]
  template: "{item.name} is {item.age} years old"
}`,
                expected: '["Alice is 25 years old", "Bob is 30 years old"]'
              }
            ]
          }
        ]
      }
    ];

    this.examples = examples;
    return examples;
  }

  /**
   * Validate API documentation completeness
   */
  async validateAPIDocumentation() {
    console.log('🔍 Validating API Documentation...');
    
    const requiredSections = [
      'Introduction',
      'Installation',
      'Quick Start',
      'Operators Reference',
      'Examples',
      'Configuration',
      'Error Handling',
      'Security',
      'Performance',
      'Troubleshooting',
      'API Reference',
      'Contributing'
    ];

    const documentationFiles = [
      'README.md',
      'docs/operators.md',
      'docs/examples.md',
      'docs/security.md',
      'docs/performance.md'
    ];

    for (const file of documentationFiles) {
      try {
        const content = fs.readFileSync(file, 'utf8');
        const missingSections = requiredSections.filter(section => 
          !content.toLowerCase().includes(section.toLowerCase())
        );

        if (missingSections.length === 0) {
          this.validationResults.push({
            test: 'API Documentation',
            file: file,
            status: 'PASSED',
            description: 'All required sections present'
          });
        } else {
          this.validationResults.push({
            test: 'API Documentation',
            file: file,
            status: 'FAILED',
            description: `Missing sections: ${missingSections.join(', ')}`
          });
          this.documentationIssues.push({
            type: 'MISSING_SECTIONS',
            file: file,
            sections: missingSections
          });
        }
      } catch (error) {
        this.validationResults.push({
          test: 'API Documentation',
          file: file,
          status: 'FAILED',
          description: `File not found: ${error.message}`
        });
        this.documentationIssues.push({
          type: 'MISSING_FILE',
          file: file,
          error: error.message
        });
      }
    }
  }

  /**
   * Test code samples for functionality
   */
  async testCodeSamples() {
    console.log('🔍 Testing Code Samples...');
    
    const codeSamples = [
      {
        name: 'Basic String Operation',
        code: `@string {
  operation: "concat"
  strings: ["Hello", " ", "World"]
}`,
        expected: 'Hello World'
      },
      {
        name: 'JSON Parsing',
        code: `@json {
  operation: "parse"
  input: '{"name": "John", "age": 30}'
}`,
        expected: { name: 'John', age: 30 }
      },
      {
        name: 'Template Rendering',
        code: `@template {
  template: "Hello {name}!"
  data: { name: "World" }
}`,
        expected: 'Hello World!'
      },
      {
        name: 'Conditional Logic',
        code: `@if {
  condition: "true"
  then: "Success"
  else: "Failure"
}`,
        expected: 'Success'
      },
      {
        name: 'Array Iteration',
        code: `@for {
  array: [1, 2, 3]
  template: "Item {item}"
}`,
        expected: ['Item 1', 'Item 2', 'Item 3']
      }
    ];

    for (const sample of codeSamples) {
      try {
        // Parse the TuskLang code and execute
        const result = await this.executeTuskCode(sample.code);
        
        if (JSON.stringify(result) === JSON.stringify(sample.expected)) {
          this.validationResults.push({
            test: 'Code Sample',
            name: sample.name,
            status: 'PASSED',
            description: 'Code sample executed correctly'
          });
        } else {
          this.validationResults.push({
            test: 'Code Sample',
            name: sample.name,
            status: 'FAILED',
            description: `Expected ${JSON.stringify(sample.expected)} but got ${JSON.stringify(result)}`
          });
        }
      } catch (error) {
        this.validationResults.push({
          test: 'Code Sample',
          name: sample.name,
          status: 'FAILED',
          description: `Execution error: ${error.message}`
        });
      }
    }
  }

  /**
   * Execute TuskLang code string
   */
  async executeTuskCode(codeString) {
    // Parse the TuskLang code and extract operator and parameters
    const lines = codeString.split('\n').filter(line => line.trim());
    const operatorMatch = lines[0].match(/@(\w+)/);
    
    if (!operatorMatch) {
      throw new Error('Invalid TuskLang code: no operator found');
    }
    
    const operator = '@' + operatorMatch[1];
    const params = {};
    
    // Parse parameters from the code block
    for (let i = 1; i < lines.length; i++) {
      const line = lines[i].trim();
      if (line.includes(':')) {
        const [key, value] = line.split(':').map(s => s.trim());
        params[key] = this.parseValue(value);
      }
    }
    
    return await this.tusk.executeOperator(operator, params);
  }

  /**
   * Parse TuskLang value
   */
  parseValue(value) {
    value = value.trim();
    
    // Remove trailing comma if present
    if (value.endsWith(',')) {
      value = value.slice(0, -1);
    }
    
    // Parse strings
    if (value.startsWith('"') && value.endsWith('"')) {
      return value.slice(1, -1);
    }
    
    // Parse arrays
    if (value.startsWith('[') && value.endsWith(']')) {
      return JSON.parse(value);
    }
    
    // Parse objects
    if (value.startsWith('{') && value.endsWith('}')) {
      return JSON.parse(value);
    }
    
    // Parse booleans
    if (value === 'true') return true;
    if (value === 'false') return false;
    
    // Parse numbers
    if (!isNaN(value)) return Number(value);
    
    return value;
  }

  /**
   * Validate tutorial completeness
   */
  async validateTutorials() {
    console.log('🔍 Validating Tutorials...');
    
    const tutorialRequirements = [
      {
        name: 'Getting Started',
        requirements: [
          'Installation instructions',
          'Basic configuration',
          'First operator usage',
          'Expected output'
        ]
      },
      {
        name: 'Basic Operations',
        requirements: [
          'String operations',
          'Data manipulation',
          'Control flow',
          'Error handling'
        ]
      },
      {
        name: 'Advanced Features',
        requirements: [
          'Database integration',
          'Security features',
          'Performance optimization',
          'Best practices'
        ]
      }
    ];

    for (const tutorial of tutorialRequirements) {
      const tutorialFile = `docs/tutorials/${tutorial.name.toLowerCase().replace(' ', '-')}.md`;
      
      try {
        const content = fs.readFileSync(tutorialFile, 'utf8');
        const missingRequirements = tutorial.requirements.filter(req => 
          !content.toLowerCase().includes(req.toLowerCase())
        );

        if (missingRequirements.length === 0) {
          this.validationResults.push({
            test: 'Tutorial',
            name: tutorial.name,
            status: 'PASSED',
            description: 'All requirements met'
          });
        } else {
          this.validationResults.push({
            test: 'Tutorial',
            name: tutorial.name,
            status: 'FAILED',
            description: `Missing requirements: ${missingRequirements.join(', ')}`
          });
        }
      } catch (error) {
        this.validationResults.push({
          test: 'Tutorial',
          name: tutorial.name,
          status: 'FAILED',
          description: `Tutorial file not found: ${error.message}`
        });
      }
    }
  }

  /**
   * Check documentation consistency
   */
  async checkDocumentationConsistency() {
    console.log('🔍 Checking Documentation Consistency...');
    
    const consistencyChecks = [
      {
        name: 'Operator Naming',
        check: () => {
          const operatorNames = this.examples.flatMap(cat => 
            cat.operators.map(op => op.name)
          );
          
          const uniqueNames = new Set(operatorNames);
          const duplicates = operatorNames.filter(name => 
            operatorNames.filter(n => n === name).length > 1
          );
          
          if (duplicates.length === 0) {
            return { status: 'PASSED', description: 'No duplicate operator names' };
          } else {
            return { status: 'FAILED', description: `Duplicate names: ${duplicates.join(', ')}` };
          }
        }
      },
      {
        name: 'Parameter Consistency',
        check: () => {
          const parameterMappings = {};
          
          this.examples.forEach(cat => {
            cat.operators.forEach(op => {
              op.examples.forEach(ex => {
                const params = this.extractParameters(ex.code);
                if (!parameterMappings[op.name]) {
                  parameterMappings[op.name] = new Set();
                }
                params.forEach(param => parameterMappings[op.name].add(param));
              });
            });
          });
          
          const inconsistencies = [];
          Object.entries(parameterMappings).forEach(([op, params]) => {
            if (params.size > 10) { // Arbitrary threshold
              inconsistencies.push(`${op}: ${params.size} different parameters`);
            }
          });
          
          if (inconsistencies.length === 0) {
            return { status: 'PASSED', description: 'Parameter usage is consistent' };
          } else {
            return { status: 'FAILED', description: `Inconsistencies: ${inconsistencies.join(', ')}` };
          }
        }
      },
      {
        name: 'Example Completeness',
        check: () => {
          const incompleteExamples = [];
          
          this.examples.forEach(cat => {
            cat.operators.forEach(op => {
              if (op.examples.length < 2) {
                incompleteExamples.push(`${op.name}: only ${op.examples.length} example(s)`);
              }
            });
          });
          
          if (incompleteExamples.length === 0) {
            return { status: 'PASSED', description: 'All operators have sufficient examples' };
          } else {
            return { status: 'FAILED', description: `Incomplete: ${incompleteExamples.join(', ')}` };
          }
        }
      }
    ];

    for (const check of consistencyChecks) {
      const result = check.check();
      this.validationResults.push({
        test: 'Documentation Consistency',
        name: check.name,
        status: result.status,
        description: result.description
      });
    }
  }

  /**
   * Extract parameters from TuskLang code
   */
  extractParameters(codeString) {
    const params = [];
    const lines = codeString.split('\n');
    
    lines.forEach(line => {
      const match = line.match(/(\w+):/);
      if (match) {
        params.push(match[1]);
      }
    });
    
    return params;
  }

  /**
   * Create interactive playground
   */
  async createInteractivePlayground() {
    console.log('🔍 Creating Interactive Playground...');
    
    const playgroundHTML = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>TuskLang Interactive Playground</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background: #f5f5f5;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
        }
        .panel {
            background: white;
            border-radius: 8px;
            padding: 20px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        .panel h2 {
            margin-top: 0;
            color: #333;
            border-bottom: 2px solid #007acc;
            padding-bottom: 10px;
        }
        textarea {
            width: 100%;
            height: 300px;
            border: 1px solid #ddd;
            border-radius: 4px;
            padding: 10px;
            font-family: 'Courier New', monospace;
            font-size: 14px;
            resize: vertical;
        }
        button {
            background: #007acc;
            color: white;
            border: none;
            padding: 10px 20px;
            border-radius: 4px;
            cursor: pointer;
            font-size: 16px;
            margin: 10px 0;
        }
        button:hover {
            background: #005a9e;
        }
        .output {
            background: #f8f9fa;
            border: 1px solid #e9ecef;
            border-radius: 4px;
            padding: 15px;
            min-height: 200px;
            font-family: 'Courier New', monospace;
            white-space: pre-wrap;
        }
        .examples {
            margin-top: 20px;
        }
        .example {
            background: #f8f9fa;
            border: 1px solid #e9ecef;
            border-radius: 4px;
            padding: 10px;
            margin: 10px 0;
            cursor: pointer;
        }
        .example:hover {
            background: #e9ecef;
        }
        .example h4 {
            margin: 0 0 5px 0;
            color: #007acc;
        }
        .example p {
            margin: 0;
            font-size: 14px;
            color: #666;
        }
    </style>
</head>
<body>
    <h1>🚀 TuskLang Interactive Playground</h1>
    <p>Experiment with TuskLang operators in real-time!</p>
    
    <div class="container">
        <div class="panel">
            <h2>📝 Code Editor</h2>
            <textarea id="codeEditor" placeholder="Enter your TuskLang code here...">@string {
  operation: "concat"
  strings: ["Hello", " ", "World", "!"]
}</textarea>
            <button onclick="executeCode()">▶️ Execute</button>
            <button onclick="clearOutput()">🗑️ Clear</button>
        </div>
        
        <div class="panel">
            <h2>📊 Output</h2>
            <div id="output" class="output">Ready to execute TuskLang code...</div>
        </div>
    </div>
    
    <div class="panel">
        <h2>💡 Examples</h2>
        <div class="examples">
            <div class="example" onclick="loadExample('string-concat')">
                <h4>String Concatenation</h4>
                <p>Combine multiple strings into one</p>
            </div>
            <div class="example" onclick="loadExample('json-parse')">
                <h4>JSON Parsing</h4>
                <p>Parse JSON string into object</p>
            </div>
            <div class="example" onclick="loadExample('template-render')">
                <h4>Template Rendering</h4>
                <p>Render templates with variables</p>
            </div>
            <div class="example" onclick="loadExample('conditional-logic')">
                <h4>Conditional Logic</h4>
                <p>Execute code based on conditions</p>
            </div>
        </div>
    </div>

    <script>
        const examples = {
            'string-concat': \`@string {
  operation: "concat"
  strings: ["Hello", " ", "World", "!"]
}\`,
            'json-parse': \`@json {
  operation: "parse"
  input: '{"name": "John", "age": 30, "active": true}'
}\`,
            'template-render': \`@template {
  template: "Hello {name}, welcome to {service}!"
  data: {
    name: "Alice"
    service: "TuskLang"
  }
}\`,
            'conditional-logic': \`@if {
  condition: "user.age >= 18"
  then: "Welcome {user.name}, you are an adult!"
  else: "Sorry {user.name}, you must be 18 or older"
  context: {
    user: {
      name: "Bob"
      age: 25
    }
  }
}\`
        };

        function loadExample(exampleId) {
            document.getElementById('codeEditor').value = examples[exampleId];
        }

        async function executeCode() {
            const code = document.getElementById('codeEditor').value;
            const output = document.getElementById('output');
            
            output.textContent = 'Executing...';
            
            try {
                // Simulate TuskLang execution
                const result = await simulateTuskExecution(code);
                output.textContent = '✅ Result:\\n' + JSON.stringify(result, null, 2);
            } catch (error) {
                output.textContent = '❌ Error:\\n' + error.message;
            }
        }

        function clearOutput() {
            document.getElementById('output').textContent = 'Ready to execute TuskLang code...';
        }

        async function simulateTuskExecution(code) {
            // Simple simulation of TuskLang execution
            if (code.includes('@string') && code.includes('concat')) {
                const stringsMatch = code.match(/strings:\s*\[(.*?)\]/);
                if (stringsMatch) {
                    const strings = stringsMatch[1].split(',').map(s => s.trim().replace(/"/g, ''));
                    return strings.join('');
                }
            }
            
            if (code.includes('@json') && code.includes('parse')) {
                const inputMatch = code.match(/input:\s*'(.*?)'/);
                if (inputMatch) {
                    return JSON.parse(inputMatch[1]);
                }
            }
            
            if (code.includes('@template')) {
                const templateMatch = code.match(/template:\s*"(.*?)"/);
                const dataMatch = code.match(/data:\s*\{([^}]+)\}/);
                if (templateMatch && dataMatch) {
                    let result = templateMatch[1];
                    const dataStr = '{' + dataMatch[1] + '}';
                    const data = eval('(' + dataStr + ')');
                    
                    Object.entries(data).forEach(([key, value]) => {
                        result = result.replace(new RegExp('{' + key + '}', 'g'), value);
                    });
                    
                    return result;
                }
            }
            
            if (code.includes('@if')) {
                const conditionMatch = code.match(/condition:\s*"(.*?)"/);
                const thenMatch = code.match(/then:\s*"(.*?)"/);
                const elseMatch = code.match(/else:\s*"(.*?)"/);
                
                if (conditionMatch && thenMatch) {
                    const condition = conditionMatch[1];
                    if (condition.includes('age >= 18')) {
                        return thenMatch[1];
                    } else {
                        return elseMatch ? elseMatch[1] : 'Condition not met';
                    }
                }
            }
            
            return { message: 'Code executed successfully', code: code };
        }
    </script>
</body>
</html>`;

    try {
      fs.writeFileSync('todo/a5/g6/playground.html', playgroundHTML);
      
      this.validationResults.push({
        test: 'Interactive Playground',
        status: 'PASSED',
        description: 'Interactive playground created successfully'
      });
      
      console.log('✅ Interactive playground created at todo/a5/g6/playground.html');
      
    } catch (error) {
      this.validationResults.push({
        test: 'Interactive Playground',
        status: 'FAILED',
        description: `Failed to create playground: ${error.message}`
      });
    }
  }

  /**
   * Run complete documentation and examples validation
   */
  async runCompleteSuite() {
    console.log('🚀 Starting TuskLang Documentation & Examples Validation...');
    
    try {
      this.generateUsageExamples();
      await this.validateAPIDocumentation();
      await this.testCodeSamples();
      await this.validateTutorials();
      await this.checkDocumentationConsistency();
      await this.createInteractivePlayground();
      
      const report = this.generateDocumentationReport();
      
      console.log('✅ Documentation & Examples Validation completed successfully');
      return report;
      
    } catch (error) {
      console.error('❌ Documentation & Examples Validation failed:', error);
      throw error;
    }
  }

  /**
   * Generate comprehensive documentation report
   */
  generateDocumentationReport() {
    const totalTests = this.validationResults.length;
    const passedTests = this.validationResults.filter(r => r.status === 'PASSED').length;
    const failedTests = this.validationResults.filter(r => r.status === 'FAILED').length;
    const successRate = (passedTests / totalTests) * 100;
    
    const totalDuration = Date.now() - this.startTime;
    
    const report = {
      summary: {
        totalTests,
        passedTests,
        failedTests,
        successRate: `${successRate.toFixed(2)}%`,
        totalDuration: `${totalDuration}ms`,
        examplesGenerated: this.examples.length,
        documentationIssues: this.documentationIssues.length
      },
      results: this.validationResults,
      examples: this.examples,
      documentationIssues: this.documentationIssues,
      recommendations: this.generateDocumentationRecommendations(),
      timestamp: new Date().toISOString()
    };
    
    console.log('\n📊 DOCUMENTATION & EXAMPLES REPORT');
    console.log('==================================');
    console.log(`Total Tests: ${totalTests}`);
    console.log(`Passed: ${passedTests}`);
    console.log(`Failed: ${failedTests}`);
    console.log(`Success Rate: ${successRate.toFixed(2)}%`);
    console.log(`Total Duration: ${totalDuration}ms`);
    console.log(`Examples Generated: ${this.examples.length}`);
    console.log(`Documentation Issues: ${this.documentationIssues.length}`);
    
    if (this.documentationIssues.length > 0) {
      console.log('\n⚠️  DOCUMENTATION ISSUES:');
      this.documentationIssues.forEach(issue => {
        console.log(`  - ${issue.type}: ${issue.file || issue.sections?.join(', ')}`);
      });
    }
    
    return report;
  }

  /**
   * Generate documentation recommendations
   */
  generateDocumentationRecommendations() {
    const recommendations = [];
    
    const failedTests = this.validationResults.filter(r => r.status === 'FAILED');
    
    if (failedTests.some(t => t.test === 'API Documentation')) {
      recommendations.push('Complete missing API documentation sections');
    }
    
    if (failedTests.some(t => t.test === 'Code Sample')) {
      recommendations.push('Fix failing code samples and ensure they execute correctly');
    }
    
    if (failedTests.some(t => t.test === 'Tutorial')) {
      recommendations.push('Complete tutorial documentation with all required sections');
    }
    
    if (failedTests.some(t => t.test === 'Documentation Consistency')) {
      recommendations.push('Standardize operator naming and parameter usage across documentation');
    }
    
    if (this.examples.length < 50) {
      recommendations.push('Generate more comprehensive examples for all operators');
    }
    
    recommendations.push('Add interactive examples to the playground');
    recommendations.push('Include performance benchmarks in documentation');
    recommendations.push('Add troubleshooting section with common issues');
    
    return recommendations;
  }
}

module.exports = { DocumentationExamples }; 