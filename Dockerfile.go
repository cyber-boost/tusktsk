# Go SDK Dockerfile
FROM golang:1.23-alpine AS builder

# Set environment variables
ENV CGO_ENABLED=0
ENV GOOS=linux
ENV GOARCH=amd64

# Install system dependencies
RUN apk add --no-cache \
    git \
    ca-certificates \
    tzdata

# Set working directory
WORKDIR /app

# Copy go mod files
COPY sdk/go/go.mod sdk/go/go.sum ./

# Download dependencies
RUN go mod download

# Copy source code
COPY sdk/go/ ./

# Build the application
RUN go build -a -installsuffix cgo -o tusktsk-go .

# Final stage
FROM alpine:latest

# Install ca-certificates for HTTPS requests
RUN apk --no-cache add ca-certificates tzdata

# Create non-root user
RUN addgroup -g 1001 -S tusktsk
RUN adduser -S tusktsk -u 1001

# Set working directory
WORKDIR /app

# Copy binary from builder stage
COPY --from=builder /app/tusktsk-go .

# Change ownership
RUN chown tusktsk:tusktsk /app/tusktsk-go

# Switch to non-root user
USER tusktsk

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD ./tusktsk-go version || exit 1

# Default command
CMD ["./tusktsk-go", "version"] 