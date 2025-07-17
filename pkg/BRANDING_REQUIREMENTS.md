# TuskLang Package Publishing - Required Information

To publish TuskLang across all package managers professionally, we need the following information:

## 🏢 Company/Organization Information
- [ ] **LLC Name**: (Your registered LLC name)
- [ ] **DBA/Trade Name**: (If different from LLC)
- [ ] **Business License Number**: 
- [ ] **Tax ID/EIN**: (Some registries require this)

## 📧 Contact Information
- [ ] **Official Email**: (e.g., support@tusklang.org)
- [ ] **Developer Email**: (for package manager accounts)
- [ ] **Support Email**: (can be same as official)
- [ ] **Security Email**: (for vulnerability reports)

## 🌐 Web Presence
- [ ] **Official Website**: tusklang.org (confirm)
- [ ] **Documentation URL**: docs.tusklang.org
- [ ] **GitHub Organization**: github.com/tuskphp (confirm)
- [ ] **Support/Issues URL**: 
- [ ] **Community Forum/Discord**: 

## 📄 Legal Documents
- [ ] **License Type**: (MIT, Apache 2.0, etc.)
- [ ] **Copyright Holder**: (Your name or LLC name)
- [ ] **Copyright Year**: (When project started)
- [ ] **Terms of Service URL**: 
- [ ] **Privacy Policy URL**: 

## 🔐 Security & Signing
- [ ] **Code Signing Certificate**: (Required for some platforms)
- [ ] **GPG Key**: (Required for Maven Central)
  - Name on key
  - Email on key
- [ ] **Domain Verification**: (For package namespaces)

## 📦 Package-Specific Requirements

### NPM (JavaScript)
- Organization scope: @tusklang or unscoped?
- Package author name
- Funding URL (optional)

### PyPI (Python)
- Author name
- Author email
- Project URLs (homepage, documentation, etc.)

### Maven Central (Java)
- Group ID: org.tusklang (needs domain control)
- Developer ID
- Developer name
- Organization name
- Organization URL

### NuGet (C#)
- Package authors
- Package owners
- Icon URL (optional but recommended)

### RubyGems
- Authors (array of names)
- Email addresses
- Homepage URL

### Crates.io (Rust)
- Authors list
- Repository URL
- Categories/keywords

## 🎨 Branding Assets
- [ ] **Logo**: (URL to hosted logo, preferably PNG/SVG)
- [ ] **Icon**: (Square version for package managers)
- [ ] **Brand Colors**: (Hex codes)
- [ ] **Tagline/Description**: (One-liner about TuskLang)

## 📝 Descriptions
- [ ] **Short Description** (< 100 chars): 
- [ ] **Long Description** (< 500 chars):
- [ ] **Keywords/Tags**: (comma-separated)

## Example Format:
```yaml
company:
  legal_name: "Your LLC Name"
  dba: "TuskLang"
  ein: "XX-XXXXXXX"

contact:
  official_email: "hello@tusklang.org"
  support_email: "support@tusklang.org"
  security_email: "security@tusklang.org"

copyright:
  holder: "Your Name or LLC"
  year: "2024"
  license: "MIT"

description:
  short: "Next-generation configuration language with built-in intelligence"
  long: "TuskLang is a powerful configuration language that combines..."
  keywords: "configuration, DSL, automation, devops, infrastructure"

branding:
  logo_url: "https://tusklang.org/assets/logo.png"
  primary_color: "#FF6B6B"
  secondary_color: "#4ECDC4"
```

## Notes:
- Some package managers require email verification
- Maven Central requires domain ownership proof
- Several registries need 2FA enabled
- Company information adds legitimacy
- Consistent branding across all platforms is important