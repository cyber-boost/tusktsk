/**
 * TuskLang Error Handling Middleware
 * ==================================
 * Comprehensive error handling and logging
 */

/**
 * Global error handler middleware
 */
function errorHandler(err, req, res, next) {
    // Log the error
    console.error('Error occurred:', {
        error: err.message,
        stack: err.stack,
        url: req.url,
        method: req.method,
        ip: req.ip,
        userAgent: req.get('User-Agent'),
        timestamp: new Date().toISOString()
    });

    // Determine error type and status code
    let statusCode = 500;
    let errorMessage = 'Internal Server Error';
    let errorDetails = null;

    if (err.name === 'ValidationError') {
        statusCode = 400;
        errorMessage = 'Validation Error';
        errorDetails = err.details || err.message;
    } else if (err.name === 'UnauthorizedError') {
        statusCode = 401;
        errorMessage = 'Unauthorized';
    } else if (err.name === 'ForbiddenError') {
        statusCode = 403;
        errorMessage = 'Forbidden';
    } else if (err.name === 'NotFoundError') {
        statusCode = 404;
        errorMessage = 'Not Found';
    } else if (err.name === 'ConflictError') {
        statusCode = 409;
        errorMessage = 'Conflict';
    } else if (err.name === 'RateLimitError') {
        statusCode = 429;
        errorMessage = 'Too Many Requests';
    } else if (err.code === 'ECONNREFUSED') {
        statusCode = 503;
        errorMessage = 'Service Unavailable';
    } else if (err.code === 'ENOTFOUND') {
        statusCode = 404;
        errorMessage = 'Resource Not Found';
    }

    // Create error response
    const errorResponse = {
        error: errorMessage,
        message: err.message || errorMessage,
        timestamp: new Date().toISOString(),
        path: req.path,
        method: req.method
    };

    // Add error details if available
    if (errorDetails) {
        errorResponse.details = errorDetails;
    }

    // Add stack trace in development
    if (process.env.NODE_ENV === 'development') {
        errorResponse.stack = err.stack;
    }

    // Add request ID if available
    if (req.id) {
        errorResponse.requestId = req.id;
    }

    // Send error response
    res.status(statusCode).json(errorResponse);
}

/**
 * Async error wrapper for route handlers
 */
function asyncHandler(fn) {
    return (req, res, next) => {
        Promise.resolve(fn(req, res, next)).catch(next);
    };
}

/**
 * Custom error classes
 */
class ValidationError extends Error {
    constructor(message, details = null) {
        super(message);
        this.name = 'ValidationError';
        this.details = details;
    }
}

class UnauthorizedError extends Error {
    constructor(message = 'Unauthorized') {
        super(message);
        this.name = 'UnauthorizedError';
    }
}

class ForbiddenError extends Error {
    constructor(message = 'Forbidden') {
        super(message);
        this.name = 'ForbiddenError';
    }
}

class NotFoundError extends Error {
    constructor(message = 'Not Found') {
        super(message);
        this.name = 'NotFoundError';
    }
}

class ConflictError extends Error {
    constructor(message = 'Conflict') {
        super(message);
        this.name = 'ConflictError';
    }
}

class RateLimitError extends Error {
    constructor(message = 'Too Many Requests') {
        super(message);
        this.name = 'RateLimitError';
    }
}

/**
 * Error response helper
 */
function sendError(res, statusCode, message, details = null) {
    const errorResponse = {
        error: message,
        timestamp: new Date().toISOString()
    };

    if (details) {
        errorResponse.details = details;
    }

    res.status(statusCode).json(errorResponse);
}

/**
 * Success response helper
 */
function sendSuccess(res, data, message = 'Success') {
    res.json({
        success: true,
        message,
        data,
        timestamp: new Date().toISOString()
    });
}

/**
 * Not found handler
 */
function notFoundHandler(req, res) {
    sendError(res, 404, 'Route not found', {
        path: req.path,
        method: req.method
    });
}

/**
 * Method not allowed handler
 */
function methodNotAllowedHandler(req, res) {
    sendError(res, 405, 'Method not allowed', {
        path: req.path,
        method: req.method,
        allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
    });
}

module.exports = {
    errorHandler,
    asyncHandler,
    ValidationError,
    UnauthorizedError,
    ForbiddenError,
    NotFoundError,
    ConflictError,
    RateLimitError,
    sendError,
    sendSuccess,
    notFoundHandler,
    methodNotAllowedHandler
}; 