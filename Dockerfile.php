# PHP SDK Dockerfile
FROM php:8.1-fpm-alpine

# Set environment variables
ENV PHP_MEMORY_LIMIT=512M
ENV PHP_MAX_EXECUTION_TIME=300

# Install system dependencies and PHP extensions
RUN apk add --no-cache \
    git \
    curl \
    zip \
    unzip \
    libzip-dev \
    oniguruma-dev \
    && docker-php-ext-install \
    pdo \
    pdo_mysql \
    json \
    mbstring \
    zip \
    && docker-php-ext-enable \
    pdo \
    pdo_mysql \
    json \
    mbstring \
    zip

# Install Composer
COPY --from=composer:latest /usr/bin/composer /usr/bin/composer

# Set working directory
WORKDIR /app

# Copy composer files
COPY sdk/php/composer.json sdk/php/composer.lock ./

# Install dependencies
RUN composer install --no-dev --optimize-autoloader --no-interaction

# Copy source code
COPY sdk/php/ ./

# Create non-root user
RUN addgroup -g 1001 -S tusktsk
RUN adduser -S tusktsk -u 1001
RUN chown -R tusktsk:tusktsk /app
USER tusktsk

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD php -r "echo 'TuskTsk PHP SDK is healthy';" || exit 1

# Default command
CMD ["php", "-r", "echo 'TuskTsk PHP SDK v2.0.2 ready';"] 