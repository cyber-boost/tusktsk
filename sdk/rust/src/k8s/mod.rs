//! Kubernetes Operator for TuskLang
//! 
//! This module provides a complete Kubernetes operator implementation for TuskLang,
//! including ConfigMap management, secret rotation, CRD support, and reconciliation logic.

pub mod crd;
pub mod configmap;
pub mod secrets;
pub mod operator;
pub mod reconciliation;
pub mod monitoring;
pub mod deployment;

pub use crd::*;
pub use configmap::*;
pub use secrets::*;
pub use operator::*;
pub use reconciliation::*;
pub use monitoring::*;
pub use deployment::*;

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