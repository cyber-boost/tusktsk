const { Goal23Implementation } = require("./src/goal23-implementation");
const g23 = new Goal23Implementation();
g23.initialize();
g23.createSecurityPolicy("policy-1", ["no-sql-injection", "rate-limit"]);
const threat = g23.detectThreat({ type: "suspicious-login", ip: "1.2.3.4" });
console.log("✅ G23 Threat detected:", threat.severity);
