#!/usr/bin/env node
/**
 * TuskLang Express.js Web Server
 * ==============================
 * Production-ready web framework with enterprise-grade security
 * 
 * Features:
 * - Express.js web application with TSK integration
 * - Advanced authentication and authorization
 * - Security middleware (Helmet, CORS, Rate Limiting)
 * - Real-time WebSocket support
 * - Comprehensive logging and monitoring
 * - Performance optimization
 * - Error handling and validation
 */

const express = require('express');
const helmet = require('helmet');
const cors = require('cors');
const compression = require('compression');
const morgan = require('morgan');
const rateLimit = require('express-rate-limit');
const session = require('express-session');
const path = require('path');
const http = require('http');
const socketIo = require('socket.io');
const winston = require('winston');
const expressWinston = require('express-winston');

// Import TuskLang core
const TuskLang = require('../index.js');
const PeanutConfig = require('../peanut-config.js');

// Import security and auth modules
const SecurityManager = require('../security/security-manager.js');
const AuthMiddleware = require('../auth/auth-middleware.js');
const JWTManager = require('../auth/jwt-manager.js');

// Import web routes
const apiRoutes = require('./routes/api.js');
const authRoutes = require('./routes/auth.js');
const webRoutes = require('./routes/web.js');
const adminRoutes = require('./routes/admin.js');

// Import middleware
const { errorHandler } = require('./middleware/error-handler.js');
const { requestValidator } = require('./middleware/request-validator.js');
const { performanceMonitor } = require('./middleware/performance-monitor.js');

class TuskWebServer {
    constructor(options = {}) {
        this.options = {
            port: options.port || process.env.PORT || 3000,
            host: options.host || process.env.HOST || 'localhost',
            environment: options.environment || process.env.NODE_ENV || 'development',
            secret: options.secret || process.env.SESSION_SECRET || 'tusk-web-secret-key',
            ...options
        };

        this.app = express();
        this.server = null;
        this.io = null;
        this.securityManager = null;
        this.jwtManager = null;
        this.logger = null;

        this.initializeLogger();
        this.initializeSecurity();
        this.initializeMiddleware();
        this.initializeRoutes();
        this.initializeWebSockets();
        this.initializeErrorHandling();
    }

    /**
     * Initialize Winston logger
     */
    initializeLogger() {
        this.logger = winston.createLogger({
            level: this.options.environment === 'production' ? 'info' : 'debug',
            format: winston.format.combine(
                winston.format.timestamp(),
                winston.format.errors({ stack: true }),
                winston.format.json()
            ),
            defaultMeta: { service: 'tusk-web-server' },
            transports: [
                new winston.transports.File({ filename: 'logs/error.log', level: 'error' }),
                new winston.transports.File({ filename: 'logs/combined.log' }),
                new winston.transports.Console({
                    format: winston.format.combine(
                        winston.format.colorize(),
                        winston.format.simple()
                    )
                })
            ]
        });

        // Express Winston middleware
        this.app.use(expressWinston.logger({
            winstonInstance: this.logger,
            meta: true,
            msg: "HTTP {{req.method}} {{req.url}}",
            expressFormat: true,
            colorize: false
        }));
    }

    /**
     * Initialize security components
     */
    initializeSecurity() {
        this.securityManager = new SecurityManager({
            sessionTimeout: 3600000, // 1 hour
            maxLoginAttempts: 5,
            lockoutDuration: 900000 // 15 minutes
        });

        this.jwtManager = new JWTManager({
            secret: this.options.secret,
            expiresIn: '24h'
        });
    }

    /**
     * Initialize Express middleware
     */
    initializeMiddleware() {
        // Security middleware
        this.app.use(helmet({
            contentSecurityPolicy: {
                directives: {
                    defaultSrc: ["'self'"],
                    styleSrc: ["'self'", "'unsafe-inline'"],
                    scriptSrc: ["'self'"],
                    imgSrc: ["'self'", "data:", "https:"],
                    connectSrc: ["'self'", "ws:", "wss:"]
                }
            },
            crossOriginEmbedderPolicy: false
        }));

        // CORS configuration
        this.app.use(cors({
            origin: this.options.environment === 'production' 
                ? ['https://tuskt.sk', 'https://api.tuskt.sk']
                : true,
            credentials: true,
            methods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
            allowedHeaders: ['Content-Type', 'Authorization', 'X-Requested-With']
        }));

        // Rate limiting
        const limiter = rateLimit({
            windowMs: 15 * 60 * 1000, // 15 minutes
            max: 100, // limit each IP to 100 requests per windowMs
            message: {
                error: 'Too many requests from this IP, please try again later.',
                retryAfter: '15 minutes'
            },
            standardHeaders: true,
            legacyHeaders: false
        });
        this.app.use('/api/', limiter);

        // Compression
        this.app.use(compression());

        // Body parsing
        this.app.use(express.json({ limit: '10mb' }));
        this.app.use(express.urlencoded({ extended: true, limit: '10mb' }));

        // Session management
        this.app.use(session({
            secret: this.options.secret,
            resave: false,
            saveUninitialized: false,
            cookie: {
                secure: this.options.environment === 'production',
                httpOnly: true,
                maxAge: 24 * 60 * 60 * 1000 // 24 hours
            }
        }));

        // Morgan HTTP logging
        this.app.use(morgan('combined', {
            stream: {
                write: (message) => this.logger.info(message.trim())
            }
        }));

        // Performance monitoring
        this.app.use(performanceMonitor);

        // Request validation
        this.app.use(requestValidator);

        // Static file serving
        this.app.use('/static', express.static(path.join(__dirname, 'public')));
        this.app.use('/assets', express.static(path.join(__dirname, 'assets')));

        // TuskLang integration middleware
        this.app.use((req, res, next) => {
            req.tusk = TuskLang;
            req.peanut = PeanutConfig;
            req.security = this.securityManager;
            req.jwt = this.jwtManager;
            next();
        });
    }

    /**
     * Initialize application routes
     */
    initializeRoutes() {
        // Health check endpoint
        this.app.get('/health', (req, res) => {
            res.json({
                status: 'healthy',
                timestamp: new Date().toISOString(),
                uptime: process.uptime(),
                environment: this.options.environment,
                version: require('../package.json').version
            });
        });

        // API routes
        this.app.use('/api/v1', apiRoutes);
        this.app.use('/api/auth', authRoutes);
        this.app.use('/api/admin', AuthMiddleware.requireAuth, adminRoutes);

        // Web routes
        this.app.use('/', webRoutes);

        // 404 handler
        this.app.use('*', (req, res) => {
            res.status(404).json({
                error: 'Not Found',
                message: `Route ${req.originalUrl} not found`,
                timestamp: new Date().toISOString()
            });
        });
    }

    /**
     * Initialize WebSocket support
     */
    initializeWebSockets() {
        this.server = http.createServer(this.app);
        this.io = socketIo(this.server, {
            cors: {
                origin: this.options.environment === 'production' 
                    ? ['https://tuskt.sk']
                    : true,
                methods: ["GET", "POST"]
            }
        });

        // WebSocket authentication middleware
        this.io.use((socket, next) => {
            const token = socket.handshake.auth.token;
            if (token) {
                try {
                    const decoded = this.jwtManager.verify(token);
                    socket.userId = decoded.userId;
                    socket.username = decoded.username;
                } catch (error) {
                    return next(new Error('Authentication error'));
                }
            }
            next();
        });

        // WebSocket event handlers
        this.io.on('connection', (socket) => {
            this.logger.info(`WebSocket client connected: ${socket.id}`);

            socket.on('join-room', (room) => {
                socket.join(room);
                this.logger.info(`Client ${socket.id} joined room: ${room}`);
            });

            socket.on('leave-room', (room) => {
                socket.leave(room);
                this.logger.info(`Client ${socket.id} left room: ${room}`);
            });

            socket.on('disconnect', () => {
                this.logger.info(`WebSocket client disconnected: ${socket.id}`);
            });
        });
    }

    /**
     * Initialize error handling
     */
    initializeErrorHandling() {
        this.app.use(expressWinston.errorLogger({
            winstonInstance: this.logger
        }));

        this.app.use(errorHandler);
    }

    /**
     * Start the web server
     */
    start() {
        return new Promise((resolve, reject) => {
            try {
                this.server.listen(this.options.port, this.options.host, () => {
                    this.logger.info(`🚀 TuskLang Web Server started successfully!`);
                    this.logger.info(`📍 Server: http://${this.options.host}:${this.options.port}`);
                    this.logger.info(`🔧 Environment: ${this.options.environment}`);
                    this.logger.info(`🔒 Security: Enabled with enterprise-grade protection`);
                    this.logger.info(`⚡ Performance: Optimized for high-throughput operations`);
                    
                    resolve({
                        port: this.options.port,
                        host: this.options.host,
                        environment: this.options.environment
                    });
                });

                this.server.on('error', (error) => {
                    this.logger.error('Server error:', error);
                    reject(error);
                });

            } catch (error) {
                this.logger.error('Failed to start server:', error);
                reject(error);
            }
        });
    }

    /**
     * Stop the web server
     */
    stop() {
        return new Promise((resolve) => {
            if (this.server) {
                this.server.close(() => {
                    this.logger.info('Web server stopped gracefully');
                    resolve();
                });
            } else {
                resolve();
            }
        });
    }

    /**
     * Get server statistics
     */
    getStats() {
        return {
            uptime: process.uptime(),
            memory: process.memoryUsage(),
            environment: this.options.environment,
            port: this.options.port,
            host: this.options.host,
            connections: this.server ? this.server.connections : 0,
            security: this.securityManager.getSecurityStats()
        };
    }

    /**
     * Broadcast message to all connected clients
     */
    broadcast(event, data) {
        if (this.io) {
            this.io.emit(event, data);
        }
    }

    /**
     * Broadcast message to specific room
     */
    broadcastToRoom(room, event, data) {
        if (this.io) {
            this.io.to(room).emit(event, data);
        }
    }
}

// CLI support
if (require.main === module) {
    const server = new TuskWebServer();
    
    server.start()
        .then((config) => {
            console.log(`✅ TuskLang Web Server running on http://${config.host}:${config.port}`);
        })
        .catch((error) => {
            console.error('❌ Failed to start server:', error);
            process.exit(1);
        });

    // Graceful shutdown
    process.on('SIGTERM', () => {
        console.log('SIGTERM received, shutting down gracefully...');
        server.stop().then(() => process.exit(0));
    });

    process.on('SIGINT', () => {
        console.log('SIGINT received, shutting down gracefully...');
        server.stop().then(() => process.exit(0));
    });
}

module.exports = TuskWebServer; 