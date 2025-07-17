# TuskLang Rust SDK

## Universal CLI Command Structure

The Rust SDK now implements the full TuskLang Universal CLI Command Specification. All commands are available via the `tusk-rust` CLI.

### Database Commands
- `tusk-rust db status`
- `tusk-rust db migrate <file>`
- `tusk-rust db console`
- `tusk-rust db backup [file]`
- `tusk-rust db restore <file>`
- `tusk-rust db init`

### Development Commands
- `tusk-rust dev serve [port]`
- `tusk-rust dev compile <file>`
- `tusk-rust dev optimize <file>`

### Testing Commands
- `tusk-rust test suite [suite]`
- `tusk-rust test all`
- `tusk-rust test parser`
- `tusk-rust test fujsen`
- `tusk-rust test sdk`
- `tusk-rust test performance`

### Service Commands
- `tusk-rust services start`
- `tusk-rust services stop`
- `tusk-rust services restart`
- `tusk-rust services status`

### Cache Commands
- `tusk-rust cache clear`
- `tusk-rust cache status`
- `tusk-rust cache warm`
- `tusk-rust cache memcached [subcommand]`
- `tusk-rust cache distributed`

### Configuration Commands
- `tusk-rust config get <key.path> [dir]`
- `tusk-rust config check [path]`
- `tusk-rust config validate [path]`
- `tusk-rust config compile [path]`
- `tusk-rust config docs [path]`
- `tusk-rust config clear-cache [path]`
- `tusk-rust config stats`

### Binary Performance Commands
- `tusk-rust binary compile <file.tsk>`
- `tusk-rust binary execute <file.tskb>`
- `tusk-rust binary benchmark <file>`
- `tusk-rust binary optimize <file>`

### AI Commands
- `tusk-rust ai claude <prompt>`
- `tusk-rust ai chatgpt <prompt>`
- `tusk-rust ai analyze <file>`
- `tusk-rust ai optimize <file>`
- `tusk-rust ai security <file>`

### Utility Commands
- `tusk-rust utility parse <file>`
- `tusk-rust utility validate <file>`
- `tusk-rust utility convert <input> <output>`
- `tusk-rust utility get <file> <key.path>`
- `tusk-rust utility set <file> <key.path> <val>`

## Usage Examples

```sh
# Database
$ tusk-rust db status
$ tusk-rust db migrate schema.sql

# Development
$ tusk-rust dev serve 3000
$ tusk-rust dev compile config.tsk

# Testing
$ tusk-rust test all
$ tusk-rust test performance

# Configuration
$ tusk-rust config get server.port
$ tusk-rust config compile ./

# Binary
$ tusk-rust binary compile app.tsk
$ tusk-rust binary benchmark app.tsk

# AI
$ tusk-rust ai claude "How do I optimize this config?"

# Utility
$ tusk-rust utility parse config.tsk
$ tusk-rust utility get config.tsk database.host
```

---

All commands support `--help`, `--json`, `--quiet`, and `--verbose` flags.