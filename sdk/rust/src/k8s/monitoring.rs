use std::collections::HashMap;
use std::sync::Arc;
use tokio::sync::RwLock;
use anyhow::{Result, Context};
use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};
use tracing::{info, warn, error, debug, instrument};

use crate::k8s::crd::{TuskLangApp, AppPhase};

/// Monitoring manager for TuskLang applications
pub struct MonitoringManager {
    metrics: Arc<RwLock<HashMap<String, ApplicationMetrics>>>,
    health_checks: Arc<RwLock<HashMap<String, HealthCheckResult>>>,
    logs: Arc<RwLock<Vec<LogEntry>>>,
}

/// Application metrics
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ApplicationMetrics {
    /// Application name
    pub app_name: String,
    /// Current phase
    pub phase: AppPhase,
    /// Number of ready replicas
    pub ready_replicas: i32,
    /// Number of available replicas
    pub available_replicas: i32,
    /// CPU usage percentage
    pub cpu_usage: f64,
    /// Memory usage percentage
    pub memory_usage: f64,
    /// Number of ConfigMaps
    pub configmap_count: usize,
    /// Number of secrets
    pub secret_count: usize,
    /// Last reconciliation time
    pub last_reconciliation: Option<DateTime<Utc>>,
    /// Reconciliation success rate
    pub reconciliation_success_rate: f64,
    /// Total reconciliations
    pub total_reconciliations: usize,
    /// Successful reconciliations
    pub successful_reconciliations: usize,
    /// Failed reconciliations
    pub failed_reconciliations: usize,
    /// Average reconciliation duration in milliseconds
    pub avg_reconciliation_duration_ms: u64,
    /// Last update time
    pub last_update: DateTime<Utc>,
}

/// Health check result
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct HealthCheckResult {
    /// Application name
    pub app_name: String,
    /// Whether the application is healthy
    pub healthy: bool,
    /// Health check timestamp
    pub timestamp: DateTime<Utc>,
    /// Error message if unhealthy
    pub error_message: Option<String>,
    /// Health check duration in milliseconds
    pub duration_ms: u64,
    /// Health check type
    pub check_type: HealthCheckType,
}

/// Health check type
#[derive(Clone, Debug, Serialize, Deserialize)]
pub enum HealthCheckType {
    ConfigMap,
    Secret,
    Deployment,
    Pod,
    Service,
    Overall,
}

/// Log entry
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct LogEntry {
    /// Log level
    pub level: LogLevel,
    /// Application name
    pub app_name: String,
    /// Log message
    pub message: String,
    /// Timestamp
    pub timestamp: DateTime<Utc>,
    /// Additional context
    pub context: HashMap<String, String>,
}

/// Log level
#[derive(Clone, Debug, Serialize, Deserialize)]
pub enum LogLevel {
    Debug,
    Info,
    Warn,
    Error,
}

/// Monitoring statistics
#[derive(Debug, Serialize, Deserialize)]
pub struct MonitoringStatistics {
    /// Total number of applications monitored
    pub total_applications: usize,
    /// Number of healthy applications
    pub healthy_applications: usize,
    /// Number of unhealthy applications
    pub unhealthy_applications: usize,
    /// Total number of log entries
    pub total_log_entries: usize,
    /// Last update time
    pub last_update: DateTime<Utc>,
}

impl MonitoringManager {
    /// Create a new monitoring manager
    pub fn new() -> Self {
        Self {
            metrics: Arc::new(RwLock::new(HashMap::new())),
            health_checks: Arc::new(RwLock::new(HashMap::new())),
            logs: Arc::new(RwLock::new(Vec::new())),
        }
    }

    /// Update application metrics
    #[instrument(skip(self, app))]
    pub async fn update_application_metrics(&self, app: &TuskLangApp) -> Result<()> {
        let app_name = app.metadata.name.as_ref().unwrap().clone();
        
        debug!("Updating metrics for application: {}", app_name);

        let metrics = ApplicationMetrics {
            app_name: app_name.clone(),
            phase: app.status.phase.clone(),
            ready_replicas: app.status.ready_replicas,
            available_replicas: app.status.available_replicas,
            cpu_usage: 0.0, // Would be fetched from actual metrics
            memory_usage: 0.0, // Would be fetched from actual metrics
            configmap_count: app.spec.config_files.len(),
            secret_count: app.spec.secrets.secrets.len(),
            last_reconciliation: app.status.last_update_time.as_ref()
                .and_then(|s| DateTime::parse_from_rfc3339(s).ok())
                .map(|dt| dt.with_timezone(&Utc)),
            reconciliation_success_rate: self.calculate_success_rate(&app_name).await,
            total_reconciliations: 0, // Would be updated from reconciliation history
            successful_reconciliations: 0, // Would be updated from reconciliation history
            failed_reconciliations: 0, // Would be updated from reconciliation history
            avg_reconciliation_duration_ms: 0, // Would be updated from reconciliation history
            last_update: Utc::now(),
        };

        let mut metrics_map = self.metrics.write().await;
        metrics_map.insert(app_name, metrics);

        info!("Updated metrics for application: {}", app_name);
        Ok(())
    }

    /// Perform health check for an application
    #[instrument(skip(self))]
    pub async fn perform_health_check(&self, app_name: &str) -> Result<HealthCheckResult> {
        let start_time = std::time::Instant::now();
        
        debug!("Performing health check for application: {}", app_name);

        // Get application metrics
        let metrics = self.get_application_metrics(app_name).await;
        
        let healthy = if let Some(metrics) = metrics {
            // Check if application is running
            metrics.phase == AppPhase::Running &&
            // Check if replicas are ready
            metrics.ready_replicas > 0 &&
            // Check if success rate is above threshold
            metrics.reconciliation_success_rate > 0.8
        } else {
            false
        };

        let duration = start_time.elapsed();
        let result = HealthCheckResult {
            app_name: app_name.to_string(),
            healthy,
            timestamp: Utc::now(),
            error_message: if !healthy { Some("Application health check failed".to_string()) } else { None },
            duration_ms: duration.as_millis() as u64,
            check_type: HealthCheckType::Overall,
        };

        // Store health check result
        let mut health_checks = self.health_checks.write().await;
        health_checks.insert(app_name.to_string(), result.clone());

        if healthy {
            info!("Health check passed for application: {}", app_name);
        } else {
            warn!("Health check failed for application: {}", app_name);
        }

        Ok(result)
    }

    /// Get application metrics
    pub async fn get_application_metrics(&self, app_name: &str) -> Option<ApplicationMetrics> {
        let metrics = self.metrics.read().await;
        metrics.get(app_name).cloned()
    }

    /// Get all application metrics
    pub async fn get_all_metrics(&self) -> Vec<ApplicationMetrics> {
        let metrics = self.metrics.read().await;
        metrics.values().cloned().collect()
    }

    /// Get health check result
    pub async fn get_health_check(&self, app_name: &str) -> Option<HealthCheckResult> {
        let health_checks = self.health_checks.read().await;
        health_checks.get(app_name).cloned()
    }

    /// Get all health check results
    pub async fn get_all_health_checks(&self) -> Vec<HealthCheckResult> {
        let health_checks = self.health_checks.read().await;
        health_checks.values().cloned().collect()
    }

    /// Add log entry
    pub async fn add_log_entry(&self, level: LogLevel, app_name: String, message: String, context: HashMap<String, String>) {
        let entry = LogEntry {
            level,
            app_name,
            message,
            timestamp: Utc::now(),
            context,
        };

        let mut logs = self.logs.write().await;
        logs.push(entry);

        // Keep only last 1000 log entries
        if logs.len() > 1000 {
            logs.remove(0);
        }
    }

    /// Get log entries for an application
    pub async fn get_log_entries(&self, app_name: &str, limit: Option<usize>) -> Vec<LogEntry> {
        let logs = self.logs.read().await;
        let filtered_logs: Vec<LogEntry> = logs
            .iter()
            .filter(|entry| entry.app_name == app_name)
            .cloned()
            .collect();

        if let Some(limit) = limit {
            filtered_logs.into_iter().rev().take(limit).collect()
        } else {
            filtered_logs.into_iter().rev().collect()
        }
    }

    /// Get all log entries
    pub async fn get_all_log_entries(&self, limit: Option<usize>) -> Vec<LogEntry> {
        let logs = self.logs.read().await;
        
        if let Some(limit) = limit {
            logs.iter().rev().take(limit).cloned().collect()
        } else {
            logs.iter().rev().cloned().collect()
        }
    }

    /// Get monitoring statistics
    pub async fn get_statistics(&self) -> MonitoringStatistics {
        let metrics = self.metrics.read().await;
        let health_checks = self.health_checks.read().await;
        let logs = self.logs.read().await;

        let total_applications = metrics.len();
        let healthy_applications = health_checks.values().filter(|h| h.healthy).count();
        let unhealthy_applications = total_applications - healthy_applications;

        MonitoringStatistics {
            total_applications,
            healthy_applications,
            unhealthy_applications,
            total_log_entries: logs.len(),
            last_update: Utc::now(),
        }
    }

    /// Clean up application metrics
    pub async fn cleanup_application_metrics(&self, app_name: &str) -> Result<()> {
        debug!("Cleaning up metrics for application: {}", app_name);

        // Remove metrics
        let mut metrics = self.metrics.write().await;
        metrics.remove(app_name);

        // Remove health checks
        let mut health_checks = self.health_checks.write().await;
        health_checks.remove(app_name);

        // Remove log entries
        let mut logs = self.logs.write().await;
        logs.retain(|entry| entry.app_name != app_name);

        info!("Cleaned up metrics for application: {}", app_name);
        Ok(())
    }

    /// Calculate success rate for an application
    async fn calculate_success_rate(&self, app_name: &str) -> f64 {
        // This would typically fetch from reconciliation history
        // For now, we'll return a default value
        0.95
    }

    /// Export metrics in Prometheus format
    pub async fn export_prometheus_metrics(&self) -> String {
        let metrics = self.metrics.read().await;
        let health_checks = self.health_checks.read().await;
        
        let mut prometheus_metrics = String::new();
        
        // Application metrics
        for (app_name, app_metrics) in metrics.iter() {
            prometheus_metrics.push_str(&format!(
                "# HELP tusklang_app_ready_replicas Number of ready replicas\n",
            ));
            prometheus_metrics.push_str(&format!(
                "# TYPE tusklang_app_ready_replicas gauge\n",
            ));
            prometheus_metrics.push_str(&format!(
                "tusklang_app_ready_replicas{{app=\"{}\"}} {}\n",
                app_name, app_metrics.ready_replicas
            ));

            prometheus_metrics.push_str(&format!(
                "# HELP tusklang_app_available_replicas Number of available replicas\n",
            ));
            prometheus_metrics.push_str(&format!(
                "# TYPE tusklang_app_available_replicas gauge\n",
            ));
            prometheus_metrics.push_str(&format!(
                "tusklang_app_available_replicas{{app=\"{}\"}} {}\n",
                app_name, app_metrics.available_replicas
            ));

            prometheus_metrics.push_str(&format!(
                "# HELP tusklang_app_reconciliation_success_rate Reconciliation success rate\n",
            ));
            prometheus_metrics.push_str(&format!(
                "# TYPE tusklang_app_reconciliation_success_rate gauge\n",
            ));
            prometheus_metrics.push_str(&format!(
                "tusklang_app_reconciliation_success_rate{{app=\"{}\"}} {}\n",
                app_name, app_metrics.reconciliation_success_rate
            ));
        }

        // Health check metrics
        for (app_name, health_check) in health_checks.iter() {
            prometheus_metrics.push_str(&format!(
                "# HELP tusklang_app_healthy Application health status\n",
            ));
            prometheus_metrics.push_str(&format!(
                "# TYPE tusklang_app_healthy gauge\n",
            ));
            prometheus_metrics.push_str(&format!(
                "tusklang_app_healthy{{app=\"{}\"}} {}\n",
                app_name, if health_check.healthy { 1 } else { 0 }
            ));
        }

        prometheus_metrics
    }

    /// Export metrics in JSON format
    pub async fn export_json_metrics(&self) -> serde_json::Value {
        let metrics = self.get_all_metrics().await;
        let health_checks = self.get_all_health_checks().await;
        let statistics = self.get_statistics().await;

        serde_json::json!({
            "timestamp": Utc::now().to_rfc3339(),
            "statistics": statistics,
            "applications": metrics,
            "health_checks": health_checks,
        })
    }
}

impl ApplicationMetrics {
    /// Check if application is healthy
    pub fn is_healthy(&self) -> bool {
        self.phase == AppPhase::Running &&
        self.ready_replicas > 0 &&
        self.reconciliation_success_rate > 0.8
    }

    /// Get CPU usage percentage
    pub fn cpu_usage_percentage(&self) -> f64 {
        self.cpu_usage
    }

    /// Get memory usage percentage
    pub fn memory_usage_percentage(&self) -> f64 {
        self.memory_usage
    }

    /// Get resource utilization score (0-100)
    pub fn resource_utilization_score(&self) -> f64 {
        (self.cpu_usage + self.memory_usage) / 2.0
    }
}

impl HealthCheckResult {
    /// Check if health check passed
    pub fn is_healthy(&self) -> bool {
        self.healthy
    }

    /// Get health check duration
    pub fn duration(&self) -> std::time::Duration {
        std::time::Duration::from_millis(self.duration_ms)
    }
}

impl LogEntry {
    /// Get log level as string
    pub fn level_string(&self) -> &'static str {
        match self.level {
            LogLevel::Debug => "DEBUG",
            LogLevel::Info => "INFO",
            LogLevel::Warn => "WARN",
            LogLevel::Error => "ERROR",
        }
    }

    /// Format log entry as string
    pub fn format(&self) -> String {
        format!(
            "[{}] {} {}: {}",
            self.timestamp.format("%Y-%m-%d %H:%M:%S"),
            self.level_string(),
            self.app_name,
            self.message
        )
    }
} 