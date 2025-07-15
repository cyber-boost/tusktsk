# TuskLang Go SDK Makefile

.PHONY: build clean test install uninstall help

# Build variables
BINARY_NAME=tsk
BUILD_DIR=build
VERSION=2.0.0
COMMIT=$(shell git rev-parse --short HEAD 2>/dev/null || echo "unknown")
BUILD_TIME=$(shell date -u '+%Y-%m-%d_%H:%M:%S')

# Go build flags
LDFLAGS=-ldflags "-X main.Version=${VERSION} -X main.Commit=${COMMIT} -X main.BuildTime=${BUILD_TIME}"

# Default target
all: build

# Build the CLI
build:
	@echo "🔨 Building TuskLang CLI..."
	@mkdir -p ${BUILD_DIR}
	go build ${LDFLAGS} -o ${BUILD_DIR}/${BINARY_NAME} ./cmd/tsk
	@echo "✅ Built ${BUILD_DIR}/${BINARY_NAME}"

# Build for multiple platforms
build-all: build-linux build-darwin build-windows

build-linux:
	@echo "🔨 Building for Linux..."
	@mkdir -p ${BUILD_DIR}
	GOOS=linux GOARCH=amd64 go build ${LDFLAGS} -o ${BUILD_DIR}/${BINARY_NAME}-linux-amd64 ./cmd/tsk

build-darwin:
	@echo "🔨 Building for macOS..."
	@mkdir -p ${BUILD_DIR}
	GOOS=darwin GOARCH=amd64 go build ${LDFLAGS} -o ${BUILD_DIR}/${BINARY_NAME}-darwin-amd64 ./cmd/tsk

build-windows:
	@echo "🔨 Building for Windows..."
	@mkdir -p ${BUILD_DIR}
	GOOS=windows GOARCH=amd64 go build ${LDFLAGS} -o ${BUILD_DIR}/${BINARY_NAME}-windows-amd64.exe ./cmd/tsk

# Install the CLI globally
install: build
	@echo "📦 Installing TuskLang CLI..."
	@sudo cp ${BUILD_DIR}/${BINARY_NAME} /usr/local/bin/
	@echo "✅ TuskLang CLI installed to /usr/local/bin/${BINARY_NAME}"

# Uninstall the CLI
uninstall:
	@echo "🗑️ Uninstalling TuskLang CLI..."
	@sudo rm -f /usr/local/bin/${BINARY_NAME}
	@echo "✅ TuskLang CLI uninstalled"

# Run tests
test:
	@echo "🧪 Running tests..."
	go test -v ./...

# Run tests with coverage
test-coverage:
	@echo "🧪 Running tests with coverage..."
	go test -v -coverprofile=coverage.out ./...
	go tool cover -html=coverage.out -o coverage.html
	@echo "📊 Coverage report generated: coverage.html"

# Clean build artifacts
clean:
	@echo "🧹 Cleaning build artifacts..."
	@rm -rf ${BUILD_DIR}
	@rm -f coverage.out coverage.html
	@echo "✅ Cleaned build artifacts"

# Format code
fmt:
	@echo "🎨 Formatting code..."
	go fmt ./...

# Lint code
lint:
	@echo "🔍 Linting code..."
	golangci-lint run

# Update dependencies
deps:
	@echo "📦 Updating dependencies..."
	go mod tidy
	go mod download

# Run the CLI
run: build
	@echo "▶️ Running TuskLang CLI..."
	./${BUILD_DIR}/${BINARY_NAME}

# Run the CLI with arguments
run-args: build
	@echo "▶️ Running TuskLang CLI with arguments..."
	./${BUILD_DIR}/${BINARY_NAME} $(ARGS)

# Build and run tests
test-build: build
	@echo "🧪 Running CLI tests..."
	./${BUILD_DIR}/${BINARY_NAME} --help
	./${BUILD_DIR}/${BINARY_NAME} version
	./${BUILD_DIR}/${BINARY_NAME} parse testdata/complex.tsk
	./${BUILD_DIR}/${BINARY_NAME} validate testdata/complex.tsk

# Create release package
release: build-all
	@echo "📦 Creating release package..."
	@mkdir -p release
	@tar -czf release/tusklang-go-${VERSION}-linux-amd64.tar.gz -C ${BUILD_DIR} ${BINARY_NAME}-linux-amd64
	@tar -czf release/tusklang-go-${VERSION}-darwin-amd64.tar.gz -C ${BUILD_DIR} ${BINARY_NAME}-darwin-amd64
	@zip -j release/tusklang-go-${VERSION}-windows-amd64.zip ${BUILD_DIR}/${BINARY_NAME}-windows-amd64.exe
	@echo "✅ Release packages created in release/"

# Show help
help:
	@echo "🐘 TuskLang Go SDK Makefile"
	@echo ""
	@echo "Available targets:"
	@echo "  build          - Build the CLI"
	@echo "  build-all      - Build for all platforms"
	@echo "  install        - Install CLI globally"
	@echo "  uninstall      - Uninstall CLI"
	@echo "  test           - Run tests"
	@echo "  test-coverage  - Run tests with coverage"
	@echo "  clean          - Clean build artifacts"
	@echo "  fmt            - Format code"
	@echo "  lint           - Lint code"
	@echo "  deps           - Update dependencies"
	@echo "  run            - Run the CLI"
	@echo "  run-args       - Run CLI with arguments (ARGS=...)"
	@echo "  test-build     - Build and test CLI"
	@echo "  release        - Create release packages"
	@echo "  help           - Show this help"
	@echo ""
	@echo "Examples:"
	@echo "  make run-args ARGS=\"parse testdata/complex.tsk\""
	@echo "  make run-args ARGS=\"config get database.host\"" 