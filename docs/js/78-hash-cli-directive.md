# #cli Directive - Command Line Interface

## Overview
The `#cli` directive in TuskLang provides comprehensive command line interface configuration capabilities, enabling you to define CLI commands, subcommands, options, arguments, help text, and interactive prompts in a declarative manner.

## TuskLang Syntax

### Basic CLI Command
```tusk
#cli {
  name: "myapp",
  version: "1.0.0",
  description: "A powerful command line application",
  commands: {
    "serve": {
      description: "Start the server",
      options: {
        "--port": {
          type: "number",
          default: 3000,
          description: "Port to run the server on"
        },
        "--host": {
          type: "string",
          default: "localhost",
          description: "Host to bind to"
        }
      }
    }
  }
}
```

### CLI with Subcommands
```tusk
#cli {
  name: "docker",
  commands: {
    "container": {
      description: "Manage containers",
      subcommands: {
        "run": {
          description: "Run a container",
          arguments: ["image"],
          options: {
            "--name": {
              type: "string",
              description: "Container name"
            },
            "--detach": {
              type: "boolean",
              description: "Run in background"
            }
          }
        },
        "stop": {
          description: "Stop a container",
          arguments: ["container"],
          options: {
            "--time": {
              type: "number",
              default: 10,
              description: "Timeout in seconds"
            }
          }
        }
      }
    }
  }
}
```

### Interactive CLI
```tusk
#cli {
  name: "wizard",
  interactive: true,
  prompts: {
    "project_name": {
      type: "input",
      message: "What is your project name?",
      default: "my-project",
      validate: "required"
    },
    "framework": {
      type: "select",
      message: "Choose a framework:",
      choices: ["react", "vue", "angular", "svelte"]
    },
    "database": {
      type: "confirm",
      message: "Do you want to include a database?",
      default: true
    }
  }
}
```

## JavaScript Integration

### Node.js CLI Application
```javascript
const tusklang = require('@tusklang/core');
const { Command } = require('commander');
const inquirer = require('inquirer');

const config = tusklang.parse(`
cli_config: #cli {
  name: "myapp",
  version: "1.0.0",
  description: "A powerful command line application",
  commands: {
    "serve": {
      description: "Start the server",
      handler: "serverController.start",
      options: {
        "--port": {
          type: "number",
          default: 3000,
          description: "Port to run the server on"
        },
        "--host": {
          type: "string",
          default: "localhost",
          description: "Host to bind to"
        },
        "--debug": {
          type: "boolean",
          description: "Enable debug mode"
        }
      }
    },
    "build": {
      description: "Build the application",
      handler: "buildController.build",
      options: {
        "--output": {
          type: "string",
          default: "dist",
          description: "Output directory"
        },
        "--minify": {
          type: "boolean",
          description: "Minify output"
        }
      }
    },
    "deploy": {
      description: "Deploy the application",
      handler: "deployController.deploy",
      arguments: ["environment"],
      options: {
        "--force": {
          type: "boolean",
          description: "Force deployment"
        }
      }
    }
  }
}
`);

class CliApplication {
  constructor(config) {
    this.config = config.cli_config;
    this.program = new Command();
    this.handlers = new Map();
    this.initializeCli();
  }

  initializeCli() {
    // Set up basic program information
    this.program
      .name(this.config.name)
      .version(this.config.version)
      .description(this.config.description);

    // Set up commands
    this.setupCommands();

    // Set up global options
    this.setupGlobalOptions();

    // Set up error handling
    this.setupErrorHandling();
  }

  setupCommands() {
    if (!this.config.commands) return;

    Object.entries(this.config.commands).forEach(([name, commandConfig]) => {
      this.createCommand(name, commandConfig);
    });
  }

  createCommand(name, config) {
    const command = this.program
      .command(name)
      .description(config.description);

    // Add arguments
    if (config.arguments) {
      config.arguments.forEach(arg => {
        command.argument(arg);
      });
    }

    // Add options
    if (config.options) {
      Object.entries(config.options).forEach(([option, optionConfig]) => {
        this.addOption(command, option, optionConfig);
      });
    }

    // Add action
    if (config.handler) {
      command.action((...args) => {
        this.executeHandler(config.handler, args, command.opts());
      });
    }

    // Add subcommands
    if (config.subcommands) {
      Object.entries(config.subcommands).forEach(([subName, subConfig]) => {
        this.createSubcommand(command, subName, subConfig);
      });
    }
  }

  createSubcommand(parentCommand, name, config) {
    const subcommand = parentCommand
      .command(name)
      .description(config.description);

    // Add arguments
    if (config.arguments) {
      config.arguments.forEach(arg => {
        subcommand.argument(arg);
      });
    }

    // Add options
    if (config.options) {
      Object.entries(config.options).forEach(([option, optionConfig]) => {
        this.addOption(subcommand, option, optionConfig);
      });
    }

    // Add action
    if (config.handler) {
      subcommand.action((...args) => {
        this.executeHandler(config.handler, args, subcommand.opts());
      });
    }
  }

  addOption(command, option, config) {
    const optionName = option.replace(/^--/, '');
    const optionString = config.type === 'boolean' ? option : `${option} <value>`;
    
    command.option(optionString, config.description, config.default);
  }

  setupGlobalOptions() {
    this.program
      .option('--verbose', 'Enable verbose output')
      .option('--quiet', 'Suppress output')
      .option('--config <path>', 'Path to config file');
  }

  setupErrorHandling() {
    this.program.exitOverride();

    process.on('unhandledRejection', (error) => {
      console.error('Unhandled rejection:', error);
      process.exit(1);
    });

    process.on('uncaughtException', (error) => {
      console.error('Uncaught exception:', error);
      process.exit(1);
    });
  }

  registerHandler(name, handler) {
    this.handlers.set(name, handler);
  }

  async executeHandler(handlerName, args, options) {
    const handler = this.handlers.get(handlerName);
    
    if (!handler) {
      console.error(`Handler '${handlerName}' not found`);
      process.exit(1);
    }

    try {
      await handler(args, options);
    } catch (error) {
      console.error('Command execution failed:', error.message);
      process.exit(1);
    }
  }

  async run(argv = process.argv) {
    try {
      await this.program.parseAsync(argv);
    } catch (error) {
      if (error.code === 'commander.help') {
        // Help was displayed, exit normally
        process.exit(0);
      } else {
        console.error('CLI execution failed:', error.message);
        process.exit(1);
      }
    }
  }

  // Interactive CLI support
  async runInteractive() {
    if (!this.config.interactive) {
      console.error('Interactive mode not configured');
      return;
    }

    const answers = await this.runPrompts();
    await this.processInteractiveAnswers(answers);
  }

  async runPrompts() {
    const questions = [];

    if (this.config.prompts) {
      Object.entries(this.config.prompts).forEach(([name, promptConfig]) => {
        questions.push(this.createPrompt(name, promptConfig));
      });
    }

    return await inquirer.prompt(questions);
  }

  createPrompt(name, config) {
    const prompt = {
      name: name,
      message: config.message,
      type: config.type || 'input'
    };

    if (config.default !== undefined) {
      prompt.default = config.default;
    }

    if (config.choices) {
      prompt.choices = config.choices;
    }

    if (config.validate) {
      prompt.validate = this.createValidator(config.validate);
    }

    return prompt;
  }

  createValidator(validation) {
    if (validation === 'required') {
      return (input) => {
        if (!input || input.trim() === '') {
          return 'This field is required';
        }
        return true;
      };
    }

    if (typeof validation === 'function') {
      return validation;
    }

    return () => true;
  }

  async processInteractiveAnswers(answers) {
    console.log('Processing answers:', answers);
    
    // Process the answers based on your application logic
    if (answers.project_name) {
      console.log(`Creating project: ${answers.project_name}`);
    }
    
    if (answers.framework) {
      console.log(`Using framework: ${answers.framework}`);
    }
    
    if (answers.database) {
      console.log('Setting up database...');
    }
  }
}

// Handler implementations
class ServerController {
  static async start(args, options) {
    const { port, host, debug } = options;
    
    console.log(`Starting server on ${host}:${port}`);
    if (debug) {
      console.log('Debug mode enabled');
    }
    
    // Simulate server start
    await new Promise(resolve => setTimeout(resolve, 1000));
    console.log('Server started successfully');
  }
}

class BuildController {
  static async build(args, options) {
    const { output, minify } = options;
    
    console.log(`Building application to ${output}`);
    if (minify) {
      console.log('Minification enabled');
    }
    
    // Simulate build process
    await new Promise(resolve => setTimeout(resolve, 2000));
    console.log('Build completed successfully');
  }
}

class DeployController {
  static async deploy(args, options) {
    const [environment] = args;
    const { force } = options;
    
    console.log(`Deploying to ${environment}`);
    if (force) {
      console.log('Force deployment enabled');
    }
    
    // Simulate deployment
    await new Promise(resolve => setTimeout(resolve, 3000));
    console.log('Deployment completed successfully');
  }
}

// Usage
const cliApp = new CliApplication(config);

// Register handlers
cliApp.registerHandler('serverController.start', ServerController.start);
cliApp.registerHandler('buildController.build', BuildController.build);
cliApp.registerHandler('deployController.deploy', DeployController.deploy);

// Run the CLI
if (require.main === module) {
  cliApp.run();
}
```

### Interactive CLI Application
```javascript
const interactiveConfig = tusklang.parse(`
interactive_cli: #cli {
  name: "wizard",
  interactive: true,
  description: "Interactive project setup wizard",
  prompts: {
    "project_name": {
      type: "input",
      message: "What is your project name?",
      default: "my-project",
      validate: "required"
    },
    "framework": {
      type: "list",
      message: "Choose a framework:",
      choices: [
        { name: "React", value: "react" },
        { name: "Vue.js", value: "vue" },
        { name: "Angular", value: "angular" },
        { name: "Svelte", value: "svelte" }
      ]
    },
    "database": {
      type: "confirm",
      message: "Do you want to include a database?",
      default: true
    },
    "database_type": {
      type: "list",
      message: "Choose a database:",
      choices: ["postgresql", "mysql", "mongodb", "sqlite"],
      when: (answers) => answers.database
    },
    "features": {
      type: "checkbox",
      message: "Select additional features:",
      choices: [
        { name: "Authentication", value: "auth" },
        { name: "API Documentation", value: "docs" },
        { name: "Testing", value: "testing" },
        { name: "Docker", value: "docker" }
      ]
    }
  }
}
`);

class InteractiveCli {
  constructor(config) {
    this.config = config.interactive_cli;
    this.templates = new Map();
    this.initializeTemplates();
  }

  initializeTemplates() {
    // Register project templates
    this.templates.set('react', {
      files: ['package.json', 'src/App.js', 'public/index.html'],
      dependencies: ['react', 'react-dom']
    });

    this.templates.set('vue', {
      files: ['package.json', 'src/App.vue', 'public/index.html'],
      dependencies: ['vue']
    });

    this.templates.set('angular', {
      files: ['package.json', 'src/app/app.component.ts', 'src/index.html'],
      dependencies: ['@angular/core', '@angular/common']
    });

    this.templates.set('svelte', {
      files: ['package.json', 'src/App.svelte', 'public/index.html'],
      dependencies: ['svelte']
    });
  }

  async run() {
    console.log(`Welcome to ${this.config.name}!`);
    console.log(this.config.description);
    console.log('');

    const answers = await this.runPrompts();
    await this.processAnswers(answers);
  }

  async runPrompts() {
    const questions = [];

    Object.entries(this.config.prompts).forEach(([name, promptConfig]) => {
      const question = this.createQuestion(name, promptConfig);
      questions.push(question);
    });

    return await inquirer.prompt(questions);
  }

  createQuestion(name, config) {
    const question = {
      name: name,
      message: config.message,
      type: config.type || 'input'
    };

    if (config.default !== undefined) {
      question.default = config.default;
    }

    if (config.choices) {
      question.choices = config.choices;
    }

    if (config.when) {
      question.when = config.when;
    }

    if (config.validate) {
      question.validate = this.createValidator(config.validate);
    }

    return question;
  }

  createValidator(validation) {
    if (validation === 'required') {
      return (input) => {
        if (!input || input.trim() === '') {
          return 'This field is required';
        }
        return true;
      };
    }

    return () => true;
  }

  async processAnswers(answers) {
    console.log('\nProcessing your selections...\n');

    // Create project structure
    await this.createProject(answers);

    // Install dependencies
    await this.installDependencies(answers);

    // Setup additional features
    await this.setupFeatures(answers);

    console.log('\n🎉 Project setup completed successfully!');
    console.log(`\nNext steps:`);
    console.log(`  cd ${answers.project_name}`);
    console.log(`  npm start`);
  }

  async createProject(answers) {
    console.log(`📁 Creating project: ${answers.project_name}`);
    
    // Simulate project creation
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    const template = this.templates.get(answers.framework);
    if (template) {
      console.log(`📄 Creating ${template.files.length} files...`);
      await new Promise(resolve => setTimeout(resolve, 500));
    }
  }

  async installDependencies(answers) {
    console.log('📦 Installing dependencies...');
    
    const dependencies = [];
    
    // Add framework dependencies
    const template = this.templates.get(answers.framework);
    if (template) {
      dependencies.push(...template.dependencies);
    }
    
    // Add database dependencies
    if (answers.database && answers.database_type) {
      dependencies.push(this.getDatabaseDependency(answers.database_type));
    }
    
    // Add feature dependencies
    if (answers.features) {
      answers.features.forEach(feature => {
        const featureDeps = this.getFeatureDependencies(feature);
        dependencies.push(...featureDeps);
      });
    }
    
    // Simulate dependency installation
    await new Promise(resolve => setTimeout(resolve, 2000));
    console.log(`✅ Installed ${dependencies.length} dependencies`);
  }

  getDatabaseDependency(dbType) {
    const deps = {
      postgresql: 'pg',
      mysql: 'mysql2',
      mongodb: 'mongodb',
      sqlite: 'sqlite3'
    };
    return deps[dbType] || 'sqlite3';
  }

  getFeatureDependencies(feature) {
    const deps = {
      auth: ['passport', 'jsonwebtoken'],
      docs: ['swagger-ui-express', 'swagger-jsdoc'],
      testing: ['jest', 'supertest'],
      docker: ['docker-compose']
    };
    return deps[feature] || [];
  }

  async setupFeatures(answers) {
    if (!answers.features || answers.features.length === 0) {
      return;
    }

    console.log('🔧 Setting up additional features...');
    
    for (const feature of answers.features) {
      console.log(`  Setting up ${feature}...`);
      await new Promise(resolve => setTimeout(resolve, 500));
    }
  }
}

// Usage
const interactiveCli = new InteractiveCli(interactiveConfig);

if (require.main === module) {
  interactiveCli.run().catch(console.error);
}
```

## Advanced Usage Scenarios

### Multi-Command CLI
```tusk
#cli {
  name: "git",
  commands: {
    "init": {
      description: "Initialize a git repository",
      handler: "gitController.init"
    },
    "add": {
      description: "Add files to staging",
      arguments: ["files"],
      handler: "gitController.add"
    },
    "commit": {
      description: "Commit changes",
      options: {
        "--message": {
          type: "string",
          description: "Commit message"
        }
      },
      handler: "gitController.commit"
    },
    "push": {
      description: "Push to remote",
      arguments: ["remote", "branch"],
      handler: "gitController.push"
    }
  }
}
```

### Plugin System CLI
```tusk
#cli {
  name: "plugin-manager",
  commands: {
    "install": {
      description: "Install a plugin",
      arguments: ["plugin-name"],
      options: {
        "--version": {
          type: "string",
          description: "Plugin version"
        }
      }
    },
    "list": {
      description: "List installed plugins",
      options: {
        "--json": {
          type: "boolean",
          description: "Output as JSON"
        }
      }
    },
    "remove": {
      description: "Remove a plugin",
      arguments: ["plugin-name"]
    }
  }
}
```

### Configuration CLI
```tusk
#cli {
  name: "config",
  commands: {
    "get": {
      description: "Get configuration value",
      arguments: ["key"],
      handler: "configController.get"
    },
    "set": {
      description: "Set configuration value",
      arguments: ["key", "value"],
      handler: "configController.set"
    },
    "list": {
      description: "List all configuration",
      handler: "configController.list"
    },
    "reset": {
      description: "Reset configuration to defaults",
      handler: "configController.reset"
    }
  }
}
```

## TypeScript Implementation

### Typed CLI Application
```typescript
interface CliConfig {
  name: string;
  version: string;
  description: string;
  commands?: Record<string, CommandConfig>;
  interactive?: boolean;
  prompts?: Record<string, PromptConfig>;
}

interface CommandConfig {
  description: string;
  handler?: string;
  arguments?: string[];
  options?: Record<string, OptionConfig>;
  subcommands?: Record<string, CommandConfig>;
}

interface OptionConfig {
  type: 'string' | 'number' | 'boolean';
  description: string;
  default?: any;
}

interface PromptConfig {
  type: 'input' | 'list' | 'confirm' | 'checkbox';
  message: string;
  default?: any;
  choices?: any[];
  validate?: string | Function;
  when?: Function;
}

class TypedCliApplication {
  private config: CliConfig;
  private handlers: Map<string, Function> = new Map();

  constructor(config: CliConfig) {
    this.config = config;
    this.initializeCli();
  }

  private initializeCli(): void {
    // Implementation for CLI initialization
  }

  registerHandler(name: string, handler: Function): void {
    this.handlers.set(name, handler);
  }

  async run(argv: string[] = process.argv): Promise<void> {
    // Implementation for running CLI
  }
}
```

## Real-World Examples

### Package Manager CLI
```javascript
// Package manager CLI with TuskLang configuration
const config = tusklang.parse(`
package_cli: #cli {
  name: "pkg",
  version: "1.0.0",
  commands: {
    "install": {
      description: "Install packages",
      arguments: ["packages"],
      options: {
        "--save": {
          type: "boolean",
          description: "Save to dependencies"
        },
        "--dev": {
          type: "boolean",
          description: "Save to devDependencies"
        }
      }
    },
    "uninstall": {
      description: "Uninstall packages",
      arguments: ["packages"]
    },
    "update": {
      description: "Update packages",
      arguments: ["packages"]
    }
  }
}
`);

const cliApp = new CliApplication(config);

// Register handlers
cliApp.registerHandler('install', async (args, options) => {
  const packages = args[0].split(',');
  console.log(`Installing: ${packages.join(', ')}`);
  if (options.save) console.log('Saving to dependencies');
  if (options.dev) console.log('Saving to devDependencies');
});

cliApp.registerHandler('uninstall', async (args) => {
  const packages = args[0].split(',');
  console.log(`Uninstalling: ${packages.join(', ')}`);
});

cliApp.registerHandler('update', async (args) => {
  const packages = args[0] ? args[0].split(',') : ['all'];
  console.log(`Updating: ${packages.join(', ')}`);
});

cliApp.run();
```

### Development CLI
```javascript
// Development CLI with multiple commands
const devConfig = tusklang.parse(`
dev_cli: #cli {
  name: "dev",
  commands: {
    "start": {
      description: "Start development server",
      options: {
        "--port": {
          type: "number",
          default: 3000,
          description: "Development server port"
        }
      }
    },
    "build": {
      description: "Build for production",
      options: {
        "--optimize": {
          type: "boolean",
          description: "Enable optimization"
        }
      }
    },
    "test": {
      description: "Run tests",
      options: {
        "--watch": {
          type: "boolean",
          description: "Watch mode"
        }
      }
    }
  }
}
`);

const devCli = new CliApplication(devConfig);

devCli.registerHandler('start', async (args, options) => {
  console.log(`Starting dev server on port ${options.port}`);
  // Start development server
});

devCli.registerHandler('build', async (args, options) => {
  console.log('Building for production...');
  if (options.optimize) console.log('Optimization enabled');
  // Build process
});

devCli.registerHandler('test', async (args, options) => {
  console.log('Running tests...');
  if (options.watch) console.log('Watch mode enabled');
  // Test execution
});

devCli.run();
```

## Performance Considerations
- Use async/await for command handlers
- Implement command caching for frequently used commands
- Use streaming for large output
- Optimize help text generation

## Security Notes
- Validate all user inputs
- Sanitize file paths and arguments
- Implement proper error handling
- Use secure defaults for sensitive options

## Best Practices
- Provide clear and helpful error messages
- Use consistent command naming conventions
- Implement proper help text for all commands
- Use TypeScript for type safety

## Related Topics
- [@shell Operator](./62-shell-operator.md) - Shell command execution
- [@exec Operator](./63-exec-operator.md) - External command execution
- [Scripting](./16-scripting.md) - Script automation
- [Deployment](./17-deployment.md) - Deployment automation 