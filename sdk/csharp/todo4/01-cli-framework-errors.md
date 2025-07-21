# CLI Framework Errors (127 errors)

## 🎯 **Priority: CRITICAL**
**Estimated Fix Time**: 45 minutes
**Impact**: CLI functionality completely broken

---

## 📋 **Error Summary**
- **Total Errors**: 127
- **Primary Issue**: System.CommandLine API usage problems
- **Files Affected**: All CLI command files
- **Fix Complexity**: Medium

---

## 🔍 **Error Categories**

### 1. **Argument Constructor Issues** (45 errors)
**Error Type**: `CS1503` - Argument conversion from `string[]` to `string`

#### **Files Affected:**
- `src/CLI/Program.cs`
- `src/CLI/Commands/CommandBase.cs`
- `src/CLI/Commands/ParseCommand.cs`
- `src/CLI/Commands/CompileCommand.cs`
- `src/CLI/Commands/ValidateCommand.cs`
- `src/CLI/Commands/InitCommand.cs`
- `src/CLI/Commands/BuildCommand.cs`
- `src/CLI/Commands/TestCommand.cs`
- `src/CLI/Commands/ServeCommand.cs`
- `src/CLI/Commands/ConfigCommand.cs`
- `src/CLI/Commands/DatabaseCommand.cs`
- `src/CLI/Commands/CacheCommand.cs`
- `src/CLI/Commands/LicenseCommand.cs`
- `src/CLI/Commands/PeanutsCommand.cs`
- `src/CLI/Commands/CssCommand.cs`
- `src/CLI/Commands/ProjectCommand.cs`
- `src/CLI/Commands/AiCommand.cs`
- `src/CLI/Commands/UtilityCommand.cs`

#### **Fix Pattern:**
```csharp
// BEFORE: new Argument<string[]>("names")
// AFTER: new Argument<string>("name")

// BEFORE: new Argument<FileInfo>("file")
// AFTER: new Argument<FileInfo>("file")
```

---

### 2. **Missing Command Methods** (38 errors)
**Error Type**: `CS1061` - Missing `AddOption`, `AddCommand`, `Handler` methods

#### **Files Affected:**
- All command files in `src/CLI/Commands/`
- `src/CLI/Program.cs`

#### **Fix Pattern:**
```csharp
// Add using statement
using System.CommandLine;
using System.CommandLine.Invocation;

// Fix command creation
// BEFORE: command.Handler = CommandHandler.Create<...>
// AFTER: command.SetHandler(async (context) => { ... });
```

---

### 3. **Static Method Call Issues** (24 errors)
**Error Type**: `CS0120` - Static method calls on non-static methods

#### **Files Affected:**
- `src/CLI/Program.cs`

#### **Fix Pattern:**
```csharp
// BEFORE: ParseCommand.Create()
// AFTER: new ParseCommand().Create()

// BEFORE: CompileCommand.Create()
// AFTER: new CompileCommand().Create()
```

---

### 4. **Missing CommandHandler Type** (20 errors)
**Error Type**: `CS0103` - Missing `CommandHandler` type

#### **Files Affected:**
- All command files in `src/CLI/Commands/`
- `src/CLI/Program.cs`

#### **Fix Pattern:**
```csharp
// Add using statement
using System.CommandLine.Invocation;

// Replace CommandHandler usage with SetHandler
// BEFORE: command.Handler = CommandHandler.Create<...>
// AFTER: command.SetHandler(async (context) => { ... });
```

---

## 🛠️ **Systematic Fix Plan**

### **Step 1: Update Using Statements** (5 minutes)
Add to all CLI files:
```csharp
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
```

### **Step 2: Fix Argument Constructors** (15 minutes)
Update all `Argument<T>` constructors to use single string parameters instead of arrays.

### **Step 3: Fix Command Creation** (15 minutes)
Replace `CommandHandler.Create` with `SetHandler` pattern.

### **Step 4: Fix Static Method Calls** (10 minutes)
Update all command creation calls to use instance methods.

---

## 📝 **Detailed Fix Examples**

### **Example 1: ParseCommand.cs**
```csharp
// BEFORE:
var fileArgument = new Argument<FileInfo>("file");
command.Handler = CommandHandler.Create<FileInfo>(HandleParse);

// AFTER:
var fileArgument = new Argument<FileInfo>("file");
command.SetHandler(async (context) => {
    var file = context.ParseResult.GetValueForArgument(fileArgument);
    await HandleParse(file);
});
```

### **Example 2: Program.cs**
```csharp
// BEFORE:
rootCommand.AddCommand(ParseCommand.Create());

// AFTER:
rootCommand.AddCommand(new ParseCommand().Create());
```

---

## ✅ **Verification Checklist**

- [ ] All CLI files have proper using statements
- [ ] All Argument constructors use single parameters
- [ ] All commands use SetHandler instead of CommandHandler
- [ ] All command creation uses instance methods
- [ ] `dotnet run -- --help` works
- [ ] `dotnet run -- parse --help` works
- [ ] No CS1503, CS1061, CS0120, or CS0103 errors in CLI files

---

## 🎯 **Expected Outcome**

After fixing all 127 CLI framework errors:
- **CLI Functionality**: ✅ Fully working
- **Command Help**: ✅ All commands show help
- **Argument Parsing**: ✅ All arguments work correctly
- **Error Handling**: ✅ Proper error messages
- **Build Status**: ✅ CLI compiles successfully

---

## 📊 **Progress Tracking**

- **Total CLI Errors**: 127
- **Fixed**: 0
- **Remaining**: 127
- **Status**: 🔴 Not Started 