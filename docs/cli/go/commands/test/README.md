# Testing Commands

Testing and validation tools for TuskLang Go CLI, providing comprehensive test execution and validation capabilities.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk test all](./all.md) | Run all test suites |
| [tsk test parser](./parser.md) | Test parser only |
| [tsk test fujsen](./fujsen.md) | Test FUJSEN only |
| [tsk test sdk](./sdk.md) | Test SDK only |
| [tsk test performance](./performance.md) | Performance tests |

## Common Use Cases

- **Unit Testing**: Test individual components and functions
- **Integration Testing**: Test component interactions
- **Performance Testing**: Benchmark and performance validation
- **Parser Testing**: Validate TuskLang syntax parsing
- **SDK Testing**: Test SDK functionality and APIs

## Go-Specific Notes

The Go CLI testing commands leverage Go's excellent testing framework:

- **Fast Execution**: Quick test runs with parallel execution
- **Coverage Reports**: Built-in code coverage analysis
- **Benchmarking**: Performance benchmarking capabilities
- **Test Suites**: Organized test suite management
- **Continuous Integration**: CI/CD friendly test output

## Examples

### Complete Testing

```bash
# Run all tests
tsk test all

# Run with coverage
tsk test all --coverage

# Run in parallel
tsk test all --parallel
```

### Component Testing

```bash
# Test parser functionality
tsk test parser

# Test FUJSEN system
tsk test fujsen

# Test SDK features
tsk test sdk
```

### Performance Testing

```bash
# Run performance benchmarks
tsk test performance

# Extended performance testing
tsk test performance --duration 120
``` 