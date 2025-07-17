use serde::{Deserialize, Serialize};
use kube::CustomResource;
use schemars::JsonSchema;

#[derive(CustomResource, Clone, Debug, Deserialize, Serialize, JsonSchema)]
#[kube(group = "tusklang.org", version = "v1alpha1", kind = "TuskLangApp")]
#[kube(namespaced)]
pub struct TuskLangAppSpec {
    pub name: String,
    pub version: String,
    pub config_files: Option<Vec<ConfigFile>>,
    pub secrets: Option<SecretConfig>,
    pub scaling: Option<ScalingConfig>,
    pub monitoring: Option<MonitoringConfig>,
    pub resources: Option<ResourceRequirements>,
}

#[derive(Clone, Debug, Deserialize, Serialize, JsonSchema, PartialEq)]
pub enum AppPhase {
    Pending,
    Running,
    Failed,
    Succeeded,
}

#[derive(Clone, Debug, Deserialize, Serialize, JsonSchema)]
pub struct ConfigFile {
    pub name: String,
    pub path: String,
    pub content: String,
}

#[derive(Clone, Debug, Deserialize, Serialize, JsonSchema)]
pub struct SecretConfig {
    pub rotation_interval: Option<u64>,
    pub policies: Option<Vec<SecretPolicy>>,
}

#[derive(Clone, Debug, Deserialize, Serialize, JsonSchema)]
pub struct SecretPolicy {
    pub name: String,
    pub length: u32,
    pub charset: String,
}

#[derive(Clone, Debug, Deserialize, Serialize, JsonSchema)]
pub struct ScalingConfig {
    pub min_replicas: u32,
    pub max_replicas: u32,
    pub target_cpu_utilization_percentage: Option<u32>,
    pub target_memory_utilization_percentage: Option<u32>,
}

#[derive(Clone, Debug, Deserialize, Serialize, JsonSchema)]
pub struct MonitoringConfig {
    pub enabled: Option<bool>,
    pub metrics: Option<MetricsConfig>,
    pub health_checks: Option<HealthCheckConfig>,
}

#[derive(Clone, Debug, Deserialize, Serialize, JsonSchema)]
pub struct MetricsConfig {
    pub prometheus: Option<bool>,
    pub json: Option<bool>,
}

#[derive(Clone, Debug, Deserialize, Serialize, JsonSchema)]
pub struct HealthCheckConfig {
    pub enabled: Option<bool>,
    pub interval: Option<u32>,
}

#[derive(Clone, Debug, Deserialize, Serialize, JsonSchema)]
pub struct ResourceRequirements {
    pub requests: Option<ResourceList>,
    pub limits: Option<ResourceList>,
}

#[derive(Clone, Debug, Deserialize, Serialize, JsonSchema)]
pub struct ResourceList {
    pub cpu: Option<String>,
    pub memory: Option<String>,
}

#[derive(Clone, Debug, Deserialize, Serialize, JsonSchema)]
pub struct TuskLangAppStatus {
    pub phase: Option<AppPhase>,
    pub conditions: Option<Vec<Condition>>,
    pub managed_config_maps: Option<u32>,
    pub managed_secrets: Option<u32>,
    pub last_reconciliation: Option<String>,
    pub reconciliation_count: Option<u32>,
    pub successful_reconciliations: Option<u32>,
    pub failed_reconciliations: Option<u32>,
}

#[derive(Clone, Debug, Deserialize, Serialize, JsonSchema)]
pub struct Condition {
    pub condition_type: String,
    pub status: String,
    pub last_transition_time: Option<String>,
    pub reason: Option<String>,
    pub message: Option<String>,
} 