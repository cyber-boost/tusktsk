const { Goal15Implementation } = require("./src/goal15-implementation");
const goal15 = new Goal15Implementation();
goal15.initialize();
goal15.createIndex("docs", {});
goal15.indexDocument("docs", "doc1", "This is a test document about machine learning and AI");
goal15.indexDocument("docs", "doc2", "Another document discussing data science and analytics");
const results = goal15.searchDocuments("docs", "machine learning", { limit: 5 });
console.log("✅ G15 Search results:", results.results.length);
console.log("✅ G15 Status:", goal15.getSystemStatus());
