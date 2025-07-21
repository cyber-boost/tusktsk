const { Goal21Implementation } = require("./src/goal21-implementation");
const g21 = new Goal21Implementation();
g21.initialize();
g21.registerService("api-service", { port: 3000 });
const route = g21.routeRequest("api-service", { method: "GET", path: "/health" });
console.log("✅ G21 Service routed:", route.routed);
