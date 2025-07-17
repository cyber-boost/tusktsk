# 🔧 Custom Hash Directives in TuskLang Java

**"We don't bow to any king" - Build custom directives like a Java master**

TuskLang Java provides sophisticated custom hash directive capabilities that integrate seamlessly with Spring Boot, JPA, and modern Java patterns. Create, configure, and manage custom directives with enterprise-grade performance and flexibility.

## 🎯 Overview

Custom hash directives in TuskLang Java combine the power of Java directive technologies with TuskLang's configuration system. From custom validation directives to business logic directives, we'll show you how to build robust, scalable directive systems.

## 🔧 Core Custom Directive Features

### 1. Custom Validation Directives
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.TuskCustomDirectiveManager;
import java.util.Map;
import java.util.List;
import java.util.regex.Pattern;

public class CustomValidationDirectiveExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [custom_directives]
            # Custom Hash Directives Configuration
            enable_custom_directives: true
            enable_directive_chaining: true
            enable_directive_caching: true
            
            [directive_config]
            # Directive configuration
            enable_validation: true
            enable_business_logic: true
            enable_data_transformation: true
            
            [validation_directives]
            # Custom validation directives
            #email: {
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
            
            #phone: {
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
            
            #password: {
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
            
            #credit_card: {
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
                        return value.replace(/(\\d{4})(\\d{4})(\\d{4})(\\d{4})/, '$1-****-****-$4');
                    }
                """
            }
            
            [business_logic_directives]
            # Custom business logic directives
            #calculate_tax: {
                description: "Calculate tax based on amount and location"
                parameters: ["amount", "state", "country"]
                return_type: "object"
                implementation: """
                    function calculateTax(amount, state, country) {
                        let taxRates = {
                            'US': {
                                'CA': 0.085,
                                'NY': 0.08,
                                'TX': 0.0625,
                                'default': 0.06
                            },
                            'CA': {
                                'ON': 0.13,
                                'BC': 0.12,
                                'default': 0.10
                            }
                        };
                        
                        let rate = taxRates[country]?.[state] || taxRates[country]?.['default'] || 0.05;
                        let taxAmount = amount * rate;
                        let totalAmount = amount + taxAmount;
                        
                        return {
                            subtotal: amount,
                            tax_rate: rate,
                            tax_amount: taxAmount,
                            total: totalAmount
                        };
                    }
                """
            }
            
            #calculate_shipping: {
                description: "Calculate shipping cost"
                parameters: ["weight", "destination", "service"]
                return_type: "object"
                implementation: """
                    function calculateShipping(weight, destination, service) {
                        let baseRates = {
                            'ground': 5.99,
                            'express': 12.99,
                            'overnight': 24.99
                        };
                        
                        let weightMultiplier = {
                            'ground': 0.5,
                            'express': 1.0,
                            'overnight': 1.5
                        };
                        
                        let baseRate = baseRates[service] || baseRates['ground'];
                        let multiplier = weightMultiplier[service] || weightMultiplier['ground'];
                        
                        let weightCost = weight * multiplier;
                        let totalCost = baseRate + weightCost;
                        
                        // Add destination surcharge
                        if (destination === 'international') {
                            totalCost += 15.00;
                        }
                        
                        return {
                            base_rate: baseRate,
                            weight_cost: weightCost,
                            destination_surcharge: destination === 'international' ? 15.00 : 0,
                            total_cost: totalCost,
                            estimated_days: getEstimatedDays(service, destination)
                        };
                    }
                    
                    function getEstimatedDays(service, destination) {
                        let days = {
                            'ground': destination === 'international' ? 7 : 3,
                            'express': destination === 'international' ? 3 : 1,
                            'overnight': 1
                        };
                        
                        return days[service] || days['ground'];
                    }
                """
            }
            
            [data_transformation_directives]
            # Custom data transformation directives
            #format_currency: {
                description: "Format currency value"
                parameters: ["amount", "currency", "locale"]
                return_type: "string"
                implementation: """
                    function formatCurrency(amount, currency, locale) {
                        let formatter = new Intl.NumberFormat(locale || 'en-US', {
                            style: 'currency',
                            currency: currency || 'USD'
                        });
                        
                        return formatter.format(amount);
                    }
                """
            }
            
            #format_date: {
                description: "Format date value"
                parameters: ["date", "format", "locale"]
                return_type: "string"
                implementation: """
                    function formatDate(date, format, locale) {
                        let dateObj = new Date(date);
                        
                        if (isNaN(dateObj.getTime())) {
                            return 'Invalid date';
                        }
                        
                        let options = {
                            year: 'numeric',
                            month: 'long',
                            day: 'numeric'
                        };
                        
                        if (format === 'short') {
                            options.month = 'short';
                        } else if (format === 'numeric') {
                            options.month = 'numeric';
                        }
                        
                        return dateObj.toLocaleDateString(locale || 'en-US', options);
                    }
                """
            }
            
            [directive_methods]
            # Directive execution methods
            execute_validation_directive: """
                function executeValidationDirective(directiveName, parameters) {
                    let directive = @validation_directives[directiveName];
                    
                    if (!directive) {
                        throw new Error("Unknown validation directive: " + directiveName);
                    }
                    
                    return directive.implementation.apply(null, parameters);
                }
            """
            
            execute_business_logic_directive: """
                function executeBusinessLogicDirective(directiveName, parameters) {
                    let directive = @business_logic_directives[directiveName];
                    
                    if (!directive) {
                        throw new Error("Unknown business logic directive: " + directiveName);
                    }
                    
                    return directive.implementation.apply(null, parameters);
                }
            """
            
            execute_transformation_directive: """
                function executeTransformationDirective(directiveName, parameters) {
                    let directive = @data_transformation_directives[directiveName];
                    
                    if (!directive) {
                        throw new Error("Unknown transformation directive: " + directiveName);
                    }
                    
                    return directive.implementation.apply(null, parameters);
                }
            """
            
            chain_directives: """
                function chainDirectives(directives) {
                    let result = null;
                    
                    for (let directive of directives) {
                        let params = directive.parameters || [];
                        
                        switch (directive.type) {
                            case 'validation':
                                result = executeValidationDirective(directive.name, params);
                                break;
                            case 'business_logic':
                                result = executeBusinessLogicDirective(directive.name, params);
                                break;
                            case 'transformation':
                                result = executeTransformationDirective(directive.name, params);
                                break;
                            default:
                                throw new Error("Unknown directive type: " + directive.type);
                        }
                        
                        // Check if validation failed
                        if (directive.type === 'validation' && !result.valid) {
                            return result;
                        }
                    }
                    
                    return result;
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize custom directive manager
        TuskCustomDirectiveManager directiveManager = new TuskCustomDirectiveManager();
        directiveManager.configure(config);
        
        // Execute validation directives
        boolean emailValid = directiveManager.executeValidationDirective("directive_methods", 
            "execute_validation_directive", Map.of("directiveName", "email", "parameters", List.of("john@example.com")));
        
        boolean phoneValid = directiveManager.executeValidationDirective("directive_methods", 
            "execute_validation_directive", Map.of("directiveName", "phone", "parameters", List.of("+1-555-123-4567", "US")));
        
        Map<String, Object> passwordValidation = directiveManager.executeValidationDirective("directive_methods", 
            "execute_validation_directive", Map.of("directiveName", "password", "parameters", List.of("SecurePass123!", 8, true)));
        
        Map<String, Object> creditCardValidation = directiveManager.executeValidationDirective("directive_methods", 
            "execute_validation_directive", Map.of("directiveName", "credit_card", "parameters", List.of("4111-1111-1111-1111", "visa")));
        
        // Execute business logic directives
        Map<String, Object> taxCalculation = directiveManager.executeBusinessLogicDirective("directive_methods", 
            "execute_business_logic_directive", Map.of("directiveName", "calculate_tax", "parameters", List.of(100.0, "CA", "US")));
        
        Map<String, Object> shippingCalculation = directiveManager.executeBusinessLogicDirective("directive_methods", 
            "execute_business_logic_directive", Map.of("directiveName", "calculate_shipping", "parameters", List.of(5.0, "domestic", "express")));
        
        // Execute transformation directives
        String formattedCurrency = directiveManager.executeTransformationDirective("directive_methods", 
            "execute_transformation_directive", Map.of("directiveName", "format_currency", "parameters", List.of(1234.56, "USD", "en-US")));
        
        String formattedDate = directiveManager.executeTransformationDirective("directive_methods", 
            "execute_transformation_directive", Map.of("directiveName", "format_date", "parameters", List.of("2024-01-15", "long", "en-US")));
        
        // Chain directives
        List<Map<String, Object>> directiveChain = List.of(
            Map.of("type", "validation", "name", "email", "parameters", List.of("user@example.com")),
            Map.of("type", "validation", "name", "password", "parameters", List.of("SecurePass123!", 8, true)),
            Map.of("type", "business_logic", "name", "calculate_tax", "parameters", List.of(100.0, "CA", "US")),
            Map.of("type", "transformation", "name", "format_currency", "parameters", List.of(108.5, "USD", "en-US"))
        );
        
        Object chainedResult = directiveManager.chainDirectives("directive_methods", 
            "chain_directives", directiveChain);
        
        System.out.println("Validation Results:");
        System.out.println("Email valid: " + emailValid);
        System.out.println("Phone valid: " + phoneValid);
        System.out.println("Password valid: " + passwordValidation.get("valid"));
        System.out.println("Password strength: " + passwordValidation.get("strength"));
        System.out.println("Credit card valid: " + creditCardValidation.get("valid"));
        System.out.println("Credit card type: " + creditCardValidation.get("type"));
        
        System.out.println("Business Logic Results:");
        System.out.println("Tax calculation: $" + taxCalculation.get("tax_amount") + " (Total: $" + taxCalculation.get("total") + ")");
        System.out.println("Shipping cost: $" + shippingCalculation.get("total_cost") + " (" + shippingCalculation.get("estimated_days") + " days)");
        
        System.out.println("Transformation Results:");
        System.out.println("Formatted currency: " + formattedCurrency);
        System.out.println("Formatted date: " + formattedDate);
        
        System.out.println("Chained Directives Result: " + chainedResult);
    }
}
```

### 2. Spring Boot Custom Directive Integration
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.TuskSpringDirectiveManager;
import org.springframework.stereotype.Service;
import org.springframework.context.annotation.Bean;
import java.util.Map;
import java.util.List;

@Service
public class CustomDirectiveService {
    private final TuskSpringDirectiveManager directiveManager;
    
    public CustomDirectiveService(TuskSpringDirectiveManager directiveManager) {
        this.directiveManager = directiveManager;
    }
    
    public boolean validateUserData(String email, String phone, String password) {
        // Execute validation directives
        boolean emailValid = directiveManager.executeValidationDirective("email", List.of(email));
        boolean phoneValid = directiveManager.executeValidationDirective("phone", List.of(phone, "US"));
        Map<String, Object> passwordValidation = directiveManager.executeValidationDirective("password", List.of(password, 8, true));
        
        return emailValid && phoneValid && (Boolean) passwordValidation.get("valid");
    }
    
    public Map<String, Object> calculateOrderTotal(double subtotal, String state, double weight, String service) {
        // Execute business logic directives
        Map<String, Object> taxCalculation = directiveManager.executeBusinessLogicDirective("calculate_tax", List.of(subtotal, state, "US"));
        Map<String, Object> shippingCalculation = directiveManager.executeBusinessLogicDirective("calculate_shipping", List.of(weight, "domestic", service));
        
        double total = (Double) taxCalculation.get("total") + (Double) shippingCalculation.get("total_cost");
        
        return Map.of(
            "subtotal", subtotal,
            "tax", taxCalculation.get("tax_amount"),
            "shipping", shippingCalculation.get("total_cost"),
            "total", total
        );
    }
    
    public String formatOrderSummary(Map<String, Object> orderData) {
        // Execute transformation directives
        String formattedTotal = directiveManager.executeTransformationDirective("format_currency", 
            List.of(orderData.get("total"), "USD", "en-US"));
        String formattedDate = directiveManager.executeTransformationDirective("format_date", 
            List.of(orderData.get("orderDate"), "long", "en-US"));
        
        return String.format("Order Total: %s, Date: %s", formattedTotal, formattedDate);
    }
}

public class SpringDirectiveExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [spring_custom_directives]
            # Spring Boot Custom Directives Configuration
            enable_spring_integration: true
            enable_service_integration: true
            enable_bean_integration: true
            
            [spring_directive_config]
            # Spring directive configuration
            service_class: "com.example.CustomDirectiveService"
            enable_autowiring: true
            enable_transaction_support: true
            
            [spring_directive_methods]
            # Spring directive methods
            validate_user_registration: """
                function validateUserRegistration(directiveService, userData) {
                    return directiveService.validateUserData(
                        userData.email,
                        userData.phone,
                        userData.password
                    );
                }
            """
            
            process_order: """
                function processOrder(directiveService, orderData) {
                    let orderTotal = directiveService.calculateOrderTotal(
                        orderData.subtotal,
                        orderData.state,
                        orderData.weight,
                        orderData.service
                    );
                    
                    let summary = directiveService.formatOrderSummary({
                        total: orderTotal.total,
                        orderDate: new Date()
                    });
                    
                    return {
                        order: orderTotal,
                        summary: summary
                    };
                }
            """
            
            create_directive_bean: """
                function createDirectiveBean(directiveConfig) {
                    let bean = new CustomDirectiveService();
                    
                    // Configure bean properties
                    if (directiveConfig.validation_enabled) {
                        bean.setValidationEnabled(true);
                    }
                    
                    if (directiveConfig.caching_enabled) {
                        bean.setCachingEnabled(true);
                    }
                    
                    return bean;
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize Spring directive manager
        TuskSpringDirectiveManager springDirectiveManager = new TuskSpringDirectiveManager();
        springDirectiveManager.configure(config);
        
        // Create directive service
        CustomDirectiveService directiveService = springDirectiveManager.createDirectiveBean("spring_directive_methods", 
            "create_directive_bean", Map.of("validation_enabled", true, "caching_enabled", true));
        
        // Validate user registration
        Map<String, Object> userData = Map.of(
            "email", "john@example.com",
            "phone", "+1-555-123-4567",
            "password", "SecurePass123!"
        );
        
        boolean userValid = springDirectiveManager.executeServiceMethod("spring_directive_methods", 
            "validate_user_registration", Map.of("directiveService", directiveService, "userData", userData));
        
        // Process order
        Map<String, Object> orderData = Map.of(
            "subtotal", 99.99,
            "state", "CA",
            "weight", 2.5,
            "service", "express"
        );
        
        Map<String, Object> orderResult = springDirectiveManager.executeServiceMethod("spring_directive_methods", 
            "process_order", Map.of("directiveService", directiveService, "orderData", orderData));
        
        System.out.println("Spring Custom Directives Results:");
        System.out.println("User validation: " + (userValid ? "Valid" : "Invalid"));
        System.out.println("Order processing: " + orderResult.get("summary"));
        System.out.println("Order total: $" + orderResult.get("order.total"));
    }
}
```

### 3. Advanced Custom Directive Patterns
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.TuskAdvancedDirectiveManager;
import java.util.Map;
import java.util.List;

public class AdvancedDirectiveExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [advanced_custom_directives]
            # Advanced Custom Directives Configuration
            enable_advanced_patterns: true
            enable_directive_composition: true
            enable_async_directives: true
            
            [advanced_patterns]
            # Advanced directive patterns
            decorator_pattern: {
                description: "Decorator pattern for directives"
                base_directive: "base_validation"
                decorators: ["logging", "caching", "metrics"]
            }
            
            factory_pattern: {
                description: "Factory pattern for directive creation"
                directive_types: ["validation", "business_logic", "transformation"]
                factory_method: "create_directive"
            }
            
            observer_pattern: {
                description: "Observer pattern for directive events"
                events: ["directive_executed", "directive_failed", "directive_cached"]
                observers: ["logger", "metrics_collector", "audit_trail"]
            }
            
            [advanced_directives]
            # Advanced custom directives
            #async_validation: {
                description: "Asynchronous validation directive"
                parameters: ["value", "validation_rules"]
                return_type: "Promise<object>"
                implementation: """
                    async function asyncValidation(value, validationRules) {
                        return new Promise((resolve, reject) => {
                            setTimeout(() => {
                                try {
                                    let results = [];
                                    
                                    for (let rule of validationRules) {
                                        let result = executeValidationRule(value, rule);
                                        results.push(result);
                                    }
                                    
                                    let valid = results.every(r => r.valid);
                                    
                                    resolve({
                                        valid: valid,
                                        results: results,
                                        timestamp: new Date()
                                    });
                                } catch (error) {
                                    reject(error);
                                }
                            }, 100);
                        });
                    }
                """
            }
            
            #composite_validation: {
                description: "Composite validation directive"
                parameters: ["value", "validators"]
                return_type: "object"
                implementation: """
                    function compositeValidation(value, validators) {
                        let results = [];
                        let allValid = true;
                        
                        for (let validator of validators) {
                            let result = executeValidator(value, validator);
                            results.push(result);
                            
                            if (!result.valid) {
                                allValid = false;
                            }
                        }
                        
                        return {
                            valid: allValid,
                            results: results,
                            summary: generateValidationSummary(results)
                        };
                    }
                """
            }
            
            #conditional_directive: {
                description: "Conditional directive execution"
                parameters: ["condition", "true_directive", "false_directive", "params"]
                return_type: "any"
                implementation: """
                    function conditionalDirective(condition, trueDirective, falseDirective, params) {
                        if (evaluateCondition(condition, params)) {
                            return executeDirective(trueDirective, params);
                        } else {
                            return executeDirective(falseDirective, params);
                        }
                    }
                """
            }
            
            [advanced_methods]
            # Advanced directive methods
            execute_async_directive: """
                async function executeAsyncDirective(directiveName, parameters) {
                    let directive = @advanced_directives[directiveName];
                    
                    if (!directive) {
                        throw new Error("Unknown advanced directive: " + directiveName);
                    }
                    
                    return await directive.implementation.apply(null, parameters);
                }
            """
            
            execute_composite_directive: """
                function executeCompositeDirective(directiveName, parameters) {
                    let directive = @advanced_directives[directiveName];
                    
                    if (!directive) {
                        throw new Error("Unknown advanced directive: " + directiveName);
                    }
                    
                    return directive.implementation.apply(null, parameters);
                }
            """
            
            create_directive_decorator: """
                function createDirectiveDecorator(baseDirective, decorators) {
                    let decoratedDirective = baseDirective;
                    
                    for (let decorator of decorators) {
                        decoratedDirective = applyDecorator(decoratedDirective, decorator);
                    }
                    
                    return decoratedDirective;
                }
            """
            
            execute_conditional_directive: """
                function executeConditionalDirective(condition, trueDirective, falseDirective, params) {
                    let directive = @advanced_directives.conditional_directive;
                    return directive.implementation.apply(null, [condition, trueDirective, falseDirective, params]);
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize advanced directive manager
        TuskAdvancedDirectiveManager advancedDirectiveManager = new TuskAdvancedDirectiveManager();
        advancedDirectiveManager.configure(config);
        
        // Execute async directive
        List<Map<String, Object>> validationRules = List.of(
            Map.of("type", "email", "required", true),
            Map.of("type", "length", "min", 5, "max", 50),
            Map.of("type", "format", "pattern", "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")
        );
        
        Object asyncResult = advancedDirectiveManager.executeAsyncDirective("advanced_methods", 
            "execute_async_directive", Map.of("directiveName", "async_validation", "parameters", List.of("test@example.com", validationRules)));
        
        // Execute composite directive
        List<Map<String, Object>> validators = List.of(
            Map.of("type", "email", "value", "user@example.com"),
            Map.of("type", "password", "value", "SecurePass123!", "minLength", 8),
            Map.of("type", "phone", "value", "+1-555-123-4567", "country", "US")
        );
        
        Map<String, Object> compositeResult = advancedDirectiveManager.executeCompositeDirective("advanced_methods", 
            "execute_composite_directive", Map.of("directiveName", "composite_validation", "parameters", List.of("test_value", validators)));
        
        // Execute conditional directive
        Map<String, Object> condition = Map.of("type", "user_role", "value", "admin");
        String trueDirective = "admin_validation";
        String falseDirective = "user_validation";
        Map<String, Object> params = Map.of("userId", 123, "permissions", List.of("read", "write"));
        
        Object conditionalResult = advancedDirectiveManager.executeConditionalDirective("advanced_methods", 
            "execute_conditional_directive", Map.of("condition", condition, "trueDirective", trueDirective, "falseDirective", falseDirective, "params", params));
        
        System.out.println("Advanced Custom Directives Results:");
        System.out.println("Async validation: " + asyncResult);
        System.out.println("Composite validation: " + compositeResult.get("valid"));
        System.out.println("Conditional directive: " + conditionalResult);
    }
}
```

### 4. Custom Directive Testing and Validation
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.TuskDirectiveTester;
import java.util.Map;
import java.util.List;

public class DirectiveTestingExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [directive_testing]
            # Custom Directive Testing Configuration
            enable_unit_testing: true
            enable_integration_testing: true
            enable_performance_testing: true
            
            [testing_config]
            # Testing configuration
            enable_test_cases: true
            enable_test_reports: true
            enable_coverage_analysis: true
            
            [test_cases]
            # Test cases for directives
            email_validation_tests: [
                {
                    name: "Valid email test"
                    input: "test@example.com"
                    expected: true
                    description: "Should accept valid email format"
                },
                {
                    name: "Invalid email test"
                    input: "invalid-email"
                    expected: false
                    description: "Should reject invalid email format"
                },
                {
                    name: "Empty email test"
                    input: ""
                    expected: false
                    description: "Should reject empty email"
                }
            ]
            
            password_validation_tests: [
                {
                    name: "Strong password test"
                    input: "SecurePass123!"
                    expected: { valid: true, strength: "strong" }
                    description: "Should accept strong password"
                },
                {
                    name: "Weak password test"
                    input: "123"
                    expected: { valid: false, strength: "weak" }
                    description: "Should reject weak password"
                }
            ]
            
            [testing_methods]
            # Directive testing methods
            run_unit_tests: """
                function runUnitTests(directiveName, testCases) {
                    let results = [];
                    
                    for (let testCase of testCases) {
                        let startTime = System.currentTimeMillis();
                        let result;
                        
                        try {
                            result = executeDirective(directiveName, [testCase.input]);
                            let success = compareResults(result, testCase.expected);
                            
                            results.push({
                                name: testCase.name,
                                success: success,
                                input: testCase.input,
                                expected: testCase.expected,
                                actual: result,
                                duration: System.currentTimeMillis() - startTime,
                                error: null
                            });
                        } catch (error) {
                            results.push({
                                name: testCase.name,
                                success: false,
                                input: testCase.input,
                                expected: testCase.expected,
                                actual: null,
                                duration: System.currentTimeMillis() - startTime,
                                error: error.message
                            });
                        }
                    }
                    
                    return {
                        directive: directiveName,
                        total_tests: testCases.length,
                        passed_tests: results.filter(r => r.success).length,
                        failed_tests: results.filter(r => !r.success).length,
                        results: results
                    };
                }
            """
            
            run_performance_tests: """
                function runPerformanceTests(directiveName, testCases, iterations) {
                    let performanceResults = [];
                    
                    for (let testCase of testCases) {
                        let times = [];
                        
                        for (let i = 0; i < iterations; i++) {
                            let startTime = System.nanoTime();
                            executeDirective(directiveName, [testCase.input]);
                            let endTime = System.nanoTime();
                            times.push(endTime - startTime);
                        }
                        
                        let avgTime = times.reduce((a, b) => a + b, 0) / times.length;
                        let minTime = Math.min(...times);
                        let maxTime = Math.max(...times);
                        
                        performanceResults.push({
                            test_case: testCase.name,
                            average_time_ns: avgTime,
                            min_time_ns: minTime,
                            max_time_ns: maxTime,
                            iterations: iterations
                        });
                    }
                    
                    return {
                        directive: directiveName,
                        performance_results: performanceResults
                    };
                }
            """
            
            generate_test_report: """
                function generateTestReport(testResults, performanceResults) {
                    let report = {
                        timestamp: new Date(),
                        summary: {
                            total_directives: testResults.length,
                            total_tests: testResults.reduce((sum, r) => sum + r.total_tests, 0),
                            total_passed: testResults.reduce((sum, r) => sum + r.passed_tests, 0),
                            total_failed: testResults.reduce((sum, r) => sum + r.failed_tests, 0),
                            success_rate: 0
                        },
                        test_results: testResults,
                        performance_results: performanceResults
                    };
                    
                    report.summary.success_rate = (report.summary.total_passed / report.summary.total_tests) * 100;
                    
                    return report;
                }
            """
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Initialize directive tester
        TuskDirectiveTester directiveTester = new TuskDirectiveTester();
        directiveTester.configure(config);
        
        // Run unit tests
        Map<String, Object> emailTestResults = directiveTester.runUnitTests("testing_methods", 
            "run_unit_tests", Map.of("directiveName", "email", "testCases", config.get("test_cases.email_validation_tests")));
        
        Map<String, Object> passwordTestResults = directiveTester.runUnitTests("testing_methods", 
            "run_unit_tests", Map.of("directiveName", "password", "testCases", config.get("test_cases.password_validation_tests")));
        
        // Run performance tests
        Map<String, Object> emailPerformanceResults = directiveTester.runPerformanceTests("testing_methods", 
            "run_performance_tests", Map.of("directiveName", "email", "testCases", config.get("test_cases.email_validation_tests"), "iterations", 1000));
        
        Map<String, Object> passwordPerformanceResults = directiveTester.runPerformanceTests("testing_methods", 
            "run_performance_tests", Map.of("directiveName", "password", "testCases", config.get("test_cases.password_validation_tests"), "iterations", 1000));
        
        // Generate test report
        Map<String, Object> testReport = directiveTester.generateTestReport("testing_methods", 
            "generate_test_report", Map.of(
                "testResults", List.of(emailTestResults, passwordTestResults),
                "performanceResults", List.of(emailPerformanceResults, passwordPerformanceResults)
            ));
        
        System.out.println("Directive Testing Results:");
        System.out.println("Email validation tests: " + emailTestResults.get("passed_tests") + "/" + emailTestResults.get("total_tests") + " passed");
        System.out.println("Password validation tests: " + passwordTestResults.get("passed_tests") + "/" + passwordTestResults.get("total_tests") + " passed");
        
        System.out.println("Test Report Summary:");
        System.out.println("Total tests: " + testReport.get("summary.total_tests"));
        System.out.println("Passed: " + testReport.get("summary.total_passed"));
        System.out.println("Failed: " + testReport.get("summary.total_failed"));
        System.out.println("Success rate: " + testReport.get("summary.success_rate") + "%");
    }
}
```

## 🔧 Spring Boot Integration

### 1. Spring Boot Configuration
```java
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.tusklang.java.TuskLang;
import org.tusklang.java.spring.TuskCustomDirectiveConfig;

@SpringBootApplication
@Configuration
public class CustomDirectiveApplication {
    
    @Bean
    public TuskCustomDirectiveConfig tuskCustomDirectiveConfig() {
        TuskLang parser = new TuskLang();
        return parser.parseFile("custom-directives.tsk", TuskCustomDirectiveConfig.class);
    }
    
    public static void main(String[] args) {
        SpringApplication.run(CustomDirectiveApplication.class, args);
    }
}

@TuskConfig
public class TuskCustomDirectiveConfig {
    private ValidationDirectiveConfig validationDirectives;
    private BusinessLogicDirectiveConfig businessLogicDirectives;
    private DataTransformationDirectiveConfig transformationDirectives;
    private AdvancedDirectiveConfig advancedDirectives;
    
    // Getters and setters
    public ValidationDirectiveConfig getValidationDirectives() { return validationDirectives; }
    public void setValidationDirectives(ValidationDirectiveConfig validationDirectives) { this.validationDirectives = validationDirectives; }
    
    public BusinessLogicDirectiveConfig getBusinessLogicDirectives() { return businessLogicDirectives; }
    public void setBusinessLogicDirectives(BusinessLogicDirectiveConfig businessLogicDirectives) { this.businessLogicDirectives = businessLogicDirectives; }
    
    public DataTransformationDirectiveConfig getTransformationDirectives() { return transformationDirectives; }
    public void setTransformationDirectives(DataTransformationDirectiveConfig transformationDirectives) { this.transformationDirectives = transformationDirectives; }
    
    public AdvancedDirectiveConfig getAdvancedDirectives() { return advancedDirectives; }
    public void setAdvancedDirectives(AdvancedDirectiveConfig advancedDirectives) { this.advancedDirectives = advancedDirectives; }
}
```

## 🎯 Best Practices

### 1. Custom Directive Design Patterns
```java
// ✅ Use appropriate directive types
- Validation directives: For data validation
- Business logic directives: For business rules
- Transformation directives: For data formatting
- Composite directives: For complex operations

// ✅ Implement proper error handling
- Validate input parameters
- Handle directive failures
- Provide meaningful error messages
- Log directive execution

// ✅ Use directive composition
- Chain multiple directives
- Reuse common directives
- Implement decorator patterns
- Use factory patterns

// ✅ Optimize directive performance
- Cache directive results
- Use async execution when appropriate
- Monitor directive performance
- Implement lazy loading
```

### 2. Testing and Validation
```java
// 1. Unit Testing
- Test individual directives
- Test edge cases
- Test error conditions
- Measure performance

// 2. Integration Testing
- Test directive chains
- Test with real data
- Test error propagation
- Validate results

// 3. Performance Testing
- Measure execution time
- Test with large datasets
- Monitor memory usage
- Optimize bottlenecks

// 4. Documentation
- Document directive purpose
- Document parameters
- Document return values
- Provide usage examples
```

## 🚀 Summary

TuskLang Java custom hash directives provide:

- **Custom Validation Directives**: Email, phone, password, credit card validation
- **Business Logic Directives**: Tax calculation, shipping calculation
- **Data Transformation Directives**: Currency formatting, date formatting
- **Advanced Directive Patterns**: Async, composite, conditional directives
- **Spring Boot Integration**: Native Spring Boot configuration support

With these custom directive features, your Java applications will achieve enterprise-grade directive management while maintaining the flexibility and power of TuskLang configuration.

**"We don't bow to any king" - Build custom directives like a Java master with TuskLang!** 