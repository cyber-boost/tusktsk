package org.tusklang;

import jakarta.persistence.EntityManager;
import jakarta.persistence.EntityManagerFactory;
import jakarta.persistence.Persistence;
import jakarta.persistence.Query;
import java.io.*;
import java.nio.file.*;
import java.text.SimpleDateFormat;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.time.Instant;
import java.util.Arrays;

/**
 * Enhanced TuskLang parser with flexible syntax and @ operators
 * 
 * Features:
 * - Multiple syntax styles: [], {}, <>
 * - Global variables with $ prefix
 * - All @ operators: @env, @cache, @metrics, @learn, @optimize, @date, @query
 * - peanut.tsk automatic loading
 * - Cross-file communication
 * - Database integration with JPA
 * - Conditional expressions (ternary operator)
 * - Range syntax (8000-9000)
 */
public class TuskLangEnhanced {
    private final Map<String, Object> config;
    private final Map<String, String> globalVariables;
    private final Map<String, Map<String, Object>> fileCache;
    private final Map<String, CacheEntry> operatorCache;
    private final Map<String, Double> metrics;
    private final Map<String, Object> learnedValues;
    private EntityManagerFactory emf;
    private boolean peanutLoaded = false;
    
    // Configuration options
    private boolean allowBrackets = true;      // []
    private boolean allowBraces = true;        // {}
    private boolean allowAngleBrackets = true; // <>
    private boolean allowSemicolons = false;   // Optional semicolons
    
    public TuskLangEnhanced() {
        this.config = new LinkedHashMap<>();
        this.globalVariables = new ConcurrentHashMap<>();
        this.fileCache = new ConcurrentHashMap<>();
        this.operatorCache = new ConcurrentHashMap<>();
        this.metrics = new ConcurrentHashMap<>();
        this.learnedValues = new ConcurrentHashMap<>();
        
        // Load peanut.tsk automatically
        loadPeanutConfig();
    }
    
    /**
     * Parse TuskLang content with enhanced syntax
     */
    public Map<String, Object> parse(String content) {
        Map<String, Object> result = new LinkedHashMap<>();
        String[] lines = content.split("\n");
        Stack<ParseContext> contextStack = new Stack<>();
        contextStack.push(new ParseContext(result, 0));
        
        for (int i = 0; i < lines.length; i++) {
            String line = lines[i];
            String trimmed = line.trim();
            
            // Skip empty lines and comments
            if (trimmed.isEmpty() || trimmed.startsWith("#")) {
                continue;
            }
            
            // Remove optional semicolon
            if (allowSemicolons && trimmed.endsWith(";")) {
                trimmed = trimmed.substring(0, trimmed.length() - 1).trim();
            }
            
            // Calculate indentation
            int indent = getIndentLevel(line);
            
            // Pop contexts to match indentation
            while (contextStack.size() > 1 && contextStack.peek().indent >= indent) {
                contextStack.pop();
            }
            
            ParseContext currentContext = contextStack.peek();
            
            // Check for array item
            if (trimmed.startsWith("- ")) {
                handleArrayItem(trimmed.substring(2).trim(), currentContext);
                continue;
            }
            
            // Check for section headers with flexible syntax
            if (isGroupStart(trimmed)) {
                String sectionName = extractSectionName(trimmed);
                Map<String, Object> section = new LinkedHashMap<>();
                currentContext.map.put(sectionName, section);
                contextStack.push(new ParseContext(section, indent));
                continue;
            }
            
            // Parse key-value pair
            parseKeyValue(trimmed, currentContext.map);
        }
        
        // Process all values for @ operators and variables
        processValues(result);
        
        return result;
    }
    
    /**
     * Parse key-value pair with flexible delimiters
     */
    private void parseKeyValue(String line, Map<String, Object> target) {
        // Support both : and = as delimiters
        String delimiter = line.contains(":") ? ":" : "=";
        int delimIndex = line.indexOf(delimiter);
        
        if (delimIndex == -1) {
            // No delimiter, treat as boolean true
            target.put(line, true);
            return;
        }
        
        String key = line.substring(0, delimIndex).trim();
        String valueStr = line.substring(delimIndex + 1).trim();
        
        // Parse the value
        Object value = parseValue(valueStr);
        target.put(key, value);
    }
    
    /**
     * Parse a value with support for all data types and @ operators
     */
    private Object parseValue(String valueStr) {
        if (valueStr.isEmpty()) {
            return "";
        }
        
        // Check for @ operators
        if (valueStr.startsWith("@")) {
            return valueStr; // Process later in processValues
        }
        
        // Check for variable reference
        if (valueStr.startsWith("$")) {
            return valueStr; // Process later in processValues
        }
        
        // Check for range syntax (e.g., 8000-9000)
        if (valueStr.matches("\\d+-\\d+")) {
            String[] parts = valueStr.split("-");
            int start = Integer.parseInt(parts[0]);
            int end = Integer.parseInt(parts[1]);
            List<Integer> range = new ArrayList<>();
            for (int i = start; i <= end; i++) {
                range.add(i);
            }
            return range;
        }
        
        // String with quotes
        if ((valueStr.startsWith("\"") && valueStr.endsWith("\"")) ||
            (valueStr.startsWith("'") && valueStr.endsWith("'"))) {
            return valueStr.substring(1, valueStr.length() - 1);
        }
        
        // Array
        if (valueStr.startsWith("[") && valueStr.endsWith("]")) {
            return parseArray(valueStr);
        }
        
        // Object
        if (valueStr.startsWith("{") && valueStr.endsWith("}")) {
            return parseInlineObject(valueStr);
        }
        
        // Boolean
        if ("true".equalsIgnoreCase(valueStr)) return true;
        if ("false".equalsIgnoreCase(valueStr)) return false;
        
        // Null
        if ("null".equalsIgnoreCase(valueStr)) return null;
        
        // Number
        try {
            if (valueStr.contains(".")) {
                return Double.parseDouble(valueStr);
            } else {
                return Long.parseLong(valueStr);
            }
        } catch (NumberFormatException e) {
            // Not a number
        }
        
        // Default to string
        return valueStr;
    }
    
    /**
     * Process all values for @ operators and variable substitution
     */
    private void processValues(Map<String, Object> map) {
        for (Map.Entry<String, Object> entry : map.entrySet()) {
            Object value = entry.getValue();
            
            if (value instanceof String) {
                String str = (String) value;
                
                // Process @ operators
                if (str.startsWith("@")) {
                    Object processed = processOperator(str);
                    map.put(entry.getKey(), processed);
                }
                // Process variables
                else if (str.contains("$")) {
                    String processed = processVariables(str);
                    map.put(entry.getKey(), processed);
                }
                // Process conditional expressions
                else if (str.contains(" ? ") && str.contains(" : ")) {
                    Object processed = processTernary(str);
                    map.put(entry.getKey(), processed);
                }
            } else if (value instanceof Map) {
                @SuppressWarnings("unchecked")
                Map<String, Object> nestedMap = (Map<String, Object>) value;
                processValues(nestedMap);
            } else if (value instanceof List) {
                @SuppressWarnings("unchecked")
                List<Object> list = (List<Object>) value;
                processList(list);
            }
        }
    }
    
    /**
     * Process @ operators
     */
    private Object processOperator(String operator) {
        // Check cache first
        CacheEntry cached = operatorCache.get(operator);
        if (cached != null && !cached.isExpired()) {
            return cached.value;
        }
        
        // @env operator
        if (operator.startsWith("@env(")) {
            return processEnv(operator);
        }
        
        // @date operator
        if (operator.startsWith("@date(")) {
            return processDate(operator);
        }
        
        // @cache operator
        if (operator.startsWith("@cache(")) {
            return processCache(operator);
        }
        
        // @metrics operator
        if (operator.startsWith("@metrics(")) {
            return processMetrics(operator);
        }
        
        // @learn operator
        if (operator.startsWith("@learn(")) {
            return processLearn(operator);
        }
        
        // @optimize operator
        if (operator.startsWith("@optimize(")) {
            return processOptimize(operator);
        }
        
        // @query operator (database queries)
        if (operator.startsWith("@query(")) {
            return processQuery(operator);
        }
        
        // @file.tsk.get operator (cross-file communication)
        if (operator.matches("@\\w+\\.tsk\\.get\\(.+\\)")) {
            return processFileGet(operator);
        }
        
        // Default: return as-is
        return operator;
    }
    
    /**
     * Process @env operator
     */
    private String processEnv(String operator) {
        Pattern pattern = Pattern.compile("@env\\(\"([^\"]+)\"(?:,\\s*\"([^\"]+)\")?\\)");
        Matcher matcher = pattern.matcher(operator);
        
        if (matcher.find()) {
            String varName = matcher.group(1);
            String defaultValue = matcher.group(2);
            String value = System.getenv(varName);
            return value != null ? value : (defaultValue != null ? defaultValue : "");
        }
        
        return operator;
    }
    
    /**
     * Process @date operator
     */
    private String processDate(String operator) {
        Pattern pattern = Pattern.compile("@date\\(\"([^\"]+)\"\\)");
        Matcher matcher = pattern.matcher(operator);
        
        if (matcher.find()) {
            String format = matcher.group(1);
            SimpleDateFormat sdf = new SimpleDateFormat(format);
            return sdf.format(new Date());
        }
        
        // Default format
        return new SimpleDateFormat("yyyy-MM-dd HH:mm:ss").format(new Date());
    }
    
    /**
     * Process @cache operator
     */
    private Object processCache(String operator) {
        Pattern pattern = Pattern.compile("@cache\\(\"([^\"]+)\",\\s*(.+)\\)");
        Matcher matcher = pattern.matcher(operator);
        
        if (matcher.find()) {
            String ttl = matcher.group(1);
            String valueExpr = matcher.group(2);
            
            // Parse TTL (e.g., "5m", "1h", "30s")
            long ttlMillis = parseTTL(ttl);
            
            // Check if we have a cached value
            String cacheKey = "cache:" + valueExpr;
            CacheEntry cached = operatorCache.get(cacheKey);
            
            if (cached != null && !cached.isExpired()) {
                return cached.value;
            }
            
            // Evaluate the value expression
            Object value = parseValue(valueExpr);
            
            // Cache it
            operatorCache.put(cacheKey, new CacheEntry(value, ttlMillis));
            
            return value;
        }
        
        return operator;
    }
    
    /**
     * Process @metrics operator
     */
    private Object processMetrics(String operator) {
        Pattern pattern = Pattern.compile("@metrics\\(\"([^\"]+)\"(?:,\\s*(.+))?\\)");
        Matcher matcher = pattern.matcher(operator);
        
        if (matcher.find()) {
            String metricName = matcher.group(1);
            String valueStr = matcher.group(2);
            
            if (valueStr != null) {
                // Set metric value
                try {
                    double value = Double.parseDouble(valueStr);
                    metrics.put(metricName, value);
                    return value;
                } catch (NumberFormatException e) {
                    return 0.0;
                }
            } else {
                // Get metric value
                return metrics.getOrDefault(metricName, 0.0);
            }
        }
        
        return 0.0;
    }
    
    /**
     * Process @learn operator
     */
    private Object processLearn(String operator) {
        Pattern pattern = Pattern.compile("@learn\\(\"([^\"]+)\",\\s*(.+)\\)");
        Matcher matcher = pattern.matcher(operator);
        
        if (matcher.find()) {
            String key = matcher.group(1);
            String defaultValueStr = matcher.group(2);
            
            // Check if we have a learned value
            if (learnedValues.containsKey(key)) {
                return learnedValues.get(key);
            }
            
            // Use default value
            Object defaultValue = parseValue(defaultValueStr);
            learnedValues.put(key, defaultValue);
            
            return defaultValue;
        }
        
        return operator;
    }
    
    /**
     * Process @optimize operator
     */
    private Object processOptimize(String operator) {
        Pattern pattern = Pattern.compile("@optimize\\(\"([^\"]+)\",\\s*(.+)\\)");
        Matcher matcher = pattern.matcher(operator);
        
        if (matcher.find()) {
            String param = matcher.group(1);
            String initialValueStr = matcher.group(2);
            
            // For now, use a simple optimization strategy
            // In a real implementation, this would use ML or heuristics
            Object currentValue = learnedValues.get("optimize:" + param);
            
            if (currentValue != null) {
                return currentValue;
            }
            
            // Use initial value
            Object initialValue = parseValue(initialValueStr);
            learnedValues.put("optimize:" + param, initialValue);
            
            return initialValue;
        }
        
        return operator;
    }
    
    /**
     * Process @query operator (database queries)
     */
    private Object processQuery(String operator) {
        if (emf == null) {
            // Initialize JPA if not already done
            try {
                emf = Persistence.createEntityManagerFactory("tusklang");
            } catch (Exception e) {
                System.err.println("Database not configured: " + e.getMessage());
                return Collections.emptyList();
            }
        }
        
        Pattern pattern = Pattern.compile("@query\\(\"([^\"]+)\"\\)(.*)");
        Matcher matcher = pattern.matcher(operator);
        
        if (matcher.find()) {
            String entityName = matcher.group(1);
            String queryChain = matcher.group(2);
            
            EntityManager em = emf.createEntityManager();
            try {
                // Build JPQL query
                StringBuilder jpql = new StringBuilder("SELECT e FROM ");
                jpql.append(entityName).append(" e");
                
                // Parse query chain (.where(), .orderBy(), etc.)
                if (queryChain.contains(".where(")) {
                    Pattern wherePattern = Pattern.compile("\\.where\\(\"([^\"]+)\",\\s*(.+?)\\)");
                    Matcher whereMatcher = wherePattern.matcher(queryChain);
                    if (whereMatcher.find()) {
                        jpql.append(" WHERE e.").append(whereMatcher.group(1));
                        jpql.append(" = :param");
                    }
                }
                
                Query query = em.createQuery(jpql.toString());
                
                // Set parameters if needed
                if (queryChain.contains(".where(")) {
                    Pattern wherePattern = Pattern.compile("\\.where\\(\"([^\"]+)\",\\s*(.+?)\\)");
                    Matcher whereMatcher = wherePattern.matcher(queryChain);
                    if (whereMatcher.find()) {
                        String valueStr = whereMatcher.group(2);
                        Object value = parseValue(valueStr);
                        query.setParameter("param", value);
                    }
                }
                
                // Execute query
                return query.getResultList();
                
            } finally {
                em.close();
            }
        }
        
        return Collections.emptyList();
    }
    
    /**
     * Process cross-file communication
     */
    private Object processFileGet(String operator) {
        Pattern pattern = Pattern.compile("@(\\w+)\\.tsk\\.get\\(\"([^\"]+)\"\\)");
        Matcher matcher = pattern.matcher(operator);
        
        if (matcher.find()) {
            String fileName = matcher.group(1) + ".tsk";
            String key = matcher.group(2);
            
            // Check file cache
            Map<String, Object> fileConfig = fileCache.get(fileName);
            
            if (fileConfig == null) {
                // Load the file
                try {
                    String content = new String(Files.readAllBytes(Paths.get(fileName)));
                    fileConfig = parse(content);
                    fileCache.put(fileName, fileConfig);
                } catch (IOException e) {
                    return null;
                }
            }
            
            // Get the value
            return getNestedValue(fileConfig, key);
        }
        
        return null;
    }
    
    /**
     * Process variables with $ prefix
     */
    private String processVariables(String input) {
        String result = input;
        
        // Replace $variables
        Pattern pattern = Pattern.compile("\\$(\\w+)");
        Matcher matcher = pattern.matcher(input);
        
        while (matcher.find()) {
            String varName = matcher.group(1);
            String value = globalVariables.get(varName);
            
            if (value != null) {
                result = result.replace("$" + varName, value);
            }
        }
        
        return result;
    }
    
    /**
     * Process ternary conditional expressions
     */
    private Object processTernary(String expression) {
        Pattern pattern = Pattern.compile("(.+?)\\s*\\?\\s*(.+?)\\s*:\\s*(.+)");
        Matcher matcher = pattern.matcher(expression);
        
        if (matcher.find()) {
            String condition = matcher.group(1).trim();
            String trueValue = matcher.group(2).trim();
            String falseValue = matcher.group(3).trim();
            
            // Evaluate condition
            boolean result = evaluateCondition(condition);
            
            // Return appropriate value
            return parseValue(result ? trueValue : falseValue);
        }
        
        return expression;
    }
    
    /**
     * Load peanut.tsk configuration
     */
    private void loadPeanutConfig() {
        if (peanutLoaded) {
            return;
        }
        
        List<String> searchPaths = Arrays.asList(
            "peanut.tsk",
            "../peanut.tsk",
            "../../peanut.tsk",
            System.getProperty("user.home") + "/.config/tusklang/peanut.tsk",
            "/etc/tusklang/peanut.tsk"
        );
        
        for (String path : searchPaths) {
            try {
                Path filePath = Paths.get(path);
                if (Files.exists(filePath)) {
                    String content = new String(Files.readAllBytes(filePath));
                    Map<String, Object> peanutConfig = parse(content);
                    
                    // Merge into main config
                    mergeConfig(config, peanutConfig);
                    
                    peanutLoaded = true;
                    System.out.println("Loaded peanut.tsk from: " + path);
                    break;
                }
            } catch (IOException e) {
                // Continue searching
            }
        }
    }
    
    /**
     * Helper methods
     */
    
    private int getIndentLevel(String line) {
        int spaces = 0;
        for (char c : line.toCharArray()) {
            if (c == ' ') spaces++;
            else if (c == '\t') spaces += 4;
            else break;
        }
        return spaces / 2;
    }
    
    private boolean isGroupStart(String line) {
        return (allowBrackets && line.matches("\\[\\w+\\]")) ||
               (allowBraces && line.endsWith("{")) ||
               (allowAngleBrackets && line.matches("<\\w+>"));
    }
    
    private String extractSectionName(String line) {
        if (line.matches("\\[\\w+\\]")) {
            return line.substring(1, line.length() - 1);
        } else if (line.endsWith("{")) {
            return line.substring(0, line.length() - 1).trim();
        } else if (line.matches("<\\w+>")) {
            return line.substring(1, line.length() - 1);
        }
        return line;
    }
    
    private void handleArrayItem(String value, ParseContext context) {
        if (context.currentList == null) {
            throw new RuntimeException("Array item without array context");
        }
        context.currentList.add(parseValue(value));
    }
    
    private List<Object> parseArray(String arrayStr) {
        List<Object> result = new ArrayList<>();
        String content = arrayStr.substring(1, arrayStr.length() - 1).trim();
        
        if (!content.isEmpty()) {
            String[] items = content.split(",");
            for (String item : items) {
                result.add(parseValue(item.trim()));
            }
        }
        
        return result;
    }
    
    private Map<String, Object> parseInlineObject(String objStr) {
        Map<String, Object> result = new LinkedHashMap<>();
        String content = objStr.substring(1, objStr.length() - 1).trim();
        
        if (!content.isEmpty()) {
            // Simple inline object parsing
            String[] pairs = content.split(",");
            for (String pair : pairs) {
                String[] kv = pair.split(":");
                if (kv.length == 2) {
                    result.put(kv[0].trim(), parseValue(kv[1].trim()));
                }
            }
        }
        
        return result;
    }
    
    private void processList(List<Object> list) {
        for (int i = 0; i < list.size(); i++) {
            Object item = list.get(i);
            if (item instanceof String) {
                String str = (String) item;
                if (str.startsWith("@")) {
                    list.set(i, processOperator(str));
                } else if (str.contains("$")) {
                    list.set(i, processVariables(str));
                }
            } else if (item instanceof Map) {
                @SuppressWarnings("unchecked")
                Map<String, Object> map = (Map<String, Object>) item;
                processValues(map);
            } else if (item instanceof List) {
                @SuppressWarnings("unchecked")
                List<Object> nestedList = (List<Object>) item;
                processList(nestedList);
            }
        }
    }
    
    private Object getNestedValue(Map<String, Object> map, String key) {
        String[] parts = key.split("\\.");
        Object current = map;
        
        for (String part : parts) {
            if (current instanceof Map) {
                @SuppressWarnings("unchecked")
                Map<String, Object> currentMap = (Map<String, Object>) current;
                current = currentMap.get(part);
            } else {
                return null;
            }
        }
        
        return current;
    }
    
    private void mergeConfig(Map<String, Object> target, Map<String, Object> source) {
        for (Map.Entry<String, Object> entry : source.entrySet()) {
            String key = entry.getKey();
            Object value = entry.getValue();
            
            if (value instanceof Map && target.containsKey(key) && target.get(key) instanceof Map) {
                @SuppressWarnings("unchecked")
                Map<String, Object> targetMap = (Map<String, Object>) target.get(key);
                @SuppressWarnings("unchecked")
                Map<String, Object> sourceMap = (Map<String, Object>) value;
                mergeConfig(targetMap, sourceMap);
            } else {
                target.put(key, value);
            }
        }
    }
    
    private long parseTTL(String ttl) {
        Pattern pattern = Pattern.compile("(\\d+)([smhd])");
        Matcher matcher = pattern.matcher(ttl);
        
        if (matcher.find()) {
            int value = Integer.parseInt(matcher.group(1));
            String unit = matcher.group(2);
            
            switch (unit) {
                case "s": return value * 1000L;
                case "m": return value * 60 * 1000L;
                case "h": return value * 60 * 60 * 1000L;
                case "d": return value * 24 * 60 * 60 * 1000L;
            }
        }
        
        return 300000L; // Default 5 minutes
    }
    
    private boolean evaluateCondition(String condition) {
        // Simple condition evaluation
        if (condition.contains(">")) {
            String[] parts = condition.split(">");
            try {
                double left = Double.parseDouble(parts[0].trim());
                double right = Double.parseDouble(parts[1].trim());
                return left > right;
            } catch (NumberFormatException e) {
                return false;
            }
        }
        
        // Check for boolean
        return "true".equalsIgnoreCase(condition.trim());
    }
    
    /**
     * Public API methods
     */
    
    public Map<String, Object> parseFile(String filename) throws IOException {
        String content = new String(Files.readAllBytes(Paths.get(filename)));
        return parse(content);
    }
    
    public void setGlobalVariable(String name, String value) {
        globalVariables.put(name, value);
    }
    
    public Object get(String key) {
        return getNestedValue(config, key);
    }
    
    public void set(String key, Object value) {
        String[] parts = key.split("\\.");
        Map<String, Object> current = config;
        
        for (int i = 0; i < parts.length - 1; i++) {
            String part = parts[i];
            if (!current.containsKey(part)) {
                current.put(part, new LinkedHashMap<String, Object>());
            }
            @SuppressWarnings("unchecked")
            Map<String, Object> next = (Map<String, Object>) current.get(part);
            current = next;
        }
        
        current.put(parts[parts.length - 1], value);
    }
    
    public Map<String, Object> getConfig() {
        return config;
    }
    
    /**
     * Helper classes
     */
    
    private static class ParseContext {
        final Map<String, Object> map;
        final List<Object> currentList;
        final int indent;
        
        ParseContext(Map<String, Object> map, int indent) {
            this.map = map;
            this.currentList = null;
            this.indent = indent;
        }
        
        ParseContext(List<Object> list, int indent) {
            this.map = null;
            this.currentList = list;
            this.indent = indent;
        }
    }
    
    private static class CacheEntry {
        final Object value;
        final long expiry;
        
        CacheEntry(Object value, long ttlMillis) {
            this.value = value;
            this.expiry = System.currentTimeMillis() + ttlMillis;
        }
        
        boolean isExpired() {
            return System.currentTimeMillis() > expiry;
        }
    }
}