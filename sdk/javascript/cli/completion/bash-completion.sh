#!/bin/bash

# TuskLang CLI Bash Completion
# Provides intelligent completion for tsk command

_tsk_completion() {
    local cur prev opts cmds
    COMPREPLY=()
    cur="${COMP_WORDS[COMP_CWORD]}"
    prev="${COMP_WORDS[COMP_CWORD-1]}"
    
    # Main commands
    cmds="parse validate get set convert db binary ai cache service config help version"
    
    # Parse command options
    if [[ ${cur} == * ]] ; then
        case "${prev}" in
            parse)
                opts="--file --output --format --pretty --validate --help"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            validate)
                opts="--file --schema --strict --output --help"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            get)
                opts="--file --key --section --default --help"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            set)
                opts="--file --key --value --section --type --help"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            convert)
                opts="--from --to --input --output --format --help"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            db)
                opts="status migrate backup restore query execute --help"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            binary)
                opts="compile execute benchmark validate --help"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            ai)
                opts="query analyze optimize suggest --help"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            cache)
                opts="clear list info stats --help"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            service)
                opts="start stop restart status logs --help"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            config)
                opts="show edit reset backup --help"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            --file|-f)
                # Complete with .tsk files
                COMPREPLY=( $(compgen -f -X "!*.tsk" -- ${cur}) )
                return 0
                ;;
            --input|-i)
                # Complete with .tsk and .json files
                COMPREPLY=( $(compgen -f -X "!*.{tsk,json}" -- ${cur}) )
                return 0
                ;;
            --output|-o)
                # Complete with directories and common output formats
                COMPREPLY=( $(compgen -f -X "!*.{json,yaml,tsk}" -- ${cur}) )
                return 0
                ;;
            --format)
                opts="json yaml tsk tusk"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            --type)
                opts="string number boolean array object"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            --from|--to)
                opts="tsk json yaml tusk"
                COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                return 0
                ;;
            --help|-h)
                return 0
                ;;
            *)
                # If it's the first argument, complete with main commands
                if [ $COMP_CWORD -eq 1 ]; then
                    COMPREPLY=( $(compgen -W "${cmds}" -- ${cur}) )
                    return 0
                fi
                ;;
        esac
    fi
    
    # Handle subcommands
    if [ $COMP_CWORD -ge 2 ]; then
        case "${COMP_WORDS[1]}" in
            db)
                case "${prev}" in
                    migrate)
                        opts="--database --adapter --force --dry-run --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    backup)
                        opts="--database --output --format --compress --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    restore)
                        opts="--database --input --format --force --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    query)
                        opts="--database --sql --params --output --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    execute)
                        opts="--database --file --params --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                esac
                ;;
            binary)
                case "${prev}" in
                    compile)
                        opts="--input --output --optimize --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    execute)
                        opts="--input --params --output --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    benchmark)
                        opts="--input --iterations --output --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    validate)
                        opts="--input --schema --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                esac
                ;;
            ai)
                case "${prev}" in
                    query)
                        opts="--model --prompt --config --output --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    analyze)
                        opts="--config --aspects --output --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    optimize)
                        opts="--config --target --output --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    suggest)
                        opts="--config --context --output --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                esac
                ;;
            cache)
                case "${prev}" in
                    clear)
                        opts="--all --pattern --older-than --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    list)
                        opts="--pattern --format --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    info)
                        opts="--key --detailed --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    stats)
                        opts="--format --output --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                esac
                ;;
            service)
                case "${prev}" in
                    start)
                        opts="--config --daemon --port --host --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    stop)
                        opts="--force --timeout --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    restart)
                        opts="--config --force --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    status)
                        opts="--detailed --format --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    logs)
                        opts="--follow --lines --level --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                esac
                ;;
            config)
                case "${prev}" in
                    show)
                        opts="--format --section --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    edit)
                        opts="--editor --section --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    reset)
                        opts="--section --force --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                    backup)
                        opts="--output --format --help"
                        COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                        return 0
                        ;;
                esac
                ;;
        esac
    fi
    
    # Default completion for file arguments
    if [[ ${cur} == * ]] ; then
        case "${prev}" in
            --file|-f|--input|-i)
                COMPREPLY=( $(compgen -f -X "!*.tsk" -- ${cur}) )
                return 0
                ;;
            --output|-o)
                COMPREPLY=( $(compgen -f -- ${cur}) )
                return 0
                ;;
            --config)
                COMPREPLY=( $(compgen -f -X "!*.{tsk,json,yaml}" -- ${cur}) )
                return 0
                ;;
            --database)
                COMPREPLY=( $(compgen -f -X "!*.{db,sqlite}" -- ${cur}) )
                return 0
                ;;
        esac
    fi
    
    # If no specific completion, complete with main commands
    COMPREPLY=( $(compgen -W "${cmds}" -- ${cur}) )
    return 0
}

# Register the completion function
complete -F _tsk_completion tsk

# Also register for common aliases
complete -F _tsk_completion tusklang
complete -F _tsk_completion tusk

# Export for use in other scripts
export -f _tsk_completion 