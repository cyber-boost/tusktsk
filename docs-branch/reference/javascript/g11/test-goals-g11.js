const { Goal11Implementation } = require("./src/goal11-implementation");

async function testGoal11() {
    const goal11 = new Goal11Implementation();
    await goal11.initialize();
    
    // Test
    goal11.loadModel("test", {}, "simple");
    const inf = await goal11.infer("test", "data");
    console.log(inf);
    
    goal11.createPipeline("pipe", [{type: "preprocess"}]);
    const res = await goal11.executePipeline("pipe", [1,2,3]);
    console.log(res);
    
    goal11.createAgent("agent", {});
    const dec = await goal11.makeDecision("agent", {});
    console.log(dec);
    
    console.log("Tests passed");
}

testGoal11();
