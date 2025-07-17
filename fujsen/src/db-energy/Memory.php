<?php
/**
 * <?tusk> TuskPHP Memory - Elephant Never Forgets
 * ===============================================
 * Session and cache management with the wisdom of an elephant
 * "An elephant never forgets, and neither does your application"
 */

namespace TuskPHP;

class Memory {
    
    private static $herd_memory = [];
    private static $long_term_memory = []; // For persistent storage
    
    /**
     * remember() - Store something in elephant memory
     * Like an elephant, we never forget what's important
     */
    public static function remember($key, $value = null, $ttl = 3600) {
        if ($value === null) {
            // Retrieve memory
            return self::recall($key);
        }
        
        // Store in memory with expiration
        self::$herd_memory[$key] = [
            'value' => $value,
            'stored_at' => time(),
            'expires_at' => time() + $ttl,
            'access_count' => 0,
        ];
        
        // Also store in session for persistence
        if (session_status() === PHP_SESSION_ACTIVE) {
            $_SESSION['tusk_memory'][$key] = self::$herd_memory[$key];
        }
        
        return true;
    }
    
    /**
     * recall() - Retrieve from elephant memory
     */
    public static function recall($key) {
        // Check runtime memory first
        if (isset(self::$herd_memory[$key])) {
            $memory = &self::$herd_memory[$key];
            
            // Check if expired
            if (time() > $memory['expires_at']) {
                self::forget($key);
                return null;
            }
            
            $memory['access_count']++;
            return $memory['value'];
        }
        
        // Check session memory
        if (session_status() === PHP_SESSION_ACTIVE && 
            isset($_SESSION['tusk_memory'][$key])) {
            
            $memory = $_SESSION['tusk_memory'][$key];
            
            // Check if expired
            if (time() > $memory['expires_at']) {
                self::forget($key);
                return null;
            }
            
            // Restore to runtime memory
            self::$herd_memory[$key] = $memory;
            self::$herd_memory[$key]['access_count']++;
            $_SESSION['tusk_memory'][$key] = self::$herd_memory[$key];
            
            return $memory['value'];
        }
        
        return null;
    }
    
    /**
     * forget() - Remove from memory (rare for an elephant!)
     */
    public static function forget($key) {
        unset(self::$herd_memory[$key]);
        
        if (session_status() === PHP_SESSION_ACTIVE) {
            unset($_SESSION['tusk_memory'][$key]);
        }
        
        return true;
    }
    
    /**
     * flush() - Clear memory (elephant alzheimer's prevention)
     */
    public static function flush($pattern = null) {
        if ($pattern === null) {
            // Clear all memory
            self::$herd_memory = [];
            if (session_status() === PHP_SESSION_ACTIVE) {
                unset($_SESSION['tusk_memory']);
            }
        } else {
            // Clear matching pattern
            foreach (self::$herd_memory as $key => $memory) {
                if (fnmatch($pattern, $key)) {
                    self::forget($key);
                }
            }
        }
        
        return true;
    }
    
    /**
     * digest() - Process and clean old memories
     */
    public static function digest() {
        $cleaned = 0;
        $now = time();
        
        foreach (self::$herd_memory as $key => $memory) {
            if ($now > $memory['expires_at']) {
                self::forget($key);
                $cleaned++;
            }
        }
        
        // Also clean session memory
        if (session_status() === PHP_SESSION_ACTIVE && 
            isset($_SESSION['tusk_memory'])) {
            
            foreach ($_SESSION['tusk_memory'] as $key => $memory) {
                if ($now > $memory['expires_at']) {
                    unset($_SESSION['tusk_memory'][$key]);
                    $cleaned++;
                }
            }
        }
        
        return $cleaned;
    }
    
    /**
     * wisdom() - Get memory statistics (elephant wisdom)
     */
    public static function wisdom() {
        $stats = [
            'total_memories' => count(self::$herd_memory),
            'session_memories' => 0,
            'expired_count' => 0,
            'most_accessed' => null,
            'oldest_memory' => null,
        ];
        
        if (session_status() === PHP_SESSION_ACTIVE && 
            isset($_SESSION['tusk_memory'])) {
            $stats['session_memories'] = count($_SESSION['tusk_memory']);
        }
        
        $now = time();
        $max_access = 0;
        $oldest_time = $now;
        
        foreach (self::$herd_memory as $key => $memory) {
            if ($now > $memory['expires_at']) {
                $stats['expired_count']++;
            }
            
            if ($memory['access_count'] > $max_access) {
                $max_access = $memory['access_count'];
                $stats['most_accessed'] = $key;
            }
            
            if ($memory['stored_at'] < $oldest_time) {
                $oldest_time = $memory['stored_at'];
                $stats['oldest_memory'] = $key;
            }
        }
        
        return $stats;
    }
    
    /**
     * health() - Check memory health
     */
    public static function health() {
        $health = [
            'status' => 'strong',
            'memory_count' => count(self::$herd_memory),
            'session_active' => session_status() === PHP_SESSION_ACTIVE,
            'last_cleanup' => self::recall('last_digest') ?? 'never',
        ];
        
        // Run cleanup and update health
        $cleaned = self::digest();
        if ($cleaned > 0) {
            $health['last_cleanup'] = date('Y-m-d H:i:s');
            self::remember('last_digest', $health['last_cleanup'], 86400);
        }
        
        return $health;
    }
    
    /**
     * longTermMemory() - Store in persistent database memory
     */
    public static function longTermMemory($key, $value = null, $ttl = 86400) {
        // For database-backed persistent storage
        // This would integrate with the PostgreSQL caching system
        
        if ($value === null) {
            // Retrieve from long-term memory (database)
            return self::recallLongTerm($key);
        }
        
        // Store in database for long-term memory
        // Implementation would use TuskDb for PostgreSQL storage
        
        return true;
    }
    
    /**
     * recallLongTerm() - Retrieve from database memory
     */
    private static function recallLongTerm($key) {
        // Database retrieval logic
        // This would query the PostgreSQL cache tables
        
        return null;
    }
    
    /**
     * migrate() - Move memories around (like elephant migration)
     */
    public static function migrate($from_key, $to_key) {
        $value = self::recall($from_key);
        if ($value !== null) {
            self::remember($to_key, $value);
            self::forget($from_key);
            return true;
        }
        
        return false;
    }
} 