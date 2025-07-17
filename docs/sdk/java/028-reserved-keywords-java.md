# Reserved Keywords in TuskLang - Java Edition

**"We don't bow to any king" - Keyword Mastery with Java Integration**

TuskLang has a set of reserved keywords that have special meaning in the language. Understanding these keywords is crucial for writing valid TuskLang configurations and avoiding conflicts with Java integration.

## 🎯 Java Keyword Integration

### @TuskConfig Keyword Handling

```java
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.annotations.TuskKeyword;
import org.tusklang.java.annotations.TuskReserved;

@TuskConfig
public class KeywordAwareConfig {
    
    // Using @TuskKeyword to handle reserved keywords
    @TuskKeyword("import")
    private String importPath;
    
    @TuskKeyword("export")
    private String exportPath;
    
    @TuskKeyword("function")
    private String functionName;
    
    @TuskKeyword("return")
    private String returnValue;
    
    @TuskKeyword("if")
    private boolean conditionalFlag;
    
    @TuskKeyword("else")
    private String alternativeValue;
    
    @TuskKeyword("for")
    private List<String> loopItems;
    
    @TuskKeyword("while")
    private boolean loopCondition;
    
    @TuskKeyword("break")
    private boolean breakFlag;
    
    @TuskKeyword("continue")
    private boolean continueFlag;
    
    // Reserved keyword validation
    public boolean validateKeywords() {
        // Check for keyword conflicts
        if (hasKeywordConflict()) {
            throw new IllegalStateException("Keyword conflict detected");
        }
        
        return true;
    }
    
    private boolean hasKeywordConflict() {
        // Implement keyword conflict detection
        return false;
    }
    
    // Getters and setters
    public String getImportPath() { return importPath; }
    public void setImportPath(String importPath) { this.importPath = importPath; }
    
    public String getExportPath() { return exportPath; }
    public void setExportPath(String exportPath) { this.exportPath = exportPath; }
    
    public String getFunctionName() { return functionName; }
    public void setFunctionName(String functionName) { this.functionName = functionName; }
    
    public String getReturnValue() { return returnValue; }
    public void setReturnValue(String returnValue) { this.returnValue = returnValue; }
    
    public boolean isConditionalFlag() { return conditionalFlag; }
    public void setConditionalFlag(boolean conditionalFlag) { this.conditionalFlag = conditionalFlag; }
    
    public String getAlternativeValue() { return alternativeValue; }
    public void setAlternativeValue(String alternativeValue) { this.alternativeValue = alternativeValue; }
    
    public List<String> getLoopItems() { return loopItems; }
    public void setLoopItems(List<String> loopItems) { this.loopItems = loopItems; }
    
    public boolean isLoopCondition() { return loopCondition; }
    public void setLoopCondition(boolean loopCondition) { this.loopCondition = loopCondition; }
    
    public boolean isBreakFlag() { return breakFlag; }
    public void setBreakFlag(boolean breakFlag) { this.breakFlag = breakFlag; }
    
    public boolean isContinueFlag() { return continueFlag; }
    public void setContinueFlag(boolean continueFlag) { this.continueFlag = continueFlag; }
}
```

### TuskLang Reserved Keywords

```tusk
# Reserved keywords in TuskLang
# These keywords have special meaning and should be used carefully

# Control flow keywords
if: boolean = true
else: string = "alternative"
for: array = [1, 2, 3, 4, 5]
while: boolean = false
break: boolean = false
continue: boolean = false

# Function keywords
function: string = "myFunction"
return: string = "result"
lambda: function = @lambda(x, x * 2)

# Import/Export keywords
import: string = "module.tsk"
export: string = "data.json"

# Type keywords
string: string = "text"
number: number = 42
boolean: boolean = true
object: object = { key: "value" }
array: array = [1, 2, 3]
null: null = null

# Operator keywords
and: boolean = true
or: boolean = false
not: boolean = true
in: boolean = true
is: boolean = true
as: string = "type"

# Special keywords
self: object = @self
parent: object = @parent
root: object = @root
```

## 🚫 Reserved Keywords List

### Control Flow Keywords

```java
@TuskConfig
public class ControlFlowKeywords {
    
    // Control flow keywords that are reserved
    private static final List<String> CONTROL_FLOW_KEYWORDS = Arrays.asList(
        "if", "else", "for", "while", "break", "continue", "switch", "case", "default"
    );
    
    // Using keywords with @TuskKeyword annotation
    @TuskKeyword("if")
    private boolean ifCondition;
    
    @TuskKeyword("else")
    private String elseValue;
    
    @TuskKeyword("for")
    private List<String> forItems;
    
    @TuskKeyword("while")
    private boolean whileCondition;
    
    @TuskKeyword("break")
    private boolean breakFlag;
    
    @TuskKeyword("continue")
    private boolean continueFlag;
    
    // Validate control flow keywords
    public boolean validateControlFlow() {
        for (String keyword : CONTROL_FLOW_KEYWORDS) {
            if (isKeywordUsed(keyword)) {
                validateKeywordUsage(keyword);
            }
        }
        return true;
    }
    
    private boolean isKeywordUsed(String keyword) {
        // Check if keyword is used in configuration
        return false;
    }
    
    private void validateKeywordUsage(String keyword) {
        // Validate proper usage of keyword
    }
    
    // Getters and setters
    public boolean isIfCondition() { return ifCondition; }
    public void setIfCondition(boolean ifCondition) { this.ifCondition = ifCondition; }
    
    public String getElseValue() { return elseValue; }
    public void setElseValue(String elseValue) { this.elseValue = elseValue; }
    
    public List<String> getForItems() { return forItems; }
    public void setForItems(List<String> forItems) { this.forItems = forItems; }
    
    public boolean isWhileCondition() { return whileCondition; }
    public void setWhileCondition(boolean whileCondition) { this.whileCondition = whileCondition; }
    
    public boolean isBreakFlag() { return breakFlag; }
    public void setBreakFlag(boolean breakFlag) { this.breakFlag = breakFlag; }
    
    public boolean isContinueFlag() { return continueFlag; }
    public void setContinueFlag(boolean continueFlag) { this.continueFlag = continueFlag; }
}
```

### Function Keywords

```java
@TuskConfig
public class FunctionKeywords {
    
    private static final List<String> FUNCTION_KEYWORDS = Arrays.asList(
        "function", "return", "lambda", "def", "call", "apply"
    );
    
    @TuskKeyword("function")
    private String functionName;
    
    @TuskKeyword("return")
    private Object returnValue;
    
    @TuskKeyword("lambda")
    private String lambdaExpression;
    
    @TuskKeyword("def")
    private String definition;
    
    @TuskKeyword("call")
    private String callTarget;
    
    @TuskKeyword("apply")
    private String applyFunction;
    
    // Function keyword validation
    public boolean validateFunctionKeywords() {
        // Validate function keyword usage
        if (functionName != null && !isValidFunctionName(functionName)) {
            throw new IllegalArgumentException("Invalid function name: " + functionName);
        }
        
        return true;
    }
    
    private boolean isValidFunctionName(String name) {
        // Validate function name format
        return name != null && name.matches("^[a-zA-Z_][a-zA-Z0-9_]*$");
    }
    
    // Getters and setters
    public String getFunctionName() { return functionName; }
    public void setFunctionName(String functionName) { this.functionName = functionName; }
    
    public Object getReturnValue() { return returnValue; }
    public void setReturnValue(Object returnValue) { this.returnValue = returnValue; }
    
    public String getLambdaExpression() { return lambdaExpression; }
    public void setLambdaExpression(String lambdaExpression) { this.lambdaExpression = lambdaExpression; }
    
    public String getDefinition() { return definition; }
    public void setDefinition(String definition) { this.definition = definition; }
    
    public String getCallTarget() { return callTarget; }
    public void setCallTarget(String callTarget) { this.callTarget = callTarget; }
    
    public String getApplyFunction() { return applyFunction; }
    public void setApplyFunction(String applyFunction) { this.applyFunction = applyFunction; }
}
```

### Type Keywords

```java
@TuskConfig
public class TypeKeywords {
    
    private static final List<String> TYPE_KEYWORDS = Arrays.asList(
        "string", "number", "boolean", "object", "array", "null", "any", "void"
    );
    
    @TuskKeyword("string")
    private String stringType;
    
    @TuskKeyword("number")
    private Number numberType;
    
    @TuskKeyword("boolean")
    private Boolean booleanType;
    
    @TuskKeyword("object")
    private Object objectType;
    
    @TuskKeyword("array")
    private List<Object> arrayType;
    
    @TuskKeyword("null")
    private Object nullValue;
    
    @TuskKeyword("any")
    private Object anyType;
    
    @TuskKeyword("void")
    private Object voidType;
    
    // Type keyword validation
    public boolean validateTypeKeywords() {
        // Validate type keyword usage
        for (String keyword : TYPE_KEYWORDS) {
            validateTypeUsage(keyword);
        }
        return true;
    }
    
    private void validateTypeUsage(String keyword) {
        // Validate proper type keyword usage
    }
    
    // Getters and setters
    public String getStringType() { return stringType; }
    public void setStringType(String stringType) { this.stringType = stringType; }
    
    public Number getNumberType() { return numberType; }
    public void setNumberType(Number numberType) { this.numberType = numberType; }
    
    public Boolean getBooleanType() { return booleanType; }
    public void setBooleanType(Boolean booleanType) { this.booleanType = booleanType; }
    
    public Object getObjectType() { return objectType; }
    public void setObjectType(Object objectType) { this.objectType = objectType; }
    
    public List<Object> getArrayType() { return arrayType; }
    public void setArrayType(List<Object> arrayType) { this.arrayType = arrayType; }
    
    public Object getNullValue() { return nullValue; }
    public void setNullValue(Object nullValue) { this.nullValue = nullValue; }
    
    public Object getAnyType() { return anyType; }
    public void setAnyType(Object anyType) { this.anyType = anyType; }
    
    public Object getVoidType() { return voidType; }
    public void setVoidType(Object voidType) { this.voidType = voidType; }
}
```

## 🔧 Keyword Escaping and Workarounds

### Java Keyword Escaping

```java
@TuskConfig
public class KeywordEscapingConfig {
    
    // Using underscores to escape keywords
    private String _import;
    private String _export;
    private String _function;
    private String _return;
    private String _if;
    private String _else;
    private String _for;
    private String _while;
    
    // Using prefixes to avoid conflicts
    private String configImport;
    private String configExport;
    private String configFunction;
    private String configReturn;
    private String configIf;
    private String configElse;
    private String configFor;
    private String configWhile;
    
    // Using @TuskKeyword annotation for proper handling
    @TuskKeyword("import")
    private String importPath;
    
    @TuskKeyword("export")
    private String exportPath;
    
    @TuskKeyword("function")
    private String functionName;
    
    @TuskKeyword("return")
    private String returnValue;
    
    // Keyword escaping methods
    public String escapeKeyword(String keyword) {
        return "_" + keyword;
    }
    
    public String unescapeKeyword(String escapedKeyword) {
        if (escapedKeyword.startsWith("_")) {
            return escapedKeyword.substring(1);
        }
        return escapedKeyword;
    }
    
    // Getters and setters
    public String get_import() { return _import; }
    public void set_import(String _import) { this._import = _import; }
    
    public String get_export() { return _export; }
    public void set_export(String _export) { this._export = _export; }
    
    public String get_function() { return _function; }
    public void set_function(String _function) { this._function = _function; }
    
    public String get_return() { return _return; }
    public void set_return(String _return) { this._return = _return; }
    
    public String get_if() { return _if; }
    public void set_if(String _if) { this._if = _if; }
    
    public String get_else() { return _else; }
    public void set_else(String _else) { this._else = _else; }
    
    public String get_for() { return _for; }
    public void set_for(String _for) { this._for = _for; }
    
    public String get_while() { return _while; }
    public void set_while(String _while) { this._while = _while; }
    
    public String getConfigImport() { return configImport; }
    public void setConfigImport(String configImport) { this.configImport = configImport; }
    
    public String getConfigExport() { return configExport; }
    public void setConfigExport(String configExport) { this.configExport = configExport; }
    
    public String getConfigFunction() { return configFunction; }
    public void setConfigFunction(String configFunction) { this.configFunction = configFunction; }
    
    public String getConfigReturn() { return configReturn; }
    public void setConfigReturn(String configReturn) { this.configReturn = configReturn; }
    
    public String getConfigIf() { return configIf; }
    public void setConfigIf(String configIf) { this.configIf = configIf; }
    
    public String getConfigElse() { return configElse; }
    public void setConfigElse(String configElse) { this.configElse = configElse; }
    
    public String getConfigFor() { return configFor; }
    public void setConfigFor(String configFor) { this.configFor = configFor; }
    
    public String getConfigWhile() { return configWhile; }
    public void setConfigWhile(String configWhile) { this.configWhile = configWhile; }
    
    public String getImportPath() { return importPath; }
    public void setImportPath(String importPath) { this.importPath = importPath; }
    
    public String getExportPath() { return exportPath; }
    public void setExportPath(String exportPath) { this.exportPath = exportPath; }
    
    public String getFunctionName() { return functionName; }
    public void setFunctionName(String functionName) { this.functionName = functionName; }
    
    public String getReturnValue() { return returnValue; }
    public void setReturnValue(String returnValue) { this.returnValue = returnValue; }
}
```

### TuskLang Keyword Escaping

```tusk
# Escaping keywords in TuskLang configuration

# Using underscores to escape keywords
_import: string = "module.tsk"
_export: string = "data.json"
_function: string = "myFunction"
_return: string = "result"
_if: boolean = true
_else: string = "alternative"
_for: array = [1, 2, 3]
_while: boolean = false

# Using prefixes to avoid conflicts
config_import: string = "config/module.tsk"
config_export: string = "config/data.json"
config_function: string = "config/myFunction"
config_return: string = "config/result"
config_if: boolean = true
config_else: string = "config/alternative"
config_for: array = [1, 2, 3]
config_while: boolean = false

# Using quotes for keyword-like values
"import": string = "module.tsk"
"export": string = "data.json"
"function": string = "myFunction"
"return": string = "result"
"if": boolean = true
"else": string = "alternative"
"for": array = [1, 2, 3]
"while": boolean = false
```

## 🛡️ Keyword Validation

### Java Keyword Validation

```java
@TuskConfig
public class KeywordValidationConfig {
    
    private static final Set<String> RESERVED_KEYWORDS = Set.of(
        // Control flow
        "if", "else", "for", "while", "break", "continue", "switch", "case", "default",
        // Functions
        "function", "return", "lambda", "def", "call", "apply",
        // Types
        "string", "number", "boolean", "object", "array", "null", "any", "void",
        // Operators
        "and", "or", "not", "in", "is", "as",
        // Special
        "self", "parent", "root", "import", "export"
    );
    
    // Validate configuration keys
    public boolean validateConfigurationKeys(Map<String, Object> config) {
        for (String key : config.keySet()) {
            if (RESERVED_KEYWORDS.contains(key)) {
                throw new IllegalArgumentException("Reserved keyword used as key: " + key);
            }
        }
        return true;
    }
    
    // Check if identifier is a keyword
    public boolean isReservedKeyword(String identifier) {
        return RESERVED_KEYWORDS.contains(identifier);
    }
    
    // Suggest alternative names
    public String suggestAlternativeName(String keyword) {
        return "_" + keyword;
    }
    
    // Validate and escape keywords
    public String validateAndEscape(String identifier) {
        if (isReservedKeyword(identifier)) {
            return suggestAlternativeName(identifier);
        }
        return identifier;
    }
}
```

### TuskLang Keyword Validation

```tusk
# Keyword validation in TuskLang configuration

# Valid configuration (no keyword conflicts)
app_name: string = "My Application"
database_host: string = "localhost"
server_port: number = 8080
debug_mode: boolean = true

# Invalid configuration (keyword conflicts)
# if: boolean = true          # This would cause an error
# function: string = "test"   # This would cause an error
# return: string = "value"    # This would cause an error

# Escaped configuration (using underscores)
_if: boolean = true
_function: string = "test"
_return: string = "value"

# Alternative naming (using prefixes)
config_if: boolean = true
config_function: string = "test"
config_return: string = "value"
```

## 🎯 Best Practices

### Keyword Management Guidelines

1. **Avoid using reserved keywords** as variable names
2. **Use descriptive prefixes** for configuration keys
3. **Escape keywords with underscores** when necessary
4. **Validate configuration keys** before processing
5. **Document keyword usage** in your codebase

### Performance Considerations

```java
// Efficient keyword checking with HashSet
@TuskConfig
public class EfficientKeywordConfig {
    
    private static final Set<String> KEYWORDS = new HashSet<>(Arrays.asList(
        "if", "else", "for", "while", "function", "return", "import", "export"
    ));
    
    public boolean isKeyword(String identifier) {
        return KEYWORDS.contains(identifier);
    }
    
    public String escapeIfNeeded(String identifier) {
        return isKeyword(identifier) ? "_" + identifier : identifier;
    }
}
```

## 🚨 Troubleshooting

### Common Keyword Issues

1. **Keyword conflicts**: Use escaping or alternative names
2. **Validation errors**: Check for reserved keyword usage
3. **Parser errors**: Ensure proper keyword handling
4. **Naming confusion**: Use clear, descriptive names

### Debug Keyword Issues

```java
// Debug keyword conflicts
public void debugKeywordIssues(Map<String, Object> config) {
    for (String key : config.keySet()) {
        if (isReservedKeyword(key)) {
            System.out.println("Keyword conflict found: " + key);
            System.out.println("Suggested alternative: " + suggestAlternativeName(key));
        }
    }
}
```

## 🎯 Next Steps

1. **Review keyword list** for your specific use case
2. **Implement keyword validation** in your configuration
3. **Use proper escaping** for keyword conflicts
4. **Document keyword usage** in your project
5. **Test keyword handling** thoroughly

---

**Ready to master TuskLang keywords with Java power? Understanding reserved keywords is crucial for writing clean, conflict-free configurations. We don't bow to any king - especially not keyword constraints!** 