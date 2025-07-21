# GitHub Token Setup for TuskLang SDK Deployment

## Overview
This guide explains how to set up GitHub tokens for automated SDK deployment and release creation.

## Step 1: Create GitHub Personal Access Token

1. Go to GitHub.com and sign in
2. Click your profile picture → Settings
3. Scroll down to "Developer settings" (bottom left)
4. Click "Personal access tokens" → "Tokens (classic)"
5. Click "Generate new token" → "Generate new token (classic)"

## Step 2: Configure Token Permissions

Select the following scopes:
- ✅ `repo` (Full control of private repositories)
- ✅ `workflow` (Update GitHub Action workflows)
- ✅ `write:packages` (Upload packages to GitHub Package Registry)
- ✅ `delete:packages` (Delete packages from GitHub Package Registry)

## Step 3: Generate and Save Token

1. Click "Generate token"
2. **IMPORTANT:** Copy the token immediately (you won't see it again)
3. Save it securely (password manager recommended)

## Step 4: Configure Local Git

### Option A: Store in Git Credential Manager
```bash
git config --global credential.helper store
# Next git push will prompt for username and token
```

### Option B: Set Environment Variable
```bash
export GITHUB_TOKEN="your_github_token_here"
# Add to ~/.bashrc or ~/.zshrc for persistence
```

### Option C: Use GitHub CLI
```bash
gh auth login
# Follow prompts and use token when asked
```

## Step 5: Test Configuration

```bash
# Test with a simple push
echo "test" >> README.md
git add README.md
git commit -m "test: GitHub token configuration"
git push origin main
```

## Step 6: Automated Deployment

Once configured, use the deployment script:
```bash
./gd.sh --deploy-all
```

This will:
1. Commit all SDK changes
2. Create GitHub releases
3. Upload packages
4. Update documentation

## Troubleshooting

### "Authentication failed"
- Check token permissions
- Verify token is not expired
- Ensure correct username/token combination

### "Permission denied"
- Token needs `repo` scope for private repos
- Check repository access permissions

### "Package upload failed"
- Token needs `write:packages` scope
- Check package registry permissions

## Security Notes

- Never commit tokens to version control
- Use environment variables or credential managers
- Rotate tokens regularly
- Use minimal required permissions
- Monitor token usage in GitHub settings

## Next Steps

After setting up the token:
1. Run `./gd.sh --test-token` to verify
2. Run `./gd.sh --deploy-all` for full deployment
3. Monitor releases on GitHub.com
