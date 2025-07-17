package org.tusklang;

import java.util.Map;

/**
 * Simple example showing how to use TuskLang in Java
 */
public class Example {
    
    public static void main(String[] args) {
        // Create a simple TuskLang configuration
        String config = """
            # Simple app configuration
            app_name: "My Java App"
            version: "1.0.0"
            debug: true
            port: 8080
            
            # Database settings
            database:
              host: "localhost"
              port: 5432
              name: "myapp"
            
            # Features
            features:
              - logging
              - metrics
              - caching
            """;
        
        try {
            // Create parser
            TuskLangParser parser = new TuskLangParser();
            
            // Set some variables for interpolation
            parser.setVariable("base_url", "https://api.example.com");
            
            // Parse the configuration
            Map<String, Object> result = parser.parse(config);
            
            // Use the configuration
            System.out.println("🚀 Starting " + result.get("app_name") + " v" + result.get("version"));
            System.out.println("📍 Debug mode: " + result.get("debug"));
            System.out.println("🔌 Port: " + result.get("port"));
            
            // Convert to JSON
            String json = parser.toPrettyJson(result);
            System.out.println("\n📄 JSON Output:");
            System.out.println(json);
            
            // Validate some input
            String validInput = "name: \"test\"\nvalue: 42";
            String invalidInput = "name: \"test\"\nvalue:";
            
            System.out.println("\n✅ Valid input: " + parser.validate(validInput));
            System.out.println("❌ Invalid input: " + parser.validate(invalidInput));
            
        } catch (TuskLangException e) {
            System.err.println("Error: " + e.getMessage());
        }
    }
} 