package org.tusklang.cli;

import org.apache.commons.cli.*;
import org.tusklang.TuskLangEnhanced;
import org.tusklang.TuskLangParser;
import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.concurrent.TimeUnit;

/**
 * Testing Commands Implementation
 * 
 * Commands:
 * - tsk test [suite]                - Run specific test suite
 * - tsk test all                    - Run all test suites
 * - tsk test parser                 - Test parser functionality only
 * - tsk test fujsen                 - Test FUJSEN operators only
 * - tsk test sdk                    - Test SDK-specific features
 * - tsk test performance            - Run performance benchmarks
 */
public class TestingCommands {
    
    public static boolean handle(String[] args, CommandLine globalCmd) {
        String suite = args.length > 0 ? args[0] : "all";
        
        switch (suite) {
            case "all":
                return runAllTests(globalCmd);
                
            case "parser":
                return runParserTests(globalCmd);
                
            case "fujsen":
                return runFujsenTests(globalCmd);
                
            case "sdk":
                return runSdkTests(globalCmd);
                
            case "performance":
                return runPerformanceTests(globalCmd);
                
            default:
                System.err.println("Unknown test suite: " + suite);
                printHelp();
                return false;
        }
    }
    
    /**
     * Run all test suites
     */
    private static boolean runAllTests(CommandLine globalCmd) {
        if (globalCmd.hasOption("json")) {
            System.out.println("{");
            System.out.println("  \"test_suite\": \"all\",");
            System.out.println("  \"results\": [");
        } else {
            System.out.println("🧪 Running all test suites...");
            System.out.println();
        }
        
        boolean allPassed = true;
        int totalTests = 0;
        int passedTests = 0;
        
        // Run each test suite
        String[] suites = {"parser", "fujsen", "sdk", "performance"};
        
        for (int i = 0; i < suites.length; i++) {
            String suite = suites[i];
            
            if (globalCmd.hasOption("json")) {
                if (i > 0) System.out.println(",");
            } else {
                System.out.println("📋 " + suite.toUpperCase() + " Tests:");
            }
            
            TestResult result = runTestSuite(suite, globalCmd);
            totalTests += result.totalTests;
            passedTests += result.passedTests;
            
            if (!result.success) {
                allPassed = false;
            }
            
            if (globalCmd.hasOption("json")) {
                System.out.println("    {");
                System.out.println("      \"suite\": \"" + suite + "\",");
                System.out.println("      \"success\": " + result.success + ",");
                System.out.println("      \"passed\": " + result.passedTests + ",");
                System.out.println("      \"total\": " + result.totalTests + ",");
                System.out.println("      \"duration_ms\": " + result.durationMs);
                System.out.println("    }");
            } else {
                System.out.println();
            }
        }
        
        if (globalCmd.hasOption("json")) {
            System.out.println("  ],");
            System.out.println("  \"summary\": {");
            System.out.println("    \"total_tests\": " + totalTests + ",");
            System.out.println("    \"passed_tests\": " + passedTests + ",");
            System.out.println("    \"failed_tests\": " + (totalTests - passedTests) + ",");
            System.out.println("    \"success_rate\": " + String.format("%.1f", (double)passedTests/totalTests*100) + "%");
            System.out.println("  }");
            System.out.println("}");
        } else {
            System.out.println("📊 Test Summary:");
            System.out.println("📍 Total tests: " + totalTests);
            System.out.println("📍 Passed: " + passedTests);
            System.out.println("📍 Failed: " + (totalTests - passedTests));
            System.out.println("📍 Success rate: " + String.format("%.1f", (double)passedTests/totalTests*100) + "%");
            System.out.println();
            
            if (allPassed) {
                System.out.println("✅ All tests passed!");
            } else {
                System.out.println("❌ Some tests failed!");
            }
        }
        
        return allPassed;
    }
    
    /**
     * Run parser tests
     */
    private static boolean runParserTests(CommandLine globalCmd) {
        TestResult result = runTestSuite("parser", globalCmd);
        return result.success;
    }
    
    /**
     * Run FUJSEN operator tests
     */
    private static boolean runFujsenTests(CommandLine globalCmd) {
        TestResult result = runTestSuite("fujsen", globalCmd);
        return result.success;
    }
    
    /**
     * Run SDK-specific tests
     */
    private static boolean runSdkTests(CommandLine globalCmd) {
        TestResult result = runTestSuite("sdk", globalCmd);
        return result.success;
    }
    
    /**
     * Run performance tests
     */
    private static boolean runPerformanceTests(CommandLine globalCmd) {
        TestResult result = runTestSuite("performance", globalCmd);
        return result.success;
    }
    
    /**
     * Run a specific test suite
     */
    private static TestResult runTestSuite(String suite, CommandLine globalCmd) {
        long startTime = System.currentTimeMillis();
        int totalTests = 0;
        int passedTests = 0;
        boolean success = true;
        
        switch (suite) {
            case "parser":
                TestResult parserResult = runParserTestSuite(globalCmd);
                totalTests = parserResult.totalTests;
                passedTests = parserResult.passedTests;
                success = parserResult.success;
                break;
                
            case "fujsen":
                TestResult fujsenResult = runFujsenTestSuite(globalCmd);
                totalTests = fujsenResult.totalTests;
                passedTests = fujsenResult.passedTests;
                success = fujsenResult.success;
                break;
                
            case "sdk":
                TestResult sdkResult = runSdkTestSuite(globalCmd);
                totalTests = sdkResult.totalTests;
                passedTests = sdkResult.passedTests;
                success = sdkResult.success;
                break;
                
            case "performance":
                TestResult perfResult = runPerformanceTestSuite(globalCmd);
                totalTests = perfResult.totalTests;
                passedTests = perfResult.passedTests;
                success = perfResult.success;
                break;
        }
        
        long duration = System.currentTimeMillis() - startTime;
        
        if (!globalCmd.hasOption("json")) {
            if (success) {
                System.out.println("✅ " + passedTests + "/" + totalTests + " tests passed (" + duration + "ms)");
            } else {
                System.out.println("❌ " + passedTests + "/" + totalTests + " tests passed (" + duration + "ms)");
            }
        }
        
        return new TestResult(success, totalTests, passedTests, duration);
    }
    
    /**
     * Run parser test suite
     */
    private static TestResult runParserTestSuite(CommandLine globalCmd) {
        int totalTests = 0;
        int passedTests = 0;
        
        try {
            TuskLangParser parser = new TuskLangParser();
            
            // Test 1: Basic parsing
            totalTests++;
            try {
                String config = "app_name: \"Test App\"\nversion: \"1.0.0\"";
                Map<String, Object> result = parser.parse(config);
                if ("Test App".equals(result.get("app_name")) && "1.0.0".equals(result.get("version"))) {
                    passedTests++;
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("Parser test 1 failed: " + e.getMessage());
                }
            }
            
            // Test 2: Nested objects
            totalTests++;
            try {
                String config = """
                    server {
                      host: "localhost"
                      port: 8080
                    }
                    """;
                Map<String, Object> result = parser.parse(config);
                @SuppressWarnings("unchecked")
                Map<String, Object> server = (Map<String, Object>) result.get("server");
                if ("localhost".equals(server.get("host")) && Integer.valueOf(8080).equals(server.get("port"))) {
                    passedTests++;
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("Parser test 2 failed: " + e.getMessage());
                }
            }
            
            // Test 3: Arrays
            totalTests++;
            try {
                String config = """
                    features [
                      "logging"
                      "metrics"
                      "caching"
                    ]
                    """;
                Map<String, Object> result = parser.parse(config);
                @SuppressWarnings("unchecked")
                List<Object> features = (List<Object>) result.get("features");
                if (features.size() == 3 && "logging".equals(features.get(0))) {
                    passedTests++;
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("Parser test 3 failed: " + e.getMessage());
                }
            }
            
            // Test 4: Variable interpolation
            totalTests++;
            try {
                String config = "base_url: \"https://api.example.com\"\nendpoint: \"$base_url/v1/users\"";
                parser.setVariable("base_url", "https://api.example.com");
                Map<String, Object> result = parser.parse(config);
                if ("https://api.example.com/v1/users".equals(result.get("endpoint"))) {
                    passedTests++;
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("Parser test 4 failed: " + e.getMessage());
                }
            }
            
        } catch (Exception e) {
            if (globalCmd.hasOption("verbose")) {
                System.err.println("Parser test suite setup failed: " + e.getMessage());
            }
        }
        
        return new TestResult(passedTests == totalTests, totalTests, passedTests, 0);
    }
    
    /**
     * Run FUJSEN operator test suite
     */
    private static TestResult runFujsenTestSuite(CommandLine globalCmd) {
        int totalTests = 0;
        int passedTests = 0;
        
        try {
            TuskLangEnhanced parser = new TuskLangEnhanced();
            
            // Test 1: @env operator
            totalTests++;
            try {
                System.setProperty("TEST_VAR", "test_value");
                String config = "debug: @env(\"TEST_VAR\", \"default\")";
                Map<String, Object> result = parser.parse(config);
                if ("test_value".equals(result.get("debug"))) {
                    passedTests++;
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("FUJSEN test 1 failed: " + e.getMessage());
                }
            }
            
            // Test 2: @date operator
            totalTests++;
            try {
                String config = "timestamp: @date(\"yyyy-MM-dd\")";
                Map<String, Object> result = parser.parse(config);
                String timestamp = (String) result.get("timestamp");
                if (timestamp != null && timestamp.matches("\\d{4}-\\d{2}-\\d{2}")) {
                    passedTests++;
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("FUJSEN test 2 failed: " + e.getMessage());
                }
            }
            
            // Test 3: Global variables
            totalTests++;
            try {
                parser.setGlobalVariable("env", "production");
                String config = "environment: $env";
                Map<String, Object> result = parser.parse(config);
                if ("production".equals(result.get("environment"))) {
                    passedTests++;
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("FUJSEN test 3 failed: " + e.getMessage());
                }
            }
            
            // Test 4: Range syntax
            totalTests++;
            try {
                String config = "ports: 8000-9000";
                Map<String, Object> result = parser.parse(config);
                @SuppressWarnings("unchecked")
                List<Integer> ports = (List<Integer>) result.get("ports");
                if (ports != null && ports.size() == 1001 && ports.get(0) == 8000 && ports.get(1000) == 9000) {
                    passedTests++;
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("FUJSEN test 4 failed: " + e.getMessage());
                }
            }
            
        } catch (Exception e) {
            if (globalCmd.hasOption("verbose")) {
                System.err.println("FUJSEN test suite setup failed: " + e.getMessage());
            }
        }
        
        return new TestResult(passedTests == totalTests, totalTests, passedTests, 0);
    }
    
    /**
     * Run SDK-specific test suite
     */
    private static TestResult runSdkTestSuite(CommandLine globalCmd) {
        int totalTests = 0;
        int passedTests = 0;
        
        try {
            // Test 1: File parsing
            totalTests++;
            try {
                // Create a temporary test file
                Path testFile = Files.createTempFile("test", ".tsk");
                Files.write(testFile, "app_name: \"SDK Test\"\nversion: \"2.0.0\"".getBytes());
                
                TuskLangParser parser = new TuskLangParser();
                Map<String, Object> result = parser.parseFile(testFile.toString());
                
                if ("SDK Test".equals(result.get("app_name")) && "2.0.0".equals(result.get("version"))) {
                    passedTests++;
                }
                
                Files.delete(testFile);
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("SDK test 1 failed: " + e.getMessage());
                }
            }
            
            // Test 2: JSON conversion
            totalTests++;
            try {
                TuskLangParser parser = new TuskLangParser();
                String config = "name: \"test\"\nvalue: 42";
                Map<String, Object> result = parser.parse(config);
                String json = parser.toJson(result);
                
                if (json.contains("\"name\"") && json.contains("\"test\"") && json.contains("\"value\"") && json.contains("42")) {
                    passedTests++;
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("SDK test 2 failed: " + e.getMessage());
                }
            }
            
            // Test 3: Validation
            totalTests++;
            try {
                TuskLangParser parser = new TuskLangParser();
                String validConfig = "name: \"test\"";
                String invalidConfig = "name:";
                
                if (parser.validate(validConfig) && !parser.validate(invalidConfig)) {
                    passedTests++;
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("SDK test 3 failed: " + e.getMessage());
                }
            }
            
        } catch (Exception e) {
            if (globalCmd.hasOption("verbose")) {
                System.err.println("SDK test suite setup failed: " + e.getMessage());
            }
        }
        
        return new TestResult(passedTests == totalTests, totalTests, passedTests, 0);
    }
    
    /**
     * Run performance test suite
     */
    private static TestResult runPerformanceTestSuite(CommandLine globalCmd) {
        int totalTests = 0;
        int passedTests = 0;
        
        try {
            TuskLangParser parser = new TuskLangParser();
            TuskLangEnhanced enhancedParser = new TuskLangEnhanced();
            
            // Test 1: Basic parsing performance
            totalTests++;
            try {
                String config = generateLargeConfig(1000);
                
                long startTime = System.nanoTime();
                Map<String, Object> result = parser.parse(config);
                long endTime = System.nanoTime();
                
                long durationMs = TimeUnit.NANOSECONDS.toMillis(endTime - startTime);
                
                if (durationMs < 1000) { // Should parse 1000 lines in under 1 second
                    passedTests++;
                }
                
                if (globalCmd.hasOption("verbose")) {
                    System.out.println("Basic parsing: " + durationMs + "ms for 1000 lines");
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("Performance test 1 failed: " + e.getMessage());
                }
            }
            
            // Test 2: Enhanced parsing performance
            totalTests++;
            try {
                String config = generateLargeConfig(500);
                
                long startTime = System.nanoTime();
                Map<String, Object> result = enhancedParser.parse(config);
                long endTime = System.nanoTime();
                
                long durationMs = TimeUnit.NANOSECONDS.toMillis(endTime - startTime);
                
                if (durationMs < 2000) { // Enhanced parser is slower but should still be reasonable
                    passedTests++;
                }
                
                if (globalCmd.hasOption("verbose")) {
                    System.out.println("Enhanced parsing: " + durationMs + "ms for 500 lines");
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("Performance test 2 failed: " + e.getMessage());
                }
            }
            
            // Test 3: Memory usage
            totalTests++;
            try {
                Runtime runtime = Runtime.getRuntime();
                long initialMemory = runtime.totalMemory() - runtime.freeMemory();
                
                String config = generateLargeConfig(100);
                Map<String, Object> result = parser.parse(config);
                
                long finalMemory = runtime.totalMemory() - runtime.freeMemory();
                long memoryUsed = finalMemory - initialMemory;
                
                if (memoryUsed < 10 * 1024 * 1024) { // Should use less than 10MB
                    passedTests++;
                }
                
                if (globalCmd.hasOption("verbose")) {
                    System.out.println("Memory usage: " + (memoryUsed / 1024) + "KB");
                }
            } catch (Exception e) {
                if (globalCmd.hasOption("verbose")) {
                    System.err.println("Performance test 3 failed: " + e.getMessage());
                }
            }
            
        } catch (Exception e) {
            if (globalCmd.hasOption("verbose")) {
                System.err.println("Performance test suite setup failed: " + e.getMessage());
            }
        }
        
        return new TestResult(passedTests == totalTests, totalTests, passedTests, 0);
    }
    
    /**
     * Generate a large configuration for performance testing
     */
    private static String generateLargeConfig(int lines) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < lines; i++) {
            sb.append("key_").append(i).append(": \"value_").append(i).append("\"\n");
        }
        return sb.toString();
    }
    
    private static void printHelp() {
        System.out.println("Testing Commands:");
        System.out.println("  tsk test [suite]                - Run specific test suite");
        System.out.println("  tsk test all                    - Run all test suites");
        System.out.println("  tsk test parser                 - Test parser functionality only");
        System.out.println("  tsk test fujsen                 - Test FUJSEN operators only");
        System.out.println("  tsk test sdk                    - Test SDK-specific features");
        System.out.println("  tsk test performance            - Run performance benchmarks");
    }
    
    /**
     * Test result holder
     */
    private static class TestResult {
        final boolean success;
        final int totalTests;
        final int passedTests;
        final long durationMs;
        
        TestResult(boolean success, int totalTests, int passedTests, long durationMs) {
            this.success = success;
            this.totalTests = totalTests;
            this.passedTests = passedTests;
            this.durationMs = durationMs;
        }
    }
} 