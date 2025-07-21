# Parser & AST Errors (156 errors)

## 🎯 **Priority: HIGH**
**Estimated Fix Time**: 60 minutes
**Impact**: Core parsing functionality broken

---

## 📋 **Error Summary**
- **Total Errors**: 156
- **Primary Issues**: Constructor mismatches, missing properties, type conversions
- **Files Affected**: Parser, AST, and Configuration Engine files
- **Fix Complexity**: High

---

## 🔍 **Error Categories**

### 1. **Constructor Parameter Mismatches** (67 errors)
**Error Type**: `CS1729` - Constructor doesn't take expected number of arguments

#### **Files Affected:**
- `src/Parser/TuskTskParser.cs`
- `src/Parser/AstNodes.cs`

#### **Specific Errors:**
```csharp
// AtOperatorNode constructor issues
new AtOperatorNode(operatorName, arguments, location) // WRONG
new AtOperatorNode(operatorName, arguments) // CORRECT

// CrossFileOperatorNode constructor issues
new CrossFileOperatorNode(fileName, methodName, arguments, location) // WRONG
new CrossFileOperatorNode(fileName, methodName, arguments) // CORRECT

// ArrayNode constructor issues
new ArrayNode(elements, location) // WRONG
new ArrayNode(elements) // CORRECT

// ObjectNode constructor issues
new ObjectNode(properties, location) // WRONG
new ObjectNode(properties) // CORRECT

// NamedObjectNode constructor issues
new NamedObjectNode(name, properties, location) // WRONG
new NamedObjectNode(name, properties) // CORRECT
```

#### **Fix Strategy:**
Add proper constructors to `src/Parser/AstNodes.cs`:
```csharp
public class AtOperatorNode : AstNode
{
    public string OperatorName { get; set; } = string.Empty;
    public ExpressionNode[] Arguments { get; set; } = Array.Empty<ExpressionNode>();
    
    public AtOperatorNode(string operatorName, ExpressionNode[] arguments)
    {
        OperatorName = operatorName;
        Arguments = arguments;
    }
}
```

---

### 2. **Property Access Issues** (32 errors)
**Error Type**: `CS1061` - Missing properties like `Key`, `Path`, `Text`

#### **Files Affected:**
- `src/Core/Configuration/ConfigurationEngine.cs`
- `src/Parser/Semantic/SemanticAnalyzer.cs`

#### **Missing Properties:**
```csharp
// AssignmentNode missing Key property
assignment.Key // MISSING
assignment.Name // EXISTS

// IncludeNode missing Path property
include.Path // MISSING
include.FilePath // EXISTS

// CommentNode missing Text property
comment.Text // MISSING
comment.Content // EXISTS

// StringNode missing IsTemplate property
stringNode.IsTemplate // MISSING

// VariableReferenceNode missing IsGlobal property
varRef.IsGlobal // MISSING

// UnaryOperatorNode missing Expression property
unary.Expression // MISSING
unary.Operand // EXISTS
```

#### **Fix Strategy:**
Add missing properties to AST nodes:
```csharp
public class AssignmentNode : AstNode
{
    public string Key { get; set; } = string.Empty;
    public ExpressionNode Value { get; set; }
}

public class IncludeNode : AstNode
{
    public string Path { get; set; } = string.Empty;
    public bool IsImport { get; set; }
}

public class CommentNode : AstNode
{
    public string Text { get; set; } = string.Empty;
}

public class StringNode : AstNode
{
    public string Value { get; set; } = string.Empty;
    public bool IsTemplate { get; set; }
}

public class VariableReferenceNode : AstNode
{
    public string Name { get; set; } = string.Empty;
    public bool IsGlobal { get; set; }
}

public class UnaryOperatorNode : AstNode
{
    public string Operator { get; set; } = string.Empty;
    public ExpressionNode Expression { get; set; }
}
```

---

### 3. **Type Conversion Issues** (43 errors)
**Error Type**: `CS1503` - Argument type mismatches, `CS0266` - Implicit conversions

#### **Files Affected:**
- `src/Core/Configuration/ConfigurationEngine.cs`
- `src/Parser/Semantic/SemanticAnalyzer.cs`

#### **Specific Issues:**
```csharp
// AstNode to ExpressionNode conversion
EvaluateExpressionAsync(statement) // WRONG - AstNode
EvaluateExpressionAsync((ExpressionNode)statement) // CORRECT

// Isolation level conversion
IsolationLevel.ReadCommitted // System.Data.IsolationLevel
(System.Transactions.IsolationLevel)IsolationLevel.ReadCommitted // CORRECT

// Transaction type conversion
return connection.BeginTransaction(); // DbTransaction
return (SqlTransaction)connection.BeginTransaction(); // CORRECT
```

#### **Fix Strategy:**
```csharp
// Fix AST node conversions
private async Task<object> EvaluateExpressionAsync(AstNode node)
{
    if (node is ExpressionNode expressionNode)
    {
        return await EvaluateExpressionAsync(expressionNode);
    }
    throw new InvalidOperationException($"Expected ExpressionNode, got {node.GetType().Name}");
}

// Fix isolation level conversion
var isolationLevel = (System.Transactions.IsolationLevel)IsolationLevel.ReadCommitted;

// Fix transaction conversion
var transaction = (SqlTransaction)connection.BeginTransaction();
```

---

### 4. **Missing Type References** (14 errors)
**Error Type**: `CS0246` - Type not found, `CS0103` - Name does not exist

#### **Files Affected:**
- `src/Core/Configuration/ConfigurationEngine.cs`
- `src/Parser/Semantic/SemanticAnalyzer.cs`

#### **Missing Types:**
```csharp
// Missing TSK type
TSK.ParseAsync(content) // MISSING
TSK.CompileAsync(content) // MISSING

// Missing ValidationResult type
ValidationResult result; // AMBIGUOUS
TuskLang.Core.ValidationResult result; // CORRECT
```

#### **Fix Strategy:**
```csharp
// Add TSK class
public static class TSK
{
    public static async Task<object> ParseAsync(string content)
    {
        // Placeholder implementation
        return null;
    }
    
    public static async Task<object> CompileAsync(string content)
    {
        // Placeholder implementation
        return null;
    }
}

// Use fully qualified names
TuskLang.Core.ValidationResult result;
```

---

## 🛠️ **Systematic Fix Plan**

### **Step 1: Fix AST Node Constructors** (25 minutes)
1. Update `src/Parser/TuskTskParser.cs` to use correct constructor calls
2. Add missing constructors to `src/Parser/AstNodes.cs`
3. Fix parameter types and counts

### **Step 2: Add Missing Properties** (20 minutes)
1. Add missing properties to all AST node classes
2. Update property access in ConfigurationEngine
3. Fix property references in SemanticAnalyzer

### **Step 3: Fix Type Conversions** (10 minutes)
1. Add proper type casting for AST nodes
2. Fix isolation level conversions
3. Update transaction type conversions

### **Step 4: Add Missing Types** (5 minutes)
1. Add TSK static class
2. Use fully qualified type names
3. Add missing using statements

---

## 📝 **Detailed Fix Examples**

### **Example 1: Fix AtOperatorNode Constructor**
```csharp
// In src/Parser/TuskTskParser.cs
// BEFORE:
return new AtOperatorNode(operatorName, arguments, location);

// AFTER:
return new AtOperatorNode(operatorName, arguments);

// In src/Parser/AstNodes.cs
public class AtOperatorNode : AstNode
{
    public string OperatorName { get; set; } = string.Empty;
    public ExpressionNode[] Arguments { get; set; } = Array.Empty<ExpressionNode>();
    
    public AtOperatorNode(string operatorName, ExpressionNode[] arguments)
    {
        OperatorName = operatorName;
        Arguments = arguments;
    }
}
```

### **Example 2: Fix Property Access**
```csharp
// In src/Core/Configuration/ConfigurationEngine.cs
// BEFORE:
var key = assignment.Key;
var path = include.Path;

// AFTER:
var key = assignment.Name; // or add Key property
var path = include.FilePath; // or add Path property
```

---

## ✅ **Verification Checklist**

- [ ] All AST node constructors work correctly
- [ ] All AST nodes have required properties
- [ ] Type conversions work without errors
- [ ] Missing types are defined or imported
- [ ] Parser can create AST nodes successfully
- [ ] ConfigurationEngine can process AST nodes
- [ ] No CS1729, CS1061, CS1503, or CS0266 errors in parser files

---

## 🎯 **Expected Outcome**

After fixing all 156 parser and AST errors:
- **AST Creation**: ✅ All nodes can be created
- **Property Access**: ✅ All properties accessible
- **Type Safety**: ✅ All conversions work
- **Parser Functionality**: ✅ Full parsing capability
- **Configuration Processing**: ✅ AST processing works

---

## 📊 **Progress Tracking**

- **Total Parser/AST Errors**: 156
- **Fixed**: 0
- **Remaining**: 156
- **Status**: 🔴 Not Started 