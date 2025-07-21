<?php

declare(strict_types=1);

namespace TuskLang\A5\G4;

use TuskLang\CoreOperator;
use InvalidArgumentException;
use DateTimeImmutable;
use DateInterval;
use DatePeriod;

/**
 * DurationOperator - Duration calculations, intervals, and time difference operations
 * 
 * Provides comprehensive duration operations including calculations, formatting,
 * arithmetic, comparison, and complex interval manipulations.
 */
class DurationOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'duration';
    }

    public function getDescription(): string 
    {
        return 'Duration calculations, intervals, and time difference operations';
    }

    public function getSupportedActions(): array
    {
        return [
            'create', 'parse', 'format', 'add', 'subtract', 'multiply', 'divide',
            'compare', 'total_seconds', 'total_minutes', 'total_hours', 'total_days',
            'humanize', 'to_array', 'between', 'overlap', 'normalize', 'validate'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'create' => $this->createDuration($params['years'] ?? 0, $params['months'] ?? 0, $params['days'] ?? 0, $params['hours'] ?? 0, $params['minutes'] ?? 0, $params['seconds'] ?? 0),
            'parse' => $this->parseDuration($params['duration'] ?? ''),
            'format' => $this->formatDuration($params['interval'] ?? null, $params['format'] ?? null),
            'add' => $this->addDurations($params['interval1'] ?? null, $params['interval2'] ?? null),
            'subtract' => $this->subtractDurations($params['interval1'] ?? null, $params['interval2'] ?? null),
            'multiply' => $this->multiplyDuration($params['interval'] ?? null, $params['factor'] ?? 1),
            'divide' => $this->divideDuration($params['interval'] ?? null, $params['divisor'] ?? 1),
            'compare' => $this->compareDurations($params['interval1'] ?? null, $params['interval2'] ?? null),
            'total_seconds' => $this->totalSeconds($params['interval'] ?? null),
            'total_minutes' => $this->totalMinutes($params['interval'] ?? null),
            'total_hours' => $this->totalHours($params['interval'] ?? null),
            'total_days' => $this->totalDays($params['interval'] ?? null),
            'humanize' => $this->humanizeDuration($params['interval'] ?? null, $params['precision'] ?? 2),
            'to_array' => $this->durationToArray($params['interval'] ?? null),
            'between' => $this->durationBetween($params['start'] ?? null, $params['end'] ?? null, $params['absolute'] ?? true),
            'overlap' => $this->calculateOverlap($params['start1'] ?? null, $params['end1'] ?? null, $params['start2'] ?? null, $params['end2'] ?? null),
            'normalize' => $this->normalizeDuration($params['interval'] ?? null),
            'validate' => $this->validateDuration($params['duration'] ?? ''),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Create duration from individual components
     */
    private function createDuration(int $years = 0, int $months = 0, int $days = 0, int $hours = 0, int $minutes = 0, int $seconds = 0): DateInterval
    {
        $spec = 'P';
        
        if ($years > 0) $spec .= $years . 'Y';
        if ($months > 0) $spec .= $months . 'M';
        if ($days > 0) $spec .= $days . 'D';
        
        if ($hours > 0 || $minutes > 0 || $seconds > 0) {
            $spec .= 'T';
            if ($hours > 0) $spec .= $hours . 'H';
            if ($minutes > 0) $spec .= $minutes . 'M';
            if ($seconds > 0) $spec .= $seconds . 'S';
        }
        
        // Handle empty duration
        if ($spec === 'P') {
            $spec = 'PT0S';
        }

        try {
            return new DateInterval($spec);
        } catch (\Exception $e) {
            throw new InvalidArgumentException("Invalid duration components: " . $e->getMessage());
        }
    }

    /**
     * Parse duration string in various formats
     */
    private function parseDuration(string $duration): DateInterval
    {
        if (empty($duration)) {
            throw new InvalidArgumentException('Duration string cannot be empty');
        }

        // Try ISO 8601 format first
        if (str_starts_with($duration, 'P') || str_starts_with($duration, '-P')) {
            try {
                return new DateInterval(ltrim($duration, '-'));
            } catch (\Exception $e) {
                // Continue to other formats
            }
        }

        // Try human readable formats
        return $this->parseHumanDuration($duration);
    }

    /**
     * Format duration with custom format
     */
    private function formatDuration(?DateInterval $interval, ?string $format = null): string
    {
        if ($interval === null) {
            throw new InvalidArgumentException('Duration interval cannot be null');
        }

        if ($format === null) {
            return $interval->format('%R%Y years, %M months, %D days, %H:%I:%S');
        }

        return $interval->format($format);
    }

    /**
     * Add two durations together
     */
    private function addDurations(?DateInterval $interval1, ?DateInterval $interval2): DateInterval
    {
        if ($interval1 === null || $interval2 === null) {
            throw new InvalidArgumentException('Both intervals must be provided');
        }

        $date = new DateTimeImmutable('@0');
        $result = $date->add($interval1)->add($interval2);
        
        return $date->diff($result);
    }

    /**
     * Subtract one duration from another
     */
    private function subtractDurations(?DateInterval $interval1, ?DateInterval $interval2): DateInterval
    {
        if ($interval1 === null || $interval2 === null) {
            throw new InvalidArgumentException('Both intervals must be provided');
        }

        $date = new DateTimeImmutable('@0');
        $result = $date->add($interval1)->sub($interval2);
        
        return $date->diff($result, false);
    }

    /**
     * Multiply duration by factor
     */
    private function multiplyDuration(?DateInterval $interval, float $factor): DateInterval
    {
        if ($interval === null) {
            throw new InvalidArgumentException('Duration interval cannot be null');
        }

        if ($factor < 0) {
            throw new InvalidArgumentException('Factor must be non-negative');
        }

        $totalSeconds = $this->intervalToSeconds($interval);
        $newSeconds = (int) ($totalSeconds * $factor);
        
        return $this->secondsToInterval($newSeconds);
    }

    /**
     * Divide duration by divisor
     */
    private function divideDuration(?DateInterval $interval, float $divisor): DateInterval
    {
        if ($interval === null) {
            throw new InvalidArgumentException('Duration interval cannot be null');
        }

        if ($divisor <= 0) {
            throw new InvalidArgumentException('Divisor must be positive');
        }

        return $this->multiplyDuration($interval, 1 / $divisor);
    }

    /**
     * Compare two durations
     */
    private function compareDurations(?DateInterval $interval1, ?DateInterval $interval2): array
    {
        if ($interval1 === null || $interval2 === null) {
            throw new InvalidArgumentException('Both intervals must be provided');
        }

        $seconds1 = $this->intervalToSeconds($interval1);
        $seconds2 = $this->intervalToSeconds($interval2);
        
        return [
            'comparison' => $seconds1 <=> $seconds2,
            'is_equal' => $seconds1 === $seconds2,
            'is_greater' => $seconds1 > $seconds2,
            'is_less' => $seconds1 < $seconds2,
            'difference_seconds' => abs($seconds1 - $seconds2)
        ];
    }

    /**
     * Get total seconds in duration
     */
    private function totalSeconds(?DateInterval $interval): int
    {
        if ($interval === null) {
            throw new InvalidArgumentException('Duration interval cannot be null');
        }

        return $this->intervalToSeconds($interval);
    }

    /**
     * Get total minutes in duration
     */
    private function totalMinutes(?DateInterval $interval): float
    {
        return $this->totalSeconds($interval) / 60;
    }

    /**
     * Get total hours in duration
     */
    private function totalHours(?DateInterval $interval): float
    {
        return $this->totalSeconds($interval) / 3600;
    }

    /**
     * Get total days in duration
     */
    private function totalDays(?DateInterval $interval): float
    {
        return $this->totalSeconds($interval) / 86400;
    }

    /**
     * Convert duration to human-readable format
     */
    private function humanizeDuration(?DateInterval $interval, int $precision = 2): string
    {
        if ($interval === null) {
            throw new InvalidArgumentException('Duration interval cannot be null');
        }

        $totalSeconds = $this->intervalToSeconds($interval);
        
        if ($totalSeconds === 0) {
            return '0 seconds';
        }

        $units = [
            'year' => 31536000,
            'month' => 2592000,
            'week' => 604800,
            'day' => 86400,
            'hour' => 3600,
            'minute' => 60,
            'second' => 1
        ];

        $parts = [];
        $remaining = $totalSeconds;

        foreach ($units as $unit => $seconds) {
            if ($remaining >= $seconds) {
                $count = intval($remaining / $seconds);
                $remaining = $remaining % $seconds;
                
                $unitText = $count === 1 ? $unit : $unit . 's';
                $parts[] = "{$count} {$unitText}";
                
                if (count($parts) >= $precision) {
                    break;
                }
            }
        }

        if (empty($parts)) {
            return '0 seconds';
        }

        if (count($parts) === 1) {
            return $parts[0];
        }

        return implode(' ', array_slice($parts, 0, -1)) . ' and ' . end($parts);
    }

    /**
     * Convert duration to array representation
     */
    private function durationToArray(?DateInterval $interval): array
    {
        if ($interval === null) {
            throw new InvalidArgumentException('Duration interval cannot be null');
        }

        return [
            'years' => $interval->y,
            'months' => $interval->m,
            'days' => $interval->d,
            'hours' => $interval->h,
            'minutes' => $interval->i,
            'seconds' => $interval->s,
            'total_seconds' => $this->intervalToSeconds($interval),
            'invert' => $interval->invert === 1,
            'iso8601' => $this->toISO8601($interval)
        ];
    }

    /**
     * Calculate duration between two dates
     */
    private function durationBetween(?DateTimeImmutable $start, ?DateTimeImmutable $end, bool $absolute = true): array
    {
        if ($start === null || $end === null) {
            throw new InvalidArgumentException('Both start and end dates must be provided');
        }

        $interval = $start->diff($end, $absolute);
        
        return [
            'interval' => $interval,
            'duration_array' => $this->durationToArray($interval),
            'total_seconds' => $this->intervalToSeconds($interval),
            'humanized' => $this->humanizeDuration($interval),
            'is_negative' => !$absolute && $end < $start
        ];
    }

    /**
     * Calculate overlap between two time periods
     */
    private function calculateOverlap(?DateTimeImmutable $start1, ?DateTimeImmutable $end1, ?DateTimeImmutable $start2, ?DateTimeImmutable $end2): array
    {
        if ($start1 === null || $end1 === null || $start2 === null || $end2 === null) {
            throw new InvalidArgumentException('All datetime parameters must be provided');
        }

        $overlapStart = $start1 > $start2 ? $start1 : $start2;
        $overlapEnd = $end1 < $end2 ? $end1 : $end2;
        
        $hasOverlap = $overlapStart < $overlapEnd;
        
        $result = [
            'has_overlap' => $hasOverlap,
            'overlap_start' => $hasOverlap ? $overlapStart->format('Y-m-d H:i:s') : null,
            'overlap_end' => $hasOverlap ? $overlapEnd->format('Y-m-d H:i:s') : null
        ];

        if ($hasOverlap) {
            $overlapDuration = $this->durationBetween($overlapStart, $overlapEnd);
            $result['overlap_duration'] = $overlapDuration;
        }

        return $result;
    }

    /**
     * Normalize duration (handle negative components)
     */
    private function normalizeDuration(?DateInterval $interval): DateInterval
    {
        if ($interval === null) {
            throw new InvalidArgumentException('Duration interval cannot be null');
        }

        // Convert to total seconds and back to normalize
        $totalSeconds = $this->intervalToSeconds($interval);
        return $this->secondsToInterval(abs($totalSeconds));
    }

    /**
     * Validate duration string
     */
    private function validateDuration(string $duration): array
    {
        if (empty($duration)) {
            return ['valid' => false, 'error' => 'Duration string is empty'];
        }

        try {
            $this->parseDuration($duration);
            return ['valid' => true, 'normalized' => $this->parseDuration($duration)];
        } catch (\Exception $e) {
            return [
                'valid' => false,
                'error' => $e->getMessage(),
                'input' => $duration
            ];
        }
    }

    /**
     * Parse human-readable duration strings
     */
    private function parseHumanDuration(string $duration): DateInterval
    {
        $duration = strtolower(trim($duration));
        
        // Common patterns
        $patterns = [
            '/(\d+)\s*years?\s*(\d+)\s*months?\s*(\d+)\s*days?\s*(\d+)\s*hours?\s*(\d+)\s*minutes?\s*(\d+)\s*seconds?/' => [1, 2, 3, 4, 5, 6],
            '/(\d+)\s*days?\s*(\d+)\s*hours?\s*(\d+)\s*minutes?\s*(\d+)\s*seconds?/' => [0, 0, 1, 2, 3, 4],
            '/(\d+)\s*hours?\s*(\d+)\s*minutes?\s*(\d+)\s*seconds?/' => [0, 0, 0, 1, 2, 3],
            '/(\d+)\s*minutes?\s*(\d+)\s*seconds?/' => [0, 0, 0, 0, 1, 2],
            '/(\d+)\s*seconds?/' => [0, 0, 0, 0, 0, 1],
            '/(\d+)\s*minutes?/' => [0, 0, 0, 0, 1, 0],
            '/(\d+)\s*hours?/' => [0, 0, 0, 1, 0, 0],
            '/(\d+)\s*days?/' => [0, 0, 1, 0, 0, 0],
            '/(\d+)\s*weeks?/' => [0, 0, 7, 0, 0, 0], // Convert weeks to days
            '/(\d+)\s*months?/' => [0, 1, 0, 0, 0, 0],
            '/(\d+)\s*years?/' => [1, 0, 0, 0, 0, 0],
        ];

        foreach ($patterns as $pattern => $positions) {
            if (preg_match($pattern, $duration, $matches)) {
                $components = [0, 0, 0, 0, 0, 0]; // [years, months, days, hours, minutes, seconds]
                
                for ($i = 0; $i < 6; $i++) {
                    if ($positions[$i] > 0 && isset($matches[$positions[$i]])) {
                        $components[$i] = (int) $matches[$positions[$i]];
                        
                        // Handle weeks conversion
                        if ($i === 2 && $pattern === '/(\d+)\s*weeks?/') {
                            $components[$i] *= 7;
                        }
                    }
                }
                
                return $this->createDuration(...$components);
            }
        }

        throw new InvalidArgumentException("Unable to parse duration: {$duration}");
    }

    /**
     * Convert DateInterval to total seconds
     */
    private function intervalToSeconds(DateInterval $interval): int
    {
        $reference = new DateTimeImmutable('@0');
        $end = $reference->add($interval);
        
        return $end->getTimestamp() - $reference->getTimestamp();
    }

    /**
     * Convert seconds to DateInterval
     */
    private function secondsToInterval(int $seconds): DateInterval
    {
        $reference = new DateTimeImmutable('@0');
        $end = $reference->modify("+{$seconds} seconds");
        
        return $reference->diff($end);
    }

    /**
     * Convert interval to ISO 8601 format
     */
    private function toISO8601(DateInterval $interval): string
    {
        $spec = 'P';
        
        if ($interval->y > 0) $spec .= $interval->y . 'Y';
        if ($interval->m > 0) $spec .= $interval->m . 'M';
        if ($interval->d > 0) $spec .= $interval->d . 'D';
        
        if ($interval->h > 0 || $interval->i > 0 || $interval->s > 0) {
            $spec .= 'T';
            if ($interval->h > 0) $spec .= $interval->h . 'H';
            if ($interval->i > 0) $spec .= $interval->i . 'M';
            if ($interval->s > 0) $spec .= $interval->s . 'S';
        }
        
        if ($spec === 'P') {
            $spec = 'PT0S';
        }
        
        return $spec;
    }
} 