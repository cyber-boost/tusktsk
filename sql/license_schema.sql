-- TuskLang License System Database Schema
-- Mother Database for License Management and Protection

-- Create database if not exists
-- CREATE DATABASE tusklang_theory;

-- Connect to database
-- \c tusklang_theory;

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Licenses table - Core license management
CREATE TABLE IF NOT EXISTS licenses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    license_key VARCHAR(19) UNIQUE NOT NULL, -- Format: XXXX-XXXX-XXXX-XXXX
    customer_name VARCHAR(255) NOT NULL,
    customer_email VARCHAR(255) NOT NULL,
    license_type VARCHAR(50) NOT NULL DEFAULT 'standard', -- standard, premium, enterprise
    status VARCHAR(20) NOT NULL DEFAULT 'active', -- active, suspended, revoked, expired
    max_installations INTEGER NOT NULL DEFAULT 1,
    current_installations INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    expires_at TIMESTAMP WITH TIME ZONE,
    last_validated_at TIMESTAMP WITH TIME ZONE,
    created_by VARCHAR(100) DEFAULT 'system',
    notes TEXT,
    metadata JSONB DEFAULT '{}'
);

-- Installations table - Track all installations
CREATE TABLE IF NOT EXISTS installations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    license_id UUID NOT NULL REFERENCES licenses(id) ON DELETE CASCADE,
    installation_hash VARCHAR(64) NOT NULL, -- SHA256 of machine fingerprint
    machine_id VARCHAR(255) NOT NULL,
    platform VARCHAR(50) NOT NULL, -- windows, mac, linux
    os_version VARCHAR(100),
    hostname VARCHAR(255),
    ip_address INET,
    user_agent TEXT,
    installed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_seen_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    status VARCHAR(20) NOT NULL DEFAULT 'active', -- active, inactive, revoked
    version VARCHAR(20),
    metadata JSONB DEFAULT '{}'
);

-- Usage logs table - Track API usage and validation
CREATE TABLE IF NOT EXISTS usage_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    license_id UUID REFERENCES licenses(id) ON DELETE SET NULL,
    installation_id UUID REFERENCES installations(id) ON DELETE SET NULL,
    action VARCHAR(50) NOT NULL, -- validate, install, revoke, check
    ip_address INET,
    user_agent TEXT,
    request_data JSONB DEFAULT '{}',
    response_data JSONB DEFAULT '{}',
    status_code INTEGER NOT NULL,
    response_time_ms INTEGER,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    error_message TEXT
);

-- Admin actions table - Track administrative operations
CREATE TABLE IF NOT EXISTS admin_actions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    admin_user VARCHAR(100) NOT NULL,
    action_type VARCHAR(50) NOT NULL, -- create_license, revoke_license, update_license
    target_license_id UUID REFERENCES licenses(id) ON DELETE SET NULL,
    target_installation_id UUID REFERENCES installations(id) ON DELETE SET NULL,
    action_data JSONB DEFAULT '{}',
    ip_address INET,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    notes TEXT
);

-- API keys table - For admin authentication
CREATE TABLE IF NOT EXISTS api_keys (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key_name VARCHAR(100) NOT NULL,
    key_hash VARCHAR(255) NOT NULL, -- Hashed API key
    permissions JSONB DEFAULT '{}', -- Array of allowed permissions
    created_by VARCHAR(100) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_used_at TIMESTAMP WITH TIME ZONE,
    expires_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT TRUE
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_licenses_license_key ON licenses(license_key);
CREATE INDEX IF NOT EXISTS idx_licenses_status ON licenses(status);
CREATE INDEX IF NOT EXISTS idx_licenses_customer_email ON licenses(customer_email);
CREATE INDEX IF NOT EXISTS idx_installations_license_id ON installations(license_id);
CREATE INDEX IF NOT EXISTS idx_installations_hash ON installations(installation_hash);
CREATE INDEX IF NOT EXISTS idx_usage_logs_license_id ON usage_logs(license_id);
CREATE INDEX IF NOT EXISTS idx_usage_logs_created_at ON usage_logs(created_at);
CREATE INDEX IF NOT EXISTS idx_admin_actions_admin_user ON admin_actions(admin_user);
CREATE INDEX IF NOT EXISTS idx_admin_actions_created_at ON admin_actions(created_at);

-- Create views for common queries
CREATE OR REPLACE VIEW license_summary AS
SELECT 
    l.id,
    l.license_key,
    l.customer_name,
    l.customer_email,
    l.license_type,
    l.status,
    l.max_installations,
    l.current_installations,
    l.created_at,
    l.expires_at,
    l.last_validated_at,
    COUNT(i.id) as active_installations,
    COUNT(ul.id) as total_validations
FROM licenses l
LEFT JOIN installations i ON l.id = i.license_id AND i.status = 'active'
LEFT JOIN usage_logs ul ON l.id = ul.license_id
GROUP BY l.id;

-- Insert default admin API key (for development - change in production)
INSERT INTO api_keys (key_name, key_hash, permissions, created_by) 
VALUES (
    'admin-key',
    '$2b$10$default.admin.key.hash.here', -- Replace with actual hash
    '["read", "write", "admin"]',
    'system'
) ON CONFLICT DO NOTHING;

-- Create function to update installation count
CREATE OR REPLACE FUNCTION update_installation_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE licenses 
        SET current_installations = current_installations + 1
        WHERE id = NEW.license_id;
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE licenses 
        SET current_installations = current_installations - 1
        WHERE id = OLD.license_id;
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Create trigger for installation count updates
CREATE TRIGGER trigger_update_installation_count
    AFTER INSERT OR DELETE ON installations
    FOR EACH ROW
    EXECUTE FUNCTION update_installation_count();

-- Grant permissions (adjust as needed)
-- GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO tt_c3b2;
-- GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO tt_c3b2; 