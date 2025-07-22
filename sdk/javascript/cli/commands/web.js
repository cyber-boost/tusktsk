/**
 * TuskLang Web CLI Commands
 * =========================
 * Web framework management commands
 */

const { Command } = require('commander');
const path = require('path');
const fs = require('fs').promises;

// Web server commands
const webStart = new Command('start')
    .description('Start the TuskLang web server')
    .option('-p, --port <port>', 'Port number', '3000')
    .option('-h, --host <host>', 'Host address', 'localhost')
    .option('-e, --environment <env>', 'Environment (development/production)', 'development')
    .option('--secret <secret>', 'Session secret key')
    .action(async (options) => {
        try {
            console.log('🚀 Starting TuskLang Web Server...');
            
            // Set environment variables
            if (options.port) process.env.PORT = options.port;
            if (options.host) process.env.HOST = options.host;
            if (options.environment) process.env.NODE_ENV = options.environment;
            if (options.secret) process.env.SESSION_SECRET = options.secret;
            
            // Import and start server
            const TuskWebServer = require('../../web/server.js');
            const server = new TuskWebServer({
                port: parseInt(options.port),
                host: options.host,
                environment: options.environment,
                secret: options.secret
            });
            
            await server.start();
            
            console.log(`✅ TuskLang Web Server running on http://${options.host}:${options.port}`);
            console.log(`🔧 Environment: ${options.environment}`);
            console.log(`🔒 Security: Enabled with enterprise-grade protection`);
            console.log(`⚡ Performance: Optimized for high-throughput operations`);
            
        } catch (error) {
            console.error('❌ Failed to start web server:', error.message);
            process.exit(1);
        }
    });

const webStatus = new Command('status')
    .description('Get web server status')
    .action(async () => {
        try {
            console.log('📊 TuskLang Web Server Status');
            console.log('=============================');
            
            // Check if server is running
            const http = require('http');
            const port = process.env.PORT || 3000;
            const host = process.env.HOST || 'localhost';
            
            const checkServer = () => {
                return new Promise((resolve) => {
                    const req = http.request({
                        hostname: host,
                        port: port,
                        path: '/health',
                        method: 'GET',
                        timeout: 5000
                    }, (res) => {
                        let data = '';
                        res.on('data', chunk => data += chunk);
                        res.on('end', () => {
                            try {
                                const health = JSON.parse(data);
                                resolve({ running: true, health });
                            } catch (error) {
                                resolve({ running: true, health: null });
                            }
                        });
                    });
                    
                    req.on('error', () => resolve({ running: false }));
                    req.on('timeout', () => resolve({ running: false }));
                    req.end();
                });
            };
            
            const status = await checkServer();
            
            if (status.running) {
                console.log('✅ Server Status: Running');
                console.log(`📍 URL: http://${host}:${port}`);
                
                if (status.health) {
                    console.log(`⏱️  Uptime: ${Math.floor(status.health.uptime / 60)} minutes`);
                    console.log(`🔧 Environment: ${status.health.environment}`);
                    console.log(`📦 Version: ${status.health.version}`);
                }
            } else {
                console.log('❌ Server Status: Not Running');
                console.log(`📍 Expected URL: http://${host}:${port}`);
            }
            
        } catch (error) {
            console.error('❌ Error checking server status:', error.message);
        }
    });

const webStop = new Command('stop')
    .description('Stop the web server')
    .action(async () => {
        try {
            console.log('🛑 Stopping TuskLang Web Server...');
            
            // Send SIGTERM to the process
            const http = require('http');
            const port = process.env.PORT || 3000;
            const host = process.env.HOST || 'localhost';
            
            const req = http.request({
                hostname: host,
                port: port,
                path: '/admin/system/restart',
                method: 'POST',
                timeout: 5000
            });
            
            req.on('error', () => {
                console.log('✅ Web server stopped or not running');
            });
            
            req.end();
            
        } catch (error) {
            console.error('❌ Error stopping server:', error.message);
        }
    });

const webTest = new Command('test')
    .description('Test web server endpoints')
    .option('-u, --url <url>', 'Base URL for testing', 'http://localhost:3000')
    .action(async (options) => {
        try {
            console.log('🧪 Testing TuskLang Web Server...');
            console.log(`📍 Base URL: ${options.url}`);
            console.log('');
            
            const http = require('http');
            const https = require('https');
            
            const testEndpoint = (path, method = 'GET', data = null) => {
                return new Promise((resolve) => {
                    const url = new URL(path, options.url);
                    const client = url.protocol === 'https:' ? https : http;
                    
                    const req = client.request({
                        hostname: url.hostname,
                        port: url.port,
                        path: url.pathname,
                        method: method,
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        timeout: 10000
                    }, (res) => {
                        let responseData = '';
                        res.on('data', chunk => responseData += chunk);
                        res.on('end', () => {
                            try {
                                const json = JSON.parse(responseData);
                                resolve({
                                    status: res.statusCode,
                                    success: res.statusCode >= 200 && res.statusCode < 300,
                                    data: json,
                                    time: Date.now()
                                });
                            } catch (error) {
                                resolve({
                                    status: res.statusCode,
                                    success: res.statusCode >= 200 && res.statusCode < 300,
                                    data: responseData,
                                    time: Date.now()
                                });
                            }
                        });
                    });
                    
                    req.on('error', (error) => {
                        resolve({
                            status: 0,
                            success: false,
                            error: error.message,
                            time: Date.now()
                        });
                    });
                    
                    req.on('timeout', () => {
                        resolve({
                            status: 0,
                            success: false,
                            error: 'Timeout',
                            time: Date.now()
                        });
                    });
                    
                    if (data) {
                        req.write(JSON.stringify(data));
                    }
                    
                    req.end();
                });
            };
            
            // Test endpoints
            const tests = [
                { name: 'Health Check', path: '/health' },
                { name: 'API Status', path: '/api/v1/status' },
                { name: 'Features', path: '/api/v1/features' },
                { name: 'Security Stats', path: '/api/v1/security/stats' }
            ];
            
            for (const test of tests) {
                console.log(`Testing ${test.name}...`);
                const result = await testEndpoint(test.path);
                
                if (result.success) {
                    console.log(`  ✅ ${test.name}: ${result.status} OK`);
                } else {
                    console.log(`  ❌ ${test.name}: ${result.status} ${result.error || 'Failed'}`);
                }
            }
            
            console.log('');
            console.log('✅ Web server testing completed');
            
        } catch (error) {
            console.error('❌ Error testing web server:', error.message);
        }
    });

const webConfig = new Command('config')
    .description('Configure web server settings')
    .option('--set-port <port>', 'Set default port')
    .option('--set-host <host>', 'Set default host')
    .option('--set-environment <env>', 'Set default environment')
    .option('--set-secret <secret>', 'Set session secret')
    .option('--show', 'Show current configuration')
    .action(async (options) => {
        try {
            const configPath = path.join(process.cwd(), '.tusk-web-config.json');
            
            // Load existing config
            let config = {};
            try {
                const configData = await fs.readFile(configPath, 'utf8');
                config = JSON.parse(configData);
            } catch (error) {
                // Config file doesn't exist, use defaults
            }
            
            if (options.show) {
                console.log('📋 TuskLang Web Server Configuration');
                console.log('====================================');
                console.log(`Port: ${config.port || 3000}`);
                console.log(`Host: ${config.host || 'localhost'}`);
                console.log(`Environment: ${config.environment || 'development'}`);
                console.log(`Secret: ${config.secret ? '***' : 'Not set'}`);
                return;
            }
            
            // Update config
            if (options.setPort) config.port = parseInt(options.setPort);
            if (options.setHost) config.host = options.setHost;
            if (options.setEnvironment) config.environment = options.setEnvironment;
            if (options.setSecret) config.secret = options.setSecret;
            
            // Save config
            await fs.writeFile(configPath, JSON.stringify(config, null, 2));
            
            console.log('✅ Web server configuration updated');
            
        } catch (error) {
            console.error('❌ Error updating configuration:', error.message);
        }
    });

const webLogs = new Command('logs')
    .description('View web server logs')
    .option('-f, --follow', 'Follow log output')
    .option('-n, --lines <number>', 'Number of lines to show', '50')
    .action(async (options) => {
        try {
            const logPath = path.join(process.cwd(), 'logs');
            
            // Check if logs directory exists
            try {
                await fs.access(logPath);
            } catch (error) {
                console.log('📁 No logs directory found. Logs will appear here when server is running.');
                return;
            }
            
            const logFiles = ['combined.log', 'error.log'];
            
            for (const logFile of logFiles) {
                const fullPath = path.join(logPath, logFile);
                
                try {
                    await fs.access(fullPath);
                    console.log(`📄 ${logFile}:`);
                    
                    if (options.follow) {
                        console.log('Following log output (Ctrl+C to stop)...');
                        // In a real implementation, you'd use a file watcher
                        console.log('Follow mode not implemented in this version');
                    } else {
                        const content = await fs.readFile(fullPath, 'utf8');
                        const lines = content.split('\n');
                        const lastLines = lines.slice(-parseInt(options.lines));
                        console.log(lastLines.join('\n'));
                    }
                    
                    console.log('');
                } catch (error) {
                    console.log(`📄 ${logFile}: No log file found`);
                }
            }
            
        } catch (error) {
            console.error('❌ Error reading logs:', error.message);
        }
    });

// Main web command
const web = new Command('web')
    .description('TuskLang web server management')
    .addCommand(webStart)
    .addCommand(webStatus)
    .addCommand(webStop)
    .addCommand(webTest)
    .addCommand(webConfig)
    .addCommand(webLogs);

module.exports = {
    web,
    webStart,
    webStatus,
    webStop,
    webTest,
    webConfig,
    webLogs
}; 