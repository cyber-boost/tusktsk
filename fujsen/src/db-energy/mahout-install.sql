-- TuskPHP Mahout Installation SQL
-- This file contains the essential database schema for a TuskPHP project
-- It will be merged with the framework's install.sql during tusk init

-- Project-specific configuration table
CREATE TABLE IF NOT EXISTS mahout_config (
    id SERIAL PRIMARY KEY,
    key VARCHAR(255) UNIQUE NOT NULL,
    value TEXT,
    type VARCHAR(50) DEFAULT 'string',
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Project-specific data tables (example)
CREATE TABLE IF NOT EXISTS mahout_data (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    type VARCHAR(100) NOT NULL,
    data JSONB,
    metadata JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Project activity log
CREATE TABLE IF NOT EXISTS mahout_activity (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    action VARCHAR(255) NOT NULL,
    entity_type VARCHAR(100),
    entity_id INTEGER,
    details JSONB,
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for performance
CREATE INDEX idx_mahout_config_key ON mahout_config(key);
CREATE INDEX idx_mahout_data_user_type ON mahout_data(user_id, type);
CREATE INDEX idx_mahout_data_created ON mahout_data(created_at DESC);
CREATE INDEX idx_mahout_activity_user ON mahout_activity(user_id);
CREATE INDEX idx_mahout_activity_entity ON mahout_activity(entity_type, entity_id);
CREATE INDEX idx_mahout_activity_created ON mahout_activity(created_at DESC);

-- Initial configuration values
INSERT INTO mahout_config (key, value, type, description) VALUES
    ('project_name', 'Mahout Application', 'string', 'The name of this project'),
    ('project_version', '1.0.0', 'string', 'Current project version'),
    ('maintenance_mode', 'false', 'boolean', 'Enable/disable maintenance mode'),
    ('api_rate_limit', '100', 'integer', 'API requests per minute limit'),
    ('theme', 'casual', 'string', 'Active TuskTone theme');

-- Grant permissions (adjust based on your database user)
-- GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO tuskphp_user;
-- GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO tuskphp_user;