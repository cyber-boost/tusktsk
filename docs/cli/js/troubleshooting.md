# Troubleshooting: TuskLang JavaScript CLI

## Common Issues & Solutions

### 1. Command Not Found
- **Solution:** Ensure `tsk` is in your PATH. Try `npm install -g tusklang`.

### 2. Permission Denied
- **Solution:** Use `sudo` for global installs or fix npm permissions.

### 3. Database Connection Fails
- **Solution:** Check your `peanu.peanuts` config and database server status.

### 4. Invalid Configuration File
- **Solution:** Run `tsk validate peanu.peanuts` to check for syntax errors.

### 5. Binary Compile Fails
- **Solution:** Ensure you are using `.peanuts` or `.tsk` as input and `.pnt` as output.

### 6. CLI Crashes or Unexpected Output
- **Solution:** Run with `--verbose` for more info. Check Node.js version (16+).

## Getting Help
- [PNT Guide](../js/docs/PNT_GUIDE.md)
- [Command Reference](./commands/README.md)
- [GitHub Issues](https://github.com/tusklang/tusklang/issues)
- [Discord Community](https://discord.gg/tusklang) 