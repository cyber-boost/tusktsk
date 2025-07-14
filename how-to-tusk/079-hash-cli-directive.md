# #cli - Command Line Interface Directive

The `#cli` directive creates command-line interface commands for your TuskLang applications, enabling scripts, tools, and automation.

## Basic Syntax

```tusk
# Simple command
#cli hello {
    @output("Hello, World!")
}

# Run with: tusk hello

# Command with logic
#cli greet {
    name: @args[1] || "User"
    @output("Hello, " + name + "!")
}

# Run with: tusk greet John
```

## Command Arguments

```tusk
# Positional arguments
#cli deploy {
    environment: @args[1]
    version: @args[2] || "latest"
    
    if (!environment) {
        @error("Environment required: tusk deploy <environment> [version]")
        @exit(1)
    }
    
    @output("Deploying " + version + " to " + environment)
    @deploy_application(environment, version)
}

# Named command structure
#cli user:create {
    # Command: tusk user:create john@example.com "John Doe"
    email: @args[1]
    name: @args[2]
    
    if (!email || !name) {
        @error("Usage: tusk user:create <email> <name>")
        @exit(1)
    }
    
    user: @User.create({
        email: email,
        name: name,
        password: @generate_temp_password()
    })
    
    @success("User created with ID: " + user.id)
}

# Variable arguments
#cli concat {
    # Accepts any number of arguments
    result: @args.slice(1).join(" ")
    @output(result)
}
```

## Command Options

```tusk
# Options and flags
#cli migrate {
    # Parse options
    options: @parse_options({
        # Long and short options
        "force|f": "boolean",
        "step|s": "number",
        "seed": "boolean",
        "rollback|r": "boolean"
    })
    
    if (options.rollback) {
        steps: options.step || 1
        @output("Rolling back " + steps + " migration(s)...")
        @rollback_migrations(steps)
    } else {
        @output("Running migrations...")
        @run_migrations(options.force)
        
        if (options.seed) {
            @output("Seeding database...")
            @seed_database()
        }
    }
}

# Run with:
# tusk migrate
# tusk migrate --force --seed
# tusk migrate --rollback --step=3
# tusk migrate -r -s 3

# Complex options
#cli backup {
    options: @parse_options({
        "database|d": "string",
        "output|o": "string",
        "compress|c": "boolean",
        "exclude": "array",
        "include": "array",
        "quiet|q": "boolean",
        "verbose|v": "boolean"
    })
    
    # Set defaults
    database: options.database || @env.DB_DATABASE
    output: options.output || "backup_" + @date("Y-m-d_H-i-s") + ".sql"
    
    if (!options.quiet) {
        @output("Backing up database: " + database)
    }
    
    # Perform backup
    @backup_database(database, output, options)
    
    if (options.compress) {
        @compress_file(output)
        output += ".gz"
    }
    
    if (!options.quiet) {
        @success("Backup completed: " + output)
    }
}
```

## Interactive Commands

```tusk
# User input
#cli setup {
    @output("Welcome to TuskLang Setup")
    @output("=" * 30)
    
    # Ask questions
    app_name: @ask("Application name:")
    db_type: @choice("Database type:", ["mysql", "postgresql", "sqlite"])
    
    if (db_type != "sqlite") {
        db_host: @ask("Database host:", "localhost")
        db_name: @ask("Database name:")
        db_user: @ask("Database username:")
        db_pass: @secret("Database password:")
    }
    
    # Confirmation
    if (@confirm("Create application with these settings?")) {
        @create_application({
            name: app_name,
            database: {
                type: db_type,
                host: db_host,
                name: db_name,
                user: db_user,
                pass: db_pass
            }
        })
        
        @success("Application created successfully!")
    } else {
        @info("Setup cancelled")
    }
}

# Menu selection
#cli manage {
    action: @menu("What would you like to do?", {
        "1": "Clear cache",
        "2": "Run maintenance",
        "3": "View logs",
        "4": "Exit"
    })
    
    switch (action) {
        case "1":
            @clear_cache()
        case "2":
            @run_maintenance()
        case "3":
            @view_logs()
        case "4":
            @exit(0)
    }
}
```

## Output Formatting

```tusk
# Colored output
#cli status {
    @info("Checking system status...")
    
    services: [
        {name: "Database", status: @check_database()},
        {name: "Cache", status: @check_cache()},
        {name: "Queue", status: @check_queue()}
    ]
    
    @output("")  # Empty line
    @output("Service Status:")
    @output("-" * 20)
    
    for (service in services) {
        if (service.status) {
            @success("✓ " + service.name + ": Online")
        } else {
            @error("✗ " + service.name + ": Offline")
        }
    }
}

# Table output
#cli users:list {
    users: @User.all()
    
    @table(users, ["ID", "Name", "Email", "Created"], (user) => [
        user.id,
        user.name,
        user.email,
        user.created_at.format("Y-m-d")
    ])
}

# Progress bar
#cli import {
    file: @args[1]
    
    if (!file) {
        @error("Usage: tusk import <file>")
        @exit(1)
    }
    
    rows: @read_csv(file)
    total: rows.length
    
    @progress_start(total, "Importing records")
    
    for (i, row in rows) {
        @import_record(row)
        @progress_advance()
        
        # Update message every 10 records
        if (i % 10 == 0) {
            @progress_message("Processing: " + row.name)
        }
    }
    
    @progress_finish()
    @success("Imported " + total + " records")
}
```

## File Operations

```tusk
# File processing command
#cli process:files {
    options: @parse_options({
        "input|i": "string",
        "output|o": "string",
        "format|f": "string",
        "recursive|r": "boolean"
    })
    
    input_dir: options.input || "."
    output_dir: options.output || "processed"
    format: options.format || "json"
    
    # Find files
    files: @find_files(input_dir, {
        pattern: "*.csv",
        recursive: options.recursive
    })
    
    if (files.length == 0) {
        @warning("No files found")
        @exit(0)
    }
    
    @info("Found " + files.length + " files to process")
    
    # Process each file
    for (file in files) {
        @output("Processing: " + file)
        
        try {
            data: @read_csv(file)
            output_file: @path.join(output_dir, 
                @path.basename(file, ".csv") + "." + format)
            
            switch (format) {
                case "json":
                    @file.write_json(output_file, data)
                case "xml":
                    @file.write_xml(output_file, data)
                default:
                    @error("Unknown format: " + format)
            }
            
            @success("✓ Saved: " + output_file)
            
        } catch (e) {
            @error("✗ Failed: " + e.message)
        }
    }
}
```

## Background Jobs

```tusk
# Queue worker command
#cli queue:work {
    options: @parse_options({
        "queue": "string",
        "timeout": "number",
        "sleep": "number",
        "tries": "number",
        "daemon": "boolean"
    })
    
    queue_name: options.queue || "default"
    timeout: options.timeout || 60
    sleep: options.sleep || 3
    max_tries: options.tries || 3
    
    @info("Starting queue worker for: " + queue_name)
    
    # Handle signals
    @signal("SIGINT", () => {
        @info("Shutting down gracefully...")
        @exit(0)
    })
    
    while (true) {
        job: @queue.pop(queue_name)
        
        if (job) {
            @info("Processing job: " + job.id)
            
            try {
                @timeout(timeout, () => {
                    @process_job(job)
                })
                
                @success("Job completed: " + job.id)
                
            } catch (e) {
                @error("Job failed: " + job.id + " - " + e.message)
                
                if (job.attempts < max_tries) {
                    @queue.retry(job)
                } else {
                    @queue.failed(job, e)
                }
            }
        } else {
            if (!options.daemon) {
                break
            }
            @sleep(sleep)
        }
    }
}
```

## Scheduling

```tusk
# Scheduler command
#cli schedule:run {
    @info("Running scheduled tasks...")
    
    tasks: @get_scheduled_tasks()
    current_time: @now()
    
    for (task in tasks) {
        if (@should_run(task, current_time)) {
            @output("Running: " + task.name)
            
            try {
                @run_task(task)
                @update_last_run(task, current_time)
                @success("✓ " + task.name + " completed")
                
            } catch (e) {
                @error("✗ " + task.name + " failed: " + e.message)
                @log_task_failure(task, e)
            }
        }
    }
    
    @info("Scheduler run completed")
}

# This would typically be run by system cron:
# * * * * * tusk schedule:run
```

## Testing Commands

```tusk
# Run tests
#cli test {
    options: @parse_options({
        "filter": "string",
        "coverage": "boolean",
        "verbose|v": "boolean",
        "stop-on-failure": "boolean"
    })
    
    @output("TuskLang Test Runner")
    @output("=" * 40)
    
    # Find test files
    test_files: @find_files("tests", {
        pattern: "*Test.tusk",
        recursive: true
    })
    
    if (options.filter) {
        test_files: test_files.filter(f => f.includes(options.filter))
    }
    
    # Run tests
    results: {
        passed: 0,
        failed: 0,
        skipped: 0
    }
    
    for (file in test_files) {
        @output("\nRunning: " + file)
        
        result: @run_test_file(file, options)
        results.passed += result.passed
        results.failed += result.failed
        results.skipped += result.skipped
        
        if (result.failed > 0 && options["stop-on-failure"]) {
            break
        }
    }
    
    # Summary
    @output("\n" + "=" * 40)
    @success("Passed: " + results.passed)
    if (results.failed > 0) {
        @error("Failed: " + results.failed)
    }
    if (results.skipped > 0) {
        @warning("Skipped: " + results.skipped)
    }
    
    if (options.coverage) {
        @output("\nCode Coverage: " + @calculate_coverage() + "%")
    }
    
    @exit(results.failed > 0 ? 1 : 0)
}
```

## Command Composition

```tusk
# Main command with subcommands
#cli db {
    subcommand: @args[1]
    
    if (!subcommand) {
        @output("Database management commands:")
        @output("  db:create     Create database")
        @output("  db:drop       Drop database")
        @output("  db:migrate    Run migrations")
        @output("  db:seed       Seed database")
        @output("  db:backup     Backup database")
        @exit(0)
    }
    
    # Remove subcommand from args
    @args: @args.slice(1)
    
    switch (subcommand) {
        case "create":
            @call_command("db:create")
        case "drop":
            @call_command("db:drop")
        case "migrate":
            @call_command("migrate")
        case "seed":
            @call_command("db:seed")
        case "backup":
            @call_command("backup")
        default:
            @error("Unknown subcommand: " + subcommand)
            @exit(1)
    }
}

# Individual subcommands
#cli db:create {
    name: @args[1] || @env.DB_DATABASE
    
    if (@confirm("Create database '" + name + "'?")) {
        @create_database(name)
        @success("Database created: " + name)
    }
}
```

## Best Practices

1. **Provide help text** - Make commands self-documenting
2. **Validate arguments** - Check required parameters
3. **Use consistent naming** - Follow conventions (noun:verb)
4. **Handle errors gracefully** - Provide helpful error messages
5. **Add confirmation prompts** - For destructive operations
6. **Use appropriate exit codes** - 0 for success, non-zero for errors
7. **Support common options** - --help, --verbose, --quiet
8. **Make commands idempotent** - Safe to run multiple times

## Related Topics

- `hash-cron-directive` - Scheduled tasks
- `console-output` - Terminal formatting
- `process-management` - Background processes
- `file-operations` - File handling
- `database-operations` - Database commands