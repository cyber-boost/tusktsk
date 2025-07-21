<?php

namespace TuskLang\CoreOperators;

use TuskLang\CoreOperators\BaseOperator;

/**
 * Email Operator for sending emails with various protocols and features
 */
class EmailOperator extends BaseOperator
{
    public function execute(array $config, array $context = []): mixed
    {
        $action = $config['action'] ?? 'send';
        $data = $config['data'] ?? '';
        $options = $config['options'] ?? [];

        try {
            switch ($action) {
                case 'send':
                    return $this->sendEmail($data, $options);
                case 'validate':
                    return $this->validateEmail($data, $options);
                case 'parse':
                    return $this->parseEmail($data, $options);
                case 'template':
                    return $this->sendTemplateEmail($data, $options);
                case 'bulk':
                    return $this->sendBulkEmail($data, $options);
                case 'schedule':
                    return $this->scheduleEmail($data, $options);
                case 'track':
                    return $this->trackEmail($data, $options);
                default:
                    throw new \Exception("Unknown Email action: $action");
            }
        } catch (\Exception $e) {
            error_log("Email Operator error: " . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Send email
     */
    private function sendEmail(array $emailData, array $options): array
    {
        $to = $emailData['to'] ?? '';
        $from = $emailData['from'] ?? '';
        $subject = $emailData['subject'] ?? '';
        $body = $emailData['body'] ?? '';
        $cc = $emailData['cc'] ?? [];
        $bcc = $emailData['bcc'] ?? [];
        $attachments = $emailData['attachments'] ?? [];
        $headers = $emailData['headers'] ?? [];

        // Validate required fields
        if (empty($to) || empty($from) || empty($subject)) {
            throw new \Exception("To, from, and subject are required");
        }

        // Validate email addresses
        if (!$this->validateEmailAddress($to)) {
            throw new \Exception("Invalid recipient email address: $to");
        }
        if (!$this->validateEmailAddress($from)) {
            throw new \Exception("Invalid sender email address: $from");
        }

        // Prepare email headers
        $emailHeaders = $this->prepareHeaders($from, $to, $cc, $bcc, $headers);
        
        // Prepare email body
        $emailBody = $this->prepareBody($body, $attachments);

        // Send email
        $result = $this->sendMail($to, $subject, $emailBody, $emailHeaders);

        return [
            'success' => $result['success'],
            'messageId' => $result['messageId'],
            'error' => $result['error'],
            'sentTo' => $to,
            'sentFrom' => $from,
            'subject' => $subject
        ];
    }

    /**
     * Validate email address
     */
    private function validateEmail(string $email, array $options): array
    {
        $checkDns = $options['checkDns'] ?? true;
        $checkDisposable = $options['checkDisposable'] ?? false;
        $checkFormat = $options['checkFormat'] ?? true;

        $errors = [];
        $warnings = [];

        // Check format
        if ($checkFormat && !$this->validateEmailAddress($email)) {
            $errors[] = "Invalid email format";
        }

        // Check DNS
        if ($checkDns && $checkFormat) {
            $domain = substr(strrchr($email, "@"), 1);
            if (!checkdnsrr($domain, 'MX') && !checkdnsrr($domain, 'A')) {
                $errors[] = "Domain does not have valid DNS records";
            }
        }

        // Check disposable email
        if ($checkDisposable && $this->isDisposableEmail($email)) {
            $warnings[] = "Email appears to be from a disposable email service";
        }

        return [
            'valid' => empty($errors),
            'errors' => $errors,
            'warnings' => $warnings,
            'email' => $email
        ];
    }

    /**
     * Parse email content
     */
    private function parseEmail(string $emailContent, array $options): array
    {
        $parseHeaders = $options['parseHeaders'] ?? true;
        $parseBody = $options['parseBody'] ?? true;
        $parseAttachments = $options['parseAttachments'] ?? true;

        $parsed = [
            'headers' => [],
            'body' => '',
            'attachments' => [],
            'metadata' => []
        ];

        if ($parseHeaders) {
            $parsed['headers'] = $this->parseHeaders($emailContent);
        }

        if ($parseBody) {
            $parsed['body'] = $this->parseBody($emailContent);
        }

        if ($parseAttachments) {
            $parsed['attachments'] = $this->parseAttachments($emailContent);
        }

        // Extract metadata
        $parsed['metadata'] = $this->extractMetadata($emailContent);

        return $parsed;
    }

    /**
     * Send template email
     */
    private function sendTemplateEmail(array $emailData, array $options): array
    {
        $template = $options['template'] ?? '';
        $variables = $options['variables'] ?? [];
        $templateEngine = $options['templateEngine'] ?? 'simple';

        if (empty($template)) {
            throw new \Exception("Template is required");
        }

        // Process template
        $processedBody = $this->processTemplate($template, $variables, $templateEngine);
        
        // Update email data with processed body
        $emailData['body'] = $processedBody;

        // Send email
        return $this->sendEmail($emailData, $options);
    }

    /**
     * Send bulk email
     */
    private function sendBulkEmail(array $emailData, array $options): array
    {
        $recipients = $emailData['recipients'] ?? [];
        $template = $emailData['template'] ?? '';
        $batchSize = $options['batchSize'] ?? 100;
        $delay = $options['delay'] ?? 1; // seconds between batches

        if (empty($recipients)) {
            throw new \Exception("Recipients list is required");
        }

        $results = [];
        $successCount = 0;
        $errorCount = 0;

        // Process in batches
        $batches = array_chunk($recipients, $batchSize);
        
        foreach ($batches as $batchIndex => $batch) {
            $batchResults = [];
            
            foreach ($batch as $recipient) {
                try {
                    $emailData['to'] = $recipient['email'];
                    $emailData['variables'] = $recipient['variables'] ?? [];
                    
                    if (!empty($template)) {
                        $result = $this->sendTemplateEmail($emailData, array_merge($options, ['template' => $template]));
                    } else {
                        $result = $this->sendEmail($emailData, $options);
                    }
                    
                    $batchResults[] = [
                        'email' => $recipient['email'],
                        'success' => $result['success'],
                        'error' => $result['error'] ?? null
                    ];
                    
                    if ($result['success']) {
                        $successCount++;
                    } else {
                        $errorCount++;
                    }
                } catch (\Exception $e) {
                    $batchResults[] = [
                        'email' => $recipient['email'],
                        'success' => false,
                        'error' => $e->getMessage()
                    ];
                    $errorCount++;
                }
            }
            
            $results[] = [
                'batch' => $batchIndex + 1,
                'results' => $batchResults
            ];
            
            // Delay between batches
            if ($batchIndex < count($batches) - 1 && $delay > 0) {
                sleep($delay);
            }
        }

        return [
            'success' => $errorCount === 0,
            'totalSent' => $successCount,
            'totalErrors' => $errorCount,
            'batches' => $results,
            'summary' => [
                'totalRecipients' => count($recipients),
                'successRate' => count($recipients) > 0 ? ($successCount / count($recipients)) * 100 : 0
            ]
        ];
    }

    /**
     * Schedule email
     */
    private function scheduleEmail(array $emailData, array $options): array
    {
        $scheduleTime = $options['scheduleTime'] ?? null;
        $timezone = $options['timezone'] ?? 'UTC';
        $queueName = $options['queueName'] ?? 'email_queue';

        if (empty($scheduleTime)) {
            throw new \Exception("Schedule time is required");
        }

        // Convert to timestamp
        $timestamp = $this->parseScheduleTime($scheduleTime, $timezone);
        
        if ($timestamp === false) {
            throw new \Exception("Invalid schedule time format");
        }

        // Create scheduled job
        $jobId = uniqid('email_', true);
        $jobData = [
            'id' => $jobId,
            'emailData' => $emailData,
            'options' => $options,
            'scheduledFor' => $timestamp,
            'status' => 'scheduled'
        ];

        // Store in queue (this would integrate with a job queue system)
        $this->storeScheduledJob($jobData, $queueName);

        return [
            'success' => true,
            'jobId' => $jobId,
            'scheduledFor' => date('Y-m-d H:i:s', $timestamp),
            'timezone' => $timezone
        ];
    }

    /**
     * Track email
     */
    private function trackEmail(string $messageId, array $options): array
    {
        $trackingType = $options['trackingType'] ?? 'all'; // open, click, delivery, all
        $trackingUrl = $options['trackingUrl'] ?? null;

        if (empty($messageId)) {
            throw new \Exception("Message ID is required");
        }

        // Get tracking data
        $trackingData = $this->getTrackingData($messageId, $trackingType);

        return [
            'messageId' => $messageId,
            'tracking' => $trackingData,
            'summary' => $this->summarizeTracking($trackingData)
        ];
    }

    /**
     * Validate email address format
     */
    private function validateEmailAddress(string $email): bool
    {
        return filter_var($email, FILTER_VALIDATE_EMAIL) !== false;
    }

    /**
     * Check if email is from disposable service
     */
    private function isDisposableEmail(string $email): bool
    {
        $domain = substr(strrchr($email, "@"), 1);
        $disposableDomains = [
            '10minutemail.com', 'guerrillamail.com', 'mailinator.com',
            'tempmail.org', 'throwaway.email', 'yopmail.com'
        ];
        
        return in_array(strtolower($domain), $disposableDomains);
    }

    /**
     * Prepare email headers
     */
    private function prepareHeaders(string $from, string $to, array $cc, array $bcc, array $customHeaders): string
    {
        $headers = [];
        
        // Basic headers
        $headers[] = "From: $from";
        $headers[] = "To: $to";
        
        if (!empty($cc)) {
            $headers[] = "Cc: " . implode(', ', $cc);
        }
        
        if (!empty($bcc)) {
            $headers[] = "Bcc: " . implode(', ', $bcc);
        }
        
        $headers[] = "MIME-Version: 1.0";
        $headers[] = "Content-Type: text/html; charset=UTF-8";
        
        // Custom headers
        foreach ($customHeaders as $key => $value) {
            $headers[] = "$key: $value";
        }
        
        return implode("\r\n", $headers);
    }

    /**
     * Prepare email body
     */
    private function prepareBody(string $body, array $attachments): string
    {
        if (empty($attachments)) {
            return $body;
        }

        // For simplicity, return body with attachment info
        // In a real implementation, this would create multipart MIME
        $body .= "\n\nAttachments: " . implode(', ', array_column($attachments, 'name'));
        
        return $body;
    }

    /**
     * Send mail using PHP mail function
     */
    private function sendMail(string $to, string $subject, string $body, string $headers): array
    {
        $messageId = '<' . uniqid() . '@' . gethostname() . '>';
        $headers .= "\r\nMessage-ID: $messageId";
        
        $result = mail($to, $subject, $body, $headers);
        
        return [
            'success' => $result,
            'messageId' => $messageId,
            'error' => $result ? null : 'Failed to send email'
        ];
    }

    /**
     * Parse email headers
     */
    private function parseHeaders(string $emailContent): array
    {
        $headers = [];
        $lines = explode("\n", $emailContent);
        
        foreach ($lines as $line) {
            if (empty(trim($line))) {
                break; // End of headers
            }
            
            if (preg_match('/^([^:]+):\s*(.+)$/', $line, $matches)) {
                $key = trim($matches[1]);
                $value = trim($matches[2]);
                $headers[$key] = $value;
            }
        }
        
        return $headers;
    }

    /**
     * Parse email body
     */
    private function parseBody(string $emailContent): string
    {
        $parts = explode("\n\n", $emailContent, 2);
        return isset($parts[1]) ? $parts[1] : '';
    }

    /**
     * Parse email attachments
     */
    private function parseAttachments(string $emailContent): array
    {
        // This would parse multipart MIME attachments
        // For now, return empty array
        return [];
    }

    /**
     * Extract email metadata
     */
    private function extractMetadata(string $emailContent): array
    {
        $headers = $this->parseHeaders($emailContent);
        
        return [
            'date' => $headers['Date'] ?? null,
            'messageId' => $headers['Message-ID'] ?? null,
            'subject' => $headers['Subject'] ?? null,
            'from' => $headers['From'] ?? null,
            'to' => $headers['To'] ?? null,
            'contentType' => $headers['Content-Type'] ?? null
        ];
    }

    /**
     * Process template
     */
    private function processTemplate(string $template, array $variables, string $engine): string
    {
        switch ($engine) {
            case 'simple':
                return $this->processSimpleTemplate($template, $variables);
            case 'twig':
                return $this->processTwigTemplate($template, $variables);
            default:
                return $this->processSimpleTemplate($template, $variables);
        }
    }

    /**
     * Process simple template
     */
    private function processSimpleTemplate(string $template, array $variables): string
    {
        foreach ($variables as $key => $value) {
            $template = str_replace("{{$key}}", $value, $template);
        }
        
        return $template;
    }

    /**
     * Process Twig template (placeholder)
     */
    private function processTwigTemplate(string $template, array $variables): string
    {
        // This would integrate with Twig if available
        return $this->processSimpleTemplate($template, $variables);
    }

    /**
     * Parse schedule time
     */
    private function parseScheduleTime(string $scheduleTime, string $timezone): int|false
    {
        $dateTime = new \DateTime($scheduleTime, new \DateTimeZone($timezone));
        return $dateTime->getTimestamp();
    }

    /**
     * Store scheduled job
     */
    private function storeScheduledJob(array $jobData, string $queueName): void
    {
        // This would integrate with a job queue system
        // For now, just log the job
        error_log("Scheduled email job: " . json_encode($jobData));
    }

    /**
     * Get tracking data
     */
    private function getTrackingData(string $messageId, string $trackingType): array
    {
        // This would retrieve tracking data from database
        // For now, return mock data
        return [
            'delivered' => true,
            'deliveredAt' => date('Y-m-d H:i:s'),
            'opened' => false,
            'openedAt' => null,
            'clicked' => false,
            'clickedAt' => null
        ];
    }

    /**
     * Summarize tracking data
     */
    private function summarizeTracking(array $trackingData): array
    {
        return [
            'status' => $trackingData['delivered'] ? 'delivered' : 'failed',
            'delivered' => $trackingData['delivered'],
            'opened' => $trackingData['opened'],
            'clicked' => $trackingData['clicked']
        ];
    }
} 