<?php
/**
 * <?tusk> TuskPHP Query Builder - Parse-Style ORM System
 * ====================================================
 * "Every elephant remembers objects, not just tables"
 * Complete Parse/Firebase-style query system for TuskPHP
 * Strong. Secure. Scalable. 🐘
 */

namespace TuskPHP;

use PDO;
use PDOException;
use TuskPHP\TuskDb;
use TuskPHP\TuskObject;

class TuskQuery 
{
    private $className;
    private $tableName;
    private $where = [];
    private $orderBy = [];
    private $limitValue = null;
    private $skipValue = null;
    private $selectFields = null;
    private $includeKeys = [];
    private $excludeKeys = [];
    private $withCount = false;
    private $parameters = [];
    
    /**
     * Constructor - Initialize with className
     */
    public function __construct(string $className) 
    {
        $this->className = $className;
        $this->tableName = $this->classNameToTable($className);
    }
    
    /**
     * Convert className to table name
     */
    private function classNameToTable(string $className): string
    {
        // Convert CamelCase to snake_case for table names
        return strtolower(preg_replace('/(?<!^)[A-Z]/', '_$0', $className));
    }
    
    // ========================================
    // CONSTRUCTION & EXECUTION
    // ========================================
    
    /**
     * Retrieve a single object by ID
     */
    public function get(string $objectId): ?TuskObject
    {
        $result = TuskDb::query(
            "SELECT * FROM {$this->tableName} WHERE id = ? LIMIT 1",
            [$objectId]
        );
        
        if (empty($result)) {
            return null;
        }
        
        return new TuskObject($this->className, $result[0]);
    }
    
    /**
     * Find all matching objects
     */
    public function find(bool $useMasterKey = false): array
    {
        $sql = $this->buildSelectQuery();
        $params = $this->getAllParameters();
        
        $results = TuskDb::query($sql, $params) ?? [];
        
        $objects = [];
        foreach ($results as $row) {
            $objects[] = new TuskObject($this->className, $row);
        }
        
        return $objects;
    }
    
    /**
     * Get the first matching object
     */
    public function first(bool $useMasterKey = false): ?TuskObject
    {
        $originalLimit = $this->limitValue;
        $this->limitValue = 1;
        
        $results = $this->find($useMasterKey);
        $this->limitValue = $originalLimit;
        
        return empty($results) ? null : $results[0];
    }
    
    /**
     * Iterate over all results without limit/skip/sort
     */
    public function each(callable $callback, bool $useMasterKey = false, int $batchSize = 100): void
    {
        $originalLimit = $this->limitValue;
        $originalSkip = $this->skipValue;
        $originalOrder = $this->orderBy;
        
        // Clear pagination and sorting for each()
        $this->limitValue = $batchSize;
        $this->skipValue = 0;
        $this->orderBy = [];
        
        do {
            $batch = $this->find($useMasterKey);
            
            foreach ($batch as $object) {
                call_user_func($callback, $object);
            }
            
            $this->skipValue += $batchSize;
            
        } while (count($batch) === $batchSize);
        
        // Restore original values
        $this->limitValue = $originalLimit;
        $this->skipValue = $originalSkip;
        $this->orderBy = $originalOrder;
    }
    
    /**
     * Get count of matching objects
     */
    public function count(): int
    {
        $sql = "SELECT COUNT(*) as count FROM {$this->tableName}";
        if (!empty($this->where)) {
            $sql .= " WHERE " . $this->buildWhereClause();
        }
        
        $result = TuskDb::query($sql, $this->getAllParameters());
        return $result ? (int)$result[0]['count'] : 0;
    }
    
    /**
     * Get distinct values for a field
     */
    public function distinct(string $field): array
    {
        $sql = "SELECT DISTINCT {$field} FROM {$this->tableName}";
        if (!empty($this->where)) {
            $sql .= " WHERE " . $this->buildWhereClause();
        }
        
        $results = TuskDb::query($sql, $this->getAllParameters()) ?? [];
        return array_column($results, $field);
    }
    
    /**
     * Include total count with results
     */
    public function withCount(bool $enable = true): self
    {
        $this->withCount = $enable;
        return $this;
    }
    
    // ========================================
    // BASIC FILTERS
    // ========================================
    
    /**
     * Equal to condition
     */
    public function equalTo(string $key, $value): self
    {
        $this->addWhere($key, '=', $value);
        return $this;
    }
    
    /**
     * Not equal to condition
     */
    public function notEqualTo(string $key, $value): self
    {
        $this->addWhere($key, '!=', $value);
        return $this;
    }
    
    /**
     * Greater than condition
     */
    public function greaterThan(string $key, $value): self
    {
        $this->addWhere($key, '>', $value);
        return $this;
    }
    
    /**
     * Greater than or equal to condition
     */
    public function greaterThanOrEqualTo(string $key, $value): self
    {
        $this->addWhere($key, '>=', $value);
        return $this;
    }
    
    /**
     * Less than condition
     */
    public function lessThan(string $key, $value): self
    {
        $this->addWhere($key, '<', $value);
        return $this;
    }
    
    /**
     * Less than or equal to condition
     */
    public function lessThanOrEqualTo(string $key, $value): self
    {
        $this->addWhere($key, '<=', $value);
        return $this;
    }
    
    /**
     * Field exists (not null)
     */
    public function exists(string $key): self
    {
        $this->where[] = [
            'sql' => "{$key} IS NOT NULL",
            'params' => []
        ];
        return $this;
    }
    
    /**
     * Field does not exist (is null)
     */
    public function doesNotExist(string $key): self
    {
        $this->where[] = [
            'sql' => "{$key} IS NULL",
            'params' => []
        ];
        return $this;
    }
    
    // ========================================
    // ARRAY & SET FILTERS
    // ========================================
    
    /**
     * Value is contained in array
     */
    public function containedIn(string $key, array $values): self
    {
        $placeholders = array_fill(0, count($values), '?');
        $this->where[] = [
            'sql' => "{$key} IN (" . implode(',', $placeholders) . ")",
            'params' => $values
        ];
        return $this;
    }
    
    /**
     * Value is not contained in array
     */
    public function notContainedIn(string $key, array $values): self
    {
        $placeholders = array_fill(0, count($values), '?');
        $this->where[] = [
            'sql' => "{$key} NOT IN (" . implode(',', $placeholders) . ")",
            'params' => $values
        ];
        return $this;
    }
    
    /**
     * Array field contains all specified values
     */
    public function containsAll(string $key, array $values): self
    {
        // For PostgreSQL JSON arrays
        foreach ($values as $value) {
            $this->where[] = [
                'sql' => "JSON_EXTRACT({$key}, '$') LIKE ?",
                'params' => ['%"' . $value . '"%']
            ];
        }
        return $this;
    }
    
    /**
     * Array field contains all values starting with prefix
     */
    public function containsAllStartingWith(string $key, string $prefix): self
    {
        $this->where[] = [
            'sql' => "JSON_EXTRACT({$key}, '$') LIKE ?",
            'params' => ['%"' . $prefix . '%']
        ];
        return $this;
    }
    
    /**
     * Array field is contained by specified array
     */
    public function containedBy(string $key, array $values): self
    {
        // Implementation depends on database type
        $valueJson = json_encode($values);
        $this->where[] = [
            'sql' => "JSON_CONTAINS(?, {$key})",
            'params' => [$valueJson]
        ];
        return $this;
    }
    
    /**
     * String field contains substring
     */
    public function contains(string $key, string $substr): self
    {
        $this->addWhere($key, 'LIKE', "%{$substr}%");
        return $this;
    }
    
    // ========================================
    // STRING & REGEX
    // ========================================
    
    /**
     * String starts with prefix
     */
    public function startsWith(string $key, string $prefix): self
    {
        $this->addWhere($key, 'LIKE', "{$prefix}%");
        return $this;
    }
    
    /**
     * String ends with suffix
     */
    public function endsWith(string $key, string $suffix): self
    {
        $this->addWhere($key, 'LIKE', "%{$suffix}");
        return $this;
    }
    
    /**
     * Regular expression match
     */
    public function matches(string $key, string $regex, string $modifiers = ''): self
    {
        // Database-specific regex implementation
        $this->where[] = [
            'sql' => "{$key} REGEXP ?",
            'params' => [$regex]
        ];
        return $this;
    }
    
    /**
     * Full-text search
     */
    public function fullText(string $key, string $text): self
    {
        // PostgreSQL full-text search
        $this->where[] = [
            'sql' => "to_tsvector('english', {$key}) @@ plainto_tsquery('english', ?)",
            'params' => [$text]
        ];
        return $this;
    }
    
    // ========================================
    // SUBQUERIES & RELATIONS
    // ========================================
    
    /**
     * Field matches another query
     */
    public function matchesQuery(string $key, TuskQuery $otherQuery): self
    {
        $subSql = $otherQuery->buildSelectQuery();
        $this->where[] = [
            'sql' => "{$key} IN ({$subSql})",
            'params' => $otherQuery->getAllParameters()
        ];
        return $this;
    }
    
    /**
     * Field does not match another query
     */
    public function doesNotMatchQuery(string $key, TuskQuery $otherQuery): self
    {
        $subSql = $otherQuery->buildSelectQuery();
        $this->where[] = [
            'sql' => "{$key} NOT IN ({$subSql})",
            'params' => $otherQuery->getAllParameters()
        ];
        return $this;
    }
    
    /**
     * Field matches key in another query
     */
    public function matchesKeyInQuery(string $key, string $otherKey, TuskQuery $otherQuery): self
    {
        $subSql = "SELECT {$otherKey} FROM {$otherQuery->tableName}";
        if (!empty($otherQuery->where)) {
            $subSql .= " WHERE " . $otherQuery->buildWhereClause();
        }
        
        $this->where[] = [
            'sql' => "{$key} IN ({$subSql})",
            'params' => $otherQuery->getAllParameters()
        ];
        return $this;
    }
    
    /**
     * Field does not match key in another query
     */
    public function doesNotMatchKeyInQuery(string $key, string $otherKey, TuskQuery $otherQuery): self
    {
        $subSql = "SELECT {$otherKey} FROM {$otherQuery->tableName}";
        if (!empty($otherQuery->where)) {
            $subSql .= " WHERE " . $otherQuery->buildWhereClause();
        }
        
        $this->where[] = [
            'sql' => "{$key} NOT IN ({$subSql})",
            'params' => $otherQuery->getAllParameters()
        ];
        return $this;
    }
    
    /**
     * Related to object or ID
     */
    public function relatedTo(string $key, $objectOrId): self
    {
        $id = $objectOrId instanceof TuskObject ? $objectOrId->getId() : $objectOrId;
        return $this->equalTo($key, $id);
    }
    
    // ========================================
    // COMPOUND QUERIES
    // ========================================
    
    /**
     * OR multiple queries
     */
    public static function orQueries(array $queries): TuskQuery
    {
        if (empty($queries)) {
            throw new \InvalidArgumentException('At least one query required for OR operation');
        }
        
        $baseQuery = clone $queries[0];
        $orClauses = [];
        $allParams = [];
        
        foreach ($queries as $query) {
            if (!empty($query->where)) {
                $orClauses[] = '(' . $query->buildWhereClause() . ')';
                $allParams = array_merge($allParams, $query->getAllParameters());
            }
        }
        
        if (!empty($orClauses)) {
            $baseQuery->where = [[
                'sql' => implode(' OR ', $orClauses),
                'params' => $allParams
            ]];
        }
        
        return $baseQuery;
    }
    
    /**
     * AND multiple queries
     */
    public static function andQueries(array $queries): TuskQuery
    {
        if (empty($queries)) {
            throw new \InvalidArgumentException('At least one query required for AND operation');
        }
        
        $baseQuery = clone $queries[0];
        
        // Merge all WHERE conditions
        foreach (array_slice($queries, 1) as $query) {
            $baseQuery->where = array_merge($baseQuery->where, $query->where);
        }
        
        return $baseQuery;
    }
    
    /**
     * NOR multiple queries (none match)
     */
    public static function norQueries(array $queries): TuskQuery
    {
        if (empty($queries)) {
            throw new \InvalidArgumentException('At least one query required for NOR operation');
        }
        
        $baseQuery = clone $queries[0];
        $norClauses = [];
        $allParams = [];
        
        foreach ($queries as $query) {
            if (!empty($query->where)) {
                $norClauses[] = 'NOT (' . $query->buildWhereClause() . ')';
                $allParams = array_merge($allParams, $query->getAllParameters());
            }
        }
        
        if (!empty($norClauses)) {
            $baseQuery->where = [[
                'sql' => implode(' AND ', $norClauses),
                'params' => $allParams
            ]];
        }
        
        return $baseQuery;
    }
    
    // ========================================
    // GEOSPATIAL QUERIES
    // ========================================
    
    /**
     * Near a geographic point
     */
    public function near(string $key, array $geoPoint): self
    {
        $lat = $geoPoint['latitude'] ?? $geoPoint['lat'] ?? $geoPoint[0];
        $lng = $geoPoint['longitude'] ?? $geoPoint['lng'] ?? $geoPoint[1];
        
        $this->orderBy[] = "ST_Distance({$key}, ST_GeomFromText('POINT({$lng} {$lat})', 4326)) ASC";
        return $this;
    }
    
    /**
     * Within geographic bounding box
     */
    public function withinGeoBox(string $key, array $southwestPoint, array $northeastPoint): self
    {
        $swLat = $southwestPoint['latitude'] ?? $southwestPoint['lat'] ?? $southwestPoint[0];
        $swLng = $southwestPoint['longitude'] ?? $southwestPoint['lng'] ?? $southwestPoint[1];
        $neLat = $northeastPoint['latitude'] ?? $northeastPoint['lat'] ?? $northeastPoint[0];
        $neLng = $northeastPoint['longitude'] ?? $northeastPoint['lng'] ?? $northeastPoint[1];
        
        $this->where[] = [
            'sql' => "ST_Within({$key}, ST_GeomFromText('POLYGON(({$swLng} {$swLat}, {$neLng} {$swLat}, {$neLng} {$neLat}, {$swLng} {$neLat}, {$swLng} {$swLat}))', 4326))",
            'params' => []
        ];
        return $this;
    }
    
    /**
     * Within kilometers of point
     */
    public function withinKilometers(string $key, array $point, float $maxKm, bool $sort = true): self
    {
        $lat = $point['latitude'] ?? $point['lat'] ?? $point[0];
        $lng = $point['longitude'] ?? $point['lng'] ?? $point[1];
        
        $this->where[] = [
            'sql' => "ST_DWithin({$key}, ST_GeomFromText('POINT({$lng} {$lat})', 4326), ?)",
            'params' => [$maxKm * 1000] // Convert to meters
        ];
        
        if ($sort) {
            $this->orderBy[] = "ST_Distance({$key}, ST_GeomFromText('POINT({$lng} {$lat})', 4326)) ASC";
        }
        
        return $this;
    }
    
    /**
     * Within miles of point
     */
    public function withinMiles(string $key, array $point, float $maxMiles, bool $sort = true): self
    {
        return $this->withinKilometers($key, $point, $maxMiles * 1.60934, $sort);
    }
    
    /**
     * Within radians of point
     */
    public function withinRadians(string $key, array $point, float $maxRadians, bool $sort = true): self
    {
        $maxKm = $maxRadians * 6371; // Earth's radius in km
        return $this->withinKilometers($key, $point, $maxKm, $sort);
    }
    
    /**
     * Within polygon
     */
    public function withinPolygon(string $key, array $points): self
    {
        $pointStrings = [];
        foreach ($points as $point) {
            $lat = $point['latitude'] ?? $point['lat'] ?? $point[0];
            $lng = $point['longitude'] ?? $point['lng'] ?? $point[1];
            $pointStrings[] = "{$lng} {$lat}";
        }
        
        // Close the polygon
        $pointStrings[] = $pointStrings[0];
        $polygonWkt = 'POLYGON((' . implode(', ', $pointStrings) . '))';
        
        $this->where[] = [
            'sql' => "ST_Within({$key}, ST_GeomFromText(?, 4326))",
            'params' => [$polygonWkt]
        ];
        return $this;
    }
    
    /**
     * Polygon contains point
     */
    public function polygonContains(string $key, array $point): self
    {
        $lat = $point['latitude'] ?? $point['lat'] ?? $point[0];
        $lng = $point['longitude'] ?? $point['lng'] ?? $point[1];
        
        $this->where[] = [
            'sql' => "ST_Contains({$key}, ST_GeomFromText('POINT({$lng} {$lat})', 4326))",
            'params' => []
        ];
        return $this;
    }
    
    // ========================================
    // SORTING, PAGINATION & FIELD SELECTION
    // ========================================
    
    /**
     * Sort ascending by key(s)
     */
    public function ascending($keyOrArray): self
    {
        $keys = is_array($keyOrArray) ? $keyOrArray : [$keyOrArray];
        foreach ($keys as $key) {
            $this->orderBy[] = "{$key} ASC";
        }
        return $this;
    }
    
    /**
     * Add ascending sort by key(s)
     */
    public function addAscending($keyOrArray): self
    {
        return $this->ascending($keyOrArray);
    }
    
    /**
     * Sort descending by key(s)
     */
    public function descending($keyOrArray): self
    {
        $keys = is_array($keyOrArray) ? $keyOrArray : [$keyOrArray];
        foreach ($keys as $key) {
            $this->orderBy[] = "{$key} DESC";
        }
        return $this;
    }
    
    /**
     * Add descending sort by key(s)
     */
    public function addDescending($keyOrArray): self
    {
        return $this->descending($keyOrArray);
    }
    
    /**
     * Limit number of results
     */
    public function limit(int $n): self
    {
        $this->limitValue = $n;
        return $this;
    }
    
    /**
     * Skip number of results
     */
    public function skip(int $n): self
    {
        $this->skipValue = $n;
        return $this;
    }
    
    /**
     * Select specific fields
     */
    public function select($keyOrArray): self
    {
        $this->selectFields = is_array($keyOrArray) ? $keyOrArray : [$keyOrArray];
        return $this;
    }
    
    /**
     * Include nested pointer keys
     */
    public function includeKey($keyOrArray): self
    {
        $keys = is_array($keyOrArray) ? $keyOrArray : [$keyOrArray];
        $this->includeKeys = array_merge($this->includeKeys, $keys);
        return $this;
    }
    
    /**
     * Include all nested pointer keys
     */
    public function includeAllKeys(): self
    {
        $this->includeKeys = ['*'];
        return $this;
    }
    
    /**
     * Exclude specific fields
     */
    public function excludeKey($keyOrArray): self
    {
        $keys = is_array($keyOrArray) ? $keyOrArray : [$keyOrArray];
        $this->excludeKeys = array_merge($this->excludeKeys, $keys);
        return $this;
    }
    
    // ========================================
    // RELATIVE-TIME QUERIES
    // ========================================
    
    /**
     * Greater than relative time
     */
    public function greaterThanRelativeTime(string $key, string $timeString): self
    {
        $timestamp = $this->parseRelativeTime($timeString);
        return $this->greaterThan($key, $timestamp);
    }
    
    /**
     * Greater than or equal to relative time
     */
    public function greaterThanOrEqualToRelativeTime(string $key, string $timeString): self
    {
        $timestamp = $this->parseRelativeTime($timeString);
        return $this->greaterThanOrEqualTo($key, $timestamp);
    }
    
    /**
     * Less than relative time
     */
    public function lessThanRelativeTime(string $key, string $timeString): self
    {
        $timestamp = $this->parseRelativeTime($timeString);
        return $this->lessThan($key, $timestamp);
    }
    
    /**
     * Less than or equal to relative time
     */
    public function lessThanOrEqualToRelativeTime(string $key, string $timeString): self
    {
        $timestamp = $this->parseRelativeTime($timeString);
        return $this->lessThanOrEqualTo($key, $timestamp);
    }
    
    /**
     * Parse relative time strings like "2 weeks ago", "in 1 day", "now"
     */
    private function parseRelativeTime(string $timeString): string
    {
        if ($timeString === 'now') {
            return date('Y-m-d H:i:s');
        }
        
        // Handle "X ago" format
        if (preg_match('/^(.+?)\s+ago$/', $timeString, $matches)) {
            $interval = $matches[1];
            $timestamp = strtotime("-{$interval}");
            return date('Y-m-d H:i:s', $timestamp);
        }
        
        // Handle "in X" format
        if (preg_match('/^in\s+(.+)$/', $timeString, $matches)) {
            $interval = $matches[1];
            $timestamp = strtotime("+{$interval}");
            return date('Y-m-d H:i:s', $timestamp);
        }
        
        // Try to parse as strtotime
        $timestamp = strtotime($timeString);
        if ($timestamp !== false) {
            return date('Y-m-d H:i:s', $timestamp);
        }
        
        throw new \InvalidArgumentException("Cannot parse relative time: {$timeString}");
    }
    
    // ========================================
    // ADVANCED OPTIONS
    // ========================================
    
    /**
     * MongoDB-style aggregation pipeline
     */
    public function aggregate(array $pipeline): array
    {
        // This would need to be implemented based on specific aggregation needs
        // For now, return empty array
        return [];
    }
    
    /**
     * Read preference for queries
     */
    public function readPreference(string $preference, bool $includePref = false, bool $subqueryPref = false): self
    {
        // Implementation would depend on database setup
        return $this;
    }
    
    // ========================================
    // INTERNAL METHODS
    // ========================================
    
    /**
     * Add WHERE condition
     */
    private function addWhere(string $key, string $operator, $value): void
    {
        $this->where[] = [
            'sql' => "{$key} {$operator} ?",
            'params' => [$value]
        ];
    }
    
    /**
     * Build SELECT query
     */
    private function buildSelectQuery(): string
    {
        // SELECT clause
        if ($this->selectFields) {
            $select = implode(', ', $this->selectFields);
        } else {
            $select = '*';
        }
        
        $sql = "SELECT {$select} FROM {$this->tableName}";
        
        // WHERE clause
        if (!empty($this->where)) {
            $sql .= " WHERE " . $this->buildWhereClause();
        }
        
        // ORDER BY clause
        if (!empty($this->orderBy)) {
            $sql .= " ORDER BY " . implode(', ', $this->orderBy);
        }
        
        // LIMIT clause
        if ($this->limitValue !== null) {
            $sql .= " LIMIT {$this->limitValue}";
        }
        
        // OFFSET clause
        if ($this->skipValue !== null) {
            $sql .= " OFFSET {$this->skipValue}";
        }
        
        return $sql;
    }
    
    /**
     * Build WHERE clause
     */
    private function buildWhereClause(): string
    {
        $clauses = [];
        foreach ($this->where as $condition) {
            $clauses[] = $condition['sql'];
        }
        return implode(' AND ', $clauses);
    }
    
    /**
     * Get all parameters for query execution
     */
    private function getAllParameters(): array
    {
        $params = [];
        foreach ($this->where as $condition) {
            $params = array_merge($params, $condition['params']);
        }
        return $params;
    }
}

// Global helper functions
if (!function_exists('Query')) {
    function Query(string $className): TuskQuery {
        return new TuskQuery($className);
    }
} 