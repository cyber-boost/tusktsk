package org.tusklang.cli;

import org.apache.commons.cli.*;
import org.tusklang.TuskLangEnhanced;
import org.tusklang.TuskLangParser;
import java.io.*;
import java.nio.file.*;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.concurrent.Executors;
import java.util.concurrent.ExecutorService;
import java.util.Map;

/**
 * Development Commands Implementation
 * 
 * Commands:
 * - tsk serve [port]                - Start development server
 * - tsk compile <file>              - Compile .tsk file
 * - tsk optimize <file>             - Optimize .tsk file
 */
public class DevelopmentCommands {
    
    /**
     * tsk serve [port] - Start development server
     */
    public static boolean handleServe(String[] args, CommandLine globalCmd) {
        int port = 8080; // Default port
        
        if (args.length > 0) {
            try {
                port = Integer.parseInt(args[0]);
            } catch (NumberFormatException e) {
                System.err.println("Error: Invalid port number: " + args[0]);
                return false;
            }
        }
        
        try {
            ServerSocket serverSocket = new ServerSocket(port);
            
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"started\",");
                System.out.println("  \"port\": " + port + ",");
                System.out.println("  \"url\": \"http://localhost:" + port + "\"");
                System.out.println("}");
            } else {
                System.out.println("🚀 TuskLang Development Server started");
                System.out.println("📍 Port: " + port);
                System.out.println("📍 URL: http://localhost:" + port);
                System.out.println("📍 Press Ctrl+C to stop");
            }
            
            ExecutorService executor = Executors.newCachedThreadPool();
            
            while (true) {
                try {
                    Socket clientSocket = serverSocket.accept();
                    executor.submit(() -> handleClient(clientSocket, globalCmd));
                } catch (IOException e) {
                    if (!serverSocket.isClosed()) {
                        System.err.println("Error accepting connection: " + e.getMessage());
                    }
                    break;
                }
            }
            
            executor.shutdown();
            serverSocket.close();
            return true;
            
        } catch (IOException e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Failed to start server: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk compile <file> - Compile .tsk file
     */
    public static boolean handleCompile(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            System.err.println("Error: compile command requires a file path");
            return false;
        }
        
        String inputFile = args[0];
        
        try {
            if (!Files.exists(Paths.get(inputFile))) {
                System.err.println("Error: File not found: " + inputFile);
                return false;
            }
            
            // Parse the TSK file
            TuskLangEnhanced parser = new TuskLangEnhanced();
            Map<String, Object> config = parser.parseFile(inputFile);
            
            // Generate output filename
            String outputFile = inputFile;
            if (inputFile.endsWith(".tsk")) {
                outputFile = inputFile.substring(0, inputFile.length() - 4) + ".compiled.tsk";
            } else {
                outputFile = inputFile + ".compiled";
            }
            
            // Write compiled output
            String compiledContent = generateCompiledContent(config, inputFile);
            Files.write(Paths.get(outputFile), compiledContent.getBytes());
            
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"success\",");
                System.out.println("  \"input_file\": \"" + inputFile + "\",");
                System.out.println("  \"output_file\": \"" + outputFile + "\",");
                System.out.println("  \"size_bytes\": " + Files.size(Paths.get(outputFile)));
                System.out.println("}");
            } else {
                System.out.println("✅ File compiled successfully");
                System.out.println("📍 Input: " + inputFile);
                System.out.println("📍 Output: " + outputFile);
                System.out.println("📍 Size: " + Files.size(Paths.get(outputFile)) + " bytes");
            }
            
            return true;
            
        } catch (Exception e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Compilation failed: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk optimize <file> - Optimize .tsk file
     */
    public static boolean handleOptimize(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            System.err.println("Error: optimize command requires a file path");
            return false;
        }
        
        String inputFile = args[0];
        
        try {
            if (!Files.exists(Paths.get(inputFile))) {
                System.err.println("Error: File not found: " + inputFile);
                return false;
            }
            
            // Parse the TSK file
            TuskLangEnhanced parser = new TuskLangEnhanced();
            Map<String, Object> config = parser.parseFile(inputFile);
            
            // Apply optimizations
            Map<String, Object> optimized = optimizeConfiguration(config);
            
            // Generate output filename
            String outputFile = inputFile;
            if (inputFile.endsWith(".tsk")) {
                outputFile = inputFile.substring(0, inputFile.length() - 4) + ".optimized.tsk";
            } else {
                outputFile = inputFile + ".optimized";
            }
            
            // Write optimized output
            String optimizedContent = generateOptimizedContent(optimized, inputFile);
            Files.write(Paths.get(outputFile), optimizedContent.getBytes());
            
            long originalSize = Files.size(Paths.get(inputFile));
            long optimizedSize = Files.size(Paths.get(outputFile));
            double improvement = ((double)(originalSize - optimizedSize) / originalSize) * 100;
            
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"success\",");
                System.out.println("  \"input_file\": \"" + inputFile + "\",");
                System.out.println("  \"output_file\": \"" + outputFile + "\",");
                System.out.println("  \"original_size\": " + originalSize + ",");
                System.out.println("  \"optimized_size\": " + optimizedSize + ",");
                System.out.println("  \"improvement_percent\": " + String.format("%.1f", improvement));
                System.out.println("}");
            } else {
                System.out.println("✅ File optimized successfully");
                System.out.println("📍 Input: " + inputFile);
                System.out.println("📍 Output: " + outputFile);
                System.out.println("📍 Original size: " + originalSize + " bytes");
                System.out.println("📍 Optimized size: " + optimizedSize + " bytes");
                System.out.println("📍 Improvement: " + String.format("%.1f", improvement) + "%");
            }
            
            return true;
            
        } catch (Exception e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Optimization failed: " + e.getMessage());
            }
            return false;
        }
    }
    
    // Helper methods
    
    private static void handleClient(Socket clientSocket, CommandLine globalCmd) {
        try (BufferedReader in = new BufferedReader(new InputStreamReader(clientSocket.getInputStream()));
             PrintWriter out = new PrintWriter(clientSocket.getOutputStream(), true)) {
            
            // Read HTTP request
            String requestLine = in.readLine();
            if (requestLine == null) return;
            
            String[] parts = requestLine.split(" ");
            if (parts.length < 2) return;
            
            String method = parts[0];
            String path = parts[1];
            
            // Handle different paths
            if ("GET".equals(method)) {
                if ("/".equals(path)) {
                    sendResponse(out, "200 OK", "text/html", generateIndexHtml());
                } else if ("/health".equals(path)) {
                    sendResponse(out, "200 OK", "application/json", "{\"status\":\"healthy\"}");
                } else if (path.startsWith("/config/")) {
                    handleConfigRequest(out, path.substring(8), globalCmd);
                } else {
                    sendResponse(out, "404 Not Found", "text/plain", "Not Found");
                }
            } else {
                sendResponse(out, "405 Method Not Allowed", "text/plain", "Method Not Allowed");
            }
            
        } catch (IOException e) {
            // Ignore client errors
        } finally {
            try {
                clientSocket.close();
            } catch (IOException e) {
                // Ignore
            }
        }
    }
    
    private static void sendResponse(PrintWriter out, String status, String contentType, String body) {
        out.println("HTTP/1.1 " + status);
        out.println("Content-Type: " + contentType + "; charset=UTF-8");
        out.println("Content-Length: " + body.getBytes().length);
        out.println("Access-Control-Allow-Origin: *");
        out.println();
        out.println(body);
    }
    
    private static String generateIndexHtml() {
        return """
            <!DOCTYPE html>
            <html>
            <head>
                <title>TuskLang Development Server</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 40px; }
                    .endpoint { background: #f5f5f5; padding: 10px; margin: 10px 0; border-radius: 5px; }
                    .method { color: #0066cc; font-weight: bold; }
                </style>
            </head>
            <body>
                <h1>🐘 TuskLang Development Server</h1>
                <p>Welcome to the TuskLang development server!</p>
                
                <h2>Available Endpoints:</h2>
                <div class="endpoint">
                    <span class="method">GET</span> / - This page
                </div>
                <div class="endpoint">
                    <span class="method">GET</span> /health - Health check
                </div>
                <div class="endpoint">
                    <span class="method">GET</span> /config/{file} - Get configuration
                </div>
                
                <h2>Examples:</h2>
                <ul>
                    <li><a href="/health">/health</a> - Check server health</li>
                    <li><a href="/config/peanut.tsk">/config/peanut.tsk</a> - Get peanut configuration</li>
                </ul>
            </body>
            </html>
            """;
    }
    
    private static void handleConfigRequest(PrintWriter out, String filename, CommandLine globalCmd) {
        try {
            if (!Files.exists(Paths.get(filename))) {
                sendResponse(out, "404 Not Found", "application/json", 
                    "{\"error\":\"Configuration file not found: " + filename + "\"}");
                return;
            }
            
            TuskLangEnhanced parser = new TuskLangEnhanced();
            Map<String, Object> config = parser.parseFile(filename);
            
            // Convert to JSON
            com.fasterxml.jackson.databind.ObjectMapper mapper = new com.fasterxml.jackson.databind.ObjectMapper();
            String json = mapper.writeValueAsString(config);
            
            sendResponse(out, "200 OK", "application/json", json);
            
        } catch (Exception e) {
            sendResponse(out, "500 Internal Server Error", "application/json", 
                "{\"error\":\"" + e.getMessage() + "\"}");
        }
    }
    
    private static String generateCompiledContent(Map<String, Object> config, String originalFile) {
        StringBuilder sb = new StringBuilder();
        sb.append("# Compiled TuskLang Configuration\n");
        sb.append("# Original file: ").append(originalFile).append("\n");
        sb.append("# Compiled at: ").append(java.time.LocalDateTime.now()).append("\n");
        sb.append("# This file was automatically generated - do not edit manually\n\n");
        
        // Convert config back to TSK format
        sb.append(convertToTskFormat(config, 0));
        
        return sb.toString();
    }
    
    private static String generateOptimizedContent(Map<String, Object> config, String originalFile) {
        StringBuilder sb = new StringBuilder();
        sb.append("# Optimized TuskLang Configuration\n");
        sb.append("# Original file: ").append(originalFile).append("\n");
        sb.append("# Optimized at: ").append(java.time.LocalDateTime.now()).append("\n");
        sb.append("# This file was automatically optimized - do not edit manually\n\n");
        
        // Convert optimized config back to TSK format
        sb.append(convertToTskFormat(config, 0));
        
        return sb.toString();
    }
    
    private static Map<String, Object> optimizeConfiguration(Map<String, Object> config) {
        // Apply various optimizations
        Map<String, Object> optimized = new java.util.LinkedHashMap<>();
        
        for (Map.Entry<String, Object> entry : config.entrySet()) {
            String key = entry.getKey();
            Object value = entry.getValue();
            
            // Remove comments and empty values
            if (key.startsWith("#") || value == null) {
                continue;
            }
            
            // Optimize string values (remove unnecessary quotes)
            if (value instanceof String) {
                String str = (String) value;
                if (str.matches("^[a-zA-Z0-9_-]+$")) {
                    // Don't quote simple identifiers
                    optimized.put(key, str);
                } else {
                    optimized.put(key, value);
                }
            } else {
                optimized.put(key, value);
            }
        }
        
        return optimized;
    }
    
    private static String convertToTskFormat(Object value, int indent) {
        StringBuilder sb = new StringBuilder();
        String indentStr = "  ".repeat(indent);
        
        if (value instanceof Map) {
            @SuppressWarnings("unchecked")
            Map<String, Object> map = (Map<String, Object>) value;
            
            for (Map.Entry<String, Object> entry : map.entrySet()) {
                String key = entry.getKey();
                Object val = entry.getValue();
                
                if (val instanceof Map || val instanceof java.util.List) {
                    sb.append(indentStr).append(key).append(":\n");
                    sb.append(convertToTskFormat(val, indent + 1));
                } else {
                    sb.append(indentStr).append(key).append(": ");
                    if (val instanceof String) {
                        sb.append("\"").append(val).append("\"");
                    } else {
                        sb.append(val);
                    }
                    sb.append("\n");
                }
            }
        } else if (value instanceof java.util.List) {
            @SuppressWarnings("unchecked")
            java.util.List<Object> list = (java.util.List<Object>) value;
            
            for (Object item : list) {
                sb.append(indentStr).append("- ");
                if (item instanceof String) {
                    sb.append("\"").append(item).append("\"");
                } else {
                    sb.append(item);
                }
                sb.append("\n");
            }
        }
        
        return sb.toString();
    }
} 