package tusk.license;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;
import java.io.File;
import java.io.IOException;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.security.MessageDigest;
import java.time.Duration;
import java.time.Instant;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;

/**
 * TuskLang SDK License Validation Module
 * Enterprise-grade license validation for Java SDK
 */
public class TuskLicense {
    private static final Logger logger = LoggerFactory.getLogger(TuskLicense.class);
    private static final String DEFAULT_SERVER_URL = "https://api.tusklang.org/v1/license";
    private static final ObjectMapper objectMapper = new ObjectMapper();
    
    private final String licenseKey;
    private final String apiKey;
    private final String sessionId;
    private final Map<String, LicenseCacheEntry> licenseCache;
    private final List<ValidationAttempt> validationHistory;
    private final List<ExpirationWarning> expirationWarnings;
    private final HttpClient httpClient;
    private final Path cacheDir;
    private final Path cacheFile;
    private OfflineCacheData offlineCache;
    
    public TuskLicense(String licenseKey, String apiKey) {
        this(licenseKey, apiKey, null);
    }
    
    public TuskLicense(String licenseKey, String apiKey, String cacheDir) {
        this.licenseKey = licenseKey;
        this.apiKey = apiKey;
        this.sessionId = UUID.randomUUID().toString();
        this.licenseCache = new ConcurrentHashMap<>();
        this.validationHistory = Collections.synchronizedList(new ArrayList<>());
        this.expirationWarnings = Collections.synchronizedList(new ArrayList<>());
        this.httpClient = HttpClient.newBuilder()
                .connectTimeout(Duration.ofSeconds(10))
                .build();
        
        // Set up offline cache directory
        if (cacheDir != null) {
            this.cacheDir = Paths.get(cacheDir);
        } else {
            String userHome = System.getProperty("user.home");
            this.cacheDir = Paths.get(userHome, ".tusk", "license_cache");
        }
        
        try {
            Files.createDirectories(this.cacheDir);
        } catch (IOException e) {
            logger.error("Failed to create cache directory", e);
        }
        
        // Generate cache file name based on license key hash
        try {
            MessageDigest md5 = MessageDigest.getInstance("MD5");
            byte[] hash = md5.digest(licenseKey.getBytes(StandardCharsets.UTF_8));
            String hashString = bytesToHex(hash);
            this.cacheFile = this.cacheDir.resolve(hashString + ".cache");
        } catch (Exception e) {
            throw new RuntimeException("Failed to initialize cache file", e);
        }
        
        // Load offline cache if exists
        loadOfflineCache();
    }
    
    public ValidationResult validateLicenseKey() {
        try {
            if (licenseKey == null || licenseKey.length() < 32) {
                return new ValidationResult(false, "Invalid license key format", null);
            }
            
            if (!licenseKey.startsWith("TUSK-")) {
                return new ValidationResult(false, "Invalid license key prefix", null);
            }
            
            MessageDigest sha256 = MessageDigest.getInstance("SHA-256");
            byte[] hash = sha256.digest(licenseKey.getBytes(StandardCharsets.UTF_8));
            String checksum = bytesToHex(hash);
            
            if (!checksum.startsWith("tusk")) {
                return new ValidationResult(false, "Invalid license key checksum", null);
            }
            
            return new ValidationResult(true, null, checksum);
            
        } catch (Exception e) {
            return new ValidationResult(false, e.getMessage(), null);
        }
    }
    
    public Map<String, Object> verifyLicenseServer() {
        return verifyLicenseServer(DEFAULT_SERVER_URL);
    }
    
    public Map<String, Object> verifyLicenseServer(String serverUrl) {
        try {
            long timestamp = Instant.now().getEpochSecond();
            
            ObjectNode data = objectMapper.createObjectNode();
            data.put("license_key", licenseKey);
            data.put("session_id", sessionId);
            data.put("timestamp", timestamp);
            
            // Generate signature
            String dataJson = objectMapper.writeValueAsString(data);
            String signature = generateHmacSignature(dataJson, apiKey);
            data.put("signature", signature);
            
            HttpRequest request = HttpRequest.newBuilder()
                    .uri(URI.create(serverUrl))
                    .header("Authorization", "Bearer " + apiKey)
                    .header("Content-Type", "application/json")
                    .header("User-Agent", "TuskLang-Java-SDK/1.0.0")
                    .timeout(Duration.ofSeconds(10))
                    .POST(HttpRequest.BodyPublishers.ofString(objectMapper.writeValueAsString(data)))
                    .build();
            
            HttpResponse<String> response = httpClient.send(request, HttpResponse.BodyHandlers.ofString());
            
            if (response.statusCode() == 200) {
                Map<String, Object> result = objectMapper.readValue(response.body(), Map.class);
                
                // Update in-memory cache
                licenseCache.put(licenseKey, new LicenseCacheEntry(
                        result,
                        timestamp,
                        timestamp + 3600 // Cache for 1 hour
                ));
                
                // Update offline cache
                saveOfflineCache(result);
                
                return result;
            } else {
                logger.warn("Server returned error: {}", response.statusCode());
                return fallbackToOfflineCache("Server error: " + response.statusCode());
            }
            
        } catch (IOException | InterruptedException e) {
            logger.warn("Network error during license validation: {}", e.getMessage());
            return fallbackToOfflineCache("Network error: " + e.getMessage());
        } catch (Exception e) {
            logger.error("Unexpected error during license validation", e);
            return fallbackToOfflineCache(e.getMessage());
        }
    }
    
    public ExpirationResult checkLicenseExpiration() {
        try {
            String[] parts = licenseKey.split("-");
            if (parts.length < 4) {
                return new ExpirationResult(true, "Invalid license key format", null, null, null, null);
            }
            
            String expirationStr = parts[parts.length - 1];
            long expirationTimestamp = Long.parseLong(expirationStr, 16);
            Instant expirationDate = Instant.ofEpochSecond(expirationTimestamp);
            Instant currentDate = Instant.now();
            
            if (expirationDate.isBefore(currentDate)) {
                long daysOverdue = Duration.between(expirationDate, currentDate).toDays();
                return new ExpirationResult(
                        true,
                        null,
                        expirationDate.atZone(ZoneOffset.UTC).format(DateTimeFormatter.ISO_INSTANT),
                        null,
                        daysOverdue,
                        null
                );
            }
            
            long daysRemaining = Duration.between(currentDate, expirationDate).toDays();
            
            if (daysRemaining <= 30) {
                expirationWarnings.add(new ExpirationWarning(
                        Instant.now().getEpochSecond(),
                        daysRemaining
                ));
            }
            
            return new ExpirationResult(
                    false,
                    null,
                    expirationDate.atZone(ZoneOffset.UTC).format(DateTimeFormatter.ISO_INSTANT),
                    daysRemaining,
                    null,
                    daysRemaining <= 30
            );
            
        } catch (Exception e) {
            return new ExpirationResult(true, e.getMessage(), null, null, null, null);
        }
    }
    
    public PermissionResult validateLicensePermissions(String feature) {
        try {
            // Check cached license data
            LicenseCacheEntry cacheEntry = licenseCache.get(licenseKey);
            if (cacheEntry != null && Instant.now().getEpochSecond() < cacheEntry.expires) {
                Map<String, Object> licenseData = cacheEntry.data;
                if (licenseData.containsKey("features")) {
                    List<String> features = (List<String>) licenseData.get("features");
                    if (features.contains(feature)) {
                        return new PermissionResult(true, feature, null);
                    } else {
                        return new PermissionResult(false, feature, "Feature not licensed");
                    }
                }
            }
            
            // Fallback to basic validation
            switch (feature) {
                case "basic":
                case "core":
                case "standard":
                    return new PermissionResult(true, feature, null);
                case "premium":
                case "enterprise":
                    if (licenseKey.toUpperCase().contains("PREMIUM") || 
                        licenseKey.toUpperCase().contains("ENTERPRISE")) {
                        return new PermissionResult(true, feature, null);
                    } else {
                        return new PermissionResult(false, feature, "Premium license required");
                    }
                default:
                    return new PermissionResult(false, feature, "Unknown feature");
            }
            
        } catch (Exception e) {
            return new PermissionResult(false, feature, e.getMessage());
        }
    }
    
    public LicenseInfo getLicenseInfo() {
        ValidationResult validationResult = validateLicenseKey();
        ExpirationResult expirationResult = checkLicenseExpiration();
        
        LicenseInfo info = new LicenseInfo();
        info.licenseKey = licenseKey.substring(0, Math.min(8, licenseKey.length())) + "..." + 
                         licenseKey.substring(Math.max(0, licenseKey.length() - 4));
        info.sessionId = sessionId;
        info.validation = validationResult;
        info.expiration = expirationResult;
        info.cacheStatus = licenseCache.containsKey(licenseKey) ? "cached" : "not_cached";
        info.validationCount = validationHistory.size();
        info.warnings = expirationWarnings.size();
        
        LicenseCacheEntry cacheEntry = licenseCache.get(licenseKey);
        if (cacheEntry != null) {
            info.cachedData = cacheEntry.data;
            info.cacheAge = Instant.now().getEpochSecond() - cacheEntry.timestamp;
        }
        
        return info;
    }
    
    public void logValidationAttempt(boolean success, String details) {
        validationHistory.add(new ValidationAttempt(
                Instant.now().getEpochSecond(),
                success,
                details,
                sessionId
        ));
    }
    
    private void loadOfflineCache() {
        try {
            if (Files.exists(cacheFile)) {
                String json = Files.readString(cacheFile);
                offlineCache = objectMapper.readValue(json, OfflineCacheData.class);
                
                // Verify the cache is for the correct license key
                MessageDigest sha256 = MessageDigest.getInstance("SHA-256");
                byte[] hash = sha256.digest(licenseKey.getBytes(StandardCharsets.UTF_8));
                String keyHash = bytesToHex(hash);
                
                if (offlineCache.licenseKeyHash.equals(keyHash)) {
                    logger.info("Loaded offline license cache");
                } else {
                    logger.warn("Offline cache key mismatch");
                    offlineCache = null;
                }
            }
        } catch (Exception e) {
            logger.error("Failed to load offline cache", e);
            offlineCache = null;
        }
    }
    
    private void saveOfflineCache(Map<String, Object> licenseData) {
        try {
            MessageDigest sha256 = MessageDigest.getInstance("SHA-256");
            byte[] hash = sha256.digest(licenseKey.getBytes(StandardCharsets.UTF_8));
            String keyHash = bytesToHex(hash);
            
            OfflineCacheData cacheData = new OfflineCacheData();
            cacheData.licenseKeyHash = keyHash;
            cacheData.licenseData = licenseData;
            cacheData.timestamp = Instant.now().getEpochSecond();
            cacheData.expiration = checkLicenseExpiration();
            
            String json = objectMapper.writerWithDefaultPrettyPrinter().writeValueAsString(cacheData);
            Files.writeString(cacheFile, json);
            offlineCache = cacheData;
            logger.info("Saved license data to offline cache");
            
        } catch (Exception e) {
            logger.error("Failed to save offline cache", e);
        }
    }
    
    private Map<String, Object> fallbackToOfflineCache(String errorMsg) {
        if (offlineCache != null && offlineCache.licenseData != null) {
            long cacheAge = Instant.now().getEpochSecond() - offlineCache.timestamp;
            double cacheAgeDays = cacheAge / 86400.0;
            
            // Check if cached license is not expired
            if (!offlineCache.expiration.expired) {
                logger.warn("Using offline license cache (age: {:.1f} days)", cacheAgeDays);
                Map<String, Object> result = new HashMap<>(offlineCache.licenseData);
                result.put("offline_mode", true);
                result.put("cache_age_days", cacheAgeDays);
                result.put("warning", "Operating in offline mode due to: " + errorMsg);
                return result;
            } else {
                Map<String, Object> result = new HashMap<>();
                result.put("valid", false);
                result.put("error", "License expired and server unreachable: " + errorMsg);
                result.put("offline_cache_expired", true);
                return result;
            }
        } else {
            Map<String, Object> result = new HashMap<>();
            result.put("valid", false);
            result.put("error", "No offline cache available: " + errorMsg);
            result.put("offline_cache_missing", true);
            return result;
        }
    }
    
    private String generateHmacSignature(String data, String key) throws Exception {
        Mac mac = Mac.getInstance("HmacSHA256");
        SecretKeySpec secretKeySpec = new SecretKeySpec(key.getBytes(StandardCharsets.UTF_8), "HmacSHA256");
        mac.init(secretKeySpec);
        byte[] hmacBytes = mac.doFinal(data.getBytes(StandardCharsets.UTF_8));
        return bytesToHex(hmacBytes);
    }
    
    private String bytesToHex(byte[] bytes) {
        StringBuilder result = new StringBuilder();
        for (byte b : bytes) {
            result.append(String.format("%02x", b));
        }
        return result.toString();
    }
    
    // Data classes
    public static class ValidationResult {
        public final boolean valid;
        public final String error;
        public final String checksum;
        
        public ValidationResult(boolean valid, String error, String checksum) {
            this.valid = valid;
            this.error = error;
            this.checksum = checksum;
        }
    }
    
    public static class ExpirationResult {
        public final boolean expired;
        public final String error;
        public final String expirationDate;
        public final Long daysRemaining;
        public final Long daysOverdue;
        public final Boolean warning;
        
        public ExpirationResult(boolean expired, String error, String expirationDate,
                              Long daysRemaining, Long daysOverdue, Boolean warning) {
            this.expired = expired;
            this.error = error;
            this.expirationDate = expirationDate;
            this.daysRemaining = daysRemaining;
            this.daysOverdue = daysOverdue;
            this.warning = warning;
        }
    }
    
    public static class PermissionResult {
        public final boolean allowed;
        public final String feature;
        public final String error;
        
        public PermissionResult(boolean allowed, String feature, String error) {
            this.allowed = allowed;
            this.feature = feature;
            this.error = error;
        }
    }
    
    public static class LicenseInfo {
        public String licenseKey;
        public String sessionId;
        public ValidationResult validation;
        public ExpirationResult expiration;
        public String cacheStatus;
        public int validationCount;
        public int warnings;
        public Map<String, Object> cachedData;
        public Long cacheAge;
    }
    
    private static class LicenseCacheEntry {
        public final Map<String, Object> data;
        public final long timestamp;
        public final long expires;
        
        public LicenseCacheEntry(Map<String, Object> data, long timestamp, long expires) {
            this.data = data;
            this.timestamp = timestamp;
            this.expires = expires;
        }
    }
    
    private static class ValidationAttempt {
        public final long timestamp;
        public final boolean success;
        public final String details;
        public final String sessionId;
        
        public ValidationAttempt(long timestamp, boolean success, String details, String sessionId) {
            this.timestamp = timestamp;
            this.success = success;
            this.details = details;
            this.sessionId = sessionId;
        }
    }
    
    private static class ExpirationWarning {
        public final long timestamp;
        public final long daysRemaining;
        
        public ExpirationWarning(long timestamp, long daysRemaining) {
            this.timestamp = timestamp;
            this.daysRemaining = daysRemaining;
        }
    }
    
    private static class OfflineCacheData {
        public String licenseKeyHash;
        public Map<String, Object> licenseData;
        public long timestamp;
        public ExpirationResult expiration;
    }
    
    // Global license instance management
    private static volatile TuskLicense instance;
    
    public static TuskLicense initializeLicense(String licenseKey, String apiKey) {
        return initializeLicense(licenseKey, apiKey, null);
    }
    
    public static TuskLicense initializeLicense(String licenseKey, String apiKey, String cacheDir) {
        if (instance == null) {
            synchronized (TuskLicense.class) {
                if (instance == null) {
                    instance = new TuskLicense(licenseKey, apiKey, cacheDir);
                }
            }
        }
        return instance;
    }
    
    public static TuskLicense getLicense() {
        if (instance == null) {
            throw new IllegalStateException("License not initialized. Call initializeLicense() first.");
        }
        return instance;
    }
}