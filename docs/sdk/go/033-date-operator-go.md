# @date Operator in TuskLang - Go Guide

## ⏰ **Time is Power: @date Operator Mastery**

TuskLang's `@date` operator is your temporal superpower. We don't bow to any king—not even time itself. Here's how to wield `@date` in Go projects for dynamic, time-aware configuration.

## 📋 **Table of Contents**
- [What is @date?](#what-is-date)
- [Basic Usage](#basic-usage)
- [Date Formatting](#date-formatting)
- [Date Calculations](#date-calculations)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🕐 **What is @date?**

The `@date` operator provides dynamic date and time operations directly in your config. From timestamps to relative dates, `@date` makes your config time-aware.

## 🛠️ **Basic Usage**

```go
[build]
build_time: @date.now()
build_date: @date("Y-m-d")
```

## 📅 **Date Formatting**

```go
[timestamps]
iso_time: @date("2006-01-02T15:04:05Z07:00")
short_date: @date("2006-01-02")
long_date: @date("Monday, January 2, 2006")
```

## 🧮 **Date Calculations**

```go
[expiry]
tomorrow: @date("+1d")
next_week: @date("+7d")
next_month: @date("+1M")
```

## 🔗 **Go Integration**

```go
buildTime := config.GetString("build_time")
buildDate := config.GetString("build_date")
```

### **Manual Date Operations**
```go
now := time.Now()
formatted := now.Format("2006-01-02")
tomorrow := now.AddDate(0, 0, 1)
```

## 🥇 **Best Practices**
- Use ISO 8601 format for timestamps
- Cache expensive date calculations
- Validate date ranges in your Go code

---

**TuskLang: Time is on your side with @date.** 