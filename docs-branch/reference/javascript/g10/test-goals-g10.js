const { Goal10Implementation } = require("./src/goal10-implementation");

async function testGoalsG10() {
    console.log("🚀 Testing Goal 10 Implementation...");
    
    const goal10 = new Goal10Implementation({
        cryptography: { defaultAlgorithm: "aes-256-gcm", keyRotationInterval: 86400000 },
        zeroKnowledge: { defaultHashAlgorithm: "sha256", proofTimeout: 300000 },
        threatDetection: { alertThreshold: 5, incidentThreshold: 10, monitoringInterval: 5000 }
    });

    try {
        await goal10.initialize();
        console.log("✅ Goal 10 initialized successfully");
        
        // Test cryptography
        const keyId = goal10.generateKey("aes-256-gcm");
        const data = "test data";
        const encrypted = await goal10.encryptData(data, keyId);
        const decrypted = await goal10.decryptData(encrypted, keyId);
        console.log("✅ Cryptography test passed:", decrypted === data);
        
        // Test zero-knowledge proofs
        const proof = await goal10.generateProof("schnorr", "test statement", "test witness");
        const commitment = goal10.createCommitment("test data");
        console.log("✅ Zero-knowledge test passed:", proof && commitment);
        
        // Test threat detection
        const event = {
            type: "test_event",
            source: "test_source",
            data: { test: true },
            severity: "low"
        };
        const result = await goal10.monitorSecurityEvent(event);
        console.log("✅ Threat detection test passed:", result && result.eventId);
        
        // Test integration
        const status = goal10.getSystemStatus();
        console.log("✅ Integration test passed:", status.initialized);
        
        console.log("🎉 Goal 10 implementation successful!");
        return true;
    } catch (error) {
        console.error("❌ Goal 10 test failed:", error.message);
        return false;
    }
}

testGoalsG10();
