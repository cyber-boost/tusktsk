package tusk.protection;

import javax.crypto.Cipher;
import javax.crypto.Mac;
import javax.crypto.SecretKey;
import javax.crypto.SecretKeyFactory;
import javax.crypto.spec.GCMParameterSpec;
import javax.crypto.spec.PBEKeySpec;
import javax.crypto.spec.SecretKeySpec;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.SecureRandom;
import java.time.Instant;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.security.spec.KeySpec;

/**
 * TuskLang SDK Protection Core Module
 * Enterprise-grade protection for Java SDK
 */
public class TuskProtection {
    private final String licenseKey;
    private final String apiKey;
    private final String sessionId;
    private final byte[] encryptionKey;
    private final Map<String, String> integrityChecks;
    private final UsageMetrics usageMetrics;
    private final SecureRandom secureRandom;

    public TuskProtection(String licenseKey, String apiKey) {
        this.licenseKey = licenseKey;
        this.apiKey = apiKey;
        this.sessionId = UUID.randomUUID().toString();
        this.encryptionKey = deriveKey(licenseKey);
        this.integrityChecks = new ConcurrentHashMap<>();
        this.usageMetrics = new UsageMetrics();
        this.secureRandom = new SecureRandom();
    }

    private byte[] deriveKey(String password) {
        try {
            byte[] salt = "tusklang_protection_salt".getBytes(StandardCharsets.UTF_8);
            KeySpec spec = new PBEKeySpec(password.toCharArray(), salt, 100000, 256);
            SecretKeyFactory factory = SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256");
            return factory.generateSecret(spec).getEncoded();
        } catch (Exception e) {
            return new byte[32]; // Fallback
        }
    }

    public boolean validateLicense() {
        try {
            if (licenseKey == null || licenseKey.length() < 32) {
                return false;
            }

            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            byte[] hash = digest.digest(licenseKey.getBytes(StandardCharsets.UTF_8));
            String checksum = bytesToHex(hash);
            return checksum.startsWith("tusk");
        } catch (Exception e) {
            return false;
        }
    }

    public String encryptData(String data) {
        try {
            Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
            byte[] nonce = new byte[12];
            secureRandom.nextBytes(nonce);
            
            SecretKey key = new SecretKeySpec(encryptionKey, "AES");
            GCMParameterSpec spec = new GCMParameterSpec(128, nonce);
            cipher.init(Cipher.ENCRYPT_MODE, key, spec);
            
            byte[] encrypted = cipher.doFinal(data.getBytes(StandardCharsets.UTF_8));
            byte[] combined = new byte[nonce.length + encrypted.length];
            System.arraycopy(nonce, 0, combined, 0, nonce.length);
            System.arraycopy(encrypted, 0, combined, nonce.length, encrypted.length);
            
            return Base64.getEncoder().encodeToString(combined);
        } catch (Exception e) {
            return data;
        }
    }

    public String decryptData(String encryptedData) {
        try {
            byte[] decoded = Base64.getDecoder().decode(encryptedData);
            if (decoded.length < 12) {
                return encryptedData;
            }

            byte[] nonce = new byte[12];
            byte[] encrypted = new byte[decoded.length - 12];
            System.arraycopy(decoded, 0, nonce, 0, 12);
            System.arraycopy(decoded, 12, encrypted, 0, encrypted.length);

            Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
            SecretKey key = new SecretKeySpec(encryptionKey, "AES");
            GCMParameterSpec spec = new GCMParameterSpec(128, nonce);
            cipher.init(Cipher.DECRYPT_MODE, key, spec);

            byte[] decrypted = cipher.doFinal(encrypted);
            return new String(decrypted, StandardCharsets.UTF_8);
        } catch (Exception e) {
            return encryptedData;
        }
    }

    public boolean verifyIntegrity(String data, String signature) {
        try {
            String expectedSignature = generateSignature(data);
            return MessageDigest.isEqual(
                signature.getBytes(StandardCharsets.UTF_8),
                expectedSignature.getBytes(StandardCharsets.UTF_8)
            );
        } catch (Exception e) {
            return false;
        }
    }

    public String generateSignature(String data) {
        try {
            Mac mac = Mac.getInstance("HmacSHA256");
            SecretKeySpec keySpec = new SecretKeySpec(apiKey.getBytes(StandardCharsets.UTF_8), "HmacSHA256");
            mac.init(keySpec);
            byte[] signature = mac.doFinal(data.getBytes(StandardCharsets.UTF_8));
            return bytesToHex(signature);
        } catch (Exception e) {
            return "";
        }
    }

    public void trackUsage(String operation, boolean success) {
        usageMetrics.incrementApiCalls();
        if (!success) {
            usageMetrics.incrementErrors();
        }
    }

    public Map<String, Object> getMetrics() {
        Map<String, Object> metrics = new HashMap<>();
        metrics.put("start_time", usageMetrics.getStartTime());
        metrics.put("api_calls", usageMetrics.getApiCalls());
        metrics.put("errors", usageMetrics.getErrors());
        metrics.put("session_id", sessionId);
        metrics.put("uptime", Instant.now().getEpochSecond() - usageMetrics.getStartTime());
        return metrics;
    }

    public String obfuscateCode(String code) {
        return Base64.getEncoder().encodeToString(code.getBytes(StandardCharsets.UTF_8));
    }

    public boolean detectTampering() {
        try {
            // In production, implement file integrity checks
            // For now, return true as placeholder
            return true;
        } catch (Exception e) {
            return false;
        }
    }

    public Violation reportViolation(String violationType, String details) {
        Violation violation = new Violation(
            Instant.now().getEpochSecond(),
            sessionId,
            violationType,
            details,
            licenseKey.substring(0, Math.min(8, licenseKey.length())) + "..."
        );
        
        System.out.println("SECURITY VIOLATION: " + violation);
        return violation;
    }

    private String bytesToHex(byte[] bytes) {
        StringBuilder result = new StringBuilder();
        for (byte b : bytes) {
            result.append(String.format("%02x", b));
        }
        return result.toString();
    }

    // Inner classes
    public static class UsageMetrics {
        private final long startTime;
        private long apiCalls;
        private long errors;

        public UsageMetrics() {
            this.startTime = Instant.now().getEpochSecond();
            this.apiCalls = 0;
            this.errors = 0;
        }

        public synchronized void incrementApiCalls() {
            apiCalls++;
        }

        public synchronized void incrementErrors() {
            errors++;
        }

        public long getStartTime() { return startTime; }
        public long getApiCalls() { return apiCalls; }
        public long getErrors() { return errors; }
    }

    public static class Violation {
        private final long timestamp;
        private final String sessionId;
        private final String violationType;
        private final String details;
        private final String licenseKeyPartial;

        public Violation(long timestamp, String sessionId, String violationType, String details, String licenseKeyPartial) {
            this.timestamp = timestamp;
            this.sessionId = sessionId;
            this.violationType = violationType;
            this.details = details;
            this.licenseKeyPartial = licenseKeyPartial;
        }

        @Override
        public String toString() {
            return String.format("Violation{timestamp=%d, sessionId='%s', type='%s', details='%s', licenseKeyPartial='%s'}",
                timestamp, sessionId, violationType, details, licenseKeyPartial);
        }
    }

    // Global protection instance
    private static volatile TuskProtection protectionInstance;
    private static final Object lock = new Object();

    public static TuskProtection initializeProtection(String licenseKey, String apiKey) {
        if (protectionInstance == null) {
            synchronized (lock) {
                if (protectionInstance == null) {
                    protectionInstance = new TuskProtection(licenseKey, apiKey);
                }
            }
        }
        return protectionInstance;
    }

    public static TuskProtection getProtection() {
        if (protectionInstance == null) {
            throw new RuntimeException("Protection not initialized. Call initializeProtection() first.");
        }
        return protectionInstance;
    }
} 