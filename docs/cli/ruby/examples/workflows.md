# Complete Workflows

End-to-end workflows for TuskLang Ruby SDK.

## Development Workflow

1. **Initialize Project**
   ```bash
   tsk db init
   tsk config check
   ```

2. **Development**
   ```bash
   tsk serve 3000
   tsk test all
   ```

3. **Deployment**
   ```bash
   tsk config compile
   tsk db backup
   ```

## Testing Workflow

1. **Run Tests**
   ```bash
   tsk test all
   tsk test parser
   tsk test performance
   ```

2. **Debug Issues**
   ```bash
   tsk db console
   tsk config validate
   ```
