# TuskLang SDK Completion Prompts

## Status Summary

Based on the inspection, here's what each language needs to complete:

### ✅ Fully Complete
- **C#** - All done!
- **Go** - All done!

### ⚠️ Needs Work
- **Ruby** - Critical: Still using .pntb extension
- **Java** - CLI docs have placeholder content
- **Python** - Missing 3 CLI commands
- **Rust** - Missing CLI docs and 3 commands
- **PHP** - Missing 1 CLI command
- **JavaScript** - Missing 3 CLI commands
- **Bash** - Missing 3 CLI commands

---

## Ruby - CRITICAL FIX NEEDED

```
CRITICAL: Your PeanutConfig implementation still uses .pntb extension instead of .pnt. Please immediately:

1. Fix the extension in /opt/tsk_git/sdk-pnt-test/ruby/lib/peanut_config.rb
   - Change ALL occurrences of .pntb to .pnt
   - This includes lines: 37, 80, 223, 494, 558, 581, 584

2. Complete your CLI documentation in /opt/tsk_git/sdk-pnt-test/cli-docs/ruby/
   - You only have ai, config, and db documented
   - Need: binary, cache, css, dev, license, peanuts, services, test, utility

3. Verify all CLI commands are implemented in the main CLI file
```

---

## Java - Documentation Fix

```
Your CLI documentation in /opt/tsk_git/sdk-pnt-test/cli-docs/java/ has many placeholder "memcached" directories. Please:

1. Replace all placeholder content with actual documentation
2. Ensure each command file has proper content following the template
3. Review /opt/tsk_git/sdk-pnt-test/CLI_DOCS_STRUCTURE_PROMPT.md for the correct format
```

---

## Python - Missing Commands

```
Please implement the following missing CLI commands:

1. css commands (css expand, css map)
2. license commands (license check, license activate)  
3. peanuts commands (peanuts compile, peanuts auto-compile, peanuts load)

These should be added to your CLI implementation and documented in /opt/tsk_git/sdk-pnt-test/cli-docs/python/
```

---

## Rust - Documentation and Commands

```
You need to:

1. Complete CLI documentation in /opt/tsk_git/sdk-pnt-test/cli-docs/rust/
   - Currently only config and utility are documented
   - Need ALL command categories as per CLI_DOCS_STRUCTURE_PROMPT.md

2. Implement missing commands:
   - css commands (css expand, css map)
   - license commands (license check, license activate)
   - peanuts commands (peanuts compile, peanuts auto-compile, peanuts load)
```

---

## PHP - Missing License Command

```
Please implement the license commands:
- license check
- license activate

Add these to your CLI implementation and create documentation at:
/opt/tsk_git/sdk-pnt-test/cli-docs/php/commands/license/
```

---

## JavaScript - Missing Commands

```
Please implement the following missing CLI commands:

1. css commands (css expand, css map)
2. license commands (license check, license activate)
3. peanuts commands (peanuts compile, peanuts auto-compile, peanuts load)

These should be added to your CLI implementation and documented in /opt/tsk_git/sdk-pnt-test/cli-docs/js/
```

---

## Bash - Missing Commands

```
Please implement the following missing CLI commands:

1. css commands (css expand, css map)
2. license commands (license check, license activate)
3. peanuts commands (peanuts compile, peanuts auto-compile, peanuts load)

Note: For Bash, simplified implementations are acceptable. Focus on core functionality.
```

---

## Universal Check

All languages should verify:

1. ✅ Using .pnt extension (not .pntb)
2. ✅ CLI documentation complete in cli-docs/[language]/
3. ✅ PNT_GUIDE.md exists in [language]/docs/
4. ✅ All CLI commands implemented
5. ✅ Tests are passing

## Priority Order

1. **URGENT**: Ruby - Fix .pntb → .pnt
2. **HIGH**: Missing commands (css, license, peanuts)
3. **MEDIUM**: Complete CLI documentation
4. **LOW**: Polish and testing