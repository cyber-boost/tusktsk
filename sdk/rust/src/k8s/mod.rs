//! Kubernetes Operator for TuskLang
//! 
//! This module provides a complete Kubernetes operator implementation for TuskLang,
//! including ConfigMap management, secret rotation, CRD support, and reconciliation logic.

pub mod crd;
pub mod operator;
pub mod deployment;
pub mod monitoring;
pub mod reconciliation;
pub mod secrets;
pub mod configmap;
pub mod cloud_providers;
pub mod service_mesh;
pub mod observability;
pub mod gitops;
pub mod helm;

pub use crd::{TuskConfig, TuskConfigSpec, TuskConfigStatus};
pub use operator::TuskOperator;
pub use deployment::TuskDeployment;
pub use monitoring::MetricsServer;
pub use reconciliation::reconcile;
pub use secrets::SecretManager;
pub use configmap::ConfigMapManager;
pub use cloud_providers::{
    CloudProvider, AWSParameterStore, GCPSecretManager, AzureKeyVault,
    CloudProviderManager, CloudProviderFactory
};
pub use service_mesh::{
    ServiceMesh, IstioServiceMesh, ServiceMeshFactory,
    VirtualServiceConfig, DestinationRuleConfig, PeerAuthenticationConfig,
    MTLSConfig, TrafficRoutingConfig
};
pub use observability::{
    ObservabilityManager, TuskMetrics, MetricsServer as ObservabilityMetricsServer,
    HealthCheckManager, HealthCheck, HealthStatus
};
pub use gitops::GitOpsManager;
pub use helm::HelmManager;

/// Kubernetes operator version
pub const OPERATOR_VERSION: &str = env!("CARGO_PKG_VERSION");

/// Default namespace for TuskLang resources
pub const DEFAULT_NAMESPACE: &str = "tusklang-system";

/// Default operator name
pub const OPERATOR_NAME: &str = "tusklang-operator";

/// Default reconciliation interval in seconds
pub const DEFAULT_RECONCILIATION_INTERVAL: u64 = 30;

/// Default secret rotation interval in seconds
pub const DEFAULT_SECRET_ROTATION_INTERVAL: u64 = 3600; // 1 hour

/// Default ConfigMap update interval in seconds
pub const DEFAULT_CONFIGMAP_UPDATE_INTERVAL: u64 = 60; 