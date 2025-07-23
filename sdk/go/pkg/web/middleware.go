package web

import (
	"fmt"
	"net/http"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/golang-jwt/jwt/v4"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/trace"
)

// corsMiddleware provides CORS support
func corsMiddleware() gin.HandlerFunc {
	return gin.HandlerFunc(func(c *gin.Context) {
		c.Header("Access-Control-Allow-Origin", "*")
		c.Header("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
		c.Header("Access-Control-Allow-Headers", "Origin, Content-Type, Content-Length, Accept-Encoding, X-CSRF-Token, Authorization")
		c.Header("Access-Control-Expose-Headers", "Content-Length")
		c.Header("Access-Control-Allow-Credentials", "true")

		if c.Request.Method == "OPTIONS" {
			c.AbortWithStatus(http.StatusNoContent)
			return
		}

		c.Next()
	})
}

// tracingMiddleware adds OpenTelemetry tracing
func tracingMiddleware() gin.HandlerFunc {
	return gin.HandlerFunc(func(c *gin.Context) {
		ctx := c.Request.Context()
		tracer := otel.Tracer("tusktsk-web")

		spanName := c.Request.Method + " " + c.Request.URL.Path
		ctx, span := tracer.Start(ctx, spanName,
			trace.WithAttributes(
				attribute.String("http.method", c.Request.Method),
				attribute.String("http.url", c.Request.URL.String()),
				attribute.String("http.user_agent", c.Request.UserAgent()),
				attribute.String("http.remote_addr", c.ClientIP()),
			),
		)
		defer span.End()

		c.Request = c.Request.WithContext(ctx)
		c.Next()

		span.SetAttributes(
			attribute.Int("http.status_code", c.Writer.Status()),
		)
	})
}

// rateLimitMiddleware provides rate limiting
func rateLimitMiddleware(limit int, window time.Duration) gin.HandlerFunc {
	limiter := NewRateLimiter(limit, window)
	
	return gin.HandlerFunc(func(c *gin.Context) {
		key := c.ClientIP()
		
		if !limiter.Allow(key) {
			c.JSON(http.StatusTooManyRequests, gin.H{
				"error": "Rate limit exceeded",
				"retry_after": window.Seconds(),
			})
			c.Abort()
			return
		}

		c.Header("X-RateLimit-Limit", string(rune(limit)))
		c.Header("X-RateLimit-Remaining", string(rune(limiter.Remaining(key))))
		c.Header("X-RateLimit-Reset", time.Now().Add(window).Format(time.RFC3339))

		c.Next()
	})
}

// authMiddleware provides JWT authentication
func authMiddleware(secret string) gin.HandlerFunc {
	return gin.HandlerFunc(func(c *gin.Context) {
		tokenString := c.GetHeader("Authorization")
		if tokenString == "" {
			c.JSON(http.StatusUnauthorized, gin.H{"error": "Authorization header required"})
			c.Abort()
			return
		}

		// Remove "Bearer " prefix if present
		if len(tokenString) > 7 && tokenString[:7] == "Bearer " {
			tokenString = tokenString[7:]
		}

		token, err := jwt.Parse(tokenString, func(token *jwt.Token) (interface{}, error) {
			return []byte(secret), nil
		})

		if err != nil || !token.Valid {
			c.JSON(http.StatusUnauthorized, gin.H{"error": "Invalid token"})
			c.Abort()
			return
		}

		if claims, ok := token.Claims.(jwt.MapClaims); ok {
			c.Set("user_id", claims["user_id"])
			c.Set("username", claims["username"])
			c.Set("roles", claims["roles"])
		}

		c.Next()
	})
}

// roleMiddleware checks for specific roles
func roleMiddleware(requiredRoles ...string) gin.HandlerFunc {
	return gin.HandlerFunc(func(c *gin.Context) {
		userRoles, exists := c.Get("roles")
		if !exists {
			c.JSON(http.StatusForbidden, gin.H{"error": "No roles found"})
			c.Abort()
			return
		}

		roles, ok := userRoles.([]interface{})
		if !ok {
			c.JSON(http.StatusForbidden, gin.H{"error": "Invalid roles format"})
			c.Abort()
			return
		}

		userRoleMap := make(map[string]bool)
		for _, role := range roles {
			if roleStr, ok := role.(string); ok {
				userRoleMap[roleStr] = true
			}
		}

		for _, requiredRole := range requiredRoles {
			if !userRoleMap[requiredRole] {
				c.JSON(http.StatusForbidden, gin.H{"error": "Insufficient permissions"})
				c.Abort()
				return
			}
		}

		c.Next()
	})
}

// loggingMiddleware provides detailed request logging
func loggingMiddleware() gin.HandlerFunc {
	return gin.HandlerFunc(func(c *gin.Context) {
		start := time.Now()
		path := c.Request.URL.Path
		raw := c.Request.URL.RawQuery

		c.Next()

		latency := time.Since(start)
		clientIP := c.ClientIP()
		method := c.Request.Method
		statusCode := c.Writer.Status()
		bodySize := c.Writer.Size()

		if raw != "" {
			path = path + "?" + raw
		}

		// Log request details
		if statusCode >= 400 {
			// Log errors
			c.Error(fmt.Errorf("HTTP %d %s %s %s %v %d", 
				statusCode, method, path, clientIP, latency, bodySize))
		} else {
			// Log successful requests
			fmt.Printf("HTTP %d %s %s %s %v %d\n", 
				statusCode, method, path, clientIP, latency, bodySize)
		}
	})
}

// compressionMiddleware provides response compression
func compressionMiddleware() gin.HandlerFunc {
	return gin.HandlerFunc(func(c *gin.Context) {
		// Check if client accepts gzip
		if c.GetHeader("Accept-Encoding") != "" {
			c.Header("Content-Encoding", "gzip")
			c.Header("Vary", "Accept-Encoding")
		}

		c.Next()
	})
}

// securityMiddleware adds security headers
func securityMiddleware() gin.HandlerFunc {
	return gin.HandlerFunc(func(c *gin.Context) {
		c.Header("X-Content-Type-Options", "nosniff")
		c.Header("X-Frame-Options", "DENY")
		c.Header("X-XSS-Protection", "1; mode=block")
		c.Header("Strict-Transport-Security", "max-age=31536000; includeSubDomains")
		c.Header("Content-Security-Policy", "default-src 'self'")

		c.Next()
	})
}

// cacheMiddleware provides response caching
func cacheMiddleware(duration time.Duration) gin.HandlerFunc {
	return gin.HandlerFunc(func(c *gin.Context) {
		if c.Request.Method == "GET" {
			c.Header("Cache-Control", "public, max-age="+string(rune(int(duration.Seconds()))))
			c.Header("Expires", time.Now().Add(duration).Format(time.RFC1123))
		}

		c.Next()
	})
}

// errorMiddleware provides centralized error handling
func errorMiddleware() gin.HandlerFunc {
	return gin.HandlerFunc(func(c *gin.Context) {
		c.Next()

		// Handle any errors that occurred
		if len(c.Errors) > 0 {
			err := c.Errors.Last()
			
			// Log the error
			fmt.Printf("Error: %v\n", err.Error())

			// Return appropriate error response
			c.JSON(http.StatusInternalServerError, gin.H{
				"error": "Internal server error",
				"message": err.Error(),
			})
		}
	})
} 