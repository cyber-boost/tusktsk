-- V2 Framework - Complete Database Installation
-- This file creates all necessary tables for the V2 Framework
-- Run this ONCE during initial setup

-- Enable UUID extension if needed
-- CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =================
-- CORE USER SYSTEM
-- =================

-- Users table
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    profile_picture VARCHAR(255),
    phone VARCHAR(20),
    status VARCHAR(20) DEFAULT 'active',
    is_admin BOOLEAN DEFAULT FALSE,
    employee BOOLEAN DEFAULT FALSE,
    email_verified BOOLEAN DEFAULT FALSE,
    email_verification_token VARCHAR(255),
    password_reset_token VARCHAR(255),
    password_reset_expires TIMESTAMP,
    last_login TIMESTAMP,
    login_attempts INTEGER DEFAULT 0,
    locked_until TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Roles table
CREATE TABLE IF NOT EXISTS roles (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) UNIQUE NOT NULL,
    display_name VARCHAR(100) NOT NULL,
    description TEXT,
    level INTEGER DEFAULT 0,
    is_system BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Capabilities table
CREATE TABLE IF NOT EXISTS capabilities (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL,
    display_name VARCHAR(150) NOT NULL,
    description TEXT,
    category VARCHAR(50),
    is_system BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- User roles junction table
CREATE TABLE IF NOT EXISTS user_roles (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    role_id INTEGER REFERENCES roles(id) ON DELETE CASCADE,
    assigned_by INTEGER REFERENCES users(id),
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, role_id)
);

-- Role capabilities junction table
CREATE TABLE IF NOT EXISTS role_capabilities (
    id SERIAL PRIMARY KEY,
    role_id INTEGER REFERENCES roles(id) ON DELETE CASCADE,
    capability_id INTEGER REFERENCES capabilities(id) ON DELETE CASCADE,
    granted_by INTEGER REFERENCES users(id),
    granted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(role_id, capability_id)
);

-- User capabilities (direct capabilities assigned to users)
CREATE TABLE IF NOT EXISTS user_capabilities (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    capability_id INTEGER REFERENCES capabilities(id) ON DELETE CASCADE,
    granted_by INTEGER REFERENCES users(id),
    granted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, capability_id)
);

-- Capability dependencies (some capabilities require others)
CREATE TABLE IF NOT EXISTS capability_dependencies (
    id SERIAL PRIMARY KEY,
    capability_id INTEGER REFERENCES capabilities(id) ON DELETE CASCADE,
    depends_on_capability_id INTEGER REFERENCES capabilities(id) ON DELETE CASCADE,
    UNIQUE(capability_id, depends_on_capability_id)
);

-- ===================
-- TRACKING SYSTEM
-- ===================

-- Active users tracking
CREATE TABLE IF NOT EXISTS active_users (
    id SERIAL PRIMARY KEY,
    user_id INTEGER UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    last_activity TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    current_page VARCHAR(255),
    ip_address VARCHAR(45),
    user_agent TEXT
);

-- Session tracking
CREATE TABLE IF NOT EXISTS session_tracking (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    session_id VARCHAR(255) NOT NULL,
    ip_address VARCHAR(45),
    user_agent TEXT,
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_activity TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ended_at TIMESTAMP,
    start_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Page views tracking
CREATE TABLE IF NOT EXISTS page_views (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    session_id VARCHAR(255),
    page_url VARCHAR(500) NOT NULL,
    referrer VARCHAR(500),
    ip_address VARCHAR(45),
    user_agent TEXT,
    load_time REAL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Activity tracking (general user activities)
CREATE TABLE IF NOT EXISTS activity_tracking (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    session_id VARCHAR(255),
    activity_type VARCHAR(100) NOT NULL,
    activity_data JSONB,
    ip_address VARCHAR(45),
    user_agent TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Error tracking
CREATE TABLE IF NOT EXISTS error_tracking (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    session_id VARCHAR(255),
    error_type VARCHAR(100) NOT NULL,
    error_message TEXT,
    stack_trace TEXT,
    page_url VARCHAR(500),
    ip_address VARCHAR(45),
    user_agent TEXT,
    resolved BOOLEAN DEFAULT FALSE,
    resolved_by INTEGER REFERENCES users(id) ON DELETE SET NULL,
    resolved_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- IP cache for geolocation
CREATE TABLE IF NOT EXISTS ip_cache (
    id SERIAL PRIMARY KEY,
    ip_address VARCHAR(45) UNIQUE NOT NULL,
    country_code VARCHAR(2),
    country_name VARCHAR(100),
    region_name VARCHAR(100),
    city_name VARCHAR(100),
    geo_data JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ===================
-- AUTHENTICATION
-- ===================

-- Auto login tokens
CREATE TABLE IF NOT EXISTS auto_login_tokens (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(255) UNIQUE NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    used BOOLEAN DEFAULT FALSE,
    used_at TIMESTAMP,
    ip_address VARCHAR(45),
    user_agent TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ===================
-- AFFILIATE SYSTEM
-- ===================

-- Affiliates table (Enhanced for MLM)
CREATE TABLE IF NOT EXISTS affiliates (
    id INTEGER PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    commission_rate DECIMAL(5,2) DEFAULT 10.00,
    tier VARCHAR(50) DEFAULT 'bronze',
    status VARCHAR(50) DEFAULT 'active',
    referrer_id INTEGER DEFAULT NULL,
    level INTEGER DEFAULT 1,
    genealogy_path TEXT DEFAULT '',
    total_clicks INTEGER DEFAULT 0,
    total_conversions INTEGER DEFAULT 0,
    total_earnings DECIMAL(10,2) DEFAULT 0.00,
    unpaid_earnings DECIMAL(10,2) DEFAULT 0.00,
    total_downline_earnings DECIMAL(10,2) DEFAULT 0.00,
    direct_referrals INTEGER DEFAULT 0,
    total_downline INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (referrer_id) REFERENCES affiliates(id)
);

-- Affiliate links table
CREATE TABLE IF NOT EXISTS affiliate_links (
    id INTEGER PRIMARY KEY,
    affiliate_id INTEGER NOT NULL,
    product_id INTEGER,
    campaign VARCHAR(255) DEFAULT 'general',
    clicks INTEGER DEFAULT 0,
    conversions INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (affiliate_id) REFERENCES affiliates(id)
);

-- Affiliate purchases table
CREATE TABLE IF NOT EXISTS affiliate_purchases (
    id INTEGER PRIMARY KEY,
    affiliate_id INTEGER NOT NULL,
    link_id INTEGER,
    amount DECIMAL(10,2) NOT NULL,
    commission DECIMAL(10,2) NOT NULL,
    status VARCHAR(50) DEFAULT 'completed',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (affiliate_id) REFERENCES affiliates(id),
    FOREIGN KEY (link_id) REFERENCES affiliate_links(id)
);

-- Affiliate payments table
CREATE TABLE IF NOT EXISTS affiliate_payments (
    id INTEGER PRIMARY KEY,
    affiliate_id INTEGER NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    payment_method VARCHAR(100) DEFAULT 'manual',
    status VARCHAR(50) DEFAULT 'completed',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (affiliate_id) REFERENCES affiliates(id)
);

-- MLM Commission tracking table
CREATE TABLE IF NOT EXISTS affiliate_commissions (
    id SERIAL PRIMARY KEY,
    purchase_id INTEGER NOT NULL,
    affiliate_id INTEGER NOT NULL,
    level INTEGER NOT NULL,
    commission_rate DECIMAL(5,2) NOT NULL,
    commission_amount DECIMAL(10,2) NOT NULL,
    commission_type VARCHAR(50) DEFAULT 'direct',
    status VARCHAR(50) DEFAULT 'pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (purchase_id) REFERENCES affiliate_purchases(id),
    FOREIGN KEY (affiliate_id) REFERENCES affiliates(id)
);

-- Referral relationships table
CREATE TABLE IF NOT EXISTS affiliate_referrals (
    id SERIAL PRIMARY KEY,
    referrer_id INTEGER NOT NULL,
    referred_id INTEGER NOT NULL,
    level INTEGER NOT NULL,
    status VARCHAR(50) DEFAULT 'active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (referrer_id) REFERENCES affiliates(id),
    FOREIGN KEY (referred_id) REFERENCES affiliates(id),
    UNIQUE(referrer_id, referred_id)
);

-- MLM Settings table
CREATE TABLE IF NOT EXISTS mlm_settings (
    id SERIAL PRIMARY KEY,
    setting_name VARCHAR(100) UNIQUE NOT NULL,
    setting_value TEXT NOT NULL,
    setting_type VARCHAR(50) DEFAULT 'string',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ===================
-- INDEXES FOR PERFORMANCE
-- ===================

-- User indexes
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_status ON users(status);
CREATE INDEX IF NOT EXISTS idx_users_last_login ON users(last_login);

-- Tracking indexes
CREATE INDEX IF NOT EXISTS idx_active_users_user_id ON active_users(user_id);
CREATE INDEX IF NOT EXISTS idx_active_users_last_activity ON active_users(last_activity);
CREATE INDEX IF NOT EXISTS idx_session_tracking_user_id ON session_tracking(user_id);
CREATE INDEX IF NOT EXISTS idx_session_tracking_session_id ON session_tracking(session_id);
CREATE INDEX IF NOT EXISTS idx_page_views_user_id ON page_views(user_id);
CREATE INDEX IF NOT EXISTS idx_page_views_created_at ON page_views(created_at);
CREATE INDEX IF NOT EXISTS idx_activity_tracking_user_id ON activity_tracking(user_id);
CREATE INDEX IF NOT EXISTS idx_activity_tracking_type ON activity_tracking(activity_type);
CREATE INDEX IF NOT EXISTS idx_error_tracking_created_at ON error_tracking(created_at);
CREATE INDEX IF NOT EXISTS idx_error_tracking_resolved ON error_tracking(resolved);

-- Auth indexes
CREATE INDEX IF NOT EXISTS idx_auto_login_tokens_token ON auto_login_tokens(token);
CREATE INDEX IF NOT EXISTS idx_auto_login_tokens_user_id ON auto_login_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_auto_login_tokens_expires_at ON auto_login_tokens(expires_at);

-- Affiliate indexes
CREATE INDEX IF NOT EXISTS idx_affiliates_email ON affiliates(email);
CREATE INDEX IF NOT EXISTS idx_affiliates_status ON affiliates(status);
CREATE INDEX IF NOT EXISTS idx_affiliates_tier ON affiliates(tier);
CREATE INDEX IF NOT EXISTS idx_affiliates_referrer_id ON affiliates(referrer_id);
CREATE INDEX IF NOT EXISTS idx_affiliates_level ON affiliates(level);
CREATE INDEX IF NOT EXISTS idx_affiliates_genealogy_path ON affiliates(genealogy_path);
CREATE INDEX IF NOT EXISTS idx_affiliate_links_affiliate_id ON affiliate_links(affiliate_id);
CREATE INDEX IF NOT EXISTS idx_affiliate_links_product_id ON affiliate_links(product_id);
CREATE INDEX IF NOT EXISTS idx_affiliate_links_campaign ON affiliate_links(campaign);
CREATE INDEX IF NOT EXISTS idx_affiliate_purchases_affiliate_id ON affiliate_purchases(affiliate_id);
CREATE INDEX IF NOT EXISTS idx_affiliate_purchases_link_id ON affiliate_purchases(link_id);
CREATE INDEX IF NOT EXISTS idx_affiliate_purchases_status ON affiliate_purchases(status);
CREATE INDEX IF NOT EXISTS idx_affiliate_payments_affiliate_id ON affiliate_payments(affiliate_id);
CREATE INDEX IF NOT EXISTS idx_affiliate_payments_status ON affiliate_payments(status);
CREATE INDEX IF NOT EXISTS idx_affiliate_commissions_purchase_id ON affiliate_commissions(purchase_id);
CREATE INDEX IF NOT EXISTS idx_affiliate_commissions_affiliate_id ON affiliate_commissions(affiliate_id);
CREATE INDEX IF NOT EXISTS idx_affiliate_commissions_level ON affiliate_commissions(level);
CREATE INDEX IF NOT EXISTS idx_affiliate_referrals_referrer_id ON affiliate_referrals(referrer_id);
CREATE INDEX IF NOT EXISTS idx_affiliate_referrals_referred_id ON affiliate_referrals(referred_id);
CREATE INDEX IF NOT EXISTS idx_affiliate_referrals_level ON affiliate_referrals(level);

-- ===================
-- DEFAULT DATA
-- ===================

-- Insert default roles
INSERT INTO roles (name, display_name, description, level, is_system) VALUES
('super_admin', 'Super Administrator', 'Full system access', 100, TRUE),
('admin', 'Administrator', 'Administrative access', 90, TRUE),
('employee', 'Employee', 'Staff member access', 50, TRUE),
('user', 'User', 'Standard user access', 10, TRUE),
('guest', 'Guest', 'Public access only', 0, TRUE)
ON CONFLICT (name) DO NOTHING;

-- Insert default capabilities
INSERT INTO capabilities (name, display_name, description, category, is_system) VALUES
('all', 'All Permissions', 'Complete system access', 'system', TRUE),
('manage_users', 'Manage Users', 'Create, edit, delete users', 'user_management', TRUE),
('manage_roles', 'Manage Roles', 'Create, edit, delete roles', 'user_management', TRUE),
('manage_settings', 'Manage Settings', 'Modify system settings', 'administration', TRUE),
('view_logs', 'View Logs', 'Access system logs', 'administration', TRUE),
('view_analytics', 'View Analytics', 'Access analytics data', 'administration', TRUE),
('manage_content', 'Manage Content', 'Create, edit, delete content', 'content', TRUE),
('view_dashboard', 'View Dashboard', 'Access user dashboard', 'general', TRUE),
('manage_own_profile', 'Manage Own Profile', 'Edit own profile information', 'general', TRUE),
('view_public', 'View Public Content', 'Access public pages', 'general', TRUE)
ON CONFLICT (name) DO NOTHING;

-- Assign capabilities to roles
INSERT INTO role_capabilities (role_id, capability_id) 
SELECT r.id, c.id 
FROM roles r, capabilities c 
WHERE r.name = 'super_admin' AND c.name = 'all'
ON CONFLICT (role_id, capability_id) DO NOTHING;

INSERT INTO role_capabilities (role_id, capability_id) 
SELECT r.id, c.id 
FROM roles r, capabilities c 
WHERE r.name = 'admin' AND c.name IN ('manage_users', 'manage_settings', 'view_logs', 'manage_content', 'view_dashboard', 'manage_own_profile', 'view_analytics')
ON CONFLICT (role_id, capability_id) DO NOTHING;

INSERT INTO role_capabilities (role_id, capability_id) 
SELECT r.id, c.id 
FROM roles r, capabilities c 
WHERE r.name = 'employee' AND c.name IN ('view_dashboard', 'manage_own_profile', 'manage_content')
ON CONFLICT (role_id, capability_id) DO NOTHING;

INSERT INTO role_capabilities (role_id, capability_id) 
SELECT r.id, c.id 
FROM roles r, capabilities c 
WHERE r.name = 'user' AND c.name IN ('view_dashboard', 'manage_own_profile')
ON CONFLICT (role_id, capability_id) DO NOTHING;

INSERT INTO role_capabilities (role_id, capability_id) 
SELECT r.id, c.id 
FROM roles r, capabilities c 
WHERE r.name = 'guest' AND c.name = 'view_public'
ON CONFLICT (role_id, capability_id) DO NOTHING;

-- ===================
-- FUNCTIONS
-- ===================

-- Function to clean up expired tokens
CREATE OR REPLACE FUNCTION cleanup_expired_tokens(days_to_keep INTEGER DEFAULT 7)
RETURNS void AS $$
BEGIN
    DELETE FROM auto_login_tokens 
    WHERE expires_at < NOW() - INTERVAL '1 day' * days_to_keep
    OR (used = TRUE AND used_at < NOW() - INTERVAL '1 day' * days_to_keep);
END;
$$ LANGUAGE plpgsql;

-- Function to clean up old tracking data
CREATE OR REPLACE FUNCTION cleanup_old_tracking_data(days_to_keep INTEGER DEFAULT 30)
RETURNS void AS $$
BEGIN
    DELETE FROM page_views WHERE created_at < NOW() - INTERVAL '1 day' * days_to_keep;
    DELETE FROM activity_tracking WHERE created_at < NOW() - INTERVAL '1 day' * days_to_keep;
    DELETE FROM session_tracking WHERE started_at < NOW() - INTERVAL '1 day' * days_to_keep;
    DELETE FROM ip_cache WHERE created_at < NOW() - INTERVAL '1 day' * days_to_keep;
END;
$$ LANGUAGE plpgsql;

-- ===================
-- FINAL NOTES
-- ===================

-- Installation complete!
-- Next steps:
-- 1. Create your first admin user through the web interface
-- 2. Configure email settings in config.php
-- 3. Set up SSL certificates
-- 4. Configure cron jobs for cleanup functions

COMMENT ON TABLE users IS 'Core user accounts';
COMMENT ON TABLE roles IS 'User role definitions';
COMMENT ON TABLE capabilities IS 'System capability definitions';
COMMENT ON TABLE user_roles IS 'User to role assignments';
COMMENT ON TABLE role_capabilities IS 'Role to capability assignments';
COMMENT ON TABLE active_users IS 'Currently active user sessions';
COMMENT ON TABLE session_tracking IS 'User session history';
COMMENT ON TABLE page_views IS 'Page view analytics';
COMMENT ON TABLE activity_tracking IS 'User activity logs';
COMMENT ON TABLE error_tracking IS 'Application error logs';
COMMENT ON TABLE auto_login_tokens IS 'Secure auto-login tokens';
-- Insert default MLM settings
INSERT INTO mlm_settings (setting_name, setting_value, setting_type) VALUES
('max_levels', '7', 'integer'),
('level_1_rate', '10.00', 'decimal'),
('level_2_rate', '5.00', 'decimal'),
('level_3_rate', '3.00', 'decimal'),
('level_4_rate', '2.00', 'decimal'),
('level_5_rate', '1.50', 'decimal'),
('level_6_rate', '1.00', 'decimal'),
('level_7_rate', '0.50', 'decimal'),
('binary_matching_bonus', '3.00', 'decimal'),
('rank_advancement_bonus', '100.00', 'decimal'),
('minimum_payout', '50.00', 'decimal'),
('compression_enabled', 'true', 'boolean'),
('infinity_bonus_enabled', 'false', 'boolean')
ON CONFLICT (setting_name) DO NOTHING;

COMMENT ON TABLE affiliates IS 'Affiliate partner accounts with MLM support';
COMMENT ON TABLE affiliate_links IS 'Affiliate tracking links';
COMMENT ON TABLE affiliate_purchases IS 'Affiliate purchase/conversion records';
COMMENT ON TABLE affiliate_payments IS 'Affiliate payment history';
COMMENT ON TABLE affiliate_commissions IS 'Multi-level commission tracking';
COMMENT ON TABLE affiliate_referrals IS 'MLM referral relationship tree';
COMMENT ON TABLE mlm_settings IS 'MLM system configuration settings'; 