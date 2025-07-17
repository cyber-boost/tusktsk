# @ Request Object in TuskLang - Java Edition

**"We don't bow to any king" - Request Power with Java Integration**

The @ request object in TuskLang provides access to HTTP request data, headers, parameters, and body content. The Java SDK offers comprehensive request object handling with type safety and validation.

## 🎯 Java @ Request Object Integration

### @TuskConfig Request Object Support

```java
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.request.RequestObject;
import org.tusklang.java.request.RequestHandler;
import org.tusklang.java.annotations.TuskRequest;

@TuskConfig
public class RequestObjectConfig {
    
    // Request object access
    @TuskRequest("@request.method")
    private String requestMethod;
    
    @TuskRequest("@request.url")
    private String requestUrl;
    
    @TuskRequest("@request.path")
    private String requestPath;
    
    @TuskRequest("@request.query")
    private String requestQuery;
    
    @TuskRequest("@request.headers")
    private Map<String, String> requestHeaders;
    
    @TuskRequest("@request.body")
    private String requestBody;
    
    @TuskRequest("@request.params")
    private Map<String, String> requestParams;
    
    @TuskRequest("@request.cookies")
    private Map<String, String> requestCookies;
    
    @TuskRequest("@request.ip")
    private String clientIp;
    
    @TuskRequest("@request.user_agent")
    private String userAgent;
    
    @TuskRequest("@request.content_type")
    private String contentType;
    
    @TuskRequest("@request.content_length")
    private long contentLength;
    
    // Request handler
    private RequestHandler requestHandler;
    
    public RequestObjectConfig() {
        this.requestHandler = new RequestHandler();
    }
    
    // Get request object
    public RequestObject getRequestObject() {
        return requestHandler.getCurrentRequest();
    }
    
    // Validate request
    public boolean validateRequest() {
        List<String> errors = new ArrayList<>();
        
        if (requestMethod == null || requestMethod.isEmpty()) {
            errors.add("Request method is required");
        }
        
        if (requestUrl == null || requestUrl.isEmpty()) {
            errors.add("Request URL is required");
        }
        
        if (!errors.isEmpty()) {
            throw new RequestException("Request validation failed: " + String.join(", ", errors));
        }
        
        return true;
    }
    
    // Getters and setters
    public String getRequestMethod() { return requestMethod; }
    public void setRequestMethod(String requestMethod) { this.requestMethod = requestMethod; }
    
    public String getRequestUrl() { return requestUrl; }
    public void setRequestUrl(String requestUrl) { this.requestUrl = requestUrl; }
    
    public String getRequestPath() { return requestPath; }
    public void setRequestPath(String requestPath) { this.requestPath = requestPath; }
    
    public String getRequestQuery() { return requestQuery; }
    public void setRequestQuery(String requestQuery) { this.requestQuery = requestQuery; }
    
    public Map<String, String> getRequestHeaders() { return requestHeaders; }
    public void setRequestHeaders(Map<String, String> requestHeaders) { this.requestHeaders = requestHeaders; }
    
    public String getRequestBody() { return requestBody; }
    public void setRequestBody(String requestBody) { this.requestBody = requestBody; }
    
    public Map<String, String> getRequestParams() { return requestParams; }
    public void setRequestParams(Map<String, String> requestParams) { this.requestParams = requestParams; }
    
    public Map<String, String> getRequestCookies() { return requestCookies; }
    public void setRequestCookies(Map<String, String> requestCookies) { this.requestCookies = requestCookies; }
    
    public String getClientIp() { return clientIp; }
    public void setClientIp(String clientIp) { this.clientIp = clientIp; }
    
    public String getUserAgent() { return userAgent; }
    public void setUserAgent(String userAgent) { this.userAgent = userAgent; }
    
    public String getContentType() { return contentType; }
    public void setContentType(String contentType) { this.contentType = contentType; }
    
    public long getContentLength() { return contentLength; }
    public void setContentLength(long contentLength) { this.contentLength = contentLength; }
}

// Request object for Java integration
public class RequestObject {
    
    private String method;
    private String url;
    private String path;
    private String query;
    private Map<String, String> headers;
    private String body;
    private Map<String, String> params;
    private Map<String, String> cookies;
    private String clientIp;
    private String userAgent;
    private String contentType;
    private long contentLength;
    
    public RequestObject() {
        this.headers = new HashMap<>();
        this.params = new HashMap<>();
        this.cookies = new HashMap<>();
    }
    
    // Getters and setters
    public String getMethod() { return method; }
    public void setMethod(String method) { this.method = method; }
    
    public String getUrl() { return url; }
    public void setUrl(String url) { this.url = url; }
    
    public String getPath() { return path; }
    public void setPath(String path) { this.path = path; }
    
    public String getQuery() { return query; }
    public void setQuery(String query) { this.query = query; }
    
    public Map<String, String> getHeaders() { return headers; }
    public void setHeaders(Map<String, String> headers) { this.headers = headers; }
    
    public String getBody() { return body; }
    public void setBody(String body) { this.body = body; }
    
    public Map<String, String> getParams() { return params; }
    public void setParams(Map<String, String> params) { this.params = params; }
    
    public Map<String, String> getCookies() { return cookies; }
    public void setCookies(Map<String, String> cookies) { this.cookies = cookies; }
    
    public String getClientIp() { return clientIp; }
    public void setClientIp(String clientIp) { this.clientIp = clientIp; }
    
    public String getUserAgent() { return userAgent; }
    public void setUserAgent(String userAgent) { this.userAgent = userAgent; }
    
    public String getContentType() { return contentType; }
    public void setContentType(String contentType) { this.contentType = contentType; }
    
    public long getContentLength() { return contentLength; }
    public void setContentLength(long contentLength) { this.contentLength = contentLength; }
}

// Request handler
public class RequestHandler {
    
    private static final ThreadLocal<RequestObject> currentRequest = new ThreadLocal<>();
    
    public RequestObject getCurrentRequest() {
        return currentRequest.get();
    }
    
    public void setCurrentRequest(RequestObject request) {
        currentRequest.set(request);
    }
    
    public void clearCurrentRequest() {
        currentRequest.remove();
    }
}

// Request exception
public class RequestException extends RuntimeException {
    
    public RequestException(String message) {
        super(message);
    }
    
    public RequestException(String message, Throwable cause) {
        super(message, cause);
    }
}
```

### TuskLang @ Request Object Examples

```tusk
# @ Request object examples in TuskLang configuration

# Basic request object access
request_method: string = @request.method
request_url: string = @request.url
request_path: string = @request.path
request_query: string = @request.query

# Request headers
content_type: string = @request.headers["Content-Type"]
authorization: string = @request.headers["Authorization"]
user_agent: string = @request.headers["User-Agent"]
accept: string = @request.headers["Accept"]

# Request parameters
user_id: number = @request.params["user_id"]
page: number = @request.params["page"] || 1
limit: number = @request.params["limit"] || 10
search: string = @request.params["search"] || ""

# Request cookies
session_id: string = @request.cookies["session_id"]
auth_token: string = @request.cookies["auth_token"]
preferences: string = @request.cookies["preferences"]

# Request body
request_body: string = @request.body
parsed_body: object = @json.parse(@request.body)
form_data: object = @request.form_data

# Client information
client_ip: string = @request.ip
user_agent: string = @request.user_agent
content_length: number = @request.content_length

# Conditional request handling
is_api_request: boolean = @if(@request.path.startsWith("/api"), true, false)
is_authenticated: boolean = @if(@request.headers["Authorization"] != null, true, false)
is_json_request: boolean = @if(@request.headers["Content-Type"] == "application/json", true, false)

# Request validation
is_valid_method: boolean = @validate.method(@request.method, ["GET", "POST", "PUT", "DELETE"])
is_valid_content_type: boolean = @validate.content_type(@request.content_type)
is_valid_body_size: boolean = @validate.size(@request.content_length, 0, 1048576)

# Request processing
log_request: string = @log.info("Request: " + @request.method + " " + @request.url)
cache_key: string = @cache.key(@request.method + "_" + @request.path + "_" + @request.query)
rate_limit_key: string = @rate_limit.key(@request.ip + "_" + @request.path)
```

## 🎯 Best Practices

### Request Object Guidelines

1. **Validate request data** before processing
2. **Handle missing request fields** gracefully
3. **Sanitize request input** for security
4. **Log request information** for debugging
5. **Use request caching** for performance
6. **Implement rate limiting** based on request data
7. **Handle different content types** appropriately
8. **Validate request size** and limits
9. **Use request context** for user identification
10. **Implement proper error handling** for malformed requests

### Performance Considerations

```java
// Efficient request object handling
@TuskConfig
public class EfficientRequestHandling {
    
    // Cache request data
    private Map<String, Object> requestCache = new ConcurrentHashMap<>();
    
    // Get request data with caching
    public Object getRequestData(String key) {
        return requestCache.computeIfAbsent(key, 
            k -> extractRequestData(k));
    }
    
    // Clear request cache
    public void clearCache() {
        requestCache.clear();
    }
    
    // Extract request data
    private Object extractRequestData(String key) {
        // Implementation here
        return null;
    }
}
```

## 🚨 Troubleshooting

### Common Request Object Issues

1. **Missing request data**: Use default values and validation
2. **Malformed request body**: Implement proper parsing
3. **Security vulnerabilities**: Sanitize all input
4. **Performance issues**: Cache expensive operations
5. **Memory leaks**: Clear request context properly

### Debug Request Object Issues

```java
// Debug request object problems
public void debugRequestObject(RequestObject request) {
    System.out.println("Request details:");
    System.out.println("  Method: " + request.getMethod());
    System.out.println("  URL: " + request.getUrl());
    System.out.println("  Path: " + request.getPath());
    System.out.println("  Headers: " + request.getHeaders());
    System.out.println("  Params: " + request.getParams());
    System.out.println("  Body: " + request.getBody());
}
```

## 🎯 Next Steps

1. **Explore request object validation** and sanitization
2. **Learn about request caching** and performance optimization
3. **Master request security** and input validation
4. **Implement custom request processors** for your needs
5. **Optimize request handling** in your applications

---

**Ready to master request objects with Java power? Request objects are essential for building dynamic and responsive TuskLang applications. We don't bow to any king - especially not static request handling!** 