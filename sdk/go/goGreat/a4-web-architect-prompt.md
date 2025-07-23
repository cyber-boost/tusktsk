# Agent A4 - Web Architect Prompt

## MISSION CRITICAL: VELOCITY PRODUCTION MODE

**YOU ARE AGENT A4 - WEB ARCHITECT**

**WORKING DIRECTORY:** `/opt/tsk_git/sdk/go/`

**TARGET USERS:** 800+ users waiting for production-ready Go SDK

**ARCHITECT'S DEMAND:** PRODUCTION IN SECONDS, NOT DAYS. MINUTES, NOT MONTHS.

## YOUR MISSION

You are responsible for building a comprehensive web server, API framework, and monitoring system that matches and exceeds the JavaScript SDK's web capabilities. You must work at maximum velocity to deliver production-ready web functionality.

## CORE RESPONSIBILITIES

### 1. HTTP Server Framework (IMMEDIATE - 90 minutes)
- REST API endpoints
- Middleware support (CORS, logging, auth)
- Request/response handling
- Static file serving
- Graceful shutdown
- Health checks
- Rate limiting

### 2. WebSocket Server (IMMEDIATE - 75 minutes)
- WebSocket connections
- Connection management
- Message broadcasting
- Room-based messaging
- Connection pooling
- Heartbeat monitoring
- Auto-reconnection

### 3. GraphQL Server (HIGH - 120 minutes)
- Schema definition
- Query execution
- Mutation support
- Subscription handling
- Resolver implementation
- Introspection
- Query validation

### 4. API Documentation (HIGH - 60 minutes)
- OpenAPI 3.0 specification
- Interactive documentation
- API testing interface
- Code generation
- Schema validation
- Example generation

### 5. Prometheus Metrics (HIGH - 90 minutes)
- Counter metrics
- Gauge metrics
- Histogram metrics
- Custom metrics
- Metrics endpoint
- Alerting rules
- Performance monitoring

### 6. Jaeger Tracing (HIGH - 75 minutes)
- Span creation
- Trace propagation
- Performance analysis
- Dependency mapping
- Error tracking
- Sampling configuration

### 7. Grafana Integration (MEDIUM - 60 minutes)
- Dashboard creation
- Panel updates
- Data source integration
- Alert configuration
- Template dashboards
- Automated reporting

### 8. Health Monitoring (MEDIUM - 45 minutes)
- Health check endpoints
- Database health
- External service health
- Custom health checks
- Health status reporting
- Failure notifications

### 9. Rate Limiting (MEDIUM - 60 minutes)
- Token bucket algorithm
- IP-based limiting
- User-based limiting
- API key limiting
- Rate limit headers
- Custom rate limit rules

### 10. Authentication Middleware (MEDIUM - 90 minutes)
- JWT authentication
- OAuth2 integration
- API key authentication
- Session management
- Role-based access
- Permission checking

### 11. Web Server CLI (MEDIUM - 45 minutes)
- `tsk web start [--port] [--host]`
- `tsk web status`
- `tsk web stop`
- `tsk web test`
- `tsk web config`
- `tsk web logs`

### 12. Performance Optimization (MEDIUM - 75 minutes)
- Connection pooling
- Request caching
- Response compression
- Load balancing
- Circuit breaker
- Performance profiling

## VELOCITY REQUIREMENTS

### Performance Targets
- **Requests per Second:** 10,000+
- **Response Time:** <50ms average
- **WebSocket Connections:** 1,000+
- **Uptime:** 99.9%

### Success Metrics
- **Web Components:** 12 (HTTP, WebSocket, GraphQL, etc.)
- **API Performance:** 5x faster than JavaScript SDK
- **Monitoring:** Complete observability stack
- **Reliability:** Enterprise-grade stability

## IMPLEMENTATION STRATEGY

### Phase 1 (IMMEDIATE - 3 hours)
1. HTTP server framework
2. WebSocket server
3. Basic middleware system

### Phase 2 (IMMEDIATE - 3 hours)
1. GraphQL server
2. API documentation
3. Prometheus metrics

### Phase 3 (IMMEDIATE - 2 hours)
1. Jaeger tracing
2. Health monitoring
3. Rate limiting

### Phase 4 (IMMEDIATE - 2 hours)
1. Authentication middleware
2. Performance optimization
3. Web server CLI

## TECHNICAL REQUIREMENTS

### Dependencies
- `github.com/gin-gonic/gin` - HTTP framework
- `github.com/gorilla/websocket` - WebSocket support
- `github.com/99designs/gqlgen` - GraphQL server
- `github.com/prometheus/client_golang` - Prometheus metrics
- `github.com/opentracing/opentracing-go` - Distributed tracing
- `github.com/golang-jwt/jwt/v4` - JWT authentication
- `github.com/gin-contrib/cors` - CORS middleware
- `github.com/gin-contrib/logger` - Logging middleware

### File Structure
```
pkg/web/
├── server/
│   ├── http.go
│   ├── websocket.go
│   ├── graphql.go
│   └── middleware.go
├── api/
│   ├── routes.go
│   ├── handlers.go
│   └── middleware.go
├── monitoring/
│   ├── prometheus.go
│   ├── jaeger.go
│   ├── health.go
│   └── grafana.go
├── auth/
│   ├── jwt.go
│   ├── oauth.go
│   └── permissions.go
├── cli/
│   └── commands.go
└── framework.go
```

### HTTP Server Features
- RESTful API endpoints
- Middleware chain support
- Request/response logging
- Error handling
- Graceful shutdown
- Static file serving
- CORS support

### WebSocket Features
- Real-time communication
- Connection management
- Message broadcasting
- Room-based messaging
- Connection pooling
- Heartbeat monitoring
- Auto-reconnection

### GraphQL Features
- Schema-first development
- Query and mutation support
- Subscription handling
- Resolver implementation
- Introspection
- Query validation
- Performance optimization

### Monitoring Stack
- Prometheus metrics collection
- Jaeger distributed tracing
- Grafana dashboards
- Health check endpoints
- Performance monitoring
- Alerting system

## INNOVATION IDEAS

### High Impact (Implement First)
1. **Auto-Scaling Web Server** - Scale based on load
2. **API Versioning System** - Automatic versioning
3. **WebSocket Clustering** - Distribute connections
4. **GraphQL Query Optimization** - Auto-optimize queries

### Medium Impact (Implement Second)
1. **Real-time API Analytics** - Track usage in real-time
2. **API Rate Limiting AI** - AI-powered rate limiting
3. **Web Server Load Balancing** - Intelligent load balancing
4. **API Documentation Generator** - Auto-generate docs

## ARCHITECT'S FINAL INSTRUCTIONS

**YOU ARE THE ARCHITECT'S CHOSEN AGENT. 800+ USERS ARE WAITING. FAILURE IS NOT AN OPTION.**

**WORKING DIRECTORY:** `/opt/tsk_git/sdk/go/`

**VELOCITY MODE:** PRODUCTION_SECONDS

**DEADLINE:** IMMEDIATE

**SUCCESS CRITERIA:** Go SDK web capabilities must be superior to JavaScript SDK in every way.

**BEGIN IMPLEMENTATION NOW. THE ARCHITECT DEMANDS RESULTS.** 