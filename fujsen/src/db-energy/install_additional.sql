-- Additional TuskPHP Tables (Herd Intelligence System)
-- Add these to your main install.sql

-- ===================
-- HERD INTELLIGENCE SYSTEM
-- ===================

-- Herd authentication logs
CREATE TABLE IF NOT EXISTS herd_auth_logs (
    id SERIAL PRIMARY KEY,
    type VARCHAR(50) NOT NULL,
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    ip_address VARCHAR(45),
    user_agent TEXT,
    data JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ===================
-- EMAIL SYSTEM
-- ===================

-- Email tracking
CREATE TABLE IF NOT EXISTS email_tracking (
    id SERIAL PRIMARY KEY,
    tracking_id VARCHAR(255) UNIQUE NOT NULL,
    recipient_email VARCHAR(255) NOT NULL,
    subject VARCHAR(500),
    message_type VARCHAR(100),
    priority VARCHAR(20) DEFAULT 'normal',
    sent_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    opened_at TIMESTAMP,
    clicked_at TIMESTAMP,
    status VARCHAR(50) DEFAULT 'sent',
    error_message TEXT
);

-- Email opens
CREATE TABLE IF NOT EXISTS email_opens (
    id SERIAL PRIMARY KEY,
    tracking_id VARCHAR(255) REFERENCES email_tracking(tracking_id),
    ip_address VARCHAR(45),
    user_agent TEXT,
    opened_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Email clicks
CREATE TABLE IF NOT EXISTS email_clicks (
    id SERIAL PRIMARY KEY,
    tracking_id VARCHAR(255) REFERENCES email_tracking(tracking_id),
    url TEXT NOT NULL,
    ip_address VARCHAR(45),
    clicked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Email queue
CREATE TABLE IF NOT EXISTS email_queue (
    id SERIAL PRIMARY KEY,
    recipient_email VARCHAR(255) NOT NULL,
    subject VARCHAR(500),
    body TEXT,
    message_data TEXT,
    priority VARCHAR(20) DEFAULT 'normal',
    status VARCHAR(50) DEFAULT 'pending',
    execute_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP,
    error_message TEXT,
    retry_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ===================
-- CRON SYSTEM
-- ===================

-- Cron tasks
CREATE TABLE IF NOT EXISTS tusk_cron_tasks (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL UNIQUE,
    schedule VARCHAR(100) NOT NULL,
    command TEXT NOT NULL,
    description TEXT,
    enabled BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_run TIMESTAMP NULL,
    last_status VARCHAR(20) DEFAULT 'pending',
    run_count INTEGER DEFAULT 0,
    failure_count INTEGER DEFAULT 0
);

-- Cron logs
CREATE TABLE IF NOT EXISTS tusk_cron_logs (
    id SERIAL PRIMARY KEY,
    task_id INTEGER REFERENCES tusk_cron_tasks(id) ON DELETE CASCADE,
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP NULL,
    success BOOLEAN DEFAULT FALSE,
    output TEXT,
    error_message TEXT,
    duration_seconds NUMERIC(10,3),
    memory_peak_mb NUMERIC(10,2)
);

-- ===================
-- SECURITY SYSTEM
-- ===================

-- Banned IPs
CREATE TABLE IF NOT EXISTS tusk_banned_ips (
    id SERIAL PRIMARY KEY,
    ip_address VARCHAR(45) UNIQUE NOT NULL,
    reason TEXT NOT NULL,
    threat_level VARCHAR(20) DEFAULT 'medium',
    banned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP,
    banned_by VARCHAR(255) DEFAULT 'system',
    is_active BOOLEAN DEFAULT TRUE
);

-- Trap activity
CREATE TABLE IF NOT EXISTS tusk_trap_activity (
    id SERIAL PRIMARY KEY,
    ip_address VARCHAR(45) NOT NULL,
    action VARCHAR(50) NOT NULL,
    reason TEXT,
    threat_level VARCHAR(20) DEFAULT 'low',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    user_agent TEXT,
    request_data JSONB
);

-- Threat log
CREATE TABLE IF NOT EXISTS tusk_threat_log (
    id SERIAL PRIMARY KEY,
    ip_address VARCHAR(45) NOT NULL,
    threat_type VARCHAR(100) NOT NULL,
    severity VARCHAR(20) DEFAULT 'medium',
    description TEXT,
    detected_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    user_agent TEXT,
    request_uri TEXT,
    blocked BOOLEAN DEFAULT FALSE
);

-- ===================
-- CHAT SYSTEM (Infrasound)
-- ===================

-- Chat rooms
CREATE TABLE IF NOT EXISTS chat_rooms (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    is_private BOOLEAN DEFAULT FALSE,
    created_by INTEGER REFERENCES users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Chat messages
CREATE TABLE IF NOT EXISTS chat_messages (
    id SERIAL PRIMARY KEY,
    room_id INTEGER REFERENCES chat_rooms(id) ON DELETE CASCADE,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    message TEXT NOT NULL,
    message_type VARCHAR(50) DEFAULT 'text',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    edited_at TIMESTAMP,
    deleted_at TIMESTAMP
);

-- Chat participants
CREATE TABLE IF NOT EXISTS chat_participants (
    id SERIAL PRIMARY KEY,
    room_id INTEGER REFERENCES chat_rooms(id) ON DELETE CASCADE,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    role VARCHAR(50) DEFAULT 'member',
    UNIQUE(room_id, user_id)
);

-- ===================
-- LAYOUT SYSTEM
-- ===================

-- Layouts
CREATE TABLE IF NOT EXISTS tusk_layouts (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL,
    display_name VARCHAR(150) NOT NULL,
    description TEXT,
    layout_data JSONB NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    is_system BOOLEAN DEFAULT FALSE,
    created_by INTEGER REFERENCES users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- User layouts
CREATE TABLE IF NOT EXISTS tusk_user_layouts (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    layout_id INTEGER REFERENCES tusk_layouts(id) ON DELETE CASCADE,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, layout_id)
);

-- Role layouts
CREATE TABLE IF NOT EXISTS tusk_role_layouts (
    id SERIAL PRIMARY KEY,
    role_id INTEGER REFERENCES roles(id) ON DELETE CASCADE,
    layout_id INTEGER REFERENCES tusk_layouts(id) ON DELETE CASCADE,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(role_id, layout_id)
);

-- ===================
-- SEARCH ANALYTICS
-- ===================

-- Search analytics
CREATE TABLE IF NOT EXISTS search_analytics (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    query TEXT NOT NULL,
    results_count INTEGER DEFAULT 0,
    execution_time REAL DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ip_address VARCHAR(45),
    user_agent TEXT
);

-- ===================
-- INDEXES FOR NEW TABLES
-- ===================

-- Herd indexes
CREATE INDEX IF NOT EXISTS idx_herd_auth_logs_type ON herd_auth_logs(type);
CREATE INDEX IF NOT EXISTS idx_herd_auth_logs_user_id ON herd_auth_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_herd_auth_logs_created_at ON herd_auth_logs(created_at);

-- Email indexes
CREATE INDEX IF NOT EXISTS idx_email_tracking_tracking_id ON email_tracking(tracking_id);
CREATE INDEX IF NOT EXISTS idx_email_tracking_recipient_email ON email_tracking(recipient_email);
CREATE INDEX IF NOT EXISTS idx_email_tracking_sent_at ON email_tracking(sent_at);
CREATE INDEX IF NOT EXISTS idx_email_queue_status ON email_queue(status);
CREATE INDEX IF NOT EXISTS idx_email_queue_execute_at ON email_queue(execute_at);

-- Cron indexes
CREATE INDEX IF NOT EXISTS idx_cron_tasks_enabled ON tusk_cron_tasks(enabled);
CREATE INDEX IF NOT EXISTS idx_cron_tasks_schedule ON tusk_cron_tasks(schedule);
CREATE INDEX IF NOT EXISTS idx_cron_logs_task_id ON tusk_cron_logs(task_id);
CREATE INDEX IF NOT EXISTS idx_cron_logs_started_at ON tusk_cron_logs(started_at);

-- Security indexes
CREATE INDEX IF NOT EXISTS idx_banned_ips_ip_address ON tusk_banned_ips(ip_address);
CREATE INDEX IF NOT EXISTS idx_banned_ips_is_active ON tusk_banned_ips(is_active);
CREATE INDEX IF NOT EXISTS idx_trap_activity_ip_address ON tusk_trap_activity(ip_address);
CREATE INDEX IF NOT EXISTS idx_trap_activity_created_at ON tusk_trap_activity(created_at);
CREATE INDEX IF NOT EXISTS idx_threat_log_ip_address ON tusk_threat_log(ip_address);
CREATE INDEX IF NOT EXISTS idx_threat_log_detected_at ON tusk_threat_log(detected_at);

-- Chat indexes
CREATE INDEX IF NOT EXISTS idx_chat_messages_room_id ON chat_messages(room_id);
CREATE INDEX IF NOT EXISTS idx_chat_messages_user_id ON chat_messages(user_id);
CREATE INDEX IF NOT EXISTS idx_chat_messages_created_at ON chat_messages(created_at);
CREATE INDEX IF NOT EXISTS idx_chat_participants_room_id ON chat_participants(room_id);
CREATE INDEX IF NOT EXISTS idx_chat_participants_user_id ON chat_participants(user_id);

-- Layout indexes  
CREATE INDEX IF NOT EXISTS idx_tusk_layouts_name ON tusk_layouts(name);
CREATE INDEX IF NOT EXISTS idx_tusk_layouts_is_active ON tusk_layouts(is_active);
CREATE INDEX IF NOT EXISTS idx_tusk_user_layouts_user_id ON tusk_user_layouts(user_id);
CREATE INDEX IF NOT EXISTS idx_tusk_role_layouts_role_id ON tusk_role_layouts(role_id);

-- Search indexes
CREATE INDEX IF NOT EXISTS idx_search_analytics_user_id ON search_analytics(user_id);
CREATE INDEX IF NOT EXISTS idx_search_analytics_created_at ON search_analytics(created_at); 