<?php

declare(strict_types=1);

namespace TuskLang\A5\G4;

use TuskLang\CoreOperator;
use InvalidArgumentException;
use DateTimeZone;
use DateTimeImmutable;

/**
 * TimezoneOperator - Complex timezone conversion and handling across global regions
 * 
 * Provides comprehensive timezone operations including conversion, validation,
 * DST handling, and timezone information retrieval with full global support.
 */
class TimezoneOperator extends CoreOperator
{
    private array $commonTimezones;

    public function __construct()
    {
        $this->commonTimezones = [
            'UTC' => 'UTC',
            'EST' => 'America/New_York',
            'CST' => 'America/Chicago', 
            'MST' => 'America/Denver',
            'PST' => 'America/Los_Angeles',
            'GMT' => 'Europe/London',
            'CET' => 'Europe/Paris',
            'JST' => 'Asia/Tokyo',
            'AEST' => 'Australia/Sydney'
        ];
    }

    public function getName(): string
    {
        return 'timezone';
    }

    public function getDescription(): string 
    {
        return 'Complex timezone conversion and handling across global regions';
    }

    public function getSupportedActions(): array
    {
        return [
            'convert', 'list_timezones', 'get_info', 'validate', 'get_offset',
            'is_dst', 'dst_transitions', 'abbreviation', 'detect_from_offset',
            'business_hours', 'time_overlap', 'world_clock', 'batch_convert'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'convert' => $this->convertTimezone($params['datetime'] ?? null, $params['from_tz'] ?? '', $params['to_tz'] ?? ''),
            'list_timezones' => $this->listTimezones($params['region'] ?? null),
            'get_info' => $this->getTimezoneInfo($params['timezone'] ?? ''),
            'validate' => $this->validateTimezone($params['timezone'] ?? ''),
            'get_offset' => $this->getTimezoneOffset($params['timezone'] ?? '', $params['datetime'] ?? null),
            'is_dst' => $this->isDaylight($params['timezone'] ?? '', $params['datetime'] ?? null),
            'dst_transitions' => $this->getDSTTransitions($params['timezone'] ?? '', $params['year'] ?? null),
            'abbreviation' => $this->getAbbreviation($params['timezone'] ?? '', $params['datetime'] ?? null),
            'detect_from_offset' => $this->detectFromOffset($params['offset'] ?? 0, $params['datetime'] ?? null),
            'business_hours' => $this->getBusinessHours($params['timezone'] ?? '', $params['datetime'] ?? null),
            'time_overlap' => $this->findTimeOverlap($params['timezone1'] ?? '', $params['timezone2'] ?? '', $params['datetime'] ?? null),
            'world_clock' => $this->worldClock($params['timezones'] ?? [], $params['datetime'] ?? null),
            'batch_convert' => $this->batchConvert($params['datetimes'] ?? [], $params['from_tz'] ?? '', $params['to_tz'] ?? ''),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Convert datetime between timezones
     */
    private function convertTimezone(?DateTimeImmutable $datetime, string $fromTz, string $toTz): array
    {
        if ($datetime === null) {
            $datetime = new DateTimeImmutable('now');
        }

        $fromTimezone = $this->parseTimezone($fromTz);
        $toTimezone = $this->parseTimezone($toTz);

        // Set the original timezone
        $originalDateTime = $datetime->setTimezone($fromTimezone);
        
        // Convert to target timezone
        $convertedDateTime = $originalDateTime->setTimezone($toTimezone);

        return [
            'original' => [
                'datetime' => $originalDateTime->format('Y-m-d H:i:s'),
                'timezone' => $fromTimezone->getName(),
                'offset' => $originalDateTime->format('P'),
                'timestamp' => $originalDateTime->getTimestamp()
            ],
            'converted' => [
                'datetime' => $convertedDateTime->format('Y-m-d H:i:s'),
                'timezone' => $toTimezone->getName(),
                'offset' => $convertedDateTime->format('P'),
                'timestamp' => $convertedDateTime->getTimestamp()
            ],
            'difference_hours' => ($convertedDateTime->getOffset() - $originalDateTime->getOffset()) / 3600
        ];
    }

    /**
     * List available timezones
     */
    private function listTimezones(?string $region = null): array
    {
        $timezones = DateTimeZone::listIdentifiers();
        
        if ($region) {
            $timezones = array_filter($timezones, function($tz) use ($region) {
                return str_starts_with($tz, ucfirst(strtolower($region)) . '/');
            });
        }

        $result = [];
        foreach ($timezones as $timezone) {
            $tz = new DateTimeZone($timezone);
            $dt = new DateTimeImmutable('now', $tz);
            
            $result[] = [
                'identifier' => $timezone,
                'name' => $this->getTimezoneName($timezone),
                'offset' => $dt->format('P'),
                'offset_seconds' => $tz->getOffset($dt),
                'abbreviation' => $dt->format('T'),
                'is_dst' => $dt->format('I') === '1'
            ];
        }

        return $result;
    }

    /**
     * Get comprehensive timezone information
     */
    private function getTimezoneInfo(string $timezone): array
    {
        $tz = $this->parseTimezone($timezone);
        $now = new DateTimeImmutable('now', $tz);
        
        $location = $tz->getLocation();
        $transitions = $tz->getTransitions(time(), time() + (365 * 24 * 3600));

        return [
            'identifier' => $tz->getName(),
            'name' => $this->getTimezoneName($tz->getName()),
            'current_offset' => $now->format('P'),
            'current_offset_seconds' => $tz->getOffset($now),
            'abbreviation' => $now->format('T'),
            'is_dst' => $now->format('I') === '1',
            'location' => $location ? [
                'country_code' => $location['country_code'],
                'latitude' => $location['latitude'],
                'longitude' => $location['longitude'],
                'comments' => $location['comments'] ?? ''
            ] : null,
            'next_transition' => $this->getNextTransition($tz),
            'dst_savings' => $this->getDSTSavings($tz)
        ];
    }

    /**
     * Validate timezone identifier
     */
    private function validateTimezone(string $timezone): array
    {
        try {
            $tz = $this->parseTimezone($timezone);
            return [
                'valid' => true,
                'identifier' => $tz->getName(),
                'normalized' => $this->normalizeTimezone($timezone)
            ];
        } catch (\Exception $e) {
            return [
                'valid' => false,
                'error' => $e->getMessage(),
                'suggestions' => $this->suggestTimezones($timezone)
            ];
        }
    }

    /**
     * Get timezone offset for specific datetime
     */
    private function getTimezoneOffset(string $timezone, ?DateTimeImmutable $datetime = null): array
    {
        $tz = $this->parseTimezone($timezone);
        $dt = $datetime ?? new DateTimeImmutable('now');
        $dt = $dt->setTimezone($tz);

        return [
            'offset_formatted' => $dt->format('P'),
            'offset_seconds' => $tz->getOffset($dt),
            'offset_hours' => $tz->getOffset($dt) / 3600,
            'utc_datetime' => $dt->setTimezone(new DateTimeZone('UTC'))->format('Y-m-d H:i:s'),
            'local_datetime' => $dt->format('Y-m-d H:i:s')
        ];
    }

    /**
     * Check if timezone is in daylight saving time
     */
    private function isDaylight(string $timezone, ?DateTimeImmutable $datetime = null): array
    {
        $tz = $this->parseTimezone($timezone);
        $dt = ($datetime ?? new DateTimeImmutable('now'))->setTimezone($tz);

        return [
            'is_dst' => $dt->format('I') === '1',
            'abbreviation' => $dt->format('T'),
            'offset' => $dt->format('P'),
            'dst_savings' => $this->getDSTSavings($tz, $dt)
        ];
    }

    /**
     * Get DST transitions for a year
     */
    private function getDSTTransitions(string $timezone, ?int $year = null): array
    {
        $tz = $this->parseTimezone($timezone);
        $year = $year ?? (int) date('Y');
        
        $start = mktime(0, 0, 0, 1, 1, $year);
        $end = mktime(23, 59, 59, 12, 31, $year);
        
        $transitions = $tz->getTransitions($start, $end);
        
        $result = [];
        foreach ($transitions as $transition) {
            if ($transition['ts'] >= $start && $transition['ts'] <= $end) {
                $result[] = [
                    'datetime' => date('Y-m-d H:i:s', $transition['ts']),
                    'offset' => $transition['offset'],
                    'offset_formatted' => $this->formatOffset($transition['offset']),
                    'abbreviation' => $transition['abbr'],
                    'is_dst' => $transition['isdst'],
                    'type' => $transition['isdst'] ? 'dst_start' : 'dst_end'
                ];
            }
        }

        return $result;
    }

    /**
     * Get timezone abbreviation
     */
    private function getAbbreviation(string $timezone, ?DateTimeImmutable $datetime = null): string
    {
        $tz = $this->parseTimezone($timezone);
        $dt = ($datetime ?? new DateTimeImmutable('now'))->setTimezone($tz);
        
        return $dt->format('T');
    }

    /**
     * Detect timezone from UTC offset
     */
    private function detectFromOffset(int $offsetSeconds, ?DateTimeImmutable $datetime = null): array
    {
        $dt = $datetime ?? new DateTimeImmutable('now');
        $candidates = [];
        
        foreach (DateTimeZone::listIdentifiers() as $tzId) {
            $tz = new DateTimeZone($tzId);
            if ($tz->getOffset($dt) === $offsetSeconds) {
                $candidates[] = [
                    'identifier' => $tzId,
                    'name' => $this->getTimezoneName($tzId),
                    'abbreviation' => $dt->setTimezone($tz)->format('T'),
                    'is_dst' => $dt->setTimezone($tz)->format('I') === '1'
                ];
            }
        }

        return [
            'offset_seconds' => $offsetSeconds,
            'offset_formatted' => $this->formatOffset($offsetSeconds),
            'candidates' => $candidates,
            'count' => count($candidates)
        ];
    }

    /**
     * Get business hours information for timezone
     */
    private function getBusinessHours(string $timezone, ?DateTimeImmutable $datetime = null): array
    {
        $tz = $this->parseTimezone($timezone);
        $dt = ($datetime ?? new DateTimeImmutable('now'))->setTimezone($tz);
        
        $businessStart = $dt->setTime(9, 0, 0); // 9 AM
        $businessEnd = $dt->setTime(17, 0, 0);  // 5 PM
        
        return [
            'date' => $dt->format('Y-m-d'),
            'timezone' => $tz->getName(),
            'business_start' => $businessStart->format('H:i:s'),
            'business_end' => $businessEnd->format('H:i:s'),
            'current_time' => $dt->format('H:i:s'),
            'is_business_hours' => $dt >= $businessStart && $dt <= $businessEnd,
            'utc_business_start' => $businessStart->setTimezone(new DateTimeZone('UTC'))->format('Y-m-d H:i:s'),
            'utc_business_end' => $businessEnd->setTimezone(new DateTimeZone('UTC'))->format('Y-m-d H:i:s')
        ];
    }

    /**
     * Find overlapping time window between two timezones
     */
    private function findTimeOverlap(string $timezone1, string $timezone2, ?DateTimeImmutable $datetime = null): array
    {
        $tz1 = $this->parseTimezone($timezone1);
        $tz2 = $this->parseTimezone($timezone2);
        $dt = $datetime ?? new DateTimeImmutable('now');
        
        $business1 = $this->getBusinessHours($timezone1, $dt);
        $business2 = $this->getBusinessHours($timezone2, $dt);
        
        $start1 = new DateTimeImmutable($business1['utc_business_start'], new DateTimeZone('UTC'));
        $end1 = new DateTimeImmutable($business1['utc_business_end'], new DateTimeZone('UTC'));
        $start2 = new DateTimeImmutable($business2['utc_business_start'], new DateTimeZone('UTC'));
        $end2 = new DateTimeImmutable($business2['utc_business_end'], new DateTimeZone('UTC'));
        
        $overlapStart = $start1 > $start2 ? $start1 : $start2;
        $overlapEnd = $end1 < $end2 ? $end1 : $end2;
        
        $hasOverlap = $overlapStart < $overlapEnd;
        
        return [
            'has_overlap' => $hasOverlap,
            'overlap_start_utc' => $hasOverlap ? $overlapStart->format('Y-m-d H:i:s') : null,
            'overlap_end_utc' => $hasOverlap ? $overlapEnd->format('Y-m-d H:i:s') : null,
            'overlap_duration_hours' => $hasOverlap ? ($overlapEnd->getTimestamp() - $overlapStart->getTimestamp()) / 3600 : 0,
            'timezone1_info' => $business1,
            'timezone2_info' => $business2
        ];
    }

    /**
     * World clock showing multiple timezones
     */
    private function worldClock(array $timezones, ?DateTimeImmutable $datetime = null): array
    {
        $dt = $datetime ?? new DateTimeImmutable('now');
        $result = [];
        
        foreach ($timezones as $timezone) {
            try {
                $tz = $this->parseTimezone($timezone);
                $localTime = $dt->setTimezone($tz);
                
                $result[] = [
                    'timezone' => $tz->getName(),
                    'name' => $this->getTimezoneName($tz->getName()),
                    'local_time' => $localTime->format('Y-m-d H:i:s'),
                    'formatted_time' => $localTime->format('g:i A'),
                    'date' => $localTime->format('M j, Y'),
                    'offset' => $localTime->format('P'),
                    'abbreviation' => $localTime->format('T'),
                    'is_dst' => $localTime->format('I') === '1'
                ];
            } catch (\Exception $e) {
                $result[] = [
                    'timezone' => $timezone,
                    'error' => $e->getMessage()
                ];
            }
        }
        
        return [
            'utc_time' => $dt->setTimezone(new DateTimeZone('UTC'))->format('Y-m-d H:i:s'),
            'clocks' => $result
        ];
    }

    /**
     * Batch convert multiple datetimes
     */
    private function batchConvert(array $datetimes, string $fromTz, string $toTz): array
    {
        $results = [];
        
        foreach ($datetimes as $index => $datetime) {
            try {
                $dt = is_string($datetime) ? new DateTimeImmutable($datetime) : $datetime;
                $results[$index] = $this->convertTimezone($dt, $fromTz, $toTz);
            } catch (\Exception $e) {
                $results[$index] = [
                    'error' => $e->getMessage(),
                    'input' => $datetime
                ];
            }
        }
        
        return $results;
    }

    /**
     * Parse timezone string to DateTimeZone object
     */
    private function parseTimezone(string $timezone): DateTimeZone
    {
        if (empty($timezone)) {
            throw new InvalidArgumentException('Timezone cannot be empty');
        }

        // Check common timezone abbreviations
        $normalized = $this->commonTimezones[strtoupper($timezone)] ?? $timezone;
        
        try {
            return new DateTimeZone($normalized);
        } catch (\Exception $e) {
            throw new InvalidArgumentException("Invalid timezone: {$timezone}");
        }
    }

    /**
     * Get human-readable timezone name
     */
    private function getTimezoneName(string $identifier): string
    {
        $parts = explode('/', $identifier);
        if (count($parts) >= 2) {
            return str_replace('_', ' ', end($parts));
        }
        
        return $identifier;
    }

    /**
     * Get next DST transition
     */
    private function getNextTransition(DateTimeZone $tz): ?array
    {
        $transitions = $tz->getTransitions(time(), time() + (365 * 24 * 3600));
        
        foreach ($transitions as $transition) {
            if ($transition['ts'] > time()) {
                return [
                    'datetime' => date('Y-m-d H:i:s', $transition['ts']),
                    'offset' => $transition['offset'],
                    'abbreviation' => $transition['abbr'],
                    'is_dst' => $transition['isdst'],
                    'type' => $transition['isdst'] ? 'dst_start' : 'dst_end'
                ];
            }
        }
        
        return null;
    }

    /**
     * Get DST savings amount
     */
    private function getDSTSavings(DateTimeZone $tz, ?DateTimeImmutable $datetime = null): int
    {
        $dt = $datetime ?? new DateTimeImmutable('now');
        $transitions = $tz->getTransitions($dt->getTimestamp() - 86400, $dt->getTimestamp() + 86400);
        
        $dstOffset = null;
        $standardOffset = null;
        
        foreach ($transitions as $transition) {
            if ($transition['isdst']) {
                $dstOffset = $transition['offset'];
            } else {
                $standardOffset = $transition['offset'];
            }
        }
        
        if ($dstOffset !== null && $standardOffset !== null) {
            return $dstOffset - $standardOffset;
        }
        
        return 0;
    }

    /**
     * Normalize timezone identifier
     */
    private function normalizeTimezone(string $timezone): string
    {
        return $this->commonTimezones[strtoupper($timezone)] ?? $timezone;
    }

    /**
     * Suggest similar timezones for invalid input
     */
    private function suggestTimezones(string $input): array
    {
        $allTimezones = DateTimeZone::listIdentifiers();
        $suggestions = [];
        
        foreach ($allTimezones as $tz) {
            if (stripos($tz, $input) !== false) {
                $suggestions[] = $tz;
                if (count($suggestions) >= 5) break;
            }
        }
        
        return $suggestions;
    }

    /**
     * Format offset seconds as string
     */
    private function formatOffset(int $offsetSeconds): string
    {
        $hours = intval($offsetSeconds / 3600);
        $minutes = abs(intval(($offsetSeconds % 3600) / 60));
        
        $sign = $offsetSeconds >= 0 ? '+' : '-';
        return sprintf('%s%02d:%02d', $sign, abs($hours), $minutes);
    }
} 