<?php

declare(strict_types=1);

namespace TuskLang\A5\G1\Tests;

use PHPUnit\Framework\TestCase;
use TuskLang\A5\G1\JsonOperator;
use InvalidArgumentException;

/**
 * Comprehensive test suite for JsonOperator
 */
class JsonOperatorTest extends TestCase
{
    private JsonOperator $operator;

    protected function setUp(): void
    {
        $this->operator = new JsonOperator();
    }

    public function testGetName(): void
    {
        $this->assertEquals('json', $this->operator->getName());
    }

    public function testGetDescription(): void
    {
        $this->assertIsString($this->operator->getDescription());
        $this->assertNotEmpty($this->operator->getDescription());
    }

    public function testGetSupportedActions(): void
    {
        $actions = $this->operator->getSupportedActions();
        $expectedActions = [
            'parse', 'stringify', 'validate', 'transform', 'merge',
            'extract', 'query', 'minify', 'prettify', 'schema_validate'
        ];
        
        $this->assertEquals($expectedActions, $actions);
    }

    public function testParseValidJson(): void
    {
        $json = '{"name": "test", "value": 123}';
        $result = $this->operator->execute('parse', ['json' => $json]);
        
        $this->assertIsArray($result);
        $this->assertEquals('test', $result['name']);
        $this->assertEquals(123, $result['value']);
    }

    public function testParseInvalidJson(): void
    {
        $this->expectException(InvalidArgumentException::class);
        $this->expectExceptionMessage('Invalid JSON');
        
        $this->operator->execute('parse', ['json' => '{"invalid": json}']);
    }

    public function testParseEmptyJson(): void
    {
        $this->expectException(InvalidArgumentException::class);
        $this->expectExceptionMessage('JSON string cannot be empty');
        
        $this->operator->execute('parse', ['json' => '']);
    }

    public function testStringifyData(): void
    {
        $data = ['name' => 'test', 'value' => 123];
        $result = $this->operator->execute('stringify', ['data' => $data]);
        
        $this->assertIsString($result);
        $this->assertJson($result);
        
        $parsed = json_decode($result, true);
        $this->assertEquals($data, $parsed);
    }

    public function testStringifyWithPrettyPrint(): void
    {
        $data = ['name' => 'test', 'nested' => ['value' => 123]];
        $result = $this->operator->execute('stringify', [
            'data' => $data,
            'options' => ['pretty' => true]
        ]);
        
        $this->assertStringContainsString("\n", $result);
        $this->assertStringContainsString("    ", $result); // Indentation
    }

    public function testValidateValidJson(): void
    {
        $json = '{"valid": true}';
        $result = $this->operator->execute('validate', ['json' => $json]);
        
        $this->assertTrue($result['valid']);
        $this->assertNull($result['error']);
    }

    public function testValidateInvalidJson(): void
    {
        $json = '{"invalid": json}';
        $result = $this->operator->execute('validate', ['json' => $json]);
        
        $this->assertFalse($result['valid']);
        $this->assertNotNull($result['error']);
        $this->assertIsString($result['error']);
    }

    public function testValidateEmptyJson(): void
    {
        $result = $this->operator->execute('validate', ['json' => '']);
        
        $this->assertFalse($result['valid']);
        $this->assertEquals('Empty JSON string', $result['error']);
    }

    public function testTransformJson(): void
    {
        $json = '{"name": "test", "value": 123}';
        $transformer = function($data, $type) {
            if ($type === 'key') {
                return strtoupper($data);
            }
            if ($type === 'value' && is_string($data)) {
                return strtoupper($data);
            }
            return $data;
        };
        
        $result = $this->operator->execute('transform', [
            'json' => $json,
            'transformer' => $transformer
        ]);
        
        $this->assertIsString($result);
        $parsed = json_decode($result, true);
        $this->assertEquals('TEST', $parsed['NAME']);
        $this->assertEquals(123, $parsed['VALUE']); // Number unchanged
    }

    public function testMergeJson(): void
    {
        $json1 = '{"a": 1, "b": 2}';
        $json2 = '{"b": 3, "c": 4}';
        
        $result = $this->operator->execute('merge', [
            'json1' => $json1,
            'json2' => $json2,
            'options' => ['strategy' => 'override']
        ]);
        
        $parsed = json_decode($result, true);
        $this->assertEquals(1, $parsed['a']);
        $this->assertEquals(3, $parsed['b']); // Overridden
        $this->assertEquals(4, $parsed['c']);
    }

    public function testMergeWithKeepStrategy(): void
    {
        $json1 = '{"a": 1, "b": 2}';
        $json2 = '{"b": 3, "c": 4}';
        
        $result = $this->operator->execute('merge', [
            'json1' => $json1,
            'json2' => $json2,
            'options' => ['strategy' => 'keep']
        ]);
        
        $parsed = json_decode($result, true);
        $this->assertEquals(1, $parsed['a']);
        $this->assertEquals(2, $parsed['b']); // Kept original
        $this->assertEquals(4, $parsed['c']);
    }

    public function testExtractFromJson(): void
    {
        $json = '{"user": {"name": "test", "details": {"age": 25}}}';
        
        $result = $this->operator->execute('extract', [
            'json' => $json,
            'path' => 'user.name'
        ]);
        
        $this->assertEquals('test', $result);
        
        $result = $this->operator->execute('extract', [
            'json' => $json,
            'path' => 'user.details.age'
        ]);
        
        $this->assertEquals(25, $result);
    }

    public function testExtractNonExistentPath(): void
    {
        $json = '{"user": {"name": "test"}}';
        
        $result = $this->operator->execute('extract', [
            'json' => $json,
            'path' => 'user.nonexistent'
        ]);
        
        $this->assertNull($result);
    }

    public function testQueryJson(): void
    {
        $json = '[{"name": "test1", "type": "A"}, {"name": "test2", "type": "B"}]';
        
        $result = $this->operator->execute('query', [
            'json' => $json,
            'query' => 'test1'
        ]);
        
        $this->assertIsArray($result);
        $this->assertCount(1, $result);
    }

    public function testMinifyJson(): void
    {
        $json = '{
            "name": "test",
            "value": 123
        }';
        
        $result = $this->operator->execute('minify', ['json' => $json]);
        
        $this->assertIsString($result);
        $this->assertStringNotContainsString("\n", $result);
        $this->assertStringNotContainsString("    ", $result);
        
        // Should still be valid JSON
        $this->assertIsArray(json_decode($result, true));
    }

    public function testPrettifyJson(): void
    {
        $json = '{"name":"test","value":123}';
        
        $result = $this->operator->execute('prettify', ['json' => $json]);
        
        $this->assertIsString($result);
        $this->assertStringContainsString("\n", $result);
        $this->assertStringContainsString("    ", $result); // Indentation
        
        // Should still be valid JSON
        $this->assertIsArray(json_decode($result, true));
    }

    public function testSchemaValidation(): void
    {
        $json = '{"name": "test", "age": 25}';
        $schema = '{"type": "object", "properties": {"name": {"type": "string"}, "age": {"type": "number"}}}';
        
        $result = $this->operator->execute('schema_validate', [
            'json' => $json,
            'schema' => $schema
        ]);
        
        $this->assertTrue($result['valid']);
        $this->assertEmpty($result['errors']);
    }

    public function testUnsupportedAction(): void
    {
        $this->expectException(InvalidArgumentException::class);
        $this->expectExceptionMessage('Unsupported action: unknown');
        
        $this->operator->execute('unknown');
    }

    public function testParseWithOptions(): void
    {
        $json = '{"bigint": 9223372036854775807}';
        
        $result = $this->operator->execute('parse', [
            'json' => $json,
            'options' => ['big_int_as_string' => true]
        ]);
        
        $this->assertIsArray($result);
        $this->assertArrayHasKey('bigint', $result);
    }

    public function testStringifyWithOptions(): void
    {
        $data = ['path' => 'test/path', 'unicode' => 'tëst'];
        
        $result = $this->operator->execute('stringify', [
            'data' => $data,
            'options' => [
                'unescaped_slashes' => true,
                'unescaped_unicode' => true
            ]
        ]);
        
        $this->assertStringContainsString('test/path', $result);
        $this->assertStringContainsString('tëst', $result);
    }
} 