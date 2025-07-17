# Quick Start Guide: TuskLang JavaScript CLI

Welcome to the TuskLang JavaScript CLI! This guide will help you get started quickly.

## 1. Installation

```bash
npm install -g tusklang
```

## 2. Check CLI Version

```bash
tsk --version
```

## 3. Create Your First Configuration

```bash
echo '[app]' > peanu.peanuts
echo 'name: "My App"' >> peanu.peanuts
echo 'version: "1.0.0"' >> peanu.peanuts
```

## 4. Parse and Validate

```bash
tsk parse peanu.peanuts
tsk validate peanu.peanuts
```

## 5. Compile to Binary

```bash
tsk config compile peanu.peanuts
```

## 6. Start Development Server

```bash
tsk serve 3000
```

## 7. Run Tests

```bash
tsk test all
```

## 8. Learn More

- [Full PNT Guide](../js/docs/PNT_GUIDE.md)
- [Command Reference](./commands/README.md)
- [Troubleshooting](./troubleshooting.md) 