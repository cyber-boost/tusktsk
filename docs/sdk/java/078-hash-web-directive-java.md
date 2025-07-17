# #web - Web Directive (Java)

The `#web` directive provides enterprise-grade web application capabilities for Java applications, enabling dynamic web page generation, routing, and server-side rendering with Spring Boot integration.

## Basic Syntax

```tusk
# Basic web route
#web /home {
    @render("home.html", {
        title: "Welcome",
        user: @auth.user
    })
}

# Web route with parameters
#web /user/{id} {
    user: @get_user(@params.id)
    @render("user.html", {user: user})
}

# Web route with query parameters
#web /search {
    query: @request.query.q
    results: @search_database(query)
    @render("search.html", {query: query, results: results})
}
```

## Java Implementation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.WebDirective;
import org.springframework.web.bind.annotation.*;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.servlet.ModelAndView;

@Controller
public class WebController {
    
    private final TuskLang tuskLang;
    private final WebDirective webDirective;
    private final UserService userService;
    private final SearchService searchService;
    
    public WebController(TuskLang tuskLang, 
                        UserService userService,
                        SearchService searchService) {
        this.tuskLang = tuskLang;
        this.webDirective = new WebDirective();
        this.userService = userService;
        this.searchService = searchService;
    }
    
    // Basic web route
    @GetMapping("/home")
    public String home(Model model, @AuthenticationPrincipal User user) {
        model.addAttribute("title", "Welcome");
        model.addAttribute("user", user);
        return "home";
    }
    
    // Web route with path parameters
    @GetMapping("/user/{id}")
    public String userProfile(@PathVariable Long id, Model model) {
        User user = userService.getUserById(id);
        model.addAttribute("user", user);
        return "user";
    }
    
    // Web route with query parameters
    @GetMapping("/search")
    public String search(@RequestParam String q, Model model) {
        List<SearchResult> results = searchService.searchDatabase(q);
        model.addAttribute("query", q);
        model.addAttribute("results", results);
        return "search";
    }
}
```

## Web Configuration

```tusk
# Detailed web configuration
#web {
    route: "/dashboard"
    method: "GET"
    template: "dashboard.html"
    layout: "main.html"
    cache: true
    cache_ttl: 300
} {
    data: @get_dashboard_data(@auth.user.id)
    @render(template, {data: data, user: @auth.user})
}

# Web route with middleware
#web {
    route: "/admin"
    method: "GET"
    middleware: ["auth", "admin_check"]
    template: "admin.html"
} {
    @render(template, {admin_data: @get_admin_data()})
}

# Web route with conditions
#web {
    route: "/premium"
    method: "GET"
    condition: @auth.user.isPremium()
    template: "premium.html"
} {
    @render(template, {premium_content: @get_premium_content()})
}
```

## Java Web Configuration

```java
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;
import org.springframework.web.servlet.config.annotation.InterceptorRegistry;
import java.util.Map;
import java.util.List;

@Component
@ConfigurationProperties(prefix = "tusk.web")
public class WebConfig {
    
    private String defaultTemplate = "default";
    private String defaultLayout = "main";
    private boolean defaultCache = false;
    private int defaultCacheTtl = 300;
    
    private Map<String, WebRouteDefinition> routes;
    private List<String> globalMiddleware;
    private Map<String, String> templates;
    
    // Getters and setters
    public String getDefaultTemplate() { return defaultTemplate; }
    public void setDefaultTemplate(String defaultTemplate) { this.defaultTemplate = defaultTemplate; }
    
    public String getDefaultLayout() { return defaultLayout; }
    public void setDefaultLayout(String defaultLayout) { this.defaultLayout = defaultLayout; }
    
    public boolean isDefaultCache() { return defaultCache; }
    public void setDefaultCache(boolean defaultCache) { this.defaultCache = defaultCache; }
    
    public int getDefaultCacheTtl() { return defaultCacheTtl; }
    public void setDefaultCacheTtl(int defaultCacheTtl) { this.defaultCacheTtl = defaultCacheTtl; }
    
    public Map<String, WebRouteDefinition> getRoutes() { return routes; }
    public void setRoutes(Map<String, WebRouteDefinition> routes) { this.routes = routes; }
    
    public List<String> getGlobalMiddleware() { return globalMiddleware; }
    public void setGlobalMiddleware(List<String> globalMiddleware) { this.globalMiddleware = globalMiddleware; }
    
    public Map<String, String> getTemplates() { return templates; }
    public void setTemplates(Map<String, String> templates) { this.templates = templates; }
    
    public static class WebRouteDefinition {
        private String route;
        private String method = "GET";
        private String template;
        private String layout;
        private boolean cache;
        private int cacheTtl;
        private List<String> middleware;
        private String condition;
        private Map<String, Object> data;
        
        // Getters and setters
        public String getRoute() { return route; }
        public void setRoute(String route) { this.route = route; }
        
        public String getMethod() { return method; }
        public void setMethod(String method) { this.method = method; }
        
        public String getTemplate() { return template; }
        public void setTemplate(String template) { this.template = template; }
        
        public String getLayout() { return layout; }
        public void setLayout(String layout) { this.layout = layout; }
        
        public boolean isCache() { return cache; }
        public void setCache(boolean cache) { this.cache = cache; }
        
        public int getCacheTtl() { return cacheTtl; }
        public void setCacheTtl(int cacheTtl) { this.cacheTtl = cacheTtl; }
        
        public List<String> middleware() { return middleware; }
        public void setMiddleware(List<String> middleware) { this.middleware = middleware; }
        
        public String getCondition() { return condition; }
        public void setCondition(String condition) { this.condition = condition; }
        
        public Map<String, Object> getData() { return data; }
        public void setData(Map<String, Object> data) { this.data = data; }
    }
}

@Configuration
public class WebConfiguration implements WebMvcConfigurer {
    
    private final WebConfig config;
    private final List<HandlerInterceptor> interceptors;
    
    public WebConfiguration(WebConfig config, List<HandlerInterceptor> interceptors) {
        this.config = config;
        this.interceptors = interceptors;
    }
    
    @Override
    public void addInterceptors(InterceptorRegistry registry) {
        // Add global middleware
        if (config.getGlobalMiddleware() != null) {
            for (String middlewareName : config.getGlobalMiddleware()) {
                HandlerInterceptor interceptor = createInterceptor(middlewareName);
                registry.addInterceptor(interceptor);
            }
        }
        
        // Add route-specific middleware
        if (config.getRoutes() != null) {
            config.getRoutes().forEach((routeName, routeDef) -> {
                if (routeDef.getMiddleware() != null) {
                    for (String middlewareName : routeDef.getMiddleware()) {
                        HandlerInterceptor interceptor = createInterceptor(middlewareName);
                        registry.addInterceptor(interceptor).addPathPatterns(routeDef.getRoute());
                    }
                }
            });
        }
    }
    
    private HandlerInterceptor createInterceptor(String middlewareName) {
        switch (middlewareName) {
            case "auth":
                return new AuthInterceptor();
            case "admin_check":
                return new AdminCheckInterceptor();
            case "cache":
                return new CacheInterceptor();
            default:
                return new CustomInterceptor(middlewareName);
        }
    }
}
```

## Template Rendering

```tusk
# Template rendering with data
#web /dashboard {
    user_data: @get_user_data(@auth.user.id)
    recent_activity: @get_recent_activity(@auth.user.id)
    
    @render("dashboard.html", {
        user: @auth.user,
        user_data: user_data,
        recent_activity: recent_activity,
        title: "Dashboard"
    })
}

# Template with layout
#web /profile {
    profile: @get_user_profile(@auth.user.id)
    
    @render_with_layout("profile.html", "main.html", {
        profile: profile,
        title: "User Profile"
    })
}

# Conditional template rendering
#web /content {
    content: @get_content(@request.params.id)
    
    if (@content.isPremium()) {
        @render("premium-content.html", {content: content})
    } else {
        @render("free-content.html", {content: content})
    }
}
```

## Java Template Rendering

```java
import org.springframework.stereotype.Service;
import org.thymeleaf.TemplateEngine;
import org.thymeleaf.context.Context;
import org.springframework.ui.Model;

@Service
public class TemplateRenderingService {
    
    private final TemplateEngine templateEngine;
    private final WebConfig config;
    
    public TemplateRenderingService(TemplateEngine templateEngine, WebConfig config) {
        this.templateEngine = templateEngine;
        this.config = config;
    }
    
    public String renderTemplate(String templateName, Map<String, Object> data) {
        Context context = new Context();
        
        // Add data to context
        if (data != null) {
            data.forEach(context::setVariable);
        }
        
        // Add default data
        context.setVariable("config", config);
        context.setVariable("timestamp", LocalDateTime.now());
        
        return templateEngine.process(templateName, context);
    }
    
    public String renderWithLayout(String templateName, String layoutName, Map<String, Object> data) {
        // Create layout context
        Context layoutContext = new Context();
        layoutContext.setVariable("config", config);
        layoutContext.setVariable("timestamp", LocalDateTime.now());
        
        // Render main content
        String mainContent = renderTemplate(templateName, data);
        layoutContext.setVariable("content", mainContent);
        
        // Add data to layout context
        if (data != null) {
            data.forEach(layoutContext::setVariable);
        }
        
        return templateEngine.process(layoutName, layoutContext);
    }
    
    public String renderConditional(String condition, String trueTemplate, String falseTemplate, 
                                  Map<String, Object> data) {
        if (evaluateCondition(condition, data)) {
            return renderTemplate(trueTemplate, data);
        } else {
            return renderTemplate(falseTemplate, data);
        }
    }
    
    private boolean evaluateCondition(String condition, Map<String, Object> data) {
        // Simple condition evaluation
        if (condition.contains("isPremium")) {
            Object content = data.get("content");
            return content instanceof Content && ((Content) content).isPremium();
        }
        return false;
    }
}

@Controller
public class TemplateController {
    
    private final TemplateRenderingService templateService;
    private final UserService userService;
    private final ContentService contentService;
    
    public TemplateController(TemplateRenderingService templateService,
                            UserService userService,
                            ContentService contentService) {
        this.templateService = templateService;
        this.userService = userService;
        this.contentService = contentService;
    }
    
    @GetMapping("/dashboard")
    public String dashboard(@AuthenticationPrincipal User user, Model model) {
        UserData userData = userService.getUserData(user.getId());
        List<Activity> recentActivity = userService.getRecentActivity(user.getId());
        
        Map<String, Object> data = new HashMap<>();
        data.put("user", user);
        data.put("user_data", userData);
        data.put("recent_activity", recentActivity);
        data.put("title", "Dashboard");
        
        String rendered = templateService.renderTemplate("dashboard", data);
        model.addAttribute("renderedContent", rendered);
        
        return "layout";
    }
    
    @GetMapping("/profile")
    public String profile(@AuthenticationPrincipal User user, Model model) {
        UserProfile profile = userService.getUserProfile(user.getId());
        
        Map<String, Object> data = new HashMap<>();
        data.put("profile", profile);
        data.put("title", "User Profile");
        
        String rendered = templateService.renderWithLayout("profile", "main", data);
        model.addAttribute("renderedContent", rendered);
        
        return "layout";
    }
    
    @GetMapping("/content/{id}")
    public String content(@PathVariable Long id, Model model) {
        Content content = contentService.getContent(id);
        
        Map<String, Object> data = new HashMap<>();
        data.put("content", content);
        
        String rendered = templateService.renderConditional(
            "content.isPremium()", 
            "premium-content", 
            "free-content", 
            data
        );
        
        model.addAttribute("renderedContent", rendered);
        return "layout";
    }
}
```

## Dynamic Routing

```tusk
# Dynamic route with parameters
#web /product/{category}/{id} {
    category: @params.category
    product_id: @params.id
    product: @get_product(category, product_id)
    
    @render("product.html", {product: product, category: category})
}

# Dynamic route with optional parameters
#web /blog/{year}/{month}/{day} {
    year: @params.year || @date.year()
    month: @params.month || @date.month()
    day: @params.day || @date.day()
    
    posts: @get_blog_posts(year, month, day)
    @render("blog.html", {posts: posts, year: year, month: month, day: day})
}

# Dynamic route with query parameters
#web /api/search/{type} {
    search_type: @params.type
    query: @request.query.q
    filters: @request.query.filters
    
    results: @search_by_type(search_type, query, filters)
    @render_json({results: results, type: search_type})
}
```

## Java Dynamic Routing

```java
import org.springframework.web.bind.annotation.*;
import org.springframework.http.ResponseEntity;
import java.util.Map;
import java.util.List;

@Controller
public class DynamicRoutingController {
    
    private final ProductService productService;
    private final BlogService blogService;
    private final SearchService searchService;
    private final TemplateRenderingService templateService;
    
    public DynamicRoutingController(ProductService productService,
                                  BlogService blogService,
                                  SearchService searchService,
                                  TemplateRenderingService templateService) {
        this.productService = productService;
        this.blogService = blogService;
        this.searchService = searchService;
        this.templateService = templateService;
    }
    
    @GetMapping("/product/{category}/{id}")
    public String product(@PathVariable String category,
                         @PathVariable Long id,
                         Model model) {
        
        Product product = productService.getProduct(category, id);
        
        Map<String, Object> data = new HashMap<>();
        data.put("product", product);
        data.put("category", category);
        
        String rendered = templateService.renderTemplate("product", data);
        model.addAttribute("renderedContent", rendered);
        
        return "layout";
    }
    
    @GetMapping("/blog/{year}/{month}/{day}")
    public String blog(@PathVariable(required = false) Integer year,
                      @PathVariable(required = false) Integer month,
                      @PathVariable(required = false) Integer day,
                      Model model) {
        
        // Use current date if parameters are not provided
        LocalDate currentDate = LocalDate.now();
        int blogYear = year != null ? year : currentDate.getYear();
        int blogMonth = month != null ? month : currentDate.getMonthValue();
        int blogDay = day != null ? day : currentDate.getDayOfMonth();
        
        List<BlogPost> posts = blogService.getBlogPosts(blogYear, blogMonth, blogDay);
        
        Map<String, Object> data = new HashMap<>();
        data.put("posts", posts);
        data.put("year", blogYear);
        data.put("month", blogMonth);
        data.put("day", blogDay);
        
        String rendered = templateService.renderTemplate("blog", data);
        model.addAttribute("renderedContent", rendered);
        
        return "layout";
    }
    
    @GetMapping("/api/search/{type}")
    public ResponseEntity<Map<String, Object>> search(@PathVariable String type,
                                                     @RequestParam String q,
                                                     @RequestParam(required = false) String filters) {
        
        List<SearchResult> results = searchService.searchByType(type, q, filters);
        
        Map<String, Object> response = new HashMap<>();
        response.put("results", results);
        response.put("type", type);
        
        return ResponseEntity.ok(response);
    }
}

@Service
public class ProductService {
    
    private final ProductRepository productRepository;
    
    public ProductService(ProductRepository productRepository) {
        this.productRepository = productRepository;
    }
    
    public Product getProduct(String category, Long id) {
        return productRepository.findByCategoryAndId(category, id)
            .orElseThrow(() -> new ProductNotFoundException("Product not found"));
    }
}

@Service
public class BlogService {
    
    private final BlogPostRepository blogPostRepository;
    
    public BlogService(BlogPostRepository blogPostRepository) {
        this.blogPostRepository = blogPostRepository;
    }
    
    public List<BlogPost> getBlogPosts(int year, int month, int day) {
        LocalDate date = LocalDate.of(year, month, day);
        return blogPostRepository.findByDate(date);
    }
}

@Service
public class SearchService {
    
    private final Map<String, SearchStrategy> searchStrategies;
    
    public SearchService(List<SearchStrategy> strategies) {
        this.searchStrategies = strategies.stream()
            .collect(Collectors.toMap(SearchStrategy::getType, Function.identity()));
    }
    
    public List<SearchResult> searchByType(String type, String query, String filters) {
        SearchStrategy strategy = searchStrategies.get(type);
        if (strategy == null) {
            throw new IllegalArgumentException("Unknown search type: " + type);
        }
        
        return strategy.search(query, filters);
    }
}
```

## Form Handling

```tusk
# Form display
#web /contact {
    @render("contact.html", {title: "Contact Us"})
}

# Form processing
#web /contact method: "POST" {
    name: @request.body.name
    email: @request.body.email
    message: @request.body.message
    
    @validate_form({name: name, email: email, message: message})
    @send_contact_email(name, email, message)
    @redirect("/contact?success=true")
}

# Form with file upload
#web /upload method: "POST" {
    file: @request.files.upload
    description: @request.body.description
    
    @validate_file(file)
    @upload_file(file, description)
    @redirect("/upload?success=true")
}
```

## Java Form Handling

```java
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;
import org.springframework.validation.BindingResult;
import javax.validation.Valid;

@Controller
public class FormController {
    
    private final ContactService contactService;
    private final FileUploadService fileUploadService;
    private final ValidationService validationService;
    
    public FormController(ContactService contactService,
                         FileUploadService fileUploadService,
                         ValidationService validationService) {
        this.contactService = contactService;
        this.fileUploadService = fileUploadService;
        this.validationService = validationService;
    }
    
    @GetMapping("/contact")
    public String contactForm(Model model) {
        model.addAttribute("title", "Contact Us");
        model.addAttribute("contactForm", new ContactForm());
        return "contact";
    }
    
    @PostMapping("/contact")
    public String processContact(@Valid @ModelAttribute ContactForm contactForm,
                               BindingResult bindingResult,
                               RedirectAttributes redirectAttributes) {
        
        if (bindingResult.hasErrors()) {
            return "contact";
        }
        
        try {
            // Validate form
            validationService.validateContactForm(contactForm);
            
            // Send contact email
            contactService.sendContactEmail(contactForm.getName(), 
                                          contactForm.getEmail(), 
                                          contactForm.getMessage());
            
            redirectAttributes.addFlashAttribute("success", "Message sent successfully!");
            return "redirect:/contact?success=true";
        } catch (Exception e) {
            redirectAttributes.addFlashAttribute("error", "Failed to send message: " + e.getMessage());
            return "redirect:/contact?error=true";
        }
    }
    
    @GetMapping("/upload")
    public String uploadForm(Model model) {
        model.addAttribute("uploadForm", new UploadForm());
        return "upload";
    }
    
    @PostMapping("/upload")
    public String processUpload(@ModelAttribute UploadForm uploadForm,
                              @RequestParam("file") MultipartFile file,
                              RedirectAttributes redirectAttributes) {
        
        try {
            // Validate file
            validationService.validateFile(file);
            
            // Upload file
            String fileUrl = fileUploadService.uploadFile(file, uploadForm.getDescription());
            
            redirectAttributes.addFlashAttribute("success", "File uploaded successfully!");
            redirectAttributes.addFlashAttribute("fileUrl", fileUrl);
            return "redirect:/upload?success=true";
        } catch (Exception e) {
            redirectAttributes.addFlashAttribute("error", "Failed to upload file: " + e.getMessage());
            return "redirect:/upload?error=true";
        }
    }
}

@Service
public class ContactService {
    
    private final JavaMailSender mailSender;
    private final TemplateEngine templateEngine;
    
    public ContactService(JavaMailSender mailSender, TemplateEngine templateEngine) {
        this.mailSender = mailSender;
        this.templateEngine = templateEngine;
    }
    
    public void sendContactEmail(String name, String email, String message) throws MessagingException {
        MimeMessage mimeMessage = mailSender.createMimeMessage();
        MimeMessageHelper helper = new MimeMessageHelper(mimeMessage, true);
        
        helper.setTo("contact@example.com");
        helper.setSubject("New Contact Form Submission");
        helper.setFrom("noreply@example.com");
        
        // Generate email content
        Context context = new Context();
        context.setVariable("name", name);
        context.setVariable("email", email);
        context.setVariable("message", message);
        context.setVariable("timestamp", LocalDateTime.now());
        
        String emailContent = templateEngine.process("contact-email", context);
        helper.setText(emailContent, true);
        
        mailSender.send(mimeMessage);
    }
}

@Service
public class FileUploadService {
    
    private final String uploadDirectory = "/uploads";
    
    public String uploadFile(MultipartFile file, String description) throws IOException {
        // Create upload directory if it doesn't exist
        Path uploadPath = Paths.get(uploadDirectory);
        if (!Files.exists(uploadPath)) {
            Files.createDirectories(uploadPath);
        }
        
        // Generate unique filename
        String originalFilename = file.getOriginalFilename();
        String fileExtension = originalFilename.substring(originalFilename.lastIndexOf("."));
        String filename = UUID.randomUUID().toString() + fileExtension;
        
        // Save file
        Path filePath = uploadPath.resolve(filename);
        Files.copy(file.getInputStream(), filePath, StandardCopyOption.REPLACE_EXISTING);
        
        // Save file metadata
        FileMetadata metadata = new FileMetadata();
        metadata.setFilename(filename);
        metadata.setOriginalFilename(originalFilename);
        metadata.setDescription(description);
        metadata.setUploadDate(LocalDateTime.now());
        metadata.setFileSize(file.getSize());
        
        fileMetadataRepository.save(metadata);
        
        return "/uploads/" + filename;
    }
}

@Service
public class ValidationService {
    
    public void validateContactForm(ContactForm form) {
        List<String> errors = new ArrayList<>();
        
        if (form.getName() == null || form.getName().trim().isEmpty()) {
            errors.add("Name is required");
        }
        
        if (form.getEmail() == null || !isValidEmail(form.getEmail())) {
            errors.add("Valid email is required");
        }
        
        if (form.getMessage() == null || form.getMessage().trim().isEmpty()) {
            errors.add("Message is required");
        }
        
        if (!errors.isEmpty()) {
            throw new ValidationException("Form validation failed", errors);
        }
    }
    
    public void validateFile(MultipartFile file) {
        List<String> errors = new ArrayList<>();
        
        if (file.isEmpty()) {
            errors.add("File is required");
        }
        
        if (file.getSize() > 10 * 1024 * 1024) { // 10MB limit
            errors.add("File size must be less than 10MB");
        }
        
        String contentType = file.getContentType();
        if (contentType == null || !isAllowedContentType(contentType)) {
            errors.add("File type not allowed");
        }
        
        if (!errors.isEmpty()) {
            throw new ValidationException("File validation failed", errors);
        }
    }
    
    private boolean isValidEmail(String email) {
        return email.matches("^[A-Za-z0-9+_.-]+@(.+)$");
    }
    
    private boolean isAllowedContentType(String contentType) {
        List<String> allowedTypes = Arrays.asList(
            "image/jpeg", "image/png", "image/gif",
            "application/pdf", "text/plain"
        );
        return allowedTypes.contains(contentType);
    }
}
```

## Web Caching

```tusk
# Web route with caching
#web {
    route: "/cached-page"
    cache: true
    cache_ttl: 300
} {
    data: @get_expensive_data()
    @render("cached-page.html", {data: data})
}

# Conditional caching
#web {
    route: "/conditional-cached"
    cache: @is_production()
    cache_ttl: 600
} {
    data: @get_data()
    @render("conditional-cached.html", {data: data})
}

# Cache invalidation
#web {
    route: "/cache-invalidate"
    method: "POST"
} {
    @invalidate_cache("cached-page")
    @redirect("/admin/cache-status")
}
```

## Java Web Caching

```java
import org.springframework.cache.annotation.Cacheable;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.stereotype.Service;

@Service
public class WebCachingService {
    
    private final CacheManager cacheManager;
    private final EnvironmentService environmentService;
    
    public WebCachingService(CacheManager cacheManager, EnvironmentService environmentService) {
        this.cacheManager = cacheManager;
        this.environmentService = environmentService;
    }
    
    @Cacheable(value = "web-pages", key = "#pageName")
    public String getCachedPage(String pageName, Map<String, Object> data) {
        // Expensive operation to generate page content
        return generatePageContent(pageName, data);
    }
    
    public String getConditionalCachedPage(String pageName, Map<String, Object> data) {
        if (environmentService.isProduction()) {
            return getCachedPage(pageName, data);
        } else {
            // No caching in development
            return generatePageContent(pageName, data);
        }
    }
    
    @CacheEvict(value = "web-pages", allEntries = true)
    public void invalidateAllPages() {
        // This method will clear all cached pages
    }
    
    @CacheEvict(value = "web-pages", key = "#pageName")
    public void invalidatePage(String pageName) {
        // This method will clear specific cached page
    }
    
    private String generatePageContent(String pageName, Map<String, Object> data) {
        // Simulate expensive operation
        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }
        
        return "Generated content for " + pageName;
    }
}

@Controller
public class CachedWebController {
    
    private final WebCachingService cachingService;
    private final DataService dataService;
    
    public CachedWebController(WebCachingService cachingService, DataService dataService) {
        this.cachingService = cachingService;
        this.dataService = dataService;
    }
    
    @GetMapping("/cached-page")
    public String cachedPage(Model model) {
        Map<String, Object> data = dataService.getExpensiveData();
        
        String content = cachingService.getCachedPage("cached-page", data);
        model.addAttribute("content", content);
        
        return "cached-page";
    }
    
    @GetMapping("/conditional-cached")
    public String conditionalCachedPage(Model model) {
        Map<String, Object> data = dataService.getData();
        
        String content = cachingService.getConditionalCachedPage("conditional-cached", data);
        model.addAttribute("content", content);
        
        return "conditional-cached";
    }
    
    @PostMapping("/cache-invalidate")
    public String invalidateCache(RedirectAttributes redirectAttributes) {
        cachingService.invalidateAllPages();
        redirectAttributes.addFlashAttribute("success", "Cache invalidated successfully");
        return "redirect:/admin/cache-status";
    }
}
```

## Web Testing

```java
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.context.TestPropertySource;

@WebMvcTest(WebController.class)
@TestPropertySource(properties = {
    "tusk.web.default-template=default",
    "tusk.web.default-layout=main"
})
public class WebControllerTest {
    
    @Autowired
    private MockMvc mockMvc;
    
    @MockBean
    private UserService userService;
    
    @MockBean
    private SearchService searchService;
    
    @Test
    public void testHomePage() throws Exception {
        User user = new User();
        user.setId(1L);
        user.setName("Test User");
        
        when(userService.getUserById(1L)).thenReturn(user);
        
        mockMvc.perform(get("/home"))
               .andExpect(status().isOk())
               .andExpect(view().name("home"))
               .andExpect(model().attribute("title", "Welcome"))
               .andExpect(model().attribute("user", user));
    }
    
    @Test
    public void testUserProfile() throws Exception {
        User user = new User();
        user.setId(1L);
        user.setName("Test User");
        
        when(userService.getUserById(1L)).thenReturn(user);
        
        mockMvc.perform(get("/user/1"))
               .andExpect(status().isOk())
               .andExpect(view().name("user"))
               .andExpect(model().attribute("user", user));
    }
    
    @Test
    public void testSearch() throws Exception {
        List<SearchResult> results = Arrays.asList(
            new SearchResult("Result 1"),
            new SearchResult("Result 2")
        );
        
        when(searchService.searchDatabase("test")).thenReturn(results);
        
        mockMvc.perform(get("/search").param("q", "test"))
               .andExpect(status().isOk())
               .andExpect(view().name("search"))
               .andExpect(model().attribute("query", "test"))
               .andExpect(model().attribute("results", results));
    }
    
    @Test
    public void testContactForm() throws Exception {
        mockMvc.perform(get("/contact"))
               .andExpect(status().isOk())
               .andExpect(view().name("contact"))
               .andExpect(model().attribute("title", "Contact Us"));
    }
    
    @Test
    public void testContactFormSubmission() throws Exception {
        mockMvc.perform(post("/contact")
               .param("name", "Test User")
               .param("email", "test@example.com")
               .param("message", "Test message"))
               .andExpect(status().is3xxRedirection())
               .andExpect(redirectedUrl("/contact?success=true"));
    }
}
```

## Configuration Properties

```yaml
# application.yml
tusk:
  web:
    default-template: "default"
    default-layout: "main"
    default-cache: false
    default-cache-ttl: 300
    
    routes:
      dashboard:
        route: "/dashboard"
        method: "GET"
        template: "dashboard.html"
        layout: "main.html"
        cache: true
        cache-ttl: 300
        middleware: ["auth"]
      
      admin:
        route: "/admin"
        method: "GET"
        template: "admin.html"
        middleware: ["auth", "admin_check"]
        condition: "user.hasRole('ADMIN')"
      
      premium:
        route: "/premium"
        method: "GET"
        template: "premium.html"
        condition: "user.isPremium()"
    
    global-middleware: ["logging", "auth"]
    
    templates:
      home: "home.html"
      dashboard: "dashboard.html"
      profile: "profile.html"
      contact: "contact.html"

spring:
  thymeleaf:
    cache: false
    prefix: "classpath:/templates/"
    suffix: ".html"
    encoding: "UTF-8"
    mode: "HTML"
  
  web:
    resources:
      static-locations: "classpath:/static/"
      cache:
        period: 3600
```

## Summary

The `#web` directive in TuskLang provides comprehensive web application capabilities for Java applications. With Spring Boot integration, flexible routing, template rendering, and caching support, you can implement sophisticated web applications that enhance your application's functionality.

Key features include:
- **Multiple web patterns**: Template rendering, dynamic routing, form handling
- **Spring Boot integration**: Seamless integration with Spring Boot web framework
- **Flexible configuration**: Configurable routes with middleware and conditions
- **Template rendering**: Support for multiple template engines and layouts
- **Form handling**: Comprehensive form processing and validation
- **Caching support**: Built-in caching capabilities with conditional caching
- **Testing support**: Comprehensive testing utilities

The Java implementation provides enterprise-grade web capabilities that integrate seamlessly with Spring Boot applications while maintaining the simplicity and power of TuskLang's declarative syntax. 