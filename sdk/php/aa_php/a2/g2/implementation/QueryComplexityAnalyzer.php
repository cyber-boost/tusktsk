<?php

namespace TuskLang\Communication\GraphQL;

use GraphQL\Language\AST\DocumentNode;
use GraphQL\Language\AST\FieldNode;
use GraphQL\Language\AST\FragmentDefinitionNode;
use GraphQL\Language\AST\InlineFragmentNode;
use GraphQL\Language\AST\SelectionSetNode;
use GraphQL\Language\Visitor;

/**
 * Query Complexity Analyzer
 * 
 * Analyzes GraphQL queries to prevent DoS attacks through:
 * - Query complexity scoring
 * - Depth analysis
 * - Field counting
 * - Fragment analysis
 */
class QueryComplexityAnalyzer
{
    private array $config;
    private array $fieldComplexity = [];

    public function __construct(array $config)
    {
        $this->config = $config;
        $this->initializeFieldComplexity();
    }

    /**
     * Analyze query complexity
     */
    public function analyze(DocumentNode $document): int
    {
        $complexity = 0;
        $fragments = $this->extractFragments($document);
        
        Visitor::visit($document, [
            'Field' => function (FieldNode $field) use (&$complexity, $fragments) {
                $fieldComplexity = $this->calculateFieldComplexity($field, $fragments);
                $complexity += $fieldComplexity;
            }
        ]);
        
        return $complexity;
    }

    /**
     * Calculate depth of query
     */
    public function calculateDepth(DocumentNode $document): int
    {
        $maxDepth = 0;
        $fragments = $this->extractFragments($document);
        
        Visitor::visit($document, [
            'SelectionSet' => function (SelectionSetNode $selectionSet) use (&$maxDepth, $fragments) {
                $depth = $this->calculateSelectionSetDepth($selectionSet, $fragments);
                $maxDepth = max($maxDepth, $depth);
            }
        ]);
        
        return $maxDepth;
    }

    /**
     * Calculate field complexity
     */
    private function calculateFieldComplexity(FieldNode $field, array $fragments): int
    {
        $fieldName = $field->name->value;
        $baseComplexity = $this->fieldComplexity[$fieldName] ?? 1;
        
        // Increase complexity for arguments
        if ($field->arguments && count($field->arguments) > 0) {
            $baseComplexity += count($field->arguments) * 0.5;
        }
        
        // Increase complexity for nested selections
        if ($field->selectionSet) {
            $nestedComplexity = $this->calculateSelectionSetComplexity($field->selectionSet, $fragments);
            $baseComplexity += $nestedComplexity;
        }
        
        return (int) ceil($baseComplexity);
    }

    /**
     * Calculate selection set complexity
     */
    private function calculateSelectionSetComplexity(SelectionSetNode $selectionSet, array $fragments): int
    {
        $complexity = 0;
        
        foreach ($selectionSet->selections as $selection) {
            if ($selection instanceof FieldNode) {
                $complexity += $this->calculateFieldComplexity($selection, $fragments);
            } elseif ($selection instanceof InlineFragmentNode) {
                if ($selection->selectionSet) {
                    $complexity += $this->calculateSelectionSetComplexity($selection->selectionSet, $fragments);
                }
            } elseif ($selection instanceof FragmentSpreadNode) {
                $fragmentName = $selection->name->value;
                if (isset($fragments[$fragmentName])) {
                    $complexity += $this->calculateSelectionSetComplexity($fragments[$fragmentName]->selectionSet, $fragments);
                }
            }
        }
        
        return $complexity;
    }

    /**
     * Calculate selection set depth
     */
    private function calculateSelectionSetDepth(SelectionSetNode $selectionSet, array $fragments): int
    {
        $maxDepth = 0;
        
        foreach ($selectionSet->selections as $selection) {
            $depth = 1;
            
            if ($selection instanceof FieldNode && $selection->selectionSet) {
                $depth += $this->calculateSelectionSetDepth($selection->selectionSet, $fragments);
            } elseif ($selection instanceof InlineFragmentNode && $selection->selectionSet) {
                $depth += $this->calculateSelectionSetDepth($selection->selectionSet, $fragments);
            } elseif ($selection instanceof FragmentSpreadNode) {
                $fragmentName = $selection->name->value;
                if (isset($fragments[$fragmentName])) {
                    $depth += $this->calculateSelectionSetDepth($fragments[$fragmentName]->selectionSet, $fragments);
                }
            }
            
            $maxDepth = max($maxDepth, $depth);
        }
        
        return $maxDepth;
    }

    /**
     * Extract fragments from document
     */
    private function extractFragments(DocumentNode $document): array
    {
        $fragments = [];
        
        foreach ($document->definitions as $definition) {
            if ($definition instanceof FragmentDefinitionNode) {
                $fragments[$definition->name->value] = $definition;
            }
        }
        
        return $fragments;
    }

    /**
     * Initialize field complexity scores
     */
    private function initializeFieldComplexity(): void
    {
        $this->fieldComplexity = array_merge([
            // Simple fields
            'id' => 1,
            'name' => 1,
            'title' => 1,
            'description' => 1,
            'created_at' => 1,
            'updated_at' => 1,
            
            // Complex fields
            'users' => 5,
            'posts' => 4,
            'comments' => 3,
            'search' => 8,
            'recommendations' => 10,
            
            // Very complex operations
            'report' => 15,
            'analytics' => 20,
            'export' => 25
        ], $this->config['field_complexity'] ?? []);
    }

    /**
     * Set custom field complexity
     */
    public function setFieldComplexity(string $fieldName, int $complexity): self
    {
        $this->fieldComplexity[$fieldName] = $complexity;
        return $this;
    }

    /**
     * Get field complexity scores
     */
    public function getFieldComplexity(): array
    {
        return $this->fieldComplexity;
    }

    /**
     * Validate query against limits
     */
    public function validateQuery(DocumentNode $document): array
    {
        $errors = [];
        
        $complexity = $this->analyze($document);
        $maxComplexity = $this->config['max_query_complexity'] ?? 1000;
        
        if ($complexity > $maxComplexity) {
            $errors[] = "Query complexity ({$complexity}) exceeds maximum allowed ({$maxComplexity})";
        }
        
        $depth = $this->calculateDepth($document);
        $maxDepth = $this->config['max_query_depth'] ?? 15;
        
        if ($depth > $maxDepth) {
            $errors[] = "Query depth ({$depth}) exceeds maximum allowed ({$maxDepth})";
        }
        
        return $errors;
    }
} 