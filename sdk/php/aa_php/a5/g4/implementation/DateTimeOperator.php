<?php

declare(strict_types=1);

namespace TuskLang\A5\G4;

use TuskLang\CoreOperator;
use InvalidArgumentException;
use DateTime;
use DateTimeImmutable;
use DateTimeZone;
use DateInterval;
use DatePeriod;

/**
 * DateTimeOperator - Advanced date/time parsing, formatting, and arithmetic operations
 * 
 * Provides comprehensive date and time operations including parsing, formatting,
 * arithmetic, comparison, and manipulation with full timezone support.
 */
class DateTimeOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'datetime';
    }

    public function getDescription(): string 
    {
        return 'Advanced date/time parsing, formatting, and arithmetic operations';
    }

    public function getSupportedActions(): array
    {
        return [
            'parse', 'format', 'now', 'create', 'add', 'sub', 'diff',
            'compare', 'is_valid', 'to_timestamp', 'from_timestamp',
            'start_of', 'end_of', 'is_weekend', 'is_weekday', 'age'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'parse' => $this->parseDateTime($params['datetime'] ?? '', $params['format'] ?? null, $params['timezone'] ?? null),
            'format' => $this->formatDateTime($params['datetime'] ?? null, $params['format'] ?? 'Y-m-d H:i:s', $params['timezone'] ?? null),
            'now' => $this->getCurrentDateTime($params['timezone'] ?? null),
            'create' => $this->createDateTime($params['year'] ?? null, $params['month'] ?? null, $params['day'] ?? null, $params['hour'] ?? 0, $params['minute'] ?? 0, $params['second'] ?? 0, $params['timezone'] ?? null),
            'add' => $this->addToDateTime($params['datetime'] ?? null, $params['interval'] ?? ''),
            'sub' => $this->subtractFromDateTime($params['datetime'] ?? null, $params['interval'] ?? ''),
            'diff' => $this->diffDateTime($params['datetime1'] ?? null, $params['datetime2'] ?? null, $params['absolute'] ?? true),
            'compare' => $this->compareDateTime($params['datetime1'] ?? null, $params['datetime2'] ?? null),
            'is_valid' => $this->isValidDateTime($params['datetime'] ?? '', $params['format'] ?? null),
            'to_timestamp' => $this->toTimestamp($params['datetime'] ?? null),
            'from_timestamp' => $this->fromTimestamp($params['timestamp'] ?? 0, $params['timezone'] ?? null),
            'start_of' => $this->startOf($params['datetime'] ?? null, $params['unit'] ?? 'day'),
            'end_of' => $this->endOf($params['datetime'] ?? null, $params['unit'] ?? 'day'),
            'is_weekend' => $this->isWeekend($params['datetime'] ?? null),
            'is_weekday' => $this->isWeekday($params['datetime'] ?? null),
            'age' => $this->calculateAge($params['birthdate'] ?? null, $params['reference_date'] ?? null),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Parse datetime string with optional format and timezone
     */
    private function parseDateTime(string $datetime, ?string $format = null, ?string $timezone = null): DateTimeImmutable
    {
        if (empty($datetime)) {
            throw new InvalidArgumentException('DateTime string cannot be empty');
        }

        $tz = $timezone ? new DateTimeZone($timezone) : null;

        try {
            if ($format) {
                $result = DateTimeImmutable::createFromFormat($format, $datetime, $tz);
                if ($result === false) {
                    throw new InvalidArgumentException("Failed to parse datetime '{$datetime}' with format '{$format}'");
                }
                return $result;
            } else {
                return new DateTimeImmutable($datetime, $tz);
            }
        } catch (\Exception $e) {
            throw new InvalidArgumentException("Invalid datetime: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Format datetime object to string
     */
    private function formatDateTime(?DateTimeImmutable $datetime, string $format, ?string $timezone = null): string
    {
        if ($datetime === null) {
            throw new InvalidArgumentException('DateTime object cannot be null');
        }

        if ($timezone) {
            $datetime = $datetime->setTimezone(new DateTimeZone($timezone));
        }

        return $datetime->format($format);
    }

    /**
     * Get current datetime
     */
    private function getCurrentDateTime(?string $timezone = null): DateTimeImmutable
    {
        $tz = $timezone ? new DateTimeZone($timezone) : null;
        return new DateTimeImmutable('now', $tz);
    }

    /**
     * Create datetime from components
     */
    private function createDateTime(?int $year, ?int $month, ?int $day, int $hour = 0, int $minute = 0, int $second = 0, ?string $timezone = null): DateTimeImmutable
    {
        $year = $year ?? date('Y');
        $month = $month ?? date('n');
        $day = $day ?? date('j');

        if ($month < 1 || $month > 12) {
            throw new InvalidArgumentException('Month must be between 1 and 12');
        }

        if ($day < 1 || $day > 31) {
            throw new InvalidArgumentException('Day must be between 1 and 31');
        }

        if ($hour < 0 || $hour > 23) {
            throw new InvalidArgumentException('Hour must be between 0 and 23');
        }

        if ($minute < 0 || $minute > 59) {
            throw new InvalidArgumentException('Minute must be between 0 and 59');
        }

        if ($second < 0 || $second > 59) {
            throw new InvalidArgumentException('Second must be between 0 and 59');
        }

        $tz = $timezone ? new DateTimeZone($timezone) : null;
        
        try {
            return new DateTimeImmutable(
                sprintf('%04d-%02d-%02d %02d:%02d:%02d', $year, $month, $day, $hour, $minute, $second),
                $tz
            );
        } catch (\Exception $e) {
            throw new InvalidArgumentException("Invalid date components: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Add interval to datetime
     */
    private function addToDateTime(?DateTimeImmutable $datetime, string $interval): DateTimeImmutable
    {
        if ($datetime === null) {
            throw new InvalidArgumentException('DateTime object cannot be null');
        }

        if (empty($interval)) {
            throw new InvalidArgumentException('Interval cannot be empty');
        }

        try {
            return $datetime->add(new DateInterval($interval));
        } catch (\Exception $e) {
            throw new InvalidArgumentException("Invalid interval '{$interval}': " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Subtract interval from datetime
     */
    private function subtractFromDateTime(?DateTimeImmutable $datetime, string $interval): DateTimeImmutable
    {
        if ($datetime === null) {
            throw new InvalidArgumentException('DateTime object cannot be null');
        }

        if (empty($interval)) {
            throw new InvalidArgumentException('Interval cannot be empty');
        }

        try {
            return $datetime->sub(new DateInterval($interval));
        } catch (\Exception $e) {
            throw new InvalidArgumentException("Invalid interval '{$interval}': " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Calculate difference between two datetimes
     */
    private function diffDateTime(?DateTimeImmutable $datetime1, ?DateTimeImmutable $datetime2, bool $absolute = true): array
    {
        if ($datetime1 === null || $datetime2 === null) {
            throw new InvalidArgumentException('Both DateTime objects must be provided');
        }

        $diff = $datetime1->diff($datetime2, $absolute);

        return [
            'years' => $diff->y,
            'months' => $diff->m,
            'days' => $diff->d,
            'hours' => $diff->h,
            'minutes' => $diff->i,
            'seconds' => $diff->s,
            'total_days' => $diff->days,
            'invert' => $diff->invert === 1,
            'formatted' => $diff->format('%R%a days, %H:%I:%S')
        ];
    }

    /**
     * Compare two datetimes
     */
    private function compareDateTime(?DateTimeImmutable $datetime1, ?DateTimeImmutable $datetime2): array
    {
        if ($datetime1 === null || $datetime2 === null) {
            throw new InvalidArgumentException('Both DateTime objects must be provided');
        }

        $timestamp1 = $datetime1->getTimestamp();
        $timestamp2 = $datetime2->getTimestamp();

        return [
            'comparison' => $timestamp1 <=> $timestamp2,
            'is_equal' => $timestamp1 === $timestamp2,
            'is_before' => $timestamp1 < $timestamp2,
            'is_after' => $timestamp1 > $timestamp2,
            'difference_seconds' => abs($timestamp1 - $timestamp2)
        ];
    }

    /**
     * Validate datetime string
     */
    private function isValidDateTime(string $datetime, ?string $format = null): array
    {
        if (empty($datetime)) {
            return ['valid' => false, 'error' => 'DateTime string is empty'];
        }

        try {
            if ($format) {
                $result = DateTimeImmutable::createFromFormat($format, $datetime);
                $valid = $result !== false;
                $error = $valid ? null : 'Failed to parse with specified format';
            } else {
                new DateTimeImmutable($datetime);
                $valid = true;
                $error = null;
            }

            return [
                'valid' => $valid,
                'error' => $error,
                'parsed_format' => $format,
                'input' => $datetime
            ];
        } catch (\Exception $e) {
            return [
                'valid' => false,
                'error' => $e->getMessage(),
                'parsed_format' => $format,
                'input' => $datetime
            ];
        }
    }

    /**
     * Convert datetime to timestamp
     */
    private function toTimestamp(?DateTimeImmutable $datetime): int
    {
        if ($datetime === null) {
            throw new InvalidArgumentException('DateTime object cannot be null');
        }

        return $datetime->getTimestamp();
    }

    /**
     * Create datetime from timestamp
     */
    private function fromTimestamp(int $timestamp, ?string $timezone = null): DateTimeImmutable
    {
        $tz = $timezone ? new DateTimeZone($timezone) : null;
        
        try {
            return (new DateTimeImmutable())->setTimestamp($timestamp)->setTimezone($tz ?: new DateTimeZone('UTC'));
        } catch (\Exception $e) {
            throw new InvalidArgumentException("Invalid timestamp: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Get start of time unit (day, week, month, year)
     */
    private function startOf(?DateTimeImmutable $datetime, string $unit): DateTimeImmutable
    {
        if ($datetime === null) {
            throw new InvalidArgumentException('DateTime object cannot be null');
        }

        return match(strtolower($unit)) {
            'second' => $datetime->setTime(
                (int)$datetime->format('H'),
                (int)$datetime->format('i'),
                (int)$datetime->format('s'),
                0
            ),
            'minute' => $datetime->setTime(
                (int)$datetime->format('H'),
                (int)$datetime->format('i'),
                0,
                0
            ),
            'hour' => $datetime->setTime((int)$datetime->format('H'), 0, 0, 0),
            'day' => $datetime->setTime(0, 0, 0, 0),
            'week' => $datetime->setTime(0, 0, 0, 0)->modify('monday this week'),
            'month' => $datetime->setDate((int)$datetime->format('Y'), (int)$datetime->format('n'), 1)->setTime(0, 0, 0, 0),
            'year' => $datetime->setDate((int)$datetime->format('Y'), 1, 1)->setTime(0, 0, 0, 0),
            default => throw new InvalidArgumentException("Unsupported unit: {$unit}")
        };
    }

    /**
     * Get end of time unit (day, week, month, year)
     */
    private function endOf(?DateTimeImmutable $datetime, string $unit): DateTimeImmutable
    {
        if ($datetime === null) {
            throw new InvalidArgumentException('DateTime object cannot be null');
        }

        return match(strtolower($unit)) {
            'second' => $datetime->setTime(
                (int)$datetime->format('H'),
                (int)$datetime->format('i'),
                (int)$datetime->format('s'),
                999999
            ),
            'minute' => $datetime->setTime(
                (int)$datetime->format('H'),
                (int)$datetime->format('i'),
                59,
                999999
            ),
            'hour' => $datetime->setTime((int)$datetime->format('H'), 59, 59, 999999),
            'day' => $datetime->setTime(23, 59, 59, 999999),
            'week' => $datetime->setTime(23, 59, 59, 999999)->modify('sunday this week'),
            'month' => $datetime->setDate((int)$datetime->format('Y'), (int)$datetime->format('n'), (int)$datetime->format('t'))->setTime(23, 59, 59, 999999),
            'year' => $datetime->setDate((int)$datetime->format('Y'), 12, 31)->setTime(23, 59, 59, 999999),
            default => throw new InvalidArgumentException("Unsupported unit: {$unit}")
        };
    }

    /**
     * Check if datetime is weekend
     */
    private function isWeekend(?DateTimeImmutable $datetime): bool
    {
        if ($datetime === null) {
            throw new InvalidArgumentException('DateTime object cannot be null');
        }

        $dayOfWeek = (int)$datetime->format('N'); // 1 (Monday) to 7 (Sunday)
        return in_array($dayOfWeek, [6, 7], true); // Saturday and Sunday
    }

    /**
     * Check if datetime is weekday
     */
    private function isWeekday(?DateTimeImmutable $datetime): bool
    {
        return !$this->isWeekend($datetime);
    }

    /**
     * Calculate age from birthdate
     */
    private function calculateAge(?DateTimeImmutable $birthdate, ?DateTimeImmutable $referenceDate = null): array
    {
        if ($birthdate === null) {
            throw new InvalidArgumentException('Birthdate cannot be null');
        }

        $referenceDate = $referenceDate ?? new DateTimeImmutable('now');
        
        if ($birthdate > $referenceDate) {
            throw new InvalidArgumentException('Birthdate cannot be in the future');
        }

        $diff = $birthdate->diff($referenceDate);

        return [
            'years' => $diff->y,
            'months' => $diff->m,
            'days' => $diff->d,
            'total_days' => $diff->days,
            'formatted' => "{$diff->y} years, {$diff->m} months, {$diff->d} days"
        ];
    }
} 