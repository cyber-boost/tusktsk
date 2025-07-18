use k8s_openapi::api::core::v1::{Container, ResourceRequirements, SecurityContext, PodSecurityContext};
use k8s_openapi::api::apps::v1::Deployment;
use k8s_openapi::api::networking::v1::Ingress;
use k8s_openapi::api::rbac::v1::{Role, RoleBinding, ServiceAccount};
use k8s_openapi::apimachinery::pkg::apis::meta::v1::{ObjectMeta, LabelSelector};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct TuskLangApp {
    pub metadata: ObjectMeta,
    pub spec: TuskLangAppSpec,
    pub status: Option<TuskLangAppStatus>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct TuskLangAppSpec {
    // Core application configuration
    pub replicas: Option<i32>,
    pub image: String,
    pub image_pull_policy: Option<String>,
    pub image_pull_secrets: Option<Vec<String>>,
    
    // Resource configuration
    pub resources: Option<ResourceRequirements>,
    pub security_context: Option<SecurityContext>,
    pub pod_security_context: Option<PodSecurityContext>,
    
    // Environment and configuration
    pub env: Option<Vec<EnvVar>>,
    pub config_maps: Option<Vec<String>>,
    pub secrets: Option<Vec<String>>,
    
    // Networking
    pub ports: Option<Vec<Port>>,
    pub service_type: Option<String>,
    pub ingress: Option<IngressConfig>,
    
    // Cloud provider integration
    pub cloud_provider: Option<CloudProviderConfig>,
    
    // Service mesh integration
    pub service_mesh: Option<ServiceMeshConfig>,
    
    // Observability
    pub observability: Option<ObservabilityConfig>,
    
    // GitOps configuration
    pub gitops: Option<GitOpsConfig>,
    
    // Helm configuration
    pub helm: Option<HelmConfig>,
    
    // Container configuration
    pub containers: Option<Vec<Container>>,
    pub init_containers: Option<Vec<Container>>,
    pub sidecar_containers: Option<Vec<Container>>,
    
    // High availability
    pub high_availability: Option<HighAvailabilityConfig>,
    
    // Database configuration
    pub database: Option<DatabaseConfig>,
    
    // Security
    pub security: Option<SecurityConfig>,
    
    // Monitoring and alerting
    pub monitoring: Option<MonitoringConfig>,
    
    // Backup and disaster recovery
    pub backup: Option<BackupConfig>,
    
    // Debug and development
    pub debug: Option<DebugConfig>,
    
    // Custom configurations
    pub custom: Option<HashMap<String, serde_json::Value>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct TuskLangAppStatus {
    pub phase: String,
    pub conditions: Vec<AppCondition>,
    pub replicas: i32,
    pub available_replicas: i32,
    pub ready_replicas: i32,
    pub updated_replicas: i32,
    pub observed_generation: i64,
    pub last_update_time: Option<String>,
    pub cloud_provider_status: Option<CloudProviderStatus>,
    pub service_mesh_status: Option<ServiceMeshStatus>,
    pub observability_status: Option<ObservabilityStatus>,
    pub gitops_status: Option<GitOpsStatus>,
    pub helm_status: Option<HelmStatus>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct EnvVar {
    pub name: String,
    pub value: Option<String>,
    pub value_from: Option<EnvVarSource>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct EnvVarSource {
    pub field_ref: Option<ObjectFieldSelector>,
    pub resource_field_ref: Option<ResourceFieldSelector>,
    pub config_map_key_ref: Option<ConfigMapKeySelector>,
    pub secret_key_ref: Option<SecretKeySelector>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ObjectFieldSelector {
    pub api_version: Option<String>,
    pub field_path: String,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ResourceFieldSelector {
    pub container_name: Option<String>,
    pub resource: String,
    pub divisor: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ConfigMapKeySelector {
    pub name: String,
    pub key: String,
    pub optional: Option<bool>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct SecretKeySelector {
    pub name: String,
    pub key: String,
    pub optional: Option<bool>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct Port {
    pub name: String,
    pub container_port: i32,
    pub protocol: Option<String>,
    pub host_port: Option<i32>,
    pub host_ip: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct IngressConfig {
    pub enabled: bool,
    pub annotations: Option<HashMap<String, String>>,
    pub hosts: Vec<IngressHost>,
    pub tls: Option<Vec<IngressTLS>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct IngressHost {
    pub host: String,
    pub paths: Vec<IngressPath>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct IngressPath {
    pub path: String,
    pub path_type: Option<String>,
    pub backend: IngressBackend,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct IngressBackend {
    pub service: Option<IngressServiceBackend>,
    pub resource: Option<IngressResourceBackend>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct IngressServiceBackend {
    pub name: String,
    pub port: IngressServiceBackendPort,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct IngressServiceBackendPort {
    pub number: i32,
    pub name: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct IngressResourceBackend {
    pub api_group: Option<String>,
    pub kind: String,
    pub name: String,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct IngressTLS {
    pub hosts: Option<Vec<String>>,
    pub secret_name: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct CloudProviderConfig {
    pub provider: String, // aws, gcp, azure
    pub region: Option<String>,
    pub credentials: Option<CloudCredentials>,
    pub services: Option<CloudServices>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct CloudCredentials {
    pub access_key_id: Option<String>,
    pub secret_access_key: Option<String>,
    pub session_token: Option<String>,
    pub role_arn: Option<String>,
    pub profile: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct CloudServices {
    pub s3: Option<S3Config>,
    pub secrets_manager: Option<SecretsManagerConfig>,
    pub parameter_store: Option<ParameterStoreConfig>,
    pub lambda: Option<LambdaConfig>,
    pub ecr: Option<ECRConfig>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct S3Config {
    pub bucket: String,
    pub prefix: Option<String>,
    pub region: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct SecretsManagerConfig {
    pub region: Option<String>,
    pub prefix: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ParameterStoreConfig {
    pub region: Option<String>,
    pub prefix: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct LambdaConfig {
    pub region: Option<String>,
    pub function_name: Option<String>,
    pub timeout: Option<i32>,
    pub memory_size: Option<i32>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ECRConfig {
    pub region: Option<String>,
    pub repository: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ServiceMeshConfig {
    pub provider: String, // istio, linkerd, consul
    pub enabled: bool,
    pub namespace: Option<String>,
    pub virtual_service: Option<VirtualServiceConfig>,
    pub destination_rule: Option<DestinationRuleConfig>,
    pub peer_authentication: Option<PeerAuthenticationConfig>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct VirtualServiceConfig {
    pub hosts: Vec<String>,
    pub gateways: Option<Vec<String>>,
    pub http: Option<Vec<HTTPRoute>>,
    pub tcp: Option<Vec<TCPRoute>>,
    pub tls: Option<Vec<TLSRoute>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HTTPRoute {
    pub match_: Option<Vec<HTTPMatchRequest>>,
    pub route: Option<Vec<HTTPRouteDestination>>,
    pub redirect: Option<HTTPRedirect>,
    pub rewrite: Option<HTTPRewrite>,
    pub timeout: Option<String>,
    pub retries: Option<HTTPRetry>,
    pub fault: Option<HTTPFaultInjection>,
    pub mirror: Option<Destination>,
    pub mirror_percent: Option<f64>,
    pub cors_policy: Option<CorsPolicy>,
    pub headers: Option<Headers>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HTTPMatchRequest {
    pub uri: Option<StringMatch>,
    pub scheme: Option<StringMatch>,
    pub method: Option<StringMatch>,
    pub authority: Option<StringMatch>,
    pub headers: Option<HashMap<String, StringMatch>>,
    pub port: Option<u32>,
    pub source_labels: Option<HashMap<String, String>>,
    pub gateways: Option<Vec<String>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct StringMatch {
    pub exact: Option<String>,
    pub prefix: Option<String>,
    pub regex: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HTTPRouteDestination {
    pub destination: Destination,
    pub weight: Option<i32>,
    pub headers: Option<Headers>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct Destination {
    pub host: String,
    pub subset: Option<String>,
    pub port: Option<PortSelector>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct PortSelector {
    pub number: Option<u32>,
    pub name: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HTTPRedirect {
    pub uri: Option<String>,
    pub authority: Option<String>,
    pub port: Option<u32>,
    pub scheme: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HTTPRewrite {
    pub uri: Option<String>,
    pub authority: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HTTPRetry {
    pub attempts: i32,
    pub per_try_timeout: Option<String>,
    pub retry_on: Option<String>,
    pub retry_remote_localities: Option<bool>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HTTPFaultInjection {
    pub delay: Option<InjectDelay>,
    pub abort: Option<InjectAbort>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct InjectDelay {
    pub percentage: Option<Percent>,
    pub fixed_delay: Option<String>,
    pub exponential_delay: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct InjectAbort {
    pub percentage: Option<Percent>,
    pub http_status: Option<i32>,
    pub grpc_status: Option<String>,
    pub http2_error: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct Percent {
    pub value: f64,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct CorsPolicy {
    pub allow_origin: Option<Vec<String>>,
    pub allow_methods: Option<Vec<String>>,
    pub allow_headers: Option<Vec<String>>,
    pub expose_headers: Option<Vec<String>>,
    pub max_age: Option<String>,
    pub allow_credentials: Option<bool>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct Headers {
    pub request: Option<HeaderOperations>,
    pub response: Option<HeaderOperations>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HeaderOperations {
    pub set: Option<HashMap<String, String>>,
    pub add: Option<HashMap<String, String>>,
    pub remove: Option<Vec<String>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct TCPRoute {
    pub match_: Option<Vec<L4MatchAttributes>>,
    pub route: Option<Vec<RouteDestination>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct L4MatchAttributes {
    pub destination_subnets: Option<Vec<String>>,
    pub port: Option<u32>,
    pub source_labels: Option<HashMap<String, String>>,
    pub gateways: Option<Vec<String>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct RouteDestination {
    pub destination: Destination,
    pub weight: Option<i32>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct TLSRoute {
    pub match_: Option<Vec<TLSMatchAttributes>>,
    pub route: Option<Vec<RouteDestination>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct TLSMatchAttributes {
    pub sni_hosts: Option<Vec<String>>,
    pub destination_subnets: Option<Vec<String>>,
    pub port: Option<u32>,
    pub source_labels: Option<HashMap<String, String>>,
    pub gateways: Option<Vec<String>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct DestinationRuleConfig {
    pub host: String,
    pub traffic_policy: Option<TrafficPolicy>,
    pub subsets: Option<Vec<Subset>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct TrafficPolicy {
    pub load_balancer: Option<LoadBalancerSettings>,
    pub connection_pool: Option<ConnectionPoolSettings>,
    pub outlier_detection: Option<OutlierDetection>,
    pub tls: Option<ClientTLSSettings>,
    pub port_level_settings: Option<Vec<PortTrafficPolicy>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct LoadBalancerSettings {
    pub simple: Option<String>,
    pub consistent_hash: Option<ConsistentHashLB>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ConsistentHashLB {
    pub http_header_name: Option<String>,
    pub http_cookie: Option<HTTPCookie>,
    pub use_source_ip: Option<bool>,
    pub http_query_parameter_name: Option<String>,
    pub minimum_ring_size: Option<u64>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HTTPCookie {
    pub name: String,
    pub path: Option<String>,
    pub ttl: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ConnectionPoolSettings {
    pub tcp: Option<TCPSettings>,
    pub http: Option<HTTPSettings>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct TCPSettings {
    pub max_connections: Option<i32>,
    pub connect_timeout: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HTTPSettings {
    pub http1_max_pending_requests: Option<i32>,
    pub http2_max_requests: Option<i32>,
    pub max_requests_per_connection: Option<i32>,
    pub max_retries: Option<i32>,
    pub idle_timeout: Option<String>,
    pub h2_upgrade_policy: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct OutlierDetection {
    pub consecutive_5xx_errors: Option<u32>,
    pub interval: Option<String>,
    pub base_ejection_time: Option<String>,
    pub max_ejection_percent: Option<u32>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ClientTLSSettings {
    pub mode: String,
    pub client_certificate: Option<String>,
    pub private_key: Option<String>,
    pub ca_certificates: Option<String>,
    pub subject_alt_names: Option<Vec<String>>,
    pub sni: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct PortTrafficPolicy {
    pub port: Option<PortSelector>,
    pub load_balancer: Option<LoadBalancerSettings>,
    pub connection_pool: Option<ConnectionPoolSettings>,
    pub outlier_detection: Option<OutlierDetection>,
    pub tls: Option<ClientTLSSettings>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct Subset {
    pub name: String,
    pub labels: Option<HashMap<String, String>>,
    pub traffic_policy: Option<TrafficPolicy>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct PeerAuthenticationConfig {
    pub mtls: Option<PeerAuthenticationMutualTLS>,
    pub port_level_mtls: Option<HashMap<String, PeerAuthenticationMutualTLS>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct PeerAuthenticationMutualTLS {
    pub mode: String,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ObservabilityConfig {
    pub tracing: Option<TracingConfig>,
    pub metrics: Option<MetricsConfig>,
    pub logging: Option<LoggingConfig>,
    pub health_checks: Option<HealthCheckConfig>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct TracingConfig {
    pub enabled: bool,
    pub provider: String, // jaeger, zipkin, otel
    pub endpoint: Option<String>,
    pub sampling_rate: Option<f64>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct MetricsConfig {
    pub enabled: bool,
    pub provider: String, // prometheus, statsd
    pub endpoint: Option<String>,
    pub port: Option<i32>,
    pub path: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct LoggingConfig {
    pub level: Option<String>,
    pub format: Option<String>, // json, text
    pub output: Option<String>, // stdout, stderr, file
    pub file_path: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HealthCheckConfig {
    pub liveness_probe: Option<Probe>,
    pub readiness_probe: Option<Probe>,
    pub startup_probe: Option<Probe>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct Probe {
    pub http_get: Option<HTTPGetAction>,
    pub tcp_socket: Option<TCPSocketAction>,
    pub exec: Option<ExecAction>,
    pub initial_delay_seconds: Option<i32>,
    pub timeout_seconds: Option<i32>,
    pub period_seconds: Option<i32>,
    pub success_threshold: Option<i32>,
    pub failure_threshold: Option<i32>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HTTPGetAction {
    pub path: String,
    pub port: IntOrString,
    pub host: Option<String>,
    pub scheme: Option<String>,
    pub http_headers: Option<Vec<HTTPHeader>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct IntOrString {
    pub int_val: Option<i32>,
    pub str_val: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HTTPHeader {
    pub name: String,
    pub value: String,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct TCPSocketAction {
    pub port: IntOrString,
    pub host: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ExecAction {
    pub command: Vec<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct GitOpsConfig {
    pub enabled: bool,
    pub provider: String, // argocd, flux
    pub repository: Option<String>,
    pub branch: Option<String>,
    pub path: Option<String>,
    pub sync_policy: Option<SyncPolicy>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct SyncPolicy {
    pub automated: Option<AutomatedSyncPolicy>,
    pub sync_options: Option<Vec<String>>,
    pub retry: Option<RetryStrategy>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct AutomatedSyncPolicy {
    pub prune: Option<bool>,
    pub self_heal: Option<bool>,
    pub allow_empty: Option<bool>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct RetryStrategy {
    pub limit: Option<i32>,
    pub backoff: Option<Backoff>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct Backoff {
    pub duration: Option<String>,
    pub factor: Option<i32>,
    pub max_duration: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HelmConfig {
    pub enabled: bool,
    pub chart: Option<String>,
    pub version: Option<String>,
    pub values: Option<HashMap<String, serde_json::Value>>,
    pub repository: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HighAvailabilityConfig {
    pub enabled: bool,
    pub replicas: Option<i32>,
    pub pod_disruption_budget: Option<PodDisruptionBudgetConfig>,
    pub horizontal_pod_autoscaler: Option<HorizontalPodAutoscalerConfig>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct PodDisruptionBudgetConfig {
    pub min_available: Option<i32>,
    pub max_unavailable: Option<i32>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HorizontalPodAutoscalerConfig {
    pub min_replicas: Option<i32>,
    pub max_replicas: Option<i32>,
    pub target_cpu_utilization_percentage: Option<i32>,
    pub target_memory_utilization_percentage: Option<i32>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct DatabaseConfig {
    pub enabled: bool,
    pub type_: Option<String>, // postgres, mysql, sqlite
    pub host: Option<String>,
    pub port: Option<i32>,
    pub name: Option<String>,
    pub username: Option<String>,
    pub password: Option<String>,
    pub ssl_mode: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct SecurityConfig {
    pub pod_security_standards: Option<PodSecurityStandards>,
    pub network_policies: Option<Vec<NetworkPolicyConfig>>,
    pub rbac: Option<RBACConfig>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct PodSecurityStandards {
    pub level: String, // privileged, baseline, restricted
    pub version: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct NetworkPolicyConfig {
    pub name: String,
    pub pod_selector: Option<LabelSelector>,
    pub ingress: Option<Vec<NetworkPolicyIngressRule>>,
    pub egress: Option<Vec<NetworkPolicyEgressRule>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct NetworkPolicyIngressRule {
    pub ports: Option<Vec<NetworkPolicyPort>>,
    pub from: Option<Vec<NetworkPolicyPeer>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct NetworkPolicyEgressRule {
    pub ports: Option<Vec<NetworkPolicyPort>>,
    pub to: Option<Vec<NetworkPolicyPeer>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct NetworkPolicyPort {
    pub protocol: Option<String>,
    pub port: Option<IntOrString>,
    pub end_port: Option<i32>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct NetworkPolicyPeer {
    pub pod_selector: Option<LabelSelector>,
    pub namespace_selector: Option<LabelSelector>,
    pub ip_block: Option<IPBlock>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct IPBlock {
    pub cidr: String,
    pub except: Option<Vec<String>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct RBACConfig {
    pub create_service_account: Option<bool>,
    pub service_account_name: Option<String>,
    pub roles: Option<Vec<Role>>,
    pub role_bindings: Option<Vec<RoleBinding>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct MonitoringConfig {
    pub enabled: bool,
    pub prometheus: Option<PrometheusConfig>,
    pub grafana: Option<GrafanaConfig>,
    pub alertmanager: Option<AlertmanagerConfig>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct PrometheusConfig {
    pub enabled: bool,
    pub retention: Option<String>,
    pub storage: Option<StorageConfig>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct GrafanaConfig {
    pub enabled: bool,
    pub admin_password: Option<String>,
    pub dashboards: Option<Vec<String>>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct AlertmanagerConfig {
    pub enabled: bool,
    pub config: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct StorageConfig {
    pub type_: String, // persistent, empty_dir
    pub size: Option<String>,
    pub storage_class: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct BackupConfig {
    pub enabled: bool,
    pub schedule: Option<String>,
    pub retention: Option<String>,
    pub storage: Option<BackupStorageConfig>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct BackupStorageConfig {
    pub type_: String, // s3, gcs, azure
    pub bucket: Option<String>,
    pub prefix: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct DebugConfig {
    pub enabled: bool,
    pub log_level: Option<String>,
    pub debug_endpoints: Option<bool>,
    pub profiling: Option<bool>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct AppCondition {
    pub type_: String,
    pub status: String,
    pub last_transition_time: Option<String>,
    pub reason: Option<String>,
    pub message: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct CloudProviderStatus {
    pub connected: bool,
    pub services: Vec<String>,
    pub last_check: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ServiceMeshStatus {
    pub enabled: bool,
    pub provider: String,
    pub virtual_services: Vec<String>,
    pub destination_rules: Vec<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct ObservabilityStatus {
    pub tracing_enabled: bool,
    pub metrics_enabled: bool,
    pub logging_enabled: bool,
    pub endpoints: HashMap<String, String>,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct GitOpsStatus {
    pub enabled: bool,
    pub provider: String,
    pub repository: String,
    pub last_sync: Option<String>,
    pub sync_status: String,
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct HelmStatus {
    pub enabled: bool,
    pub chart: String,
    pub version: String,
    pub status: String,
    pub last_updated: Option<String>,
} 