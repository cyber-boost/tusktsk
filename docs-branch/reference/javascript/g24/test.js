const { Goal24Implementation } = require("./src/goal24-implementation");
const g24 = new Goal24Implementation();
g24.initialize();
g24.deployFunction("hello-world", "return Hello World", { memory: 128 });
const execution = g24.executeFunction("hello-world", { name: "test" });
console.log("✅ G24 Serverless function executed:", execution.result);
