const { Goal18Implementation } = require("./src/goal18-implementation");
const goal18 = new Goal18Implementation();
goal18.initialize();
console.log("✅ G18 Complete:", goal18.getSystemStatus());
