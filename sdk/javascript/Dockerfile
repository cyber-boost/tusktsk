FROM node:18-alpine
LABEL org.opencontainers.image.source=https://github.com/cyber-boost/tusktsk
LABEL org.opencontainers.image.description="TuskLang JavaScript SDK - Configuration with a Heartbeat"
LABEL org.opencontainers.image.licenses=MIT

# Set working directory
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci --only=production

# Copy source files
COPY . .

# Create global symlink
RUN npm link

# Set entry point
ENTRYPOINT ["tsk"]
CMD ["--help"] 