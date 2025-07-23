// Package core provides core TuskLang operators
package core

import (
	"fmt"
	"strings"
	"time"
)

// DateTimeOperator handles date and time operations
type DateTimeOperator struct{}

// NewDateTimeOperator creates a new date time operator
func NewDateTimeOperator() *DateTimeOperator {
	return &DateTimeOperator{}
}

// Date executes @date operator
func (dto *DateTimeOperator) Date(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return time.Now().Format("2006-01-02"), nil
	}
	
	if len(args) == 1 {
		format, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@date format must be string")
		}
		return time.Now().Format(format), nil
	}
	
	if len(args) == 2 {
		dateStr, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@date date must be string")
		}
		
		format, ok := args[1].(string)
		if !ok {
			return nil, fmt.Errorf("@date format must be string")
		}
		
		// Parse the date string
		parsed, err := time.Parse("2006-01-02", dateStr)
		if err != nil {
			return nil, fmt.Errorf("invalid date format: %v", err)
		}
		
		return parsed.Format(format), nil
	}
	
	return nil, fmt.Errorf("@date requires 0, 1, or 2 arguments")
}

// Time executes @time operator
func (dto *DateTimeOperator) Time(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return time.Now().Format("15:04:05"), nil
	}
	
	if len(args) == 1 {
		format, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@time format must be string")
		}
		return time.Now().Format(format), nil
	}
	
	if len(args) == 2 {
		timeStr, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@time time must be string")
		}
		
		format, ok := args[1].(string)
		if !ok {
			return nil, fmt.Errorf("@time format must be string")
		}
		
		// Parse the time string
		parsed, err := time.Parse("15:04:05", timeStr)
		if err != nil {
			return nil, fmt.Errorf("invalid time format: %v", err)
		}
		
		return parsed.Format(format), nil
	}
	
	return nil, fmt.Errorf("@time requires 0, 1, or 2 arguments")
}

// Timestamp executes @timestamp operator
func (dto *DateTimeOperator) Timestamp(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return time.Now().Unix(), nil
	}
	
	if len(args) == 1 {
		format, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@timestamp format must be string")
		}
		
		switch strings.ToLower(format) {
		case "unix":
			return time.Now().Unix(), nil
		case "unixmilli":
			return time.Now().UnixMilli(), nil
		case "unixnano":
			return time.Now().UnixNano(), nil
		case "rfc3339":
			return time.Now().Format(time.RFC3339), nil
		case "rfc3339nano":
			return time.Now().Format(time.RFC3339Nano), nil
		case "iso8601":
			return time.Now().Format("2006-01-02T15:04:05Z07:00"), nil
		default:
			return nil, fmt.Errorf("unknown timestamp format: %s", format)
		}
	}
	
	if len(args) == 2 {
		dateStr, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@timestamp date must be string")
		}
		
		format, ok := args[1].(string)
		if !ok {
			return nil, fmt.Errorf("@timestamp format must be string")
		}
		
		// Parse the date string
		parsed, err := time.Parse("2006-01-02 15:04:05", dateStr)
		if err != nil {
			// Try different formats
			parsed, err = time.Parse("2006-01-02", dateStr)
			if err != nil {
				return nil, fmt.Errorf("invalid date format: %v", err)
			}
		}
		
		switch strings.ToLower(format) {
		case "unix":
			return parsed.Unix(), nil
		case "unixmilli":
			return parsed.UnixMilli(), nil
		case "unixnano":
			return parsed.UnixNano(), nil
		case "rfc3339":
			return parsed.Format(time.RFC3339), nil
		case "rfc3339nano":
			return parsed.Format(time.RFC3339Nano), nil
		case "iso8601":
			return parsed.Format("2006-01-02T15:04:05Z07:00"), nil
		default:
			return nil, fmt.Errorf("unknown timestamp format: %s", format)
		}
	}
	
	return nil, fmt.Errorf("@timestamp requires 0, 1, or 2 arguments")
}

// Now executes @now operator
func (dto *DateTimeOperator) Now(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return time.Now().Format("2006-01-02 15:04:05"), nil
	}
	
	if len(args) == 1 {
		format, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@now format must be string")
		}
		return time.Now().Format(format), nil
	}
	
	return nil, fmt.Errorf("@now requires 0 or 1 arguments")
}

// Format executes @format operator
func (dto *DateTimeOperator) Format(args ...interface{}) (interface{}, error) {
	if len(args) < 2 {
		return nil, fmt.Errorf("@format requires at least 2 arguments")
	}
	
	dateStr, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@format first argument must be string")
	}
	
	format, ok := args[1].(string)
	if !ok {
		return nil, fmt.Errorf("@format second argument must be string")
	}
	
	// Try to parse the date string with common formats
	parsed, err := time.Parse("2006-01-02 15:04:05", dateStr)
	if err != nil {
		parsed, err = time.Parse("2006-01-02", dateStr)
		if err != nil {
			parsed, err = time.Parse("15:04:05", dateStr)
			if err != nil {
				// Try RFC3339 format
				parsed, err = time.Parse(time.RFC3339, dateStr)
				if err != nil {
					return nil, fmt.Errorf("unable to parse date: %v", err)
				}
			}
		}
	}
	
	return parsed.Format(format), nil
}

// Timezone executes @timezone operator
func (dto *DateTimeOperator) Timezone(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return time.Now().Location().String(), nil
	}
	
	if len(args) == 1 {
		zone, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@timezone zone must be string")
		}
		
		loc, err := time.LoadLocation(zone)
		if err != nil {
			return nil, fmt.Errorf("invalid timezone: %v", err)
		}
		
		return time.Now().In(loc).Format("2006-01-02 15:04:05 MST"), nil
	}
	
	if len(args) == 2 {
		dateStr, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@timezone date must be string")
		}
		
		zone, ok := args[1].(string)
		if !ok {
			return nil, fmt.Errorf("@timezone zone must be string")
		}
		
		// Parse the date string
		parsed, err := time.Parse("2006-01-02 15:04:05", dateStr)
		if err != nil {
			parsed, err = time.Parse("2006-01-02", dateStr)
			if err != nil {
				return nil, fmt.Errorf("invalid date format: %v", err)
			}
		}
		
		loc, err := time.LoadLocation(zone)
		if err != nil {
			return nil, fmt.Errorf("invalid timezone: %v", err)
		}
		
		return parsed.In(loc).Format("2006-01-02 15:04:05 MST"), nil
	}
	
	return nil, fmt.Errorf("@timezone requires 0, 1, or 2 arguments")
}

// AddDays adds days to a date
func (dto *DateTimeOperator) AddDays(dateStr string, days int) (string, error) {
	parsed, err := time.Parse("2006-01-02", dateStr)
	if err != nil {
		return "", fmt.Errorf("invalid date format: %v", err)
	}
	
	result := parsed.AddDate(0, 0, days)
	return result.Format("2006-01-02"), nil
}

// AddMonths adds months to a date
func (dto *DateTimeOperator) AddMonths(dateStr string, months int) (string, error) {
	parsed, err := time.Parse("2006-01-02", dateStr)
	if err != nil {
		return "", fmt.Errorf("invalid date format: %v", err)
	}
	
	result := parsed.AddDate(0, months, 0)
	return result.Format("2006-01-02"), nil
}

// AddYears adds years to a date
func (dto *DateTimeOperator) AddYears(dateStr string, years int) (string, error) {
	parsed, err := time.Parse("2006-01-02", dateStr)
	if err != nil {
		return "", fmt.Errorf("invalid date format: %v", err)
	}
	
	result := parsed.AddDate(years, 0, 0)
	return result.Format("2006-01-02"), nil
}

// DaysBetween calculates days between two dates
func (dto *DateTimeOperator) DaysBetween(date1, date2 string) (int, error) {
	parsed1, err := time.Parse("2006-01-02", date1)
	if err != nil {
		return 0, fmt.Errorf("invalid first date format: %v", err)
	}
	
	parsed2, err := time.Parse("2006-01-02", date2)
	if err != nil {
		return 0, fmt.Errorf("invalid second date format: %v", err)
	}
	
	duration := parsed2.Sub(parsed1)
	return int(duration.Hours() / 24), nil
}

// IsWeekend checks if a date is on weekend
func (dto *DateTimeOperator) IsWeekend(dateStr string) (bool, error) {
	parsed, err := time.Parse("2006-01-02", dateStr)
	if err != nil {
		return false, fmt.Errorf("invalid date format: %v", err)
	}
	
	weekday := parsed.Weekday()
	return weekday == time.Saturday || weekday == time.Sunday, nil
}

// IsWeekday checks if a date is on weekday
func (dto *DateTimeOperator) IsWeekday(dateStr string) (bool, error) {
	parsed, err := time.Parse("2006-01-02", dateStr)
	if err != nil {
		return false, fmt.Errorf("invalid date format: %v", err)
	}
	
	weekday := parsed.Weekday()
	return weekday != time.Saturday && weekday != time.Sunday, nil
}

// GetWeekday gets the weekday name
func (dto *DateTimeOperator) GetWeekday(dateStr string) (string, error) {
	parsed, err := time.Parse("2006-01-02", dateStr)
	if err != nil {
		return "", fmt.Errorf("invalid date format: %v", err)
	}
	
	return parsed.Weekday().String(), nil
}

// GetMonth gets the month name
func (dto *DateTimeOperator) GetMonth(dateStr string) (string, error) {
	parsed, err := time.Parse("2006-01-02", dateStr)
	if err != nil {
		return "", fmt.Errorf("invalid date format: %v", err)
	}
	
	return parsed.Month().String(), nil
}

// GetYear gets the year
func (dto *DateTimeOperator) GetYear(dateStr string) (int, error) {
	parsed, err := time.Parse("2006-01-02", dateStr)
	if err != nil {
		return 0, fmt.Errorf("invalid date format: %v", err)
	}
	
	return parsed.Year(), nil
} 