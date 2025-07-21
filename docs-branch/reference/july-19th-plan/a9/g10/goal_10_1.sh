#!/bin/bash

# MongoDB and Redis Operators Implementation
# Provides NoSQL database operations

# Global variables
MONGODB_URI=""
MONGODB_DATABASE=""
MONGODB_COLLECTION=""
MONGODB_USERNAME=""
MONGODB_PASSWORD=""
MONGODB_HOST="localhost"
MONGODB_PORT="27017"
MONGODB_TIMEOUT="30"

REDIS_HOST="localhost"
REDIS_PORT="6379"
REDIS_PASSWORD=""
REDIS_DATABASE="0"
REDIS_TIMEOUT="30"

# Initialize MongoDB operator
mongodb_init() {
    local uri="$1"
    local database="$2"
    local collection="$3"
    local username="$4"
    local password="$5"
    local host="$6"
    local port="$7"
    
    if [[ -z "$database" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MongoDB database is required\"}"
        return 1
    fi
    
    MONGODB_DATABASE="$database"
    MONGODB_COLLECTION="${collection:-}"
    MONGODB_USERNAME="$username"
    MONGODB_PASSWORD="$password"
    MONGODB_HOST="${host:-localhost}"
    MONGODB_PORT="${port:-27017}"
    
    # Build URI if not provided
    if [[ -z "$uri" ]]; then
        if [[ -n "$username" && -n "$password" ]]; then
            MONGODB_URI="mongodb://${username}:${password}@${MONGODB_HOST}:${MONGODB_PORT}/${database}"
        else
            MONGODB_URI="mongodb://${MONGODB_HOST}:${MONGODB_PORT}/${database}"
        fi
    else
        MONGODB_URI="$uri"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"MongoDB operator initialized\",\"database\":\"$database\",\"host\":\"$MONGODB_HOST\",\"port\":\"$MONGODB_PORT\"}"
}

# MongoDB connection test
mongodb_test_connection() {
    if [[ -z "$MONGODB_URI" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MongoDB not initialized\"}"
        return 1
    fi
    
    # Test connection using mongosh or mongo
    local response=""
    if command -v mongosh >/dev/null 2>&1; then
        response=$(mongosh --quiet --eval "db.runCommand('ping')" "$MONGODB_URI" 2>&1)
    elif command -v mongo >/dev/null 2>&1; then
        response=$(mongo --quiet --eval "db.runCommand('ping')" "$MONGODB_URI" 2>&1)
    else
        echo "{\"status\":\"error\",\"message\":\"MongoDB client (mongosh/mongo) not found\"}"
        return 1
    fi
    
    if [[ $? -eq 0 && "$response" == *"ok"* ]]; then
        echo "{\"status\":\"success\",\"message\":\"MongoDB connection successful\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"MongoDB connection failed\",\"error\":\"$response\"}"
        return 1
    fi
}

# MongoDB insert document
mongodb_insert() {
    local collection="$1"
    local document="$2"
    
    if [[ -z "$document" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Document is required\"}"
        return 1
    fi
    
    if [[ -z "$MONGODB_URI" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MongoDB not initialized\"}"
        return 1
    fi
    
    collection="${collection:-$MONGODB_COLLECTION}"
    if [[ -z "$collection" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Collection is required\"}"
        return 1
    fi
    
    # Insert document
    local response=""
    if command -v mongosh >/dev/null 2>&1; then
        response=$(mongosh --quiet --eval "db.${collection}.insertOne(${document})" "$MONGODB_URI" 2>&1)
    elif command -v mongo >/dev/null 2>&1; then
        response=$(mongo --quiet --eval "db.${collection}.insertOne(${document})" "$MONGODB_URI" 2>&1)
    else
        echo "{\"status\":\"error\",\"message\":\"MongoDB client not found\"}"
        return 1
    fi
    
    if [[ $? -eq 0 ]]; then
        # Extract inserted ID
        local inserted_id=$(echo "$response" | grep -o '"_id" : ObjectId("[^"]*")' | cut -d'"' -f4)
        echo "{\"status\":\"success\",\"message\":\"Document inserted\",\"collection\":\"$collection\",\"inserted_id\":\"$inserted_id\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to insert document\",\"error\":\"$response\"}"
        return 1
    fi
}

# MongoDB find documents
mongodb_find() {
    local collection="$1"
    local query="$2"
    local limit="$3"
    local sort="$4"
    
    if [[ -z "$MONGODB_URI" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MongoDB not initialized\"}"
        return 1
    fi
    
    collection="${collection:-$MONGODB_COLLECTION}"
    if [[ -z "$collection" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Collection is required\"}"
        return 1
    fi
    
    # Build find command
    local find_cmd="db.${collection}.find(${query:-{}})"
    
    if [[ -n "$sort" ]]; then
        find_cmd="${find_cmd}.sort(${sort})"
    fi
    
    if [[ -n "$limit" ]]; then
        find_cmd="${find_cmd}.limit(${limit})"
    fi
    
    find_cmd="${find_cmd}.toArray()"
    
    # Execute find
    local response=""
    if command -v mongosh >/dev/null 2>&1; then
        response=$(mongosh --quiet --eval "$find_cmd" "$MONGODB_URI" 2>&1)
    elif command -v mongo >/dev/null 2>&1; then
        response=$(mongo --quiet --eval "$find_cmd" "$MONGODB_URI" 2>&1)
    else
        echo "{\"status\":\"error\",\"message\":\"MongoDB client not found\"}"
        return 1
    fi
    
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Documents found\",\"collection\":\"$collection\",\"results\":$response}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to find documents\",\"error\":\"$response\"}"
        return 1
    fi
}

# MongoDB update documents
mongodb_update() {
    local collection="$1"
    local query="$2"
    local update="$3"
    local upsert="$4"
    
    if [[ -z "$query" || -z "$update" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Query and update are required\"}"
        return 1
    fi
    
    if [[ -z "$MONGODB_URI" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MongoDB not initialized\"}"
        return 1
    fi
    
    collection="${collection:-$MONGODB_COLLECTION}"
    if [[ -z "$collection" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Collection is required\"}"
        return 1
    fi
    
    # Build update command
    local update_cmd="db.${collection}.updateMany(${query}, ${update}"
    if [[ "$upsert" == "true" ]]; then
        update_cmd="${update_cmd}, {upsert: true}"
    fi
    update_cmd="${update_cmd})"
    
    # Execute update
    local response=""
    if command -v mongosh >/dev/null 2>&1; then
        response=$(mongosh --quiet --eval "$update_cmd" "$MONGODB_URI" 2>&1)
    elif command -v mongo >/dev/null 2>&1; then
        response=$(mongo --quiet --eval "$update_cmd" "$MONGODB_URI" 2>&1)
    else
        echo "{\"status\":\"error\",\"message\":\"MongoDB client not found\"}"
        return 1
    fi
    
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Documents updated\",\"collection\":\"$collection\",\"result\":$response}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to update documents\",\"error\":\"$response\"}"
        return 1
    fi
}

# MongoDB delete documents
mongodb_delete() {
    local collection="$1"
    local query="$2"
    
    if [[ -z "$query" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Query is required\"}"
        return 1
    fi
    
    if [[ -z "$MONGODB_URI" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MongoDB not initialized\"}"
        return 1
    fi
    
    collection="${collection:-$MONGODB_COLLECTION}"
    if [[ -z "$collection" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Collection is required\"}"
        return 1
    fi
    
    # Execute delete
    local delete_cmd="db.${collection}.deleteMany(${query})"
    local response=""
    
    if command -v mongosh >/dev/null 2>&1; then
        response=$(mongosh --quiet --eval "$delete_cmd" "$MONGODB_URI" 2>&1)
    elif command -v mongo >/dev/null 2>&1; then
        response=$(mongo --quiet --eval "$delete_cmd" "$MONGODB_URI" 2>&1)
    else
        echo "{\"status\":\"error\",\"message\":\"MongoDB client not found\"}"
        return 1
    fi
    
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Documents deleted\",\"collection\":\"$collection\",\"result\":$response}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to delete documents\",\"error\":\"$response\"}"
        return 1
    fi
}

# MongoDB count documents
mongodb_count() {
    local collection="$1"
    local query="$2"
    
    if [[ -z "$MONGODB_URI" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MongoDB not initialized\"}"
        return 1
    fi
    
    collection="${collection:-$MONGODB_COLLECTION}"
    if [[ -z "$collection" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Collection is required\"}"
        return 1
    fi
    
    # Execute count
    local count_cmd="db.${collection}.countDocuments(${query:-{}})"
    local response=""
    
    if command -v mongosh >/dev/null 2>&1; then
        response=$(mongosh --quiet --eval "$count_cmd" "$MONGODB_URI" 2>&1)
    elif command -v mongo >/dev/null 2>&1; then
        response=$(mongo --quiet --eval "$count_cmd" "$MONGODB_URI" 2>&1)
    else
        echo "{\"status\":\"error\",\"message\":\"MongoDB client not found\"}"
        return 1
    fi
    
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Document count\",\"collection\":\"$collection\",\"count\":$response}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to count documents\",\"error\":\"$response\"}"
        return 1
    fi
}

# Initialize Redis operator
redis_init() {
    local host="$1"
    local port="$2"
    local password="$3"
    local database="$4"
    
    REDIS_HOST="${host:-localhost}"
    REDIS_PORT="${port:-6379}"
    REDIS_PASSWORD="$password"
    REDIS_DATABASE="${database:-0}"
    
    echo "{\"status\":\"success\",\"message\":\"Redis operator initialized\",\"host\":\"$REDIS_HOST\",\"port\":\"$REDIS_PORT\",\"database\":\"$REDIS_DATABASE\"}"
}

# Redis connection test
redis_test_connection() {
    if ! command -v redis-cli >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"Redis client (redis-cli) not found\"}"
        return 1
    fi
    
    local redis_cmd="redis-cli -h $REDIS_HOST -p $REDIS_PORT"
    if [[ -n "$REDIS_PASSWORD" ]]; then
        redis_cmd="$redis_cmd -a $REDIS_PASSWORD"
    fi
    
    local response=$($redis_cmd ping 2>&1)
    
    if [[ $? -eq 0 && "$response" == "PONG" ]]; then
        echo "{\"status\":\"success\",\"message\":\"Redis connection successful\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Redis connection failed\",\"error\":\"$response\"}"
        return 1
    fi
}

# Redis set key-value
redis_set() {
    local key="$1"
    local value="$2"
    local expiry="$3"
    
    if [[ -z "$key" || -z "$value" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Key and value are required\"}"
        return 1
    fi
    
    if ! command -v redis-cli >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"Redis client not found\"}"
        return 1
    fi
    
    local redis_cmd="redis-cli -h $REDIS_HOST -p $REDIS_PORT -n $REDIS_DATABASE"
    if [[ -n "$REDIS_PASSWORD" ]]; then
        redis_cmd="$redis_cmd -a $REDIS_PASSWORD"
    fi
    
    local set_cmd="SET $key $value"
    if [[ -n "$expiry" ]]; then
        set_cmd="$set_cmd EX $expiry"
    fi
    
    local response=$($redis_cmd $set_cmd 2>&1)
    
    if [[ $? -eq 0 && "$response" == "OK" ]]; then
        echo "{\"status\":\"success\",\"message\":\"Key set successfully\",\"key\":\"$key\",\"value\":\"$value\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to set key\",\"error\":\"$response\"}"
        return 1
    fi
}

# Redis get value
redis_get() {
    local key="$1"
    
    if [[ -z "$key" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Key is required\"}"
        return 1
    fi
    
    if ! command -v redis-cli >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"Redis client not found\"}"
        return 1
    fi
    
    local redis_cmd="redis-cli -h $REDIS_HOST -p $REDIS_PORT -n $REDIS_DATABASE"
    if [[ -n "$REDIS_PASSWORD" ]]; then
        redis_cmd="$redis_cmd -a $REDIS_PASSWORD"
    fi
    
    local response=$($redis_cmd GET "$key" 2>&1)
    
    if [[ $? -eq 0 ]]; then
        if [[ "$response" == "(nil)" ]]; then
            echo "{\"status\":\"warning\",\"message\":\"Key not found\",\"key\":\"$key\"}"
        else
            echo "{\"status\":\"success\",\"message\":\"Value retrieved\",\"key\":\"$key\",\"value\":\"$response\"}"
        fi
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to get key\",\"error\":\"$response\"}"
        return 1
    fi
}

# Redis delete key
redis_delete() {
    local key="$1"
    
    if [[ -z "$key" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Key is required\"}"
        return 1
    fi
    
    if ! command -v redis-cli >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"Redis client not found\"}"
        return 1
    fi
    
    local redis_cmd="redis-cli -h $REDIS_HOST -p $REDIS_PORT -n $REDIS_DATABASE"
    if [[ -n "$REDIS_PASSWORD" ]]; then
        redis_cmd="$redis_cmd -a $REDIS_PASSWORD"
    fi
    
    local response=$($redis_cmd DEL "$key" 2>&1)
    
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Key deleted\",\"key\":\"$key\",\"deleted_count\":$response}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to delete key\",\"error\":\"$response\"}"
        return 1
    fi
}

# Redis list keys
redis_keys() {
    local pattern="$1"
    
    if ! command -v redis-cli >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"Redis client not found\"}"
        return 1
    fi
    
    local redis_cmd="redis-cli -h $REDIS_HOST -p $REDIS_PORT -n $REDIS_DATABASE"
    if [[ -n "$REDIS_PASSWORD" ]]; then
        redis_cmd="$redis_cmd -a $REDIS_PASSWORD"
    fi
    
    local keys_cmd="KEYS ${pattern:-*}"
    local response=$($redis_cmd $keys_cmd 2>&1)
    
    if [[ $? -eq 0 ]]; then
        # Convert response to array
        local keys_array=()
        while IFS= read -r line; do
            if [[ -n "$line" ]]; then
                keys_array+=("$line")
            fi
        done <<< "$response"
        
        echo "{\"status\":\"success\",\"message\":\"Keys retrieved\",\"pattern\":\"${pattern:-*}\",\"count\":${#keys_array[@]},\"keys\":[$(printf '"%s"' "${keys_array[@]}" | tr '\n' ',' | sed 's/,$//')]}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to list keys\",\"error\":\"$response\"}"
        return 1
    fi
}

# Redis hash operations
redis_hset() {
    local key="$1"
    local field="$2"
    local value="$3"
    
    if [[ -z "$key" || -z "$field" || -z "$value" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Key, field, and value are required\"}"
        return 1
    fi
    
    if ! command -v redis-cli >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"Redis client not found\"}"
        return 1
    fi
    
    local redis_cmd="redis-cli -h $REDIS_HOST -p $REDIS_PORT -n $REDIS_DATABASE"
    if [[ -n "$REDIS_PASSWORD" ]]; then
        redis_cmd="$redis_cmd -a $REDIS_PASSWORD"
    fi
    
    local response=$($redis_cmd HSET "$key" "$field" "$value" 2>&1)
    
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Hash field set\",\"key\":\"$key\",\"field\":\"$field\",\"value\":\"$value\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to set hash field\",\"error\":\"$response\"}"
        return 1
    fi
}

# Redis hash get
redis_hget() {
    local key="$1"
    local field="$2"
    
    if [[ -z "$key" || -z "$field" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Key and field are required\"}"
        return 1
    fi
    
    if ! command -v redis-cli >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"Redis client not found\"}"
        return 1
    fi
    
    local redis_cmd="redis-cli -h $REDIS_HOST -p $REDIS_PORT -n $REDIS_DATABASE"
    if [[ -n "$REDIS_PASSWORD" ]]; then
        redis_cmd="$redis_cmd -a $REDIS_PASSWORD"
    fi
    
    local response=$($redis_cmd HGET "$key" "$field" 2>&1)
    
    if [[ $? -eq 0 ]]; then
        if [[ "$response" == "(nil)" ]]; then
            echo "{\"status\":\"warning\",\"message\":\"Hash field not found\",\"key\":\"$key\",\"field\":\"$field\"}"
        else
            echo "{\"status\":\"success\",\"message\":\"Hash field retrieved\",\"key\":\"$key\",\"field\":\"$field\",\"value\":\"$response\"}"
        fi
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to get hash field\",\"error\":\"$response\"}"
        return 1
    fi
}

# Get MongoDB configuration
mongodb_config() {
    if [[ -z "$MONGODB_URI" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MongoDB not initialized\"}"
        return 1
    fi
    
    echo "{\"status\":\"success\",\"config\":{\"uri\":\"$MONGODB_URI\",\"database\":\"$MONGODB_DATABASE\",\"collection\":\"$MONGODB_COLLECTION\",\"host\":\"$MONGODB_HOST\",\"port\":\"$MONGODB_PORT\"}}"
}

# Get Redis configuration
redis_config() {
    echo "{\"status\":\"success\",\"config\":{\"host\":\"$REDIS_HOST\",\"port\":\"$REDIS_PORT\",\"database\":\"$REDIS_DATABASE\",\"password_configured\":\"$([ -n "$REDIS_PASSWORD" ] && echo true || echo false)\"}}"
}

# Main MongoDB operator function
execute_mongodb() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local uri=$(echo "$params" | grep -o 'uri=[^,]*' | cut -d'=' -f2)
            local database=$(echo "$params" | grep -o 'database=[^,]*' | cut -d'=' -f2)
            local collection=$(echo "$params" | grep -o 'collection=[^,]*' | cut -d'=' -f2)
            local username=$(echo "$params" | grep -o 'username=[^,]*' | cut -d'=' -f2)
            local password=$(echo "$params" | grep -o 'password=[^,]*' | cut -d'=' -f2)
            local host=$(echo "$params" | grep -o 'host=[^,]*' | cut -d'=' -f2)
            local port=$(echo "$params" | grep -o 'port=[^,]*' | cut -d'=' -f2)
            mongodb_init "$uri" "$database" "$collection" "$username" "$password" "$host" "$port"
            ;;
        "test")
            mongodb_test_connection
            ;;
        "insert")
            local collection=$(echo "$params" | grep -o 'collection=[^,]*' | cut -d'=' -f2)
            local document=$(echo "$params" | grep -o 'document=[^,]*' | cut -d'=' -f2)
            mongodb_insert "$collection" "$document"
            ;;
        "find")
            local collection=$(echo "$params" | grep -o 'collection=[^,]*' | cut -d'=' -f2)
            local query=$(echo "$params" | grep -o 'query=[^,]*' | cut -d'=' -f2)
            local limit=$(echo "$params" | grep -o 'limit=[^,]*' | cut -d'=' -f2)
            local sort=$(echo "$params" | grep -o 'sort=[^,]*' | cut -d'=' -f2)
            mongodb_find "$collection" "$query" "$limit" "$sort"
            ;;
        "update")
            local collection=$(echo "$params" | grep -o 'collection=[^,]*' | cut -d'=' -f2)
            local query=$(echo "$params" | grep -o 'query=[^,]*' | cut -d'=' -f2)
            local update=$(echo "$params" | grep -o 'update=[^,]*' | cut -d'=' -f2)
            local upsert=$(echo "$params" | grep -o 'upsert=[^,]*' | cut -d'=' -f2)
            mongodb_update "$collection" "$query" "$update" "$upsert"
            ;;
        "delete")
            local collection=$(echo "$params" | grep -o 'collection=[^,]*' | cut -d'=' -f2)
            local query=$(echo "$params" | grep -o 'query=[^,]*' | cut -d'=' -f2)
            mongodb_delete "$collection" "$query"
            ;;
        "count")
            local collection=$(echo "$params" | grep -o 'collection=[^,]*' | cut -d'=' -f2)
            local query=$(echo "$params" | grep -o 'query=[^,]*' | cut -d'=' -f2)
            mongodb_count "$collection" "$query"
            ;;
        "config")
            mongodb_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, test, insert, find, update, delete, count, config\"}"
            return 1
            ;;
    esac
}

# Main Redis operator function
execute_redis() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local host=$(echo "$params" | grep -o 'host=[^,]*' | cut -d'=' -f2)
            local port=$(echo "$params" | grep -o 'port=[^,]*' | cut -d'=' -f2)
            local password=$(echo "$params" | grep -o 'password=[^,]*' | cut -d'=' -f2)
            local database=$(echo "$params" | grep -o 'database=[^,]*' | cut -d'=' -f2)
            redis_init "$host" "$port" "$password" "$database"
            ;;
        "test")
            redis_test_connection
            ;;
        "set")
            local key=$(echo "$params" | grep -o 'key=[^,]*' | cut -d'=' -f2)
            local value=$(echo "$params" | grep -o 'value=[^,]*' | cut -d'=' -f2)
            local expiry=$(echo "$params" | grep -o 'expiry=[^,]*' | cut -d'=' -f2)
            redis_set "$key" "$value" "$expiry"
            ;;
        "get")
            local key=$(echo "$params" | grep -o 'key=[^,]*' | cut -d'=' -f2)
            redis_get "$key"
            ;;
        "delete")
            local key=$(echo "$params" | grep -o 'key=[^,]*' | cut -d'=' -f2)
            redis_delete "$key"
            ;;
        "keys")
            local pattern=$(echo "$params" | grep -o 'pattern=[^,]*' | cut -d'=' -f2)
            redis_keys "$pattern"
            ;;
        "hset")
            local key=$(echo "$params" | grep -o 'key=[^,]*' | cut -d'=' -f2)
            local field=$(echo "$params" | grep -o 'field=[^,]*' | cut -d'=' -f2)
            local value=$(echo "$params" | grep -o 'value=[^,]*' | cut -d'=' -f2)
            redis_hset "$key" "$field" "$value"
            ;;
        "hget")
            local key=$(echo "$params" | grep -o 'key=[^,]*' | cut -d'=' -f2)
            local field=$(echo "$params" | grep -o 'field=[^,]*' | cut -d'=' -f2)
            redis_hget "$key" "$field"
            ;;
        "config")
            redis_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, test, set, get, delete, keys, hset, hget, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_mongodb execute_redis mongodb_init mongodb_test_connection mongodb_insert mongodb_find mongodb_update mongodb_delete mongodb_count redis_init redis_test_connection redis_set redis_get redis_delete redis_keys redis_hset redis_hget mongodb_config redis_config 