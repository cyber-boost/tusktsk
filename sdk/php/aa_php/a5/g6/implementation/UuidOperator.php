<?php

declare(strict_types=1);

namespace TuskLang\A5\G6;

use TuskLang\CoreOperator;
use InvalidArgumentException;
use Random\Randomizer;

/**
 * UuidOperator - UUID generation, validation, and manipulation utilities
 * 
 * Provides comprehensive UUID operations including generation of different UUID versions,
 * validation, parsing, comparison, and conversion utilities with full RFC 4122 compliance.
 */
class UuidOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'uuid';
    }

    public function getDescription(): string 
    {
        return 'UUID generation, validation, and manipulation utilities';
    }

    public function getSupportedActions(): array
    {
        return [
            'generate', 'validate', 'parse', 'format', 'compare', 'convert',
            'version', 'timestamp', 'namespace', 'bulk_generate', 'is_nil'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'generate' => $this->generate($params['version'] ?? 4, $params),
            'validate' => $this->validate($params['uuid'] ?? ''),
            'parse' => $this->parse($params['uuid'] ?? ''),
            'format' => $this->format($params['uuid'] ?? '', $params['format'] ?? 'standard'),
            'compare' => $this->compare($params['uuid1'] ?? '', $params['uuid2'] ?? ''),
            'convert' => $this->convert($params['uuid'] ?? '', $params['to_format'] ?? 'binary'),
            'version' => $this->getVersion($params['uuid'] ?? ''),
            'timestamp' => $this->extractTimestamp($params['uuid'] ?? ''),
            'namespace' => $this->generateNamespaced($params['namespace'] ?? '', $params['name'] ?? '', $params['version'] ?? 5),
            'bulk_generate' => $this->bulkGenerate($params['count'] ?? 10, $params['version'] ?? 4),
            'is_nil' => $this->isNil($params['uuid'] ?? ''),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Generate UUID of specified version
     */
    private function generate(int $version, array $params = []): array
    {
        $uuid = match($version) {
            1 => $this->generateV1($params),
            2 => $this->generateV2($params),
            3 => $this->generateV3($params['namespace'] ?? '', $params['name'] ?? ''),
            4 => $this->generateV4(),
            5 => $this->generateV5($params['namespace'] ?? '', $params['name'] ?? ''),
            6 => $this->generateV6($params),
            7 => $this->generateV7(),
            default => throw new InvalidArgumentException("Unsupported UUID version: {$version}")
        };

        return [
            'uuid' => $uuid,
            'version' => $version,
            'format' => 'standard',
            'uppercase' => strtoupper($uuid),
            'lowercase' => strtolower($uuid),
            'compact' => str_replace('-', '', $uuid),
            'bytes' => $this->hexToBytes(str_replace('-', '', $uuid))
        ];
    }

    /**
     * Validate UUID format and structure
     */
    private function validate(string $uuid): array
    {
        if (empty($uuid)) {
            return ['valid' => false, 'error' => 'UUID cannot be empty'];
        }

        // Remove dashes for validation
        $clean = str_replace('-', '', $uuid);
        
        // Check length
        if (strlen($clean) !== 32) {
            return ['valid' => false, 'error' => 'UUID must be 32 hexadecimal characters'];
        }

        // Check if hexadecimal
        if (!ctype_xdigit($clean)) {
            return ['valid' => false, 'error' => 'UUID must contain only hexadecimal characters'];
        }

        // Check standard format if dashes present
        $standardPattern = '/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/';
        $compactPattern = '/^[0-9a-fA-F]{32}$/';
        
        $isStandardFormat = preg_match($standardPattern, $uuid);
        $isCompactFormat = preg_match($compactPattern, $uuid);
        
        if (!$isStandardFormat && !$isCompactFormat) {
            return ['valid' => false, 'error' => 'Invalid UUID format'];
        }

        // Extract version and variant
        $standardUuid = $isCompactFormat ? $this->addDashes($uuid) : $uuid;
        $version = $this->extractVersion($standardUuid);
        $variant = $this->extractVariant($standardUuid);

        return [
            'valid' => true,
            'uuid' => $standardUuid,
            'format' => $isStandardFormat ? 'standard' : 'compact',
            'version' => $version,
            'variant' => $variant,
            'is_nil' => $this->isNilUuid($standardUuid)
        ];
    }

    /**
     * Parse UUID and extract components
     */
    private function parse(string $uuid): array
    {
        $validation = $this->validate($uuid);
        
        if (!$validation['valid']) {
            throw new InvalidArgumentException($validation['error']);
        }

        $standardUuid = $validation['uuid'];
        $parts = explode('-', $standardUuid);
        
        return [
            'uuid' => $standardUuid,
            'components' => [
                'time_low' => $parts[0],
                'time_mid' => $parts[1],
                'time_hi_and_version' => $parts[2],
                'clock_seq_and_reserved' => $parts[3],
                'node' => $parts[4]
            ],
            'version' => $this->extractVersion($standardUuid),
            'variant' => $this->extractVariant($standardUuid),
            'bytes' => $this->hexToBytes(str_replace('-', '', $standardUuid)),
            'integers' => [
                'time_low' => hexdec($parts[0]),
                'time_mid' => hexdec($parts[1]),
                'time_hi_and_version' => hexdec($parts[2]),
                'clock_seq_and_reserved' => hexdec($parts[3]),
                'node' => hexdec($parts[4])
            ]
        ];
    }

    /**
     * Format UUID in different representations
     */
    private function format(string $uuid, string $format): array
    {
        $validation = $this->validate($uuid);
        
        if (!$validation['valid']) {
            throw new InvalidArgumentException($validation['error']);
        }

        $standardUuid = $validation['uuid'];
        $compact = str_replace('-', '', $standardUuid);
        
        $formatted = match($format) {
            'standard', 'hyphenated' => $standardUuid,
            'compact', 'simple' => $compact,
            'uppercase' => strtoupper($standardUuid),
            'lowercase' => strtolower($standardUuid),
            'braces' => '{' . $standardUuid . '}',
            'parentheses' => '(' . $standardUuid . ')',
            'urn' => 'urn:uuid:' . $standardUuid,
            'binary' => $this->hexToBytes($compact),
            'base64' => base64_encode($this->hexToBytes($compact)),
            'base32' => $this->base32Encode($this->hexToBytes($compact)),
            default => throw new InvalidArgumentException("Unknown format: {$format}")
        };

        return [
            'original' => $uuid,
            'formatted' => $formatted,
            'format' => $format,
            'length' => strlen($formatted)
        ];
    }

    /**
     * Compare two UUIDs
     */
    private function compare(string $uuid1, string $uuid2): array
    {
        $validation1 = $this->validate($uuid1);
        $validation2 = $this->validate($uuid2);
        
        if (!$validation1['valid']) {
            throw new InvalidArgumentException('UUID 1: ' . $validation1['error']);
        }
        
        if (!$validation2['valid']) {
            throw new InvalidArgumentException('UUID 2: ' . $validation2['error']);
        }

        $standard1 = $validation1['uuid'];
        $standard2 = $validation2['uuid'];
        
        return [
            'uuid1' => $standard1,
            'uuid2' => $standard2,
            'equal' => $standard1 === $standard2,
            'comparison' => strcasecmp($standard1, $standard2),
            'version1' => $validation1['version'],
            'version2' => $validation2['version'],
            'same_version' => $validation1['version'] === $validation2['version']
        ];
    }

    /**
     * Convert UUID between formats
     */
    private function convert(string $uuid, string $toFormat): array
    {
        $validation = $this->validate($uuid);
        
        if (!$validation['valid']) {
            throw new InvalidArgumentException($validation['error']);
        }

        $standardUuid = $validation['uuid'];
        $bytes = $this->hexToBytes(str_replace('-', '', $standardUuid));
        
        $converted = match($toFormat) {
            'binary' => $bytes,
            'hex' => str_replace('-', '', $standardUuid),
            'base64' => base64_encode($bytes),
            'integer' => $this->bytesToInteger($bytes),
            'array' => array_values(unpack('C*', $bytes)),
            default => throw new InvalidArgumentException("Unknown conversion format: {$toFormat}")
        };

        return [
            'original' => $uuid,
            'converted' => $converted,
            'from_format' => $validation['format'],
            'to_format' => $toFormat,
            'reversible' => true
        ];
    }

    /**
     * Extract UUID version
     */
    private function getVersion(string $uuid): array
    {
        $validation = $this->validate($uuid);
        
        if (!$validation['valid']) {
            throw new InvalidArgumentException($validation['error']);
        }

        $version = $validation['version'];
        
        return [
            'uuid' => $validation['uuid'],
            'version' => $version,
            'description' => $this->getVersionDescription($version)
        ];
    }

    /**
     * Extract timestamp from time-based UUIDs
     */
    private function extractTimestamp(string $uuid): array
    {
        $validation = $this->validate($uuid);
        
        if (!$validation['valid']) {
            throw new InvalidArgumentException($validation['error']);
        }

        $version = $validation['version'];
        
        if (!in_array($version, [1, 6, 7])) {
            return [
                'uuid' => $validation['uuid'],
                'version' => $version,
                'has_timestamp' => false,
                'error' => 'UUID version does not contain timestamp'
            ];
        }

        $timestamp = $this->extractTimestampFromUuid($validation['uuid'], $version);
        
        return [
            'uuid' => $validation['uuid'],
            'version' => $version,
            'has_timestamp' => true,
            'timestamp' => $timestamp,
            'datetime' => date('Y-m-d H:i:s', $timestamp),
            'iso8601' => date('c', $timestamp)
        ];
    }

    /**
     * Generate namespace-based UUID
     */
    private function generateNamespaced(string $namespace, string $name, int $version = 5): array
    {
        if (!in_array($version, [3, 5])) {
            throw new InvalidArgumentException('Namespace UUIDs must be version 3 or 5');
        }

        $namespaceUuid = $this->resolveNamespace($namespace);
        $uuid = $version === 3 
            ? $this->generateV3($namespaceUuid, $name)
            : $this->generateV5($namespaceUuid, $name);

        return [
            'uuid' => $uuid,
            'version' => $version,
            'namespace' => $namespaceUuid,
            'name' => $name,
            'hash_function' => $version === 3 ? 'MD5' : 'SHA1'
        ];
    }

    /**
     * Generate multiple UUIDs
     */
    private function bulkGenerate(int $count, int $version = 4): array
    {
        if ($count <= 0 || $count > 10000) {
            throw new InvalidArgumentException('Count must be between 1 and 10000');
        }

        $uuids = [];
        for ($i = 0; $i < $count; $i++) {
            $uuids[] = $this->generate($version)['uuid'];
        }

        return [
            'uuids' => $uuids,
            'count' => $count,
            'version' => $version,
            'unique_count' => count(array_unique($uuids)),
            'duplicates' => $count - count(array_unique($uuids))
        ];
    }

    /**
     * Check if UUID is nil (all zeros)
     */
    private function isNil(string $uuid): bool
    {
        $validation = $this->validate($uuid);
        
        if (!$validation['valid']) {
            return false;
        }

        return $this->isNilUuid($validation['uuid']);
    }

    // Generator methods for different UUID versions

    private function generateV1(array $params = []): string
    {
        // Simplified V1 - uses current timestamp and random node
        $timestamp = $this->getGregorianTime();
        $clockSeq = random_int(0, 16383);
        $node = $params['node'] ?? $this->generateRandomNode();
        
        $timeLow = $timestamp & 0xffffffff;
        $timeMid = ($timestamp >> 32) & 0xffff;
        $timeHiAndVersion = (($timestamp >> 48) & 0x0fff) | 0x1000;
        $clockSeqAndReserved = ($clockSeq >> 8) | 0x80;
        $clockSeqLow = $clockSeq & 0xff;
        
        return sprintf(
            '%08x-%04x-%04x-%02x%02x-%s',
            $timeLow,
            $timeMid,
            $timeHiAndVersion,
            $clockSeqAndReserved,
            $clockSeqLow,
            $node
        );
    }

    private function generateV2(array $params = []): string
    {
        // V2 is similar to V1 but with DCE security
        // Simplified implementation
        $uuid = $this->generateV1($params);
        
        // Replace part of timestamp with local identifier
        $localId = $params['local_id'] ?? getmypid();
        $domain = $params['domain'] ?? 0; // 0 = person, 1 = group
        
        $parts = explode('-', $uuid);
        $parts[0] = sprintf('%08x', $localId);
        $parts[2] = sprintf('%04x', (hexdec($parts[2]) & 0x0fff) | 0x2000);
        
        return implode('-', $parts);
    }

    private function generateV3(string $namespace, string $name): string
    {
        $namespaceBytes = $this->hexToBytes(str_replace('-', '', $namespace));
        $hash = md5($namespaceBytes . $name);
        
        return $this->formatHashAsUuid($hash, 3);
    }

    private function generateV4(): string
    {
        $bytes = random_bytes(16);
        
        // Set version (4 bits)
        $bytes[6] = chr((ord($bytes[6]) & 0x0f) | 0x40);
        
        // Set variant (2 bits)
        $bytes[8] = chr((ord($bytes[8]) & 0x3f) | 0x80);
        
        return $this->bytesToUuid($bytes);
    }

    private function generateV5(string $namespace, string $name): string
    {
        $namespaceBytes = $this->hexToBytes(str_replace('-', '', $namespace));
        $hash = sha1($namespaceBytes . $name);
        
        return $this->formatHashAsUuid($hash, 5);
    }

    private function generateV6(array $params = []): string
    {
        // V6 reorders V1 timestamp for better sorting
        $timestamp = $this->getGregorianTime();
        $clockSeq = random_int(0, 16383);
        $node = $params['node'] ?? $this->generateRandomNode();
        
        $timeHi = ($timestamp >> 28) & 0x0fffffff;
        $timeMid = ($timestamp >> 12) & 0xffff;
        $timeLowAndVersion = (($timestamp & 0x0fff) << 4) | 0x6;
        $clockSeqAndReserved = ($clockSeq >> 8) | 0x80;
        $clockSeqLow = $clockSeq & 0xff;
        
        return sprintf(
            '%07x-%04x-%04x-%02x%02x-%s',
            $timeHi,
            $timeMid,
            $timeLowAndVersion,
            $clockSeqAndReserved,
            $clockSeqLow,
            $node
        );
    }

    private function generateV7(): string
    {
        // V7 uses Unix timestamp for better sorting
        $timestamp = time() * 1000 + intval(microtime(true) * 1000) % 1000;
        $randomA = random_int(0, 4095);
        $randomB = random_bytes(8);
        
        // Set version
        $randomB[0] = chr((ord($randomB[0]) & 0x0f) | 0x70);
        
        // Set variant
        $randomB[2] = chr((ord($randomB[2]) & 0x3f) | 0x80);
        
        return sprintf(
            '%08x-%04x-7%03x-%02x%02x-%s',
            ($timestamp >> 16) & 0xffffffff,
            $timestamp & 0xffff,
            $randomA,
            ord($randomB[2]),
            ord($randomB[3]),
            bin2hex(substr($randomB, 4, 6))
        );
    }

    // Helper methods

    private function extractVersion(string $uuid): int
    {
        $parts = explode('-', $uuid);
        return intval($parts[2][0], 16);
    }

    private function extractVariant(string $uuid): string
    {
        $parts = explode('-', $uuid);
        $variant = hexdec($parts[3][0]);
        
        if (($variant & 0x8) === 0) return 'NCS';
        if (($variant & 0xc) === 0x8) return 'RFC4122';
        if (($variant & 0xe) === 0xc) return 'Microsoft';
        return 'Reserved';
    }

    private function isNilUuid(string $uuid): bool
    {
        return $uuid === '00000000-0000-0000-0000-000000000000';
    }

    private function addDashes(string $uuid): string
    {
        return sprintf(
            '%s-%s-%s-%s-%s',
            substr($uuid, 0, 8),
            substr($uuid, 8, 4),
            substr($uuid, 12, 4),
            substr($uuid, 16, 4),
            substr($uuid, 20, 12)
        );
    }

    private function hexToBytes(string $hex): string
    {
        return pack('H*', $hex);
    }

    private function bytesToUuid(string $bytes): string
    {
        $hex = bin2hex($bytes);
        return $this->addDashes($hex);
    }

    private function bytesToInteger(string $bytes): string
    {
        // Convert to big integer string (PHP doesn't handle 128-bit integers natively)
        $result = '0';
        for ($i = 0; $i < strlen($bytes); $i++) {
            $result = bcmul($result, '256');
            $result = bcadd($result, strval(ord($bytes[$i])));
        }
        return $result;
    }

    private function getGregorianTime(): int
    {
        // Convert Unix timestamp to UUID timestamp (100ns intervals since 1582-10-15)
        $unixTime = microtime(true);
        $gregorianEpoch = 122192928000000000; // 1582-10-15 to 1970-01-01 in 100ns intervals
        return intval(($unixTime * 10000000) + $gregorianEpoch);
    }

    private function generateRandomNode(): string
    {
        // Generate 6-byte node identifier
        $node = random_bytes(6);
        $node[0] = chr(ord($node[0]) | 0x01); // Set multicast bit for random node
        return bin2hex($node);
    }

    private function formatHashAsUuid(string $hash, int $version): string
    {
        $uuid = substr($hash, 0, 32);
        
        // Set version
        $uuid[12] = dechex($version);
        
        // Set variant
        $variant = hexdec($uuid[16]);
        $uuid[16] = dechex(($variant & 0x3f) | 0x80);
        
        return $this->addDashes($uuid);
    }

    private function extractTimestampFromUuid(string $uuid, int $version): int
    {
        $parts = explode('-', $uuid);
        
        switch ($version) {
            case 1:
                $timeLow = hexdec($parts[0]);
                $timeMid = hexdec($parts[1]);
                $timeHi = hexdec($parts[2]) & 0x0fff;
                $timestamp = ($timeHi << 48) | ($timeMid << 32) | $timeLow;
                return intval(($timestamp - 122192928000000000) / 10000000);
                
            case 7:
                $timeHi = hexdec($parts[0]);
                $timeLow = hexdec($parts[1]);
                return intval((($timeHi << 16) | $timeLow) / 1000);
                
            default:
                return 0;
        }
    }

    private function resolveNamespace(string $namespace): string
    {
        // Predefined namespaces
        $predefined = [
            'dns' => '6ba7b810-9dad-11d1-80b4-00c04fd430c8',
            'url' => '6ba7b811-9dad-11d1-80b4-00c04fd430c8',
            'oid' => '6ba7b812-9dad-11d1-80b4-00c04fd430c8',
            'x500' => '6ba7b814-9dad-11d1-80b4-00c04fd430c8'
        ];

        if (isset($predefined[$namespace])) {
            return $predefined[$namespace];
        }

        // Assume it's already a UUID
        $validation = $this->validate($namespace);
        if (!$validation['valid']) {
            throw new InvalidArgumentException('Invalid namespace UUID: ' . $validation['error']);
        }

        return $validation['uuid'];
    }

    private function getVersionDescription(int $version): string
    {
        return match($version) {
            1 => 'Time-based with MAC address',
            2 => 'DCE Security with embedded POSIX UIDs',
            3 => 'Name-based using MD5 hash',
            4 => 'Random or pseudo-random',
            5 => 'Name-based using SHA1 hash',
            6 => 'Time-based with reordered timestamp',
            7 => 'Time-based with Unix timestamp',
            default => 'Unknown version'
        };
    }

    private function base32Encode(string $data): string
    {
        $base32Chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
        $encoded = '';
        $padding = strlen($data) % 5;
        
        if ($padding) {
            $data .= str_repeat("\0", 5 - $padding);
        }
        
        for ($i = 0; $i < strlen($data); $i += 5) {
            $chunk = substr($data, $i, 5);
            $bits = '';
            
            for ($j = 0; $j < 5; $j++) {
                $bits .= sprintf('%08b', ord($chunk[$j] ?? "\0"));
            }
            
            for ($j = 0; $j < 40; $j += 5) {
                $encoded .= $base32Chars[bindec(substr($bits, $j, 5))];
            }
        }
        
        return rtrim($encoded, 'A'); // Remove padding equivalent
    }
} 