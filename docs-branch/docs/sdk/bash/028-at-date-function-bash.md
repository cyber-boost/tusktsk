# 📅 TuskLang Bash @date Function Guide

**"We don't bow to any king" – Time is your configuration's heartbeat.**

The @date function in TuskLang is your gateway to dynamic, time-aware configuration. Whether you're building scheduled tasks, creating time-based features, or implementing temporal logic, @date provides the power to make your configurations responsive to the passage of time and current temporal context.

## 🎯 What is @date?
The @date function provides date and time operations in TuskLang. It offers:
- **Current time** - Real-time date and time information
- **Date formatting** - Custom date string formatting
- **Date arithmetic** - Add/subtract time intervals
- **Time zones** - Timezone-aware operations
- **Temporal logic** - Time-based conditional behavior

## 📝 Basic @date Syntax

### Current Time Operations
```ini
[basic]
# Get current timestamp
current_time: @date.now()
current_date: @date("Y-m-d")
current_datetime: @date("Y-m-d H:i:s")
current_timestamp: @date("U")
```

### Formatted Date Strings
```ini
[formatted]
# Different date formats
iso_date: @date("Y-m-d")
iso_datetime: @date("Y-m-d H:i:s")
readable_date: @date("F j, Y")
readable_datetime: @date("F j, Y g:i A")
short_date: @date("m/d/Y")
time_only: @date("H:i:s")
```

### Date Arithmetic
```ini
[arithmetic]
# Add/subtract time intervals
tomorrow: @date("Y-m-d", "+1 day")
yesterday: @date("Y-m-d", "-1 day")
next_week: @date("Y-m-d", "+1 week")
last_month: @date("Y-m-d", "-1 month")
next_year: @date("Y-m-d", "+1 year")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > date-quickstart.tsk << 'EOF'
[current_time]
timestamp: @date.now()
iso_date: @date("Y-m-d")
iso_datetime: @date("Y-m-d H:i:s")
readable_date: @date("F j, Y")
readable_time: @date("g:i A")
unix_timestamp: @date("U")

[relative_dates]
yesterday: @date("Y-m-d", "-1 day")
tomorrow: @date("Y-m-d", "+1 day")
next_week: @date("Y-m-d", "+1 week")
last_month: @date("Y-m-d", "-1 month")
next_year: @date("Y-m-d", "+1 year")

[time_intervals]
one_hour_ago: @date("Y-m-d H:i:s", "-1 hour")
two_hours_later: @date("Y-m-d H:i:s", "+2 hours")
start_of_day: @date("Y-m-d 00:00:00")
end_of_day: @date("Y-m-d 23:59:59")

[week_calculations]
start_of_week: @date("Y-m-d", "monday this week")
end_of_week: @date("Y-m-d", "sunday this week")
day_of_week: @date("l")
week_number: @date("W")
EOF

config=$(tusk_parse date-quickstart.tsk)

echo "=== Current Time ==="
echo "Timestamp: $(tusk_get "$config" current_time.timestamp)"
echo "ISO Date: $(tusk_get "$config" current_time.iso_date)"
echo "ISO DateTime: $(tusk_get "$config" current_time.iso_datetime)"
echo "Readable Date: $(tusk_get "$config" current_time.readable_date)"
echo "Readable Time: $(tusk_get "$config" current_time.readable_time)"
echo "Unix Timestamp: $(tusk_get "$config" current_time.unix_timestamp)"

echo ""
echo "=== Relative Dates ==="
echo "Yesterday: $(tusk_get "$config" relative_dates.yesterday)"
echo "Tomorrow: $(tusk_get "$config" relative_dates.tomorrow)"
echo "Next Week: $(tusk_get "$config" relative_dates.next_week)"
echo "Last Month: $(tusk_get "$config" relative_dates.last_month)"
echo "Next Year: $(tusk_get "$config" relative_dates.next_year)"

echo ""
echo "=== Time Intervals ==="
echo "One Hour Ago: $(tusk_get "$config" time_intervals.one_hour_ago)"
echo "Two Hours Later: $(tusk_get "$config" time_intervals.two_hours_later)"
echo "Start of Day: $(tusk_get "$config" time_intervals.start_of_day)"
echo "End of Day: $(tusk_get "$config" time_intervals.end_of_day)"

echo ""
echo "=== Week Calculations ==="
echo "Start of Week: $(tusk_get "$config" week_calculations.start_of_week)"
echo "End of Week: $(tusk_get "$config" week_calculations.end_of_week)"
echo "Day of Week: $(tusk_get "$config" week_calculations.day_of_week)"
echo "Week Number: $(tusk_get "$config" week_calculations.week_number)"
```

## 🔗 Real-World Use Cases

### 1. Scheduled Task Configuration
```ini
[scheduled_tasks]
# Time-based task scheduling
current_time: @date("H:i")
current_date: @date("Y-m-d")
day_of_week: @date("l")

# Conditional task execution
backup_time: @if($current_time == "02:00", true, false)
maintenance_window: @if($day_of_week == "Sunday" && $current_time >= "01:00" && $current_time <= "05:00", true, false)
report_generation: @if($current_time == "09:00", true, false)

# Dynamic scheduling
next_backup: @date("Y-m-d H:i:s", "+1 day 02:00")
next_maintenance: @date("Y-m-d H:i:s", "next sunday 02:00")
next_report: @date("Y-m-d H:i:s", "tomorrow 09:00")
```

### 2. Log Rotation and Cleanup
```ini
[log_management]
# Time-based log operations
current_date: @date("Y-m-d")
current_month: @date("Y-m")
current_year: @date("Y")

# Log file naming with dates
today_log: @string.concat("/var/log/app-", $current_date, ".log")
monthly_log: @string.concat("/var/log/app-", $current_month, ".log")
yearly_log: @string.concat("/var/log/app-", $current_year, ".log")

# Cleanup thresholds
retention_date: @date("Y-m-d", "-30 days")
archive_date: @date("Y-m-d", "-90 days")
delete_date: @date("Y-m-d", "-365 days")

# Conditional cleanup
should_rotate: @if($current_time == "00:00", true, false)
should_archive: @if($current_date == @date("Y-m-01"), true, false)
should_cleanup: @if($current_date == @date("Y-01-01"), true, false)
```

### 3. Time-Based Feature Flags
```ini
[time_features]
# Time-based feature activation
current_hour: @date("H")
current_minute: @date("i")
current_second: @date("s")

# Business hours logic
business_hours: @if($current_hour >= "09" && $current_hour < "17", true, false)
lunch_break: @if($current_hour == "12" && $current_minute >= "00" && $current_minute <= "59", true, false)
after_hours: @if($current_hour < "09" || $current_hour >= "17", true, false)

# Time-based features
peak_hours: @if($current_hour >= "10" && $current_hour <= "15", true, false)
maintenance_mode: @if($current_hour == "03" && $current_minute >= "00" && $current_minute <= "59", true, false)
beta_testing: @if($current_hour >= "14" && $current_hour <= "16", true, false)
```

### 4. Analytics and Reporting
```ini
[analytics]
# Time-based analytics periods
current_period: @date("Y-m")
previous_period: @date("Y-m", "-1 month")
next_period: @date("Y-m", "+1 month")

# Date ranges for queries
today_start: @date("Y-m-d 00:00:00")
today_end: @date("Y-m-d 23:59:59")
week_start: @date("Y-m-d 00:00:00", "monday this week")
week_end: @date("Y-m-d 23:59:59", "sunday this week")
month_start: @date("Y-m-01 00:00:00")
month_end: @date("Y-m-d 23:59:59", "last day of this month")

# Relative time periods
last_24_hours: @date("Y-m-d H:i:s", "-24 hours")
last_7_days: @date("Y-m-d H:i:s", "-7 days")
last_30_days: @date("Y-m-d H:i:s", "-30 days")
last_90_days: @date("Y-m-d H:i:s", "-90 days")
```

## 🧠 Advanced @date Patterns

### Timezone Operations
```bash
#!/bin/bash
source tusk-bash.sh

cat > timezone-operations.tsk << 'EOF'
[timezones]
# Different timezone operations
utc_time: @date("Y-m-d H:i:s", "UTC")
local_time: @date("Y-m-d H:i:s")
eastern_time: @date("Y-m-d H:i:s", "America/New_York")
pacific_time: @date("Y-m-d H:i:s", "America/Los_Angeles")
london_time: @date("Y-m-d H:i:s", "Europe/London")

# Timezone conversions
utc_timestamp: @date("U", "UTC")
local_timestamp: @date("U")
timezone_offset: @math($local_timestamp - $utc_timestamp)

# Business hours in different timezones
ny_business_hours: @if(@date("H", "America/New_York") >= "09" && @date("H", "America/New_York") < "17", true, false)
la_business_hours: @if(@date("H", "America/Los_Angeles") >= "09" && @date("H", "America/Los_Angeles") < "17", true, false)
london_business_hours: @if(@date("H", "Europe/London") >= "09" && @date("H", "Europe/London") < "17", true, false)
EOF

config=$(tusk_parse timezone-operations.tsk)
echo "UTC Time: $(tusk_get "$config" timezones.utc_time)"
echo "Local Time: $(tusk_get "$config" timezones.local_time)"
echo "Eastern Time: $(tusk_get "$config" timezones.eastern_time)"
echo "Pacific Time: $(tusk_get "$config" timezones.pacific_time)"
echo "London Time: $(tusk_get "$config" timezones.london_time)"
```

### Complex Date Logic
```ini
[complex_dates]
# Complex date calculations
current_quarter: @math(ceil(@date("n") / 3))
quarter_start: @date("Y-m-01", "first day of +" + @math($current_quarter * 3 - 3) + " months")
quarter_end: @date("Y-m-d", "last day of +" + @math($current_quarter * 3 - 1) + " months")

# Fiscal year calculations
fiscal_year_start: @date("Y-07-01")
fiscal_year_end: @date("Y-06-30", "+1 year")
current_fiscal_year: @if(@date("n") >= 7, @date("Y") + 1, @date("Y"))

# Holiday calculations
christmas: @date("Y-12-25")
new_year: @date("Y-01-01", "+1 year")
easter: @date("Y-m-d", "easter")
```

### Performance Monitoring
```ini
[performance]
# Time-based performance tracking
request_timestamp: @date("U")
response_time_ms: @math(@date("U") * 1000 + @date("u") / 1000 - $request_timestamp * 1000)

# Time-based metrics
hourly_requests: @metrics("requests_per_hour", @date("H"))
daily_requests: @metrics("requests_per_day", @date("Y-m-d"))
monthly_requests: @metrics("requests_per_month", @date("Y-m"))

# Performance windows
peak_period: @if(@date("H") >= "10" && @date("H") <= "15", true, false)
off_peak_period: @if(@date("H") < "06" || @date("H") > "22", true, false)
```

## 🛡️ Security & Performance Notes
- **Timezone handling:** Be aware of timezone differences in distributed systems
- **Date validation:** Always validate date inputs before using them in calculations
- **Performance:** Date operations are fast but complex calculations may impact performance
- **Leap years:** Handle leap year calculations properly for accurate date arithmetic
- **DST transitions:** Be careful with daylight saving time transitions

## 🐞 Troubleshooting
- **Timezone issues:** Ensure proper timezone configuration for your environment
- **Date format errors:** Use correct date format strings for your requirements
- **Arithmetic errors:** Validate date arithmetic operations for edge cases
- **Performance issues:** Cache expensive date calculations when possible

## 💡 Best Practices
- **Use ISO formats:** Use ISO date formats (Y-m-d, Y-m-d H:i:s) for consistency
- **Handle timezones:** Always consider timezone implications in distributed systems
- **Cache calculations:** Cache expensive date calculations to improve performance
- **Validate inputs:** Validate date inputs before using them in calculations
- **Document formats:** Document the expected date format for your application
- **Test edge cases:** Test date operations with edge cases like leap years and DST

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@math Function](029-at-math-function-bash.md)
- [String Operations](065-string-operations-bash.md)
- [Conditional Logic](060-conditional-logic-bash.md)

---

**Master @date in TuskLang and make your configurations time-aware and dynamic. 📅** 