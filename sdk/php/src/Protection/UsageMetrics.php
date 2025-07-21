<?php

/**
 * TuskLang SDK Protection - Usage Metrics
 * Enterprise-grade usage tracking for PHP SDK
 */

namespace TuskLang\Protection;

class UsageMetrics
{
    private int $startTime;
    private int $apiCalls;
    private int $errors;

    public function __construct()
    {
        $this->startTime = time();
        $this->apiCalls = 0;
        $this->errors = 0;
    }

    public function incrementApiCalls(): void
    {
        $this->apiCalls++;
    }

    public function incrementErrors(): void
    {
        $this->errors++;
    }

    public function getStartTime(): int
    {
        return $this->startTime;
    }

    public function getApiCalls(): int
    {
        return $this->apiCalls;
    }

    public function getErrors(): int
    {
        return $this->errors;
    }
} 