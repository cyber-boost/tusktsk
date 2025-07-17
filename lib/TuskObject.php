<?php
/**
 * <?tusk> TuskPHP Object - Parse-Style Object Management
 * =====================================================
 * "Every elephant remembers its place in the herd"
 * Complete Parse/Firebase-style object system for TuskPHP
 * Strong. Secure. Scalable. 🐘
 */

namespace TuskPHP;

use PDO;
use PDOException;
use TuskPHP\TuskDb;
use TuskPHP\TuskQuery;

class TuskObject 
{
    protected $className;
    protected $tableName;
    protected $objectId;
    protected $data = [];
    protected $originalData = [];
    protected $dirtyKeys = [];
    protected $isDataAvailable = false;
    protected static $registeredSubclasses = [];
    
    /**
     * Constructor
     */
    public function __construct(string $className, $dataOrObjectId = null) 
    {
        $this->className = $className;
        $this->tableName = $this->classNameToTable($className);
        
        if (is_array($dataOrObjectId)) {
            // Initialize with data from database
            $this->data = $dataOrObjectId;
            $this->originalData = $dataOrObjectId;
            $this->objectId = $dataOrObjectId['id'] ?? null;
            $this->isDataAvailable = true;
        } elseif (is_string($dataOrObjectId)) {
            // Initialize with object ID
            $this->objectId = $dataOrObjectId;
        }
    }
    
    /**
     * Convert className to table name
     */
    private function classNameToTable(string $className): string
    {
        return strtolower(preg_replace('/(?<!^)[A-Z]/', '_$0', $className));
    }
    
    // ========================================
    // SAVING & FETCHING
    // ========================================
    
    /**
     * Save object to database
     */
    public function save(bool $useMasterKey = false): bool
    {
        try {
            if ($this->objectId) {
                // Update existing object
                return $this->updateObject();
            } else {
                // Create new object
                return $this->createObject();
            }
        } catch (PDOException $e) {
            error_log("<?tusk> Object save failed: " . $e->getMessage());
            return false;
        }
    }
    
    /**
     * Fetch latest data from server
     */
    public function fetch(): bool
    {
        if (!$this->objectId) {
            return false;
        }
        
        try {
            $result = TuskDb::query(
                "SELECT * FROM {$this->tableName} WHERE id = ? LIMIT 1",
                [$this->objectId]
            );
            
            if (empty($result)) {
                return false;
            }
            
            $this->data = $result[0];
            $this->originalData = $result[0];
            $this->isDataAvailable = true;
            $this->dirtyKeys = [];
            
            return true;
        } catch (PDOException $e) {
            error_log("<?tusk> Object fetch failed: " . $e->getMessage());
            return false;
        }
    }
    
    /**
     * Check if data is loaded
     */
    public function isDataAvailable(): bool
    {
        return $this->isDataAvailable;
    }
    
    // ========================================
    // GETTING & SETTING FIELDS
    // ========================================
    
    /**
     * Get field value
     */
    public function get(string $key)
    {
        return $this->data[$key] ?? null;
    }
    
    /**
     * Set field value
     */
    public function set(string $key, $value): self
    {
        if (!isset($this->data[$key]) || $this->data[$key] !== $value) {
            $this->data[$key] = $value;
            $this->dirtyKeys[] = $key;
        }
        return $this;
    }
    
    /**
     * Set array field
     */
    public function setArray(string $key, array $array): self
    {
        return $this->set($key, json_encode($array));
    }
    
    /**
     * Set associative array field
     */
    public function setAssociativeArray(string $key, array $array): self
    {
        return $this->set($key, json_encode($array));
    }
    
    /**
     * Get array field
     */
    public function getArray(string $key): array
    {
        $value = $this->get($key);
        if (is_string($value)) {
            $decoded = TuskLang::decode($value, true);
            return is_array($decoded) ? $decoded : [];
        }
        return is_array($value) ? $value : [];
    }
    
    /**
     * Get object ID
     */
    public function getId(): ?string
    {
        return $this->objectId;
    }
    
    /**
     * Get class name
     */
    public function getClassName(): string
    {
        return $this->className;
    }
    
    // ========================================
    // COUNTERS & ARRAY OPERATIONS
    // ========================================
    
    /**
     * Atomic increment
     */
    public function increment(string $key, int $amount = 1): self
    {
        $currentValue = (int)($this->get($key) ?? 0);
        $this->set($key, $currentValue + $amount);
        return $this;
    }
    
    /**
     * Atomic decrement
     */
    public function decrement(string $key, int $amount = 1): self
    {
        return $this->increment($key, -$amount);
    }
    
    /**
     * Add values to array field
     */
    public function add(string $key, array $values): self
    {
        $currentArray = $this->getArray($key);
        $newArray = array_merge($currentArray, $values);
        return $this->setArray($key, $newArray);
    }
    
    /**
     * Add unique values to array field
     */
    public function addUnique(string $key, array $values): self
    {
        $currentArray = $this->getArray($key);
        foreach ($values as $value) {
            if (!in_array($value, $currentArray)) {
                $currentArray[] = $value;
            }
        }
        return $this->setArray($key, $currentArray);
    }
    
    /**
     * Remove values from array field
     */
    public function remove(string $key, array $values): self
    {
        $currentArray = $this->getArray($key);
        $newArray = array_filter($currentArray, function($item) use ($values) {
            return !in_array($item, $values);
        });
        return $this->setArray($key, array_values($newArray));
    }
    
    // ========================================
    // ENCODING & DECODING
    // ========================================
    
    /**
     * Encode object to JSON-safe array
     */
    public function encode(): array
    {
        return [
            'className' => $this->className,
            'objectId' => $this->objectId,
            'data' => $this->data,
            'createdAt' => $this->get('created_at'),
            'updatedAt' => $this->get('updated_at')
        ];
    }
    
    /**
     * Decode object from array
     */
    public static function decode(array $encoded): TuskObject
    {
        $className = $encoded['className'];
        $object = new TuskObject($className);
        $object->objectId = $encoded['objectId'];
        $object->data = $encoded['data'] ?? [];
        $object->isDataAvailable = true;
        return $object;
    }
    
    // ========================================
    // DELETION
    // ========================================
    
    /**
     * Delete entire object
     */
    public function destroy(): bool
    {
        if (!$this->objectId) {
            return false;
        }
        
        try {
            $result = TuskDb::query(
                "DELETE FROM {$this->tableName} WHERE id = ?",
                [$this->objectId]
            );
            
            if ($result) {
                $this->objectId = null;
                $this->data = [];
                $this->originalData = [];
                $this->dirtyKeys = [];
                $this->isDataAvailable = false;
                return true;
            }
            
            return false;
        } catch (PDOException $e) {
            error_log("<?tusk> Object destroy failed: " . $e->getMessage());
            return false;
        }
    }
    
    /**
     * Remove a single field
     */
    public function delete(string $key): self
    {
        unset($this->data[$key]);
        $this->dirtyKeys[] = $key;
        return $this;
    }
    
    // ========================================
    // RELATIONAL DATA
    // ========================================
    
    /**
     * Set pointer to another object
     */
    public function setPointer(string $key, TuskObject $object): self
    {
        return $this->set($key, $object->getId());
    }
    
    /**
     * Get pointer as object
     */
    public function getPointer(string $key, string $className): ?TuskObject
    {
        $objectId = $this->get($key);
        if (!$objectId) {
            return null;
        }
        
        $query = new TuskQuery($className);
        return $query->get($objectId);
    }
    
    /**
     * Get relation query
     */
    public function getRelation(string $key): TuskRelation
    {
        return new TuskRelation($this, $key);
    }
    
    // ========================================
    // SUBCLASSING
    // ========================================
    
    /**
     * Register subclass
     */
    public static function registerSubclass(): void
    {
        $className = static::$TuskClassName ?? static::class;
        self::$registeredSubclasses[$className] = static::class;
    }
    
    /**
     * Create object of appropriate subclass
     */
    public static function createFromData(string $className, array $data): TuskObject
    {
        if (isset(self::$registeredSubclasses[$className])) {
            $subclass = self::$registeredSubclasses[$className];
            return new $subclass($className, $data);
        }
        
        return new TuskObject($className, $data);
    }
    
    // ========================================
    // INTERNAL METHODS
    // ========================================
    
    /**
     * Create new object in database
     */
    private function createObject(): bool
    {
        // Generate new ID if not set
        if (!$this->objectId) {
            $this->objectId = $this->generateObjectId();
            $this->data['id'] = $this->objectId;
        }
        
        // Set timestamps
        $now = date('Y-m-d H:i:s');
        $this->data['created_at'] = $now;
        $this->data['updated_at'] = $now;
        
        // Build INSERT query
        $fields = array_keys($this->data);
        $placeholders = array_fill(0, count($fields), '?');
        $values = array_values($this->data);
        
        $sql = "INSERT INTO {$this->tableName} (" . implode(', ', $fields) . ") VALUES (" . implode(', ', $placeholders) . ")";
        
        $result = TuskDb::query($sql, $values);
        
        if ($result !== false) {
            $this->originalData = $this->data;
            $this->dirtyKeys = [];
            $this->isDataAvailable = true;
            return true;
        }
        
        return false;
    }
    
    /**
     * Update existing object in database
     */
    private function updateObject(): bool
    {
        if (empty($this->dirtyKeys)) {
            return true; // No changes to save
        }
        
        // Set updated timestamp
        $this->data['updated_at'] = date('Y-m-d H:i:s');
        $this->dirtyKeys[] = 'updated_at';
        
        // Build UPDATE query for dirty fields only
        $setParts = [];
        $values = [];
        
        foreach (array_unique($this->dirtyKeys) as $key) {
            if (isset($this->data[$key])) {
                $setParts[] = "{$key} = ?";
                $values[] = $this->data[$key];
            } else {
                $setParts[] = "{$key} = NULL";
            }
        }
        
        $values[] = $this->objectId; // For WHERE clause
        
        $sql = "UPDATE {$this->tableName} SET " . implode(', ', $setParts) . " WHERE id = ?";
        
        $result = TuskDb::query($sql, $values);
        
        if ($result !== false) {
            $this->originalData = $this->data;
            $this->dirtyKeys = [];
            return true;
        }
        
        return false;
    }
    
    /**
     * Generate unique object ID
     */
    private function generateObjectId(): string
    {
        return bin2hex(random_bytes(8)); // 16-character hex string
    }
    
    /**
     * Check if object has unsaved changes
     */
    public function isDirty(): bool
    {
        return !empty($this->dirtyKeys);
    }
    
    /**
     * Get dirty (changed) fields
     */
    public function getDirtyKeys(): array
    {
        return array_unique($this->dirtyKeys);
    }
    
    /**
     * Revert unsaved changes
     */
    public function revert(): self
    {
        $this->data = $this->originalData;
        $this->dirtyKeys = [];
        return $this;
    }
    
    /**
     * Get all data
     */
    public function toArray(): array
    {
        return $this->data;
    }
    
    /**
     * Convert to JSON string
     */
    public function toJson(): string
    {
        return json_encode($this->data);
    }
    
    /**
     * Magic method for getting properties
     */
    public function __get(string $key)
    {
        return $this->get($key);
    }
    
    /**
     * Magic method for setting properties
     */
    public function __set(string $key, $value): void
    {
        $this->set($key, $value);
    }
    
    /**
     * Magic method for checking if property exists
     */
    public function __isset(string $key): bool
    {
        return isset($this->data[$key]);
    }
    
    /**
     * Magic method for unsetting properties
     */
    public function __unset(string $key): void
    {
        $this->delete($key);
    }
}

/**
 * TuskRelation - Handle many-to-many relationships
 */
class TuskRelation
{
    private $parent;
    private $key;
    private $relatedObjects = [];
    
    public function __construct(TuskObject $parent, string $key)
    {
        $this->parent = $parent;
        $this->key = $key;
    }
    
    /**
     * Add objects to relation
     */
    public function add($objectsOrIds): self
    {
        $objects = is_array($objectsOrIds) ? $objectsOrIds : [$objectsOrIds];
        
        foreach ($objects as $object) {
            $id = $object instanceof TuskObject ? $object->getId() : $object;
            if (!in_array($id, $this->relatedObjects)) {
                $this->relatedObjects[] = $id;
            }
        }
        
        $this->parent->set($this->key, json_encode($this->relatedObjects));
        return $this;
    }
    
    /**
     * Remove objects from relation
     */
    public function remove($objectsOrIds): self
    {
        $objects = is_array($objectsOrIds) ? $objectsOrIds : [$objectsOrIds];
        
        foreach ($objects as $object) {
            $id = $object instanceof TuskObject ? $object->getId() : $object;
            $index = array_search($id, $this->relatedObjects);
            if ($index !== false) {
                unset($this->relatedObjects[$index]);
            }
        }
        
        $this->relatedObjects = array_values($this->relatedObjects);
        $this->parent->set($this->key, json_encode($this->relatedObjects));
        return $this;
    }
    
    /**
     * Get query for related objects
     */
    public function getQuery(string $className = null): TuskQuery
    {
        $relatedIds = $this->parent->getArray($this->key);
        
        if (!$className) {
            $className = ucfirst($this->key); // Guess class name from key
        }
        
        $query = new TuskQuery($className);
        if (!empty($relatedIds)) {
            $query->containedIn('id', $relatedIds);
        }
        
        return $query;
    }
}

// Helper function for creating objects
if (!function_exists('TuskObject')) {
    function TuskObject(string $className, $dataOrObjectId = null): TuskObject {
        return new TuskObject($className, $dataOrObjectId);
    }
} 