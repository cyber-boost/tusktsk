use std::sync::Arc;
use tokio::sync::RwLock;
use kube::{Api, Client, ResourceExt};
use anyhow::{Result, Context};
use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};
use tracing::{info, warn, error, debug};

use crate::k8s::{
    crd::{TuskLangApp, AppPhase, AppCondition},
    configmap::ConfigMapManager,
    secrets::SecretManager,
    monitoring::MonitoringManager,
    deployment::DeploymentManager,
};

/// Reconciliation result
#[derive(Debug, Serialize, Deserialize)]
pub struct ReconciliationResult {
    /// Whether reconciliation was successful
    pub success: bool,
    /// Reconciliation duration in milliseconds
    pub duration_ms: u64,
    /// Number of resources reconciled
    pub resources_reconciled: usize,
    /// Error message if failed
    pub error: Option<String>,
    /// Timestamp of reconciliation
    pub timestamp: DateTime<Utc>,
}

/// Reconciliation manager for TuskLang applications
pub struct ReconciliationManager {
    client: Client,
    namespace: String,
    configmap_manager: Arc<ConfigMapManager>,
    secret_manager: Arc<SecretManager>,
    monitoring_manager: Arc<MonitoringManager>,
    deployment_manager: Arc<DeploymentManager>,
    reconciliation_history: Arc<RwLock<Vec<ReconciliationResult>>>,
}

impl ReconciliationManager {
    /// Create a new reconciliation manager
    pub async fn new(
        client: Client,
        namespace: String,
        configmap_manager: ConfigMapManager,
        secret_manager: SecretManager,
        monitoring_manager: MonitoringManager,
        deployment_manager: DeploymentManager,
    ) -> Result<Self> {
        Ok(Self {
            client,
            namespace,
            configmap_manager: Arc::new(configmap_manager),
            secret_manager: Arc::new(secret_manager),
            monitoring_manager: Arc::new(monitoring_manager),
            deployment_manager: Arc::new(deployment_manager),
            reconciliation_history: Arc::new(RwLock::new(Vec::new())),
        })
    }

    /// Reconcile a TuskLang application
    pub async fn reconcile_application(&self, app: &mut TuskLangApp) -> Result<ReconciliationResult> {
        let start_time = std::time::Instant::now();
        let mut resources_reconciled = 0;
        let mut error_message = None;

        info!("Starting reconciliation for application: {}", app.metadata.name.as_ref().unwrap());

        // Update application status to indicate reconciliation is in progress
        app.status.phase = AppPhase::Pending;
        app.status.last_update_time = Some(Utc::now().to_rfc3339());

        // Step 1: Reconcile ConfigMaps
        match self.reconcile_configmaps(app).await {
            Ok(results) => {
                resources_reconciled += results.len();
                let created = results.iter().filter(|r| r.created).count();
                let updated = results.iter().filter(|r| r.updated).count();
                info!("ConfigMaps reconciled: {} created, {} updated", created, updated);
            }
            Err(e) => {
                error!("Failed to reconcile ConfigMaps: {}", e);
                error_message = Some(format!("ConfigMap reconciliation failed: {}", e));
            }
        }

        // Step 2: Reconcile Secrets
        if error_message.is_none() {
            match self.reconcile_secrets(app).await {
                Ok(results) => {
                    resources_reconciled += results.len();
                    let created = results.iter().filter(|r| r.created).count();
                    let rotated = results.iter().filter(|r| r.rotated).count();
                    info!("Secrets reconciled: {} created, {} rotated", created, rotated);
                }
                Err(e) => {
                    error!("Failed to reconcile secrets: {}", e);
                    error_message = Some(format!("Secret reconciliation failed: {}", e));
                }
            }
        }

        // Step 3: Reconcile Deployment
        if error_message.is_none() {
            match self.reconcile_deployment(app).await {
                Ok(_) => {
                    resources_reconciled += 1;
                    info!("Deployment reconciled successfully");
                }
                Err(e) => {
                    error!("Failed to reconcile deployment: {}", e);
                    error_message = Some(format!("Deployment reconciliation failed: {}", e));
                }
            }
        }

        // Step 4: Update monitoring
        if error_message.is_none() {
            match self.update_monitoring(app).await {
                Ok(_) => {
                    resources_reconciled += 1;
                    info!("Monitoring updated successfully");
                }
                Err(e) => {
                    warn!("Failed to update monitoring: {}", e);
                    // Monitoring failure is not critical, so we don't set error_message
                }
            }
        }

        // Step 5: Update application status
        self.update_application_status(app, error_message.as_ref()).await;

        // Step 6: Update application in Kubernetes
        self.update_application_in_k8s(app).await?;

        let duration = start_time.elapsed();
        let result = ReconciliationResult {
            success: error_message.is_none(),
            duration_ms: duration.as_millis() as u64,
            resources_reconciled,
            error: error_message,
            timestamp: Utc::now(),
        };

        // Store reconciliation result
        self.store_reconciliation_result(result.clone()).await;

        if result.success {
            info!("Reconciliation completed successfully in {}ms", result.duration_ms);
        } else {
            error!("Reconciliation failed: {}", result.error.as_ref().unwrap());
        }

        Ok(result)
    }

    /// Reconcile ConfigMaps for an application
    async fn reconcile_configmaps(&self, app: &TuskLangApp) -> Result<Vec<crate::k8s::configmap::ConfigMapUpdateResult>> {
        self.configmap_manager.reconcile_configmaps(app).await
    }

    /// Reconcile secrets for an application
    async fn reconcile_secrets(&self, app: &TuskLangApp) -> Result<Vec<crate::k8s::secrets::SecretRotationResult>> {
        self.secret_manager.reconcile_secrets(app).await
    }

    /// Reconcile deployment for an application
    async fn reconcile_deployment(&self, app: &TuskLangApp) -> Result<()> {
        self.deployment_manager.reconcile_deployment(app).await
    }

    /// Update monitoring for an application
    async fn update_monitoring(&self, app: &TuskLangApp) -> Result<()> {
        self.monitoring_manager.update_application_metrics(app).await
    }

    /// Update application status based on reconciliation results
    async fn update_application_status(&self, app: &mut TuskLangApp, error: Option<&String>) {
        let now = Utc::now();

        // Update phase based on error status
        app.status.phase = if error.is_some() {
            AppPhase::Failed
        } else {
            AppPhase::Running
        };

        // Update last update time
        app.status.last_update_time = Some(now.to_rfc3339());

        // Update conditions
        let condition = AppCondition {
            type_: "Reconciled".to_string(),
            status: if error.is_some() { "False".to_string() } else { "True".to_string() },
            last_transition_time: now.to_rfc3339(),
            reason: if error.is_some() { "ReconciliationFailed".to_string() } else { "ReconciliationSucceeded".to_string() },
            message: error.unwrap_or(&"Application reconciled successfully".to_string()).clone(),
        };

        // Update or add condition
        if let Some(existing_condition) = app.status.conditions.iter_mut().find(|c| c.type_ == "Reconciled") {
            *existing_condition = condition;
        } else {
            app.status.conditions.push(condition);
        }

        // Update replica counts (this would be fetched from actual deployment)
        // For now, we'll set default values
        app.status.ready_replicas = if error.is_some() { 0 } else { app.spec.scaling.min_replicas };
        app.status.available_replicas = if error.is_some() { 0 } else { app.spec.scaling.min_replicas };
    }

    /// Update application in Kubernetes
    async fn update_application_in_k8s(&self, app: &TuskLangApp) -> Result<()> {
        let api: Api<TuskLangApp> = Api::namespaced(self.client.clone(), &self.namespace);
        
        if let Some(name) = &app.metadata.name {
            api.replace_status(name, &Default::default(), app).await
                .context("Failed to update application status in Kubernetes")?;
        }

        Ok(())
    }

    /// Store reconciliation result in history
    async fn store_reconciliation_result(&self, result: ReconciliationResult) {
        let mut history = self.reconciliation_history.write().await;
        history.push(result);

        // Keep only last 100 results
        if history.len() > 100 {
            history.remove(0);
        }
    }

    /// Get reconciliation history
    pub async fn get_reconciliation_history(&self) -> Vec<ReconciliationResult> {
        let history = self.reconciliation_history.read().await;
        history.clone()
    }

    /// Get reconciliation statistics
    pub async fn get_reconciliation_statistics(&self) -> ReconciliationStatistics {
        let history = self.reconciliation_history.read().await;
        
        let total_reconciliations = history.len();
        let successful_reconciliations = history.iter().filter(|r| r.success).count();
        let failed_reconciliations = total_reconciliations - successful_reconciliations;

        let avg_duration = if total_reconciliations > 0 {
            let total_duration: u64 = history.iter().map(|r| r.duration_ms).sum();
            total_duration / total_reconciliations as u64
        } else {
            0
        };

        let last_reconciliation = history.last().cloned();

        ReconciliationStatistics {
            total_reconciliations,
            successful_reconciliations,
            failed_reconciliations,
            average_duration_ms: avg_duration,
            last_reconciliation,
        }
    }

    /// Clean up resources for a deleted application
    pub async fn cleanup_application(&self, app_name: &str) -> Result<()> {
        info!("Cleaning up resources for application: {}", app_name);

        // Clean up ConfigMaps
        if let Err(e) = self.configmap_manager.cleanup_application_configmaps(app_name).await {
            error!("Failed to cleanup ConfigMaps: {}", e);
        }

        // Clean up secrets
        if let Err(e) = self.secret_manager.cleanup_application_secrets(app_name).await {
            error!("Failed to cleanup secrets: {}", e);
        }

        // Clean up deployment
        if let Err(e) = self.deployment_manager.cleanup_application_deployment(app_name).await {
            error!("Failed to cleanup deployment: {}", e);
        }

        // Clean up monitoring
        if let Err(e) = self.monitoring_manager.cleanup_application_metrics(app_name).await {
            error!("Failed to cleanup monitoring: {}", e);
        }

        info!("Cleanup completed for application: {}", app_name);
        Ok(())
    }

    /// Force reconciliation of an application
    pub async fn force_reconcile(&self, app: &mut TuskLangApp) -> Result<ReconciliationResult> {
        info!("Force reconciling application: {}", app.metadata.name.as_ref().unwrap());
        
        // Clear any existing error conditions
        app.status.conditions.retain(|c| c.type_ != "Reconciled");
        
        self.reconcile_application(app).await
    }

    /// Validate application configuration
    pub async fn validate_application(&self, app: &TuskLangApp) -> Result<Vec<String>> {
        let mut errors = Vec::new();

        // Validate application name
        if app.metadata.name.as_ref().unwrap().is_empty() {
            errors.push("Application name cannot be empty".to_string());
        }

        // Validate version
        if app.spec.version.is_empty() {
            errors.push("Application version cannot be empty".to_string());
        }

        // Validate ConfigMaps
        for config_file in &app.spec.config_files {
            if config_file.name.is_empty() {
                errors.push("Config file name cannot be empty".to_string());
            }
            if config_file.path.is_empty() {
                errors.push("Config file path cannot be empty".to_string());
            }
            if config_file.content.is_empty() {
                errors.push("Config file content cannot be empty".to_string());
            }
        }

        // Validate scaling configuration
        if app.spec.scaling.min_replicas < 0 {
            errors.push("Minimum replicas cannot be negative".to_string());
        }
        if app.spec.scaling.max_replicas < app.spec.scaling.min_replicas {
            errors.push("Maximum replicas cannot be less than minimum replicas".to_string());
        }

        // Validate resource configuration
        if app.spec.resources.cpu_request.is_empty() {
            errors.push("CPU request cannot be empty".to_string());
        }
        if app.spec.resources.memory_request.is_empty() {
            errors.push("Memory request cannot be empty".to_string());
        }

        Ok(errors)
    }
}

/// Reconciliation statistics
#[derive(Debug, Serialize, Deserialize)]
pub struct ReconciliationStatistics {
    /// Total number of reconciliations
    pub total_reconciliations: usize,
    /// Number of successful reconciliations
    pub successful_reconciliations: usize,
    /// Number of failed reconciliations
    pub failed_reconciliations: usize,
    /// Average reconciliation duration in milliseconds
    pub average_duration_ms: u64,
    /// Last reconciliation result
    pub last_reconciliation: Option<ReconciliationResult>,
}

impl ReconciliationResult {
    /// Check if reconciliation was successful
    pub fn is_success(&self) -> bool {
        self.success
    }

    /// Get error message if reconciliation failed
    pub fn error_message(&self) -> Option<&String> {
        self.error.as_ref()
    }

    /// Get reconciliation duration
    pub fn duration(&self) -> std::time::Duration {
        std::time::Duration::from_millis(self.duration_ms)
    }
} 