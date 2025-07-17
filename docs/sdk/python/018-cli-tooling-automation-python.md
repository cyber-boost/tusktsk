# 🛠️ CLI Tooling & Automation - Python

**"We don't bow to any king" - CLI & Automation Edition**

TuskLang enables you to build powerful CLI tools and automation scripts for every aspect of your workflow, from project scaffolding to advanced task runners.

## 🚀 Building CLI Tools

### Basic CLI with argparse

```python
import argparse
from tsk import TSK

# CLI configuration
cli_config = TSK.from_file('cli.tsk')

def main():
    parser = argparse.ArgumentParser(description='TuskLang CLI Tool')
    parser.add_argument('--config', type=str, help='Path to TSK config file', default='cli.tsk')
    parser.add_argument('--task', type=str, help='Task to run')
    parser.add_argument('--env', type=str, help='Environment', default='dev')
    parser.add_argument('--verbose', action='store_true', help='Enable verbose output')
    args = parser.parse_args()

    # Load configuration
    config = TSK.from_file(args.config)

    # Run task
    if args.task:
        result = config.execute_fujsen('tasks', args.task, args.env)
        print(f"Task '{args.task}' result: {result}")
    else:
        print("No task specified. Use --task <task_name>.")

if __name__ == '__main__':
    main()
```

### Advanced CLI with Click

```python
import click
from tsk import TSK

@click.group()
def cli():
    """TuskLang CLI"""
    pass

@cli.command()
@click.option('--config', default='cli.tsk', help='TSK config file')
@click.option('--env', default='dev', help='Environment')
def list_tasks(config, env):
    """List available tasks"""
    tsk = TSK.from_file(config)
    tasks = tsk.get('tasks.list')
    click.echo(f"Available tasks in {env}: {tasks}")

@cli.command()
@click.option('--config', default='cli.tsk', help='TSK config file')
@click.argument('task')
@click.option('--env', default='dev', help='Environment')
def run(config, task, env):
    """Run a specific task"""
    tsk = TSK.from_file(config)
    result = tsk.execute_fujsen('tasks', task, env)
    click.echo(f"Task '{task}' result: {result}")

if __name__ == '__main__':
    cli()
```

## 🏗️ CLI Task Runners

### Defining Tasks in TSK

```ini
# cli.tsk
[tasks]
list: ["build", "test", "deploy", "clean"]

build_fujsen = '''
def build(env):
    print(f"Building project for {env}...")
    # Build logic here
    return {'status': 'success', 'env': env}
'''

test_fujsen = '''
def test(env):
    print(f"Running tests for {env}...")
    # Test logic here
    return {'status': 'success', 'env': env}
'''

deploy_fujsen = '''
def deploy(env):
    print(f"Deploying to {env}...")
    # Deploy logic here
    return {'status': 'success', 'env': env}
'''

clean_fujsen = '''
def clean(env):
    print(f"Cleaning build artifacts for {env}...")
    # Clean logic here
    return {'status': 'success', 'env': env}
'''
```

### Interactive Prompts

```python
import click
from tsk import TSK

@click.command()
@click.option('--config', default='cli.tsk', help='TSK config file')
def interactive(config):
    """Interactive CLI for TuskLang tasks"""
    tsk = TSK.from_file(config)
    tasks = tsk.get('tasks.list')
    task = click.prompt('Select a task', type=click.Choice(tasks))
    env = click.prompt('Select environment', default='dev')
    result = tsk.execute_fujsen('tasks', task, env)
    click.echo(f"Task '{task}' result: {result}")

if __name__ == '__main__':
    interactive()
```

## 🔄 Automation Scripts

### Project Scaffolding

```python
from tsk import TSK
import os

def scaffold_project(name):
    config = TSK.from_file('scaffold.tsk')
    structure = config.get('project.structure')
    for folder in structure['folders']:
        os.makedirs(os.path.join(name, folder), exist_ok=True)
    for file, content in structure['files'].items():
        with open(os.path.join(name, file), 'w') as f:
            f.write(content)
    print(f"Project '{name}' scaffolded successfully.")

# scaffold.tsk example
# [project]
# structure: {
#     folders: ["src", "tests", "docs"],
#     files: {
#         "README.md": "# Project $name",
#         "src/__init__.py": "",
#         "tests/__init__.py": ""
#     }
# }
```

### Automation with TSK CLI

```bash
# Run a TSK task from the command line
tsk run cli.tsk --task build --env prod

tsk run cli.tsk --task deploy --env staging
```

## 🧩 Integrating with CI/CD

### Example: GitHub Actions

```yaml
# .github/workflows/cli-tasks.yml
name: TuskLang CLI Tasks
on:
  push:
    branches: [ main ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Set up Python
        uses: actions/setup-python@v4
        with:
          python-version: '3.11'
      - name: Install dependencies
        run: |
          pip install tusklang click
      - name: Run build task
        run: |
          python cli.py --task build --env prod
      - name: Run tests
        run: |
          python cli.py --task test --env prod
```

## 🎯 CLI & Automation Best Practices

- Use clear, descriptive task names
- Provide helpful --help output for all commands
- Validate user input and handle errors gracefully
- Support both interactive and non-interactive modes
- Integrate with CI/CD pipelines for automation
- Document all CLI commands and options
- Use TSK for configuration and task logic

## 🚀 Next Steps

1. **Define your CLI tasks in TSK**
2. **Build Python CLI tools with argparse or Click**
3. **Add interactive prompts for usability**
4. **Automate project scaffolding and deployment**
5. **Integrate CLI tools with CI/CD pipelines**

---

**"We don't bow to any king"** - TuskLang enables you to build powerful CLI tools and automation scripts for every aspect of your workflow. Automate, orchestrate, and empower your development process! 