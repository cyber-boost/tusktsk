package org.tusklang;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.node.ObjectNode;
import com.fasterxml.jackson.databind.node.ArrayNode;

import java.io.*;
import java.util.*;
import java.util.regex.Pattern;

/**
 * Full-featured TuskLang parser for Java applications
 * 
 * Supports:
 * - Nested objects and arrays
 * - Variable interpolation ($var and {{var}})
 * - Comments (#)
 * - All data types (strings, numbers, booleans, null)
 * - Proper indentation handling
 * 
 * Usage:
 * TuskLangParser parser = new TuskLangParser();
 * Map<String, Object> config = parser.parse("app_name: \"My App\"");
 */
public class TuskLangParser {
    private final ObjectMapper mapper;
    private final Map<String, String> variables;
    
    public TuskLangParser() {
        this.mapper = new ObjectMapper();
        this.variables = new HashMap<>();
    }
    
    /**
     * Parse TuskLang string into a Map
     */
    public Map<String, Object> parse(String input) throws TuskLangException {
        try {
            String[] lines = input.split("\n");
            List<Line> parsedLines = new ArrayList<>();
            
            // First pass: parse all lines and calculate indentation
            for (int i = 0; i < lines.length; i++) {
                String line = lines[i];
                Line parsedLine = parseLine(line, i + 1);
                if (parsedLine != null) {
                    parsedLines.add(parsedLine);
                }
            }
            
            // Second pass: build the nested structure
            Map<String, Object> result = buildNestedStructure(parsedLines);
            
            // Process variable interpolation
            interpolateVariables(result);
            
            return result;
        } catch (Exception e) {
            throw new TuskLangException("Failed to parse TuskLang", e);
        }
    }
    
    /**
     * Parse a single line and return Line object
     */
    private Line parseLine(String line, int lineNumber) throws TuskLangException {
        String trimmed = line.trim();
        
        // Skip empty lines and comments
        if (trimmed.isEmpty() || trimmed.startsWith("#")) {
            return null;
        }
        
        // Calculate indentation level
        int indentLevel = calculateIndentation(line);
        
        // Check if it's an array item
        if (trimmed.startsWith("- ")) {
            String value = trimmed.substring(2).trim();
            return new Line(lineNumber, indentLevel, null, parseValue(value), true);
        }
        
        // Parse key-value pair
        int colonIndex = trimmed.indexOf(':');
        if (colonIndex == -1) {
            throw new TuskLangException("Invalid line format at line " + lineNumber + ": missing colon");
        }
        
        String key = trimmed.substring(0, colonIndex).trim();
        String valueStr = trimmed.substring(colonIndex + 1).trim();
        
        Object value = null;
        if (!valueStr.isEmpty()) {
            value = parseValue(valueStr);
        }
        
        return new Line(lineNumber, indentLevel, key, value, false);
    }
    
    /**
     * Calculate indentation level (spaces / 2)
     */
    private int calculateIndentation(String line) {
        int spaces = 0;
        for (char c : line.toCharArray()) {
            if (c == ' ') {
                spaces++;
            } else if (c == '\t') {
                spaces += 2; // Treat tab as 2 spaces
            } else {
                break;
            }
        }
        return spaces / 2;
    }
    
    /**
     * Build nested structure from parsed lines
     */
    private Map<String, Object> buildNestedStructure(List<Line> lines) throws TuskLangException {
        Map<String, Object> result = new LinkedHashMap<>();
        Stack<Context> contextStack = new Stack<>();
        contextStack.push(new Context(result, 0));
        
        for (Line line : lines) {
            // Pop contexts until we find the right level
            while (contextStack.size() > 1 && contextStack.peek().indentLevel >= line.indentLevel) {
                contextStack.pop();
            }
            
            Context currentContext = contextStack.peek();
            
            if (line.isArrayItem) {
                // Handle array item
                if (currentContext.currentArray == null) {
                    throw new TuskLangException("Array item found without array context at line " + line.lineNumber);
                }
                currentContext.currentArray.add(line.value);
            } else {
                // Handle key-value pair
                if (line.value == null) {
                    // This is a parent object, create new context
                    Map<String, Object> newObject = new LinkedHashMap<>();
                    currentContext.map.put(line.key, newObject);
                    contextStack.push(new Context(newObject, line.indentLevel));
                } else if (line.value instanceof String && "[]".equals(line.value)) {
                    // This is an array, create array context
                    List<Object> newArray = new ArrayList<>();
                    currentContext.map.put(line.key, newArray);
                    contextStack.push(new Context(newArray, line.indentLevel));
                } else {
                    // Regular value
                    currentContext.map.put(line.key, line.value);
                }
            }
        }
        
        return result;
    }
    
    /**
     * Parse a value string
     */
    private Object parseValue(String valueStr) {
        // Remove quotes if present
        if (valueStr.startsWith("\"") && valueStr.endsWith("\"")) {
            return valueStr.substring(1, valueStr.length() - 1);
        }
        
        if (valueStr.startsWith("'") && valueStr.endsWith("'")) {
            return valueStr.substring(1, valueStr.length() - 1);
        }
        
        // Try to parse as number
        try {
            if (valueStr.contains(".")) {
                return Double.parseDouble(valueStr);
            } else {
                return Integer.parseInt(valueStr);
            }
        } catch (NumberFormatException e) {
            // Not a number, continue
        }
        
        // Try to parse as boolean
        if ("true".equalsIgnoreCase(valueStr)) {
            return true;
        }
        if ("false".equalsIgnoreCase(valueStr)) {
            return false;
        }
        
        // Check for null
        if ("null".equalsIgnoreCase(valueStr)) {
            return null;
        }
        
        // Return as string
        return valueStr;
    }
    
    /**
     * Interpolate variables in the configuration
     */
    private void interpolateVariables(Map<String, Object> config) {
        for (Map.Entry<String, Object> entry : config.entrySet()) {
            if (entry.getValue() instanceof String) {
                String value = (String) entry.getValue();
                String interpolated = interpolateString(value);
                config.put(entry.getKey(), interpolated);
            } else if (entry.getValue() instanceof Map) {
                @SuppressWarnings("unchecked")
                Map<String, Object> nestedMap = (Map<String, Object>) entry.getValue();
                interpolateVariables(nestedMap);
            } else if (entry.getValue() instanceof List) {
                @SuppressWarnings("unchecked")
                List<Object> list = (List<Object>) entry.getValue();
                for (int i = 0; i < list.size(); i++) {
                    if (list.get(i) instanceof String) {
                        String value = (String) list.get(i);
                        String interpolated = interpolateString(value);
                        list.set(i, interpolated);
                    }
                }
            }
        }
    }
    
    /**
     * Interpolate variables in a string (supports both $var and {{var}})
     */
    private String interpolateString(String input) {
        String result = input;
        
        // Handle $variable syntax
        Pattern dollarPattern = Pattern.compile("\\$(\\w+)");
        java.util.regex.Matcher dollarMatcher = dollarPattern.matcher(input);
        while (dollarMatcher.find()) {
            String varName = dollarMatcher.group(1);
            String replacement = variables.get(varName);
            if (replacement != null) {
                result = result.replace("$" + varName, replacement);
            }
        }
        
        // Handle {{variable}} syntax
        Pattern bracePattern = Pattern.compile("\\{\\{(\\w+)\\}\\}");
        java.util.regex.Matcher braceMatcher = bracePattern.matcher(result);
        while (braceMatcher.find()) {
            String varName = braceMatcher.group(1);
            String replacement = variables.get(varName);
            if (replacement != null) {
                result = result.replace("{{" + varName + "}}", replacement);
            }
        }
        
        return result;
    }
    
    /**
     * Set a variable for interpolation
     */
    public void setVariable(String name, String value) {
        variables.put(name, value);
    }
    
    /**
     * Parse TuskLang file
     */
    public Map<String, Object> parseFile(String filename) throws TuskLangException {
        try {
            String content = new String(java.nio.file.Files.readAllBytes(
                java.nio.file.Paths.get(filename)));
            return parse(content);
        } catch (IOException e) {
            throw new TuskLangException("Failed to read file: " + filename, e);
        }
    }
    
    /**
     * Convert to JSON string
     */
    public String toJson(Map<String, Object> config) throws TuskLangException {
        try {
            return mapper.writeValueAsString(config);
        } catch (Exception e) {
            throw new TuskLangException("Failed to convert to JSON", e);
        }
    }
    
    /**
     * Convert to pretty JSON string
     */
    public String toPrettyJson(Map<String, Object> config) throws TuskLangException {
        try {
            return mapper.writerWithDefaultPrettyPrinter().writeValueAsString(config);
        } catch (Exception e) {
            throw new TuskLangException("Failed to convert to JSON", e);
        }
    }
    
    /**
     * Validate TuskLang syntax
     */
    public boolean validate(String input) {
        try {
            parse(input);
            return true;
        } catch (Exception e) {
            return false;
        }
    }
    
    /**
     * Helper class to represent a parsed line
     */
    private static class Line {
        final int lineNumber;
        final int indentLevel;
        final String key;
        final Object value;
        final boolean isArrayItem;
        
        Line(int lineNumber, int indentLevel, String key, Object value, boolean isArrayItem) {
            this.lineNumber = lineNumber;
            this.indentLevel = indentLevel;
            this.key = key;
            this.value = value;
            this.isArrayItem = isArrayItem;
        }
    }
    
    /**
     * Helper class to track parsing context
     */
    private static class Context {
        final Map<String, Object> map;
        final List<Object> currentArray;
        final int indentLevel;
        
        Context(Map<String, Object> map, int indentLevel) {
            this.map = map;
            this.currentArray = null;
            this.indentLevel = indentLevel;
        }
        
        Context(List<Object> array, int indentLevel) {
            this.map = null;
            this.currentArray = array;
            this.indentLevel = indentLevel;
        }
    }
} 