const { Goal17Implementation } = require("./src/goal17-implementation");
const goal17 = new Goal17Implementation();
goal17.initialize();
goal17.createScene("main", { background: "sky" });
goal17.createEntity("player", { position: { x: 0, y: 0 }, health: 100 });
goal17.startGame();
console.log("✅ G17 Game:", goal17.getSystemStatus().gameEngine.running);
