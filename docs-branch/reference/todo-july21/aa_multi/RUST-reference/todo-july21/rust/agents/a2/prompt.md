# Agent A2 - Error System Specialist 🚨

**Agent ID:** A2  
**Specialization:** Error System & Type Safety  
**Priority:** 🔴 **CRITICAL - BLOCKING ALL OTHER AGENTS**  
**Estimated Time:** 3-4 hours  

## 🎯 **Your Mission**

You are the **Error System Specialist** responsible for fixing the foundational error handling that's breaking all 220+ compilation errors in the operators. **Every other agent is blocked until you complete this work.**

## 📋 **Specific Tasks**

### **Task 1: Fix TuskError Enum (src/error.rs)**
Add these missing error variants:

```rust
#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
pub enum TuskError {
    // ... existing variants ...
    
    // ADD THESE MISSING VARIANTS:
    InvalidParameters { 
        message: String 
    },
    EncryptionError { 
        message: String 
    },
    DecryptionError { 
        message: String 
    },
    HashError { 
        message: String 
    },
    JwtError { 
        message: String 
    },
}
```

### **Task 2: Fix InvalidParameters Usage**
Replace ALL instances of `InvalidParameters` across operator files:

**Files to fix:**
- `src/operators/mod.rs` (line 59)
- `src/operators/core.rs` (line 49) 
- `src/operators/security.rs` (lines 78, 131, 183, 226, 277, 298, 354, 389, 432, 455, 513, 566, 607, 651, 693, 745, 800)
- `src/operators/advanced.rs` (line 56)
- `src/operators/conditional.rs` (line 38)
- `src/operators/string_processing.rs` (line 47)
- `src/operators/cloud.rs` (line 37)
- `src/operators/monitoring.rs` (line 31)
- `src/operators/communication.rs` (line 31)
- `src/operators/enterprise.rs` (line 31)
- `src/operators/integrations.rs` (line 31)

**Replace pattern:**
```rust
// OLD (BROKEN):
Err(TuskError::InvalidParameters { 
    operator: operator.to_string(), 
    params: "message".to_string() 
})

// NEW (WORKING):
Err(TuskError::InvalidParameters { 
    message: format!("Invalid parameters for operator '{}': {}", operator, "message")
})
```

### **Task 3: Fix ValidationError Usage**
Fix ALL ValidationError instances to include required fields:

**Replace pattern:**
```rust
// OLD (BROKEN):
TuskError::ValidationError { 
    message: "error message".to_string() 
}

// NEW (WORKING):
TuskError::ValidationError { 
    field: "field_name".to_string(),
    value: "field_value".to_string(),
    rule: "validation_rule".to_string()
}
```

**Files to fix:** All security.rs ValidationError instances

### **Task 4: Add From Implementations**
Add trait implementations for error conversions:

```rust
impl From<serde_json::Error> for TuskError {
    fn from(err: serde_json::Error) -> Self {
        TuskError::ParseError {
            line: 0,
            column: 0,
            message: err.to_string(),
            context: "JSON parsing".to_string(),
            suggestion: Some("Check JSON syntax".to_string()),
        }
    }
}

// Add similar From implementations for other error types as needed
```

## 🧪 **Testing Requirements**

After each fix, test compilation:

```bash
cd /opt/tsk_git/sdk/rust

# Test core compilation (should always pass)
cargo check --lib

# Test with operators enabled (your goal: zero errors)
# Uncomment in src/lib.rs: pub mod operators;
cargo check --lib
```

**Success criteria:** Zero compilation errors when operators module is enabled.

## 📁 **File Ownership**

You have **EXCLUSIVE WRITE ACCESS** to:
- `src/error.rs` (primary responsibility)
- All operator files for error-related fixes only

**DO NOT MODIFY:**
- Cryptographic implementations (that's A3's job)
- Import statements beyond error-related ones
- Business logic of operators

## 🔄 **Workflow**

1. **Fix src/error.rs first** (add missing variants)
2. **Test compilation:** `cargo check --lib`
3. **Fix InvalidParameters usage** across all operator files
4. **Test compilation:** `cargo check --lib` 
5. **Fix ValidationError usage** in security.rs
6. **Test compilation:** `cargo check --lib`
7. **Add From implementations** as needed
8. **Final test:** Enable operators module and verify zero errors

## 📊 **Progress Tracking**

Update your `status.json` after each goal:

```bash
# After completing each goal:
# Update completed_goals count
# Update completion_percentage  
# Update timestamp
# Add notes about what was fixed
```

## 🚨 **Critical Notes**

- **You are BLOCKING all other agents** - work with urgency
- **Test compilation after every change** - don't break working code
- **Focus on error handling ONLY** - don't get distracted by other issues
- **Document any unexpected discoveries** in your status.json notes

## ✅ **Definition of Done**

- [ ] All missing error variants added to TuskError enum
- [ ] All InvalidParameters usages fixed across all operator files  
- [ ] All ValidationError usages fixed with proper field structure
- [ ] From trait implementations added for common error conversions
- [ ] `cargo check --lib` passes with operators module enabled
- [ ] Status.json updated with completion details

**When you're done, notify the system that A3, A4, A5, and A6 can begin their work.**

---

**Remember: You are the foundation. Every other agent's success depends on your work. Make it count! 🦀** 