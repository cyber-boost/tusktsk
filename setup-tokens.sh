#!/bin/bash
# Secure Token Setup Script for TuskLang Package Deployment
# This script helps set up all required tokens for package managers

echo "🔐 TuskLang Package Manager Token Setup"
echo "========================================"
echo ""
echo "This script will help you set up all required tokens for deploying"
echo "TuskLang SDKs to various package managers."
echo ""
echo "⚠️  IMPORTANT: Never share tokens in chat or commit them to code!"
echo ""

# Function to safely add token to bashrc
add_token_to_bashrc() {
    local token_name="$1"
    local token_value="$2"
    local bashrc_file="$HOME/.bashrc"
    
    # Check if token already exists
    if grep -q "^export $token_name=" "$bashrc_file" 2>/dev/null; then
        echo "🔄 Updating existing $token_name..."
        # Remove existing line
        sed -i "/^export $token_name=/d" "$bashrc_file"
    fi
    
    # Add new token
    echo "export $token_name=\"$token_value\"" >> "$bashrc_file"
    echo "✅ $token_name added to ~/.bashrc"
}

# Function to prompt for token
prompt_for_token() {
    local token_name="$1"
    local description="$2"
    local current_value="${!token_name}"
    
    echo ""
    echo "📦 $description"
    echo "Token name: $token_name"
    
    if [ -n "$current_value" ]; then
        echo "Current value: ${current_value:0:8}... (already set)"
        read -p "Keep current value? (y/n): " keep_current
        if [[ $keep_current =~ ^[Yy]$ ]]; then
            return 0
        fi
    fi
    
    read -s -p "Enter your $token_name: " token_value
    echo ""
    
    if [ -n "$token_value" ]; then
        add_token_to_bashrc "$token_name" "$token_value"
        export "$token_name"="$token_value"
        return 0
    else
        echo "❌ No token provided, skipping..."
        return 1
    fi
}

echo "🚀 Starting token setup..."
echo ""

# Prompt for each token
prompt_for_token "NPM_TOKEN" "NPM Registry (JavaScript packages)"
prompt_for_token "TWINE_PASSWORD" "PyPI (Python packages)"
prompt_for_token "RUBYGEMS_API_KEY" "RubyGems (Ruby packages)"
prompt_for_token "CARGO_REGISTRY_TOKEN" "crates.io (Rust packages)"
prompt_for_token "NUGET_API_KEY" "NuGet (C# packages)"
prompt_for_token "PACKAGIST_TOKEN" "Packagist (PHP packages)"
prompt_for_token "GITHUB_TOKEN" "GitHub Packages (all packages)"

echo ""
echo "🔄 Reloading environment..."
source ~/.bashrc

echo ""
echo "✅ Token setup complete!"
echo ""
echo "🔍 Verifying tokens..."
echo "NPM_TOKEN: ${NPM_TOKEN:+✅ SET}"
echo "TWINE_PASSWORD: ${TWINE_PASSWORD:+✅ SET}"
echo "RUBYGEMS_API_KEY: ${RUBYGEMS_API_KEY:+✅ SET}"
echo "CARGO_REGISTRY_TOKEN: ${CARGO_REGISTRY_TOKEN:+✅ SET}"
echo "NUGET_API_KEY: ${NUGET_API_KEY:+✅ SET}"
echo "PACKAGIST_TOKEN: ${PACKAGIST_TOKEN:+✅ SET}"
echo "GITHUB_TOKEN: ${GITHUB_TOKEN:+✅ SET}"

echo ""
echo "🎯 Next steps:"
echo "1. Run: ./deploy_v2/scripts/deploy-all.sh"
echo "2. Monitor deployment progress"
echo "3. Check package manager registries for published packages"
echo ""
echo "🔒 Security reminder:"
echo "- Tokens are stored in ~/.bashrc"
echo "- Keep your ~/.bashrc file secure"
echo "- Never commit tokens to version control"
echo "- Consider using a credential manager for production" 