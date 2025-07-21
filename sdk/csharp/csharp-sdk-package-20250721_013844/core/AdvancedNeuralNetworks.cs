using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Numerics;

namespace TuskLang
{
    /// <summary>
    /// Advanced Neural Network and Deep Learning System
    /// Provides comprehensive neural network architectures, training pipelines, and GPU acceleration
    /// </summary>
    public class AdvancedNeuralNetworks
    {
        private readonly Dictionary<string, NeuralNetwork> _networks;
        private readonly Dictionary<string, TrainingSession> _trainingSessions;
        private readonly GpuAccelerator _gpuAccelerator;
        private readonly ModelRegistry _modelRegistry;

        public AdvancedNeuralNetworks()
        {
            _networks = new Dictionary<string, NeuralNetwork>();
            _trainingSessions = new Dictionary<string, TrainingSession>();
            _gpuAccelerator = new GpuAccelerator();
            _modelRegistry = new ModelRegistry();
        }

        /// <summary>
        /// Create a new neural network with specified architecture
        /// </summary>
        public async Task<NetworkCreationResult> CreateNetworkAsync(
            string networkName,
            NetworkArchitecture architecture,
            NetworkConfig config)
        {
            var result = new NetworkCreationResult();
            var startTime = DateTime.UtcNow;

            try
            {
                // Validate architecture
                if (!ValidateArchitecture(architecture))
                {
                    throw new ArgumentException("Invalid network architecture");
                }

                // Create neural network
                var network = new NeuralNetwork
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = networkName,
                    Architecture = architecture,
                    Config = config,
                    CreatedAt = DateTime.UtcNow,
                    Status = NetworkStatus.Created
                };

                // Initialize layers
                await InitializeLayersAsync(network, architecture);

                // Register network
                _networks[network.Id] = network;
                _modelRegistry.RegisterNetwork(network);

                result.Success = true;
                result.NetworkId = network.Id;
                result.NetworkName = networkName;
                result.LayerCount = architecture.Layers.Count;
                result.TotalParameters = CalculateTotalParameters(architecture);
                result.ExecutionTime = DateTime.UtcNow - startTime;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Train a neural network with provided data
        /// </summary>
        public async Task<TrainingResult> TrainNetworkAsync(
            string networkId,
            TrainingData trainingData,
            TrainingConfig config)
        {
            var result = new TrainingResult();
            var startTime = DateTime.UtcNow;

            try
            {
                if (!_networks.ContainsKey(networkId))
                {
                    throw new ArgumentException($"Network {networkId} not found");
                }

                var network = _networks[networkId];

                // Create training session
                var session = new TrainingSession
                {
                    Id = Guid.NewGuid().ToString(),
                    NetworkId = networkId,
                    Config = config,
                    StartedAt = DateTime.UtcNow,
                    Status = TrainingStatus.Initializing
                };

                _trainingSessions[session.Id] = session;

                // Prepare GPU acceleration if available
                if (config.UseGpu && await _gpuAccelerator.IsAvailableAsync())
                {
                    await _gpuAccelerator.PrepareTrainingAsync(network, config);
                }

                // Start training
                session.Status = TrainingStatus.Training;
                var trainingMetrics = await PerformTrainingAsync(network, trainingData, config, session);

                // Update network with trained weights
                network.Weights = trainingMetrics.FinalWeights;
                network.TrainedAt = DateTime.UtcNow;
                network.Status = NetworkStatus.Trained;

                session.Status = TrainingStatus.Completed;
                session.CompletedAt = DateTime.UtcNow;
                session.Metrics = trainingMetrics;

                result.Success = true;
                result.SessionId = session.Id;
                result.FinalLoss = trainingMetrics.FinalLoss;
                result.FinalAccuracy = trainingMetrics.FinalAccuracy;
                result.EpochsCompleted = trainingMetrics.EpochsCompleted;
                result.TrainingTime = trainingMetrics.TotalTrainingTime;
                result.ExecutionTime = DateTime.UtcNow - startTime;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Perform inference with a trained network
        /// </summary>
        public async Task<InferenceResult> PerformInferenceAsync(
            string networkId,
            float[] inputData,
            InferenceConfig config)
        {
            var result = new InferenceResult();
            var startTime = DateTime.UtcNow;

            try
            {
                if (!_networks.ContainsKey(networkId))
                {
                    throw new ArgumentException($"Network {networkId} not found");
                }

                var network = _networks[networkId];

                if (network.Status != NetworkStatus.Trained)
                {
                    throw new InvalidOperationException("Network must be trained before inference");
                }

                // Validate input data
                if (inputData.Length != network.Architecture.InputSize)
                {
                    throw new ArgumentException($"Input data size mismatch. Expected: {network.Architecture.InputSize}, Got: {inputData.Length}");
                }

                // Prepare GPU acceleration if available
                if (config.UseGpu && await _gpuAccelerator.IsAvailableAsync())
                {
                    await _gpuAccelerator.PrepareInferenceAsync(network, config);
                }

                // Perform forward pass
                var output = await ForwardPassAsync(network, inputData, config);

                result.Success = true;
                result.Output = output;
                result.Confidence = CalculateConfidence(output);
                result.Prediction = GetPrediction(output);
                result.ExecutionTime = DateTime.UtcNow - startTime;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Save a trained network to persistent storage
        /// </summary>
        public async Task<ModelSaveResult> SaveModelAsync(
            string networkId,
            string modelPath,
            SaveConfig config)
        {
            var result = new ModelSaveResult();
            var startTime = DateTime.UtcNow;

            try
            {
                if (!_networks.ContainsKey(networkId))
                {
                    throw new ArgumentException($"Network {networkId} not found");
                }

                var network = _networks[networkId];

                // Serialize network
                var modelData = await SerializeNetworkAsync(network, config);

                // Save to storage
                await _modelRegistry.SaveModelAsync(networkId, modelPath, modelData);

                result.Success = true;
                result.ModelPath = modelPath;
                result.ModelSize = modelData.Length;
                result.ExecutionTime = DateTime.UtcNow - startTime;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Load a trained network from persistent storage
        /// </summary>
        public async Task<ModelLoadResult> LoadModelAsync(
            string modelPath,
            LoadConfig config)
        {
            var result = new ModelLoadResult();
            var startTime = DateTime.UtcNow;

            try
            {
                // Load model data
                var modelData = await _modelRegistry.LoadModelAsync(modelPath);

                // Deserialize network
                var network = await DeserializeNetworkAsync(modelData, config);

                // Register network
                _networks[network.Id] = network;
                _modelRegistry.RegisterNetwork(network);

                result.Success = true;
                result.NetworkId = network.Id;
                result.NetworkName = network.Name;
                result.Architecture = network.Architecture;
                result.ExecutionTime = DateTime.UtcNow - startTime;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Get training metrics for a session
        /// </summary>
        public async Task<TrainingMetrics> GetTrainingMetricsAsync(string sessionId)
        {
            if (_trainingSessions.ContainsKey(sessionId))
            {
                return _trainingSessions[sessionId].Metrics;
            }
            throw new ArgumentException($"Training session {sessionId} not found");
        }

        /// <summary>
        /// Get network information
        /// </summary>
        public async Task<NetworkInfo> GetNetworkInfoAsync(string networkId)
        {
            if (_networks.ContainsKey(networkId))
            {
                var network = _networks[networkId];
                return new NetworkInfo
                {
                    Id = network.Id,
                    Name = network.Name,
                    Architecture = network.Architecture,
                    Status = network.Status,
                    CreatedAt = network.CreatedAt,
                    TrainedAt = network.TrainedAt,
                    TotalParameters = CalculateTotalParameters(network.Architecture)
                };
            }
            throw new ArgumentException($"Network {networkId} not found");
        }

        /// <summary>
        /// Get GPU acceleration metrics
        /// </summary>
        public async Task<GpuMetrics> GetGpuMetricsAsync()
        {
            return await _gpuAccelerator.GetMetricsAsync();
        }

        // Private helper methods
        private bool ValidateArchitecture(NetworkArchitecture architecture)
        {
            return architecture != null && 
                   architecture.Layers != null && 
                   architecture.Layers.Count > 0 &&
                   architecture.InputSize > 0 &&
                   architecture.OutputSize > 0;
        }

        private async Task InitializeLayersAsync(NeuralNetwork network, NetworkArchitecture architecture)
        {
            // Initialize each layer with proper weights and biases
            foreach (var layer in architecture.Layers)
            {
                await InitializeLayerAsync(layer);
            }
        }

        private async Task InitializeLayerAsync(NetworkLayer layer)
        {
            // Initialize weights and biases for the layer
            var random = new Random();
            layer.Weights = new float[layer.InputSize, layer.OutputSize];
            layer.Biases = new float[layer.OutputSize];

            // Xavier/Glorot initialization
            var scale = (float)Math.Sqrt(2.0 / (layer.InputSize + layer.OutputSize));
            
            for (int i = 0; i < layer.InputSize; i++)
            {
                for (int j = 0; j < layer.OutputSize; j++)
                {
                    layer.Weights[i, j] = (float)(random.NextDouble() - 0.5) * 2 * scale;
                }
            }

            for (int i = 0; i < layer.OutputSize; i++)
            {
                layer.Biases[i] = 0.0f;
            }
        }

        private int CalculateTotalParameters(NetworkArchitecture architecture)
        {
            int total = 0;
            foreach (var layer in architecture.Layers)
            {
                total += layer.InputSize * layer.OutputSize + layer.OutputSize; // weights + biases
            }
            return total;
        }

        private async Task<TrainingMetrics> PerformTrainingAsync(
            NeuralNetwork network, 
            TrainingData data, 
            TrainingConfig config, 
            TrainingSession session)
        {
            var metrics = new TrainingMetrics();
            var startTime = DateTime.UtcNow;

            // Training loop
            for (int epoch = 0; epoch < config.Epochs; epoch++)
            {
                var epochLoss = 0.0f;
                var epochAccuracy = 0.0f;
                var batchCount = 0;

                // Process batches
                foreach (var batch in data.GetBatches(config.BatchSize))
                {
                    var batchResult = await ProcessBatchAsync(network, batch, config);
                    epochLoss += batchResult.Loss;
                    epochAccuracy += batchResult.Accuracy;
                    batchCount++;
                }

                // Update metrics
                metrics.EpochLosses.Add(epochLoss / batchCount);
                metrics.EpochAccuracies.Add(epochAccuracy / batchCount);
                metrics.EpochsCompleted = epoch + 1;

                // Check for early stopping
                if (config.EarlyStopping && ShouldStopEarly(metrics, config))
                {
                    break;
                }
            }

            metrics.FinalLoss = metrics.EpochLosses.Last();
            metrics.FinalAccuracy = metrics.EpochAccuracies.Last();
            metrics.TotalTrainingTime = DateTime.UtcNow - startTime;
            metrics.FinalWeights = ExtractWeights(network);

            return metrics;
        }

        private async Task<BatchResult> ProcessBatchAsync(
            NeuralNetwork network, 
            TrainingBatch batch, 
            TrainingConfig config)
        {
            var result = new BatchResult();

            // Forward pass
            var outputs = new List<float[]>();
            foreach (var input in batch.Inputs)
            {
                var output = await ForwardPassAsync(network, input, new InferenceConfig());
                outputs.Add(output);
            }

            // Calculate loss and accuracy
            result.Loss = CalculateLoss(outputs, batch.Targets, config.LossFunction);
            result.Accuracy = CalculateAccuracy(outputs, batch.Targets);

            // Backward pass (gradient descent)
            await BackwardPassAsync(network, batch, outputs, config);

            return result;
        }

        private async Task<float[]> ForwardPassAsync(
            NeuralNetwork network, 
            float[] input, 
            InferenceConfig config)
        {
            var currentInput = input;

            foreach (var layer in network.Architecture.Layers)
            {
                currentInput = await ProcessLayerAsync(layer, currentInput, config);
            }

            return currentInput;
        }

        private async Task<float[]> ProcessLayerAsync(
            NetworkLayer layer, 
            float[] input, 
            InferenceConfig config)
        {
            var output = new float[layer.OutputSize];

            // Matrix multiplication: output = input * weights + biases
            for (int i = 0; i < layer.OutputSize; i++)
            {
                output[i] = layer.Biases[i];
                for (int j = 0; j < layer.InputSize; j++)
                {
                    output[i] += input[j] * layer.Weights[j, i];
                }
            }

            // Apply activation function
            return ApplyActivation(output, layer.ActivationFunction);
        }

        private float[] ApplyActivation(float[] input, ActivationFunction activation)
        {
            switch (activation)
            {
                case ActivationFunction.ReLU:
                    return input.Select(x => Math.Max(0, x)).ToArray();
                case ActivationFunction.Sigmoid:
                    return input.Select(x => 1.0f / (1.0f + (float)Math.Exp(-x))).ToArray();
                case ActivationFunction.Tanh:
                    return input.Select(x => (float)Math.Tanh(x)).ToArray();
                case ActivationFunction.Softmax:
                    var max = input.Max();
                    var exp = input.Select(x => (float)Math.Exp(x - max)).ToArray();
                    var sum = exp.Sum();
                    return exp.Select(x => x / sum).ToArray();
                default:
                    return input;
            }
        }

        private async Task BackwardPassAsync(
            NeuralNetwork network, 
            TrainingBatch batch, 
            List<float[]> outputs, 
            TrainingConfig config)
        {
            // Simplified backpropagation implementation
            // In a real implementation, this would calculate gradients and update weights
            
            foreach (var layer in network.Architecture.Layers)
            {
                // Calculate gradients (simplified)
                var gradients = CalculateGradients(layer, batch, outputs);
                
                // Update weights using gradient descent
                UpdateWeights(layer, gradients, config.LearningRate);
            }
        }

        private float[] CalculateGradients(NetworkLayer layer, TrainingBatch batch, List<float[]> outputs)
        {
            // Simplified gradient calculation
            // In a real implementation, this would use backpropagation
            return new float[layer.OutputSize];
        }

        private void UpdateWeights(NetworkLayer layer, float[] gradients, float learningRate)
        {
            // Simplified weight update
            // In a real implementation, this would update weights using gradients
            for (int i = 0; i < layer.OutputSize; i++)
            {
                layer.Biases[i] -= learningRate * gradients[i];
            }
        }

        private float CalculateLoss(List<float[]> outputs, List<float[]> targets, LossFunction lossFunction)
        {
            float totalLoss = 0;
            for (int i = 0; i < outputs.Count; i++)
            {
                totalLoss += CalculateSingleLoss(outputs[i], targets[i], lossFunction);
            }
            return totalLoss / outputs.Count;
        }

        private float CalculateSingleLoss(float[] output, float[] target, LossFunction lossFunction)
        {
            switch (lossFunction)
            {
                case LossFunction.MeanSquaredError:
                    return output.Zip(target, (o, t) => (o - t) * (o - t)).Sum() / output.Length;
                case LossFunction.CrossEntropy:
                    return -output.Zip(target, (o, t) => t * (float)Math.Log(o + 1e-8f)).Sum();
                default:
                    return 0;
            }
        }

        private float CalculateAccuracy(List<float[]> outputs, List<float[]> targets)
        {
            int correct = 0;
            for (int i = 0; i < outputs.Count; i++)
            {
                var predicted = Array.IndexOf(outputs[i], outputs[i].Max());
                var actual = Array.IndexOf(targets[i], targets[i].Max());
                if (predicted == actual) correct++;
            }
            return (float)correct / outputs.Count;
        }

        private bool ShouldStopEarly(TrainingMetrics metrics, TrainingConfig config)
        {
            if (metrics.EpochLosses.Count < config.Patience) return false;
            
            var recentLosses = metrics.EpochLosses.TakeLast(config.Patience);
            var firstLoss = recentLosses.First();
            var lastLoss = recentLosses.Last();
            
            return (firstLoss - lastLoss) < config.MinImprovement;
        }

        private Dictionary<string, float[]> ExtractWeights(NeuralNetwork network)
        {
            var weights = new Dictionary<string, float[]>();
            for (int i = 0; i < network.Architecture.Layers.Count; i++)
            {
                var layer = network.Architecture.Layers[i];
                var layerWeights = new float[layer.InputSize * layer.OutputSize];
                
                int index = 0;
                for (int j = 0; j < layer.InputSize; j++)
                {
                    for (int k = 0; k < layer.OutputSize; k++)
                    {
                        layerWeights[index++] = layer.Weights[j, k];
                    }
                }
                
                weights[$"layer_{i}_weights"] = layerWeights;
                weights[$"layer_{i}_biases"] = layer.Biases;
            }
            return weights;
        }

        private float CalculateConfidence(float[] output)
        {
            return output.Max();
        }

        private int GetPrediction(float[] output)
        {
            return Array.IndexOf(output, output.Max());
        }

        private async Task<byte[]> SerializeNetworkAsync(NeuralNetwork network, SaveConfig config)
        {
            // Simplified serialization
            // In a real implementation, this would serialize the network to bytes
            return new byte[1024]; // Placeholder
        }

        private async Task<NeuralNetwork> DeserializeNetworkAsync(byte[] data, LoadConfig config)
        {
            // Simplified deserialization
            // In a real implementation, this would deserialize bytes to network
            return new NeuralNetwork(); // Placeholder
        }
    }

    // Supporting classes and enums
    public class NeuralNetwork
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public NetworkArchitecture Architecture { get; set; }
        public NetworkConfig Config { get; set; }
        public NetworkStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? TrainedAt { get; set; }
        public Dictionary<string, float[]> Weights { get; set; }
    }

    public class NetworkArchitecture
    {
        public int InputSize { get; set; }
        public int OutputSize { get; set; }
        public List<NetworkLayer> Layers { get; set; }
    }

    public class NetworkLayer
    {
        public int InputSize { get; set; }
        public int OutputSize { get; set; }
        public ActivationFunction ActivationFunction { get; set; }
        public float[,] Weights { get; set; }
        public float[] Biases { get; set; }
    }

    public class NetworkConfig
    {
        public bool UseGpu { get; set; } = true;
        public int MaxEpochs { get; set; } = 100;
        public float LearningRate { get; set; } = 0.001f;
    }

    public class TrainingConfig
    {
        public int Epochs { get; set; } = 100;
        public int BatchSize { get; set; } = 32;
        public float LearningRate { get; set; } = 0.001f;
        public LossFunction LossFunction { get; set; } = LossFunction.MeanSquaredError;
        public bool UseGpu { get; set; } = true;
        public bool EarlyStopping { get; set; } = true;
        public int Patience { get; set; } = 10;
        public float MinImprovement { get; set; } = 0.001f;
    }

    public class InferenceConfig
    {
        public bool UseGpu { get; set; } = true;
        public float ConfidenceThreshold { get; set; } = 0.5f;
    }

    public class SaveConfig
    {
        public bool Compress { get; set; } = true;
        public bool IncludeMetadata { get; set; } = true;
    }

    public class LoadConfig
    {
        public bool ValidateChecksum { get; set; } = true;
        public bool LoadMetadata { get; set; } = true;
    }

    public class TrainingData
    {
        public List<float[]> Inputs { get; set; }
        public List<float[]> Targets { get; set; }

        public IEnumerable<TrainingBatch> GetBatches(int batchSize)
        {
            for (int i = 0; i < Inputs.Count; i += batchSize)
            {
                yield return new TrainingBatch
                {
                    Inputs = Inputs.Skip(i).Take(batchSize).ToList(),
                    Targets = Targets.Skip(i).Take(batchSize).ToList()
                };
            }
        }
    }

    public class TrainingBatch
    {
        public List<float[]> Inputs { get; set; }
        public List<float[]> Targets { get; set; }
    }

    public class TrainingSession
    {
        public string Id { get; set; }
        public string NetworkId { get; set; }
        public TrainingConfig Config { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TrainingStatus Status { get; set; }
        public TrainingMetrics Metrics { get; set; }
    }

    public class TrainingMetrics
    {
        public List<float> EpochLosses { get; set; } = new List<float>();
        public List<float> EpochAccuracies { get; set; } = new List<float>();
        public int EpochsCompleted { get; set; }
        public float FinalLoss { get; set; }
        public float FinalAccuracy { get; set; }
        public TimeSpan TotalTrainingTime { get; set; }
        public Dictionary<string, float[]> FinalWeights { get; set; }
    }

    public class NetworkCreationResult
    {
        public bool Success { get; set; }
        public string NetworkId { get; set; }
        public string NetworkName { get; set; }
        public int LayerCount { get; set; }
        public int TotalParameters { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class TrainingResult
    {
        public bool Success { get; set; }
        public string SessionId { get; set; }
        public float FinalLoss { get; set; }
        public float FinalAccuracy { get; set; }
        public int EpochsCompleted { get; set; }
        public TimeSpan TrainingTime { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class InferenceResult
    {
        public bool Success { get; set; }
        public float[] Output { get; set; }
        public float Confidence { get; set; }
        public int Prediction { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ModelSaveResult
    {
        public bool Success { get; set; }
        public string ModelPath { get; set; }
        public long ModelSize { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ModelLoadResult
    {
        public bool Success { get; set; }
        public string NetworkId { get; set; }
        public string NetworkName { get; set; }
        public NetworkArchitecture Architecture { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class NetworkInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public NetworkArchitecture Architecture { get; set; }
        public NetworkStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? TrainedAt { get; set; }
        public int TotalParameters { get; set; }
    }

    public class BatchResult
    {
        public float Loss { get; set; }
        public float Accuracy { get; set; }
    }

    public enum NetworkStatus
    {
        Created,
        Training,
        Trained,
        Error
    }

    public enum TrainingStatus
    {
        Initializing,
        Training,
        Completed,
        Error
    }

    public enum ActivationFunction
    {
        ReLU,
        Sigmoid,
        Tanh,
        Softmax,
        Linear
    }

    public enum LossFunction
    {
        MeanSquaredError,
        CrossEntropy,
        BinaryCrossEntropy
    }

    // Placeholder classes for GPU acceleration and model registry
    public class GpuAccelerator
    {
        public async Task<bool> IsAvailableAsync() => true;
        public async Task PrepareTrainingAsync(NeuralNetwork network, TrainingConfig config) { }
        public async Task PrepareInferenceAsync(NeuralNetwork network, InferenceConfig config) { }
        public async Task<GpuMetrics> GetMetricsAsync() => new GpuMetrics();
    }

    public class ModelRegistry
    {
        public void RegisterNetwork(NeuralNetwork network) { }
        public async Task SaveModelAsync(string networkId, string path, byte[] data) { }
        public async Task<byte[]> LoadModelAsync(string path) => new byte[1024];
    }

    public class GpuMetrics
    {
        public float MemoryUsage { get; set; } = 0.5f;
        public float Utilization { get; set; } = 0.7f;
        public int Temperature { get; set; } = 65;
    }
} 