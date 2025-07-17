# IoT Integration in C# with TuskLang

## Overview

IoT Integration involves connecting Internet of Things devices to applications for data collection, monitoring, and control. This guide covers how to implement IoT integration using C# and TuskLang configuration for building connected, intelligent systems.

## Table of Contents

- [IoT Integration Concepts](#iot-integration-concepts)
- [TuskLang IoT Configuration](#tusklang-iot-configuration)
- [Device Management](#device-management)
- [C# IoT Example](#c-iot-example)
- [Data Processing](#data-processing)
- [Real-time Monitoring](#real-time-monitoring)
- [Best Practices](#best-practices)

## IoT Integration Concepts

- **IoT Devices**: Connected sensors, actuators, and controllers
- **Device Management**: Registration, configuration, and monitoring
- **Data Collection**: Gathering sensor data and telemetry
- **Message Queuing**: Reliable communication between devices and applications
- **Edge Computing**: Processing data closer to devices
- **Device Twins**: Digital representations of physical devices

## TuskLang IoT Configuration

```ini
# iot.tsk
[iot]
enabled = @env("IOT_ENABLED", "true")
platform = @env("IOT_PLATFORM", "azure")
environment = @env("IOT_ENVIRONMENT", "production")

[azure_iot_hub]
connection_string = @env.secure("AZURE_IOT_HUB_CONNECTION_STRING")
hostname = @env("AZURE_IOT_HUB_HOSTNAME", "my-iot-hub.azure-devices.net")
shared_access_key = @env.secure("AZURE_IOT_HUB_SHARED_ACCESS_KEY")
shared_access_key_name = @env("AZURE_IOT_HUB_SHARED_ACCESS_KEY_NAME", "iothubowner")

[device_management]
device_registry_enabled = @env("DEVICE_REGISTRY_ENABLED", "true")
auto_approval_enabled = @env("AUTO_APPROVAL_ENABLED", "true")
max_devices_per_hub = @env("MAX_DEVICES_PER_HUB", "1000000")
device_provisioning_enabled = @env("DEVICE_PROVISIONING_ENABLED", "true")

[message_routing]
telemetry_route = @env("TELEMETRY_ROUTE", "messages/events")
command_route = @env("COMMAND_ROUTE", "messages/devicebound")
error_route = @env("ERROR_ROUTE", "messages/error")
dead_letter_route = @env("DEAD_LETTER_ROUTE", "messages/deadletter")

[data_processing]
stream_analytics_enabled = @env("STREAM_ANALYTICS_ENABLED", "true")
data_lake_enabled = @env("DATA_LAKE_ENABLED", "true")
cosmos_db_enabled = @env("COSMOS_DB_ENABLED", "true")
time_series_insights_enabled = @env("TIME_SERIES_INSIGHTS_ENABLED", "true")

[edge_computing]
edge_runtime_enabled = @env("EDGE_RUNTIME_ENABLED", "true")
edge_modules = @env("EDGE_MODULES", "temperature-sensor,humidity-sensor,actuator-controller")
edge_deployment_enabled = @env("EDGE_DEPLOYMENT_ENABLED", "true")

[monitoring]
device_health_monitoring = @env("DEVICE_HEALTH_MONITORING", "true")
telemetry_monitoring = @env("TELEMETRY_MONITORING", "true")
alerting_enabled = @env("ALERTING_ENABLED", "true")
metrics_collection = @env("METRICS_COLLECTION", "true")

[security]
device_authentication = @env("DEVICE_AUTHENTICATION", "sas")
certificate_management = @env("CERTIFICATE_MANAGEMENT", "true")
encryption_enabled = @env("ENCRYPTION_ENABLED", "true")
```

## Device Management

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TuskLang;

public interface IIoTDeviceService
{
    Task<Device> RegisterDeviceAsync(string deviceId, DeviceCapabilities capabilities);
    Task<Device> GetDeviceAsync(string deviceId);
    Task<bool> DeleteDeviceAsync(string deviceId);
    Task<List<Device>> GetAllDevicesAsync();
    Task<Twin> GetDeviceTwinAsync(string deviceId);
    Task UpdateDeviceTwinAsync(string deviceId, Twin twin);
    Task SendCommandAsync(string deviceId, string command, object payload);
}

public interface IDeviceTelemetryService
{
    Task SendTelemetryAsync(string deviceId, object telemetry);
    Task<List<TelemetryData>> GetTelemetryAsync(string deviceId, DateTime startTime, DateTime endTime);
    Task<TelemetryData> GetLatestTelemetryAsync(string deviceId);
}

public interface IDeviceProvisioningService
{
    Task<DeviceRegistration> ProvisionDeviceAsync(string deviceId, DeviceCapabilities capabilities);
    Task<bool> DecommissionDeviceAsync(string deviceId);
    Task<DeviceStatus> GetDeviceStatusAsync(string deviceId);
}

public class AzureIoTService : IIoTDeviceService
{
    private readonly RegistryManager _registryManager;
    private readonly ServiceClient _serviceClient;
    private readonly IConfiguration _config;
    private readonly ILogger<AzureIoTService> _logger;

    public AzureIoTService(IConfiguration config, ILogger<AzureIoTService> logger)
    {
        _config = config;
        _logger = logger;
        
        var connectionString = _config["azure_iot_hub:connection_string"];
        _registryManager = RegistryManager.CreateFromConnectionString(connectionString);
        _serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
    }

    public async Task<Device> RegisterDeviceAsync(string deviceId, DeviceCapabilities capabilities)
    {
        try
        {
            if (!bool.Parse(_config["device_management:device_registry_enabled"]))
            {
                throw new InvalidOperationException("Device registry is disabled");
            }

            var device = new Device(deviceId)
            {
                Capabilities = new DeviceCapabilities
                {
                    IotEdge = capabilities.IotEdge
                }
            };

            var registeredDevice = await _registryManager.AddDeviceAsync(device);
            
            _logger.LogInformation("Registered device {DeviceId} with capabilities: IotEdge={IotEdge}", 
                deviceId, capabilities.IotEdge);

            return registeredDevice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device {DeviceId}", deviceId);
            throw;
        }
    }

    public async Task<Device> GetDeviceAsync(string deviceId)
    {
        try
        {
            return await _registryManager.GetDeviceAsync(deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device {DeviceId}", deviceId);
            throw;
        }
    }

    public async Task<bool> DeleteDeviceAsync(string deviceId)
    {
        try
        {
            await _registryManager.RemoveDeviceAsync(deviceId);
            _logger.LogInformation("Deleted device {DeviceId}", deviceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<List<Device>> GetAllDevicesAsync()
    {
        try
        {
            var devices = new List<Device>();
            var query = _registryManager.CreateQuery("SELECT * FROM devices");
            
            while (query.HasMoreResults)
            {
                var page = await query.GetNextAsTwinAsync();
                foreach (var twin in page)
                {
                    var device = await _registryManager.GetDeviceAsync(twin.DeviceId);
                    devices.Add(device);
                }
            }

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all devices");
            throw;
        }
    }

    public async Task<Twin> GetDeviceTwinAsync(string deviceId)
    {
        try
        {
            return await _registryManager.GetTwinAsync(deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device twin for {DeviceId}", deviceId);
            throw;
        }
    }

    public async Task UpdateDeviceTwinAsync(string deviceId, Twin twin)
    {
        try
        {
            await _registryManager.UpdateTwinAsync(deviceId, twin, twin.ETag);
            _logger.LogInformation("Updated device twin for {DeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device twin for {DeviceId}", deviceId);
            throw;
        }
    }

    public async Task SendCommandAsync(string deviceId, string command, object payload)
    {
        try
        {
            var message = new Message(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payload))
            {
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _serviceClient.SendAsync(deviceId, message);
            
            _logger.LogInformation("Sent command {Command} to device {DeviceId}", command, deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending command {Command} to device {DeviceId}", command, deviceId);
            throw;
        }
    }
}

public class DeviceCapabilities
{
    public bool IotEdge { get; set; }
}

public class DeviceRegistration
{
    public string DeviceId { get; set; }
    public string ConnectionString { get; set; }
    public DeviceCapabilities Capabilities { get; set; }
    public DateTime RegisteredOn { get; set; }
}

public class DeviceStatus
{
    public string DeviceId { get; set; }
    public string Status { get; set; }
    public DateTime LastSeen { get; set; }
    public bool Connected { get; set; }
}
```

## C# IoT Example

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TuskLang;

[ApiController]
[Route("api/[controller]")]
public class IoTController : ControllerBase
{
    private readonly IIoTDeviceService _deviceService;
    private readonly IDeviceTelemetryService _telemetryService;
    private readonly IDeviceProvisioningService _provisioningService;
    private readonly IConfiguration _config;
    private readonly ILogger<IoTController> _logger;

    public IoTController(
        IIoTDeviceService deviceService,
        IDeviceTelemetryService telemetryService,
        IDeviceProvisioningService provisioningService,
        IConfiguration config,
        ILogger<IoTController> logger)
    {
        _deviceService = deviceService;
        _telemetryService = telemetryService;
        _provisioningService = provisioningService;
        _config = config;
        _logger = logger;
    }

    [HttpPost("devices/register")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        try
        {
            if (!bool.Parse(_config["iot:enabled"]))
                return BadRequest("IoT services are disabled");

            var device = await _deviceService.RegisterDeviceAsync(request.DeviceId, request.Capabilities);

            return Ok(new
            {
                DeviceId = device.Id,
                Status = device.Status.ToString(),
                Authentication = device.Authentication
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device {DeviceId}", request.DeviceId);
            return StatusCode(500, "Error registering device");
        }
    }

    [HttpGet("devices/{deviceId}")]
    public async Task<IActionResult> GetDevice(string deviceId)
    {
        try
        {
            if (!bool.Parse(_config["iot:enabled"]))
                return BadRequest("IoT services are disabled");

            var device = await _deviceService.GetDeviceAsync(deviceId);

            return Ok(new
            {
                DeviceId = device.Id,
                Status = device.Status.ToString(),
                Capabilities = device.Capabilities,
                Authentication = device.Authentication
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device {DeviceId}", deviceId);
            return StatusCode(500, "Error getting device");
        }
    }

    [HttpDelete("devices/{deviceId}")]
    public async Task<IActionResult> DeleteDevice(string deviceId)
    {
        try
        {
            if (!bool.Parse(_config["iot:enabled"]))
                return BadRequest("IoT services are disabled");

            var deleted = await _deviceService.DeleteDeviceAsync(deviceId);

            if (deleted)
            {
                return NoContent();
            }
            else
            {
                return NotFound($"Device {deviceId} not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device {DeviceId}", deviceId);
            return StatusCode(500, "Error deleting device");
        }
    }

    [HttpGet("devices")]
    public async Task<IActionResult> GetAllDevices()
    {
        try
        {
            if (!bool.Parse(_config["iot:enabled"]))
                return BadRequest("IoT services are disabled");

            var devices = await _deviceService.GetAllDevicesAsync();

            var deviceList = new List<object>();
            foreach (var device in devices)
            {
                deviceList.Add(new
                {
                    DeviceId = device.Id,
                    Status = device.Status.ToString(),
                    Capabilities = device.Capabilities
                });
            }

            return Ok(new { Devices = deviceList, Count = deviceList.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all devices");
            return StatusCode(500, "Error getting devices");
        }
    }

    [HttpGet("devices/{deviceId}/twin")]
    public async Task<IActionResult> GetDeviceTwin(string deviceId)
    {
        try
        {
            if (!bool.Parse(_config["iot:enabled"]))
                return BadRequest("IoT services are disabled");

            var twin = await _deviceService.GetDeviceTwinAsync(deviceId);

            return Ok(new
            {
                DeviceId = twin.DeviceId,
                ETag = twin.ETag,
                Version = twin.Version,
                Properties = new
                {
                    Desired = twin.Properties.Desired,
                    Reported = twin.Properties.Reported
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device twin for {DeviceId}", deviceId);
            return StatusCode(500, "Error getting device twin");
        }
    }

    [HttpPut("devices/{deviceId}/twin")]
    public async Task<IActionResult> UpdateDeviceTwin(string deviceId, [FromBody] UpdateTwinRequest request)
    {
        try
        {
            if (!bool.Parse(_config["iot:enabled"]))
                return BadRequest("IoT services are disabled");

            var twin = await _deviceService.GetDeviceTwinAsync(deviceId);
            
            if (request.DesiredProperties != null)
            {
                foreach (var property in request.DesiredProperties)
                {
                    twin.Properties.Desired[property.Key] = property.Value;
                }
            }

            await _deviceService.UpdateDeviceTwinAsync(deviceId, twin);

            return Ok(new { Message = "Device twin updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device twin for {DeviceId}", deviceId);
            return StatusCode(500, "Error updating device twin");
        }
    }

    [HttpPost("devices/{deviceId}/commands")]
    public async Task<IActionResult> SendCommand(string deviceId, [FromBody] SendCommandRequest request)
    {
        try
        {
            if (!bool.Parse(_config["iot:enabled"]))
                return BadRequest("IoT services are disabled");

            await _deviceService.SendCommandAsync(deviceId, request.Command, request.Payload);

            return Ok(new { Message = "Command sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending command to device {DeviceId}", deviceId);
            return StatusCode(500, "Error sending command");
        }
    }

    [HttpPost("devices/{deviceId}/telemetry")]
    public async Task<IActionResult> SendTelemetry(string deviceId, [FromBody] object telemetry)
    {
        try
        {
            if (!bool.Parse(_config["iot:enabled"]))
                return BadRequest("IoT services are disabled");

            await _telemetryService.SendTelemetryAsync(deviceId, telemetry);

            return Ok(new { Message = "Telemetry sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending telemetry for device {DeviceId}", deviceId);
            return StatusCode(500, "Error sending telemetry");
        }
    }

    [HttpGet("devices/{deviceId}/telemetry")]
    public async Task<IActionResult> GetTelemetry(string deviceId, [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
    {
        try
        {
            if (!bool.Parse(_config["iot:enabled"]))
                return BadRequest("IoT services are disabled");

            var telemetry = await _telemetryService.GetTelemetryAsync(deviceId, startTime, endTime);

            return Ok(new { Telemetry = telemetry, Count = telemetry.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting telemetry for device {DeviceId}", deviceId);
            return StatusCode(500, "Error getting telemetry");
        }
    }

    [HttpGet("devices/{deviceId}/telemetry/latest")]
    public async Task<IActionResult> GetLatestTelemetry(string deviceId)
    {
        try
        {
            if (!bool.Parse(_config["iot:enabled"]))
                return BadRequest("IoT services are disabled");

            var telemetry = await _telemetryService.GetLatestTelemetryAsync(deviceId);

            return Ok(telemetry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest telemetry for device {DeviceId}", deviceId);
            return StatusCode(500, "Error getting latest telemetry");
        }
    }

    [HttpPost("devices/provision")]
    public async Task<IActionResult> ProvisionDevice([FromBody] ProvisionDeviceRequest request)
    {
        try
        {
            if (!bool.Parse(_config["iot:enabled"]))
                return BadRequest("IoT services are disabled");

            if (!bool.Parse(_config["device_management:device_provisioning_enabled"]))
                return BadRequest("Device provisioning is disabled");

            var registration = await _provisioningService.ProvisionDeviceAsync(request.DeviceId, request.Capabilities);

            return Ok(new
            {
                DeviceId = registration.DeviceId,
                ConnectionString = registration.ConnectionString,
                RegisteredOn = registration.RegisteredOn
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning device {DeviceId}", request.DeviceId);
            return StatusCode(500, "Error provisioning device");
        }
    }

    [HttpGet("devices/{deviceId}/status")]
    public async Task<IActionResult> GetDeviceStatus(string deviceId)
    {
        try
        {
            if (!bool.Parse(_config["iot:enabled"]))
                return BadRequest("IoT services are disabled");

            var status = await _provisioningService.GetDeviceStatusAsync(deviceId);

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device status for {DeviceId}", deviceId);
            return StatusCode(500, "Error getting device status");
        }
    }
}

// Request/Response Models
public class RegisterDeviceRequest
{
    public string DeviceId { get; set; }
    public DeviceCapabilities Capabilities { get; set; }
}

public class UpdateTwinRequest
{
    public Dictionary<string, object> DesiredProperties { get; set; }
}

public class SendCommandRequest
{
    public string Command { get; set; }
    public object Payload { get; set; }
}

public class ProvisionDeviceRequest
{
    public string DeviceId { get; set; }
    public DeviceCapabilities Capabilities { get; set; }
}

public class TelemetryData
{
    public string DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public object Data { get; set; }
}
```

## Data Processing

```csharp
public class IoTDataProcessingService
{
    private readonly IConfiguration _config;
    private readonly ILogger<IoTDataProcessingService> _logger;

    public IoTDataProcessingService(IConfiguration config, ILogger<IoTDataProcessingService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task ProcessTelemetryAsync(TelemetryData telemetry)
    {
        try
        {
            if (!bool.Parse(_config["data_processing:stream_analytics_enabled"]))
                return;

            // Process telemetry data
            var processedData = await ProcessDataAsync(telemetry);

            // Store in data lake if enabled
            if (bool.Parse(_config["data_processing:data_lake_enabled"]))
            {
                await StoreInDataLakeAsync(processedData);
            }

            // Store in Cosmos DB if enabled
            if (bool.Parse(_config["data_processing:cosmos_db_enabled"]))
            {
                await StoreInCosmosDBAsync(processedData);
            }

            // Send to Time Series Insights if enabled
            if (bool.Parse(_config["data_processing:time_series_insights_enabled"]))
            {
                await SendToTimeSeriesInsightsAsync(processedData);
            }

            _logger.LogInformation("Processed telemetry for device {DeviceId}", telemetry.DeviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing telemetry for device {DeviceId}", telemetry.DeviceId);
            throw;
        }
    }

    private async Task<ProcessedTelemetryData> ProcessDataAsync(TelemetryData telemetry)
    {
        // Implementation for data processing logic
        return new ProcessedTelemetryData
        {
            DeviceId = telemetry.DeviceId,
            Timestamp = telemetry.Timestamp,
            ProcessedData = telemetry.Data,
            ProcessingTimestamp = DateTime.UtcNow
        };
    }

    private async Task StoreInDataLakeAsync(ProcessedTelemetryData data)
    {
        // Implementation for storing data in Azure Data Lake
        _logger.LogDebug("Stored data in Data Lake for device {DeviceId}", data.DeviceId);
    }

    private async Task StoreInCosmosDBAsync(ProcessedTelemetryData data)
    {
        // Implementation for storing data in Cosmos DB
        _logger.LogDebug("Stored data in Cosmos DB for device {DeviceId}", data.DeviceId);
    }

    private async Task SendToTimeSeriesInsightsAsync(ProcessedTelemetryData data)
    {
        // Implementation for sending data to Time Series Insights
        _logger.LogDebug("Sent data to Time Series Insights for device {DeviceId}", data.DeviceId);
    }
}

public class ProcessedTelemetryData
{
    public string DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public object ProcessedData { get; set; }
    public DateTime ProcessingTimestamp { get; set; }
}
```

## Real-time Monitoring

```csharp
public class IoTMonitoringService
{
    private readonly IConfiguration _config;
    private readonly ILogger<IoTMonitoringService> _logger;

    public IoTMonitoringService(IConfiguration config, ILogger<IoTMonitoringService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task MonitorDeviceHealthAsync(string deviceId)
    {
        try
        {
            if (!bool.Parse(_config["monitoring:device_health_monitoring"]))
                return;

            // Implementation for device health monitoring
            var healthStatus = await CheckDeviceHealthAsync(deviceId);

            if (healthStatus.Status == "Unhealthy")
            {
                await SendAlertAsync(deviceId, healthStatus);
            }

            _logger.LogInformation("Monitored device health for {DeviceId}: {Status}", 
                deviceId, healthStatus.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring device health for {DeviceId}", deviceId);
        }
    }

    public async Task MonitorTelemetryAsync(TelemetryData telemetry)
    {
        try
        {
            if (!bool.Parse(_config["monitoring:telemetry_monitoring"]))
                return;

            // Implementation for telemetry monitoring
            var anomalies = await DetectAnomaliesAsync(telemetry);

            if (anomalies.Any())
            {
                await SendAnomalyAlertAsync(telemetry.DeviceId, anomalies);
            }

            _logger.LogInformation("Monitored telemetry for device {DeviceId}", telemetry.DeviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring telemetry for device {DeviceId}", telemetry.DeviceId);
        }
    }

    private async Task<DeviceHealthStatus> CheckDeviceHealthAsync(string deviceId)
    {
        // Implementation for checking device health
        return new DeviceHealthStatus
        {
            DeviceId = deviceId,
            Status = "Healthy",
            LastSeen = DateTime.UtcNow,
            BatteryLevel = 85,
            SignalStrength = -45
        };
    }

    private async Task<List<Anomaly>> DetectAnomaliesAsync(TelemetryData telemetry)
    {
        // Implementation for anomaly detection
        return new List<Anomaly>();
    }

    private async Task SendAlertAsync(string deviceId, DeviceHealthStatus healthStatus)
    {
        if (bool.Parse(_config["monitoring:alerting_enabled"]))
        {
            // Implementation for sending alerts
            _logger.LogWarning("Device {DeviceId} is unhealthy: {Status}", deviceId, healthStatus.Status);
        }
    }

    private async Task SendAnomalyAlertAsync(string deviceId, List<Anomaly> anomalies)
    {
        if (bool.Parse(_config["monitoring:alerting_enabled"]))
        {
            // Implementation for sending anomaly alerts
            _logger.LogWarning("Anomalies detected for device {DeviceId}: {Count} anomalies", 
                deviceId, anomalies.Count);
        }
    }
}

public class DeviceHealthStatus
{
    public string DeviceId { get; set; }
    public string Status { get; set; }
    public DateTime LastSeen { get; set; }
    public int BatteryLevel { get; set; }
    public int SignalStrength { get; set; }
}

public class Anomaly
{
    public string Type { get; set; }
    public string Description { get; set; }
    public double Severity { get; set; }
    public DateTime DetectedAt { get; set; }
}
```

## Best Practices

1. **Implement proper device authentication and security**
2. **Use message queuing for reliable communication**
3. **Implement retry mechanisms for failed operations**
4. **Monitor device health and telemetry**
5. **Use edge computing for local processing**
6. **Implement proper error handling and logging**
7. **Use device twins for configuration management**

## Conclusion

IoT Integration with C# and TuskLang enables building connected, intelligent systems that can collect, process, and act on data from IoT devices. By leveraging TuskLang for configuration and IoT patterns, you can create systems that are scalable, reliable, and aligned with modern IoT practices. 