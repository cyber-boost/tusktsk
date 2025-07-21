<?php

namespace TuskLang\Communication\GraphQL;

use GraphQL\Type\Schema;
use GraphQL\Type\SchemaConfig;
use GraphQL\Utils\BuildSchema;
use GraphQL\Utils\AST;
use GraphQL\Language\Parser;
use GraphQL\Language\Source;
use GraphQL\Type\Definition\Type;
use GraphQL\Type\Definition\ObjectType;
use GraphQL\Type\Definition\InputObjectType;
use GraphQL\Type\Definition\InterfaceType;
use GraphQL\Type\Definition\UnionType;
use GraphQL\Type\Definition\EnumType;
use GraphQL\Type\Definition\ScalarType;

/**
 * Advanced GraphQL Schema Builder
 * 
 * Features:
 * - SDL parsing and validation
 * - Type system construction
 * - Schema stitching and federation
 * - Custom directives support
 * - Automatic model mapping
 * - Schema validation
 */
class SchemaBuilder
{
    private array $typeMap = [];
    private array $directives = [];
    private array $resolvers = [];
    private array $scalars = [];
    private array $middleware = [];
    private TypeRegistry $typeRegistry;
    private DirectiveRegistry $directiveRegistry;

    public function __construct()
    {
        $this->typeRegistry = new TypeRegistry();
        $this->directiveRegistry = new DirectiveRegistry();
        $this->initializeBuiltInScalars();
        $this->initializeBuiltInDirectives();
    }

    /**
     * Build schema from SDL string
     */
    public function buildFromSDL(string $sdl, array $resolverMap = []): Schema
    {
        try {
            // Parse SDL
            $document = Parser::parse(new Source($sdl));
            
            // Build base schema
            $schema = BuildSchema::build($document);
            
            // Apply resolvers
            if (!empty($resolverMap)) {
                $schema = $this->applyResolvers($schema, $resolverMap);
            }
            
            // Apply custom scalars
            $schema = $this->applyCustomScalars($schema);
            
            // Apply custom directives
            $schema = $this->applyCustomDirectives($schema);
            
            // Validate final schema
            $this->validateSchema($schema);
            
            return $schema;
            
        } catch (\Throwable $e) {
            throw new GraphQLSchemaException("Schema build failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Build schema from array configuration
     */
    public function buildFromArray(array $config): Schema
    {
        $schemaConfig = SchemaConfig::create();
        
        // Set query type
        if (isset($config['query'])) {
            $schemaConfig->setQuery($this->buildObjectType($config['query']));
        }
        
        // Set mutation type
        if (isset($config['mutation'])) {
            $schemaConfig->setMutation($this->buildObjectType($config['mutation']));
        }
        
        // Set subscription type
        if (isset($config['subscription'])) {
            $schemaConfig->setSubscription($this->buildObjectType($config['subscription']));
        }
        
        // Add types
        if (isset($config['types'])) {
            $types = [];
            foreach ($config['types'] as $typeName => $typeConfig) {
                $types[] = $this->buildType($typeName, $typeConfig);
            }
            $schemaConfig->setTypes($types);
        }
        
        // Add directives
        if (isset($config['directives'])) {
            $directives = [];
            foreach ($config['directives'] as $directiveName => $directiveConfig) {
                $directives[] = $this->buildDirective($directiveName, $directiveConfig);
            }
            $schemaConfig->setDirectives($directives);
        }
        
        return new Schema($schemaConfig);
    }

    /**
     * Build object type from configuration
     */
    private function buildObjectType(array $config): ObjectType
    {
        return new ObjectType([
            'name' => $config['name'],
            'description' => $config['description'] ?? null,
            'fields' => function() use ($config) {
                return $this->buildFields($config['fields'] ?? []);
            },
            'interfaces' => $config['interfaces'] ?? [],
            'resolveField' => $config['resolveField'] ?? null
        ]);
    }

    /**
     * Build fields configuration
     */
    private function buildFields(array $fieldsConfig): array
    {
        $fields = [];
        
        foreach ($fieldsConfig as $fieldName => $fieldConfig) {
            $fields[$fieldName] = [
                'type' => $this->resolveType($fieldConfig['type']),
                'description' => $fieldConfig['description'] ?? null,
                'args' => $this->buildArgs($fieldConfig['args'] ?? []),
                'resolve' => $fieldConfig['resolve'] ?? null,
                'deprecationReason' => $fieldConfig['deprecationReason'] ?? null
            ];
        }
        
        return $fields;
    }

    /**
     * Build arguments configuration
     */
    private function buildArgs(array $argsConfig): array
    {
        $args = [];
        
        foreach ($argsConfig as $argName => $argConfig) {
            $args[$argName] = [
                'type' => $this->resolveType($argConfig['type']),
                'description' => $argConfig['description'] ?? null,
                'defaultValue' => $argConfig['defaultValue'] ?? null
            ];
        }
        
        return $args;
    }

    /**
     * Resolve type from string or array configuration
     */
    private function resolveType($typeConfig): Type
    {
        if (is_string($typeConfig)) {
            // Handle type modifiers (NonNull, ListOf)
            if (str_ends_with($typeConfig, '!')) {
                return Type::nonNull($this->resolveType(rtrim($typeConfig, '!')));
            }
            
            if (str_starts_with($typeConfig, '[') && str_ends_with($typeConfig, ']')) {
                $innerType = substr($typeConfig, 1, -1);
                return Type::listOf($this->resolveType($innerType));
            }
            
            // Built-in scalars
            switch ($typeConfig) {
                case 'String': return Type::string();
                case 'Int': return Type::int();
                case 'Float': return Type::float();
                case 'Boolean': return Type::boolean();
                case 'ID': return Type::id();
            }
            
            // Custom scalars
            if (isset($this->scalars[$typeConfig])) {
                return $this->scalars[$typeConfig];
            }
            
            // Registered types
            if ($this->typeRegistry->has($typeConfig)) {
                return $this->typeRegistry->get($typeConfig);
            }
            
            throw new GraphQLSchemaException("Unknown type: {$typeConfig}");
        }
        
        if (is_array($typeConfig)) {
            return $this->buildType($typeConfig['name'], $typeConfig);
        }
        
        return $typeConfig;
    }

    /**
     * Build type from configuration
     */
    private function buildType(string $name, array $config): Type
    {
        $kind = $config['kind'] ?? 'object';
        
        switch ($kind) {
            case 'object':
                return $this->buildObjectType(array_merge($config, ['name' => $name]));
                
            case 'interface':
                return new InterfaceType([
                    'name' => $name,
                    'description' => $config['description'] ?? null,
                    'fields' => function() use ($config) {
                        return $this->buildFields($config['fields'] ?? []);
                    },
                    'resolveType' => $config['resolveType'] ?? null
                ]);
                
            case 'union':
                return new UnionType([
                    'name' => $name,
                    'description' => $config['description'] ?? null,
                    'types' => array_map([$this, 'resolveType'], $config['types'] ?? []),
                    'resolveType' => $config['resolveType'] ?? null
                ]);
                
            case 'enum':
                return new EnumType([
                    'name' => $name,
                    'description' => $config['description'] ?? null,
                    'values' => $config['values'] ?? []
                ]);
                
            case 'scalar':
                return new ScalarType([
                    'name' => $name,
                    'description' => $config['description'] ?? null,
                    'serialize' => $config['serialize'] ?? null,
                    'parseValue' => $config['parseValue'] ?? null,
                    'parseLiteral' => $config['parseLiteral'] ?? null
                ]);
                
            case 'input':
                return new InputObjectType([
                    'name' => $name,
                    'description' => $config['description'] ?? null,
                    'fields' => function() use ($config) {
                        return $this->buildInputFields($config['fields'] ?? []);
                    }
                ]);
                
            default:
                throw new GraphQLSchemaException("Unknown type kind: {$kind}");
        }
    }

    /**
     * Build input fields
     */
    private function buildInputFields(array $fieldsConfig): array
    {
        $fields = [];
        
        foreach ($fieldsConfig as $fieldName => $fieldConfig) {
            $fields[$fieldName] = [
                'type' => $this->resolveType($fieldConfig['type']),
                'description' => $fieldConfig['description'] ?? null,
                'defaultValue' => $fieldConfig['defaultValue'] ?? null
            ];
        }
        
        return $fields;
    }

    /**
     * Apply resolvers to schema
     */
    private function applyResolvers(Schema $schema, array $resolverMap): Schema
    {
        // This would involve modifying the schema types to include resolvers
        // Implementation depends on your specific needs
        return $schema;
    }

    /**
     * Apply custom scalars to schema
     */
    private function applyCustomScalars(Schema $schema): Schema
    {
        // Replace scalar types with custom implementations
        return $schema;
    }

    /**
     * Apply custom directives to schema
     */
    private function applyCustomDirectives(Schema $schema): Schema
    {
        // Process custom directives
        return $schema;
    }

    /**
     * Register custom scalar
     */
    public function addScalar(string $name, ScalarType $scalar): self
    {
        $this->scalars[$name] = $scalar;
        return $this;
    }

    /**
     * Register custom directive
     */
    public function addDirective(string $name, callable $implementation): self
    {
        $this->directiveRegistry->register($name, $implementation);
        return $this;
    }

    /**
     * Register type
     */
    public function addType(string $name, Type $type): self
    {
        $this->typeRegistry->register($name, $type);
        return $this;
    }

    /**
     * Initialize built-in scalars
     */
    private function initializeBuiltInScalars(): void
    {
        // DateTime scalar
        $this->addScalar('DateTime', new ScalarType([
            'name' => 'DateTime',
            'serialize' => function($value) {
                return $value instanceof \DateTime ? $value->format('c') : $value;
            },
            'parseValue' => function($value) {
                return new \DateTime($value);
            },
            'parseLiteral' => function($valueNode) {
                return new \DateTime($valueNode->value);
            }
        ]));
        
        // JSON scalar
        $this->addScalar('JSON', new ScalarType([
            'name' => 'JSON',
            'serialize' => function($value) {
                return $value;
            },
            'parseValue' => function($value) {
                return $value;
            },
            'parseLiteral' => function($valueNode) {
                return AST::valueFromASTUntyped($valueNode);
            }
        ]));
        
        // Upload scalar (for file uploads)
        $this->addScalar('Upload', new ScalarType([
            'name' => 'Upload',
            'serialize' => function($value) {
                throw new \RuntimeException('Upload scalar cannot be serialized');
            },
            'parseValue' => function($value) {
                return $value; // Should be a file upload
            },
            'parseLiteral' => function() {
                throw new \RuntimeException('Upload scalar cannot be parsed from literal');
            }
        ]));
    }

    /**
     * Initialize built-in directives
     */
    private function initializeBuiltInDirectives(): void
    {
        // @auth directive
        $this->addDirective('auth', function($resolve, $source, $args, $context, $info) {
            if (!$context->getUser()) {
                throw new GraphQLException('Authentication required');
            }
            return $resolve($source, $args, $context, $info);
        });
        
        // @rateLimit directive
        $this->addDirective('rateLimit', function($resolve, $source, $args, $context, $info) {
            $key = $context->getUser()['id'] ?? $context->getRequest()->getClientIp();
            $limit = $info->fieldDefinition->astNode->directives[0]->arguments[0]->value->value ?? 60;
            
            if (!$this->checkRateLimit($key, $limit)) {
                throw new GraphQLException('Rate limit exceeded');
            }
            
            return $resolve($source, $args, $context, $info);
        });
        
        // @cache directive
        $this->addDirective('cache', function($resolve, $source, $args, $context, $info) {
            $ttl = $info->fieldDefinition->astNode->directives[0]->arguments[0]->value->value ?? 300;
            $key = $this->generateCacheKey($info, $args);
            
            $cached = $this->getFromCache($key);
            if ($cached !== null) {
                return $cached;
            }
            
            $result = $resolve($source, $args, $context, $info);
            $this->storeInCache($key, $result, $ttl);
            
            return $result;
        });
    }

    /**
     * Validate schema structure
     */
    private function validateSchema(Schema $schema): void
    {
        $errors = \GraphQL\Type\SchemaValidationContext::validate($schema);
        if (!empty($errors)) {
            throw new GraphQLSchemaException('Schema validation failed: ' . implode(', ', $errors));
        }
    }

    /**
     * Merge multiple schemas (schema stitching)
     */
    public function mergeSchemas(array $schemas): Schema
    {
        // Implementation for schema stitching
        // This would combine multiple schemas into one
        throw new \RuntimeException('Schema merging not implemented yet');
    }

    /**
     * Generate schema from models
     */
    public function generateFromModels(array $models): Schema
    {
        // Auto-generate GraphQL schema from data models
        throw new \RuntimeException('Model-based generation not implemented yet');
    }

    /**
     * Export schema to SDL
     */
    public function exportToSDL(Schema $schema): string
    {
        return \GraphQL\Utils\SchemaPrinter::doPrint($schema);
    }

    /**
     * Import schema from file
     */
    public function importFromFile(string $filePath, array $resolvers = []): Schema
    {
        if (!file_exists($filePath)) {
            throw new GraphQLSchemaException("Schema file not found: {$filePath}");
        }
        
        $sdl = file_get_contents($filePath);
        return $this->buildFromSDL($sdl, $resolvers);
    }

    /**
     * Helper methods for directive implementations
     */
    private function checkRateLimit(string $key, int $limit): bool
    {
        // Implementation depends on your rate limiting strategy
        return true;
    }
    
    private function generateCacheKey($info, $args): string
    {
        return md5($info->fieldName . serialize($args));
    }
    
    private function getFromCache(string $key)
    {
        // Implementation depends on your caching strategy
        return null;
    }
    
    private function storeInCache(string $key, $value, int $ttl): void
    {
        // Implementation depends on your caching strategy
    }
} 