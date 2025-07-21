# Hash CLI Directive in TuskLang for Ruby

Welcome to the command-line revolution with TuskLang's Hash CLI Directive! This is where we break free from the constraints of traditional CLI frameworks and embrace the power of declarative, configuration-driven command-line applications. In Ruby, this means combining TuskLang's elegant hash directives with Ruby's powerful CLI capabilities to create applications that are as expressive as they are powerful.

## What is the Hash CLI Directive?

The Hash CLI Directive (`#cli`) is TuskLang's declaration of independence from traditional CLI frameworks. It allows you to define complete command-line applications, commands, options, and behaviors using simple hash configurations. In Ruby, this translates to powerful, declarative CLI definitions that can be processed, validated, and executed with minimal ceremony.

## Basic CLI Directive Syntax

```ruby
# Basic CLI application definition
cli_app_config = {
  "#cli" => {
    "name" => "tusk-cli",
    "version" => "1.0.0",
    "description" => "A powerful CLI built with TuskLang",
    "commands" => {
      "init" => {
        "description" => "Initialize a new TuskLang project",
        "handler" => "InitCommand#execute",
        "options" => {
          "template" => {
            "type" => "string",
            "description" => "Project template to use",
            "default" => "default"
          },
          "force" => {
            "type" => "boolean",
            "description" => "Force overwrite existing files",
            "default" => false
          }
        }
      },
      "build" => {
        "description" => "Build the project",
        "handler" => "BuildCommand#execute",
        "options" => {
          "target" => {
            "type" => "string",
            "description" => "Build target",
            "required" => true
          }
        }
      }
    }
  }
}

# Ruby class to process the CLI directive
class CliDirectiveProcessor
  def initialize(config)
    @config = config
    @tusk_config = load_tusk_config
  end

  def process_cli_directive
    cli_config = @config["#cli"]
    return nil unless cli_config

    {
      name: cli_config["name"],
      version: cli_config["version"],
      description: cli_config["description"],
      commands: process_commands(cli_config["commands"]),
      global_options: process_global_options(cli_config["global_options"]),
      help_template: cli_config["help_template"] || "default"
    }
  end

  private

  def process_commands(commands_config)
    commands_config.transform_values do |command_config|
      {
        description: command_config["description"],
        handler: parse_handler(command_config["handler"]),
        options: process_options(command_config["options"]),
        arguments: process_arguments(command_config["arguments"]),
        examples: command_config["examples"] || [],
        aliases: command_config["aliases"] || []
      }
    end
  end

  def process_options(options_config)
    return {} unless options_config

    options_config.transform_values do |option_config|
      {
        type: option_config["type"],
        description: option_config["description"],
        required: option_config["required"] || false,
        default: option_config["default"],
        choices: option_config["choices"] || []
      }
    end
  end

  def process_arguments(arguments_config)
    return [] unless arguments_config

    arguments_config.map do |arg_config|
      {
        name: arg_config["name"],
        description: arg_config["description"],
        required: arg_config["required"] || false,
        type: arg_config["type"] || "string"
      }
    end
  end

  def parse_handler(handler_string)
    return nil unless handler_string
    
    command_class, method = handler_string.split("#")
    {
      class: command_class,
      method: method
    }
  end

  def process_global_options(global_options_config)
    return {} unless global_options_config

    process_options(global_options_config)
  end

  def load_tusk_config
    TuskConfig.load("peanu.tsk")
  end
end
```

## Advanced CLI Configuration

```ruby
# Comprehensive CLI application configuration
advanced_cli_config = {
  "#cli" => {
    "name" => "advanced-tusk-cli",
    "version" => "2.0.0",
    "description" => "Advanced CLI with TuskLang features",
    "author" => "TuskLang Team",
    "global_options" => {
      "verbose" => {
        "type" => "boolean",
        "description" => "Enable verbose output",
        "short" => "v",
        "default" => false
      },
      "config" => {
        "type" => "string",
        "description" => "Configuration file path",
        "short" => "c",
        "default" => "peanu.tsk"
      },
      "log-level" => {
        "type" => "string",
        "description" => "Log level (debug, info, warn, error)",
        "choices" => ["debug", "info", "warn", "error"],
        "default" => "info"
      }
    },
    "commands" => {
      "deploy" => {
        "description" => "Deploy application to various environments",
        "handler" => "DeployCommand#execute",
        "options" => {
          "environment" => {
            "type" => "string",
            "description" => "Target environment",
            "required" => true,
            "choices" => ["development", "staging", "production"]
          },
          "force" => {
            "type" => "boolean",
            "description" => "Force deployment without confirmation",
            "short" => "f",
            "default" => false
          },
          "parallel" => {
            "type" => "integer",
            "description" => "Number of parallel deployments",
            "default" => 1
          }
        },
        "arguments" => [
          {
            "name" => "target",
            "description" => "Deployment target",
            "required" => true
          }
        ],
        "examples" => [
          "tusk deploy production web-server",
          "tusk deploy staging api-server --force",
          "tusk deploy development all --parallel 3"
        ]
      },
      "monitor" => {
        "description" => "Monitor application health and performance",
        "handler" => "MonitorCommand#execute",
        "subcommands" => {
          "status" => {
            "description" => "Check application status",
            "handler" => "MonitorCommand#status"
          },
          "logs" => {
            "description" => "View application logs",
            "handler" => "MonitorCommand#logs",
            "options" => {
              "tail" => {
                "type" => "boolean",
                "description" => "Follow log output",
                "short" => "f",
                "default" => false
              },
              "lines" => {
                "type" => "integer",
                "description" => "Number of lines to show",
                "default" => 100
              }
            }
          }
        }
      }
    },
    "themes" => {
      "default" => {
        "colors" => {
          "primary" => "blue",
          "success" => "green",
          "warning" => "yellow",
          "error" => "red"
        },
        "formatting" => {
          "indent" => 2,
          "max_width" => 80
        }
      },
      "dark" => {
        "colors" => {
          "primary" => "cyan",
          "success" => "bright_green",
          "warning" => "bright_yellow",
          "error" => "bright_red"
        }
      }
    }
  }
}

# Ruby processor for advanced CLI configurations
class AdvancedCliProcessor < CliDirectiveProcessor
  def process_advanced_config
    base_config = process_cli_directive
    cli_config = @config["#cli"]
    
    base_config.merge({
      author: cli_config["author"],
      themes: process_themes(cli_config["themes"]),
      plugins: process_plugins(cli_config["plugins"]),
      hooks: process_hooks(cli_config["hooks"]),
      completions: process_completions(cli_config["completions"])
    })
  end

  private

  def process_themes(themes_config)
    return {} unless themes_config

    themes_config.transform_values do |theme_config|
      {
        colors: theme_config["colors"] || {},
        formatting: theme_config["formatting"] || {},
        styles: theme_config["styles"] || {}
      }
    end
  end

  def process_plugins(plugins_config)
    return [] unless plugins_config

    plugins_config.map do |plugin_config|
      {
        name: plugin_config["name"],
        path: plugin_config["path"],
        enabled: plugin_config["enabled"] || true,
        config: plugin_config["config"] || {}
      }
    end
  end

  def process_hooks(hooks_config)
    return {} unless hooks_config

    {
      before_command: hooks_config["before_command"] || [],
      after_command: hooks_config["after_command"] || [],
      on_error: hooks_config["on_error"] || []
    }
  end

  def process_completions(completions_config)
    return {} unless completions_config

    {
      bash: completions_config["bash"],
      zsh: completions_config["zsh"],
      fish: completions_config["fish"]
    }
  end
end
```

## Command Execution and Argument Parsing

```ruby
# Command execution configuration with TuskLang features
command_execution_config = {
  "#cli" => {
    "commands" => {
      "process" => {
        "description" => "Process data with TuskLang pipelines",
        "handler" => "ProcessCommand#execute",
        "options" => {
          "input" => {
            "type" => "string",
            "description" => "Input file or data",
            "required" => true
          },
          "output" => {
            "type" => "string",
            "description" => "Output file",
            "required" => true
          },
          "pipeline" => {
            "type" => "string",
            "description" => "TuskLang pipeline configuration",
            "default" => "@default_pipeline"
          },
          "format" => {
            "type" => "string",
            "description" => "Output format",
            "choices" => ["json", "yaml", "csv", "xml"],
            "default" => "json"
          }
        },
        "arguments" => [
          {
            "name" => "operation",
            "description" => "Operation to perform",
            "required" => true,
            "choices" => ["transform", "validate", "analyze", "export"]
          }
        ],
        "validation" => {
          "input_file_exists" => true,
          "output_directory_writable" => true,
          "pipeline_valid" => true
        }
      }
    }
  }
}

# Ruby command execution processor
class CommandExecutionProcessor
  def initialize(config)
    @config = config["#cli"]
  end

  def execute_command(command_name, args, options)
    command_config = @config["commands"][command_name]
    return { success: false, error: "Command not found" } unless command_config

    # Validate arguments and options
    validation_result = validate_command(command_name, args, options)
    return validation_result unless validation_result[:valid]

    # Execute pre-hooks
    execute_hooks("before_command", command_name, args, options)

    # Execute command
    result = execute_handler(command_config["handler"], args, options)

    # Execute post-hooks
    execute_hooks("after_command", command_name, args, options, result)

    result
  end

  def validate_command(command_name, args, options)
    command_config = @config["commands"][command_name]
    
    # Validate required arguments
    required_args = command_config["arguments"]&.select { |arg| arg["required"] } || []
    if args.length < required_args.length
      return {
        valid: false,
        error: "Missing required arguments: #{required_args.map { |arg| arg['name'] }.join(', ')}"
      }
    end

    # Validate required options
    required_options = command_config["options"]&.select { |_, opt| opt["required"] } || {}
    missing_options = required_options.keys - options.keys
    if missing_options.any?
      return {
        valid: false,
        error: "Missing required options: #{missing_options.join(', ')}"
      }
    end

    # Validate option choices
    choice_errors = validate_option_choices(command_config["options"], options)
    return choice_errors if choice_errors[:valid] == false

    # Validate custom validation rules
    validation_errors = validate_custom_rules(command_config["validation"], args, options)
    return validation_errors if validation_errors[:valid] == false

    { valid: true }
  end

  private

  def validate_option_choices(options_config, options)
    return { valid: true } unless options_config

    options_config.each do |option_name, option_config|
      next unless option_config["choices"] && options[option_name]
      
      unless option_config["choices"].include?(options[option_name])
        return {
          valid: false,
          error: "Invalid value for #{option_name}. Must be one of: #{option_config['choices'].join(', ')}"
        }
      end
    end

    { valid: true }
  end

  def validate_custom_rules(validation_config, args, options)
    return { valid: true } unless validation_config

    validation_config.each do |rule, enabled|
      next unless enabled

      case rule
      when "input_file_exists"
        input_file = options["input"]
        unless File.exist?(input_file)
          return { valid: false, error: "Input file does not exist: #{input_file}" }
        end
      when "output_directory_writable"
        output_file = options["output"]
        output_dir = File.dirname(output_file)
        unless File.writable?(output_dir)
          return { valid: false, error: "Output directory is not writable: #{output_dir}" }
        end
      when "pipeline_valid"
        pipeline = options["pipeline"]
        unless valid_pipeline?(pipeline)
          return { valid: false, error: "Invalid pipeline configuration: #{pipeline}" }
        end
      end
    end

    { valid: true }
  end

  def execute_handler(handler_config, args, options)
    command_class = handler_config[:class].constantize
    command_instance = command_class.new
    
    command_instance.send(handler_config[:method], args, options)
  rescue => e
    {
      success: false,
      error: e.message,
      backtrace: e.backtrace.first(5)
    }
  end

  def execute_hooks(hook_type, command_name, args, options, result = nil)
    hooks = @config["hooks"]&.dig(hook_type) || []
    
    hooks.each do |hook|
      hook_class, hook_method = hook.split("#")
      hook_instance = hook_class.constantize.new
      hook_instance.send(hook_method, command_name, args, options, result)
    end
  end

  def valid_pipeline?(pipeline)
    # Implementation would validate TuskLang pipeline syntax
    true
  end
end
```

## Interactive CLI Features

```ruby
# Interactive CLI configuration
interactive_cli_config = {
  "#cli" => {
    "interactive" => {
      "enabled" => true,
      "prompt" => "tusk> ",
      "history_file" => "~/.tusk_history",
      "completions" => {
        "enabled" => true,
        "provider" => "readline"
      },
      "wizard" => {
        "enabled" => true,
        "steps" => {
          "project_setup" => {
            "title" => "Project Setup Wizard",
            "questions" => [
              {
                "name" => "project_name",
                "type" => "input",
                "message" => "What is your project name?",
                "required" => true,
                "validation" => "string|min:1|max:50"
              },
              {
                "name" => "template",
                "type" => "select",
                "message" => "Choose a project template:",
                "choices" => [
                  { "name" => "Web Application", "value" => "web" },
                  { "name" => "API Service", "value" => "api" },
                  { "name" => "CLI Tool", "value" => "cli" },
                  { "name" => "Library", "value" => "library" }
                ]
              },
              {
                "name" => "features",
                "type" => "multiselect",
                "message" => "Select features to include:",
                "choices" => [
                  { "name" => "Database Integration", "value" => "database" },
                  { "name" => "Authentication", "value" => "auth" },
                  { "name" => "API Documentation", "value" => "docs" },
                  { "name" => "Testing Framework", "value" => "testing" }
                ]
              }
            ]
          }
        }
      }
    },
    "commands" => {
      "wizard" => {
        "description" => "Interactive setup wizard",
        "handler" => "WizardCommand#execute",
        "options" => {
          "step" => {
            "type" => "string",
            "description" => "Start from specific step",
            "choices" => ["project_setup", "configuration", "deployment"]
          }
        }
      }
    }
  }
}

# Ruby interactive CLI processor
class InteractiveCliProcessor
  def initialize(config)
    @config = config["#cli"]["interactive"]
    @wizard_config = config["#cli"]["commands"]["wizard"]
  end

  def start_interactive_mode
    return unless @config["enabled"]

    setup_readline
    setup_history
    
    loop do
      begin
        input = Readline.readline(@config["prompt"], true)
        break if input.nil? || input.downcase == "exit"
        
        process_interactive_input(input)
      rescue Interrupt
        puts "\nExiting..."
        break
      rescue => e
        puts "Error: #{e.message}"
      end
    end
  end

  def run_wizard(wizard_name, options = {})
    wizard_config = @wizard_config["subcommands"][wizard_name]
    return { success: false, error: "Wizard not found" } unless wizard_config

    answers = {}
    
    wizard_config["questions"].each do |question|
      answer = ask_question(question)
      answers[question["name"]] = answer
    end

    # Execute wizard handler
    execute_wizard_handler(wizard_config["handler"], answers, options)
  end

  private

  def setup_readline
    return unless @config["completions"]["enabled"]

    Readline.completion_proc = proc do |input|
      generate_completions(input)
    end
  end

  def setup_history
    history_file = File.expand_path(@config["history_file"])
    FileUtils.mkdir_p(File.dirname(history_file))
    
    if File.exist?(history_file)
      File.readlines(history_file).each { |line| Readline::HISTORY << line.chomp }
    end
  end

  def process_interactive_input(input)
    return if input.strip.empty?

    # Parse command and arguments
    parts = input.split
    command = parts[0]
    args = parts[1..-1]

    # Execute command
    result = execute_command(command, args, {})
    
    if result[:success]
      puts result[:output] if result[:output]
    else
      puts "Error: #{result[:error]}"
    end
  end

  def ask_question(question)
    case question["type"]
    when "input"
      ask_input_question(question)
    when "select"
      ask_select_question(question)
    when "multiselect"
      ask_multiselect_question(question)
    when "confirm"
      ask_confirm_question(question)
    else
      ask_input_question(question)
    end
  end

  def ask_input_question(question)
    loop do
      print "#{question['message']} "
      answer = gets.chomp
      
      if validate_answer(answer, question["validation"])
        return answer
      else
        puts "Invalid input. Please try again."
      end
    end
  end

  def ask_select_question(question)
    puts question["message"]
    
    question["choices"].each_with_index do |choice, index|
      puts "#{index + 1}. #{choice['name']}"
    end
    
    loop do
      print "Enter your choice (1-#{question['choices'].length}): "
      choice = gets.chomp.to_i
      
      if choice.between?(1, question["choices"].length)
        return question["choices"][choice - 1]["value"]
      else
        puts "Invalid choice. Please try again."
      end
    end
  end

  def ask_multiselect_question(question)
    puts question["message"]
    
    question["choices"].each_with_index do |choice, index|
      puts "#{index + 1}. #{choice['name']}"
    end
    
    puts "Enter choices separated by commas (e.g., 1,3,4):"
    
    loop do
      print "Your choices: "
      choices = gets.chomp.split(",").map(&:strip).map(&:to_i)
      
      if choices.all? { |c| c.between?(1, question["choices"].length) }
        return choices.map { |c| question["choices"][c - 1]["value"] }
      else
        puts "Invalid choices. Please try again."
      end
    end
  end

  def ask_confirm_question(question)
    loop do
      print "#{question['message']} (y/n): "
      answer = gets.chomp.downcase
      
      case answer
      when "y", "yes"
        return true
      when "n", "no"
        return false
      else
        puts "Please answer 'y' or 'n'."
      end
    end
  end

  def validate_answer(answer, validation)
    return true unless validation

    validation_rules = validation.split("|")
    
    validation_rules.each do |rule|
      case rule
      when "required"
        return false if answer.blank?
      when /^min:(\d+)$/
        min_length = $1.to_i
        return false if answer.length < min_length
      when /^max:(\d+)$/
        max_length = $1.to_i
        return false if answer.length > max_length
      end
    end

    true
  end

  def generate_completions(input)
    # Generate command completions based on available commands
    commands = @config["commands"]&.keys || []
    commands.select { |cmd| cmd.start_with?(input) }
  end

  def execute_wizard_handler(handler_config, answers, options)
    handler_class, handler_method = handler_config.split("#")
    handler_instance = handler_class.constantize.new
    
    handler_instance.send(handler_method, answers, options)
  end
end
```

## Output Formatting and Themes

```ruby
# Output formatting configuration
output_config = {
  "#cli" => {
    "output" => {
      "format" => "table",
      "theme" => "default",
      "colors" => {
        "enabled" => true,
        "force" => false
      },
      "tables" => {
        "style" => "ascii",
        "border" => true,
        "padding" => 1
      },
      "progress" => {
        "style" => "bar",
        "width" => 50,
        "characters" => {
          "filled" => "█",
          "empty" => "░",
          "current" => "█"
        }
      },
      "spinners" => {
        "style" => "dots",
        "frames" => ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"]
      }
    },
    "themes" => {
      "default" => {
        "colors" => {
          "primary" => "blue",
          "success" => "green",
          "warning" => "yellow",
          "error" => "red",
          "info" => "cyan"
        },
        "styles" => {
          "bold" => true,
          "underline" => false
        }
      },
      "dark" => {
        "colors" => {
          "primary" => "bright_blue",
          "success" => "bright_green",
          "warning" => "bright_yellow",
          "error" => "bright_red",
          "info" => "bright_cyan"
        }
      }
    }
  }
}

# Ruby output formatter
class OutputFormatter
  def initialize(config)
    @config = config["#cli"]["output"]
    @themes = config["#cli"]["themes"]
    @current_theme = @themes[@config["theme"] || "default"]
  end

  def format_output(data, format = nil)
    format ||= @config["format"]
    
    case format
    when "table"
      format_table(data)
    when "json"
      format_json(data)
    when "yaml"
      format_yaml(data)
    when "csv"
      format_csv(data)
    when "list"
      format_list(data)
    else
      format_default(data)
    end
  end

  def progress_bar(current, total, label = nil)
    return unless @config["progress"]["style"] == "bar"

    width = @config["progress"]["width"]
    filled_char = @config["progress"]["characters"]["filled"]
    empty_char = @config["progress"]["characters"]["empty"]
    
    percentage = (current.to_f / total * 100).round(1)
    filled_width = (current.to_f / total * width).round
    
    bar = filled_char * filled_width + empty_char * (width - filled_width)
    
    output = "[#{bar}] #{percentage}%"
    output = "#{label}: #{output}" if label
    
    puts output
  end

  def spinner(message = "Processing...")
    return unless @config["spinners"]["style"] == "dots"

    frames = @config["spinners"]["frames"]
    frame_index = 0
    
    loop do
      print "\r#{frames[frame_index]} #{message}"
      frame_index = (frame_index + 1) % frames.length
      sleep 0.1
    end
  end

  def success(message)
    colorize(message, @current_theme["colors"]["success"])
  end

  def error(message)
    colorize(message, @current_theme["colors"]["error"])
  end

  def warning(message)
    colorize(message, @current_theme["colors"]["warning"])
  end

  def info(message)
    colorize(message, @current_theme["colors"]["info"])
  end

  private

  def format_table(data)
    return "No data to display" if data.empty?

    if data.is_a?(Array) && data.first.is_a?(Hash)
      format_hash_table(data)
    else
      format_simple_table(data)
    end
  end

  def format_hash_table(data)
    headers = data.first.keys
    rows = data.map(&:values)
    
    table = Terminal::Table.new(
      headings: headers,
      rows: rows,
      style: { border: @config["tables"]["border"] }
    )
    
    table.to_s
  end

  def format_simple_table(data)
    if data.is_a?(Hash)
      table = Terminal::Table.new do |t|
        data.each do |key, value|
          t << [key.to_s, value.to_s]
        end
      end
      table.to_s
    else
      data.to_s
    end
  end

  def format_json(data)
    JSON.pretty_generate(data)
  end

  def format_yaml(data)
    data.to_yaml
  end

  def format_csv(data)
    return "" if data.empty?

    if data.is_a?(Array) && data.first.is_a?(Hash)
      headers = data.first.keys
      CSV.generate do |csv|
        csv << headers
        data.each { |row| csv << row.values }
      end
    else
      data.to_s
    end
  end

  def format_list(data)
    if data.is_a?(Array)
      data.map.with_index { |item, index| "#{index + 1}. #{item}" }.join("\n")
    else
      data.to_s
    end
  end

  def format_default(data)
    data.to_s
  end

  def colorize(text, color)
    return text unless @config["colors"]["enabled"]
    
    color_code = get_color_code(color)
    "\e[#{color_code}m#{text}\e[0m"
  end

  def get_color_code(color)
    case color
    when "red" then 31
    when "green" then 32
    when "yellow" then 33
    when "blue" then 34
    when "magenta" then 35
    when "cyan" then 36
    when "bright_red" then 91
    when "bright_green" then 92
    when "bright_yellow" then 93
    when "bright_blue" then 94
    when "bright_magenta" then 95
    when "bright_cyan" then 96
    else 0
    end
  end
end
```

## Integration with Ruby CLI Frameworks

```ruby
# Thor integration example
class TuskThorApp < Thor
  include CliDirectiveProcessor

  def initialize
    @tusk_config = load_tusk_config
    @cli_processor = CliDirectiveProcessor.new(@tusk_config)
    @output_formatter = OutputFormatter.new(@tusk_config)
    setup_thor_commands
  end

  private

  def setup_thor_commands
    cli_config = @cli_processor.process_cli_directive
    return unless cli_config

    cli_config[:commands].each do |command_name, command_config|
      define_command_method(command_name, command_config)
    end
  end

  def define_command_method(command_name, command_config)
    self.class.class_eval do
      desc command_config[:description]
      
      command_config[:options]&.each do |option_name, option_config|
        option option_name.to_sym, 
               type: option_config[:type].to_sym,
               desc: option_config[:description],
               required: option_config[:required],
               default: option_config[:default]
      end
      
      define_method(command_name) do |*args|
        execute_tusk_command(command_name, args, options)
      end
    end
  end

  def execute_tusk_command(command_name, args, options)
    command_config = @cli_processor.process_cli_directive[:commands][command_name]
    return unless command_config

    handler_config = command_config[:handler]
    command_class = handler_config[:class].constantize
    command_instance = command_class.new
    
    result = command_instance.send(handler_config[:method], args, options)
    
    if result[:success]
      formatted_output = @output_formatter.format_output(result[:data])
      puts formatted_output
    else
      puts @output_formatter.error(result[:error])
    end
  end

  def load_tusk_config
    TuskConfig.load("peanu.tsk")
  end
end

# OptionParser integration example
class TuskOptionParser
  include CliDirectiveProcessor

  def initialize
    @tusk_config = load_tusk_config
    @cli_processor = CliDirectiveProcessor.new(@tusk_config)
    @output_formatter = OutputFormatter.new(@tusk_config)
  end

  def parse_arguments
    cli_config = @cli_processor.process_cli_directive
    return unless cli_config

    parser = OptionParser.new do |opts|
      opts.banner = "Usage: #{cli_config[:name]} [command] [options]"
      opts.version = cli_config[:version]
      
      # Global options
      cli_config[:global_options]&.each do |option_name, option_config|
        define_option(opts, option_name, option_config)
      end
      
      opts.on("-h", "--help", "Show this help message") do
        puts opts
        exit
      end
    end

    parser.parse!
  end

  def execute_command(command_name, args, options)
    command_config = @cli_processor.process_cli_directive[:commands][command_name]
    return { success: false, error: "Command not found" } unless command_config

    handler_config = command_config[:handler]
    command_class = handler_config[:class].constantize
    command_instance = command_class.new
    
    command_instance.send(handler_config[:method], args, options)
  end

  private

  def define_option(parser, option_name, option_config)
    short_flag = option_config[:short] ? "-#{option_config[:short]}" : nil
    long_flag = "--#{option_name}"
    
    parser.on(short_flag, long_flag, option_config[:description]) do |value|
      @options[option_name] = value || true
    end
  end

  def load_tusk_config
    TuskConfig.load("peanu.tsk")
  end
end
```

## Best Practices and Patterns

```ruby
# Best practices for CLI directive usage
class CliDirectiveBestPractices
  def self.validate_config(config)
    errors = []
    
    # Check required fields
    unless config["#cli"]
      errors << "Missing #cli directive"
      return errors
    end

    cli_config = config["#cli"]
    
    # Validate name
    if cli_config["name"]&.empty?
      errors << "CLI app name cannot be empty"
    end

    # Validate version
    unless cli_config["version"]&.match?(/^\d+\.\d+\.\d+$/)
      errors << "Version must be in semantic versioning format (e.g., 1.0.0)"
    end

    # Validate commands
    unless cli_config["commands"]&.any?
      errors << "At least one command must be defined"
    end

    # Validate command configurations
    cli_config["commands"]&.each do |command_name, command_config|
      command_errors = validate_command(command_name, command_config)
      errors.concat(command_errors)
    end

    errors
  end

  def self.optimize_config(config)
    cli_config = config["#cli"]
    
    # Set defaults
    cli_config["theme"] ||= "default"
    cli_config["output"] ||= {}
    cli_config["output"]["format"] ||= "table"
    cli_config["output"]["colors"] ||= {}
    cli_config["output"]["colors"]["enabled"] ||= true
    
    # Optimize commands
    cli_config["commands"]&.each do |command_name, command_config|
      command_config["aliases"] ||= []
      command_config["examples"] ||= []
    end

    config
  end

  def self.generate_documentation(config)
    cli_config = config["#cli"]
    
    {
      name: cli_config["name"],
      version: cli_config["version"],
      description: cli_config["description"],
      commands: cli_config["commands"]&.keys,
      global_options: cli_config["global_options"]&.keys,
      themes: cli_config["themes"]&.keys
    }
  end

  private

  def self.validate_command(command_name, command_config)
    errors = []
    
    unless command_config["description"]
      errors << "Command #{command_name} must have a description"
    end
    
    unless command_config["handler"]
      errors << "Command #{command_name} must have a handler"
    end

    # Validate handler format
    if command_config["handler"] && !command_config["handler"].include?("#")
      errors << "Handler must be in format 'Class#method'"
    end

    errors
  end
end

# Usage example
config = {
  "#cli" => {
    "name" => "my-tusk-cli",
    "version" => "1.0.0",
    "description" => "A powerful CLI built with TuskLang",
    "commands" => {
      "init" => {
        "description" => "Initialize a new project",
        "handler" => "InitCommand#execute"
      }
    }
  }
}

# Validate and optimize
errors = CliDirectiveBestPractices.validate_config(config)
if errors.empty?
  optimized_config = CliDirectiveBestPractices.optimize_config(config)
  documentation = CliDirectiveBestPractices.generate_documentation(config)
  
  puts "Configuration is valid!"
  puts "Documentation: #{documentation}"
else
  puts "Configuration errors: #{errors}"
end
```

## Conclusion

The Hash CLI Directive in TuskLang represents a revolutionary approach to command-line application development. By combining declarative configuration with Ruby's powerful CLI capabilities, you can create sophisticated, maintainable command-line applications with minimal boilerplate code.

Key benefits:
- **Declarative Configuration**: Define complete CLI applications in simple hash structures
- **Ruby Integration**: Leverage Ruby's CLI frameworks and ecosystem
- **Interactive Features**: Built-in wizard and interactive mode support
- **Output Formatting**: Flexible output formatting with themes and colors
- **Argument Validation**: Comprehensive argument and option validation
- **Command Execution**: Powerful command execution with hooks and error handling
- **Performance Optimization**: Built-in progress bars, spinners, and user feedback

Remember, TuskLang is about breaking free from conventions and embracing the power of declarative, configuration-driven development. The Hash CLI Directive is your gateway to building command-line applications that are as expressive as they are powerful! 