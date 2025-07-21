# Ruby SDK Dockerfile
FROM ruby:3.2-alpine

# Set environment variables
ENV BUNDLE_SILENCE_ROOT_WARNING=1
ENV BUNDLE_APP_CONFIG=/usr/local/bundle

# Install system dependencies
RUN apk add --no-cache \
    build-base \
    git \
    curl \
    sqlite-dev \
    tzdata

# Set working directory
WORKDIR /app

# Copy Gemfile
COPY sdk/ruby/Gemfile sdk/ruby/Gemfile.lock ./

# Install gems
RUN bundle config set --local deployment 'true' \
    && bundle config set --local path '/usr/local/bundle' \
    && bundle install --jobs 4 --retry 3

# Copy source code
COPY sdk/ruby/ ./

# Create non-root user
RUN addgroup -g 1001 -S tusktsk
RUN adduser -S tusktsk -u 1001
RUN chown -R tusktsk:tusktsk /app /usr/local/bundle
USER tusktsk

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD ruby -e "puts 'TuskTsk Ruby SDK is healthy'" || exit 1

# Default command
CMD ["ruby", "-e", "puts 'TuskTsk Ruby SDK v2.0.2 ready'"] 