using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TuskLang
{
    /// <summary>
    /// Advanced IoT integration system for TuskLang C# SDK
    /// Provides device management, sensor data processing, and IoT connectivity
    /// </summary>
    public class AdvancedIoTIntegration
    {
        private readonly Dictionary<string, IIoTDevice> _devices;
        private readonly List<ISensorProcessor> _sensorProcessors;
        private readonly List<IIoTProtocol> _protocols;
        private readonly IoTMetrics _metrics;
        private readonly DeviceManager _deviceManager;
        private readonly DataProcessor _dataProcessor;
        private readonly GatewayManager _gatewayManager;
        private readonly object _lock = new object();

        public AdvancedIoTIntegration()
        {
            _devices = new Dictionary<string, IIoTDevice>();
            _sensorProcessors = new List<ISensorProcessor>();
            _protocols = new List<IIoTProtocol>();
            _metrics = new IoTMetrics();
            _deviceManager = new DeviceManager();
            _dataProcessor = new DataProcessor();
            _gatewayManager = new GatewayManager();

            // Register default sensor processors
            RegisterSensorProcessor(new TemperatureProcessor());
            RegisterSensorProcessor(new HumidityProcessor());
            RegisterSensorProcessor(new MotionProcessor());
            
            // Register default IoT protocols
            RegisterProtocol(new MQTTProtocol());
            RegisterProtocol(new CoAPProtocol());
            RegisterProtocol(new HTTPProtocol());
        }

        /// <summary>
        /// Register an IoT device
        /// </summary>
        public void RegisterDevice(string deviceId, IIoTDevice device)
        {
            lock (_lock)
            {
                _devices[deviceId] = device;
            }
        }

        /// <summary>
        /// Connect to IoT device
        /// </summary>
        public async Task<DeviceConnectionResult> ConnectToDeviceAsync(
            string deviceId,
            ConnectionConfig config)
        {
            if (!_devices.TryGetValue(deviceId, out var device))
            {
                throw new InvalidOperationException($"Device '{deviceId}' not found");
            }

            var startTime = DateTime.UtcNow;

            try
            {
                var result = await device.ConnectAsync(config);
                
                _metrics.RecordDeviceConnection(deviceId, result.Success, DateTime.UtcNow - startTime);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordDeviceConnection(deviceId, false, DateTime.UtcNow - startTime);
                return new DeviceConnectionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Read sensor data
        /// </summary>
        public async Task<SensorDataResult> ReadSensorDataAsync(
            string deviceId,
            string sensorType)
        {
            if (!_devices.TryGetValue(deviceId, out var device))
            {
                throw new InvalidOperationException($"Device '{deviceId}' not found");
            }

            var startTime = DateTime.UtcNow;

            try
            {
                var sensorData = await device.ReadSensorAsync(sensorType);
                var processedData = await _dataProcessor.ProcessSensorDataAsync(sensorData, sensorType);

                _metrics.RecordSensorReading(deviceId, sensorType, true, DateTime.UtcNow - startTime);

                return new SensorDataResult
                {
                    Success = true,
                    DeviceId = deviceId,
                    SensorType = sensorType,
                    RawData = sensorData,
                    ProcessedData = processedData,
                    Timestamp = DateTime.UtcNow,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                _metrics.RecordSensorReading(deviceId, sensorType, false, DateTime.UtcNow - startTime);
                return new SensorDataResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Send command to device
        /// </summary>
        public async Task<DeviceCommandResult> SendCommandAsync(
            string deviceId,
            DeviceCommand command)
        {
            if (!_devices.TryGetValue(deviceId, out var device))
            {
                throw new InvalidOperationException($"Device '{deviceId}' not found");
            }

            return await _deviceManager.SendCommandAsync(device, command);
        }

        /// <summary>
        /// Process sensor data stream
        /// </summary>
        public async Task<DataStreamResult> ProcessDataStreamAsync(
            string deviceId,
            string sensorType,
            DataStreamConfig config)
        {
            if (!_devices.TryGetValue(deviceId, out var device))
            {
                throw new InvalidOperationException($"Device '{deviceId}' not found");
            }

            var startTime = DateTime.UtcNow;
            var dataPoints = new List<SensorDataPoint>();

            try
            {
                var stream = device.GetDataStreamAsync(sensorType, config);
                
                await foreach (var dataPoint in stream)
                {
                    var processedPoint = await _dataProcessor.ProcessDataPointAsync(dataPoint, sensorType);
                    dataPoints.Add(processedPoint);
                }

                _metrics.RecordDataStream(deviceId, sensorType, true, DateTime.UtcNow - startTime);

                return new DataStreamResult
                {
                    Success = true,
                    DeviceId = deviceId,
                    SensorType = sensorType,
                    DataPoints = dataPoints,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                _metrics.RecordDataStream(deviceId, sensorType, false, DateTime.UtcNow - startTime);
                return new DataStreamResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Configure IoT gateway
        /// </summary>
        public async Task<GatewayConfigResult> ConfigureGatewayAsync(
            string gatewayId,
            GatewayConfig config)
        {
            return await _gatewayManager.ConfigureGatewayAsync(gatewayId, config);
        }

        /// <summary>
        /// Register sensor processor
        /// </summary>
        public void RegisterSensorProcessor(ISensorProcessor processor)
        {
            lock (_lock)
            {
                _sensorProcessors.Add(processor);
            }
        }

        /// <summary>
        /// Register IoT protocol
        /// </summary>
        public void RegisterProtocol(IIoTProtocol protocol)
        {
            lock (_lock)
            {
                _protocols.Add(protocol);
            }
        }

        /// <summary>
        /// Get IoT metrics
        /// </summary>
        public IoTMetrics GetMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Get device IDs
        /// </summary>
        public List<string> GetDeviceIds()
        {
            lock (_lock)
            {
                return new List<string>(_devices.Keys);
            }
        }
    }

    public interface IIoTDevice
    {
        string DeviceId { get; }
        string DeviceType { get; }
        Task<DeviceConnectionResult> ConnectAsync(ConnectionConfig config);
        Task<SensorData> ReadSensorAsync(string sensorType);
        IAsyncEnumerable<SensorDataPoint> GetDataStreamAsync(string sensorType, DataStreamConfig config);
    }

    public interface ISensorProcessor
    {
        string Name { get; }
        string SensorType { get; }
        Task<ProcessedSensorData> ProcessAsync(SensorData rawData);
    }

    public interface IIoTProtocol
    {
        string Name { get; }
        Task<bool> SendDataAsync(string deviceId, byte[] data);
        Task<byte[]> ReceiveDataAsync(string deviceId);
    }

    public class TemperatureSensor : IIoTDevice
    {
        public string DeviceId { get; }
        public string DeviceType => "Temperature Sensor";

        public TemperatureSensor(string deviceId)
        {
            DeviceId = deviceId;
        }

        public async Task<DeviceConnectionResult> ConnectAsync(ConnectionConfig config)
        {
            await Task.Delay(300);

            return new DeviceConnectionResult
            {
                Success = true,
                DeviceId = DeviceId,
                ConnectionTime = DateTime.UtcNow
            };
        }

        public async Task<SensorData> ReadSensorAsync(string sensorType)
        {
            await Task.Delay(100);

            var random = new Random();
            var temperature = 20.0 + random.NextDouble() * 10.0;

            return new SensorData
            {
                SensorType = sensorType,
                Value = temperature,
                Unit = "°C",
                Timestamp = DateTime.UtcNow
            };
        }

        public async IAsyncEnumerable<SensorDataPoint> GetDataStreamAsync(string sensorType, DataStreamConfig config)
        {
            var random = new Random();
            var count = 0;

            while (count < config.MaxDataPoints)
            {
                var temperature = 20.0 + random.NextDouble() * 10.0;
                
                yield return new SensorDataPoint
                {
                    SensorType = sensorType,
                    Value = temperature,
                    Unit = "°C",
                    Timestamp = DateTime.UtcNow
                };

                await Task.Delay(config.IntervalMs);
                count++;
            }
        }
    }

    public class HumiditySensor : IIoTDevice
    {
        public string DeviceId { get; }
        public string DeviceType => "Humidity Sensor";

        public HumiditySensor(string deviceId)
        {
            DeviceId = deviceId;
        }

        public async Task<DeviceConnectionResult> ConnectAsync(ConnectionConfig config)
        {
            await Task.Delay(250);

            return new DeviceConnectionResult
            {
                Success = true,
                DeviceId = DeviceId,
                ConnectionTime = DateTime.UtcNow
            };
        }

        public async Task<SensorData> ReadSensorAsync(string sensorType)
        {
            await Task.Delay(80);

            var random = new Random();
            var humidity = 40.0 + random.NextDouble() * 30.0;

            return new SensorData
            {
                SensorType = sensorType,
                Value = humidity,
                Unit = "%",
                Timestamp = DateTime.UtcNow
            };
        }

        public async IAsyncEnumerable<SensorDataPoint> GetDataStreamAsync(string sensorType, DataStreamConfig config)
        {
            var random = new Random();
            var count = 0;

            while (count < config.MaxDataPoints)
            {
                var humidity = 40.0 + random.NextDouble() * 30.0;
                
                yield return new SensorDataPoint
                {
                    SensorType = sensorType,
                    Value = humidity,
                    Unit = "%",
                    Timestamp = DateTime.UtcNow
                };

                await Task.Delay(config.IntervalMs);
                count++;
            }
        }
    }

    public class MotionSensor : IIoTDevice
    {
        public string DeviceId { get; }
        public string DeviceType => "Motion Sensor";

        public MotionSensor(string deviceId)
        {
            DeviceId = deviceId;
        }

        public async Task<DeviceConnectionResult> ConnectAsync(ConnectionConfig config)
        {
            await Task.Delay(200);

            return new DeviceConnectionResult
            {
                Success = true,
                DeviceId = DeviceId,
                ConnectionTime = DateTime.UtcNow
            };
        }

        public async Task<SensorData> ReadSensorAsync(string sensorType)
        {
            await Task.Delay(50);

            var random = new Random();
            var motionDetected = random.Next(0, 2) == 1;

            return new SensorData
            {
                SensorType = sensorType,
                Value = motionDetected ? 1.0 : 0.0,
                Unit = "boolean",
                Timestamp = DateTime.UtcNow
            };
        }

        public async IAsyncEnumerable<SensorDataPoint> GetDataStreamAsync(string sensorType, DataStreamConfig config)
        {
            var random = new Random();
            var count = 0;

            while (count < config.MaxDataPoints)
            {
                var motionDetected = random.Next(0, 2) == 1;
                
                yield return new SensorDataPoint
                {
                    SensorType = sensorType,
                    Value = motionDetected ? 1.0 : 0.0,
                    Unit = "boolean",
                    Timestamp = DateTime.UtcNow
                };

                await Task.Delay(config.IntervalMs);
                count++;
            }
        }
    }

    public class TemperatureProcessor : ISensorProcessor
    {
        public string Name => "Temperature Processor";
        public string SensorType => "temperature";

        public async Task<ProcessedSensorData> ProcessAsync(SensorData rawData)
        {
            await Task.Delay(50);

            var temperature = (double)rawData.Value;
            var isHigh = temperature > 25.0;
            var isLow = temperature < 15.0;

            return new ProcessedSensorData
            {
                OriginalData = rawData,
                ProcessedValue = temperature,
                Alerts = new List<string>
                {
                    isHigh ? "High temperature detected" : null,
                    isLow ? "Low temperature detected" : null
                }.Where(a => a != null).ToList(),
                Metadata = new Dictionary<string, object>
                {
                    ["is_high"] = isHigh,
                    ["is_low"] = isLow,
                    ["normal_range"] = new { min = 15.0, max = 25.0 }
                }
            };
        }
    }

    public class HumidityProcessor : ISensorProcessor
    {
        public string Name => "Humidity Processor";
        public string SensorType => "humidity";

        public async Task<ProcessedSensorData> ProcessAsync(SensorData rawData)
        {
            await Task.Delay(40);

            var humidity = (double)rawData.Value;
            var isHigh = humidity > 60.0;
            var isLow = humidity < 30.0;

            return new ProcessedSensorData
            {
                OriginalData = rawData,
                ProcessedValue = humidity,
                Alerts = new List<string>
                {
                    isHigh ? "High humidity detected" : null,
                    isLow ? "Low humidity detected" : null
                }.Where(a => a != null).ToList(),
                Metadata = new Dictionary<string, object>
                {
                    ["is_high"] = isHigh,
                    ["is_low"] = isLow,
                    ["normal_range"] = new { min = 30.0, max = 60.0 }
                }
            };
        }
    }

    public class MotionProcessor : ISensorProcessor
    {
        public string Name => "Motion Processor";
        public string SensorType => "motion";

        public async Task<ProcessedSensorData> ProcessAsync(SensorData rawData)
        {
            await Task.Delay(30);

            var motionDetected = (double)rawData.Value > 0.5;

            return new ProcessedSensorData
            {
                OriginalData = rawData,
                ProcessedValue = motionDetected ? 1.0 : 0.0,
                Alerts = new List<string>
                {
                    motionDetected ? "Motion detected" : null
                }.Where(a => a != null).ToList(),
                Metadata = new Dictionary<string, object>
                {
                    ["motion_detected"] = motionDetected,
                    ["activity_level"] = motionDetected ? "high" : "low"
                }
            };
        }
    }

    public class MQTTProtocol : IIoTProtocol
    {
        public string Name => "MQTT";

        public async Task<bool> SendDataAsync(string deviceId, byte[] data)
        {
            await Task.Delay(100);
            return true;
        }

        public async Task<byte[]> ReceiveDataAsync(string deviceId)
        {
            await Task.Delay(80);
            return Encoding.UTF8.GetBytes("MQTT data received");
        }
    }

    public class CoAPProtocol : IIoTProtocol
    {
        public string Name => "CoAP";

        public async Task<bool> SendDataAsync(string deviceId, byte[] data)
        {
            await Task.Delay(120);
            return true;
        }

        public async Task<byte[]> ReceiveDataAsync(string deviceId)
        {
            await Task.Delay(90);
            return Encoding.UTF8.GetBytes("CoAP data received");
        }
    }

    public class HTTPProtocol : IIoTProtocol
    {
        public string Name => "HTTP";

        public async Task<bool> SendDataAsync(string deviceId, byte[] data)
        {
            await Task.Delay(150);
            return true;
        }

        public async Task<byte[]> ReceiveDataAsync(string deviceId)
        {
            await Task.Delay(110);
            return Encoding.UTF8.GetBytes("HTTP data received");
        }
    }

    public class DeviceManager
    {
        public async Task<DeviceCommandResult> SendCommandAsync(IIoTDevice device, DeviceCommand command)
        {
            await Task.Delay(200);

            return new DeviceCommandResult
            {
                Success = true,
                DeviceId = device.DeviceId,
                Command = command.Command,
                Response = "Command executed successfully"
            };
        }
    }

    public class DataProcessor
    {
        public async Task<ProcessedSensorData> ProcessSensorDataAsync(SensorData sensorData, string sensorType)
        {
            await Task.Delay(50);

            return new ProcessedSensorData
            {
                OriginalData = sensorData,
                ProcessedValue = sensorData.Value,
                Alerts = new List<string>(),
                Metadata = new Dictionary<string, object>
                {
                    ["sensor_type"] = sensorType,
                    ["processing_time"] = DateTime.UtcNow
                }
            };
        }

        public async Task<SensorDataPoint> ProcessDataPointAsync(SensorDataPoint dataPoint, string sensorType)
        {
            await Task.Delay(20);

            return new SensorDataPoint
            {
                SensorType = dataPoint.SensorType,
                Value = dataPoint.Value,
                Unit = dataPoint.Unit,
                Timestamp = dataPoint.Timestamp,
                Metadata = new Dictionary<string, object>
                {
                    ["processed"] = true,
                    ["sensor_type"] = sensorType
                }
            };
        }
    }

    public class GatewayManager
    {
        public async Task<GatewayConfigResult> ConfigureGatewayAsync(string gatewayId, GatewayConfig config)
        {
            await Task.Delay(500);

            return new GatewayConfigResult
            {
                Success = true,
                GatewayId = gatewayId,
                Configuration = config,
                Status = "Configured"
            };
        }
    }

    public class DeviceConnectionResult
    {
        public bool Success { get; set; }
        public string DeviceId { get; set; }
        public DateTime ConnectionTime { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SensorDataResult
    {
        public bool Success { get; set; }
        public string DeviceId { get; set; }
        public string SensorType { get; set; }
        public SensorData RawData { get; set; }
        public ProcessedSensorData ProcessedData { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class DeviceCommandResult
    {
        public bool Success { get; set; }
        public string DeviceId { get; set; }
        public string Command { get; set; }
        public string Response { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class DataStreamResult
    {
        public bool Success { get; set; }
        public string DeviceId { get; set; }
        public string SensorType { get; set; }
        public List<SensorDataPoint> DataPoints { get; set; } = new List<SensorDataPoint>();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class GatewayConfigResult
    {
        public bool Success { get; set; }
        public string GatewayId { get; set; }
        public GatewayConfig Configuration { get; set; }
        public string Status { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SensorData
    {
        public string SensorType { get; set; }
        public object Value { get; set; }
        public string Unit { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SensorDataPoint
    {
        public string SensorType { get; set; }
        public object Value { get; set; }
        public string Unit { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class ProcessedSensorData
    {
        public SensorData OriginalData { get; set; }
        public object ProcessedValue { get; set; }
        public List<string> Alerts { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class ConnectionConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class DeviceCommand
    {
        public string Command { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class DataStreamConfig
    {
        public int IntervalMs { get; set; } = 1000;
        public int MaxDataPoints { get; set; } = 100;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class GatewayConfig
    {
        public string GatewayType { get; set; }
        public string Protocol { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    }

    public class IoTMetrics
    {
        private readonly Dictionary<string, DeviceMetrics> _deviceMetrics = new Dictionary<string, DeviceMetrics>();
        private readonly Dictionary<string, SensorMetrics> _sensorMetrics = new Dictionary<string, SensorMetrics>();
        private readonly object _lock = new object();

        public void RecordDeviceConnection(string deviceId, bool success, TimeSpan executionTime)
        {
            lock (_lock)
            {
                if (!_deviceMetrics.ContainsKey(deviceId))
                {
                    _deviceMetrics[deviceId] = new DeviceMetrics();
                }

                var metrics = _deviceMetrics[deviceId];
                metrics.TotalConnections++;
                if (success) metrics.SuccessfulConnections++;
                metrics.TotalConnectionTime += executionTime;
            }
        }

        public void RecordSensorReading(string deviceId, string sensorType, bool success, TimeSpan executionTime)
        {
            var key = $"{deviceId}_{sensorType}";
            lock (_lock)
            {
                if (!_sensorMetrics.ContainsKey(key))
                {
                    _sensorMetrics[key] = new SensorMetrics();
                }

                var metrics = _sensorMetrics[key];
                metrics.TotalReadings++;
                if (success) metrics.SuccessfulReadings++;
                metrics.TotalReadingTime += executionTime;
            }
        }

        public void RecordDataStream(string deviceId, string sensorType, bool success, TimeSpan executionTime)
        {
            var key = $"{deviceId}_{sensorType}_stream";
            lock (_lock)
            {
                if (!_sensorMetrics.ContainsKey(key))
                {
                    _sensorMetrics[key] = new SensorMetrics();
                }

                var metrics = _sensorMetrics[key];
                metrics.TotalReadings++;
                if (success) metrics.SuccessfulReadings++;
                metrics.TotalReadingTime += executionTime;
            }
        }

        public Dictionary<string, DeviceMetrics> GetDeviceMetrics() => new Dictionary<string, DeviceMetrics>(_deviceMetrics);
        public Dictionary<string, SensorMetrics> GetSensorMetrics() => new Dictionary<string, SensorMetrics>(_sensorMetrics);
    }

    public class DeviceMetrics
    {
        public int TotalConnections { get; set; }
        public int SuccessfulConnections { get; set; }
        public TimeSpan TotalConnectionTime { get; set; }
    }

    public class SensorMetrics
    {
        public int TotalReadings { get; set; }
        public int SuccessfulReadings { get; set; }
        public TimeSpan TotalReadingTime { get; set; }
    }
} 