<?php
/**
 * TuskLang Hash Operator
 * ======================
 * Handles hashing functions and cryptographic operations
 */

namespace TuskLang\CoreOperators;

class HashOperator extends \TuskLang\CoreOperators\BaseOperator
{
    public function getName(): string
    {
        return 'hash';
    }

    protected function executeOperator(array $config, array $context): mixed
    {
        $algorithm = $config['algorithm'] ?? 'md5';
        $data = $config['data'] ?? null;
        $options = $config['options'] ?? [];

        if ($data === null) {
            throw new \Exception("Hash operator requires a 'data' parameter");
        }

        switch ($algorithm) {
            case 'md5':
                return md5($data);

            case 'sha1':
                return sha1($data);

            case 'sha256':
                return hash('sha256', $data);

            case 'sha512':
                return hash('sha512', $data);

            case 'bcrypt':
                $cost = $options['cost'] ?? 12;
                return password_hash($data, PASSWORD_BCRYPT, ['cost' => $cost]);

            case 'argon2i':
                return password_hash($data, PASSWORD_ARGON2I, $options);

            case 'argon2id':
                return password_hash($data, PASSWORD_ARGON2ID, $options);

            case 'crc32':
                return crc32($data);

            case 'adler32':
                return hash('adler32', $data);

            case 'whirlpool':
                return hash('whirlpool', $data);

            case 'ripemd160':
                return hash('ripemd160', $data);

            case 'tiger192':
                return hash('tiger192,3', $data);

            case 'gost':
                return hash('gost', $data);

            case 'snefru256':
                return hash('snefru256', $data);

            case 'haval256':
                return hash('haval256,5', $data);

            case 'verify':
                $hash = $options['hash'] ?? null;
                if ($hash === null) {
                    throw new \Exception("Hash verify operation requires 'hash' in options");
                }
                return password_verify($data, $hash);

            case 'hmac':
                $key = $options['key'] ?? '';
                $algo = $options['hmac_algo'] ?? 'sha256';
                return hash_hmac($algo, $data, $key);

            case 'pbkdf2':
                $salt = $options['salt'] ?? '';
                $iterations = $options['iterations'] ?? 1000;
                $length = $options['length'] ?? 32;
                $algo = $options['pbkdf2_algo'] ?? 'sha256';
                return hash_pbkdf2($algo, $data, $salt, $iterations, $length);

            default:
                // Try to use the algorithm directly with hash()
                if (in_array($algorithm, hash_algos())) {
                    return hash($algorithm, $data);
                }
                throw new \Exception("Unknown hash algorithm: $algorithm");
        }
    }
} 