<?php

namespace TuskLang\Communication\Http;

/**
 * API Request representation extending HttpRequest
 */
class ApiRequest extends HttpRequest
{
    private string $path;
    private array $routeParams = [];
    private array $files = [];

    public function __construct(string $method, string $path)
    {
        $this->path = $path;
        
        // Extract query string for parent URL
        $url = 'http://localhost' . $path;
        parent::__construct($method, $url);
    }

    public function getPath(): string
    {
        return $this->path;
    }

    public function setRouteParams(array $params): self
    {
        $this->routeParams = $params;
        return $this;
    }

    public function getRouteParam(string $key, $default = null)
    {
        return $this->routeParams[$key] ?? $default;
    }

    public function getRouteParams(): array
    {
        return $this->routeParams;
    }

    public function getQueryParam(string $key, $default = null)
    {
        return $this->getQueryParams()[$key] ?? $default;
    }

    public function getBodyParam(string $key, $default = null)
    {
        $data = $this->getData();
        if (is_array($data)) {
            return $data[$key] ?? $default;
        }
        return $default;
    }

    public function setFiles(array $files): self
    {
        $this->files = $files;
        return $this;
    }

    public function getFiles(): array
    {
        return $this->files;
    }

    public function getFile(string $key): ?array
    {
        return $this->files[$key] ?? null;
    }

    public function hasFile(string $key): bool
    {
        return isset($this->files[$key]) && 
               $this->files[$key]['error'] === UPLOAD_ERR_OK;
    }

    public function getHeader(string $name, $default = null): ?string
    {
        $headers = $this->getHeadersArray();
        return $headers[$name] ?? $default;
    }

    public function getContentType(): ?string
    {
        return $this->getHeader('Content-Type');
    }

    public function getAccept(): ?string
    {
        return $this->getHeader('Accept');
    }

    public function getAuthorization(): ?string
    {
        return $this->getHeader('Authorization');
    }

    public function getUserAgent(): ?string
    {
        return $this->getHeader('User-Agent');
    }

    public function getClientIp(): string
    {
        return $_SERVER['REMOTE_ADDR'] ?? 'unknown';
    }

    public function isJson(): bool
    {
        $contentType = $this->getContentType();
        return $contentType && strpos($contentType, 'application/json') !== false;
    }

    public function isXml(): bool
    {
        $contentType = $this->getContentType();
        return $contentType && strpos($contentType, 'application/xml') !== false;
    }

    public function isAjax(): bool
    {
        return $this->getHeader('X-Requested-With') === 'XMLHttpRequest';
    }

    public function expectsJson(): bool
    {
        $accept = $this->getAccept();
        return $accept && strpos($accept, 'application/json') !== false;
    }

    public function getFullUrl(): string
    {
        $protocol = isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off' ? 'https' : 'http';
        $host = $_SERVER['HTTP_HOST'] ?? 'localhost';
        return $protocol . '://' . $host . $this->path;
    }

    public function validate(array $rules): array
    {
        $validator = new ApiValidator();
        return $validator->validate($this, $rules);
    }
} 