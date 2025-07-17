# 📅 TuskLang Bash @date Function Guide

**"We don't bow to any king" – Time is your configuration's rhythm.**

The @date function in TuskLang is your temporal powerhouse, enabling dynamic date and time operations, scheduling, and time-based logic directly within your configuration files. Whether you're creating time-based configurations, scheduling tasks, or analyzing temporal data, @date provides the precision and flexibility to work with time seamlessly.

## 🎯 What is @date?
The @date function provides date and time operations in TuskLang. It offers:
- **Date formatting** - Format dates in various formats and timezones
- **Date calculations** - Add, subtract, and manipulate dates
- **Time-based logic** - Create time-dependent configurations
- **Scheduling** - Implement time-based scheduling and triggers
- **Timezone handling** - Work with multiple timezones and conversions

## 📝 Basic @date Syntax

### Simple Date Operations
```ini
[simple_dates]
# Get current date and time
current_time: @date("Y-m-d H:i:s")
current_date: @date("Y-m-d")
current_year: @date("Y")
current_month: @date("m")
current_day: @date("d")
current_hour: @date("H")
current_minute: @date("i")
current_second: @date("s")
```

### Date Formatting
```ini
[date_formatting]
# Various date formats
iso_date: @date("Y-m-d\TH:i:sP")
rfc_date: @date("D, d M Y H:i:s T")
human_date: @date("l, F j, Y")
short_date: @date("m/d/Y")
timestamp: @date("U")
```

### Date Calculations
```ini
[date_calculations]
# Add time periods
tomorrow: @date("Y-m-d", "+1 day")
next_week: @date("Y-m-d", "+1 week")
next_month: @date("Y-m-d", "+1 month")
next_year: @date("Y-m-d", "+1 year")

# Subtract time periods
yesterday: @date("Y-m-d", "-1 day")
last_week: @date("Y-m-d", "-1 week")
last_month: @date("Y-m-d", "-1 month")
last_year: @date("Y-m-d", "-1 year")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > date-quickstart.tsk << 'EOF'
[current_time]
# Current date and time
now: @date("Y-m-d H:i:s")
today: @date("Y-m-d")
timestamp: @date("U")

[formatted_dates]
# Different date formats
iso_format: @date("Y-m-d\TH:i:sP")
human_readable: @date("l, F j, Y")
short_format: @date("m/d/Y")
time_only: @date("H:i:s")

[date_math]
# Date calculations
tomorrow: @date("Y-m-d", "+1 day")
yesterday: @date("Y-m-d", "-1 day")
next_week: @date("Y-m-d", "+1 week")
last_month: @date("Y-m-d", "-1 month")

[time_based_logic]
# Time-based conditions
is_weekend: @date("N") >= 6
is_business_hour: @date("H") >= 9 && @date("H") <= 17
is_morning: @date("H") < 12
is_afternoon: @date("H") >= 12 && @date("H") < 18
is_evening: @date("H") >= 18
EOF

config=$(tusk_parse date-quickstart.tsk)

echo "=== Current Time ==="
echo "Now: $(tusk_get "$config" current_time.now)"
echo "Today: $(tusk_get "$config" current_time.today)"
echo "Timestamp: $(tusk_get "$config" current_time.timestamp)"

echo ""
echo "=== Formatted Dates ==="
echo "ISO Format: $(tusk_get "$config" formatted_dates.iso_format)"
echo "Human Readable: $(tusk_get "$config" formatted_dates.human_readable)"
echo "Short Format: $(tusk_get "$config" formatted_dates.short_format)"
echo "Time Only: $(tusk_get "$config" formatted_dates.time_only)"

echo ""
echo "=== Date Math ==="
echo "Tomorrow: $(tusk_get "$config" date_math.tomorrow)"
echo "Yesterday: $(tusk_get "$config" date_math.yesterday)"
echo "Next Week: $(tusk_get "$config" date_math.next_week)"
echo "Last Month: $(tusk_get "$config" date_math.last_month)"

echo ""
echo "=== Time-Based Logic ==="
echo "Is Weekend: $(tusk_get "$config" time_based_logic.is_weekend)"
echo "Is Business Hour: $(tusk_get "$config" time_based_logic.is_business_hour)"
echo "Is Morning: $(tusk_get "$config" time_based_logic.is_morning)"
echo "Is Afternoon: $(tusk_get "$config" time_based_logic.is_afternoon)"
echo "Is Evening: $(tusk_get "$config" time_based_logic.is_evening)"
```

## 🔗 Real-World Use Cases

### 1. Time-Based Configuration Management
```ini
[time_based_config]
# Dynamic configuration based on time
$current_hour: @date("H")
$current_day: @date("N")
$is_business_hours: $current_hour >= 9 && $current_hour <= 17 && $current_day <= 5

# Adjust configuration based on time
$config_settings: {
    "cache_ttl": @if($is_business_hours, "5m", "30m"),
    "log_level": @if($is_business_hours, "info", "warning"),
    "max_connections": @if($is_business_hours, 100, 20),
    "backup_frequency": @if($is_business_hours, "1h", "6h")
}

# Time-based feature flags
$feature_flags: {
    "maintenance_mode": @date("H") == 2 && @date("N") == 1,  # Monday 2 AM
    "debug_mode": @date("H") >= 22 || @date("H") <= 6,       # Night hours
    "high_performance": $is_business_hours,
    "reduced_services": @date("N") >= 6                      # Weekend
}
```

### 2. Scheduling and Automation
```ini
[scheduling]
# Automated task scheduling
$scheduled_tasks: {
    "daily_backup": @date("H") == 3 && @date("i") < 10,      # 3 AM daily
    "weekly_maintenance": @date("N") == 1 && @date("H") == 2, # Monday 2 AM
    "monthly_cleanup": @date("d") == 1 && @date("H") == 1,   # 1st of month 1 AM
    "hourly_health_check": @date("i") < 5                    # Every hour
}

# Time-based triggers
$triggers: {
    "peak_hours": @date("H") >= 9 && @date("H") <= 17,
    "off_peak": @date("H") < 9 || @date("H") > 17,
    "weekend": @date("N") >= 6,
    "holiday_hours": @date("H") >= 10 && @date("H") <= 16
}

# Execute scheduled tasks
@if($scheduled_tasks.daily_backup, {
    "action": "backup_database",
    "time": @date("Y-m-d H:i:s"),
    "type": "daily"
}, "no_backup_needed")
```

### 3. Log Rotation and File Management
```ini
[log_management]
# Time-based log rotation
$log_config: {
    "current_log_file": "/var/log/app-" + @date("Y-m-d") + ".log",
    "archive_date": @date("Y-m-d", "-7 days"),
    "retention_date": @date("Y-m-d", "-30 days")
}

# Log rotation schedule
$rotation_schedule: {
    "daily_rotation": @date("H") == 0 && @date("i") < 5,     # Midnight
    "weekly_compression": @date("N") == 1 && @date("H") == 1, # Monday 1 AM
    "monthly_cleanup": @date("d") == 1 && @date("H") == 2    # 1st of month 2 AM
}

# Archive old logs
@if($rotation_schedule.weekly_compression, {
    "action": "compress_logs",
    "source": "/var/log/app-" + $log_config.archive_date + ".log",
    "destination": "/var/log/archives/app-" + $log_config.archive_date + ".log.gz"
}, "no_compression_needed")
```

### 4. Business Logic and Analytics
```ini
[business_analytics]
# Time-based business analytics
$business_periods: {
    "current_month": @date("Y-m"),
    "current_quarter": "Q" + @math((@date("n") - 1) / 3 + 1) + " " + @date("Y"),
    "current_year": @date("Y"),
    "fiscal_year": @if(@date("n") >= 7, @date("Y") + 1, @date("Y"))
}

# Time-based reporting
$reporting_schedule: {
    "daily_report": @date("H") == 6,                         # 6 AM daily
    "weekly_report": @date("N") == 1 && @date("H") == 7,     # Monday 7 AM
    "monthly_report": @date("d") == 1 && @date("H") == 8,    # 1st of month 8 AM
    "quarterly_report": @date("d") == 1 && @date("n") % 3 == 1 && @date("H") == 9
}

# Business hours calculation
$business_hours: {
    "is_open": @date("N") <= 5 && @date("H") >= 9 && @date("H") <= 17,
    "next_open": @if(@date("N") >= 6, @date("Y-m-d", "+1 day") + " 09:00", 
                     @if(@date("H") < 9, @date("Y-m-d") + " 09:00", 
                         @date("Y-m-d", "+1 day") + " 09:00")),
    "hours_until_close": @if($business_hours.is_open, 17 - @date("H"), 0)
}
```

## 🧠 Advanced @date Patterns

### Timezone Management
```ini
[timezone_management]
# Multi-timezone support
$timezones: {
    "utc": @date("Y-m-d H:i:s", "now", "UTC"),
    "est": @date("Y-m-d H:i:s", "now", "America/New_York"),
    "pst": @date("Y-m-d H:i:s", "now", "America/Los_Angeles"),
    "gmt": @date("Y-m-d H:i:s", "now", "Europe/London"),
    "jst": @date("Y-m-d H:i:s", "now", "Asia/Tokyo")
}

# Timezone conversion
$user_timezone: @env("USER_TIMEZONE", "UTC")
$local_time: @date("Y-m-d H:i:s", "now", $user_timezone)
$utc_time: @date("Y-m-d H:i:s", "now", "UTC")

# Timezone-aware scheduling
$global_schedule: {
    "maintenance_window": @date("H", "now", "UTC") == 2,      # UTC 2 AM
    "peak_hours_est": @date("H", "now", "America/New_York") >= 9 && @date("H", "now", "America/New_York") <= 17,
    "business_hours_pst": @date("H", "now", "America/Los_Angeles") >= 8 && @date("H", "now", "America/Los_Angeles") <= 18
}
```

### Complex Date Calculations
```ini
[complex_calculations]
# Advanced date calculations
$date_math: {
    "next_monday": @date("Y-m-d", "next monday"),
    "last_friday": @date("Y-m-d", "last friday"),
    "first_day_of_month": @date("Y-m-01"),
    "last_day_of_month": @date("Y-m-t"),
    "next_business_day": @date("Y-m-d", "+1 weekday"),
    "days_until_weekend": @if(@date("N") >= 6, 0, 6 - @date("N"))
}

# Date ranges
$date_ranges: {
    "this_week": {
        "start": @date("Y-m-d", "monday this week"),
        "end": @date("Y-m-d", "sunday this week")
    },
    "this_month": {
        "start": @date("Y-m-01"),
        "end": @date("Y-m-t")
    },
    "this_quarter": {
        "start": @date("Y-m-01", "first day of " + @math((@date("n") - 1) / 3) * 3 + 1 + " month"),
        "end": @date("Y-m-t", "last day of " + @math((@date("n") - 1) / 3 + 1) * 3 + " month")
    }
}
```

### Seasonal and Holiday Logic
```ini
[seasonal_logic]
# Seasonal configurations
$seasons: {
    "is_summer": @date("n") >= 6 && @date("n") <= 8,
    "is_winter": @date("n") == 12 || @date("n") <= 2,
    "is_spring": @date("n") >= 3 && @date("n") <= 5,
    "is_fall": @date("n") >= 9 && @date("n") <= 11
}

# Holiday detection (simplified)
$holidays: {
    "is_new_year": @date("n") == 1 && @date("d") == 1,
    "is_christmas": @date("n") == 12 && @date("d") == 25,
    "is_weekend": @date("N") >= 6,
    "is_holiday": $holidays.is_new_year || $holidays.is_christmas || $holidays.is_weekend
}

# Seasonal adjustments
$seasonal_config: {
    "cache_ttl": @if($seasons.is_summer, "10m", "5m"),
    "backup_frequency": @if($seasons.is_winter, "2h", "1h"),
    "maintenance_window": @if($seasons.is_fall, "1h", "30m")
}
```

## 🛡️ Security & Performance Notes
- **Timezone consistency:** Ensure consistent timezone handling across systems
- **Date validation:** Validate date inputs to prevent injection attacks
- **Performance impact:** Cache frequently used date calculations
- **Leap year handling:** Account for leap years in date calculations
- **DST transitions:** Handle daylight saving time transitions properly
- **Date format security:** Use safe date formats to prevent parsing issues

## 🐞 Troubleshooting
- **Timezone issues:** Verify timezone settings and conversions
- **Date parsing errors:** Check date format compatibility
- **Leap year bugs:** Test date calculations around leap years
- **DST problems:** Handle daylight saving time transitions
- **Performance issues:** Cache expensive date calculations

## 💡 Best Practices
- **Use ISO formats:** Prefer ISO 8601 date formats for consistency
- **Handle timezones:** Always specify timezones for critical operations
- **Cache calculations:** Cache frequently used date calculations
- **Validate inputs:** Validate date inputs before processing
- **Document formats:** Document date format expectations
- **Test edge cases:** Test date logic around year boundaries and DST

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@math Function](025-at-math-function-bash.md)
- [@if Function](029-at-if-function-bash.md)
- [Scheduling and Automation](098-scheduling-automation-bash.md)
- [Configuration Management](093-configuration-management-bash.md)

---

**Master @date in TuskLang and orchestrate time-based configurations with precision. 📅** 