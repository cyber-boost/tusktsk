<?php

namespace TuskLang\Communication\Http;

/**
 * HTTP Request representation
 */
class HttpRequest
{
    private string $method;
    private string $url;
    private array $headers = [];
    private mixed $data = null;
    private array $queryParams = [];
    private array $options = [];

    public function __construct(string $method, string $url)
    {
        $this->method = strtoupper($method);
        $this->url = $url;
        $this->parseUrl();
    }

    public function getMethod(): string
    {
        return $this->method;
    }

    public function getUrl(): string
    {
        return $this->url;
    }

    public function setHeaders(array $headers): self
    {
        $this->headers = $headers;
        return $this;
    }

    public function addHeader(string $key, string $value): self
    {
        $this->headers[$key] = $value;
        return $this;
    }

    public function getHeaders(): array
    {
        $formatted = [];
        foreach ($this->headers as $key => $value) {
            $formatted[] = $key . ': ' . $value;
        }
        return $formatted;
    }

    public function getHeadersArray(): array
    {
        return $this->headers;
    }

    public function setData(mixed $data): self
    {
        $this->data = $data;
        return $this;
    }

    public function getData(): mixed
    {
        return $this->data;
    }

    public function setQueryParams(array $params): self
    {
        $this->queryParams = $params;
        $this->rebuildUrl();
        return $this;
    }

    public function addQueryParam(string $key, string $value): self
    {
        $this->queryParams[$key] = $value;
        $this->rebuildUrl();
        return $this;
    }

    public function getQueryParams(): array
    {
        return $this->queryParams;
    }

    private function parseUrl(): void
    {
        $parts = parse_url($this->url);
        if (isset($parts['query'])) {
            parse_str($parts['query'], $this->queryParams);
        }
    }

    private function rebuildUrl(): void
    {
        $parts = parse_url($this->url);
        $baseUrl = $parts['scheme'] . '://' . $parts['host'];
        
        if (isset($parts['port'])) {
            $baseUrl .= ':' . $parts['port'];
        }
        
        if (isset($parts['path'])) {
            $baseUrl .= $parts['path'];
        }
        
        if (!empty($this->queryParams)) {
            $baseUrl .= '?' . http_build_query($this->queryParams);
        }
        
        if (isset($parts['fragment'])) {
            $baseUrl .= '#' . $parts['fragment'];
        }
        
        $this->url = $baseUrl;
    }
} 