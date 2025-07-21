<?php

namespace TuskLang\Communication\Http;

/**
 * HTTP-specific exception class
 */
class HttpException extends \Exception
{
    private ?HttpResponse $response = null;
    private ?HttpRequest $request = null;

    public function __construct(string $message = "", int $code = 0, ?\Throwable $previous = null)
    {
        parent::__construct($message, $code, $previous);
    }

    public function setResponse(HttpResponse $response): self
    {
        $this->response = $response;
        return $this;
    }

    public function getResponse(): ?HttpResponse
    {
        return $this->response;
    }

    public function setRequest(HttpRequest $request): self
    {
        $this->request = $request;
        return $this;
    }

    public function getRequest(): ?HttpRequest
    {
        return $this->request;
    }

    public function isClientError(): bool
    {
        return $this->response && $this->response->isClientError();
    }

    public function isServerError(): bool
    {
        return $this->response && $this->response->isServerError();
    }

    public function getStatusCode(): ?int
    {
        return $this->response ? $this->response->getStatusCode() : null;
    }
} 