using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace TuskLang.CLI.Commands.Tusk
{
    /// <summary>
    /// Init command implementation - Create new TuskLang projects
    /// Provides project scaffolding with templates and proper structure
    /// </summary>
    public static class InitCommand
    {
        public static Command CreateInitCommand()
        {
            // Arguments
            var nameArgument = new Argument<string>(
                name: "name",
                getDefaultValue: () => Path.GetFileName(Directory.GetCurrentDirectory()),
                description: "Name of the project to create");

            // Options
            var templateOption = new Option<string>(
                aliases: new[] { "--template", "-t" },
                getDefaultValue: () => "basic",
                description: "Project template: basic, web, api, database, microservice")
            {
                AllowedValues = { "basic", "web", "api", "database", "microservice" }
            };

            var directoryOption = new Option<string>(
                aliases: new[] { "--directory", "-d" },
                description: "Directory to create project in (default: current directory)");

            var forceOption = new Option<bool>(
                aliases: new[] { "--force", "-f" },
                description: "Overwrite existing files");

            var interactiveOption = new Option<bool>(
                aliases: new[] { "--interactive", "-i" },
                description: "Interactive project setup");

            var gitOption = new Option<bool>(
                aliases: new[] { "--git" },
                getDefaultValue: () => true,
                description: "Initialize git repository");

            // Create command
            var initCommand = new Command("init", "Create new TuskLang project with scaffolding and templates")
            {
                nameArgument,
                templateOption,
                directoryOption,
                forceOption,
                interactiveOption,
                gitOption
            };

            initCommand.SetHandler(async (name, template, directory, force, interactive, git) =>
            {
                var command = new InitCommandImplementation();
                Environment.ExitCode = await command.ExecuteAsync(name, template, directory, force, interactive, git);
            }, nameArgument, templateOption, directoryOption, forceOption, interactiveOption, gitOption);

            return initCommand;
        }
    }

    /// <summary>
    /// Init command implementation with full project scaffolding capabilities
    /// </summary>
    public class InitCommandImplementation : CommandBase
    {
        public async Task<int> ExecuteAsync(
            string name,
            string template,
            string directory,
            bool force,
            bool interactive,
            bool git)
        {
            return await ExecuteWithTimingAsync(async () =>
            {
                // Resolve project directory
                var projectDir = string.IsNullOrEmpty(directory) 
                    ? Directory.GetCurrentDirectory() 
                    : Path.GetFullPath(directory);

                var projectPath = Path.Combine(projectDir, name);

                WriteProcessing($"Initializing project '{name}' with '{template}' template...");

                // Check if directory exists
                if (Directory.Exists(projectPath) && Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories).Length > 0)
                {
                    if (!force)
                    {
                        WriteError($"Directory '{projectPath}' already exists and is not empty. Use --force to overwrite.");
                        return 1;
                    }
                    WriteWarning("Overwriting existing project files...");
                }

                // Interactive setup
                if (interactive)
                {
                    var interactiveResult = await RunInteractiveSetupAsync(name, ref template);
                    if (interactiveResult.name != null) name = interactiveResult.name;
                    template = interactiveResult.template;
                }

                // Create project structure
                await CreateProjectStructureAsync(projectPath, name, template);

                // Initialize git repository
                if (git)
                {
                    await InitializeGitRepositoryAsync(projectPath);
                }

                WriteSuccess($"Project '{name}' created successfully!");
                WriteInfo($"Project location: {projectPath}");
                WriteInfo($"Template: {template}");
                WriteInfo("Next steps:");
                WriteInfo("  1. cd " + name);
                WriteInfo("  2. tusk validate peanu.tsk");
                WriteInfo("  3. tusk build");

                return 0;
            }, "Init");
        }

        private async Task<(string name, string template)> RunInteractiveSetupAsync(string currentName, string currentTemplate)
        {
            WriteInfo("🚀 Interactive Project Setup");
            Console.WriteLine();

            // Project name
            Console.Write($"Project name [{currentName}]: ");
            var nameInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(nameInput))
                currentName = nameInput;

            // Template selection
            Console.WriteLine();
            Console.WriteLine("Available templates:");
            Console.WriteLine("  1. basic       - Simple configuration project");
            Console.WriteLine("  2. web         - Web application configuration");
            Console.WriteLine("  3. api         - API service configuration");
            Console.WriteLine("  4. database    - Database-focused configuration");
            Console.WriteLine("  5. microservice - Microservice configuration");
            Console.WriteLine();

            while (true)
            {
                Console.Write($"Select template [1-5, default: basic]: ");
                var templateInput = Console.ReadLine();
                
                var template = templateInput switch
                {
                    "1" or "" => "basic",
                    "2" => "web",
                    "3" => "api",
                    "4" => "database",
                    "5" => "microservice",
                    _ => null
                };

                if (template != null)
                {
                    currentTemplate = template;
                    break;
                }

                WriteError("Invalid selection. Please choose 1-5.");
            }

            return (currentName, currentTemplate);
        }

        private async Task CreateProjectStructureAsync(string projectPath, string name, string template)
        {
            WriteProcessing("Creating project structure...");

            // Ensure project directory exists
            Directory.CreateDirectory(projectPath);

            // Create base directories
            var directories = new[]
            {
                "config",
                "templates",
                "scripts",
                "docs"
            };

            foreach (var dir in directories)
            {
                Directory.CreateDirectory(Path.Combine(projectPath, dir));
            }

            // Create main configuration file
            await CreateMainConfigurationAsync(projectPath, name, template);

            // Create template-specific files
            await CreateTemplateFilesAsync(projectPath, name, template);

            // Create documentation
            await CreateDocumentationAsync(projectPath, name, template);

            // Create .gitignore if needed
            await CreateGitIgnoreAsync(projectPath);

            WriteSuccess("Project structure created");
        }

        private async Task CreateMainConfigurationAsync(string projectPath, string name, string template)
        {
            var configContent = template switch
            {
                "web" => GetWebTemplate(name),
                "api" => GetApiTemplate(name),
                "database" => GetDatabaseTemplate(name),
                "microservice" => GetMicroserviceTemplate(name),
                _ => GetBasicTemplate(name)
            };

            await SaveFileAtomicAsync(Path.Combine(projectPath, "peanu.tsk"), configContent, "main configuration");
        }

        private async Task CreateTemplateFilesAsync(string projectPath, string name, string template)
        {
            // Create environment-specific configurations
            var environments = new[] { "development", "staging", "production" };
            
            foreach (var env in environments)
            {
                var envConfig = GetEnvironmentTemplate(name, env, template);
                await SaveFileAtomicAsync(
                    Path.Combine(projectPath, "config", $"{env}.tsk"), 
                    envConfig, 
                    $"{env} configuration");
            }

            // Create template-specific additional files
            switch (template)
            {
                case "web":
                    await CreateWebTemplateFilesAsync(projectPath, name);
                    break;
                case "api":
                    await CreateApiTemplateFilesAsync(projectPath, name);
                    break;
                case "database":
                    await CreateDatabaseTemplateFilesAsync(projectPath, name);
                    break;
                case "microservice":
                    await CreateMicroserviceTemplateFilesAsync(projectPath, name);
                    break;
            }
        }

        private async Task CreateDocumentationAsync(string projectPath, string name, string template)
        {
            // README.md
            var readmeContent = $@"# {name}

{GetProjectDescription(template)}

## Quick Start

1. Validate configuration:
   ```bash
   tusk validate peanu.tsk
   ```

2. Build project:
   ```bash
   tusk build
   ```

3. Watch for changes:
   ```bash
   tusk watch peanu.tsk
   ```

## Configuration Structure

- `peanu.tsk` - Main configuration file
- `config/` - Environment-specific configurations
- `templates/` - Configuration templates
- `scripts/` - Build and deployment scripts
- `docs/` - Project documentation

## Template: {template}

{GetTemplateDocumentation(template)}

## Environment Configuration

This project supports multiple environments:

- **Development** (`config/development.tsk`) - Local development settings
- **Staging** (`config/staging.tsk`) - Staging environment settings  
- **Production** (`config/production.tsk`) - Production environment settings

## Usage Examples

```bash
# Parse and analyze configuration
tusk parse peanu.tsk --format detailed --statistics

# Validate with strict checks
tusk validate peanu.tsk --strict --best-practices --security

# Compile to binary format
tusk compile peanu.tsk --optimize --compression gzip

# Initialize new environment
tusk init new-service --template {template}
```

## Contributing

1. Make changes to configuration files
2. Validate changes: `tusk validate peanu.tsk`
3. Test compilation: `tusk compile peanu.tsk`
4. Commit changes with descriptive messages

## License

This project configuration is licensed under [Your License].
";

            await SaveFileAtomicAsync(Path.Combine(projectPath, "README.md"), readmeContent, "README");

            // Configuration guide
            var configGuide = $@"# Configuration Guide - {name}

## Overview

This document explains the configuration structure and options for the {name} project using the {template} template.

## Main Configuration (peanu.tsk)

The main configuration file contains the core settings for your application. It follows TuskLang syntax with sections and key-value pairs.

### Sections

{GetConfigurationGuide(template)}

## Environment Configurations

Environment-specific configurations override the main configuration:

- `config/development.tsk` - Development overrides
- `config/staging.tsk` - Staging overrides  
- `config/production.tsk` - Production overrides

## Best Practices

1. **Security**: Never commit sensitive data like passwords or API keys
2. **Environment Variables**: Use `${{ENV_VAR}}` for environment-specific values
3. **Documentation**: Comment your configuration with `# comments`
4. **Validation**: Always run `tusk validate` before deployment
5. **Compilation**: Use `tusk compile` for production deployments

## Common Patterns

### Database Configuration
```tsk
[database]
host = ""${{DB_HOST}}""
port = ${{DB_PORT}}
database = ""{name}""
username = ""${{DB_USER}}""
password = ""${{DB_PASSWORD}}""
```

### Service Configuration
```tsk
[service]
name = ""{name}""
port = ${{PORT}}
environment = ""${{ENVIRONMENT}}""
debug = ${{DEBUG}}
```

### Logging Configuration
```tsk
[logging]
level = ""${{LOG_LEVEL}}""
format = ""json""
output = ""stdout""
```

## Troubleshooting

### Common Issues

1. **Parse Errors**: Check syntax with `tusk parse peanu.tsk`
2. **Validation Failures**: Review with `tusk validate peanu.tsk --verbose`
3. **Environment Issues**: Verify environment variables are set

### Support

For help with TuskLang configuration:
- Run `tusk help [command]` for command help
- Use `tusk validate --verbose` for detailed error messages
- Check the TuskLang documentation at https://tusklang.org
";

            await SaveFileAtomicAsync(Path.Combine(projectPath, "docs", "configuration.md"), configGuide, "configuration guide");
        }

        private async Task CreateGitIgnoreAsync(string projectPath)
        {
            var gitignoreContent = @"# TuskLang compiled files
*.pnt

# Environment files
.env
.env.local
.env.development
.env.staging
.env.production

# Logs
logs/
*.log

# Runtime data
pids/
*.pid
*.seed

# Coverage directory used by tools like istanbul
coverage/

# Dependency directories
node_modules/

# Optional npm cache directory
.npm

# Optional REPL history
.node_repl_history

# Output of 'npm pack'
*.tgz

# Yarn Integrity file
.yarn-integrity

# OS generated files
.DS_Store
.DS_Store?
._*
.Spotlight-V100
.Trashes
ehthumbs.db
Thumbs.db

# IDE files
.vscode/
.idea/
*.swp
*.swo

# Temporary files
tmp/
temp/
";

            await SaveFileAtomicAsync(Path.Combine(projectPath, ".gitignore"), gitignoreContent, ".gitignore");
        }

        private async Task InitializeGitRepositoryAsync(string projectPath)
        {
            try
            {
                WriteProcessing("Initializing git repository...");

                var gitDir = Path.Combine(projectPath, ".git");
                if (!Directory.Exists(gitDir))
                {
                    // Simple git init simulation - in real implementation would call git
                    Directory.CreateDirectory(gitDir);
                    WriteInfo("Git repository initialized (placeholder)");
                }
                else
                {
                    WriteInfo("Git repository already exists");
                }
            }
            catch (Exception ex)
            {
                WriteWarning($"Could not initialize git repository: {ex.Message}");
            }
        }

        // Template content generators
        private string GetBasicTemplate(string name)
        {
            return $@"# {name} - Basic TuskLang Configuration
# Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}

[project]
name = ""{name}""
version = ""1.0.0""
description = ""Basic TuskLang project configuration""
author = ""${{USER}}""

[application]
name = ""{name}""
environment = ""${{ENVIRONMENT}}""
debug = ${{DEBUG}}

[logging]
level = ""${{LOG_LEVEL}}""
format = ""text""
output = ""stdout""
";
        }

        private string GetWebTemplate(string name)
        {
            return $@"# {name} - Web Application Configuration
# Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}

[project]
name = ""{name}""
version = ""1.0.0""
description = ""Web application configuration""
author = ""${{USER}}""

[server]
host = ""${{HOST}}""
port = ${{PORT}}
ssl = ${{SSL_ENABLED}}
cert_path = ""${{SSL_CERT_PATH}}""
key_path = ""${{SSL_KEY_PATH}}""

[application]
name = ""{name}""
environment = ""${{ENVIRONMENT}}""
debug = ${{DEBUG}}
session_secret = ""${{SESSION_SECRET}}""
csrf_protection = true

[database]
host = ""${{DB_HOST}}""
port = ${{DB_PORT}}
database = ""{name}""
username = ""${{DB_USER}}""
password = ""${{DB_PASSWORD}}""
pool_size = ${{DB_POOL_SIZE}}

[cache]
type = ""redis""
host = ""${{REDIS_HOST}}""
port = ${{REDIS_PORT}}
password = ""${{REDIS_PASSWORD}}""

[logging]
level = ""${{LOG_LEVEL}}""
format = ""json""
output = ""stdout""
access_log = true
";
        }

        private string GetApiTemplate(string name)
        {
            return $@"# {name} - API Service Configuration
# Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}

[project]
name = ""{name}""
version = ""1.0.0""
description = ""API service configuration""
author = ""${{USER}}""

[api]
name = ""{name}""
version = ""v1""
base_path = ""/api/v1""
port = ${{API_PORT}}
cors_enabled = true
rate_limit = ${{RATE_LIMIT}}

[auth]
jwt_secret = ""${{JWT_SECRET}}""
jwt_expiry = ""24h""
refresh_enabled = true

[database]
host = ""${{DB_HOST}}""
port = ${{DB_PORT}}
database = ""{name}""
username = ""${{DB_USER}}""
password = ""${{DB_PASSWORD}}""
migrations_path = ""./migrations""

[monitoring]
metrics_enabled = true
health_check = true
prometheus_port = ${{METRICS_PORT}}

[logging]
level = ""${{LOG_LEVEL}}""
format = ""json""
output = ""stdout""
request_logging = true
";
        }

        private string GetDatabaseTemplate(string name)
        {
            return $@"# {name} - Database Configuration
# Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}

[project]
name = ""{name}""
version = ""1.0.0""
description = ""Database project configuration""
author = ""${{USER}}""

[database]
host = ""${{DB_HOST}}""
port = ${{DB_PORT}}
database = ""{name}""
username = ""${{DB_USER}}""
password = ""${{DB_PASSWORD}}""
charset = ""utf8mb4""
timezone = ""UTC""

[connection_pool]
min_connections = ${{DB_POOL_MIN}}
max_connections = ${{DB_POOL_MAX}}
idle_timeout = ""30s""
max_lifetime = ""1h""

[migrations]
path = ""./migrations""
table = ""schema_migrations""
auto_migrate = ${{AUTO_MIGRATE}}

[backup]
enabled = ${{BACKUP_ENABLED}}
schedule = ""@daily""
retention = ""30d""
path = ""${{BACKUP_PATH}}""

[monitoring]
slow_query_log = true
slow_query_time = ""1s""
performance_schema = true

[logging]
level = ""${{LOG_LEVEL}}""
format = ""json""
output = ""stdout""
query_logging = ${{QUERY_LOG_ENABLED}}
";
        }

        private string GetMicroserviceTemplate(string name)
        {
            return $@"# {name} - Microservice Configuration
# Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}

[project]
name = ""{name}""
version = ""1.0.0""
description = ""Microservice configuration""
author = ""${{USER}}""

[service]
name = ""{name}""
port = ${{SERVICE_PORT}}
health_port = ${{HEALTH_PORT}}
metrics_port = ${{METRICS_PORT}}
graceful_shutdown = true

[discovery]
type = ""consul""
host = ""${{CONSUL_HOST}}""
port = ${{CONSUL_PORT}}
service_id = ""{name}-${{INSTANCE_ID}}""

[messaging]
type = ""kafka""
brokers = ""${{KAFKA_BROKERS}}""
group_id = ""{name}-consumer""
auto_commit = true

[database]
host = ""${{DB_HOST}}""
port = ${{DB_PORT}}
database = ""{name}""
username = ""${{DB_USER}}""
password = ""${{DB_PASSWORD}}""

[tracing]
enabled = ${{TRACING_ENABLED}}
jaeger_endpoint = ""${{JAEGER_ENDPOINT}}""
sample_rate = ""0.1""

[circuit_breaker]
enabled = true
failure_threshold = 5
timeout = ""30s""
recovery_timeout = ""10s""

[logging]
level = ""${{LOG_LEVEL}}""
format = ""json""
output = ""stdout""
correlation_id = true
";
        }

        private string GetEnvironmentTemplate(string name, string environment, string template)
        {
            return $@"# {name} - {environment} Environment Configuration
# This file overrides settings in peanu.tsk for {environment} environment

[project]
environment = ""{environment}""

{GetEnvironmentSpecificSettings(environment, template)}
";
        }

        private string GetEnvironmentSpecificSettings(string environment, string template)
        {
            return environment switch
            {
                "development" => @"
[logging]
level = ""debug""
format = ""text""

[application]
debug = true

[database]
host = ""localhost""
port = 5432
",
                "staging" => @"
[logging]
level = ""info""
format = ""json""

[application]
debug = false

[monitoring]
enabled = true
",
                "production" => @"
[logging]
level = ""warn""
format = ""json""

[application]
debug = false

[security]
strict_mode = true

[monitoring]
enabled = true
alerts_enabled = true
",
                _ => ""
            };
        }

        private async Task CreateWebTemplateFilesAsync(string projectPath, string name)
        {
            // Create nginx configuration template
            var nginxConfig = $@"server {{
    listen 80;
    server_name {name}.local;
    
    location / {{
        proxy_pass http://localhost:3000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }}
}}";

            await SaveFileAtomicAsync(Path.Combine(projectPath, "templates", "nginx.conf"), nginxConfig, "nginx template");
        }

        private async Task CreateApiTemplateFilesAsync(string projectPath, string name)
        {
            // Create API documentation template
            var apiDocs = $@"# {name} API Documentation

## Endpoints

### Health Check
GET /health

### Authentication
POST /api/v1/auth/login
POST /api/v1/auth/refresh

### Main API
GET /api/v1/items
POST /api/v1/items
GET /api/v1/items/:id
PUT /api/v1/items/:id
DELETE /api/v1/items/:id
";

            await SaveFileAtomicAsync(Path.Combine(projectPath, "docs", "api.md"), apiDocs, "API documentation");
        }

        private async Task CreateDatabaseTemplateFilesAsync(string projectPath, string name)
        {
            // Create migration template
            var migrationDir = Path.Combine(projectPath, "migrations");
            Directory.CreateDirectory(migrationDir);

            var initialMigration = @"-- Initial migration
-- Created: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") + @"

CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_users_email ON users(email);
";

            await SaveFileAtomicAsync(Path.Combine(migrationDir, "001_initial.sql"), initialMigration, "initial migration");
        }

        private async Task CreateMicroserviceTemplateFilesAsync(string projectPath, string name)
        {
            // Create Docker configuration
            var dockerfile = $@"FROM node:16-alpine
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production
COPY . .
EXPOSE 3000
CMD [""node"", ""server.js""]
";

            await SaveFileAtomicAsync(Path.Combine(projectPath, "Dockerfile"), dockerfile, "Dockerfile");

            // Create docker-compose
            var dockerCompose = $@"version: '3.8'
services:
  {name}:
    build: .
    ports:
      - ""3000:3000""
    environment:
      - NODE_ENV=development
    depends_on:
      - db
      
  db:
    image: postgres:13
    environment:
      - POSTGRES_DB={name}
      - POSTGRES_USER=user
      - POSTGRES_PASSWORD=password
    ports:
      - ""5432:5432""
";

            await SaveFileAtomicAsync(Path.Combine(projectPath, "docker-compose.yml"), dockerCompose, "docker-compose");
        }

        private string GetProjectDescription(string template)
        {
            return template switch
            {
                "web" => "A web application project with server, database, and caching configuration.",
                "api" => "An API service project with authentication, database, and monitoring configuration.",
                "database" => "A database-focused project with connection pooling, migrations, and backup configuration.",
                "microservice" => "A microservice project with service discovery, messaging, and monitoring configuration.",
                _ => "A basic TuskLang configuration project."
            };
        }

        private string GetTemplateDocumentation(string template)
        {
            return template switch
            {
                "web" => "Includes server configuration, database setup, caching with Redis, and logging configuration suitable for web applications.",
                "api" => "Includes API configuration with JWT authentication, database setup, rate limiting, and monitoring suitable for REST APIs.",
                "database" => "Includes comprehensive database configuration with connection pooling, migrations, backup strategies, and monitoring.",
                "microservice" => "Includes service discovery, messaging with Kafka, distributed tracing, circuit breakers, and monitoring suitable for microservices.",
                _ => "Includes basic project structure with logging and application configuration."
            };
        }

        private string GetConfigurationGuide(string template)
        {
            return template switch
            {
                "web" => @"
- **[project]** - Project metadata and information
- **[server]** - Web server configuration (host, port, SSL)
- **[application]** - Application-specific settings
- **[database]** - Database connection and pooling
- **[cache]** - Redis caching configuration
- **[logging]** - Logging configuration and formats",

                "api" => @"
- **[project]** - Project metadata and information  
- **[api]** - API configuration (versioning, CORS, rate limiting)
- **[auth]** - Authentication and JWT configuration
- **[database]** - Database connection and migrations
- **[monitoring]** - Metrics and health check configuration
- **[logging]** - Request logging and formatting",

                "database" => @"
- **[project]** - Project metadata and information
- **[database]** - Database connection settings
- **[connection_pool]** - Connection pooling configuration
- **[migrations]** - Database migration settings
- **[backup]** - Backup strategy and scheduling
- **[monitoring]** - Database monitoring and performance
- **[logging]** - Query logging and diagnostics",

                "microservice" => @"
- **[project]** - Project metadata and information
- **[service]** - Service configuration and ports
- **[discovery]** - Service discovery (Consul/etcd)
- **[messaging]** - Message queue configuration (Kafka/RabbitMQ)
- **[database]** - Database connection settings
- **[tracing]** - Distributed tracing (Jaeger/Zipkin)
- **[circuit_breaker]** - Circuit breaker configuration
- **[logging]** - Structured logging with correlation IDs",

                _ => @"
- **[project]** - Project metadata and information
- **[application]** - Application-specific settings  
- **[logging]** - Logging configuration"
            };
        }
    }
} 