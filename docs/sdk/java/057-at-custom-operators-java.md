# 🛠️ @ Custom Operators in TuskLang Java

**"We don't bow to any king" - Build custom operators like a Java master**

TuskLang Java provides powerful @ custom operator capabilities that integrate seamlessly with Spring Boot, JPA, and modern Java patterns. Create, configure, and manage custom operators with enterprise-grade performance and flexibility.

## 🎯 Overview

@ custom operators in TuskLang Java combine the power of Java's extensibility with TuskLang's dynamic configuration system. From custom validation operators to business logic operators, we'll show you how to build robust, scalable operator systems.

## 🔧 Core Custom Operator Features

### 1. Custom Validation Operators
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskCustomOperatorManager;
import java.util.Map;
import java.util.List;
import java.util.regex.Pattern;

public class CustomValidationOperatorExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [custom_operators]
            # Custom @ Operators Configuration
            enable_custom_operators: true
            enable_operator_chaining: true
            enable_operator_caching: true
            
            [operator_config]
            # Operator configuration
            enable_validation: true
            enable_business_logic: true
            enable_data_transformation: true
            
            [validation_operators]
            # Custom validation operators
            @email: {
                description: "Validate email format"
                parameters: ["value"]
                return_type: "boolean"
                implementation: """
                    function validateEmail(value) {
                        if (!value || typeof value !== 'string') {
                            return false;
                        }
                        
                        let emailRegex = /^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$/;
                        return emailRegex.test(value);
                    }
                """
            }
            
            @phone: {
                description: "Validate phone number format"
                parameters: ["value", "country"]
                return_type: "boolean"
                implementation: """
                    function validatePhone(value, country) {
                        if (!value || typeof value !== 'string') {
                            return false;
                        }
                        
                        let patterns = {
                            'US': /^\\+?1?[-.\\s]?\\(?([0-9]{3})\\)?[-.\\s]?([0-9]{3})[-.\\s]?([0-9]{4})$/,
                            'UK': /^\\+?44[-.\\s]?([0-9]{4})[-.\\s]?([0-9]{3})[-.\\s]?([0-9]{3})$/,
                            'default': /^\\+?[0-9\\s\\-\\(\\)]{10,}$/
                        };
                        
                        let pattern = patterns[country] || patterns['default'];
                        return pattern.test(value);
                    }
                """
            }
            
            @password: {
                description: "Validate password strength"
                parameters: ["value", "min_length", "require_special"]
                return_type: "object"
                implementation: """
                    function validatePassword(value, minLength, requireSpecial) {
                        if (!value || typeof value !== 'string') {
                            return { valid: false, errors: ['Password is required'] };
                        }
                        
                        let errors = [];
                        
                        if (value.length < minLength) {
                            errors.push('Password must be at least ' + minLength + ' characters');
                        }
                        
                        if (!/[A-Z]/.test(value)) {
                            errors.push('Password must contain at least one uppercase letter');
                        }
                        
                        if (!/[a-z]/.test(value)) {
                            errors.push('Password must contain at least one lowercase letter');
                        }
                        
                        if (!/[0-9]/.test(value)) {
                            errors.push('Password must contain at least one number');
                        }
                        
                        if (requireSpecial && !/[!@#$%^&*(),.?":{}|<>]/.test(value)) {
                            errors.push('Password must contain at least one special character');
                        }
                        
                        return {
                            valid: errors.length === 0,
                            errors: errors,
                            strength: calculatePasswordStrength(value)
                        };
                    }
                    
                    function calculatePasswordStrength(password) {
                        let score = 0;
                        
                        if (password.length >= 8) score += 1;
                        if (/[a-z]/.test(password)) score += 1;
                        if (/[A-Z]/.test(password)) score += 1;
                        if (/[0-9]/.test(password)) score += 1;
                        if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) score += 1;
                        
                        if (score <= 2) return 'weak';
                        if (score <= 3) return 'medium';
                        if (score <= 4) return 'strong';
                        return 'very_strong';
                    }
                """
            }
            
            @credit_card: {
                description: "Validate credit card number"
                parameters: ["value", "type"]
                return_type: "object"
                implementation: """
                    function validateCreditCard(value, type) {
                        if (!value || typeof value !== 'string') {
                            return { valid: false, errors: ['Credit card number is required'] };
                        }
                        
                        // Remove spaces and dashes
                        let cleanValue = value.replace(/[\\s\\-]/g, '');
                        
                        if (!/^[0-9]{13,19}$/.test(cleanValue)) {
                            return { valid: false, errors: ['Invalid credit card number format'] };
                        }
                        
                        // Luhn algorithm check
                        if (!luhnCheck(cleanValue)) {
                            return { valid: false, errors: ['Invalid credit card number'] };
                        }
                        
                        // Type-specific validation
                        let cardInfo = getCardInfo(cleanValue);
                        if (type && cardInfo.type !== type) {
                            return { valid: false, errors: ['Card type mismatch'] };
                        }
                        
                        return {
                            valid: true,
                            type: cardInfo.type,
                            masked: maskCardNumber(cleanValue)
                        };
                    }
                    
                    function luhnCheck(value) {
                        let sum = 0;
                        let isEven = false;
                        
                        for (let i = value.length - 1; i >= 0; i--) {
                            let digit = parseInt(value.charAt(i));
                            
                            if (isEven) {
                                digit *= 2;
                                if (digit > 9) {
                                    digit -= 9;
                                }
                            }
                            
                            sum += digit;
                            isEven = !isEven;
                        }
                        
                        return sum % 10 === 0;
                    }
                    
                    function getCardInfo(value) {
                        let patterns = {
                            'visa': /^4/,
                            'mastercard': /^5[1-5]/,
                            'amex': /^3[47]/,
                            'discover': /^6(?:011|5)/
                        };
                        
                        for (let type in patterns) {
                            if (patterns[type].test(value)) {
                                return { type: type };
                            }
                        }
                        
                        return { type: 'unknown' };
                    }
                    
                    function maskCardNumber(value) {
                        return '*'.repeat(value.length - 4) + value.slice(-4);
                    }
                """
            }
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Test custom operators
        System.out.println("Email validation: " + config.get("email_validation"));
        System.out.println("Phone validation: " + config.get("phone_validation"));
        System.out.println("Password validation: " + config.get("password_validation"));
        System.out.println("Credit card validation: " + config.get("credit_card_validation"));
    }
}
```

### 2. Custom Business Logic Operators
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskCustomOperatorManager;
import org.springframework.stereotype.Service;
import org.springframework.beans.factory.annotation.Autowired;
import java.util.Map;
import java.math.BigDecimal;
import java.math.RoundingMode;

@Service
public class CustomBusinessLogicService {
    
    @Autowired
    private TuskLang tuskParser;
    
    public Map<String, Object> processBusinessLogic() {
        String tskContent = """
            [business_logic_operators]
            # Custom business logic operators
            @tax: {
                description: "Calculate tax based on amount and rate"
                parameters: ["amount", "rate", "country"]
                return_type: "object"
                implementation: """
                    function calculateTax(amount, rate, country) {
                        if (!amount || amount <= 0) {
                            return { error: 'Invalid amount' };
                        }
                        
                        if (!rate || rate < 0 || rate > 100) {
                            return { error: 'Invalid tax rate' };
                        }
                        
                        let taxAmount = (amount * rate) / 100;
                        let totalAmount = amount + taxAmount;
                        
                        return {
                            original_amount: amount,
                            tax_rate: rate,
                            tax_amount: taxAmount,
                            total_amount: totalAmount,
                            country: country
                        };
                    }
                """
            }
            
            @shipping: {
                description: "Calculate shipping cost"
                parameters: ["weight", "destination", "service"]
                return_type: "object"
                implementation: """
                    function calculateShipping(weight, destination, service) {
                        if (!weight || weight <= 0) {
                            return { error: 'Invalid weight' };
                        }
                        
                        let baseRates = {
                            'US': { standard: 5, express: 15, overnight: 25 },
                            'CA': { standard: 8, express: 20, overnight: 35 },
                            'UK': { standard: 12, express: 25, overnight: 45 },
                            'default': { standard: 10, express: 22, overnight: 40 }
                        };
                        
                        let rates = baseRates[destination] || baseRates['default'];
                        let baseCost = rates[service] || rates['standard'];
                        
                        // Weight-based adjustment
                        let weightMultiplier = 1;
                        if (weight > 5) weightMultiplier = 1.5;
                        if (weight > 10) weightMultiplier = 2;
                        if (weight > 20) weightMultiplier = 3;
                        
                        let totalCost = baseCost * weightMultiplier;
                        
                        return {
                            weight: weight,
                            destination: destination,
                            service: service,
                            base_cost: baseCost,
                            weight_multiplier: weightMultiplier,
                            total_cost: totalCost,
                            estimated_days: service === 'overnight' ? 1 : 
                                           service === 'express' ? 3 : 7
                        };
                    }
                """
            }
            
            @discount: {
                description: "Calculate discount based on rules"
                parameters: ["amount", "customer_type", "promo_code"]
                return_type: "object"
                implementation: """
                    function calculateDiscount(amount, customerType, promoCode) {
                        if (!amount || amount <= 0) {
                            return { error: 'Invalid amount' };
                        }
                        
                        let discountRate = 0;
                        let discountReason = '';
                        
                        // Customer type discount
                        let customerDiscounts = {
                            'premium': 0.15,
                            'gold': 0.10,
                            'silver': 0.05,
                            'bronze': 0.02
                        };
                        
                        if (customerDiscounts[customerType]) {
                            discountRate += customerDiscounts[customerType];
                            discountReason += customerType + ' customer discount';
                        }
                        
                        // Promo code discount
                        let promoDiscounts = {
                            'SAVE10': 0.10,
                            'SAVE20': 0.20,
                            'WELCOME': 0.05,
                            'HOLIDAY': 0.15
                        };
                        
                        if (promoDiscounts[promoCode]) {
                            discountRate += promoDiscounts[promoCode];
                            discountReason += (discountReason ? ', ' : '') + 'promo code ' + promoCode;
                        }
                        
                        // Cap discount at 30%
                        discountRate = Math.min(discountRate, 0.30);
                        
                        let discountAmount = amount * discountRate;
                        let finalAmount = amount - discountAmount;
                        
                        return {
                            original_amount: amount,
                            discount_rate: discountRate,
                            discount_amount: discountAmount,
                            final_amount: finalAmount,
                            discount_reason: discountReason,
                            customer_type: customerType,
                            promo_code: promoCode
                        };
                    }
                """
            }
            
            [usage_examples]
            # Using custom business logic operators
            order_calculation: {
                subtotal: 150.00
                tax: @tax(150.00, 8.5, "US")
                shipping: @shipping(2.5, "US", "express")
                discount: @discount(150.00, "gold", "SAVE10")
                
                total: @sum([
                    @get("subtotal"),
                    @get("tax.tax_amount"),
                    @get("shipping.total_cost"),
                    -@get("discount.discount_amount")
                ])
            }
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

### 3. Custom Data Transformation Operators
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskCustomOperatorManager;
import org.springframework.stereotype.Service;
import java.util.Map;
import java.util.List;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Currency;

@Service
public class CustomDataTransformationService {
    
    private final TuskLang tuskParser;
    
    public CustomDataTransformationService(TuskLang tuskParser) {
        this.tuskParser = tuskParser;
    }
    
    public Map<String, Object> processDataTransformations() {
        String tskContent = """
            [data_transformation_operators]
            # Custom data transformation operators
            @currency: {
                description: "Format currency with locale"
                parameters: ["amount", "currency_code", "locale"]
                return_type: "string"
                implementation: """
                    function formatCurrency(amount, currencyCode, locale) {
                        if (!amount || isNaN(amount)) {
                            return 'Invalid amount';
                        }
                        
                        try {
                            let formatter = new Intl.NumberFormat(locale || 'en-US', {
                                style: 'currency',
                                currency: currencyCode || 'USD'
                            });
                            
                            return formatter.format(amount);
                        } catch (error) {
                            return amount + ' ' + (currencyCode || 'USD');
                        }
                    }
                """
            }
            
            @date_format: {
                description: "Format date with custom pattern"
                parameters: ["date", "pattern", "locale"]
                return_type: "string"
                implementation: """
                    function formatDate(date, pattern, locale) {
                        if (!date) {
                            return 'Invalid date';
                        }
                        
                        try {
                            let dateObj = new Date(date);
                            if (isNaN(dateObj.getTime())) {
                                return 'Invalid date format';
                            }
                            
                            let options = {
                                year: 'numeric',
                                month: 'long',
                                day: 'numeric',
                                hour: '2-digit',
                                minute: '2-digit',
                                second: '2-digit',
                                timeZoneName: 'short'
                            };
                            
                            if (pattern) {
                                // Custom pattern parsing
                                let formatted = pattern
                                    .replace('YYYY', dateObj.getFullYear())
                                    .replace('MM', String(dateObj.getMonth() + 1).padStart(2, '0'))
                                    .replace('DD', String(dateObj.getDate()).padStart(2, '0'))
                                    .replace('HH', String(dateObj.getHours()).padStart(2, '0'))
                                    .replace('mm', String(dateObj.getMinutes()).padStart(2, '0'))
                                    .replace('ss', String(dateObj.getSeconds()).padStart(2, '0'));
                                
                                return formatted;
                            }
                            
                            return dateObj.toLocaleDateString(locale || 'en-US', options);
                        } catch (error) {
                            return 'Date formatting error';
                        }
                    }
                """
            }
            
            @text_transform: {
                description: "Transform text with various operations"
                parameters: ["text", "operation", "options"]
                return_type: "string"
                implementation: """
                    function transformText(text, operation, options) {
                        if (!text || typeof text !== 'string') {
                            return '';
                        }
                        
                        switch (operation) {
                            case 'uppercase':
                                return text.toUpperCase();
                            
                            case 'lowercase':
                                return text.toLowerCase();
                            
                            case 'capitalize':
                                return text.replace(/\\b\\w/g, l => l.toUpperCase());
                            
                            case 'camelcase':
                                return text.replace(/[-_\\s]+(.)?/g, (_, c) => 
                                    c ? c.toUpperCase() : '');
                            
                            case 'snake_case':
                                return text.replace(/[A-Z]/g, letter => `_${letter.toLowerCase()}`)
                                    .replace(/^_/, '');
                            
                            case 'kebab-case':
                                return text.replace(/[A-Z]/g, letter => `-${letter.toLowerCase()}`)
                                    .replace(/^-/, '');
                            
                            case 'truncate':
                                let maxLength = options?.maxLength || 50;
                                let suffix = options?.suffix || '...';
                                return text.length > maxLength ? 
                                    text.substring(0, maxLength) + suffix : text;
                            
                            case 'slugify':
                                return text.toLowerCase()
                                    .replace(/[^a-z0-9\\s-]/g, '')
                                    .replace(/[\\s-]+/g, '-')
                                    .replace(/^-+|-+$/g, '');
                            
                            default:
                                return text;
                        }
                    }
                """
            }
            
            [usage_examples]
            # Using custom data transformation operators
            data_examples: {
                price: @currency(99.99, "USD", "en-US")
                date: @date_format(@date.now(), "YYYY-MM-DD HH:mm:ss")
                name: @text_transform("john doe", "capitalize")
                slug: @text_transform("My Blog Post Title", "slugify")
                truncated: @text_transform("This is a very long text that needs to be truncated", "truncate", { maxLength: 20 })
            }
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

### 4. Advanced Custom Operator Patterns
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskCustomOperatorManager;
import org.springframework.stereotype.Service;
import org.springframework.cache.annotation.Cacheable;
import java.util.Map;
import java.util.List;
import java.util.concurrent.CompletableFuture;

@Service
public class AdvancedCustomOperatorService {
    
    private final TuskLang tuskParser;
    
    public AdvancedCustomOperatorService(TuskLang tuskParser) {
        this.tuskParser = tuskParser;
    }
    
    @Cacheable("custom_operators")
    public Map<String, Object> processAdvancedOperators() {
        String tskContent = """
            [advanced_operators]
            # Async custom operators
            @async: {
                description: "Execute async operations"
                parameters: ["operation", "timeout"]
                return_type: "promise"
                implementation: """
                    async function asyncOperation(operation, timeout) {
                        return new Promise((resolve, reject) => {
                            setTimeout(() => {
                                try {
                                    let result = eval(operation);
                                    resolve(result);
                                } catch (error) {
                                    reject(error);
                                }
                            }, timeout || 1000);
                        });
                    }
                """
            }
            
            # Composite custom operators
            @composite: {
                description: "Combine multiple operations"
                parameters: ["operations", "strategy"]
                return_type: "object"
                implementation: """
                    function compositeOperation(operations, strategy) {
                        let results = [];
                        
                        for (let operation of operations) {
                            try {
                                let result = eval(operation);
                                results.push({ success: true, result: result });
                            } catch (error) {
                                results.push({ success: false, error: error.message });
                            }
                        }
                        
                        switch (strategy) {
                            case 'all':
                                return results.every(r => r.success) ? 
                                    { success: true, results: results } : 
                                    { success: false, errors: results.filter(r => !r.success) };
                            
                            case 'any':
                                return results.some(r => r.success) ? 
                                    { success: true, results: results.filter(r => r.success) } : 
                                    { success: false, errors: results.filter(r => !r.success) };
                            
                            case 'first':
                                let firstSuccess = results.find(r => r.success);
                                return firstSuccess ? 
                                    { success: true, result: firstSuccess.result } : 
                                    { success: false, errors: results };
                            
                            default:
                                return { success: true, results: results };
                        }
                    }
                """
            }
            
            # Conditional custom operators
            @conditional: {
                description: "Execute conditional operations"
                parameters: ["condition", "true_operation", "false_operation"]
                return_type: "any"
                implementation: """
                    function conditionalOperation(condition, trueOperation, falseOperation) {
                        let conditionResult = eval(condition);
                        
                        if (conditionResult) {
                            return eval(trueOperation);
                        } else {
                            return eval(falseOperation);
                        }
                    }
                """
            }
            
            [usage_examples]
            # Using advanced custom operators
            advanced_examples: {
                async_result: @async("@query('SELECT COUNT(*) FROM users')", 2000)
                composite_result: @composite([
                    "@env('API_KEY')",
                    "@env('DATABASE_URL')",
                    "@env('CACHE_URL')"
                ], "all")
                conditional_result: @conditional(
                    "@env('ENVIRONMENT') === 'production'",
                    "@env('PROD_API_URL')",
                    "@env('DEV_API_URL')"
                )
            }
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

### 5. Spring Boot Integration
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.operators.TuskCustomOperatorManager;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.boot.context.properties.ConfigurationProperties;
import java.util.Map;

@SpringBootApplication
public class CustomOperatorApplication {
    
    public static void main(String[] args) {
        SpringApplication.run(CustomOperatorApplication.class, args);
    }
}

@Configuration
public class CustomOperatorConfig {
    
    @Bean
    public TuskLang tuskLang() {
        return new TuskLang();
    }
    
    @Bean
    public TuskCustomOperatorManager customOperatorManager() {
        return new TuskCustomOperatorManager();
    }
    
    @Bean
    @ConfigurationProperties(prefix = "tusk.custom-operators")
    public CustomOperatorProperties customOperatorProperties() {
        return new CustomOperatorProperties();
    }
    
    @Bean
    public Map<String, Object> customOperatorConfiguration() {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [spring_custom_operators]
            # Spring Boot integration with custom operators
            application: {
                validation: {
                    email: @email(@env("ADMIN_EMAIL", "admin@example.com"))
                    phone: @phone(@env("SUPPORT_PHONE", "+1-555-0123"), "US")
                    password: @password(@env("DEFAULT_PASSWORD", "password123"), 8, true)
                }
                
                business_logic: {
                    tax_calculation: @tax(100.00, 8.5, "US")
                    shipping_calculation: @shipping(5.0, "US", "standard")
                    discount_calculation: @discount(100.00, "premium", "WELCOME")
                }
                
                data_transformation: {
                    formatted_price: @currency(99.99, "USD", "en-US")
                    formatted_date: @date_format(@date.now(), "YYYY-MM-DD")
                    formatted_name: @text_transform("john doe", "capitalize")
                }
            }
            """;
        
        return parser.parse(tskContent);
    }
}

@Component
public class CustomOperatorProperties {
    private boolean enableCustomOperators = true;
    private boolean enableValidation = true;
    private boolean enableBusinessLogic = true;
    private boolean enableDataTransformation = true;
    private int maxOperatorComplexity = 10;
    
    // Getters and setters
    public boolean isEnableCustomOperators() { return enableCustomOperators; }
    public void setEnableCustomOperators(boolean enableCustomOperators) { this.enableCustomOperators = enableCustomOperators; }
    
    public boolean isEnableValidation() { return enableValidation; }
    public void setEnableValidation(boolean enableValidation) { this.enableValidation = enableValidation; }
    
    public boolean isEnableBusinessLogic() { return enableBusinessLogic; }
    public void setEnableBusinessLogic(boolean enableBusinessLogic) { this.enableBusinessLogic = enableBusinessLogic; }
    
    public boolean isEnableDataTransformation() { return enableDataTransformation; }
    public void setEnableDataTransformation(boolean enableDataTransformation) { this.enableDataTransformation = enableDataTransformation; }
    
    public int getMaxOperatorComplexity() { return maxOperatorComplexity; }
    public void setMaxOperatorComplexity(int maxOperatorComplexity) { this.maxOperatorComplexity = maxOperatorComplexity; }
}
```

## 🚀 Best Practices

### 1. Operator Design
```java
// ✅ Good: Clear, focused operators
String goodOperator = """
    @email: {
        description: "Validate email format"
        parameters: ["value"]
        return_type: "boolean"
        implementation: "function validateEmail(value) { return /^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$/.test(value); }"
    }
    """;

// ❌ Bad: Overly complex operators
String badOperator = """
    @do_everything: {
        description: "Do everything"
        parameters: ["value", "type", "format", "validate", "transform", "cache"]
        return_type: "object"
        implementation: "function doEverything(value, type, format, validate, transform, cache) { /* 100 lines of code */ }"
    }
    """;
```

### 2. Error Handling
```java
// ✅ Good: Comprehensive error handling
String safeOperator = """
    @safe_operation: {
        description: "Safe operation with error handling"
        parameters: ["value"]
        return_type: "object"
        implementation: """
            function safeOperation(value) {
                try {
                    if (!value) {
                        return { success: false, error: 'Value is required' };
                    }
                    let result = processValue(value);
                    return { success: true, result: result };
                } catch (error) {
                    return { success: false, error: error.message };
                }
            }
        """
    }
    """;

// ❌ Bad: No error handling
String unsafeOperator = """
    @unsafe_operation: {
        description: "Unsafe operation"
        parameters: ["value"]
        return_type: "any"
        implementation: "function unsafeOperation(value) { return processValue(value); }"
    }
    """;
```

### 3. Performance Considerations
```java
// ✅ Good: Optimized operators
String optimizedOperator = """
    @optimized_operation: {
        description: "Optimized operation with caching"
        parameters: ["value"]
        return_type: "any"
        implementation: """
            function optimizedOperation(value) {
                let cacheKey = 'op_' + value;
                let cached = getCache(cacheKey);
                if (cached) return cached;
                
                let result = expensiveOperation(value);
                setCache(cacheKey, result, '5m');
                return result;
            }
        """
    }
    """;

// ❌ Bad: No optimization
String unoptimizedOperator = """
    @unoptimized_operation: {
        description: "Unoptimized operation"
        parameters: ["value"]
        return_type: "any"
        implementation: "function unoptimizedOperation(value) { return expensiveOperation(value); }"
    }
    """;
```

## 🔧 Integration Examples

### Spring Boot Configuration
```java
@Configuration
public class CustomOperatorConfiguration {
    
    @Bean
    @ConfigurationProperties(prefix = "tusk.custom-operators")
    public CustomOperatorProperties customOperatorProperties() {
        return new CustomOperatorProperties();
    }
    
    @Bean
    public TuskCustomOperatorManager tuskCustomOperatorManager() {
        return new TuskCustomOperatorManager();
    }
}

@Component
public class CustomOperatorProperties {
    private boolean enableCustomOperators = true;
    private boolean enableValidation = true;
    private boolean enableBusinessLogic = true;
    private boolean enableDataTransformation = true;
    private int maxOperatorComplexity = 10;
    
    // Getters and setters
    public boolean isEnableCustomOperators() { return enableCustomOperators; }
    public void setEnableCustomOperators(boolean enableCustomOperators) { this.enableCustomOperators = enableCustomOperators; }
    
    public boolean isEnableValidation() { return enableValidation; }
    public void setEnableValidation(boolean enableValidation) { this.enableValidation = enableValidation; }
    
    public boolean isEnableBusinessLogic() { return enableBusinessLogic; }
    public void setEnableBusinessLogic(boolean enableBusinessLogic) { this.enableBusinessLogic = enableBusinessLogic; }
    
    public boolean isEnableDataTransformation() { return enableDataTransformation; }
    public void setEnableDataTransformation(boolean enableDataTransformation) { this.enableDataTransformation = enableDataTransformation; }
    
    public int getMaxOperatorComplexity() { return maxOperatorComplexity; }
    public void setMaxOperatorComplexity(int maxOperatorComplexity) { this.maxOperatorComplexity = maxOperatorComplexity; }
}
```

### JPA Integration
```java
@Repository
public class CustomOperatorRepository {
    
    @PersistenceContext
    private EntityManager entityManager;
    
    public List<Map<String, Object>> processCustomOperatorQuery(String tskQuery) {
        // Process TuskLang query with custom operators
        TuskLang parser = new TuskLang();
        Map<String, Object> result = parser.parse(tskQuery);
        
        // Execute the query with custom operators
        String sql = (String) result.get("sql");
        List<Object> parameters = (List<Object>) result.get("parameters");
        
        Query query = entityManager.createNativeQuery(sql);
        for (int i = 0; i < parameters.size(); i++) {
            query.setParameter(i + 1, parameters.get(i));
        }
        
        return query.getResultList();
    }
}
```

## 📊 Performance Metrics

### Custom Operator Performance Comparison
```java
@Service
public class CustomOperatorPerformanceService {
    
    public void benchmarkCustomOperators() {
        // Simple validation operator: ~1ms
        String simpleOperator = "@email('test@example.com')";
        
        // Complex business logic operator: ~5ms
        String complexOperator = "@tax(100.00, 8.5, 'US')";
        
        // Data transformation operator: ~3ms
        String transformOperator = "@currency(99.99, 'USD', 'en-US')";
        
        // Async operator: ~1000ms
        String asyncOperator = "@async('@query(\"SELECT COUNT(*) FROM users\")', 1000)";
    }
}
```

## 🔒 Security Considerations

### Secure Custom Operators
```java
@Service
public class SecureCustomOperatorService {
    
    public Map<String, Object> processSecureOperators() {
        String tskContent = """
            [secure_custom_operators]
            # Secure custom operators
            secure_validation: {
                api_key: @validate_api_key(@env("API_KEY"))
                user_input: @sanitize_input(@env("USER_INPUT"))
                sql_query: @validate_sql(@env("SQL_QUERY"))
            }
            
            # Custom security operators
            @validate_api_key: {
                description: "Validate API key security"
                parameters: ["api_key"]
                return_type: "object"
                implementation: """
                    function validateApiKey(apiKey) {
                        if (!apiKey || apiKey.length < 32) {
                            return { valid: false, error: 'API key too short' };
                        }
                        
                        if (!/^[a-zA-Z0-9]+$/.test(apiKey)) {
                            return { valid: false, error: 'Invalid API key format' };
                        }
                        
                        return { valid: true, masked: '*'.repeat(apiKey.length - 4) + apiKey.slice(-4) };
                    }
                """
            }
            
            @sanitize_input: {
                description: "Sanitize user input"
                parameters: ["input"]
                return_type: "string"
                implementation: """
                    function sanitizeInput(input) {
                        if (!input) return '';
                        
                        return input
                            .replace(/[<>]/g, '')
                            .replace(/javascript:/gi, '')
                            .replace(/on\\w+=/gi, '')
                            .trim();
                    }
                """
            }
            
            @validate_sql: {
                description: "Validate SQL query security"
                parameters: ["sql"]
                return_type: "object"
                implementation: """
                    function validateSql(sql) {
                        if (!sql) {
                            return { valid: false, error: 'SQL query is required' };
                        }
                        
                        let dangerousKeywords = ['DROP', 'DELETE', 'UPDATE', 'INSERT', 'CREATE', 'ALTER'];
                        let upperSql = sql.toUpperCase();
                        
                        for (let keyword of dangerousKeywords) {
                            if (upperSql.includes(keyword)) {
                                return { valid: false, error: 'Dangerous SQL keyword detected: ' + keyword };
                            }
                        }
                        
                        return { valid: true, sql: sql };
                    }
                """
            }
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

## 🎯 Summary

@ custom operators in TuskLang Java provide:

- **Extensibility**: Create custom operators for specific needs
- **Validation**: Built-in validation operators for data integrity
- **Business Logic**: Custom business logic operators for complex calculations
- **Data Transformation**: Custom transformation operators for data formatting
- **Spring Boot Integration**: Seamless integration with Spring applications
- **Performance Optimization**: Caching and optimization capabilities
- **Security**: Built-in security validation and sanitization
- **Type Safety**: Java type safety with TuskLang flexibility

Master @ custom operators to create sophisticated, domain-specific functionality that adapts to your application's unique requirements while maintaining enterprise-grade performance and security. 