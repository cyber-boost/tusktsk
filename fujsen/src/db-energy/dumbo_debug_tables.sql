-- 🐘 Dumbo Debug System Database Tables
-- =====================================
-- Tables for comprehensive error tracking and debugging
-- 
-- Strong. Secure. Scalable.

-- Main debug errors table
CREATE TABLE IF NOT EXISTS `debug_errors` (
    `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
    `user_id` int(11) DEFAULT NULL COMMENT 'User who experienced the error (if authenticated)',
    `session_id` varchar(255) DEFAULT NULL COMMENT 'Browser session ID from Dumbo',
    `error_type` varchar(50) NOT NULL COMMENT 'Type: javascript, network, console, performance, etc.',
    `severity` enum('debug','info','warn','error','critical') NOT NULL DEFAULT 'error',
    `message` text NOT NULL COMMENT 'Error message (max 1000 chars)',
    `source` varchar(500) DEFAULT NULL COMMENT 'Source file or URL',
    `line_number` int(11) DEFAULT NULL COMMENT 'Line number where error occurred',
    `column_number` int(11) DEFAULT NULL COMMENT 'Column number where error occurred',
    `stack_trace` text DEFAULT NULL COMMENT 'Full stack trace (max 5000 chars)',
    `url` varchar(1000) DEFAULT NULL COMMENT 'Page URL where error occurred',
    `user_agent` varchar(500) DEFAULT NULL COMMENT 'Browser user agent string',
    `browser_info` json DEFAULT NULL COMMENT 'Detailed browser information',
    `user_journey` json DEFAULT NULL COMMENT 'User interaction history leading to error',
    `additional_data` json DEFAULT NULL COMMENT 'Extra context data (performance, network, etc.)',
    `ip_address` varchar(45) DEFAULT NULL COMMENT 'Client IP address',
    `is_resolved` tinyint(1) NOT NULL DEFAULT 0 COMMENT 'Whether this error has been resolved',
    `resolved_by` int(11) DEFAULT NULL COMMENT 'Admin user who marked as resolved',
    `resolved_at` timestamp NULL DEFAULT NULL COMMENT 'When error was marked as resolved',
    `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_error_type` (`error_type`),
    KEY `idx_severity` (`severity`),
    KEY `idx_user_id` (`user_id`),
    KEY `idx_created_at` (`created_at`),
    KEY `idx_url` (`url`(255)),
    KEY `idx_session_id` (`session_id`),
    KEY `idx_is_resolved` (`is_resolved`),
    KEY `idx_error_lookup` (`error_type`, `severity`, `created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Debug error notifications table (for admin alerts)
CREATE TABLE IF NOT EXISTS `debug_error_notifications` (
    `id` int(11) unsigned NOT NULL AUTO_INCREMENT,
    `error_type` varchar(50) NOT NULL,
    `message_hash` varchar(32) NOT NULL COMMENT 'MD5 hash of error message for deduplication',
    `severity` enum('debug','info','warn','error','critical') NOT NULL,
    `url` varchar(1000) DEFAULT NULL,
    `notification_sent` tinyint(1) NOT NULL DEFAULT 0 COMMENT 'Whether notification was successfully sent',
    `notification_method` varchar(50) DEFAULT NULL COMMENT 'email, slack, etc.',
    `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_error_type_hash` (`error_type`, `message_hash`),
    KEY `idx_created_at` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Debug sessions table (for tracking debugging sessions)
CREATE TABLE IF NOT EXISTS `debug_sessions` (
    `id` int(11) unsigned NOT NULL AUTO_INCREMENT,
    `session_id` varchar(255) NOT NULL COMMENT 'Dumbo session ID',
    `user_id` int(11) DEFAULT NULL,
    `started_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `last_activity` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `page_views` int(11) NOT NULL DEFAULT 0,
    `error_count` int(11) NOT NULL DEFAULT 0,
    `browser_info` json DEFAULT NULL,
    `user_agent` varchar(500) DEFAULT NULL,
    `ip_address` varchar(45) DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `idx_session_id` (`session_id`),
    KEY `idx_user_id` (`user_id`),
    KEY `idx_started_at` (`started_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Debug configuration table
CREATE TABLE IF NOT EXISTS `debug_config` (
    `id` int(11) unsigned NOT NULL AUTO_INCREMENT,
    `config_key` varchar(100) NOT NULL,
    `config_value` text NOT NULL,
    `description` text DEFAULT NULL,
    `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `idx_config_key` (`config_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Debug error patterns table (for identifying recurring issues)
CREATE TABLE IF NOT EXISTS `debug_error_patterns` (
    `id` int(11) unsigned NOT NULL AUTO_INCREMENT,
    `pattern_hash` varchar(32) NOT NULL COMMENT 'Hash of the error pattern',
    `error_type` varchar(50) NOT NULL,
    `message_pattern` varchar(500) NOT NULL COMMENT 'Normalized error message pattern',
    `source_pattern` varchar(500) DEFAULT NULL COMMENT 'Normalized source pattern',
    `first_seen` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `last_seen` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `occurrence_count` int(11) NOT NULL DEFAULT 1,
    `affected_users` int(11) NOT NULL DEFAULT 1,
    `is_critical` tinyint(1) NOT NULL DEFAULT 0,
    `is_ignored` tinyint(1) NOT NULL DEFAULT 0 COMMENT 'Whether to ignore this pattern',
    `resolution_notes` text DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `idx_pattern_hash` (`pattern_hash`),
    KEY `idx_error_type` (`error_type`),
    KEY `idx_first_seen` (`first_seen`),
    KEY `idx_occurrence_count` (`occurrence_count`),
    KEY `idx_is_critical` (`is_critical`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert default configuration
INSERT IGNORE INTO `debug_config` (`config_key`, `config_value`, `description`) VALUES
('dumbo_enabled', 'false', 'Whether Dumbo debugging system is enabled globally'),
('dumbo_batch_size', '10', 'Number of errors to batch before sending'),
('dumbo_batch_timeout', '5000', 'Timeout in milliseconds for batching errors'),
('dumbo_rate_limit', '100', 'Maximum errors per minute per session'),
('dumbo_capture_console', 'true', 'Whether to capture console.log/warn/error'),
('dumbo_capture_network', 'true', 'Whether to capture network request failures'),
('dumbo_capture_performance', 'true', 'Whether to capture performance issues'),
('dumbo_critical_threshold', '10', 'Number of critical errors per hour to trigger admin alerts'),
('dumbo_retention_days', '30', 'How many days to keep debug errors');

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS `idx_debug_errors_composite` ON `debug_errors` (`error_type`, `severity`, `created_at`, `user_id`);
CREATE INDEX IF NOT EXISTS `idx_debug_errors_url_time` ON `debug_errors` (`url`(100), `created_at`);
CREATE INDEX IF NOT EXISTS `idx_debug_errors_user_journey` ON `debug_errors` (`user_id`, `session_id`, `created_at`);

-- Create view for error summary
CREATE OR REPLACE VIEW `debug_error_summary` AS
SELECT 
    `error_type`,
    `severity`,
    COUNT(*) as `total_count`,
    COUNT(DISTINCT `user_id`) as `affected_users`,
    COUNT(DISTINCT `session_id`) as `affected_sessions`,
    COUNT(DISTINCT DATE(`created_at`)) as `days_active`,
    MIN(`created_at`) as `first_occurrence`,
    MAX(`created_at`) as `last_occurrence`,
    AVG(CASE WHEN `severity` = 'critical' THEN 1 ELSE 0 END) * 100 as `critical_percentage`
FROM `debug_errors` 
WHERE `created_at` >= DATE_SUB(NOW(), INTERVAL 30 DAY)
GROUP BY `error_type`, `severity`
ORDER BY `total_count` DESC;

-- Create view for recent critical errors
CREATE OR REPLACE VIEW `debug_critical_errors` AS
SELECT 
    `id`,
    `error_type`,
    `message`,
    `url`,
    `user_id`,
    `session_id`,
    `created_at`,
    `stack_trace`,
    `browser_info`
FROM `debug_errors` 
WHERE `severity` IN ('critical', 'error') 
    AND `created_at` >= DATE_SUB(NOW(), INTERVAL 24 HOUR)
    AND `is_resolved` = 0
ORDER BY `created_at` DESC;

-- Trigger to update error patterns
DELIMITER //
CREATE TRIGGER IF NOT EXISTS `update_error_patterns` 
AFTER INSERT ON `debug_errors`
FOR EACH ROW
BEGIN
    DECLARE pattern_hash VARCHAR(32);
    DECLARE normalized_message VARCHAR(500);
    DECLARE normalized_source VARCHAR(500);
    
    -- Normalize the error message (remove specific details like IDs, timestamps)
    SET normalized_message = REGEX_REPLACE(NEW.message, '[0-9]+', '#');
    SET normalized_message = REGEX_REPLACE(normalized_message, '[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}', '#UUID#');
    SET normalized_message = LEFT(normalized_message, 500);
    
    -- Normalize source path
    SET normalized_source = REGEX_REPLACE(COALESCE(NEW.source, ''), '\\?.*$', '');
    SET normalized_source = LEFT(normalized_source, 500);
    
    -- Create pattern hash
    SET pattern_hash = MD5(CONCAT(NEW.error_type, '|', normalized_message, '|', normalized_source));
    
    -- Insert or update pattern
    INSERT INTO `debug_error_patterns` 
        (`pattern_hash`, `error_type`, `message_pattern`, `source_pattern`, `occurrence_count`, `affected_users`, `is_critical`)
    VALUES 
        (pattern_hash, NEW.error_type, normalized_message, normalized_source, 1, 1, IF(NEW.severity IN ('critical', 'error'), 1, 0))
    ON DUPLICATE KEY UPDATE
        `last_seen` = CURRENT_TIMESTAMP,
        `occurrence_count` = `occurrence_count` + 1,
        `affected_users` = (
            SELECT COUNT(DISTINCT user_id) 
            FROM debug_errors 
            WHERE MD5(CONCAT(error_type, '|', 
                REGEX_REPLACE(REGEX_REPLACE(message, '[0-9]+', '#'), '[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}', '#UUID#'), '|', 
                REGEX_REPLACE(COALESCE(source, ''), '\\?.*$', ''))) = pattern_hash
        ),
        `is_critical` = IF(NEW.severity IN ('critical', 'error'), 1, `is_critical`);
END//
DELIMITER ;

-- Create event to clean up old debug data (run daily)
SET GLOBAL event_scheduler = ON;

CREATE EVENT IF NOT EXISTS `cleanup_debug_data`
ON SCHEDULE EVERY 1 DAY
STARTS CURRENT_TIMESTAMP
DO
BEGIN
    DECLARE retention_days INT DEFAULT 30;
    
    -- Get retention setting
    SELECT CAST(config_value AS SIGNED) INTO retention_days 
    FROM debug_config 
    WHERE config_key = 'dumbo_retention_days';
    
    -- Delete old debug errors
    DELETE FROM debug_errors 
    WHERE created_at < DATE_SUB(NOW(), INTERVAL retention_days DAY);
    
    -- Delete old notifications
    DELETE FROM debug_error_notifications 
    WHERE created_at < DATE_SUB(NOW(), INTERVAL retention_days DAY);
    
    -- Delete old sessions
    DELETE FROM debug_sessions 
    WHERE started_at < DATE_SUB(NOW(), INTERVAL retention_days DAY);
    
    -- Clean up patterns with no recent occurrences
    DELETE FROM debug_error_patterns 
    WHERE last_seen < DATE_SUB(NOW(), INTERVAL retention_days DAY) 
    AND occurrence_count < 5;
    
END;

-- Grant permissions (adjust as needed for your setup)
-- GRANT SELECT, INSERT, UPDATE ON debug_errors TO 'web_user'@'localhost';
-- GRANT SELECT, INSERT ON debug_error_notifications TO 'web_user'@'localhost';
-- GRANT SELECT, INSERT, UPDATE ON debug_sessions TO 'web_user'@'localhost';
-- GRANT SELECT ON debug_config TO 'web_user'@'localhost';
-- GRANT SELECT ON debug_error_patterns TO 'web_user'@'localhost'; 