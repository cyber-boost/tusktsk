<?php

namespace TuskLang\Communication\Http;

/**
 * API Response representation
 */
class ApiResponse
{
    private int $statusCode;
    private array $headers;
    private mixed $data;
    private ?string $body = null;

    public function __construct(int $statusCode = 200, array $headers = [], mixed $data = null)
    {
        $this->statusCode = $statusCode;
        $this->headers = array_merge([
            'Content-Type' => 'application/json'
        ], $headers);
        $this->data = $data;
    }

    public function getStatusCode(): int
    {
        return $this->statusCode;
    }

    public function setStatusCode(int $code): self
    {
        $this->statusCode = $code;
        return $this;
    }

    public function getHeaders(): array
    {
        return $this->headers;
    }

    public function setHeaders(array $headers): self
    {
        $this->headers = $headers;
        return $this;
    }

    public function addHeader(string $name, string $value): self
    {
        $this->headers[$name] = $value;
        return $this;
    }

    public function getHeader(string $name): ?string
    {
        return $this->headers[$name] ?? null;
    }

    public function getData(): mixed
    {
        return $this->data;
    }

    public function setData(mixed $data): self
    {
        $this->data = $data;
        $this->body = null; // Reset cached body
        return $this;
    }

    public function getBody(): string
    {
        if ($this->body === null) {
            $contentType = $this->getHeader('Content-Type') ?? 'application/json';
            
            if (strpos($contentType, 'application/json') !== false) {
                $this->body = json_encode($this->data, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE);
            } elseif (strpos($contentType, 'application/xml') !== false) {
                $this->body = $this->toXml($this->data);
            } else {
                $this->body = is_string($this->data) ? $this->data : print_r($this->data, true);
            }
        }
        
        return $this->body;
    }

    /**
     * Create success response
     */
    public static function success(mixed $data = null, int $code = 200): self
    {
        return new self($code, [], $data);
    }

    /**
     * Create error response
     */
    public static function error(string $message, int $code = 400, array $details = []): self
    {
        $errorData = array_merge([
            'error' => [
                'message' => $message,
                'code' => $code,
                'timestamp' => date('c')
            ]
        ], $details);
        
        return new self($code, [], $errorData);
    }

    /**
     * Create created response (201)
     */
    public static function created(mixed $data = null): self
    {
        return new self(201, [], $data);
    }

    /**
     * Create accepted response (202)
     */
    public static function accepted(mixed $data = null): self
    {
        return new self(202, [], $data);
    }

    /**
     * Create no content response (204)
     */
    public static function noContent(): self
    {
        return new self(204, [], null);
    }

    /**
     * Create not found response (404)
     */
    public static function notFound(string $message = 'Resource not found'): self
    {
        return self::error($message, 404);
    }

    /**
     * Create unauthorized response (401)
     */
    public static function unauthorized(string $message = 'Unauthorized'): self
    {
        return self::error($message, 401);
    }

    /**
     * Create forbidden response (403)
     */
    public static function forbidden(string $message = 'Forbidden'): self
    {
        return self::error($message, 403);
    }

    /**
     * Create validation error response (422)
     */
    public static function validationError(array $errors): self
    {
        return new self(422, [], [
            'error' => [
                'message' => 'Validation failed',
                'code' => 422,
                'errors' => $errors,
                'timestamp' => date('c')
            ]
        ]);
    }

    /**
     * Create internal server error response (500)
     */
    public static function serverError(string $message = 'Internal server error'): self
    {
        return self::error($message, 500);
    }

    /**
     * Set cache headers
     */
    public function cache(int $seconds): self
    {
        $this->headers['Cache-Control'] = "public, max-age={$seconds}";
        $this->headers['Expires'] = gmdate('D, d M Y H:i:s T', time() + $seconds);
        return $this;
    }

    /**
     * Set no-cache headers
     */
    public function noCache(): self
    {
        $this->headers['Cache-Control'] = 'no-cache, no-store, must-revalidate';
        $this->headers['Pragma'] = 'no-cache';
        $this->headers['Expires'] = '0';
        return $this;
    }

    /**
     * Add ETag header
     */
    public function etag(string $value): self
    {
        $this->headers['ETag'] = '"' . $value . '"';
        return $this;
    }

    /**
     * Add Last-Modified header
     */
    public function lastModified(\DateTime $date): self
    {
        $this->headers['Last-Modified'] = $date->format('D, d M Y H:i:s T');
        return $this;
    }

    /**
     * Check if response is successful
     */
    public function isSuccessful(): bool
    {
        return $this->statusCode >= 200 && $this->statusCode < 300;
    }

    /**
     * Check if response is a client error
     */
    public function isClientError(): bool
    {
        return $this->statusCode >= 400 && $this->statusCode < 500;
    }

    /**
     * Check if response is a server error
     */
    public function isServerError(): bool
    {
        return $this->statusCode >= 500 && $this->statusCode < 600;
    }

    /**
     * Convert data to XML
     */
    private function toXml(mixed $data, string $rootElement = 'response'): string
    {
        $xml = new \SimpleXMLElement("<{$rootElement}/>");
        $this->arrayToXml($data, $xml);
        return $xml->asXML();
    }

    /**
     * Convert array to XML elements
     */
    private function arrayToXml(mixed $data, \SimpleXMLElement $xml): void
    {
        if (is_array($data)) {
            foreach ($data as $key => $value) {
                if (is_numeric($key)) {
                    $key = 'item';
                }
                
                if (is_array($value) || is_object($value)) {
                    $child = $xml->addChild($key);
                    $this->arrayToXml($value, $child);
                } else {
                    $xml->addChild($key, htmlspecialchars($value));
                }
            }
        } elseif (is_object($data)) {
            $this->arrayToXml((array) $data, $xml);
        } else {
            $xml[0] = htmlspecialchars($data);
        }
    }
} 