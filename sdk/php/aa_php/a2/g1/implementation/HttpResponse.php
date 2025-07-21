<?php

namespace TuskLang\Communication\Http;

/**
 * HTTP Response representation
 */
class HttpResponse
{
    private int $statusCode;
    private array $headers;
    private string $body;
    private array $info;

    public function __construct(int $statusCode, array $headers, string $body, array $info = [])
    {
        $this->statusCode = $statusCode;
        $this->headers = $headers;
        $this->body = $body;
        $this->info = $info;
    }

    public function getStatusCode(): int
    {
        return $this->statusCode;
    }

    public function getHeaders(): array
    {
        return $this->headers;
    }

    public function getHeader(string $name): ?string
    {
        return $this->headers[$name] ?? null;
    }

    public function getBody(): string
    {
        return $this->body;
    }

    public function getJson(): ?array
    {
        $data = json_decode($this->body, true);
        return json_last_error() === JSON_ERROR_NONE ? $data : null;
    }

    public function getInfo(): array
    {
        return $this->info;
    }

    public function isSuccess(): bool
    {
        return $this->statusCode >= 200 && $this->statusCode < 300;
    }

    public function isRedirect(): bool
    {
        return $this->statusCode >= 300 && $this->statusCode < 400;
    }

    public function isClientError(): bool
    {
        return $this->statusCode >= 400 && $this->statusCode < 500;
    }

    public function isServerError(): bool
    {
        return $this->statusCode >= 500 && $this->statusCode < 600;
    }

    public function getContentType(): ?string
    {
        return $this->getHeader('Content-Type');
    }

    public function getContentLength(): ?int
    {
        $length = $this->getHeader('Content-Length');
        return $length !== null ? (int) $length : null;
    }

    public function getTotalTime(): ?float
    {
        return $this->info['total_time'] ?? null;
    }

    public function getEffectiveUrl(): ?string
    {
        return $this->info['url'] ?? null;
    }
} 