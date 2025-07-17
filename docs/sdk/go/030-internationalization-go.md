# Internationalization (i18n) in TuskLang - Go Guide

## 🌍 **Global by Default: TuskLang i18n**

TuskLang doesn’t bow to any king—or any single language. This guide shows you how to build Go apps with world-class internationalization using TuskLang’s flexible config system.

## 📋 **Table of Contents**
- [Multi-Language Config](#multi-language-config)
- [Locale Switching](#locale-switching)
- [Go Integration](#go-integration)
- [i18n Patterns](#i18n-patterns)
- [Best Practices](#best-practices)

## 🗺️ **Multi-Language Config**

### **TuskLang Example**

```go
// TuskLang - Multi-language config
[labels]
welcome[en]: "Welcome!"
welcome[fr]: "Bienvenue!"
welcome[es]: "¡Bienvenido!"
```

```go
// Go - Accessing localized values
lang := "fr"
welcome := config.GetString(fmt.Sprintf("welcome[%s]", lang))
```

### **Section-Based Locales**

```go
// TuskLang - Section-based locales
[en]
submit: "Submit"
[fr]
submit: "Soumettre"
```

```go
// Go - Section-based locale access
section := "fr"
submit := config.Section(section).GetString("submit")
```

## 🔄 **Locale Switching**

### **Dynamic Locale Selection**

```go
// Go - Dynamic locale switching
func GetLabel(config *tusklang.Config, key, lang string) string {
    val, err := config.GetString(fmt.Sprintf("%s[%s]", key, lang))
    if err == nil {
        return val
    }
    // Fallback to English
    val, _ = config.GetString(fmt.Sprintf("%s[en]", key))
    return val
}
```

### **Environment-Based Locale**

```go
// TuskLang - Env-based locale
[settings]
default_lang: @env("LANG", "en")
```

```go
// Go - Use env for locale
lang := os.Getenv("LANG")
if lang == "" { lang = "en" }
welcome := config.GetString(fmt.Sprintf("welcome[%s]", lang))
```

## 🔗 **Go Integration**

### **i18n Structs**

```go
type Labels struct {
    Welcome map[string]string `tsk:"welcome"`
}

var labels Labels
_ = tusklang.UnmarshalFile("labels.tsk", &labels)
fmt.Println(labels.Welcome["es"]) // ¡Bienvenido!
```

### **Locale Middleware**

```go
// Go - HTTP middleware for locale
type LocaleMiddleware struct {
    config *tusklang.Config
}

func (m *LocaleMiddleware) ServeHTTP(w http.ResponseWriter, r *http.Request, next http.HandlerFunc) {
    lang := r.URL.Query().Get("lang")
    if lang == "" {
        lang = "en"
    }
    ctx := context.WithValue(r.Context(), "lang", lang)
    next(w, r.WithContext(ctx))
}
```

## 🧩 **i18n Patterns**

- Use `[key][lang]` for inline translations
- Use `[section]` for block translations
- Fallback to English if locale missing
- Store all translations in a single config for easy updates

## 🥇 **Best Practices**

- Always provide an English fallback
- Validate all keys exist for each supported language
- Use environment variables for default locale
- Document all supported locales in your config

---

**TuskLang: One config. Every language. No borders.** 