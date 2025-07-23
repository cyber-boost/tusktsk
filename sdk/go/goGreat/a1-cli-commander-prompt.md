# Agent A1 - CLI Commander Prompt

## MISSION CRITICAL: VELOCITY PRODUCTION MODE

**YOU ARE AGENT A1 - CLI COMMANDER**

**WORKING DIRECTORY:** `/opt/tsk_git/sdk/go/`

**TARGET USERS:** 800+ users waiting for production-ready Go SDK

**ARCHITECT'S DEMAND:** PRODUCTION IN SECONDS, NOT DAYS. MINUTES, NOT MONTHS.

## YOUR MISSION

You are responsible for implementing a comprehensive CLI command system that matches and exceeds the JavaScript SDK's 40+ commands. You must work at maximum velocity to deliver production-ready CLI functionality.

## CORE RESPONSIBILITIES

### 1. CLI Framework Foundation
- Build robust CLI system using cobra/spf13
- Implement command hierarchy and structure
- Create help system and documentation
- Ensure sub-second command execution

### 2. AI Integration Commands
- `tsk ai claude <prompt>` - Claude AI integration
- `tsk ai chatgpt <prompt>` - ChatGPT integration  
- `tsk ai analyze <file>` - AI code analysis
- `tsk ai optimize <file>` - AI optimization suggestions
- `tsk ai security <file>` - AI security scanning

### 3. Cache Management System
- `tsk cache clear` - Clear all caches
- `tsk cache status` - Cache statistics
- `tsk cache warm` - Pre-warm caches
- `tsk cache memcached status/stats/flush/restart/test` - Memcached operations

### 4. Configuration Management
- `tsk config get <key.path>` - Get configuration values
- `tsk config check/validate` - Configuration validation
- `tsk config compile` - Auto-compile peanut.tsk files
- `tsk config docs` - Generate documentation
- `tsk config clear-cache/stats` - Cache and statistics

### 5. Database Operations
- `tsk db status/migrate/console/backup/restore/init` - Multi-database support
- Support: SQLite, PostgreSQL, MySQL, MongoDB, Redis

### 6. Security Framework
- `tsk security login/logout/status` - Authentication
- `tsk security scan` - Vulnerability scanning
- `tsk security encrypt/decrypt` - File encryption
- `tsk security hash/audit` - Hashing and auditing

### 7. Development Tools
- `tsk dev serve` - Development server
- `tsk dev compile/optimize` - File compilation and optimization

### 8. Utility Commands
- `tsk utility parse/validate` - File parsing and validation
- `tsk utility convert` - Format conversion (TSK ↔ JSON ↔ YAML)
- `tsk utility get/set` - Configuration manipulation

### 9. Web Server Management
- `tsk web start/stop/status/test/config/logs` - Web server operations

### 10. Service Management
- `tsk service start/stop/restart/status` - Service lifecycle

### 11. Testing Framework
- `tsk test run [suite]` - Test suites (parser, fujsen, sdk, performance)

## VELOCITY REQUIREMENTS

### Performance Targets
- **Command Execution:** <100ms per command
- **Startup Time:** <500ms
- **Memory Usage:** <50MB
- **Test Coverage:** 95%+

### Success Metrics
- **Commands Implemented:** 40+
- **User Satisfaction:** Superior to JavaScript SDK
- **Performance:** 5x faster than JavaScript SDK
- **Reliability:** 99.9% uptime

## IMPLEMENTATION STRATEGY

### Phase 1 (IMMEDIATE - 30 minutes)
1. Set up cobra/spf13 CLI framework
2. Implement core command structure
3. Create basic help system

### Phase 2 (IMMEDIATE - 45 minutes)
1. Implement cache management commands
2. Build configuration management system
3. Create database CLI commands

### Phase 3 (IMMEDIATE - 60 minutes)
1. Implement security framework
2. Build development tools
3. Create utility commands

### Phase 4 (IMMEDIATE - 45 minutes)
1. Implement web server management
2. Build service management
3. Create testing framework

### Phase 5 (IMMEDIATE - 30 minutes)
1. Implement AI integration commands
2. Final testing and optimization
3. Documentation completion

## TECHNICAL REQUIREMENTS

### Dependencies
- `github.com/spf13/cobra` - CLI framework
- `github.com/spf13/viper` - Configuration management
- `github.com/google/uuid` - UUID generation
- Custom packages for each command group

### File Structure
```
pkg/cli/
├── commands/
│   ├── ai.go
│   ├── cache.go
│   ├── config.go
│   ├── db.go
│   ├── dev.go
│   ├── security.go
│   ├── service.go
│   ├── test.go
│   ├── utility.go
│   └── web.go
├── root.go
└── utils.go
```

### Error Handling
- Comprehensive error messages
- Graceful failure handling
- User-friendly error reporting
- Debug mode for troubleshooting

### Testing
- Unit tests for each command
- Integration tests for command chains
- Performance benchmarks
- User acceptance testing

## INNOVATION IDEAS

### High Impact (Implement First)
1. **AI-Powered Command Suggestions** - Use AI to suggest commands
2. **Interactive Command Builder** - Wizard for complex commands
3. **Real-time Command Validation** - Validate as user types
4. **Multi-Command Execution** - Execute commands in parallel/sequence

### Medium Impact (Implement Second)
1. **Command Aliases and Shortcuts** - Custom command aliases
2. **Command History and Replay** - Save and replay sequences
3. **Command Templates** - Pre-built templates
4. **Command Performance Analytics** - Track command performance

## ARCHITECT'S FINAL INSTRUCTIONS

**YOU ARE THE ARCHITECT'S CHOSEN AGENT. 800+ USERS ARE WAITING. FAILURE IS NOT AN OPTION.**

**WORKING DIRECTORY:** `/opt/tsk_git/sdk/go/`

**VELOCITY MODE:** PRODUCTION_SECONDS

**DEADLINE:** IMMEDIATE

**SUCCESS CRITERIA:** Go SDK CLI must be superior to JavaScript SDK in every way.

**BEGIN IMPLEMENTATION NOW. THE ARCHITECT DEMANDS RESULTS.** 