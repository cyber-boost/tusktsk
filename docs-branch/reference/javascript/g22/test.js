const { Goal22Implementation } = require("./src/goal22-implementation");
const g22 = new Goal22Implementation();
g22.initialize();
g22.registerDevice("sensor-1", { type: "temperature" });
const telemetry = g22.collectTelemetry("sensor-1", { temperature: 23.5 });
console.log("✅ G22 IoT telemetry collected:", !!telemetry.timestamp);
