#compdef tsk

# TuskLang CLI Zsh Completion
# Provides intelligent completion for tsk command with advanced features

_tsk() {
    local curcontext="$curcontext" state line
    typeset -A opt_args

    _arguments -C \
        '1: :->cmds' \
        '*:: :->args'

    case $state in
        cmds)
            local commands
            commands=(
                'parse:Parse TuskLang configuration file'
                'validate:Validate TuskLang configuration'
                'get:Get value from configuration'
                'set:Set value in configuration'
                'convert:Convert between configuration formats'
                'db:Database operations'
                'binary:Binary configuration operations'
                'ai:AI-powered analysis and optimization'
                'cache:Cache management'
                'service:Service management'
                'config:Configuration management'
                'help:Show help information'
                'version:Show version information'
            )
            _describe -t commands 'tsk commands' commands
            ;;
        args)
            case $line[1] in
                parse)
                    _tsk_parse
                    ;;
                validate)
                    _tsk_validate
                    ;;
                get)
                    _tsk_get
                    ;;
                set)
                    _tsk_set
                    ;;
                convert)
                    _tsk_convert
                    ;;
                db)
                    _tsk_db
                    ;;
                binary)
                    _tsk_binary
                    ;;
                ai)
                    _tsk_ai
                    ;;
                cache)
                    _tsk_cache
                    ;;
                service)
                    _tsk_service
                    ;;
                config)
                    _tsk_config
                    ;;
            esac
            ;;
    esac
}

_tsk_parse() {
    _arguments \
        '--file[Configuration file to parse]:file:_files -g "*.tsk"' \
        '--output[Output file]:file:_files' \
        '--format[Output format]:(json yaml tsk tusk)' \
        '--pretty[Pretty print output]' \
        '--validate[Validate after parsing]' \
        '--help[Show help]'
}

_tsk_validate() {
    _arguments \
        '--file[Configuration file to validate]:file:_files -g "*.tsk"' \
        '--schema[Schema file for validation]:file:_files -g "*.json"' \
        '--strict[Strict validation mode]' \
        '--output[Output file]:file:_files' \
        '--help[Show help]'
}

_tsk_get() {
    _arguments \
        '--file[Configuration file]:file:_files -g "*.tsk"' \
        '--key[Key to get]:key' \
        '--section[Section name]:section' \
        '--default[Default value]:value' \
        '--help[Show help]'
}

_tsk_set() {
    _arguments \
        '--file[Configuration file]:file:_files -g "*.tsk"' \
        '--key[Key to set]:key' \
        '--value[Value to set]:value' \
        '--section[Section name]:section' \
        '--type[Value type]:(string number boolean array object)' \
        '--help[Show help]'
}

_tsk_convert() {
    _arguments \
        '--from[Source format]:(tsk json yaml tusk)' \
        '--to[Target format]:(tsk json yaml tusk)' \
        '--input[Input file]:file:_files -g "*.{tsk,json,yaml}"' \
        '--output[Output file]:file:_files' \
        '--format[Output format]:(json yaml tsk tusk)' \
        '--help[Show help]'
}

_tsk_db() {
    local db_commands
    db_commands=(
        'status:Show database status'
        'migrate:Run database migrations'
        'backup:Create database backup'
        'restore:Restore database from backup'
        'query:Execute database query'
        'execute:Execute database script'
    )
    
    if (( CURRENT == 2 )); then
        _describe -t db-commands 'db commands' db_commands
    else
        case $words[2] in
            migrate)
                _arguments \
                    '--database[Database name]:database' \
                    '--adapter[Database adapter]:(sqlite postgres mysql mongodb redis)' \
                    '--force[Force migration]' \
                    '--dry-run[Show what would be done]' \
                    '--help[Show help]'
                ;;
            backup)
                _arguments \
                    '--database[Database name]:database' \
                    '--output[Backup file]:file:_files' \
                    '--format[Backup format]:(sql json binary)' \
                    '--compress[Compress backup]' \
                    '--help[Show help]'
                ;;
            restore)
                _arguments \
                    '--database[Database name]:database' \
                    '--input[Backup file]:file:_files -g "*.{sql,json,bak}"' \
                    '--format[Backup format]:(sql json binary)' \
                    '--force[Force restore]' \
                    '--help[Show help]'
                ;;
            query)
                _arguments \
                    '--database[Database name]:database' \
                    '--sql[SQL query]:query' \
                    '--params[Query parameters]:params' \
                    '--output[Output file]:file:_files' \
                    '--help[Show help]'
                ;;
            execute)
                _arguments \
                    '--database[Database name]:database' \
                    '--file[SQL file]:file:_files -g "*.sql"' \
                    '--params[Query parameters]:params' \
                    '--help[Show help]'
                ;;
        esac
    fi
}

_tsk_binary() {
    local binary_commands
    binary_commands=(
        'compile:Compile configuration to binary'
        'execute:Execute binary configuration'
        'benchmark:Benchmark binary operations'
        'validate:Validate binary configuration'
    )
    
    if (( CURRENT == 2 )); then
        _describe -t binary-commands 'binary commands' binary_commands
    else
        case $words[2] in
            compile)
                _arguments \
                    '--input[Input file]:file:_files -g "*.tsk"' \
                    '--output[Output file]:file:_files -g "*.pnt"' \
                    '--optimize[Optimization level]:(0 1 2)' \
                    '--help[Show help]'
                ;;
            execute)
                _arguments \
                    '--input[Binary file]:file:_files -g "*.pnt"' \
                    '--params[Parameters]:params' \
                    '--output[Output file]:file:_files' \
                    '--help[Show help]'
                ;;
            benchmark)
                _arguments \
                    '--input[Input file]:file:_files -g "*.{tsk,pnt}"' \
                    '--iterations[Number of iterations]:number' \
                    '--output[Output file]:file:_files' \
                    '--help[Show help]'
                ;;
            validate)
                _arguments \
                    '--input[Binary file]:file:_files -g "*.pnt"' \
                    '--schema[Schema file]:file:_files -g "*.json"' \
                    '--help[Show help]'
                ;;
        esac
    fi
}

_tsk_ai() {
    local ai_commands
    ai_commands=(
        'query:Query AI for configuration help'
        'analyze:Analyze configuration with AI'
        'optimize:Optimize configuration with AI'
        'suggest:Suggest improvements with AI'
    )
    
    if (( CURRENT == 2 )); then
        _describe -t ai-commands 'ai commands' ai_commands
    else
        case $words[2] in
            query)
                _arguments \
                    '--model[AI model]:(claude gpt-4 gpt-3.5-turbo)' \
                    '--prompt[Query prompt]:prompt' \
                    '--config[Configuration context]:file:_files -g "*.tsk"' \
                    '--output[Output file]:file:_files' \
                    '--help[Show help]'
                ;;
            analyze)
                _arguments \
                    '--config[Configuration file]:file:_files -g "*.tsk"' \
                    '--aspects[Analysis aspects]:(security performance structure)' \
                    '--output[Output file]:file:_files' \
                    '--help[Show help]'
                ;;
            optimize)
                _arguments \
                    '--config[Configuration file]:file:_files -g "*.tsk"' \
                    '--target[Optimization target]:(performance security readability)' \
                    '--output[Output file]:file:_files' \
                    '--help[Show help]'
                ;;
            suggest)
                _arguments \
                    '--config[Configuration file]:file:_files -g "*.tsk"' \
                    '--context[Additional context]:context' \
                    '--output[Output file]:file:_files' \
                    '--help[Show help]'
                ;;
        esac
    fi
}

_tsk_cache() {
    local cache_commands
    cache_commands=(
        'clear:Clear cache entries'
        'list:List cache entries'
        'info:Show cache information'
        'stats:Show cache statistics'
    )
    
    if (( CURRENT == 2 )); then
        _describe -t cache-commands 'cache commands' cache_commands
    else
        case $words[2] in
            clear)
                _arguments \
                    '--all[Clear all cache]' \
                    '--pattern[Pattern to match]:pattern' \
                    '--older-than[Clear entries older than]:time' \
                    '--help[Show help]'
                ;;
            list)
                _arguments \
                    '--pattern[Pattern to match]:pattern' \
                    '--format[Output format]:(table json yaml)' \
                    '--help[Show help]'
                ;;
            info)
                _arguments \
                    '--key[Cache key]:key' \
                    '--detailed[Show detailed information]' \
                    '--help[Show help]'
                ;;
            stats)
                _arguments \
                    '--format[Output format]:(table json yaml)' \
                    '--output[Output file]:file:_files' \
                    '--help[Show help]'
                ;;
        esac
    fi
}

_tsk_service() {
    local service_commands
    service_commands=(
        'start:Start TuskLang service'
        'stop:Stop TuskLang service'
        'restart:Restart TuskLang service'
        'status:Show service status'
        'logs:Show service logs'
    )
    
    if (( CURRENT == 2 )); then
        _describe -t service-commands 'service commands' service_commands
    else
        case $words[2] in
            start)
                _arguments \
                    '--config[Service config]:file:_files -g "*.tsk"' \
                    '--daemon[Run as daemon]' \
                    '--port[Service port]:port' \
                    '--host[Service host]:host' \
                    '--help[Show help]'
                ;;
            stop)
                _arguments \
                    '--force[Force stop]' \
                    '--timeout[Stop timeout]:timeout' \
                    '--help[Show help]'
                ;;
            restart)
                _arguments \
                    '--config[Service config]:file:_files -g "*.tsk"' \
                    '--force[Force restart]' \
                    '--help[Show help]'
                ;;
            status)
                _arguments \
                    '--detailed[Show detailed status]' \
                    '--format[Output format]:(table json yaml)' \
                    '--help[Show help]'
                ;;
            logs)
                _arguments \
                    '--follow[Follow logs]' \
                    '--lines[Number of lines]:number' \
                    '--level[Log level]:(debug info warn error)' \
                    '--help[Show help]'
                ;;
        esac
    fi
}

_tsk_config() {
    local config_commands
    config_commands=(
        'show:Show configuration'
        'edit:Edit configuration'
        'reset:Reset configuration'
        'backup:Backup configuration'
    )
    
    if (( CURRENT == 2 )); then
        _describe -t config-commands 'config commands' config_commands
    else
        case $words[2] in
            show)
                _arguments \
                    '--format[Output format]:(table json yaml)' \
                    '--section[Section to show]:section' \
                    '--help[Show help]'
                ;;
            edit)
                _arguments \
                    '--editor[Editor to use]:editor' \
                    '--section[Section to edit]:section' \
                    '--help[Show help]'
                ;;
            reset)
                _arguments \
                    '--section[Section to reset]:section' \
                    '--force[Force reset]' \
                    '--help[Show help]'
                ;;
            backup)
                _arguments \
                    '--output[Backup file]:file:_files' \
                    '--format[Backup format]:(json yaml)' \
                    '--help[Show help]'
                ;;
        esac
    fi
}

# Register the completion function
compdef _tsk tsk

# Also register for common aliases
compdef _tsk tusklang
compdef _tsk tusk 