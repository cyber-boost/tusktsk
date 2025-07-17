# Cache Directives in TuskLang - Bash Guide

## 🗄️ **Revolutionary Caching Configuration**

Cache directives in TuskLang transform your configuration files into intelligent caching systems. No more separate cache configurations or complex cache management - everything lives in your TuskLang configuration with dynamic cache strategies, automatic invalidation, and intelligent performance optimization.

> **"We don't bow to any king"** - TuskLang cache directives break free from traditional caching constraints and bring modern cache capabilities to your Bash applications.

## 🚀 **Core Cache Directives**

### **Basic Cache Setup**
```bash
#cache: redis                    # Cache backend
#cache-backend: redis            # Alternative syntax
#cache-ttl: 3600                 # Default TTL (seconds)
#cache-prefix: tusk:             # Cache key prefix
#cache-compression: gzip         # Cache compression
#cache-encryption: AES-256-GCM   # Cache encryption
#cache-persistence: true         # Enable cache persistence
```

### **Advanced Cache Configuration**
```bash
#cache-strategy: lru             # Cache eviction strategy
#cache-max-size: 1GB             # Maximum cache size
#cache-max-keys: 10000           # Maximum number of keys
#cache-write-through: true       # Write-through caching
#cache-write-behind: false       # Write-behind caching
#cache-distributed: true         # Distributed caching
#cache-cluster: redis-cluster    # Cache cluster configuration
#cache-monitoring: true          # Enable cache monitoring
```

## 🔧 **Bash Cache Implementation**

### **Basic Cache Manager**
```bash
#!/bin/bash

# Load cache configuration
source <(tsk load cache.tsk)

# Cache configuration
CACHE_BACKEND="${cache_backend:-file}"
CACHE_TTL="${cache_ttl:-3600}"
CACHE_PREFIX="${cache_prefix:-tusk:}"
CACHE_COMPRESSION="${cache_compression:-none}"
CACHE_ENCRYPTION="${cache_encryption:-none}"
CACHE_PERSISTENCE="${cache_persistence:-true}"

# Cache manager
class CacheManager {
    constructor() {
        this.backend = CACHE_BACKEND
        this.ttl = CACHE_TTL
        this.prefix = CACHE_PREFIX
        this.compression = CACHE_COMPRESSION
        this.encryption = CACHE_ENCRYPTION
        this.persistence = CACHE_PERSISTENCE
        this.stats = {
            hits: 0,
            misses: 0,
            sets: 0,
            deletes: 0
        }
    }
    
    set(key, value, ttl = null) {
        const fullKey = this.prefix + key
        const expiration = ttl ? Date.now() + ttl * 1000 : Date.now() + this.ttl * 1000
        
        const cacheEntry = {
            value: value,
            expires: expiration,
            created: Date.now(),
            accessed: Date.now()
        }
        
        // Apply compression if enabled
        if (this.compression !== 'none') {
            cacheEntry.value = this.compress(cacheEntry.value)
        }
        
        // Apply encryption if enabled
        if (this.encryption !== 'none') {
            cacheEntry.value = this.encrypt(cacheEntry.value)
        }
        
        this.saveToBackend(fullKey, cacheEntry)
        this.stats.sets++
        
        return true
    }
    
    get(key) {
        const fullKey = this.prefix + key
        const cacheEntry = this.loadFromBackend(fullKey)
        
        if (!cacheEntry) {
            this.stats.misses++
            return null
        }
        
        // Check expiration
        if (Date.now() > cacheEntry.expires) {
            this.delete(key)
            this.stats.misses++
            return null
        }
        
        // Update access time
        cacheEntry.accessed = Date.now()
        this.saveToBackend(fullKey, cacheEntry)
        
        let value = cacheEntry.value
        
        // Apply decryption if enabled
        if (this.encryption !== 'none') {
            value = this.decrypt(value)
        }
        
        // Apply decompression if enabled
        if (this.compression !== 'none') {
            value = this.decompress(value)
        }
        
        this.stats.hits++
        return value
    }
    
    delete(key) {
        const fullKey = this.prefix + key
        this.removeFromBackend(fullKey)
        this.stats.deletes++
        return true
    }
    
    exists(key) {
        const fullKey = this.prefix + key
        const cacheEntry = this.loadFromBackend(fullKey)
        
        if (!cacheEntry) {
            return false
        }
        
        if (Date.now() > cacheEntry.expires) {
            this.delete(key)
            return false
        }
        
        return true
    }
    
    clear() {
        this.clearBackend()
        this.stats = { hits: 0, misses: 0, sets: 0, deletes: 0 }
    }
    
    getStats() {
        return {
            ...this.stats,
            hitRate: this.stats.hits / (this.stats.hits + this.stats.misses) * 100
        }
    }
    
    compress(data) {
        switch (this.compression) {
            case 'gzip':
                return this.gzipCompress(data)
            case 'deflate':
                return this.deflateCompress(data)
            default:
                return data
        }
    }
    
    decompress(data) {
        switch (this.compression) {
            case 'gzip':
                return this.gzipDecompress(data)
            case 'deflate':
                return this.deflateDecompress(data)
            default:
                return data
        }
    }
    
    encrypt(data) {
        switch (this.encryption) {
            case 'AES-256-GCM':
                return this.aesEncrypt(data)
            default:
                return data
        }
    }
    
    decrypt(data) {
        switch (this.encryption) {
            case 'AES-256-GCM':
                return this.aesDecrypt(data)
            default:
                return data
        }
    }
}

# Initialize cache manager
const cacheManager = new CacheManager()
```

### **Redis Cache Backend**
```bash
#!/bin/bash

# Redis cache backend implementation
redis_cache_backend() {
    local operation="$1"
    local key="$2"
    local value="$3"
    local ttl="$4"
    
    # Redis configuration
    local redis_host="${redis_host:-localhost}"
    local redis_port="${redis_port:-6379}"
    local redis_db="${redis_db:-0}"
    local redis_password="${redis_password:-}"
    
    case "$operation" in
        "set")
            redis_set "$key" "$value" "$ttl"
            ;;
        "get")
            redis_get "$key"
            ;;
        "delete")
            redis_delete "$key"
            ;;
        "exists")
            redis_exists "$key"
            ;;
        "clear")
            redis_clear
            ;;
        *)
            echo "Unknown Redis operation: $operation"
            return 1
            ;;
    esac
}

redis_set() {
    local key="$1"
    local value="$2"
    local ttl="$3"
    
    local redis_cmd="redis-cli -h $redis_host -p $redis_port"
    
    if [[ -n "$redis_password" ]]; then
        redis_cmd="$redis_cmd -a $redis_password"
    fi
    
    if [[ -n "$redis_db" ]]; then
        redis_cmd="$redis_cmd -n $redis_db"
    fi
    
    if [[ -n "$ttl" ]]; then
        $redis_cmd SETEX "$key" "$ttl" "$value"
    else
        $redis_cmd SET "$key" "$value"
    fi
}

redis_get() {
    local key="$1"
    
    local redis_cmd="redis-cli -h $redis_host -p $redis_port"
    
    if [[ -n "$redis_password" ]]; then
        redis_cmd="$redis_cmd -a $redis_password"
    fi
    
    if [[ -n "$redis_db" ]]; then
        redis_cmd="$redis_cmd -n $redis_db"
    fi
    
    $redis_cmd GET "$key"
}

redis_delete() {
    local key="$1"
    
    local redis_cmd="redis-cli -h $redis_host -p $redis_port"
    
    if [[ -n "$redis_password" ]]; then
        redis_cmd="$redis_cmd -a $redis_password"
    fi
    
    if [[ -n "$redis_db" ]]; then
        redis_cmd="$redis_cmd -n $redis_db"
    fi
    
    $redis_cmd DEL "$key"
}

redis_exists() {
    local key="$1"
    
    local redis_cmd="redis-cli -h $redis_host -p $redis_port"
    
    if [[ -n "$redis_password" ]]; then
        redis_cmd="$redis_cmd -a $redis_password"
    fi
    
    if [[ -n "$redis_db" ]]; then
        redis_cmd="$redis_cmd -n $redis_db"
    fi
    
    local result=$($redis_cmd EXISTS "$key")
    if [[ "$result" -eq 1 ]]; then
        return 0
    else
        return 1
    fi
}

redis_clear() {
    local redis_cmd="redis-cli -h $redis_host -p $redis_port"
    
    if [[ -n "$redis_password" ]]; then
        redis_cmd="$redis_cmd -a $redis_password"
    fi
    
    if [[ -n "$redis_db" ]]; then
        redis_cmd="$redis_cmd -n $redis_db"
    fi
    
    $redis_cmd FLUSHDB
}
```

### **File Cache Backend**
```bash
#!/bin/bash

# File cache backend implementation
file_cache_backend() {
    local operation="$1"
    local key="$2"
    local value="$3"
    local ttl="$4"
    
    # File cache configuration
    local cache_dir="${cache_dir:-/tmp/tusk-cache}"
    local cache_file="$cache_dir/$key.json"
    
    case "$operation" in
        "set")
            file_cache_set "$cache_file" "$value" "$ttl"
            ;;
        "get")
            file_cache_get "$cache_file"
            ;;
        "delete")
            file_cache_delete "$cache_file"
            ;;
        "exists")
            file_cache_exists "$cache_file"
            ;;
        "clear")
            file_cache_clear "$cache_dir"
            ;;
        *)
            echo "Unknown file cache operation: $operation"
            return 1
            ;;
    esac
}

file_cache_set() {
    local cache_file="$1"
    local value="$2"
    local ttl="$3"
    
    # Create cache directory
    mkdir -p "$(dirname "$cache_file")"
    
    # Calculate expiration time
    local expiration=$(( $(date +%s) + ttl ))
    
    # Create cache entry
    local cache_entry=$(cat << EOF
{
    "value": "$value",
    "expires": $expiration,
    "created": $(date +%s),
    "accessed": $(date +%s)
}
EOF
)
    
    # Write to file
    echo "$cache_entry" > "$cache_file"
}

file_cache_get() {
    local cache_file="$1"
    
    if [[ ! -f "$cache_file" ]]; then
        return 1
    fi
    
    # Read cache entry
    local cache_entry=$(cat "$cache_file")
    local expiration=$(echo "$cache_entry" | jq -r '.expires')
    local current_time=$(date +%s)
    
    # Check expiration
    if [[ "$current_time" -gt "$expiration" ]]; then
        rm -f "$cache_file"
        return 1
    fi
    
    # Update access time
    local updated_entry=$(echo "$cache_entry" | jq --arg accessed "$current_time" '.accessed = ($accessed | tonumber)')
    echo "$updated_entry" > "$cache_file"
    
    # Return value
    echo "$cache_entry" | jq -r '.value'
}

file_cache_delete() {
    local cache_file="$1"
    
    if [[ -f "$cache_file" ]]; then
        rm -f "$cache_file"
    fi
}

file_cache_exists() {
    local cache_file="$1"
    
    if [[ -f "$cache_file" ]]; then
        local cache_entry=$(cat "$cache_file")
        local expiration=$(echo "$cache_entry" | jq -r '.expires')
        local current_time=$(date +%s)
        
        if [[ "$current_time" -le "$expiration" ]]; then
            return 0
        else
            rm -f "$cache_file"
        fi
    fi
    
    return 1
}

file_cache_clear() {
    local cache_dir="$1"
    
    if [[ -d "$cache_dir" ]]; then
        rm -rf "$cache_dir"/*
    fi
}
```

### **Memory Cache Backend**
```bash
#!/bin/bash

# Memory cache backend implementation
memory_cache_backend() {
    local operation="$1"
    local key="$2"
    local value="$3"
    local ttl="$4"
    
    # Use associative array for memory storage
    declare -gA MEMORY_CACHE
    declare -gA MEMORY_CACHE_EXPIRY
    
    case "$operation" in
        "set")
            memory_cache_set "$key" "$value" "$ttl"
            ;;
        "get")
            memory_cache_get "$key"
            ;;
        "delete")
            memory_cache_delete "$key"
            ;;
        "exists")
            memory_cache_exists "$key"
            ;;
        "clear")
            memory_cache_clear
            ;;
        *)
            echo "Unknown memory cache operation: $operation"
            return 1
            ;;
    esac
}

memory_cache_set() {
    local key="$1"
    local value="$2"
    local ttl="$3"
    
    # Calculate expiration time
    local expiration=$(( $(date +%s) + ttl ))
    
    # Store in memory
    MEMORY_CACHE["$key"]="$value"
    MEMORY_CACHE_EXPIRY["$key"]="$expiration"
}

memory_cache_get() {
    local key="$1"
    
    # Check if key exists
    if [[ -z "${MEMORY_CACHE[$key]}" ]]; then
        return 1
    fi
    
    # Check expiration
    local expiration="${MEMORY_CACHE_EXPIRY[$key]}"
    local current_time=$(date +%s)
    
    if [[ "$current_time" -gt "$expiration" ]]; then
        memory_cache_delete "$key"
        return 1
    fi
    
    # Return value
    echo "${MEMORY_CACHE[$key]}"
}

memory_cache_delete() {
    local key="$1"
    
    unset MEMORY_CACHE["$key"]
    unset MEMORY_CACHE_EXPIRY["$key"]
}

memory_cache_exists() {
    local key="$1"
    
    if [[ -n "${MEMORY_CACHE[$key]}" ]]; then
        local expiration="${MEMORY_CACHE_EXPIRY[$key]}"
        local current_time=$(date +%s)
        
        if [[ "$current_time" -le "$expiration" ]]; then
            return 0
        else
            memory_cache_delete "$key"
        fi
    fi
    
    return 1
}

memory_cache_clear() {
    MEMORY_CACHE=()
    MEMORY_CACHE_EXPIRY=()
}
```

### **Cache Compression**
```bash
#!/bin/bash

# Cache compression implementation
cache_compression() {
    local operation="$1"
    local data="$2"
    local algorithm="$3"
    
    case "$algorithm" in
        "gzip")
            if [[ "$operation" == "compress" ]]; then
                gzip_compress "$data"
            else
                gzip_decompress "$data"
            fi
            ;;
        "deflate")
            if [[ "$operation" == "compress" ]]; then
                deflate_compress "$data"
            else
                deflate_decompress "$data"
            fi
            ;;
        "brotli")
            if [[ "$operation" == "compress" ]]; then
                brotli_compress "$data"
            else
                brotli_decompress "$data"
            fi
            ;;
        *)
            echo "$data"
            ;;
    esac
}

gzip_compress() {
    local data="$1"
    echo "$data" | gzip -c | base64
}

gzip_decompress() {
    local data="$1"
    echo "$data" | base64 -d | gzip -d
}

deflate_compress() {
    local data="$1"
    echo "$data" | gzip -c -n | base64
}

deflate_decompress() {
    local data="$1"
    echo "$data" | base64 -d | gzip -d
}

brotli_compress() {
    local data="$1"
    if command -v brotli >/dev/null 2>&1; then
        echo "$data" | brotli -c | base64
    else
        echo "$data"
    fi
}

brotli_decompress() {
    local data="$1"
    if command -v brotli >/dev/null 2>&1; then
        echo "$data" | base64 -d | brotli -d
    else
        echo "$data"
    fi
}
```

### **Cache Encryption**
```bash
#!/bin/bash

# Cache encryption implementation
cache_encryption() {
    local operation="$1"
    local data="$2"
    local algorithm="$3"
    local key="$4"
    
    case "$algorithm" in
        "AES-256-GCM")
            if [[ "$operation" == "encrypt" ]]; then
                aes_encrypt "$data" "$key"
            else
                aes_decrypt "$data" "$key"
            fi
            ;;
        "ChaCha20-Poly1305")
            if [[ "$operation" == "encrypt" ]]; then
                chacha20_encrypt "$data" "$key"
            else
                chacha20_decrypt "$data" "$key"
            fi
            ;;
        *)
            echo "$data"
            ;;
    esac
}

aes_encrypt() {
    local data="$1"
    local key="$2"
    
    # Generate random IV
    local iv=$(openssl rand -hex 16)
    
    # Encrypt data
    local encrypted=$(echo -n "$data" | openssl enc -aes-256-gcm -a -A -K "$key" -iv "$iv" 2>/dev/null)
    
    if [[ $? -eq 0 ]]; then
        echo "$iv:$encrypted"
    else
        echo "$data"
    fi
}

aes_decrypt() {
    local encrypted_data="$1"
    local key="$2"
    
    # Split IV and encrypted data
    IFS=':' read -r iv encrypted <<< "$encrypted_data"
    
    # Decrypt data
    local decrypted=$(echo "$encrypted" | openssl enc -aes-256-gcm -a -A -d -K "$key" -iv "$iv" 2>/dev/null)
    
    if [[ $? -eq 0 ]]; then
        echo "$decrypted"
    else
        echo "$encrypted_data"
    fi
}

chacha20_encrypt() {
    local data="$1"
    local key="$2"
    
    # Generate random nonce
    local nonce=$(openssl rand -hex 12)
    
    # Encrypt data (simplified - in production use proper ChaCha20-Poly1305)
    local encrypted=$(echo -n "$data" | openssl enc -chacha20 -a -A -K "$key" -iv "$nonce" 2>/dev/null)
    
    if [[ $? -eq 0 ]]; then
        echo "$nonce:$encrypted"
    else
        echo "$data"
    fi
}

chacha20_decrypt() {
    local encrypted_data="$1"
    local key="$2"
    
    # Split nonce and encrypted data
    IFS=':' read -r nonce encrypted <<< "$encrypted_data"
    
    # Decrypt data
    local decrypted=$(echo "$encrypted" | openssl enc -chacha20 -a -A -d -K "$key" -iv "$nonce" 2>/dev/null)
    
    if [[ $? -eq 0 ]]; then
        echo "$decrypted"
    else
        echo "$encrypted_data"
    fi
}
```

## 🎯 **Real-World Configuration Examples**

### **Complete Cache Configuration**
```bash
# cache-config.tsk
cache_config:
  backend: redis
  ttl: 3600
  prefix: tusk:
  compression: gzip
  encryption: AES-256-GCM
  persistence: true

#cache: redis
#cache-backend: redis
#cache-ttl: 3600
#cache-prefix: tusk:
#cache-compression: gzip
#cache-encryption: AES-256-GCM
#cache-persistence: true

#cache-strategy: lru
#cache-max-size: 1GB
#cache-max-keys: 10000
#cache-write-through: true
#cache-write-behind: false
#cache-distributed: true
#cache-cluster: redis-cluster
#cache-monitoring: true

#cache-config:
#  redis:
#    host: localhost
#    port: 6379
#    db: 0
#    password: "${REDIS_PASSWORD}"
#    timeout: 5
#    retry_attempts: 3
#  compression:
#    algorithm: gzip
#    level: 6
#    min_size: 1024
#  encryption:
#    algorithm: AES-256-GCM
#    key: "${CACHE_ENCRYPTION_KEY}"
#  persistence:
#    enabled: true
#    backup_interval: 3600
#    backup_retention: 7
```

### **Distributed Cache Configuration**
```bash
# distributed-cache.tsk
distributed_config:
  enabled: true
  cluster: redis-cluster
  replication: true
  sharding: true

#cache: redis-cluster
#cache-backend: redis-cluster
#cache-distributed: true
#cache-cluster: redis-cluster
#cache-replication: true
#cache-sharding: true

#cache-config:
#  cluster:
#    nodes:
#      - host: redis-node1
#        port: 6379
#      - host: redis-node2
#        port: 6379
#      - host: redis-node3
#        port: 6379
#    replication_factor: 2
#    sharding_strategy: consistent_hashing
#  monitoring:
#    enabled: true
#    metrics: true
#    alerts: true
```

### **Application-Specific Cache**
```bash
# app-cache.tsk
app_config:
  name: myapp
  environment: production

#cache: redis
#cache-backend: redis
#cache-ttl: 1800
#cache-prefix: myapp:

#cache-config:
#  user_sessions:
#    ttl: 3600
#    prefix: session:
#    compression: gzip
#  api_responses:
#    ttl: 300
#    prefix: api:
#    compression: gzip
#  database_queries:
#    ttl: 600
#    prefix: db:
#    compression: none
#  static_assets:
#    ttl: 86400
#    prefix: assets:
#    compression: brotli
```

## 🚨 **Troubleshooting Cache Directives**

### **Common Issues and Solutions**

**1. Cache Connection Issues**
```bash
# Debug cache connection
debug_cache_connection() {
    local backend="$1"
    
    echo "Debugging cache connection for backend: $backend"
    
    case "$backend" in
        "redis")
            debug_redis_connection
            ;;
        "file")
            debug_file_cache
            ;;
        "memory")
            debug_memory_cache
            ;;
        *)
            echo "Unknown cache backend: $backend"
            ;;
    esac
}

debug_redis_connection() {
    echo "Testing Redis connection..."
    
    local redis_host="${redis_host:-localhost}"
    local redis_port="${redis_port:-6379}"
    local redis_password="${redis_password:-}"
    
    local redis_cmd="redis-cli -h $redis_host -p $redis_port"
    
    if [[ -n "$redis_password" ]]; then
        redis_cmd="$redis_cmd -a $redis_password"
    fi
    
    # Test connection
    if $redis_cmd ping >/dev/null 2>&1; then
        echo "✓ Redis connection successful"
        
        # Test basic operations
        $redis_cmd SET "test_key" "test_value" >/dev/null 2>&1
        if [[ $? -eq 0 ]]; then
            echo "✓ Redis write operation successful"
        else
            echo "✗ Redis write operation failed"
        fi
        
        local result=$($redis_cmd GET "test_key")
        if [[ "$result" == "test_value" ]]; then
            echo "✓ Redis read operation successful"
        else
            echo "✗ Redis read operation failed"
        fi
        
        $redis_cmd DEL "test_key" >/dev/null 2>&1
    else
        echo "✗ Redis connection failed"
        echo "  Host: $redis_host"
        echo "  Port: $redis_port"
        echo "  Password: $([[ -n "$redis_password" ]] && echo "Set" || echo "Not set")"
    fi
}

debug_file_cache() {
    echo "Testing file cache..."
    
    local cache_dir="${cache_dir:-/tmp/tusk-cache}"
    
    if [[ -d "$cache_dir" ]]; then
        echo "✓ Cache directory exists: $cache_dir"
        
        if [[ -w "$cache_dir" ]]; then
            echo "✓ Cache directory writable"
        else
            echo "✗ Cache directory not writable"
        fi
    else
        echo "✗ Cache directory does not exist: $cache_dir"
        
        # Try to create directory
        if mkdir -p "$cache_dir" 2>/dev/null; then
            echo "✓ Created cache directory: $cache_dir"
        else
            echo "✗ Failed to create cache directory: $cache_dir"
        fi
    fi
    
    # Test file operations
    local test_file="$cache_dir/test.json"
    local test_data='{"test": "value"}'
    
    if echo "$test_data" > "$test_file" 2>/dev/null; then
        echo "✓ File write operation successful"
        
        if [[ -f "$test_file" ]]; then
            echo "✓ File read operation successful"
            rm -f "$test_file"
        else
            echo "✗ File read operation failed"
        fi
    else
        echo "✗ File write operation failed"
    fi
}

debug_memory_cache() {
    echo "Testing memory cache..."
    
    # Test memory operations
    declare -A test_cache
    
    test_cache["test_key"]="test_value"
    
    if [[ "${test_cache[test_key]}" == "test_value" ]]; then
        echo "✓ Memory cache operations successful"
    else
        echo "✗ Memory cache operations failed"
    fi
    
    unset test_cache
}
```

**2. Cache Performance Issues**
```bash
# Debug cache performance
debug_cache_performance() {
    local backend="$1"
    
    echo "Debugging cache performance for backend: $backend"
    
    # Test cache operations with timing
    local start_time=$(date +%s%N)
    
    case "$backend" in
        "redis")
            test_redis_performance
            ;;
        "file")
            test_file_performance
            ;;
        "memory")
            test_memory_performance
            ;;
    esac
    
    local end_time=$(date +%s%N)
    local duration=$(( (end_time - start_time) / 1000000 ))
    
    echo "Cache performance test completed in ${duration}ms"
}

test_redis_performance() {
    echo "Testing Redis performance..."
    
    local redis_cmd="redis-cli -h ${redis_host:-localhost} -p ${redis_port:-6379}"
    
    # Test write performance
    local write_start=$(date +%s%N)
    for i in {1..100}; do
        $redis_cmd SET "perf_test_$i" "value_$i" >/dev/null 2>&1
    done
    local write_end=$(date +%s%N)
    local write_duration=$(( (write_end - write_start) / 1000000 ))
    echo "  Write performance: 100 operations in ${write_duration}ms"
    
    # Test read performance
    local read_start=$(date +%s%N)
    for i in {1..100}; do
        $redis_cmd GET "perf_test_$i" >/dev/null 2>&1
    done
    local read_end=$(date +%s%N)
    local read_duration=$(( (read_end - read_start) / 1000000 ))
    echo "  Read performance: 100 operations in ${read_duration}ms"
    
    # Cleanup
    for i in {1..100}; do
        $redis_cmd DEL "perf_test_$i" >/dev/null 2>&1
    done
}

test_file_performance() {
    echo "Testing file cache performance..."
    
    local cache_dir="${cache_dir:-/tmp/tusk-cache}"
    mkdir -p "$cache_dir"
    
    # Test write performance
    local write_start=$(date +%s%N)
    for i in {1..100}; do
        echo "{\"value\": \"value_$i\"}" > "$cache_dir/perf_test_$i.json"
    done
    local write_end=$(date +%s%N)
    local write_duration=$(( (write_end - write_start) / 1000000 ))
    echo "  Write performance: 100 operations in ${write_duration}ms"
    
    # Test read performance
    local read_start=$(date +%s%N)
    for i in {1..100}; do
        cat "$cache_dir/perf_test_$i.json" >/dev/null 2>&1
    done
    local read_end=$(date +%s%N)
    local read_duration=$(( (read_end - read_start) / 1000000 ))
    echo "  Read performance: 100 operations in ${read_duration}ms"
    
    # Cleanup
    rm -f "$cache_dir"/perf_test_*.json
}

test_memory_performance() {
    echo "Testing memory cache performance..."
    
    declare -A test_cache
    
    # Test write performance
    local write_start=$(date +%s%N)
    for i in {1..100}; do
        test_cache["perf_test_$i"]="value_$i"
    done
    local write_end=$(date +%s%N)
    local write_duration=$(( (write_end - write_start) / 1000000 ))
    echo "  Write performance: 100 operations in ${write_duration}ms"
    
    # Test read performance
    local read_start=$(date +%s%N)
    for i in {1..100}; do
        local value="${test_cache[perf_test_$i]}"
    done
    local read_end=$(date +%s%N)
    local read_duration=$(( (read_end - read_start) / 1000000 ))
    echo "  Read performance: 100 operations in ${read_duration}ms"
    
    unset test_cache
}
```

## 🔒 **Security Best Practices**

### **Cache Security Checklist**
```bash
# Security validation
validate_cache_security() {
    echo "Validating cache security configuration..."
    
    # Check encryption
    if [[ -n "$CACHE_ENCRYPTION" ]] && [[ "$CACHE_ENCRYPTION" != "none" ]]; then
        echo "✓ Cache encryption enabled: $CACHE_ENCRYPTION"
        
        if [[ -n "${cache_encryption_key}" ]]; then
            echo "✓ Encryption key configured"
        else
            echo "⚠ Encryption key not configured"
        fi
    else
        echo "⚠ Cache encryption not enabled"
    fi
    
    # Check Redis security
    if [[ "$CACHE_BACKEND" == "redis" ]]; then
        if [[ -n "${redis_password}" ]]; then
            echo "✓ Redis password configured"
        else
            echo "⚠ Redis password not configured"
        fi
        
        if [[ "${redis_ssl}" == "true" ]]; then
            echo "✓ Redis SSL enabled"
        else
            echo "⚠ Redis SSL not enabled"
        fi
    fi
    
    # Check file permissions
    if [[ "$CACHE_BACKEND" == "file" ]]; then
        local cache_dir="${cache_dir:-/tmp/tusk-cache}"
        if [[ -d "$cache_dir" ]]; then
            local perms=$(stat -c %a "$cache_dir")
            if [[ "$perms" == "700" ]]; then
                echo "✓ Cache directory permissions secure: $perms"
            else
                echo "⚠ Cache directory permissions should be 700, got: $perms"
            fi
        fi
    fi
    
    # Check cache prefix
    if [[ -n "$CACHE_PREFIX" ]]; then
        echo "✓ Cache prefix configured: $CACHE_PREFIX"
    else
        echo "⚠ Cache prefix not configured"
    fi
}
```

## 📈 **Performance Optimization Tips**

### **Cache Performance Checklist**
```bash
# Performance validation
validate_cache_performance() {
    echo "Validating cache performance configuration..."
    
    # Check TTL settings
    if [[ -n "$CACHE_TTL" ]]; then
        echo "✓ Cache TTL configured: ${CACHE_TTL}s"
        
        if [[ "$CACHE_TTL" -gt 86400 ]]; then
            echo "⚠ Long cache TTL may impact memory usage"
        fi
    else
        echo "⚠ Cache TTL not configured"
    fi
    
    # Check compression
    if [[ -n "$CACHE_COMPRESSION" ]] && [[ "$CACHE_COMPRESSION" != "none" ]]; then
        echo "✓ Cache compression enabled: $CACHE_COMPRESSION"
    else
        echo "⚠ Cache compression not enabled"
    fi
    
    # Check backend performance
    case "$CACHE_BACKEND" in
        "memory")
            echo "✓ Memory cache backend (fastest)"
            ;;
        "redis")
            echo "✓ Redis cache backend (high performance)"
            ;;
        "file")
            echo "⚠ File cache backend (slower, persistent)"
            ;;
    esac
    
    # Check monitoring
    if [[ "${cache_monitoring}" == "true" ]]; then
        echo "✓ Cache monitoring enabled"
    else
        echo "⚠ Cache monitoring not enabled"
    fi
}
```

## 🎯 **Next Steps**

- **Rate Limit Directives**: Learn about rate limiting-specific directives
- **Plugin Integration**: Explore cache plugins
- **Advanced Patterns**: Understand complex cache patterns
- **Testing Cache Directives**: Test cache functionality
- **Performance Tuning**: Optimize cache performance

---

**Cache directives transform your TuskLang configuration into a powerful caching system. They bring modern cache capabilities to your Bash applications with intelligent caching strategies, compression, encryption, and comprehensive performance optimization!** 