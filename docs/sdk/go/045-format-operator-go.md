# @format Operator in TuskLang - Go Guide

## 🎨 **Format Power: @format Operator Unleashed**

TuskLang's `@format` operator is your presentation rebellion. We don't bow to any king—especially not to ugly, unformatted data. Here's how to use `@format` in Go projects to create beautiful, consistent, and professional data presentation.

## 📋 **Table of Contents**
- [What is @format?](#what-is-format)
- [Basic Usage](#basic-usage)
- [Format Types](#format-types)
- [Template Formatting](#template-formatting)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🎨 **What is @format?**

The `@format` operator formats data according to specified patterns and templates. No more raw data—just pure, beautiful formatting.

## 🛠️ **Basic Usage**

```go
[formatting]
currency: @format.currency(1234.56, "USD")
date: @format.date(@date.now(), "2006-01-02")
phone: @format.phone("1234567890")
email_template: @format.template("Hello {{name}}, your order {{order_id}} is ready!")
```

## 🔧 **Format Types**

### **Numeric Formatting**
```go
[numbers]
currency_usd: @format.currency(1234.56, "USD")
currency_eur: @format.currency(1234.56, "EUR")
percentage: @format.percent(0.1234)
decimal: @format.decimal(3.14159, 2)
scientific: @format.scientific(1234567, 2)
```

### **Date/Time Formatting**
```go
[dates]
iso_date: @format.date(@date.now(), "2006-01-02")
iso_datetime: @format.datetime(@date.now(), "2006-01-02T15:04:05Z07:00")
human_date: @format.date(@date.now(), "Monday, January 2, 2006")
short_time: @format.time(@date.now(), "15:04")
```

### **String Formatting**
```go
[strings]
phone: @format.phone("1234567890")
ssn: @format.ssn("123456789")
credit_card: @format.credit_card("1234567890123456")
postal_code: @format.postal_code("12345")
```

### **Data Structure Formatting**
```go
[structures]
json: @format.json(@query("SELECT * FROM users LIMIT 5"))
xml: @format.xml(@query("SELECT * FROM products LIMIT 3"))
csv: @format.csv(@query("SELECT name, email FROM users"))
yaml: @format.yaml(@query("SELECT * FROM settings"))
```

## 📝 **Template Formatting**

### **Email Templates**
```go
[email_templates]
welcome: @format.template("Welcome {{name}}! Your account is ready.")
order_confirmation: @format.template("Order {{order_id}} for {{amount}} has been confirmed.")
password_reset: @format.template("Click here to reset your password: {{reset_url}}")
```

### **Notification Templates**
```go
[notifications]
alert: @format.template("ALERT: {{service}} is {{status}}")
update: @format.template("Update available: {{version}}")
maintenance: @format.template("Maintenance scheduled for {{date}} at {{time}}")
```

### **Report Templates**
```go
[reports]
summary: @format.template("Total users: {{user_count}}, Active: {{active_count}}")
performance: @format.template("Response time: {{response_time}}ms, Throughput: {{throughput}} req/s")
error_report: @format.template("Error rate: {{error_rate}}%, Last error: {{last_error}}")
```

## 🔗 **Go Integration**

```go
// Access formatted data
currency := config.GetString("currency")
date := config.GetString("date")
phone := config.GetString("phone")
emailTemplate := config.GetString("email_template")

// Use formatted values
fmt.Printf("Price: %s\n", currency)
fmt.Printf("Date: %s\n", date)
fmt.Printf("Phone: %s\n", phone)

// Process templates
template := config.GetString("welcome")
// Replace template variables
formatted := strings.ReplaceAll(template, "{{name}}", "John")
```

### **Manual Formatting**
```go
type DataFormatter struct{}

func (f *DataFormatter) FormatCurrency(amount float64, currency string) string {
    switch currency {
    case "USD":
        return fmt.Sprintf("$%.2f", amount)
    case "EUR":
        return fmt.Sprintf("€%.2f", amount)
    default:
        return fmt.Sprintf("%.2f", amount)
    }
}

func (f *DataFormatter) FormatDate(date time.Time, format string) string {
    return date.Format(format)
}

func (f *DataFormatter) FormatTemplate(template string, data map[string]interface{}) string {
    result := template
    for key, value := range data {
        placeholder := fmt.Sprintf("{{%s}}", key)
        result = strings.ReplaceAll(result, placeholder, fmt.Sprint(value))
    }
    return result
}
```

## 🥇 **Best Practices**
- Use consistent formatting across your application
- Cache expensive formatting operations
- Validate formatted output
- Use templates for dynamic content
- Consider localization for international users

---

**TuskLang: Beautiful formatting with @format.** 