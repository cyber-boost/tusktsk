/**
 * Advanced Web Framework for TuskLang JavaScript SDK
 * Goal 6.1: Advanced Web Framework
 * 
 * Features:
 * - HTTP server with routing and middleware
 * - Request/response handling with async support
 * - Middleware pipeline
 * - Route parameters and query string parsing
 * - Static file serving
 * - Error handling and logging
 */

const http = require('http');
const url = require('url');
const fs = require('fs').promises;
const path = require('path');

class WebFramework {
    constructor() {
        this.routes = new Map();
        this.middleware = [];
        this.staticPaths = new Map();
        this.errorHandlers = new Map();
        this.server = null;
        this.port = 3000;
        this.host = 'localhost';
    }

    /**
     * Add middleware to the pipeline
     */
    use(middleware) {
        if (typeof middleware === 'function') {
            this.middleware.push(middleware);
        }
        return this;
    }

    /**
     * Register a route handler
     */
    route(method, path, handler) {
        const key = `${method.toUpperCase()}:${path}`;
        this.routes.set(key, handler);
        return this;
    }

    /**
     * GET route shorthand
     */
    get(path, handler) {
        return this.route('GET', path, handler);
    }

    /**
     * POST route shorthand
     */
    post(path, handler) {
        return this.route('POST', path, handler);
    }

    /**
     * PUT route shorthand
     */
    put(path, handler) {
        return this.route('PUT', path, handler);
    }

    /**
     * DELETE route shorthand
     */
    delete(path, handler) {
        return this.route('DELETE', path, handler);
    }

    /**
     * Serve static files from a directory
     */
    static(route, directory) {
        this.staticPaths.set(route, directory);
        return this;
    }

    /**
     * Register error handler for specific status codes
     */
    error(statusCode, handler) {
        this.errorHandlers.set(statusCode, handler);
        return this;
    }

    /**
     * Parse route parameters from URL pattern
     */
    parseRouteParams(pattern, urlPath) {
        const params = {};
        const patternParts = pattern.split('/');
        const urlParts = urlPath.split('/');

        if (patternParts.length !== urlParts.length) {
            return null;
        }

        for (let i = 0; i < patternParts.length; i++) {
            const patternPart = patternParts[i];
            const urlPart = urlParts[i];

            if (patternPart.startsWith(':')) {
                const paramName = patternPart.slice(1);
                params[paramName] = urlPart;
            } else if (patternPart !== urlPart) {
                return null;
            }
        }

        return params;
    }

    /**
     * Find matching route and extract parameters
     */
    findRoute(method, urlPath) {
        for (const [key, handler] of this.routes) {
            const [routeMethod, routePath] = key.split(':');
            
            if (routeMethod === method) {
                const params = this.parseRouteParams(routePath, urlPath);
                if (params !== null) {
                    return { handler, params };
                }
            }
        }
        return null;
    }

    /**
     * Handle static file serving
     */
    async handleStatic(urlPath) {
        for (const [route, directory] of this.staticPaths) {
            if (urlPath.startsWith(route)) {
                const filePath = urlPath.replace(route, '');
                const fullPath = path.join(directory, filePath);
                
                try {
                    const stats = await fs.stat(fullPath);
                    if (stats.isFile()) {
                        const content = await fs.readFile(fullPath);
                        const ext = path.extname(fullPath);
                        const contentType = this.getContentType(ext);
                        return { content, contentType };
                    }
                } catch (error) {
                    // File not found or other error
                }
            }
        }
        return null;
    }

    /**
     * Get content type based on file extension
     */
    getContentType(ext) {
        const contentTypes = {
            '.html': 'text/html',
            '.css': 'text/css',
            '.js': 'application/javascript',
            '.json': 'application/json',
            '.png': 'image/png',
            '.jpg': 'image/jpeg',
            '.jpeg': 'image/jpeg',
            '.gif': 'image/gif',
            '.svg': 'image/svg+xml',
            '.ico': 'image/x-icon'
        };
        return contentTypes[ext] || 'application/octet-stream';
    }

    /**
     * Parse request body
     */
    async parseBody(req) {
        return new Promise((resolve) => {
            let body = '';
            req.on('data', chunk => {
                body += chunk.toString();
            });
            req.on('end', () => {
                try {
                    if (req.headers['content-type']?.includes('application/json')) {
                        resolve(JSON.parse(body));
                    } else {
                        resolve(body);
                    }
                } catch (error) {
                    resolve(body);
                }
            });
        });
    }

    /**
     * Create response object
     */
    createResponse(res) {
        return {
            status: (code) => {
                res.statusCode = code;
                return this.createResponse(res);
            },
            json: (data) => {
                res.setHeader('Content-Type', 'application/json');
                res.end(JSON.stringify(data));
            },
            send: (data) => {
                res.end(data);
            },
            html: (data) => {
                res.setHeader('Content-Type', 'text/html');
                res.end(data);
            },
            redirect: (url) => {
                res.writeHead(302, { 'Location': url });
                res.end();
            },
            setHeader: (name, value) => {
                res.setHeader(name, value);
                return this.createResponse(res);
            }
        };
    }

    /**
     * Create request object
     */
    createRequest(req, params = {}) {
        const parsedUrl = url.parse(req.url, true);
        return {
            method: req.method,
            url: req.url,
            pathname: parsedUrl.pathname,
            query: parsedUrl.query,
            headers: req.headers,
            params,
            body: null, // Will be populated by middleware
            original: req
        };
    }

    /**
     * Execute middleware pipeline
     */
    async executeMiddleware(req, res, index = 0) {
        if (index >= this.middleware.length) {
            return true; // Continue to route handling
        }

        const middleware = this.middleware[index];
        return new Promise((resolve) => {
            middleware(req, res, () => {
                resolve(this.executeMiddleware(req, res, index + 1));
            });
        });
    }

    /**
     * Main request handler
     */
    async handleRequest(req, res) {
        const parsedUrl = url.parse(req.url, true);
        const urlPath = parsedUrl.pathname;

        // Create request and response objects
        const request = this.createRequest(req);
        const response = this.createResponse(res);

        try {
            // Execute middleware pipeline
            await this.executeMiddleware(request, response);

            // Parse request body
            request.body = await this.parseBody(req);

            // Check for static file serving
            const staticFile = await this.handleStatic(urlPath);
            if (staticFile) {
                res.setHeader('Content-Type', staticFile.contentType);
                res.end(staticFile.content);
                return;
            }

            // Find matching route
            const routeMatch = this.findRoute(req.method, urlPath);
            if (routeMatch) {
                request.params = routeMatch.params;
                
                // Execute route handler
                const result = await routeMatch.handler(request, response);
                if (result && !res.headersSent) {
                    response.json(result);
                }
            } else {
                // No route found
                const errorHandler = this.errorHandlers.get(404);
                if (errorHandler) {
                    await errorHandler(request, response);
                } else {
                    res.statusCode = 404;
                    res.end('Not Found');
                }
            }
        } catch (error) {
            console.error('Request handling error:', error);
            
            const errorHandler = this.errorHandlers.get(500);
            if (errorHandler) {
                await errorHandler(request, response, error);
            } else {
                res.statusCode = 500;
                res.end('Internal Server Error');
            }
        }
    }

    /**
     * Start the HTTP server
     */
    listen(port = 3000, host = 'localhost', callback) {
        this.port = port;
        this.host = host;

        this.server = http.createServer((req, res) => {
            this.handleRequest(req, res);
        });

        this.server.listen(port, host, () => {
            console.log(`Web Framework server running at http://${host}:${port}`);
            if (callback) callback();
        });

        return this;
    }

    /**
     * Stop the server
     */
    close() {
        if (this.server) {
            this.server.close();
        }
        return this;
    }

    /**
     * Get server statistics
     */
    getStats() {
        return {
            routes: this.routes.size,
            middleware: this.middleware.length,
            staticPaths: this.staticPaths.size,
            errorHandlers: this.errorHandlers.size,
            port: this.port,
            host: this.host,
            isRunning: this.server !== null
        };
    }
}

// Common middleware
const commonMiddleware = {
    /**
     * Logging middleware
     */
    logger: (req, res, next) => {
        const start = Date.now();
        res.on('finish', () => {
            const duration = Date.now() - start;
            console.log(`${req.method} ${req.url} - ${res.statusCode} - ${duration}ms`);
        });
        next();
    },

    /**
     * CORS middleware
     */
    cors: (req, res, next) => {
        res.setHeader('Access-Control-Allow-Origin', '*');
        res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
        res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');
        
        if (req.method === 'OPTIONS') {
            res.statusCode = 200;
            res.end();
            return;
        }
        next();
    },

    /**
     * Body parsing middleware
     */
    bodyParser: (req, res, next) => {
        // Body parsing is handled in the main request handler
        next();
    },

    /**
     * Security headers middleware
     */
    securityHeaders: (req, res, next) => {
        res.setHeader('X-Content-Type-Options', 'nosniff');
        res.setHeader('X-Frame-Options', 'DENY');
        res.setHeader('X-XSS-Protection', '1; mode=block');
        next();
    }
};

module.exports = { WebFramework, commonMiddleware }; 