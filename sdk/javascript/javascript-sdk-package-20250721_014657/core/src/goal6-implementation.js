/**
 * Goal 6 Implementation for TuskLang JavaScript SDK
 * Integrates Web Framework, Template Engine, and Session Management
 * 
 * Goals:
 * - g6.1: Advanced Web Framework
 * - g6.2: Advanced Template Engine
 * - g6.3: Advanced Session Management
 */

const { WebFramework, commonMiddleware } = require('./web-framework');
const { TemplateEngine, predefinedTemplates } = require('./template-engine');
const { SessionManager, MemoryStorage } = require('./session-management');

class Goal6Implementation {
    constructor(options = {}) {
        this.webFramework = new WebFramework(options.web || {});
        this.templateEngine = new TemplateEngine(options.template || {});
        this.sessionManager = new SessionManager(options.session || {});
        
        this.setupMiddleware();
        this.setupRoutes();
        this.setupTemplates();
    }

    /**
     * Setup middleware for the web framework
     */
    setupMiddleware() {
        this.webFramework
            .use(commonMiddleware.logger)
            .use(commonMiddleware.cors)
            .use(commonMiddleware.securityHeaders)
            .use(this.sessionManager.middleware());
    }

    /**
     * Setup default routes
     */
    setupRoutes() {
        // Home route with template rendering
        this.webFramework.get('/', async (req, res) => {
            const template = `
                <!DOCTYPE html>
                <html>
                <head>
                    <title>{{ title }}</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 40px; }
                        .container { max-width: 800px; margin: 0 auto; }
                        .header { background: #f0f0f0; padding: 20px; border-radius: 5px; }
                        .content { margin: 20px 0; }
                        .session-info { background: #e8f4f8; padding: 15px; border-radius: 5px; }
                    </style>
                </head>
                <body>
                    <div class="container">
                        <div class="header">
                            <h1>{{ title }}</h1>
                            <p>{{ description }}</p>
                        </div>
                        
                        <div class="content">
                            <h2>Welcome to TuskLang Web Framework</h2>
                            <p>This is a demonstration of the integrated web framework, template engine, and session management.</p>
                            
                            {% if session %}
                                <div class="session-info">
                                    <h3>Session Information</h3>
                                    <p><strong>Session ID:</strong> {{ session.id }}</p>
                                    <p><strong>User ID:</strong> {{ session.userId || 'Not set' }}</p>
                                    <p><strong>Created:</strong> {{ session.createdAt | date }}</p>
                                    <p><strong>Last Accessed:</strong> {{ session.lastAccessed | date }}</p>
                                    
                                    {% if session.data %}
                                        <h4>Session Data:</h4>
                                        <ul>
                                        {% each key, value in session.data %}
                                            <li><strong>{{ key }}:</strong> {{ value }}</li>
                                        {% endeach %}
                                        </ul>
                                    {% endif %}
                                </div>
                            {% else %}
                                <p>No active session found.</p>
                            {% endif %}
                            
                            <h3>Available Actions</h3>
                            <ul>
                                <li><a href="/login">Login</a></li>
                                <li><a href="/dashboard">Dashboard</a></li>
                                <li><a href="/logout">Logout</a></li>
                                <li><a href="/api/session">Session API</a></li>
                                <li><a href="/api/stats">Framework Stats</a></li>
                            </ul>
                        </div>
                    </div>
                </body>
                </html>
            `;

            const data = {
                title: 'TuskLang Web Framework',
                description: 'Advanced web framework with template engine and session management',
                session: req.session
            };

            const html = this.templateEngine.render(template, data);
            res.html(html);
        });

        // Login page
        this.webFramework.get('/login', async (req, res) => {
            const template = `
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Login - TuskLang</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 40px; }
                        .container { max-width: 400px; margin: 0 auto; }
                        .form-group { margin: 15px 0; }
                        label { display: block; margin-bottom: 5px; }
                        input { width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 3px; }
                        button { background: #007cba; color: white; padding: 10px 20px; border: none; border-radius: 3px; cursor: pointer; }
                        .error { color: red; margin: 10px 0; }
                    </style>
                </head>
                <body>
                    <div class="container">
                        <h1>Login</h1>
                        
                        {% if error %}
                            <div class="error">{{ error }}</div>
                        {% endif %}
                        
                        <form method="POST" action="/login">
                            <div class="form-group">
                                <label for="username">Username:</label>
                                <input type="text" id="username" name="username" required>
                            </div>
                            
                            <div class="form-group">
                                <label for="password">Password:</label>
                                <input type="password" id="password" name="password" required>
                            </div>
                            
                            <button type="submit">Login</button>
                        </form>
                        
                        <p><a href="/">Back to Home</a></p>
                    </div>
                </body>
                </html>
            `;

            const data = {
                error: req.query.error
            };

            const html = this.templateEngine.render(template, data);
            res.html(html);
        });

        // Login POST handler
        this.webFramework.post('/login', async (req, res) => {
            const { username, password } = req.body;

            // Simple authentication (in real app, use proper auth)
            if (username === 'admin' && password === 'password') {
                const session = await this.sessionManager.createSession(username, {
                    username: username,
                    loginTime: Date.now(),
                    role: 'admin'
                });

                req.session = session;
                req.sessionId = session.id;
                this.sessionManager.setSessionCookie(res, session.id);

                res.redirect('/dashboard');
            } else {
                res.redirect('/login?error=Invalid credentials');
            }
        });

        // Dashboard
        this.webFramework.get('/dashboard', async (req, res) => {
            if (!req.session || !req.session.userId) {
                res.redirect('/login');
                return;
            }

            const template = `
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Dashboard - TuskLang</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 40px; }
                        .container { max-width: 800px; margin: 0 auto; }
                        .header { background: #007cba; color: white; padding: 20px; border-radius: 5px; }
                        .content { margin: 20px 0; }
                        .stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin: 20px 0; }
                        .stat-card { background: #f8f9fa; padding: 20px; border-radius: 5px; text-align: center; }
                        .stat-number { font-size: 2em; font-weight: bold; color: #007cba; }
                        .logout { background: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 3px; }
                    </style>
                </head>
                <body>
                    <div class="container">
                        <div class="header">
                            <h1>Dashboard</h1>
                            <p>Welcome, {{ session.data.username }}!</p>
                        </div>
                        
                        <div class="content">
                            <h2>Framework Statistics</h2>
                            <div class="stats">
                                <div class="stat-card">
                                    <div class="stat-number">{{ webStats.routes }}</div>
                                    <div>Routes</div>
                                </div>
                                <div class="stat-card">
                                    <div class="stat-number">{{ webStats.middleware }}</div>
                                    <div>Middleware</div>
                                </div>
                                <div class="stat-card">
                                    <div class="stat-number">{{ sessionStats.activeSessions }}</div>
                                    <div>Active Sessions</div>
                                </div>
                                <div class="stat-card">
                                    <div class="stat-number">{{ templateStats.cacheSize }}</div>
                                    <div>Cached Templates</div>
                                </div>
                            </div>
                            
                            <h3>Session Information</h3>
                            <p><strong>Session ID:</strong> {{ session.id }}</p>
                            <p><strong>Login Time:</strong> {{ session.data.loginTime | date }}</p>
                            <p><strong>Role:</strong> {{ session.data.role }}</p>
                            
                            <p><a href="/" class="logout">Back to Home</a> | <a href="/logout" class="logout">Logout</a></p>
                        </div>
                    </div>
                </body>
                </html>
            `;

            const data = {
                session: req.session,
                webStats: this.webFramework.getStats(),
                sessionStats: this.sessionManager.getStats(),
                templateStats: this.templateEngine.getStats()
            };

            const html = this.templateEngine.render(template, data);
            res.html(html);
        });

        // Logout
        this.webFramework.get('/logout', async (req, res) => {
            await res.session.destroy();
            res.redirect('/');
        });

        // API routes
        this.webFramework.get('/api/session', async (req, res) => {
            res.json({
                session: req.session ? {
                    id: req.session.id,
                    userId: req.session.userId,
                    data: req.session.data,
                    createdAt: req.session.createdAt,
                    lastAccessed: req.session.lastAccessed
                } : null
            });
        });

        this.webFramework.get('/api/stats', async (req, res) => {
            res.json({
                web: this.webFramework.getStats(),
                template: this.templateEngine.getStats(),
                session: this.sessionManager.getStats()
            });
        });

        // Error handlers
        this.webFramework.error(404, async (req, res) => {
            const template = `
                <!DOCTYPE html>
                <html>
                <head>
                    <title>404 - Not Found</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 40px; text-align: center; }
                        .error { color: #dc3545; font-size: 4em; margin: 20px 0; }
                    </style>
                </head>
                <body>
                    <div class="error">404</div>
                    <h1>Page Not Found</h1>
                    <p>The requested page "{{ pathname }}" could not be found.</p>
                    <p><a href="/">Go Home</a></p>
                </body>
                </html>
            `;

            const html = this.templateEngine.render(template, { pathname: req.pathname });
            res.html(html);
        });

        this.webFramework.error(500, async (req, res, error) => {
            const template = `
                <!DOCTYPE html>
                <html>
                <head>
                    <title>500 - Server Error</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 40px; text-align: center; }
                        .error { color: #dc3545; font-size: 4em; margin: 20px 0; }
                    </style>
                </head>
                <body>
                    <div class="error">500</div>
                    <h1>Internal Server Error</h1>
                    <p>Something went wrong on our end.</p>
                    {% if debug %}
                        <pre>{{ error }}</pre>
                    {% endif %}
                    <p><a href="/">Go Home</a></p>
                </body>
                </html>
            `;

            const html = this.templateEngine.render(template, { 
                error: error?.message || 'Unknown error',
                debug: this.webFramework.options.debug
            });
            res.html(html);
        });
    }

    /**
     * Setup predefined templates
     */
    setupTemplates() {
        // Register predefined templates as partials
        for (const [name, template] of Object.entries(predefinedTemplates.html)) {
            this.templateEngine.registerPartial(name, template);
        }

        // Register custom filters
        this.templateEngine.registerFilter('json', (value) => JSON.stringify(value, null, 2));
        this.templateEngine.registerFilter('currency', (value, currency = 'USD') => {
            return new Intl.NumberFormat('en-US', {
                style: 'currency',
                currency: currency
            }).format(value);
        });
    }

    /**
     * Start the web server
     */
    start(port = 3000, host = 'localhost') {
        return this.webFramework.listen(port, host, () => {
            console.log(`Goal 6 Implementation started at http://${host}:${port}`);
            console.log('Features:');
            console.log('- Advanced Web Framework with routing and middleware');
            console.log('- Template Engine with variables, conditionals, and loops');
            console.log('- Session Management with authentication and security');
            console.log('- Integrated demo with login, dashboard, and API endpoints');
        });
    }

    /**
     * Stop the web server
     */
    stop() {
        this.sessionManager.stopCleanup();
        return this.webFramework.close();
    }

    /**
     * Get comprehensive statistics
     */
    getStats() {
        return {
            web: this.webFramework.getStats(),
            template: this.templateEngine.getStats(),
            session: this.sessionManager.getStats(),
            timestamp: new Date().toISOString()
        };
    }

    /**
     * Test all components
     */
    async test() {
        const results = {
            webFramework: true,
            templateEngine: true,
            sessionManagement: true,
            integration: true
        };

        try {
            // Test template engine
            const testTemplate = 'Hello {{ name }}! You have {{ count }} messages.';
            const testData = { name: 'World', count: 5 };
            const rendered = this.templateEngine.render(testTemplate, testData);
            if (rendered !== 'Hello World! You have 5 messages.') {
                results.templateEngine = false;
            }

            // Test session management
            const session = await this.sessionManager.createSession('test', { test: true });
            const retrieved = await this.sessionManager.getSession(session.id);
            if (!retrieved || retrieved.data.test !== true) {
                results.sessionManagement = false;
            }

            // Test web framework
            const stats = this.webFramework.getStats();
            if (stats.routes === 0) {
                results.webFramework = false;
            }

        } catch (error) {
            console.error('Test error:', error);
            results.integration = false;
        }

        return results;
    }
}

module.exports = { Goal6Implementation }; 