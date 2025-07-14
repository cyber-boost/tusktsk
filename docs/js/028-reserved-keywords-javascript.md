# 🟨 Reserved Keywords in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Avoid reserved keywords in TuskLang configs for JavaScript. Using reserved words can cause parsing errors or unexpected behavior.

## 🚫 Common Reserved Keywords

- `if`, `else`, `for`, `while`, `function`, `return`, `true`, `false`, `null`, `yes`, `no`, `import`, `export`, `class`, `const`, `let`, `var`, `new`, `delete`, `try`, `catch`, `finally`, `switch`, `case`, `default`, `break`, `continue`, `do`, `in`, `of`, `with`, `await`, `async`, `yield`, `this`, `super`, `extends`, `static`, `public`, `private`, `protected`, `package`, `interface`, `implements`, `enum`, `type`, `namespace`, `require`, `module`, `global`, `window`, `document`, `process`, `Buffer`, `setTimeout`, `setInterval`, `clearTimeout`, `clearInterval`, `Promise`, `Map`, `Set`, `WeakMap`, `WeakSet`, `Symbol`, `Reflect`, `Proxy`, `JSON`, `Math`, `Date`, `RegExp`, `Error`, `EvalError`, `RangeError`, `ReferenceError`, `SyntaxError`, `TypeError`, `URIError`, `Number`, `String`, `Boolean`, `Object`, `Array`, `Function`

## 🧑‍💻 JavaScript Example: Avoiding Reserved Words

```ini
# Bad:
function: "myFunc" # Reserved word
class: "MyClass"   # Reserved word

# Good:
app_function: "myFunc"
user_class: "MyClass"
```

## 🚦 Best Practices
- Avoid using reserved words as keys or section names
- Use descriptive, unique names

## 🛡️ Security Note
Never use reserved words for secrets or critical logic

## 📚 Next Steps
- [Syntax Errors](029-syntax-errors-javascript.md)
- [Best Practices](030-best-practices-javascript.md)

**Ready to avoid keyword collisions in TuskLang!** 