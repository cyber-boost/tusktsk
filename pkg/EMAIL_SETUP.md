# Email Setup for TuskLang Package Publishing

## Required Email Addresses

Based on the legal documents and package manager requirements, here are the emails you should create:

### 🔴 Critical Emails (Must Have)
1. **hello@tusklang.org** - Primary contact (formal)
2. **support@tusklang.org** - User support and help
3. **security@tusklang.org** - Security vulnerability reports

### 🟡 Recommended Emails
4. **hello@tusklang.org** - General inquiries, friendly contact
5. **dev@tusklang.org** - Developer relations, contributor questions
6. **noreply@tusklang.org** - Automated messages, notifications

### 🟢 Optional (Package-Specific)
7. **npm@tusklang.org** - NPM package management
8. **pypi@tusklang.org** - Python package management
9. **packages@tusklang.org** - General package inquiries

## Free Email Service Options

Since you mentioned TuskMail, here are options for custom @tusklang.org emails:

### 1. **Use TuskMail** (If it supports custom domains)
- If TuskMail can handle @tusklang.org addresses, use it!
- Set up email forwarding or aliases

### 2. **Free Custom Email Services**
- **Zoho Mail** - Free for up to 5 users with custom domain
  - Supports @tusklang.org
  - Professional interface
  - Good for business use
  
- **Yandex Connect** - Free custom domain email
  - Unlimited aliases
  - Good deliverability
  
- **ProtonMail** (with your own domain)
  - Privacy-focused
  - Requires paid plan for custom domains

### 3. **Email Forwarding Services** (Free)
- **ImprovMX** - Free email forwarding
  - Forward support@tusklang.org → your personal email
  - Send from Gmail/Outlook as support@tusklang.org
  
- **ForwardEmail.net** - Open source forwarding
  - Privacy-focused
  - Free tier available

### 4. **Domain Registrar Email**
- Many domain registrars offer free email forwarding
- Check if tusklang.org registrar provides this

## Quick Setup with ImprovMX (Recommended for Start)

1. Go to improvmx.com
2. Add tusklang.org domain
3. Set up forwards:
   ```
   support@tusklang.org → your-personal@email.com
   security@tusklang.org → your-personal@email.com
   hello@tusklang.org → your-personal@email.com
   ```
4. Verify DNS records
5. Configure Gmail/Outlook to send as these addresses

## GPG Key Setup

For Maven Central and signing:

```bash
# Generate GPG key
gpg --gen-key

# Use these details:
Real name: Bernard Stepgen Gengel II
Email: hello@tusklang.org
Comment: TuskLang Code Signing

# Export public key
gpg --armor --export hello@tusklang.org > tusklang-public.asc

# List keys to get key ID
gpg --list-keys
```

## Package Manager Account Setup

When creating accounts, use:
- **Primary Email**: hello@tusklang.org
- **Organization**: CyberBoost LLC
- **Display Name**: Bernard Stepgen Gengel II
- **Website**: https://tusklang.org

Enable 2FA on all accounts for security!