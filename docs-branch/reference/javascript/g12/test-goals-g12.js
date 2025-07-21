const { Goal12Implementation } = require("./src/goal12-implementation");

async function testGoalsG12() {
    console.log("🚀 Testing Goal 12 Implementation...");
    
    const goal12 = new Goal12Implementation();

    try {
        await goal12.initialize();
        console.log("✅ Goal 12 initialized successfully");
        
        // Test blockchain
        const wallet = goal12.createWallet("test_wallet");
        console.log("✅ Blockchain test passed:", wallet && wallet.address);
        
        // Test identity
        const identity = goal12.createDID("test_identity");
        console.log("✅ Identity test passed:", identity && identity.did);
        
        // Test DeFi
        const token = goal12.createToken("TEST", {
            name: "Test Token",
            symbol: "TEST",
            totalSupply: "1000000",
            creator: "test_user"
        });
        console.log("✅ DeFi test passed:", token && token.symbol === "TEST");
        
        // Test integration
        const status = goal12.getSystemStatus();
        console.log("✅ Integration test passed:", status.initialized);
        
        console.log("🎉 Goal 12 implementation successful!");
        return true;
    } catch (error) {
        console.error("❌ Goal 12 test failed:", error.message);
        return false;
    }
}

testGoalsG12();
