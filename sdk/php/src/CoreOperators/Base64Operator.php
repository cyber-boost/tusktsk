<?php
/**
 * TuskLang Base64 Operator
 * ========================
 * Handles base64 encoding and decoding operations
 */

namespace TuskLang\CoreOperators;

class Base64Operator extends \TuskLang\CoreOperators\BaseOperator
{
    public function getName(): string
    {
        return 'base64';
    }

    protected function executeOperator(array $config, array $context): mixed
    {
        $operation = $config['operation'] ?? null;
        $data = $config['data'] ?? null;

        if ($operation === null) {
            throw new \Exception("Base64 operator requires an 'operation' parameter");
        }

        if ($data === null) {
            throw new \Exception("Base64 operator requires a 'data' parameter");
        }

        switch ($operation) {
            case 'encode':
                return base64_encode($data);

            case 'decode':
                $result = base64_decode($data, true);
                if ($result === false) {
                    throw new \Exception("Invalid base64 data");
                }
                return $result;

            case 'encode_url_safe':
                $encoded = base64_encode($data);
                return strtr($encoded, '+/', '-_');

            case 'decode_url_safe':
                $data = strtr($data, '-_', '+/');
                $result = base64_decode($data, true);
                if ($result === false) {
                    throw new \Exception("Invalid base64 data");
                }
                return $result;

            case 'validate':
                return base64_decode($data, true) !== false;

            default:
                throw new \Exception("Unknown base64 operation: $operation");
        }
    }
} 