/**
 * Advanced CLI Framework with Interactive Mode and Auto-completion
 * Goal 7.3 Implementation
 */

const readline = require('readline');
const EventEmitter = require('events');
const fs = require('fs').promises;
const path = require('path');

class AdvancedCLI extends EventEmitter {
    constructor(options = {}) {
        super();
        this.prompt = options.prompt || 'tusk> ';
        this.history = [];
        this.historyIndex = 0;
        this.commands = new Map();
        this.aliases = new Map();
        this.helpText = new Map();
        this.isInteractive = false;
        this.rl = null;
        this.autoCompleteEnabled = options.autoComplete !== false;
        this.maxHistory = options.maxHistory || 1000;
        
        this.registerDefaultCommands();
    }

    /**
     * Start interactive CLI mode
     */
    async startInteractive() {
        this.isInteractive = true;
        this.rl = readline.createInterface({
            input: process.stdin,
            output: process.stdout,
            prompt: this.prompt,
            completer: this.autoCompleteEnabled ? this.createCompleter() : undefined
        });

        // Handle line input
        this.rl.on('line', (input) => {
            this.processInput(input.trim());
        });

        // Handle close
        this.rl.on('close', () => {
            this.isInteractive = false;
            this.emit('exit');
        });

        // Display welcome message
        this.displayWelcome();
        
        // Start prompt
        this.rl.prompt();
        
        this.emit('started');
    }

    /**
     * Stop interactive CLI mode
     */
    stopInteractive() {
        if (this.rl) {
            this.rl.close();
            this.rl = null;
        }
        this.isInteractive = false;
        this.emit('stopped');
    }

    /**
     * Process input command
     */
    async processInput(input) {
        if (!input) {
            this.rl.prompt();
            return;
        }

        // Add to history
        this.addToHistory(input);

        try {
            const result = await this.executeCommand(input);
            if (result !== undefined) {
                console.log(result);
            }
        } catch (error) {
            console.error(`✗ Error: ${error.message}`);
        }

        if (this.isInteractive) {
            this.rl.prompt();
        }
    }

    /**
     * Execute command
     */
    async executeCommand(input) {
        const parts = this.parseCommand(input);
        const commandName = parts.command;
        const args = parts.args;
        const options = parts.options;

        // Check for aliases
        const actualCommand = this.aliases.get(commandName) || commandName;

        // Get command handler
        const command = this.commands.get(actualCommand);
        if (!command) {
            throw new Error(`Unknown command: ${commandName}. Use 'help' for available commands.`);
        }

        // Execute command
        return await command.handler(args, options);
    }

    /**
     * Parse command input
     */
    parseCommand(input) {
        const parts = input.split(' ');
        const command = parts[0];
        const args = [];
        const options = {};

        for (let i = 1; i < parts.length; i++) {
            const part = parts[i];
            if (part.startsWith('--')) {
                // Long option
                const [key, value] = part.substring(2).split('=');
                options[key] = value || true;
            } else if (part.startsWith('-')) {
                // Short option
                const key = part.substring(1);
                if (i + 1 < parts.length && !parts[i + 1].startsWith('-')) {
                    options[key] = parts[++i];
                } else {
                    options[key] = true;
                }
            } else {
                // Argument
                args.push(part);
            }
        }

        return { command, args, options };
    }

    /**
     * Register command
     */
    registerCommand(name, handler, help = '', aliases = []) {
        this.commands.set(name, { handler, help });
        this.helpText.set(name, help);

        // Register aliases
        for (const alias of aliases) {
            this.aliases.set(alias, name);
        }
    }

    /**
     * Register default commands
     */
    registerDefaultCommands() {
        // Help command
        this.registerCommand('help', async (args, options) => {
            if (args.length === 0) {
                return this.displayHelp();
            } else {
                return this.displayCommandHelp(args[0]);
            }
        }, 'Display help information', ['h', '?']);

        // Exit command
        this.registerCommand('exit', async () => {
            if (this.isInteractive) {
                this.stopInteractive();
            }
            process.exit(0);
        }, 'Exit the CLI', ['quit', 'q']);

        // Clear command
        this.registerCommand('clear', async () => {
            console.clear();
        }, 'Clear the screen', ['cls']);

        // History command
        this.registerCommand('history', async (args, options) => {
            const limit = options.limit || 10;
            const recent = this.history.slice(-limit);
            return recent.map((cmd, index) => `${index + 1}: ${cmd}`).join('\n');
        }, 'Show command history', ['hist']);

        // Config command
        this.registerCommand('config', async (args, options) => {
            if (args.length === 0) {
                return 'Usage: config <get|set|list> [key] [value]';
            }
            
            const action = args[0];
            switch (action) {
                case 'get':
                    if (args.length < 2) return 'Usage: config get <key>';
                    return await this.getConfig(args[1]);
                case 'set':
                    if (args.length < 3) return 'Usage: config set <key> <value>';
                    return await this.setConfig(args[1], args[2]);
                case 'list':
                    return await this.listConfig();
                default:
                    return `Unknown config action: ${action}`;
            }
        }, 'Manage configuration', ['cfg']);

        // Sync command
        this.registerCommand('sync', async (args, options) => {
            const action = args[0] || 'status';
            switch (action) {
                case 'start':
                    return await this.startSync();
                case 'stop':
                    return await this.stopSync();
                case 'status':
                    return await this.getSyncStatus();
                default:
                    return `Unknown sync action: ${action}`;
            }
        }, 'Manage real-time synchronization', ['realtime']);

        // Binary command
        this.registerCommand('binary', async (args, options) => {
            const action = args[0];
            if (!action) return 'Usage: binary <encode|decode> [file]';
            
            switch (action) {
                case 'encode':
                    if (args.length < 2) return 'Usage: binary encode <file>';
                    return await this.encodeBinary(args[1], options);
                case 'decode':
                    if (args.length < 2) return 'Usage: binary decode <file>';
                    return await this.decodeBinary(args[1], options);
                default:
                    return `Unknown binary action: ${action}`;
            }
        }, 'Binary format operations', ['bin']);
    }

    /**
     * Display welcome message
     */
    displayWelcome() {
        console.log(`
🚀 TuskLang Advanced CLI v1.0.0
================================
Type 'help' for available commands
Type 'exit' to quit

Features:
- Interactive command completion
- Command history
- Real-time configuration sync
- Binary format support
- Advanced configuration management

`);
    }

    /**
     * Display help
     */
    displayHelp() {
        const commands = Array.from(this.commands.keys()).sort();
        const helpText = commands.map(cmd => {
            const help = this.helpText.get(cmd) || 'No description available';
            const aliases = Array.from(this.aliases.entries())
                .filter(([alias, command]) => command === cmd)
                .map(([alias]) => alias);
            
            const aliasText = aliases.length > 0 ? ` (aliases: ${aliases.join(', ')})` : '';
            return `  ${cmd}${aliasText}\n    ${help}`;
        }).join('\n\n');

        return `
Available Commands:
==================

${helpText}

For detailed help on a specific command, use: help <command>
`;
    }

    /**
     * Display command-specific help
     */
    displayCommandHelp(commandName) {
        const actualCommand = this.aliases.get(commandName) || commandName;
        const help = this.helpText.get(actualCommand);
        
        if (!help) {
            return `No help available for command: ${commandName}`;
        }

        return `
Help for '${commandName}':
=======================

${help}

Usage examples:
- ${commandName} [args] [options]
`;
    }

    /**
     * Create auto-completer function
     */
    createCompleter() {
        return (line) => {
            const completions = [];
            const commands = Array.from(this.commands.keys());
            const aliases = Array.from(this.aliases.keys());
            const allCommands = [...commands, ...aliases];

            if (line.length === 0) {
                return [allCommands, line];
            }

            const hits = allCommands.filter(cmd => cmd.startsWith(line));
            return [hits.length ? hits : allCommands, line];
        };
    }

    /**
     * Add command to history
     */
    addToHistory(command) {
        this.history.push(command);
        if (this.history.length > this.maxHistory) {
            this.history.shift();
        }
        this.historyIndex = this.history.length;
    }

    /**
     * Configuration management methods
     */
    async getConfig(key) {
        // Implementation would integrate with actual config system
        return `Config value for '${key}': (not implemented)`;
    }

    async setConfig(key, value) {
        // Implementation would integrate with actual config system
        return `Set config '${key}' = '${value}' (not implemented)`;
    }

    async listConfig() {
        // Implementation would integrate with actual config system
        return 'Configuration listing (not implemented)';
    }

    /**
     * Sync management methods
     */
    async startSync() {
        // Implementation would integrate with RealtimeSyncManager
        return 'Real-time sync started (not implemented)';
    }

    async stopSync() {
        // Implementation would integrate with RealtimeSyncManager
        return 'Real-time sync stopped (not implemented)';
    }

    async getSyncStatus() {
        // Implementation would integrate with RealtimeSyncManager
        return 'Sync status: (not implemented)';
    }

    /**
     * Binary format methods
     */
    async encodeBinary(filePath, options) {
        // Implementation would integrate with BinaryFormatManager
        return `Binary encoding for '${filePath}' (not implemented)`;
    }

    async decodeBinary(filePath, options) {
        // Implementation would integrate with BinaryFormatManager
        return `Binary decoding for '${filePath}' (not implemented)`;
    }

    /**
     * Get CLI statistics
     */
    getStats() {
        return {
            isInteractive: this.isInteractive,
            commands: this.commands.size,
            aliases: this.aliases.size,
            historySize: this.history.length,
            autoCompleteEnabled: this.autoCompleteEnabled
        };
    }
}

module.exports = { AdvancedCLI }; 