# ☁️ Cloud-Native Patterns with TuskLang Java

**"We don't bow to any king" - Cloud Edition**

TuskLang Java brings cloud-native capabilities to your applications with seamless Kubernetes integration, container orchestration, and multi-cloud deployment patterns. Build applications that scale automatically and run anywhere in the cloud.

## 🎯 Cloud-Native Architecture Overview

### Kubernetes Configuration
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.cloud.kubernetes.config.EnableKubernetesConfig;

@SpringBootApplication
@EnableKubernetesConfig
public class CloudNativeApplication {
    
    @Bean
    public TuskConfig tuskConfig() {
        TuskLang parser = new TuskLang();
        return parser.parseFile("cloud-native.tsk", TuskConfig.class);
    }
    
    public static void main(String[] args) {
        SpringApplication.run(CloudNativeApplication.class, args);
    }
}

// Cloud-native configuration
@TuskConfig
public class CloudNativeConfig {
    private String applicationName;
    private String version;
    private KubernetesConfig kubernetes;
    private DockerConfig docker;
    private CloudProviderConfig cloudProvider;
    private AutoScalingConfig autoScaling;
    private MonitoringConfig monitoring;
    
    // Getters and setters
    public String getApplicationName() { return applicationName; }
    public void setApplicationName(String applicationName) { this.applicationName = applicationName; }
    
    public String getVersion() { return version; }
    public void setVersion(String version) { this.version = version; }
    
    public KubernetesConfig getKubernetes() { return kubernetes; }
    public void setKubernetes(KubernetesConfig kubernetes) { this.kubernetes = kubernetes; }
    
    public DockerConfig getDocker() { return docker; }
    public void setDocker(DockerConfig docker) { this.docker = docker; }
    
    public CloudProviderConfig getCloudProvider() { return cloudProvider; }
    public void setCloudProvider(CloudProviderConfig cloudProvider) { this.cloudProvider = cloudProvider; }
    
    public AutoScalingConfig getAutoScaling() { return autoScaling; }
    public void setAutoScaling(AutoScalingConfig autoScaling) { this.autoScaling = autoScaling; }
    
    public MonitoringConfig getMonitoring() { return monitoring; }
    public void setMonitoring(MonitoringConfig monitoring) { this.monitoring = monitoring; }
}

@TuskConfig
public class KubernetesConfig {
    private String namespace;
    private String serviceAccount;
    private ResourceConfig resources;
    private ProbeConfig livenessProbe;
    private ProbeConfig readinessProbe;
    private Map<String, String> labels;
    private Map<String, String> annotations;
    
    // Getters and setters
    public String getNamespace() { return namespace; }
    public void setNamespace(String namespace) { this.namespace = namespace; }
    
    public String getServiceAccount() { return serviceAccount; }
    public void setServiceAccount(String serviceAccount) { this.serviceAccount = serviceAccount; }
    
    public ResourceConfig getResources() { return resources; }
    public void setResources(ResourceConfig resources) { this.resources = resources; }
    
    public ProbeConfig getLivenessProbe() { return livenessProbe; }
    public void setLivenessProbe(ProbeConfig livenessProbe) { this.livenessProbe = livenessProbe; }
    
    public ProbeConfig getReadinessProbe() { return readinessProbe; }
    public void setReadinessProbe(ProbeConfig readinessProbe) { this.readinessProbe = readinessProbe; }
    
    public Map<String, String> getLabels() { return labels; }
    public void setLabels(Map<String, String> labels) { this.labels = labels; }
    
    public Map<String, String> getAnnotations() { return annotations; }
    public void setAnnotations(Map<String, String> annotations) { this.annotations = annotations; }
}

@TuskConfig
public class ResourceConfig {
    private String cpuRequest;
    private String cpuLimit;
    private String memoryRequest;
    private String memoryLimit;
    private String storageRequest;
    private String storageLimit;
    
    // Getters and setters
    public String getCpuRequest() { return cpuRequest; }
    public void setCpuRequest(String cpuRequest) { this.cpuRequest = cpuRequest; }
    
    public String getCpuLimit() { return cpuLimit; }
    public void setCpuLimit(String cpuLimit) { this.cpuLimit = cpuLimit; }
    
    public String getMemoryRequest() { return memoryRequest; }
    public void setMemoryRequest(String memoryRequest) { this.memoryRequest = memoryRequest; }
    
    public String getMemoryLimit() { return memoryLimit; }
    public void setMemoryLimit(String memoryLimit) { this.memoryLimit = memoryLimit; }
    
    public String getStorageRequest() { return storageRequest; }
    public void setStorageRequest(String storageRequest) { this.storageRequest = storageRequest; }
    
    public String getStorageLimit() { return storageLimit; }
    public void setStorageLimit(String storageLimit) { this.storageLimit = storageLimit; }
}

@TuskConfig
public class ProbeConfig {
    private String path;
    private int port;
    private int initialDelaySeconds;
    private int periodSeconds;
    private int timeoutSeconds;
    private int failureThreshold;
    private int successThreshold;
    
    // Getters and setters
    public String getPath() { return path; }
    public void setPath(String path) { this.path = path; }
    
    public int getPort() { return port; }
    public void setPort(int port) { this.port = port; }
    
    public int getInitialDelaySeconds() { return initialDelaySeconds; }
    public void setInitialDelaySeconds(int initialDelaySeconds) { this.initialDelaySeconds = initialDelaySeconds; }
    
    public int getPeriodSeconds() { return periodSeconds; }
    public void setPeriodSeconds(int periodSeconds) { this.periodSeconds = periodSeconds; }
    
    public int getTimeoutSeconds() { return timeoutSeconds; }
    public void setTimeoutSeconds(int timeoutSeconds) { this.timeoutSeconds = timeoutSeconds; }
    
    public int getFailureThreshold() { return failureThreshold; }
    public void setFailureThreshold(int failureThreshold) { this.failureThreshold = failureThreshold; }
    
    public int getSuccessThreshold() { return successThreshold; }
    public void setSuccessThreshold(int successThreshold) { this.successThreshold = successThreshold; }
}

@TuskConfig
public class DockerConfig {
    private String baseImage;
    private String registry;
    private String tag;
    private List<String> ports;
    private Map<String, String> environment;
    private List<String> volumes;
    private HealthCheckConfig healthCheck;
    
    // Getters and setters
    public String getBaseImage() { return baseImage; }
    public void setBaseImage(String baseImage) { this.baseImage = baseImage; }
    
    public String getRegistry() { return registry; }
    public void setRegistry(String registry) { this.registry = registry; }
    
    public String getTag() { return tag; }
    public void setTag(String tag) { this.tag = tag; }
    
    public List<String> getPorts() { return ports; }
    public void setPorts(List<String> ports) { this.ports = ports; }
    
    public Map<String, String> getEnvironment() { return environment; }
    public void setEnvironment(Map<String, String> environment) { this.environment = environment; }
    
    public List<String> getVolumes() { return volumes; }
    public void setVolumes(List<String> volumes) { this.volumes = volumes; }
    
    public HealthCheckConfig getHealthCheck() { return healthCheck; }
    public void setHealthCheck(HealthCheckConfig healthCheck) { this.healthCheck = healthCheck; }
}

@TuskConfig
public class HealthCheckConfig {
    private String command;
    private int interval;
    private int timeout;
    private int retries;
    private int startPeriod;
    
    // Getters and setters
    public String getCommand() { return command; }
    public void setCommand(String command) { this.command = command; }
    
    public int getInterval() { return interval; }
    public void setInterval(int interval) { this.interval = interval; }
    
    public int getTimeout() { return timeout; }
    public void setTimeout(int timeout) { this.timeout = timeout; }
    
    public int getRetries() { return retries; }
    public void setRetries(int retries) { this.retries = retries; }
    
    public int getStartPeriod() { return startPeriod; }
    public void setStartPeriod(int startPeriod) { this.startPeriod = startPeriod; }
}

@TuskConfig
public class CloudProviderConfig {
    private String provider;
    private String region;
    private String zone;
    private String projectId;
    private NetworkConfig network;
    private StorageConfig storage;
    
    // Getters and setters
    public String getProvider() { return provider; }
    public void setProvider(String provider) { this.provider = provider; }
    
    public String getRegion() { return region; }
    public void setRegion(String region) { this.region = region; }
    
    public String getZone() { return zone; }
    public void setZone(String zone) { this.zone = zone; }
    
    public String getProjectId() { return projectId; }
    public void setProjectId(String projectId) { this.projectId = projectId; }
    
    public NetworkConfig getNetwork() { return network; }
    public void setNetwork(NetworkConfig network) { this.network = network; }
    
    public StorageConfig getStorage() { return storage; }
    public void setStorage(StorageConfig storage) { this.storage = storage; }
}

@TuskConfig
public class NetworkConfig {
    private String vpcId;
    private String subnetId;
    private List<String> securityGroups;
    private String loadBalancerType;
    private boolean publicAccess;
    
    // Getters and setters
    public String getVpcId() { return vpcId; }
    public void setVpcId(String vpcId) { this.vpcId = vpcId; }
    
    public String getSubnetId() { return subnetId; }
    public void setSubnetId(String subnetId) { this.subnetId = subnetId; }
    
    public List<String> getSecurityGroups() { return securityGroups; }
    public void setSecurityGroups(List<String> securityGroups) { this.securityGroups = securityGroups; }
    
    public String getLoadBalancerType() { return loadBalancerType; }
    public void setLoadBalancerType(String loadBalancerType) { this.loadBalancerType = loadBalancerType; }
    
    public boolean isPublicAccess() { return publicAccess; }
    public void setPublicAccess(boolean publicAccess) { this.publicAccess = publicAccess; }
}

@TuskConfig
public class StorageConfig {
    private String type;
    private String bucket;
    private String path;
    private boolean encrypted;
    private String encryptionKey;
    
    // Getters and setters
    public String getType() { return type; }
    public void setType(String type) { this.type = type; }
    
    public String getBucket() { return bucket; }
    public void setBucket(String bucket) { this.bucket = bucket; }
    
    public String getPath() { return path; }
    public void setPath(String path) { this.path = path; }
    
    public boolean isEncrypted() { return encrypted; }
    public void setEncrypted(boolean encrypted) { this.encrypted = encrypted; }
    
    public String getEncryptionKey() { return encryptionKey; }
    public void setEncryptionKey(String encryptionKey) { this.encryptionKey = encryptionKey; }
}

@TuskConfig
public class AutoScalingConfig {
    private boolean enabled;
    private int minReplicas;
    private int maxReplicas;
    private int targetCpuUtilization;
    private int targetMemoryUtilization;
    private ScalingRuleConfig scalingRules;
    
    // Getters and setters
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public int getMinReplicas() { return minReplicas; }
    public void setMinReplicas(int minReplicas) { this.minReplicas = minReplicas; }
    
    public int getMaxReplicas() { return maxReplicas; }
    public void setMaxReplicas(int maxReplicas) { this.maxReplicas = maxReplicas; }
    
    public int getTargetCpuUtilization() { return targetCpuUtilization; }
    public void setTargetCpuUtilization(int targetCpuUtilization) { this.targetCpuUtilization = targetCpuUtilization; }
    
    public int getTargetMemoryUtilization() { return targetMemoryUtilization; }
    public void setTargetMemoryUtilization(int targetMemoryUtilization) { this.targetMemoryUtilization = targetMemoryUtilization; }
    
    public ScalingRuleConfig getScalingRules() { return scalingRules; }
    public void setScalingRules(ScalingRuleConfig scalingRules) { this.scalingRules = scalingRules; }
}

@TuskConfig
public class ScalingRuleConfig {
    private List<MetricRule> cpuRules;
    private List<MetricRule> memoryRules;
    private List<MetricRule> customRules;
    
    // Getters and setters
    public List<MetricRule> getCpuRules() { return cpuRules; }
    public void setCpuRules(List<MetricRule> cpuRules) { this.cpuRules = cpuRules; }
    
    public List<MetricRule> getMemoryRules() { return memoryRules; }
    public void setMemoryRules(List<MetricRule> memoryRules) { this.memoryRules = memoryRules; }
    
    public List<MetricRule> getCustomRules() { return customRules; }
    public void setCustomRules(List<MetricRule> customRules) { this.customRules = customRules; }
}

@TuskConfig
public class MetricRule {
    private String metric;
    private String operator;
    private double threshold;
    private int periodSeconds;
    
    // Getters and setters
    public String getMetric() { return metric; }
    public void setMetric(String metric) { this.metric = metric; }
    
    public String getOperator() { return operator; }
    public void setOperator(String operator) { this.operator = operator; }
    
    public double getThreshold() { return threshold; }
    public void setThreshold(double threshold) { this.threshold = threshold; }
    
    public int getPeriodSeconds() { return periodSeconds; }
    public void setPeriodSeconds(int periodSeconds) { this.periodSeconds = periodSeconds; }
}

@TuskConfig
public class MonitoringConfig {
    private String prometheusUrl;
    private String grafanaUrl;
    private String alertManagerUrl;
    private List<AlertRule> alertRules;
    private DashboardConfig dashboards;
    
    // Getters and setters
    public String getPrometheusUrl() { return prometheusUrl; }
    public void setPrometheusUrl(String prometheusUrl) { this.prometheusUrl = prometheusUrl; }
    
    public String getGrafanaUrl() { return grafanaUrl; }
    public void setGrafanaUrl(String grafanaUrl) { this.grafanaUrl = grafanaUrl; }
    
    public String getAlertManagerUrl() { return alertManagerUrl; }
    public void setAlertManagerUrl(String alertManagerUrl) { this.alertManagerUrl = alertManagerUrl; }
    
    public List<AlertRule> getAlertRules() { return alertRules; }
    public void setAlertRules(List<AlertRule> alertRules) { this.alertRules = alertRules; }
    
    public DashboardConfig getDashboards() { return dashboards; }
    public void setDashboards(DashboardConfig dashboards) { this.dashboards = dashboards; }
}

@TuskConfig
public class AlertRule {
    private String name;
    private String condition;
    private String severity;
    private String summary;
    private String description;
    private Map<String, String> labels;
    
    // Getters and setters
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getCondition() { return condition; }
    public void setCondition(String condition) { this.condition = condition; }
    
    public String getSeverity() { return severity; }
    public void setSeverity(String severity) { this.severity = severity; }
    
    public String getSummary() { return summary; }
    public void setSummary(String summary) { this.summary = summary; }
    
    public String getDescription() { return description; }
    public void setDescription(String description) { this.description = description; }
    
    public Map<String, String> getLabels() { return labels; }
    public void setLabels(Map<String, String> labels) { this.labels = labels; }
}

@TuskConfig
public class DashboardConfig {
    private String name;
    private String title;
    private List<PanelConfig> panels;
    private RefreshConfig refresh;
    
    // Getters and setters
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getTitle() { return title; }
    public void setTitle(String title) { this.title = title; }
    
    public List<PanelConfig> getPanels() { return panels; }
    public void setPanels(List<PanelConfig> panels) { this.panels = panels; }
    
    public RefreshConfig getRefresh() { return refresh; }
    public void setRefresh(RefreshConfig refresh) { this.refresh = refresh; }
}

@TuskConfig
public class PanelConfig {
    private String title;
    private String type;
    private String query;
    private int height;
    private int width;
    private Map<String, String> options;
    
    // Getters and setters
    public String getTitle() { return title; }
    public void setTitle(String title) { this.title = title; }
    
    public String getType() { return type; }
    public void setType(String type) { this.type = type; }
    
    public String getQuery() { return query; }
    public void setQuery(String query) { this.query = query; }
    
    public int getHeight() { return height; }
    public void setHeight(int height) { this.height = height; }
    
    public int getWidth() { return width; }
    public void setWidth(int width) { this.width = width; }
    
    public Map<String, String> getOptions() { return options; }
    public void setOptions(Map<String, String> options) { this.options = options; }
}

@TuskConfig
public class RefreshConfig {
    private boolean enabled;
    private int interval;
    private String timeRange;
    
    // Getters and setters
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public int getInterval() { return interval; }
    public void setInterval(int interval) { this.interval = interval; }
    
    public String getTimeRange() { return timeRange; }
    public void setTimeRange(String timeRange) { this.timeRange = timeRange; }
}
```

## 🏗️ Cloud-Native TuskLang Configuration

### cloud-native.tsk
```tsk
# Cloud-Native Configuration
[application]
name: "user-service"
version: "2.1.0"
environment: @env("ENVIRONMENT", "production")

[kubernetes]
namespace: @env("KUBERNETES_NAMESPACE", "user-services")
service_account: "user-service-account"

[resources]
cpu_request: "500m"
cpu_limit: "1000m"
memory_request: "512Mi"
memory_limit: "1Gi"
storage_request: "10Gi"
storage_limit: "20Gi"

[liveness_probe]
path: "/actuator/health/liveness"
port: 8080
initial_delay_seconds: 30
period_seconds: 10
timeout_seconds: 5
failure_threshold: 3
success_threshold: 1

[readiness_probe]
path: "/actuator/health/readiness"
port: 8080
initial_delay_seconds: 5
period_seconds: 5
timeout_seconds: 3
failure_threshold: 3
success_threshold: 1

[labels]
app: "user-service"
version: "2.1.0"
team: "identity"
tier: "backend"

[annotations]
prometheus.io/scrape: "true"
prometheus.io/port: "8080"
prometheus.io/path: "/actuator/prometheus"

[docker]
base_image: "openjdk:17-jre-slim"
registry: @env("DOCKER_REGISTRY", "gcr.io/my-project")
tag: @env("IMAGE_TAG", "latest")
ports: ["8080:8080", "9090:9090"]

[environment]
SPRING_PROFILES_ACTIVE: @env("SPRING_PROFILES_ACTIVE", "kubernetes")
JAVA_OPTS: "-Xmx512m -Xms256m"
LOGGING_LEVEL: @env("LOGGING_LEVEL", "INFO")

[volumes]
"/var/log": "/var/log"
"/tmp": "/tmp"

[health_check]
command: "curl -f http://localhost:8080/actuator/health || exit 1"
interval: 30
timeout: 10
retries: 3
start_period: 40

[cloud_provider]
provider: @env("CLOUD_PROVIDER", "gcp")
region: @env("CLOUD_REGION", "us-central1")
zone: @env("CLOUD_ZONE", "us-central1-a")
project_id: @env("GCP_PROJECT_ID")

[network]
vpc_id: @env("VPC_ID")
subnet_id: @env("SUBNET_ID")
security_groups: [
    @env("SECURITY_GROUP_1"),
    @env("SECURITY_GROUP_2")
]
load_balancer_type: "application"
public_access: true

[storage]
type: "s3"
bucket: @env("STORAGE_BUCKET")
path: "/user-service"
encrypted: true
encryption_key: @env.secure("STORAGE_ENCRYPTION_KEY")

[auto_scaling]
enabled: true
min_replicas: 2
max_replicas: 10
target_cpu_utilization: 70
target_memory_utilization: 80

[scaling_rules]
cpu_rules: [
    {
        metric: "cpu_utilization"
        operator: ">"
        threshold: 80.0
        period_seconds: 300
    }
]
memory_rules: [
    {
        metric: "memory_utilization"
        operator: ">"
        threshold: 85.0
        period_seconds: 300
    }
]
custom_rules: [
    {
        metric: "requests_per_second"
        operator: ">"
        threshold: 1000.0
        period_seconds: 60
    }
]

[monitoring]
prometheus_url: @env("PROMETHEUS_URL", "http://prometheus:9090")
grafana_url: @env("GRAFANA_URL", "http://grafana:3000")
alert_manager_url: @env("ALERT_MANAGER_URL", "http://alertmanager:9093")

[alert_rules]
high_cpu_usage {
    name: "HighCPUUsage"
    condition: "cpu_utilization > 90"
    severity: "warning"
    summary: "High CPU usage detected"
    description: "CPU usage is above 90% for more than 5 minutes"
    labels {
        service: "user-service"
        team: "identity"
    }
}

high_memory_usage {
    name: "HighMemoryUsage"
    condition: "memory_utilization > 85"
    severity: "critical"
    summary: "High memory usage detected"
    description: "Memory usage is above 85% for more than 5 minutes"
    labels {
        service: "user-service"
        team: "identity"
    }
}

service_down {
    name: "ServiceDown"
    condition: "up == 0"
    severity: "critical"
    summary: "Service is down"
    description: "User service is not responding to health checks"
    labels {
        service: "user-service"
        team: "identity"
    }
}

[dashboards]
main_dashboard {
    name: "user-service-dashboard"
    title: "User Service Dashboard"
    refresh {
        enabled: true
        interval: 30
        time_range: "1h"
    }
}

[panels]
cpu_panel {
    title: "CPU Usage"
    type: "graph"
    query: "rate(process_cpu_seconds_total[5m]) * 100"
    height: 8
    width: 12
    options {
        y_axis_label: "CPU %"
        color: "blue"
    }
}

memory_panel {
    title: "Memory Usage"
    type: "graph"
    query: "process_resident_memory_bytes / 1024 / 1024"
    height: 8
    width: 12
    options {
        y_axis_label: "Memory MB"
        color: "green"
    }
}

request_rate_panel {
    title: "Request Rate"
    type: "graph"
    query: "rate(http_requests_total[5m])"
    height: 8
    width: 12
    options {
        y_axis_label: "Requests/sec"
        color: "orange"
    }
}

# Dynamic cloud-native configuration
[monitoring]
pod_count: @query("SELECT COUNT(*) FROM pods WHERE app = 'user-service'")
cpu_usage: @metrics("cpu_usage_percent", 0)
memory_usage: @metrics("memory_usage_percent", 0)
request_count: @metrics("http_requests_total", 0)
error_rate: @metrics("http_requests_errors_total", 0)
response_time: @metrics("http_request_duration_seconds", 0)
```

## 🐳 Docker Configuration

### Dockerfile.tsk
```tsk
# Multi-stage Docker build configuration
[build]
base_image: "openjdk:17-jdk-slim"
build_args {
    MAVEN_VERSION: "3.9.5"
    JAVA_VERSION: "17"
}

[stages]
builder {
    base: "maven:3.9.5-openjdk-17-slim"
    working_dir: "/app"
    copy: ["pom.xml", "src/"]
    run: "mvn clean package -DskipTests"
}

runtime {
    base: "openjdk:17-jre-slim"
    working_dir: "/app"
    copy: ["target/*.jar", "app.jar"]
    expose: [8080, 9090]
    healthcheck {
        command: "curl -f http://localhost:8080/actuator/health || exit 1"
        interval: "30s"
        timeout: "10s"
        retries: 3
        start_period: "40s"
    }
}

[optimization]
multi_stage: true
layer_caching: true
security_scanning: true
size_optimization: true

[security]
user: "appuser"
group: "appgroup"
non_root: true
security_updates: true
```

## ☸️ Kubernetes Manifests

### deployment.yaml.tsk
```tsk
# Kubernetes Deployment Configuration
[api_version]
version: "apps/v1"
kind: "Deployment"

[metadata]
name: "user-service"
namespace: @env("KUBERNETES_NAMESPACE", "user-services")
labels {
    app: "user-service"
    version: "2.1.0"
    team: "identity"
}

[spec]
replicas: @env("REPLICAS", "3")
selector {
    match_labels {
        app: "user-service"
    }
}

[template]
metadata {
    labels {
        app: "user-service"
        version: "2.1.0"
    }
    annotations {
        prometheus.io/scrape: "true"
        prometheus.io/port: "8080"
        prometheus.io/path: "/actuator/prometheus"
    }
}

[containers]
user_service {
    name: "user-service"
    image: "@env('DOCKER_REGISTRY')/user-service:@env('IMAGE_TAG')"
    ports: [
        {
            container_port: 8080
            name: "http"
        },
        {
            container_port: 9090
            name: "metrics"
        }
    ]
    env: [
        {
            name: "SPRING_PROFILES_ACTIVE"
            value: "kubernetes"
        },
        {
            name: "JAVA_OPTS"
            value: "-Xmx512m -Xms256m"
        },
        {
            name: "LOGGING_LEVEL"
            value: "@env('LOGGING_LEVEL', 'INFO')"
        }
    ]
    resources {
        requests {
            cpu: "500m"
            memory: "512Mi"
        }
        limits {
            cpu: "1000m"
            memory: "1Gi"
        }
    }
    liveness_probe {
        http_get {
            path: "/actuator/health/liveness"
            port: 8080
        }
        initial_delay_seconds: 30
        period_seconds: 10
        timeout_seconds: 5
        failure_threshold: 3
        success_threshold: 1
    }
    readiness_probe {
        http_get {
            path: "/actuator/health/readiness"
            port: 8080
        }
        initial_delay_seconds: 5
        period_seconds: 5
        timeout_seconds: 3
        failure_threshold: 3
        success_threshold: 1
    }
    volume_mounts: [
        {
            name: "logs"
            mount_path: "/var/log"
        },
        {
            name: "tmp"
            mount_path: "/tmp"
        }
    ]
}

[volumes]
logs {
    empty_dir {}
}
tmp {
    empty_dir {}
}

[service_account]
name: "user-service-account"
```

### service.yaml.tsk
```tsk
# Kubernetes Service Configuration
[api_version]
version: "v1"
kind: "Service"

[metadata]
name: "user-service"
namespace: @env("KUBERNETES_NAMESPACE", "user-services")
labels {
    app: "user-service"
    version: "2.1.0"
}

[spec]
type: "ClusterIP"
selector {
    app: "user-service"
}
ports: [
    {
        name: "http"
        port: 80
        target_port: 8080
        protocol: "TCP"
    },
    {
        name: "metrics"
        port: 9090
        target_port: 9090
        protocol: "TCP"
    }
]

[annotations]
prometheus.io/scrape: "true"
prometheus.io/port: "9090"
```

### ingress.yaml.tsk
```tsk
# Kubernetes Ingress Configuration
[api_version]
version: "networking.k8s.io/v1"
kind: "Ingress"

[metadata]
name: "user-service-ingress"
namespace: @env("KUBERNETES_NAMESPACE", "user-services")
labels {
    app: "user-service"
    version: "2.1.0"
}
annotations {
    kubernetes.io/ingress.class: "nginx"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/force-ssl-redirect: "true"
}

[spec]
tls: [
    {
        hosts: ["@env('DOMAIN')"]
        secret_name: "user-service-tls"
    }
]
rules: [
    {
        host: "@env('DOMAIN')"
        http {
            paths: [
                {
                    path: "/api/users"
                    path_type: "Prefix"
                    backend {
                        service {
                            name: "user-service"
                            port {
                                number: 80
                            }
                        }
                    }
                }
            ]
        }
    }
]
```

## 🚀 CI/CD Pipeline

### pipeline.tsk
```tsk
# CI/CD Pipeline Configuration
[pipeline]
name: "user-service-pipeline"
trigger: "push"
branch: "main"

[stages]
build {
    name: "Build"
    steps: [
        "mvn clean compile test",
        "mvn package -DskipTests",
        "docker build -t user-service ."
    ]
}

test {
    name: "Test"
    steps: [
        "mvn test",
        "docker run --rm user-service mvn test"
    ]
}

security_scan {
    name: "Security Scan"
    steps: [
        "trivy image user-service",
        "snyk test --docker user-service"
    ]
}

deploy_staging {
    name: "Deploy to Staging"
    environment: "staging"
    steps: [
        "kubectl apply -f k8s/staging/",
        "kubectl rollout status deployment/user-service -n staging"
    ]
}

deploy_production {
    name: "Deploy to Production"
    environment: "production"
    manual_approval: true
    steps: [
        "kubectl apply -f k8s/production/",
        "kubectl rollout status deployment/user-service -n production"
    ]
}

[environments]
staging {
    name: "staging"
    namespace: "user-services-staging"
    replicas: 2
    resources {
        cpu_request: "250m"
        cpu_limit: "500m"
        memory_request: "256Mi"
        memory_limit: "512Mi"
    }
}

production {
    name: "production"
    namespace: "user-services"
    replicas: 3
    resources {
        cpu_request: "500m"
        cpu_limit: "1000m"
        memory_request: "512Mi"
        memory_limit: "1Gi"
    }
}

[notifications]
slack {
    webhook: @env.secure("SLACK_WEBHOOK")
    channel: "#deployments"
    events: ["success", "failure"]
}

email {
    recipients: ["devops@company.com"]
    events: ["failure"]
}
```

## 🎯 Best Practices

### 1. Containerization
- Use multi-stage builds
- Optimize image size
- Implement security scanning
- Use non-root users

### 2. Kubernetes
- Use resource limits
- Implement health checks
- Configure proper probes
- Use namespaces for isolation

### 3. Auto-scaling
- Set appropriate thresholds
- Monitor scaling metrics
- Use custom metrics
- Test scaling behavior

### 4. Monitoring
- Implement comprehensive metrics
- Set up alerting rules
- Create dashboards
- Monitor resource usage

### 5. Security
- Use secrets for sensitive data
- Implement network policies
- Enable security scanning
- Follow least privilege principle

## 🔧 Troubleshooting

### Common Issues

1. **Pod Startup Failures**
   - Check resource limits
   - Verify health check configuration
   - Review container logs
   - Check image availability

2. **Scaling Issues**
   - Monitor CPU/memory usage
   - Check HPA configuration
   - Verify metrics availability
   - Review scaling rules

3. **Network Connectivity**
   - Check service configuration
   - Verify ingress setup
   - Review network policies
   - Test DNS resolution

4. **Storage Issues**
   - Check PVC configuration
   - Verify storage class
   - Review volume mounts
   - Test storage connectivity

### Debug Commands

```bash
# Check pod status
kubectl get pods -n user-services

# View pod logs
kubectl logs -f deployment/user-service -n user-services

# Check service endpoints
kubectl get endpoints -n user-services

# Monitor resource usage
kubectl top pods -n user-services

# Check HPA status
kubectl get hpa -n user-services
```

## 🚀 Next Steps

1. **Deploy to Kubernetes** using the provided manifests
2. **Set up monitoring** with Prometheus and Grafana
3. **Configure auto-scaling** based on metrics
4. **Implement CI/CD** pipeline
5. **Monitor and optimize** performance

---

**Ready to build cloud-native applications with TuskLang Java? The future of cloud computing is here, and it's containerized!** 