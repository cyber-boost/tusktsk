/**
 * Development Commands for TuskLang CLI
 * =====================================
 * Implements development server, compilation, and optimization commands
 */

const http = require('http');
const fs = require('fs').promises;
const path = require('path');
const TuskLang = require('../../index.js');

// Development server command
async function serve(port = 8080) {
  try {
    console.log(`🔄 Starting development server on port ${port}...`);
    
    const server = http.createServer(async (req, res) => {
      try {
        // Set CORS headers
        res.setHeader('Access-Control-Allow-Origin', '*');
        res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
        res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');
        
        if (req.method === 'OPTIONS') {
          res.writeHead(200);
          res.end();
          return;
        }
        
        // Parse URL
        const url = new URL(req.url, `http://localhost:${port}`);
        const pathname = url.pathname;
        
        // Route handling
        if (pathname === '/') {
          res.writeHead(200, { 'Content-Type': 'text/html' });
          res.end(`
            <!DOCTYPE html>
            <html>
            <head>
              <title>TuskLang Development Server</title>
              <style>
                body { font-family: Arial, sans-serif; margin: 40px; }
                .container { max-width: 800px; margin: 0 auto; }
                .status { padding: 10px; background: #e8f5e8; border-radius: 5px; }
                .endpoints { margin-top: 20px; }
                .endpoint { margin: 10px 0; padding: 10px; background: #f5f5f5; border-radius: 3px; }
              </style>
            </head>
            <body>
              <div class="container">
                <h1>🐘 TuskLang Development Server</h1>
                <div class="status">
                  ✅ Server running on port ${port}
                </div>
                <div class="endpoints">
                  <h3>Available Endpoints:</h3>
                  <div class="endpoint">
                    <strong>GET /api/status</strong> - Server status
                  </div>
                  <div class="endpoint">
                    <strong>POST /api/parse</strong> - Parse TSK content
                  </div>
                  <div class="endpoint">
                    <strong>GET /api/config</strong> - Get current configuration
                  </div>
                </div>
              </div>
            </body>
            </html>
          `);
        } else if (pathname === '/api/status') {
          res.writeHead(200, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({
            status: 'running',
            port: port,
            uptime: process.uptime(),
            timestamp: new Date().toISOString()
          }));
        } else if (pathname === '/api/parse' && req.method === 'POST') {
          let body = '';
          req.on('data', chunk => body += chunk);
          req.on('end', () => {
            try {
              const { content } = JSON.parse(body);
              const tusk = new TuskLang();
              const result = tusk.parse(content);
              
              res.writeHead(200, { 'Content-Type': 'application/json' });
              res.end(JSON.stringify({ success: true, result }));
            } catch (error) {
              res.writeHead(400, { 'Content-Type': 'application/json' });
              res.end(JSON.stringify({ success: false, error: error.message }));
            }
          });
        } else if (pathname === '/api/config') {
          try {
            const peanutConfig = new (require('../../peanut-config.js'))();
            const config = peanutConfig.load(process.cwd());
            
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ success: true, config }));
          } catch (error) {
            res.writeHead(500, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ success: false, error: error.message }));
          }
        } else {
          res.writeHead(404, { 'Content-Type': 'text/plain' });
          res.end('Not Found');
        }
      } catch (error) {
        res.writeHead(500, { 'Content-Type': 'text/plain' });
        res.end('Internal Server Error');
      }
    });
    
    server.listen(port, () => {
      console.log(`✅ Development server started on http://localhost:${port}`);
      console.log('📍 Press Ctrl+C to stop the server');
    });
    
    // Handle graceful shutdown
    process.on('SIGINT', () => {
      console.log('\n🔄 Shutting down development server...');
      server.close(() => {
        console.log('✅ Server stopped');
        process.exit(0);
      });
    });
    
    return { success: true, port, url: `http://localhost:${port}` };
  } catch (error) {
    console.error('❌ Failed to start development server:', error.message);
    return { success: false, error: error.message };
  }
}

// Compile TSK file command
async function compile(file) {
  try {
    console.log(`🔄 Compiling TSK file: ${file}`);
    
    // Check if file exists
    await fs.access(file);
    
    // Read and parse TSK file
    const content = await fs.readFile(file, 'utf8');
    const tusk = new TuskLang();
    const parsed = tusk.parse(content);
    
    // Generate optimized output
    const optimized = TuskLang.stringify(parsed, {
      indent: '  ',
      style: 'mixed'
    });
    
    // Write compiled output
    const outputFile = file.replace(/\.tsk$/, '.compiled.tsk');
    await fs.writeFile(outputFile, optimized);
    
    console.log('✅ TSK file compiled successfully');
    console.log(`📍 Output file: ${outputFile}`);
    
    return { success: true, input: file, output: outputFile };
  } catch (error) {
    console.error('❌ Compilation failed:', error.message);
    return { success: false, file, error: error.message };
  }
}

// Optimize TSK file command
async function optimize(file) {
  try {
    console.log(`🔄 Optimizing TSK file: ${file}`);
    
    // Check if file exists
    await fs.access(file);
    
    // Read and parse TSK file
    const content = await fs.readFile(file, 'utf8');
    const tusk = new TuskLang();
    const parsed = tusk.parse(content);
    
    // Apply optimizations
    const optimized = optimizeConfiguration(parsed);
    
    // Generate optimized output
    const output = TuskLang.stringify(optimized, {
      indent: '  ',
      style: 'flat' // Use flat style for production
    });
    
    // Write optimized output
    const outputFile = file.replace(/\.tsk$/, '.optimized.tsk');
    await fs.writeFile(outputFile, output);
    
    // Also create binary version
    const peanutConfig = new (require('../../peanut-config.js'))();
    const binaryFile = outputFile.replace(/\.tsk$/, '.pnt');
    peanutConfig.compileToBinary(optimized, binaryFile);
    
    console.log('✅ TSK file optimized successfully');
    console.log(`📍 Optimized file: ${outputFile}`);
    console.log(`📍 Binary file: ${binaryFile}`);
    
    return { 
      success: true, 
      input: file, 
      optimized: outputFile, 
      binary: binaryFile 
    };
  } catch (error) {
    console.error('❌ Optimization failed:', error.message);
    return { success: false, file, error: error.message };
  }
}

// Helper function to optimize configuration
function optimizeConfiguration(config) {
  const optimized = {};
  
  for (const [key, value] of Object.entries(config)) {
    if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      // Recursively optimize nested objects
      optimized[key] = optimizeConfiguration(value);
    } else if (Array.isArray(value)) {
      // Optimize arrays by removing duplicates and null values
      optimized[key] = value.filter(item => item !== null && item !== undefined);
    } else {
      // Keep primitive values as-is
      optimized[key] = value;
    }
  }
  
  return optimized;
}

module.exports = {
  serve,
  compile,
  optimize
}; 