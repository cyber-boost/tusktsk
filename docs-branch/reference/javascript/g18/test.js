const { Goal18Implementation } = require("./src/goal18-implementation");
const goal18 = new Goal18Implementation();
goal18.initialize();
goal18.createAudioSource("bgm", { type: "music", frequency: 440 });
goal18.playSource("bgm");
goal18.loadVisionModel("detector", { type: "object-detection" });
const detection = goal18.detectObjects("detector", "image_data");
goal18.loadLanguageModel("sentiment", { language: "en" });
const sentiment = goal18.analyzeSentiment("sentiment", "This is good news!");
console.log("✅ G18 Audio playing:", goal18.audio.sources.get("bgm").playing);
console.log("✅ G18 Objects detected:", detection.objects.length);
console.log("✅ G18 Sentiment:", sentiment.sentiment.label);
