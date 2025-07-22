#!/usr/bin/env node

/**
 * TuskLang Development Environment Setup
 * Automated setup tool for TuskLang development environments
 */

const fs = require('fs');
const path = require('path');
const { execSync, spawn } = require('child_process');
const chalk = require('chalk');

class TuskLangDevSetup {
  constructor() {
    this.projectRoot = process.cwd();
    this.setupConfig = {
      nodeVersion: '18.0.0',
      npmVersion: '9.0.0',
      requiredPackages: [
        'chalk',
        'commander',
        'inquirer',
        'ora',
        'figlet',
        'jest',
        'eslint',
        'prettier',
        'typescript',
        '@types/node'
      ],
      devPackages: [
        'nodemon',
        'cross-env',
        'rimraf',
        'husky',
        'lint-staged'
      ],
      directories: [
        'src',
        'tests',
        'docs',
        'tools',
        'examples',
        'scripts'
      ],
      files: [
        '.gitignore',
        '.eslintrc.js',
        '.prettierrc',
        'tsconfig.json',
        'jest.config.js',
        'README.md'
      ]
    };
  }

  /**
   * Initialize development environment
   */
  async initialize(options = {}) {
    console.log(chalk.blue.bold('🚀 TuskLang Development Environment Setup'));
    console.log(chalk.gray('Initializing development environment...\n'));

    try {
      // Check prerequisites
      await this.checkPrerequisites();
      
      // Create project structure
      await this.createProjectStructure();
      
      // Install dependencies
      await this.installDependencies();
      
      // Setup configuration files
      await this.setupConfigurationFiles();
      
      // Setup Git hooks
      await this.setupGitHooks();
      
      // Setup development scripts
      await this.setupDevScripts();
      
      // Validate setup
      await this.validateSetup();
      
      console.log(chalk.green.bold('\n✅ Development environment setup complete!'));
      this.printNextSteps();
      
    } catch (error) {
      console.error(chalk.red.bold('\n❌ Setup failed:'), error.message);
      process.exit(1);
    }
  }

  /**
   * Check system prerequisites
   */
  async checkPrerequisites() {
    console.log(chalk.yellow('🔍 Checking prerequisites...'));
    
    const checks = [
      { name: 'Node.js', command: 'node --version', minVersion: '16.0.0' },
      { name: 'npm', command: 'npm --version', minVersion: '8.0.0' },
      { name: 'Git', command: 'git --version' }
    ];
    
    for (const check of checks) {
      try {
        const output = execSync(check.command, { encoding: 'utf8' });
        const version = output.trim();
        
        if (check.minVersion) {
          const isValid = this.compareVersions(version, check.minVersion);
          if (isValid) {
            console.log(chalk.green(`  ✅ ${check.name}: ${version}`));
          } else {
            throw new Error(`${check.name} version ${version} is below minimum required ${check.minVersion}`);
          }
        } else {
          console.log(chalk.green(`  ✅ ${check.name}: ${version}`));
        }
      } catch (error) {
        throw new Error(`${check.name} not found or invalid: ${error.message}`);
      }
    }
    
    console.log('');
  }

  /**
   * Create project directory structure
   */
  async createProjectStructure() {
    console.log(chalk.yellow('📁 Creating project structure...'));
    
    for (const dir of this.setupConfig.directories) {
      const dirPath = path.join(this.projectRoot, dir);
      if (!fs.existsSync(dirPath)) {
        fs.mkdirSync(dirPath, { recursive: true });
        console.log(chalk.green(`  ✅ Created: ${dir}/`));
      } else {
        console.log(chalk.gray(`  ⏭️  Exists: ${dir}/`));
      }
    }
    
    console.log('');
  }

  /**
   * Install project dependencies
   */
  async installDependencies() {
    console.log(chalk.yellow('📦 Installing dependencies...'));
    
    try {
      // Install required packages
      console.log(chalk.cyan('  Installing required packages...'));
      execSync(`npm install ${this.setupConfig.requiredPackages.join(' ')}`, {
        stdio: 'inherit',
        cwd: this.projectRoot
      });
      
      // Install dev packages
      console.log(chalk.cyan('  Installing development packages...'));
      execSync(`npm install --save-dev ${this.setupConfig.devPackages.join(' ')}`, {
        stdio: 'inherit',
        cwd: this.projectRoot
      });
      
      console.log(chalk.green('  ✅ Dependencies installed successfully'));
      console.log('');
      
    } catch (error) {
      throw new Error(`Failed to install dependencies: ${error.message}`);
    }
  }

  /**
   * Setup configuration files
   */
  async setupConfigurationFiles() {
    console.log(chalk.yellow('⚙️  Setting up configuration files...'));
    
    const configs = [
      {
        name: '.gitignore',
        content: this.generateGitignore()
      },
      {
        name: '.eslintrc.js',
        content: this.generateEslintConfig()
      },
      {
        name: '.prettierrc',
        content: this.generatePrettierConfig()
      },
      {
        name: 'tsconfig.json',
        content: this.generateTsConfig()
      },
      {
        name: 'jest.config.js',
        content: this.generateJestConfig()
      },
      {
        name: 'README.md',
        content: this.generateReadme()
      }
    ];
    
    for (const config of configs) {
      const filePath = path.join(this.projectRoot, config.name);
      if (!fs.existsSync(filePath)) {
        fs.writeFileSync(filePath, config.content);
        console.log(chalk.green(`  ✅ Created: ${config.name}`));
      } else {
        console.log(chalk.gray(`  ⏭️  Exists: ${config.name}`));
      }
    }
    
    console.log('');
  }

  /**
   * Setup Git hooks
   */
  async setupGitHooks() {
    console.log(chalk.yellow('🔧 Setting up Git hooks...'));
    
    try {
      // Initialize Git if not already done
      if (!fs.existsSync(path.join(this.projectRoot, '.git'))) {
        execSync('git init', { stdio: 'inherit', cwd: this.projectRoot });
        console.log(chalk.green('  ✅ Initialized Git repository'));
      }
      
      // Setup Husky
      execSync('npx husky install', { stdio: 'inherit', cwd: this.projectRoot });
      
      // Add pre-commit hook
      const preCommitHook = `#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

npm run lint
npm run test
`;
      
      const hookPath = path.join(this.projectRoot, '.husky', 'pre-commit');
      fs.writeFileSync(hookPath, preCommitHook);
      fs.chmodSync(hookPath, '755');
      
      console.log(chalk.green('  ✅ Git hooks configured'));
      console.log('');
      
    } catch (error) {
      console.log(chalk.yellow(`  ⚠️  Git hooks setup failed: ${error.message}`));
    }
  }

  /**
   * Setup development scripts
   */
  async setupDevScripts() {
    console.log(chalk.yellow('📜 Setting up development scripts...'));
    
    const packageJsonPath = path.join(this.projectRoot, 'package.json');
    let packageJson;
    
    if (fs.existsSync(packageJsonPath)) {
      packageJson = JSON.parse(fs.readFileSync(packageJsonPath, 'utf8'));
    } else {
      packageJson = {
        name: path.basename(this.projectRoot),
        version: '1.0.0',
        description: 'TuskLang project',
        main: 'src/index.js',
        scripts: {},
        keywords: ['tusklang', 'configuration'],
        author: '',
        license: 'MIT'
      };
    }
    
    // Add development scripts
    packageJson.scripts = {
      ...packageJson.scripts,
      'start': 'node src/index.js',
      'dev': 'nodemon src/index.js',
      'build': 'npm run clean && npm run compile',
      'compile': 'tsc',
      'clean': 'rimraf dist',
      'test': 'jest',
      'test:watch': 'jest --watch',
      'test:coverage': 'jest --coverage',
      'lint': 'eslint src/**/*.js src/**/*.ts',
      'lint:fix': 'eslint src/**/*.js src/**/*.ts --fix',
      'format': 'prettier --write src/**/*.js src/**/*.ts',
      'format:check': 'prettier --check src/**/*.js src/**/*.ts',
      'type-check': 'tsc --noEmit',
      'setup': 'node tools/dev-setup.js',
      'debug': 'node tools/debugger.js',
      'profile': 'node tools/profiler.js'
    };
    
    fs.writeFileSync(packageJsonPath, JSON.stringify(packageJson, null, 2));
    console.log(chalk.green('  ✅ Development scripts configured'));
    console.log('');
  }

  /**
   * Validate setup
   */
  async validateSetup() {
    console.log(chalk.yellow('✅ Validating setup...'));
    
    const validations = [
      { name: 'package.json', path: 'package.json' },
      { name: 'node_modules', path: 'node_modules' },
      { name: 'src directory', path: 'src' },
      { name: 'tests directory', path: 'tests' },
      { name: 'docs directory', path: 'docs' },
      { name: 'tools directory', path: 'tools' }
    ];
    
    for (const validation of validations) {
      const fullPath = path.join(this.projectRoot, validation.path);
      if (fs.existsSync(fullPath)) {
        console.log(chalk.green(`  ✅ ${validation.name}: OK`));
      } else {
        console.log(chalk.red(`  ❌ ${validation.name}: Missing`));
      }
    }
    
    // Test npm scripts
    try {
      execSync('npm run lint --dry-run', { stdio: 'pipe', cwd: this.projectRoot });
      console.log(chalk.green('  ✅ npm scripts: OK'));
    } catch (error) {
      console.log(chalk.yellow('  ⚠️  npm scripts: Some issues detected'));
    }
    
    console.log('');
  }

  /**
   * Print next steps
   */
  printNextSteps() {
    console.log(chalk.blue.bold('🎯 Next Steps:'));
    console.log(chalk.cyan('  1. Start developing:'));
    console.log(chalk.gray('     npm run dev'));
    console.log('');
    console.log(chalk.cyan('  2. Run tests:'));
    console.log(chalk.gray('     npm test'));
    console.log('');
    console.log(chalk.cyan('  3. Lint code:'));
    console.log(chalk.gray('     npm run lint'));
    console.log('');
    console.log(chalk.cyan('  4. Format code:'));
    console.log(chalk.gray('     npm run format'));
    console.log('');
    console.log(chalk.cyan('  5. Debug configurations:'));
    console.log(chalk.gray('     npm run debug <config-file>'));
    console.log('');
    console.log(chalk.cyan('  6. Profile performance:'));
    console.log(chalk.gray('     npm run profile parse <config-file>'));
    console.log('');
    console.log(chalk.green('🚀 Happy coding with TuskLang!'));
  }

  /**
   * Generate .gitignore content
   */
  generateGitignore() {
    return `# Dependencies
node_modules/
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# Build outputs
dist/
build/
*.tsbuildinfo

# Environment variables
.env
.env.local
.env.development.local
.env.test.local
.env.production.local

# IDE files
.vscode/
.idea/
*.swp
*.swo
*~

# OS files
.DS_Store
Thumbs.db

# Logs
logs/
*.log

# Runtime data
pids/
*.pid
*.seed
*.pid.lock

# Coverage directory used by tools like istanbul
coverage/
*.lcov

# nyc test coverage
.nyc_output

# Dependency directories
jspm_packages/

# Optional npm cache directory
.npm

# Optional eslint cache
.eslintcache

# Microbundle cache
.rpt2_cache/
.rts2_cache_cjs/
.rts2_cache_es/
.rts2_cache_umd/

# Optional REPL history
.node_repl_history

# Output of 'npm pack'
*.tgz

# Yarn Integrity file
.yarn-integrity

# parcel-bundler cache (https://parceljs.org/)
.cache
.parcel-cache

# next.js build output
.next

# nuxt.js build output
.nuxt

# vuepress build output
.vuepress/dist

# Serverless directories
.serverless/

# FuseBox cache
.fusebox/

# DynamoDB Local files
.dynamodb/

# TernJS port file
.tern-port

# Stores VSCode versions used for testing VSCode extensions
.vscode-test

# TuskLang specific
*.pnt
*.tsk.bak
*.tsk.tmp
`;
  }

  /**
   * Generate ESLint configuration
   */
  generateEslintConfig() {
    return `module.exports = {
  env: {
    node: true,
    es2021: true,
    jest: true
  },
  extends: [
    'eslint:recommended',
    '@typescript-eslint/recommended'
  ],
  parser: '@typescript-eslint/parser',
  parserOptions: {
    ecmaVersion: 12,
    sourceType: 'module'
  },
  plugins: [
    '@typescript-eslint'
  ],
  rules: {
    'indent': ['error', 2],
    'linebreak-style': ['error', 'unix'],
    'quotes': ['error', 'single'],
    'semi': ['error', 'always'],
    'no-unused-vars': 'warn',
    'no-console': 'warn'
  }
};
`;
  }

  /**
   * Generate Prettier configuration
   */
  generatePrettierConfig() {
    return `{
  "semi": true,
  "trailingComma": "es5",
  "singleQuote": true,
  "printWidth": 80,
  "tabWidth": 2,
  "useTabs": false
}
`;
  }

  /**
   * Generate TypeScript configuration
   */
  generateTsConfig() {
    return `{
  "compilerOptions": {
    "target": "ES2020",
    "module": "commonjs",
    "lib": ["ES2020"],
    "outDir": "./dist",
    "rootDir": "./src",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true,
    "declaration": true,
    "declarationMap": true,
    "sourceMap": true,
    "removeComments": false,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "strictFunctionTypes": true,
    "noImplicitThis": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "moduleResolution": "node",
    "baseUrl": "./",
    "paths": {
      "@/*": ["src/*"]
    },
    "allowSyntheticDefaultImports": true,
    "experimentalDecorators": true,
    "emitDecoratorMetadata": true
  },
  "include": [
    "src/**/*"
  ],
  "exclude": [
    "node_modules",
    "dist",
    "tests"
  ]
}
`;
  }

  /**
   * Generate Jest configuration
   */
  generateJestConfig() {
    return `module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  roots: ['<rootDir>/src', '<rootDir>/tests'],
  testMatch: [
    '**/__tests__/**/*.ts',
    '**/?(*.)+(spec|test).ts'
  ],
  transform: {
    '^.+\\.ts$': 'ts-jest'
  },
  collectCoverageFrom: [
    'src/**/*.ts',
    '!src/**/*.d.ts'
  ],
  coverageDirectory: 'coverage',
  coverageReporters: ['text', 'lcov', 'html'],
  setupFilesAfterEnv: ['<rootDir>/tests/setup.ts']
};
`;
  }

  /**
   * Generate README content
   */
  generateReadme() {
    return `# TuskLang Project

A TuskLang configuration project with comprehensive development tools.

## Features

- 🚀 Fast configuration parsing and validation
- 🔧 Comprehensive development tools
- 📊 Performance profiling and debugging
- 🧪 Automated testing with Jest
- 📝 Code quality with ESLint and Prettier
- 🔄 Hot reloading with Nodemon

## Quick Start

\`\`\`bash
# Install dependencies
npm install

# Start development server
npm run dev

# Run tests
npm test

# Lint code
npm run lint

# Format code
npm run format
\`\`\`

## Development Tools

### Debugger
\`\`\`bash
npm run debug <config-file> [options]
\`\`\`

### Profiler
\`\`\`bash
npm run profile <command> [options]
\`\`\`

### Development Setup
\`\`\`bash
npm run setup
\`\`\`

## Project Structure

\`\`\`
├── src/           # Source code
├── tests/         # Test files
├── docs/          # Documentation
├── tools/         # Development tools
├── examples/      # Example configurations
└── scripts/       # Build and utility scripts
\`\`\`

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests and linting
5. Submit a pull request

## License

MIT License
`;
  }

  /**
   * Compare version strings
   */
  compareVersions(version1, version2) {
    const v1 = version1.replace(/^v/, '').split('.').map(Number);
    const v2 = version2.split('.').map(Number);
    
    for (let i = 0; i < Math.max(v1.length, v2.length); i++) {
      const num1 = v1[i] || 0;
      const num2 = v2[i] || 0;
      
      if (num1 > num2) return true;
      if (num1 < num2) return false;
    }
    
    return true;
  }
}

// CLI interface
if (require.main === module) {
  const setup = new TuskLangDevSetup();
  setup.initialize();
}

module.exports = TuskLangDevSetup; 