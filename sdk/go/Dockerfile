FROM golang:1.23-alpine AS builder
LABEL org.opencontainers.image.source=https://github.com/cyber-boost/tusktsk
LABEL org.opencontainers.image.description="TuskLang Go SDK - Configuration with a Heartbeat"
LABEL org.opencontainers.image.licenses=MIT

WORKDIR /app
COPY . .
RUN go mod download
RUN go mod tidy
RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -o tusktsk .

FROM alpine:latest
RUN apk --no-cache add ca-certificates tzdata
WORKDIR /root/

COPY --from=builder /app/tusktsk .
RUN ln -s /root/tusktsk /usr/local/bin/tsk

# Create non-root user for security
RUN addgroup -g 1001 -S tusktsk && \
    adduser -u 1001 -S tusktsk -G tusktsk
USER tusktsk

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD tusktsk --version || exit 1

ENTRYPOINT ["./tusktsk"]
CMD ["--help"] 