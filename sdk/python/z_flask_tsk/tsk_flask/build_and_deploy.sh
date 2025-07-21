#!/bin/bash

# Flask-TSK Build and Deploy Script
# Revolutionary Flask Extension for TuskLang Integration

set -e

echo "🚀 Flask-TSK Build and Deploy Script"
echo "====================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if we're in the right directory
if [ ! -f "setup.py" ] || [ ! -f "tsk_flask/__init__.py" ]; then
    print_error "Must be run from the flask-tsk package directory"
    exit 1
fi

# Check Python version
PYTHON_VERSION=$(python3 --version 2>&1 | cut -d' ' -f2 | cut -d'.' -f1,2)
print_status "Python version: $PYTHON_VERSION"

if [[ $(echo "$PYTHON_VERSION >= 3.8" | bc -l) -eq 0 ]]; then
    print_error "Python 3.8 or higher is required"
    exit 1
fi

# Clean previous builds
print_status "Cleaning previous builds..."
rm -rf build/ dist/ *.egg-info/ __pycache__/ tsk_flask/__pycache__/
find . -name "*.pyc" -delete
find . -name "__pycache__" -type d -exec rm -rf {} + 2>/dev/null || true

# Install build dependencies
print_status "Installing build dependencies..."
pip install --upgrade pip setuptools wheel twine

# Run tests before building
print_status "Running tests..."
if [ -f "test_integration.py" ]; then
    python3 test_integration.py
    if [ $? -ne 0 ]; then
        print_error "Tests failed! Aborting build."
        exit 1
    fi
    print_success "Tests passed!"
else
    print_warning "No test file found, skipping tests"
fi

# Build the package
print_status "Building Flask-TSK package..."
python3 setup.py sdist bdist_wheel

# Check the built package
print_status "Checking built package..."
twine check dist/*

# Show package info
print_status "Package information:"
ls -la dist/
echo ""

# Ask for deployment confirmation
echo "====================================="
echo "🚀 Flask-TSK Package Ready for Deployment!"
echo ""
echo "Package files created:"
ls -la dist/
echo ""
echo "Next steps:"
echo "1. Test installation: pip install dist/flask-tsk-*.tar.gz"
echo "2. Upload to PyPI: twine upload dist/*"
echo "3. Upload to TestPyPI: twine upload --repository testpypi dist/*"
echo ""

read -p "Do you want to test the package installation? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    print_status "Testing package installation..."
    pip install dist/flask-tsk-*.tar.gz
    if [ $? -eq 0 ]; then
        print_success "Package installation test successful!"
        
        # Test import
        python3 -c "from tsk_flask import FlaskTSK; print('✅ Flask-TSK import successful!')"
        if [ $? -eq 0 ]; then
            print_success "Flask-TSK import test successful!"
        else
            print_error "Flask-TSK import test failed!"
            exit 1
        fi
    else
        print_error "Package installation test failed!"
        exit 1
    fi
fi

echo ""
echo "🎉 Flask-TSK Build Complete!"
echo ""
echo "To deploy to PyPI:"
echo "  twine upload dist/*"
echo ""
echo "To deploy to TestPyPI first:"
echo "  twine upload --repository testpypi dist/*"
echo ""
echo "Package ready for: pip install flask-tsk" 