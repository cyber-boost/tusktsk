-- 🐘 TuskPHP Herd Authentication System - Missing Tables
-- Add these tables to your existing install.sql or run separately
-- These tables are required for the Herd authentication system to function

-- ===================
-- HERD AUTHENTICATION LOGS
-- ===================

-- Main authentication event log
CREATE TABLE IF NOT EXISTS herd_auth_logs (
    id SERIAL PRIMARY KEY,
    type VARCHAR(50) NOT NULL, -- 'login_success', 'login_failed', 'logout_success', etc.
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    data JSONB, -- Additional event data
    ip_address VARCHAR(45),
    user_agent TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Detailed login attempts tracking
CREATE TABLE IF NOT EXISTS herd_login_attempts (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    email VARCHAR(255),
    success BOOLEAN DEFAULT FALSE,
    ip_address VARCHAR(45),
    user_agent TEXT,
    device_type VARCHAR(50), -- 'mobile', 'desktop', 'tablet'
    session_id VARCHAR(255),
    remember_me BOOLEAN DEFAULT FALSE,
    failure_reason VARCHAR(255), -- Why login failed
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ===================
-- SESSION MANAGEMENT
-- ===================

-- Active user sessions
CREATE TABLE IF NOT EXISTS herd_sessions (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    session_id VARCHAR(255) UNIQUE NOT NULL,
    last_activity INTEGER, -- Unix timestamp
    pages_viewed INTEGER DEFAULT 0,
    duration INTEGER DEFAULT 0, -- Session duration in seconds
    ended_at TIMESTAMP,
    created_at INTEGER DEFAULT EXTRACT(EPOCH FROM NOW())
);

-- Page view tracking for analytics
CREATE TABLE IF NOT EXISTS herd_page_views (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    session_id VARCHAR(255),
    page VARCHAR(500),
    ip_address VARCHAR(45),
    user_agent TEXT,
    referrer VARCHAR(500),
    time_on_page INTEGER DEFAULT 0, -- Seconds on page
    scroll_depth INTEGER DEFAULT 0, -- Percentage scrolled
    interactions INTEGER DEFAULT 0, -- Number of interactions
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Logout events tracking
CREATE TABLE IF NOT EXISTS herd_logout_events (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    session_id VARCHAR(255),
    session_duration INTEGER DEFAULT 0,
    pages_viewed INTEGER DEFAULT 0,
    ip_address VARCHAR(45),
    logout_type VARCHAR(50) DEFAULT 'manual', -- 'manual', 'timeout', 'forced'
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ===================
-- PASSWORD MANAGEMENT
-- ===================

-- Password reset tokens
CREATE TABLE IF NOT EXISTS password_reset_tokens (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(255) UNIQUE NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    used BOOLEAN DEFAULT FALSE,
    used_at TIMESTAMP,
    ip_address VARCHAR(45),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Password history for security
CREATE TABLE IF NOT EXISTS password_history (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ===================
-- REMEMBER TOKENS
-- ===================

-- Remember me tokens
CREATE TABLE IF NOT EXISTS user_remember_tokens (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(255) UNIQUE NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    last_used_at TIMESTAMP,
    ip_address VARCHAR(45),
    user_agent TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ===================
-- SECURITY MONITORING
-- ===================

-- Security alerts and events
CREATE TABLE IF NOT EXISTS herd_security_alerts (
    id SERIAL PRIMARY KEY,
    type VARCHAR(100) NOT NULL, -- 'rapid_login', 'brute_force', etc.
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    metadata JSONB, -- Alert-specific data
    severity VARCHAR(20) DEFAULT 'low', -- 'low', 'medium', 'high', 'critical'
    resolved BOOLEAN DEFAULT FALSE,
    resolved_by INTEGER REFERENCES users(id) ON DELETE SET NULL,
    resolved_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ===================
-- EMAIL TRACKING
-- ===================

-- Email delivery tracking
CREATE TABLE IF NOT EXISTS herd_email_logs (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL, -- 'verification', 'password_reset', 'login_notification'
    status VARCHAR(20) DEFAULT 'sent', -- 'sent', 'failed', 'bounced'
    email_address VARCHAR(255),
    subject VARCHAR(255),
    error_message TEXT, -- If failed
    sent_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ===================
-- DEVICE TRACKING
-- ===================

-- User device fingerprinting
CREATE TABLE IF NOT EXISTS herd_user_devices (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    device_hash VARCHAR(255) UNIQUE NOT NULL, -- Hash of device characteristics
    device_name VARCHAR(255), -- User-friendly name
    device_type VARCHAR(50), -- 'mobile', 'desktop', 'tablet'
    browser VARCHAR(100),
    os VARCHAR(100),
    first_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_trusted BOOLEAN DEFAULT FALSE,
    login_count INTEGER DEFAULT 1
);

-- ===================
-- ENHANCED AUTO LOGIN (Magic Links)
-- ===================

-- Enhanced auto login tokens with more features
CREATE TABLE IF NOT EXISTS herd_magic_links (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(255) UNIQUE NOT NULL,
    purpose VARCHAR(100) DEFAULT 'login', -- 'login', 'email_verification', 'password_reset'
    redirect_url VARCHAR(500) DEFAULT '/dashboard/',
    max_uses INTEGER DEFAULT 1,
    uses_count INTEGER DEFAULT 0,
    expires_at TIMESTAMP NOT NULL,
    metadata JSONB, -- Additional data (campaign, source, etc.)
    ip_restrictions JSONB, -- Array of allowed IPs
    used_ips JSONB DEFAULT '[]', -- Array of IPs that used this token
    is_single_use BOOLEAN DEFAULT TRUE,
    created_by INTEGER REFERENCES users(id) ON DELETE SET NULL,
    first_used_at TIMESTAMP,
    last_used_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ===================
-- INDEXES FOR PERFORMANCE
-- ===================

-- Auth logs indexes
CREATE INDEX IF NOT EXISTS idx_herd_auth_logs_type ON herd_auth_logs(type);
CREATE INDEX IF NOT EXISTS idx_herd_auth_logs_user_id ON herd_auth_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_herd_auth_logs_created_at ON herd_auth_logs(created_at);
CREATE INDEX IF NOT EXISTS idx_herd_auth_logs_ip ON herd_auth_logs(ip_address);

-- Login attempts indexes
CREATE INDEX IF NOT EXISTS idx_herd_login_attempts_email ON herd_login_attempts(email);
CREATE INDEX IF NOT EXISTS idx_herd_login_attempts_ip ON herd_login_attempts(ip_address);
CREATE INDEX IF NOT EXISTS idx_herd_login_attempts_success ON herd_login_attempts(success);
CREATE INDEX IF NOT EXISTS idx_herd_login_attempts_created_at ON herd_login_attempts(created_at);

-- Sessions indexes
CREATE INDEX IF NOT EXISTS idx_herd_sessions_user_id ON herd_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_herd_sessions_session_id ON herd_sessions(session_id);
CREATE INDEX IF NOT EXISTS idx_herd_sessions_last_activity ON herd_sessions(last_activity);

-- Page views indexes
CREATE INDEX IF NOT EXISTS idx_herd_page_views_user_id ON herd_page_views(user_id);
CREATE INDEX IF NOT EXISTS idx_herd_page_views_session_id ON herd_page_views(session_id);
CREATE INDEX IF NOT EXISTS idx_herd_page_views_page ON herd_page_views(page);
CREATE INDEX IF NOT EXISTS idx_herd_page_views_created_at ON herd_page_views(created_at);

-- Security alerts indexes
CREATE INDEX IF NOT EXISTS idx_herd_security_alerts_type ON herd_security_alerts(type);
CREATE INDEX IF NOT EXISTS idx_herd_security_alerts_severity ON herd_security_alerts(severity);
CREATE INDEX IF NOT EXISTS idx_herd_security_alerts_resolved ON herd_security_alerts(resolved);

-- Magic links indexes
CREATE INDEX IF NOT EXISTS idx_herd_magic_links_token ON herd_magic_links(token);
CREATE INDEX IF NOT EXISTS idx_herd_magic_links_user_id ON herd_magic_links(user_id);
CREATE INDEX IF NOT EXISTS idx_herd_magic_links_purpose ON herd_magic_links(purpose);
CREATE INDEX IF NOT EXISTS idx_herd_magic_links_expires_at ON herd_magic_links(expires_at);

-- Device tracking indexes
CREATE INDEX IF NOT EXISTS idx_herd_user_devices_user_id ON herd_user_devices(user_id);
CREATE INDEX IF NOT EXISTS idx_herd_user_devices_hash ON herd_user_devices(device_hash);
CREATE INDEX IF NOT EXISTS idx_herd_user_devices_trusted ON herd_user_devices(is_trusted);

-- ===================
-- CLEANUP FUNCTIONS
-- ===================

-- Function to clean up expired tokens and sessions
CREATE OR REPLACE FUNCTION cleanup_herd_data(days_to_keep INTEGER DEFAULT 30)
RETURNS void AS $$
BEGIN
    -- Clean up expired password reset tokens
    DELETE FROM password_reset_tokens 
    WHERE expires_at < NOW() - INTERVAL '1 day' * days_to_keep
    OR (used = TRUE AND used_at < NOW() - INTERVAL '1 day' * 7);
    
    -- Clean up expired remember tokens
    DELETE FROM user_remember_tokens 
    WHERE expires_at < NOW();
    
    -- Clean up expired magic links
    DELETE FROM herd_magic_links 
    WHERE expires_at < NOW();
    
    -- Clean up old page views (keep recent for analytics)
    DELETE FROM herd_page_views 
    WHERE created_at < NOW() - INTERVAL '1 day' * days_to_keep;
    
    -- Clean up old login attempts (keep recent for security)
    DELETE FROM herd_login_attempts 
    WHERE created_at < NOW() - INTERVAL '1 day' * (days_to_keep / 2);
    
    -- Clean up old auth logs (keep more for auditing)
    DELETE FROM herd_auth_logs 
    WHERE created_at < NOW() - INTERVAL '1 day' * (days_to_keep * 2);
    
    -- Clean up resolved security alerts
    DELETE FROM herd_security_alerts 
    WHERE resolved = TRUE AND resolved_at < NOW() - INTERVAL '1 day' * days_to_keep;
    
    -- Clean up old email logs
    DELETE FROM herd_email_logs 
    WHERE sent_at < NOW() - INTERVAL '1 day' * days_to_keep;
    
    -- Update device last_seen
    UPDATE herd_user_devices 
    SET last_seen = CURRENT_TIMESTAMP 
    WHERE id IN (
        SELECT DISTINCT d.id 
        FROM herd_user_devices d
        JOIN herd_login_attempts l ON l.user_id = d.user_id
        WHERE l.success = TRUE AND l.created_at > NOW() - INTERVAL '1 day'
    );
END;
$$ LANGUAGE plpgsql;

-- ===================
-- COMMENTS
-- ===================

COMMENT ON TABLE herd_auth_logs IS 'Comprehensive authentication event logging';
COMMENT ON TABLE herd_login_attempts IS 'Detailed login attempt tracking with device info';
COMMENT ON TABLE herd_sessions IS 'Active user session management and analytics';
COMMENT ON TABLE herd_page_views IS 'Page view tracking for user behavior analysis';
COMMENT ON TABLE herd_security_alerts IS 'Security monitoring and threat detection';
COMMENT ON TABLE herd_magic_links IS 'Enhanced auto-login tokens with advanced features';
COMMENT ON TABLE herd_user_devices IS 'Device fingerprinting and trust management';
COMMENT ON TABLE password_reset_tokens IS 'Secure password reset token management';
COMMENT ON TABLE password_history IS 'Password history for security compliance';
COMMENT ON TABLE user_remember_tokens IS 'Remember me token management';

-- Installation complete!
-- Run: SELECT cleanup_herd_data(30); periodically to maintain performance 