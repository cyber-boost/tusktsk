# Multi-stage build for Go SDK
FROM golang:1.21-alpine AS builder

# Set working directory
WORKDIR /app

# Install git and ca-certificates
RUN apk add --no-cache git ca-certificates

# Copy go mod files
COPY go.mod go.sum ./

# Download dependencies
RUN go mod download

# Copy source code
COPY . .

# Build the application
RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -o tusk-go ./cmd/tusk

# Production stage
FROM alpine:latest

# Install ca-certificates for HTTPS requests
RUN apk --no-cache add ca-certificates

# Create non-root user
RUN addgroup -g 1001 -S tusk && \
    adduser -u 1001 -S tusk -G tusk

# Set working directory
WORKDIR /app

# Copy binary from builder
COPY --from=builder /app/tusk-go .

# Set ownership
RUN chown tusk:tusk /app/tusk-go

# Switch to non-root user
USER tusk

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8000/health || exit 1

# Default command
CMD ["./tusk-go"] 