const { Goal8Implementation } = require("./src/goal8-implementation");

async function testGoalsG8() {
    console.log("🚀 Testing Goal 8 Implementation...");
    
    const goal8 = new Goal8Implementation({
        validation: { strictMode: false, autoFix: true },
        migration: { autoMigrate: true, backupBeforeMigration: true },
        logging: { level: "INFO", structured: true }
    });

    try {
        await goal8.initialize();
        console.log("✅ Goal 8 initialized successfully");
        
        // Test validation
        const validConfig = { host: "localhost", port: 8080 };
        const validation = await goal8.validateConfig(validConfig, "server-config");
        console.log("✅ Validation test passed:", validation.valid);
        
        // Test migration with proper config structure
        const oldConfig = { 
            server: { host: "localhost", port: 8080 },
            database: { type: "postgresql", host: "db.example.com", port: 5432, name: "testdb" }
        };
        const migration = await goal8.migrateConfig(oldConfig, "2.0.0");
        console.log("✅ Migration test passed:", migration.migrated);
        
        // Test logging
        goal8.log("INFO", "Test message", { test: true });
        console.log("✅ Logging test passed");
        
        console.log("🎉 Goal 8 implementation successful!");
        return true;
    } catch (error) {
        console.error("❌ Goal 8 test failed:", error.message);
        return false;
    }
}

testGoalsG8();
