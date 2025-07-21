const { Goal20Implementation } = require("./src/goal20-implementation");
const g20 = new Goal20Implementation();
g20.initialize();
const network = g20.createNeuralNetwork("nn1", [2, 3, 1]);
const trainingData = [
    { input: [0, 0], target: [0] },
    { input: [0, 1], target: [1] },
    { input: [1, 0], target: [1] },
    { input: [1, 1], target: [0] }
];
const training = g20.trainNetwork("nn1", trainingData, 100, 0.5);
const prediction = g20.predictWithNetwork("nn1", [1, 0]);
console.log("✅ G20 Neural network trained, final error:", training.finalError.toFixed(4));
console.log("✅ G20 Prediction for [1,0]:", prediction[0].toFixed(3));
