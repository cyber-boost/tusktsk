# @ Render Function - Java Edition

The `@render` function provides template rendering and view generation capabilities in Java applications. This guide covers template processing, view rendering, dynamic content generation, and implementing robust rendering logic using TuskLang with Spring Boot integration and Java enterprise patterns.

## 🎯 Render Function Overview

### Template Rendering in Java

```java
@RestController
@RequestMapping("/api")
public class RenderController {
    
    @Autowired
    private TuskConfig tuskConfig;
    
    @GetMapping("/render-template")
    public ResponseEntity<?> renderTemplate(@RequestParam String templateName) {
        // TuskLang handles template rendering and processing
        return tuskConfig.getRenderHandler().renderTemplate(templateName);
    }
    
    @PostMapping("/render-dynamic")
    public ResponseEntity<?> renderDynamic(@RequestBody RenderRequest request) {
        // TuskLang handles dynamic content rendering
        return tuskConfig.getRenderHandler().renderDynamicContent(request);
    }
    
    @GetMapping("/render-email")
    public ResponseEntity<?> renderEmail(@RequestParam String emailType) {
        // TuskLang handles email template rendering
        return tuskConfig.getRenderHandler().renderEmailTemplate(emailType);
    }
}
```

```tusk
# app.tsk - Render handling configuration
render_handlers: {
    render_template: @lambda({
        template_name = @request.query.template_name ?? "default"
        
        # Get template data via Java service
        template_data = @java.invoke("TemplateService", "getTemplate", template_name)
        
        @if(!template_data, {
            return: {
                status: 404
                body: { error: "Template not found" }
            }
        })
        
        # Get dynamic data for template
        dynamic_data = @java.invoke("DataService", "getTemplateData", template_name, {
            user_id: @session.get("user_id")
            request_path: @request.path
            timestamp: @time.now()
        })
        
        # Render template
        rendered_content = @render(template_data.content, dynamic_data)
        
        return: {
            status: 200
            body: { 
                content: rendered_content
                template: template_name
                rendered_at: @time.now()
            }
            headers: { "Content-Type": "text/html" }
        }
    }),
    
    render_dynamic_content: @lambda({
        # Extract render request data
        render_data = {
            template: @request.post.template
            data: @request.post.data ?? {}
            options: @request.post.options ?? {}
        }
        
        # Validate template via Java service
        validation = @java.invoke("TemplateService", "validateTemplate", render_data.template)
        
        @if(!validation.valid, {
            return: {
                status: 400
                body: { error: validation.errors }
            }
        })
        
        # Process template data via Java service
        processed_data = @java.invoke("DataProcessingService", "processForTemplate", render_data.data)
        
        # Render with options
        render_options = @merge({
            escape_html: true
            pretty_print: false
            cache_result: true
        }, render_data.options)
        
        rendered_content = @render(render_data.template, processed_data, render_options)
        
        return: {
            status: 200
            body: { 
                content: rendered_content
                template_size: @len(render_data.template)
                data_keys: @keys(processed_data)
                rendered_at: @time.now()
            }
        }
    }),
    
    render_email_template: @lambda({
        email_type = @request.query.email_type ?? "welcome"
        
        # Get email template via Java service
        email_template = @java.invoke("EmailTemplateService", "getTemplate", email_type)
        
        @if(!email_template, {
            return: {
                status: 404
                body: { error: "Email template not found" }
            }
        })
        
        # Get user data for email
        user_data = @java.invoke("UserService", "getCurrentUser")
        
        # Prepare email data
        email_data = {
            user: user_data
            company: {
                name: "TuskLang Inc"
                logo: "https://tusklang.org/logo.png"
                website: "https://tusklang.org"
            }
            email: {
                type: email_type
                subject: email_template.subject
                generated_at: @time.now()
            }
        }
        
        # Render email template
        rendered_email = @render(email_template.content, email_data, {
            escape_html: false
            pretty_print: true
            include_doctype: true
        })
        
        return: {
            status: 200
            body: { 
                subject: email_template.subject
                content: rendered_email
                template: email_type
                rendered_at: @time.now()
            }
            headers: { "Content-Type": "text/html" }
        }
    })
}
```

## 🎨 Template Processing

### Basic Template Rendering

```tusk
# Basic template rendering with Java integration
template_rendering: {
    # Render simple template
    render_simple_template: @lambda(template, data, {
        # Basic template rendering
        rendered_content = @render(template, data)
        
        return: rendered_content
    }),
    
    # Render with options
    render_with_options: @lambda(template, data, options = {}, {
        # Default options
        default_options = {
            escape_html: true
            pretty_print: false
            cache_result: false
            timeout: "30s"
        }
        
        # Merge with provided options
        render_options = @merge(default_options, options)
        
        # Render with options
        rendered_content = @render(template, data, render_options)
        
        return: rendered_content
    }),
    
    # Render with error handling
    render_with_error_handling: @lambda(template, data, {
        try {
            rendered_content = @render(template, data)
            return: { success: true, content: rendered_content }
        }, {
            return: { 
                success: false, 
                error: "Template rendering failed",
                fallback_content: @get_fallback_content()
            }
        }
    }),
    
    # Get fallback content
    get_fallback_content: @lambda({
        return: "<div>Content temporarily unavailable</div>"
    })
}
```

### Advanced Template Features

```tusk
# Advanced template features with Java integration
advanced_template_features: {
    # Render with partials
    render_with_partials: @lambda(template, data, {
        # Include partials via Java service
        partials = @java.invoke("PartialService", "getPartials", template)
        
        # Merge partials with data
        extended_data = @merge(data, { partials: partials })
        
        # Render with partials
        rendered_content = @render(template, extended_data)
        
        return: rendered_content
    }),
    
    # Render with layouts
    render_with_layout: @lambda(template, data, layout_name, {
        # Get layout via Java service
        layout = @java.invoke("LayoutService", "getLayout", layout_name)
        
        @if(!layout, {
            return: @render(template, data)
        })
        
        # Render content first
        content = @render(template, data)
        
        # Render layout with content
        layout_data = @merge(data, { content: content })
        rendered_layout = @render(layout, layout_data)
        
        return: rendered_layout
    }),
    
    # Render with caching
    render_with_caching: @lambda(template, data, cache_key, ttl = "1h", {
        # Check cache first
        cached_content = @cache.get(cache_key)
        
        @if(cached_content, {
            return: cached_content
        })
        
        # Render template
        rendered_content = @render(template, data)
        
        # Cache result
        @cache.set(cache_key, rendered_content, ttl)
        
        return: rendered_content
    }),
    
    # Render with conditional logic
    render_with_conditions: @lambda(template, data, conditions, {
        # Process conditions via Java service
        processed_conditions = @java.invoke("ConditionService", "processConditions", conditions)
        
        # Merge conditions with data
        extended_data = @merge(data, { conditions: processed_conditions })
        
        # Render with conditions
        rendered_content = @render(template, extended_data)
        
        return: rendered_content
    })
}
```

## 📧 Email Template Rendering

### Email Template Processing

```tusk
# Email template rendering with Java integration
email_template_rendering: {
    # Render welcome email
    render_welcome_email: @lambda(user_data, {
        # Get welcome email template
        template = @java.invoke("EmailTemplateService", "getTemplate", "welcome")
        
        # Prepare email data
        email_data = {
            user: user_data
            welcome_message: "Welcome to TuskLang!"
            activation_link: @java.invoke("EmailService", "generateActivationLink", user_data.id)
            company_info: {
                name: "TuskLang Inc"
                support_email: "support@tusklang.org"
                website: "https://tusklang.org"
            }
            email_metadata: {
                type: "welcome"
                generated_at: @time.now()
                template_version: "1.0"
            }
        }
        
        # Render email
        rendered_email = @render(template.content, email_data, {
            escape_html: false
            pretty_print: true
            include_doctype: true
            inline_css: true
        })
        
        return: {
            subject: template.subject
            content: rendered_email
            data: email_data
        }
    }),
    
    # Render notification email
    render_notification_email: @lambda(notification_data, {
        # Get notification template
        template = @java.invoke("EmailTemplateService", "getTemplate", "notification")
        
        # Prepare notification data
        email_data = {
            notification: notification_data
            user: @java.invoke("UserService", "getUserById", notification_data.user_id)
            action_links: @java.invoke("NotificationService", "generateActionLinks", notification_data)
            preferences: @java.invoke("UserService", "getNotificationPreferences", notification_data.user_id)
            email_metadata: {
                type: "notification"
                priority: notification_data.priority
                generated_at: @time.now()
            }
        }
        
        # Render email
        rendered_email = @render(template.content, email_data, {
            escape_html: false
            pretty_print: true
            include_doctype: true
        })
        
        return: {
            subject: template.subject
            content: rendered_email
            priority: notification_data.priority
        }
    }),
    
    # Render password reset email
    render_password_reset_email: @lambda(user_data, reset_token, {
        # Get password reset template
        template = @java.invoke("EmailTemplateService", "getTemplate", "password_reset")
        
        # Prepare reset data
        email_data = {
            user: user_data
            reset_link: @java.invoke("AuthService", "generateResetLink", reset_token)
            expiry_time: @time.add(@time.now(), "1h")
            security_info: {
                ip_address: @request.ip
                user_agent: @request.headers["User-Agent"]
                request_time: @time.now()
            }
            email_metadata: {
                type: "password_reset"
                generated_at: @time.now()
                security_level: "high"
            }
        }
        
        # Render email
        rendered_email = @render(template.content, email_data, {
            escape_html: false
            pretty_print: true
            include_doctype: true
        })
        
        return: {
            subject: template.subject
            content: rendered_email
            security_level: "high"
        }
    })
}
```

## 📄 Document Template Rendering

### PDF and Document Generation

```tusk
# Document template rendering with Java integration
document_template_rendering: {
    # Render PDF invoice
    render_pdf_invoice: @lambda(invoice_data, {
        # Get invoice template
        template = @java.invoke("DocumentTemplateService", "getTemplate", "invoice")
        
        # Prepare invoice data
        document_data = {
            invoice: invoice_data
            company: @java.invoke("CompanyService", "getCompanyInfo")
            customer: @java.invoke("CustomerService", "getCustomerById", invoice_data.customer_id)
            items: @java.invoke("InvoiceService", "getInvoiceItems", invoice_data.id)
            totals: @java.invoke("InvoiceService", "calculateTotals", invoice_data.id)
            document_metadata: {
                type: "invoice"
                generated_at: @time.now()
                template_version: "2.0"
            }
        }
        
        # Render document
        rendered_document = @render(template.content, document_data, {
            format: "pdf"
            pretty_print: true
            include_header: true
            include_footer: true
        })
        
        return: {
            content: rendered_document
            filename: "invoice_${invoice_data.number}.pdf"
            metadata: document_data.document_metadata
        }
    }),
    
    # Render report document
    render_report_document: @lambda(report_data, {
        # Get report template
        template = @java.invoke("DocumentTemplateService", "getTemplate", "report")
        
        # Prepare report data
        document_data = {
            report: report_data
            charts: @java.invoke("ChartService", "generateCharts", report_data)
            tables: @java.invoke("TableService", "generateTables", report_data)
            summary: @java.invoke("ReportService", "generateSummary", report_data)
            document_metadata: {
                type: "report"
                generated_at: @time.now()
                author: @session.get("username")
            }
        }
        
        # Render document
        rendered_document = @render(template.content, document_data, {
            format: "html"
            pretty_print: true
            include_css: true
            include_js: true
        })
        
        return: {
            content: rendered_document
            filename: "report_${report_data.id}.html"
            metadata: document_data.document_metadata
        }
    })
}
```

## 🔧 Java Service Integration

### Template Rendering Service

```java
@Service
public class TemplateRenderingService {
    
    @Autowired
    private TemplateService templateService;
    
    @Autowired
    private DataService dataService;
    
    @Autowired
    private EmailTemplateService emailTemplateService;
    
    @Autowired
    private DocumentTemplateService documentTemplateService;
    
    public ResponseEntity<?> renderTemplate(String templateName) {
        try {
            // Get template
            Template template = templateService.getTemplate(templateName);
            if (template == null) {
                return ResponseEntity.notFound()
                    .body(Map.of("error", "Template not found"));
            }
            
            // Get dynamic data
            Map<String, Object> dynamicData = dataService.getTemplateData(templateName, getRequestContext());
            
            // Render template
            String renderedContent = renderTemplate(template.getContent(), dynamicData);
            
            return ResponseEntity.ok()
                .header("Content-Type", "text/html")
                .body(Map.of(
                    "content", renderedContent,
                    "template", templateName,
                    "rendered_at", LocalDateTime.now()
                ));
                
        } catch (Exception e) {
            return ResponseEntity.internalServerError()
                .body(Map.of("error", "Template rendering failed", "details", e.getMessage()));
        }
    }
    
    public ResponseEntity<?> renderDynamicContent(RenderRequest request) {
        try {
            // Validate template
            TemplateValidationResult validation = templateService.validateTemplate(request.getTemplate());
            if (!validation.isValid()) {
                return ResponseEntity.badRequest()
                    .body(Map.of("error", validation.getErrors()));
            }
            
            // Process data
            Map<String, Object> processedData = dataProcessingService.processForTemplate(request.getData());
            
            // Render with options
            RenderOptions options = buildRenderOptions(request.getOptions());
            String renderedContent = renderTemplate(request.getTemplate(), processedData, options);
            
            return ResponseEntity.ok(Map.of(
                "content", renderedContent,
                "template_size", request.getTemplate().length(),
                "data_keys", processedData.keySet(),
                "rendered_at", LocalDateTime.now()
            ));
            
        } catch (Exception e) {
            return ResponseEntity.internalServerError()
                .body(Map.of("error", "Dynamic content rendering failed", "details", e.getMessage()));
        }
    }
    
    public ResponseEntity<?> renderEmailTemplate(String emailType) {
        try {
            // Get email template
            EmailTemplate emailTemplate = emailTemplateService.getTemplate(emailType);
            if (emailTemplate == null) {
                return ResponseEntity.notFound()
                    .body(Map.of("error", "Email template not found"));
            }
            
            // Get user data
            User user = userService.getCurrentUser();
            
            // Prepare email data
            Map<String, Object> emailData = buildEmailData(user, emailType);
            
            // Render email
            String renderedEmail = renderTemplate(emailTemplate.getContent(), emailData, getEmailRenderOptions());
            
            return ResponseEntity.ok()
                .header("Content-Type", "text/html")
                .body(Map.of(
                    "subject", emailTemplate.getSubject(),
                    "content", renderedEmail,
                    "template", emailType,
                    "rendered_at", LocalDateTime.now()
                ));
                
        } catch (Exception e) {
            return ResponseEntity.internalServerError()
                .body(Map.of("error", "Email template rendering failed", "details", e.getMessage()));
        }
    }
    
    private String renderTemplate(String template, Map<String, Object> data) {
        return renderTemplate(template, data, new RenderOptions());
    }
    
    private String renderTemplate(String template, Map<String, Object> data, RenderOptions options) {
        // Template rendering implementation
        // This would use a template engine like Thymeleaf, FreeMarker, or Velocity
        return templateEngine.render(template, data, options);
    }
    
    private Map<String, Object> getRequestContext() {
        return Map.of(
            "user_id", session.getAttribute("user_id"),
            "request_path", request.getRequestURI(),
            "timestamp", LocalDateTime.now()
        );
    }
    
    private RenderOptions buildRenderOptions(Map<String, Object> options) {
        RenderOptions renderOptions = new RenderOptions();
        renderOptions.setEscapeHtml((Boolean) options.getOrDefault("escape_html", true));
        renderOptions.setPrettyPrint((Boolean) options.getOrDefault("pretty_print", false));
        renderOptions.setCacheResult((Boolean) options.getOrDefault("cache_result", true));
        return renderOptions;
    }
    
    private Map<String, Object> buildEmailData(User user, String emailType) {
        Map<String, Object> emailData = new HashMap<>();
        emailData.put("user", user);
        emailData.put("company", Map.of(
            "name", "TuskLang Inc",
            "logo", "https://tusklang.org/logo.png",
            "website", "https://tusklang.org"
        ));
        emailData.put("email", Map.of(
            "type", emailType,
            "generated_at", LocalDateTime.now()
        ));
        return emailData;
    }
    
    private RenderOptions getEmailRenderOptions() {
        RenderOptions options = new RenderOptions();
        options.setEscapeHtml(false);
        options.setPrettyPrint(true);
        options.setIncludeDoctype(true);
        return options;
    }
}
```

### Template Service

```java
@Service
public class TemplateService {
    
    @Autowired
    private TemplateRepository templateRepository;
    
    @Autowired
    private TemplateValidationService validationService;
    
    public Template getTemplate(String templateName) {
        return templateRepository.findByName(templateName);
    }
    
    public TemplateValidationResult validateTemplate(String template) {
        List<String> errors = new ArrayList<>();
        
        // Basic validation
        if (template == null || template.trim().isEmpty()) {
            errors.add("Template content is required");
        }
        
        // Syntax validation
        if (!validationService.validateSyntax(template)) {
            errors.add("Template syntax is invalid");
        }
        
        // Security validation
        if (validationService.containsSecurityIssues(template)) {
            errors.add("Template contains security issues");
        }
        
        return new TemplateValidationResult(errors.isEmpty(), errors);
    }
    
    public List<Template> getTemplatesByType(String type) {
        return templateRepository.findByType(type);
    }
    
    public Template createTemplate(Template template) {
        // Validate template
        TemplateValidationResult validation = validateTemplate(template.getContent());
        if (!validation.isValid()) {
            throw new IllegalArgumentException("Invalid template: " + validation.getErrors());
        }
        
        return templateRepository.save(template);
    }
    
    public Template updateTemplate(String templateName, String content) {
        Template template = getTemplate(templateName);
        if (template == null) {
            throw new IllegalArgumentException("Template not found: " + templateName);
        }
        
        // Validate new content
        TemplateValidationResult validation = validateTemplate(content);
        if (!validation.isValid()) {
            throw new IllegalArgumentException("Invalid template content: " + validation.getErrors());
        }
        
        template.setContent(content);
        template.setUpdatedAt(LocalDateTime.now());
        
        return templateRepository.save(template);
    }
}
```

## 🚀 Advanced Rendering Features

### Rendering Analytics

```tusk
# Rendering analytics with Java integration
rendering_analytics: {
    # Track rendering metrics
    track_rendering_metrics: @lambda({
        # Extract rendering metrics
        rendering_metrics = {
            template_name: @request.query.template_name
            rendering_time: @request.processing_time
            template_size: @len(@request.post.template ?? "")
            data_size: @len(@json.stringify(@request.post.data ?? {}))
            operation_type: "template_rendering"
            timestamp: @time.now()
            user_id: @session.get("user_id")
        }
        
        # Send metrics to Java analytics service
        @java.invoke("RenderingAnalyticsService", "track", rendering_metrics)
    }),
    
    # Analyze rendering patterns
    analyze_rendering_patterns: @lambda(template_name, {
        # Get rendering analysis from Java service
        analysis = @java.invoke("RenderingAnalysisService", "analyze", template_name)
        
        # Log insights
        @if(analysis.insights, {
            @log.info("Rendering insights: ${analysis.insights}")
        })
        
        return: analysis
    })
}
```

### Rendering Caching

```tusk
# Rendering caching with Java integration
rendering_caching: {
    # Cache rendered content
    cache_rendered_content: @lambda(template, data, cache_key, ttl = "1h", {
        # Check cache first
        cached_content = @cache.get(cache_key)
        
        @if(cached_content, {
            return: { success: true, content: cached_content, cached: true }
        })
        
        # Render template
        rendered_content = @render(template, data)
        
        # Cache result
        @cache.set(cache_key, rendered_content, ttl)
        
        return: { success: true, content: rendered_content, cached: false }
    }),
    
    # Cache with data hash
    cache_with_data_hash: @lambda(template, data, ttl = "1h", {
        # Generate cache key from data hash
        data_hash = @java.invoke("HashService", "generateHash", @json.stringify(data))
        cache_key = "render:${template}:${data_hash}"
        
        # Cache rendered content
        return: @cache_rendered_content(template, data, cache_key, ttl)
    })
}
```

## 🔒 Security Considerations

### Rendering Security Patterns

```tusk
# Rendering security configuration
rendering_security_patterns: {
    # Template validation
    template_validation: {
        max_template_size: "1MB"
        allowed_tags: ["div", "span", "p", "h1", "h2", "h3", "a", "img"]
        block_script_tags: true
        validate_variables: true
        sanitize_output: true
    },
    
    # Data validation
    data_validation: {
        max_data_size: "10MB"
        validate_data_types: true
        block_sensitive_data: true
        sanitize_input: true
    },
    
    # Rendering logging
    rendering_logging: {
        enabled: true
        log_level: "info"
        log_template_rendering: true
        log_rendering_errors: true
        mask_sensitive_data: true
    }
}
```

## 🧪 Testing Render Handlers

### Render Testing Configuration

```tusk
# Render testing configuration
render_testing: {
    # Test cases for template rendering
    template_rendering_test_cases: [
        {
            name: "simple_template_rendering"
            template: "Hello {{name}}, welcome to {{company}}!"
            data: { name: "John", company: "TuskLang" }
            expected: { contains: "Hello John, welcome to TuskLang!" }
        },
        {
            name: "complex_template_rendering"
            template: "User: {{user.name}}, Email: {{user.email}}"
            data: { user: { name: "John", email: "john@example.com" } }
            expected: { contains: "User: John", contains: "Email: john@example.com" }
        },
        {
            name: "template_with_conditions"
            template: "{{#if user.active}}Welcome back!{{else}}Please activate your account.{{/if}}"
            data: { user: { active: true } }
            expected: { contains: "Welcome back!" }
        }
    ],
    
    # Test cases for email rendering
    email_rendering_test_cases: [
        {
            name: "welcome_email_rendering"
            email_type: "welcome"
            user_data: { name: "John", email: "john@example.com" }
            expected: { contains: "Welcome", contains: "John" }
        },
        {
            name: "notification_email_rendering"
            email_type: "notification"
            notification_data: { message: "Test notification" }
            expected: { contains: "notification" }
        }
    ]
}
```

## 🚀 Best Practices

### Render Handling Best Practices

1. **Use Java Services**: Delegate template rendering to Java services for better maintainability
2. **Implement Proper Validation**: Validate templates and data before rendering
3. **Use Caching**: Cache frequently rendered templates for performance
4. **Handle Errors Gracefully**: Provide fallback content for rendering failures
5. **Sanitize Output**: Always sanitize rendered content to prevent XSS attacks
6. **Use Template Engines**: Leverage proper template engines for complex rendering
7. **Implement Analytics**: Track rendering performance and patterns
8. **Optimize Templates**: Keep templates efficient and avoid complex logic

### Common Patterns

```tusk
# Common render handling patterns
common_patterns: {
    # Template patterns
    template_patterns: {
        simple_template: "Basic variable substitution templates"
        conditional_template: "Templates with conditional logic"
        loop_template: "Templates with iteration and loops"
        layout_template: "Templates with layout inheritance"
    },
    
    # Email patterns
    email_patterns: {
        welcome_email: "User welcome and onboarding emails"
        notification_email: "System notification emails"
        transactional_email: "Transaction confirmation emails"
        marketing_email: "Marketing and promotional emails"
    },
    
    # Document patterns
    document_patterns: {
        invoice_document: "Invoice and billing documents"
        report_document: "Analytics and report documents"
        certificate_document: "Certificate and credential documents"
        contract_document: "Legal and contract documents"
    }
}
```

---

**We don't bow to any king** - TuskLang Java Edition empowers you to handle template rendering with enterprise-grade patterns, Spring Boot integration, and the flexibility to adapt to your preferred approach. Whether you're building web applications, generating emails, or creating documents, TuskLang provides the tools you need to handle rendering efficiently and securely. 