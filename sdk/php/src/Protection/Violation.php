<?php

/**
 * TuskLang SDK Protection - Violation Tracking
 * Enterprise-grade violation reporting for PHP SDK
 */

namespace TuskLang\Protection;

class Violation
{
    public int $timestamp;
    public string $sessionId;
    public string $violationType;
    public string $details;
    public string $licenseKeyPartial;

    public function __construct(
        int $timestamp,
        string $sessionId,
        string $violationType,
        string $details,
        string $licenseKeyPartial
    ) {
        $this->timestamp = $timestamp;
        $this->sessionId = $sessionId;
        $this->violationType = $violationType;
        $this->details = $details;
        $this->licenseKeyPartial = $licenseKeyPartial;
    }

    public function __toString(): string
    {
        return sprintf(
            "Violation{timestamp=%d, sessionId='%s', type='%s', details='%s', licenseKeyPartial='%s'}",
            $this->timestamp,
            $this->sessionId,
            $this->violationType,
            $this->details,
            $this->licenseKeyPartial
        );
    }
} 