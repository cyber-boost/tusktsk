use std::sync::Arc;
use std::time::Duration;
use tokio::sync::RwLock;
use tokio::time::interval;
use kube::{Api, Client, ResourceExt, runtime::controller::Action};
use anyhow::{Result, Context};
use serde::{Deserialize, Serialize};
use tracing::{info, warn, error, debug};
use futures::StreamExt;

use crate::k8s::{
    crd::{TuskLangApp, AppPhase},
    configmap::ConfigMapManager,
    secrets::SecretManager,
    reconciliation::ReconciliationManager,
    monitoring::MonitoringManager,
    deployment::DeploymentManager,
    OPERATOR_VERSION, DEFAULT_NAMESPACE, OPERATOR_NAME, DEFAULT_RECONCILIATION_INTERVAL,
};

/// Main Kubernetes operator for TuskLang
pub struct TuskLangOperator {
    client: Client,
    namespace: String,
    configmap_manager: Arc<ConfigMapManager>,
    secret_manager: Arc<SecretManager>,
    reconciliation_manager: Arc<ReconciliationManager>,
    monitoring_manager: Arc<MonitoringManager>,
    deployment_manager: Arc<DeploymentManager>,
    operator_status: Arc<RwLock<OperatorStatus>>,
}

/// Operator status
#[derive(Debug, Serialize, Deserialize)]
pub struct OperatorStatus {
    /// Operator version
    pub version: String,
    /// Operator name
    pub name: String,
    /// Whether the operator is running
    pub running: bool,
    /// Number of applications managed
    pub managed_applications: usize,
    /// Last reconciliation time
    pub last_reconciliation: Option<chrono::DateTime<chrono::Utc>>,
    /// Total reconciliations performed
    pub total_reconciliations: usize,
    /// Successful reconciliations
    pub successful_reconciliations: usize,
    /// Failed reconciliations
    pub failed_reconciliations: usize,
    /// Start time
    pub start_time: chrono::DateTime<chrono::Utc>,
}

impl TuskLangOperator {
    /// Create a new TuskLang operator
    pub async fn new(client: Client, namespace: Option<String>) -> Result<Self> {
        let namespace = namespace.unwrap_or_else(|| DEFAULT_NAMESPACE.to_string());
        
        info!("Initializing TuskLang operator v{} in namespace: {}", OPERATOR_VERSION, namespace);

        // Initialize managers
        let configmap_manager = ConfigMapManager::new(client.clone(), namespace.clone()).await?;
        let secret_manager = SecretManager::new(client.clone(), namespace.clone()).await?;
        let monitoring_manager = MonitoringManager::new();
        let deployment_manager = DeploymentManager::new(client.clone(), namespace.clone());

        let reconciliation_manager = ReconciliationManager::new(
            client.clone(),
            namespace.clone(),
            configmap_manager.clone(),
            secret_manager.clone(),
            monitoring_manager.clone(),
            deployment_manager.clone(),
        ).await?;

        let operator_status = Arc::new(RwLock::new(OperatorStatus {
            version: OPERATOR_VERSION.to_string(),
            name: OPERATOR_NAME.to_string(),
            running: false,
            managed_applications: 0,
            last_reconciliation: None,
            total_reconciliations: 0,
            successful_reconciliations: 0,
            failed_reconciliations: 0,
            start_time: chrono::Utc::now(),
        }));

        Ok(Self {
            client,
            namespace,
            configmap_manager,
            secret_manager,
            reconciliation_manager,
            monitoring_manager,
            deployment_manager,
            operator_status,
        })
    }

    /// Start the operator
    pub async fn start(&self) -> Result<()> {
        info!("Starting TuskLang operator v{}", OPERATOR_VERSION);

        // Update operator status
        {
            let mut status = self.operator_status.write().await;
            status.running = true;
        }

        // Start reconciliation loop
        self.start_reconciliation_loop().await?;

        // Start monitoring loop
        self.start_monitoring_loop().await?;

        // Start health check loop
        self.start_health_check_loop().await?;

        info!("TuskLang operator started successfully");
        Ok(())
    }

    /// Stop the operator
    pub async fn stop(&self) -> Result<()> {
        info!("Stopping TuskLang operator");

        // Update operator status
        {
            let mut status = self.operator_status.write().await;
            status.running = false;
        }

        info!("TuskLang operator stopped");
        Ok(())
    }

    /// Start the main reconciliation loop
    async fn start_reconciliation_loop(&self) -> Result<()> {
        let api: Api<TuskLangApp> = Api::namespaced(self.client.clone(), &self.namespace);
        let reconciliation_manager = self.reconciliation_manager.clone();
        let operator_status = self.operator_status.clone();

        tokio::spawn(async move {
            let mut interval = interval(Duration::from_secs(DEFAULT_RECONCILIATION_INTERVAL));
            
            loop {
                interval.tick().await;
                
                debug!("Starting reconciliation cycle");
                
                // List all TuskLang applications
                match api.list(&Default::default()).await {
                    Ok(apps) => {
                        let mut total_reconciliations = 0;
                        let mut successful_reconciliations = 0;
                        let mut failed_reconciliations = 0;

                        for app in apps {
                            let mut app = app;
                            match reconciliation_manager.reconcile_application(&mut app).await {
                                Ok(result) => {
                                    total_reconciliations += 1;
                                    if result.success {
                                        successful_reconciliations += 1;
                                    } else {
                                        failed_reconciliations += 1;
                                    }
                                }
                                Err(e) => {
                                    error!("Failed to reconcile application {}: {}", 
                                           app.metadata.name.as_ref().unwrap(), e);
                                    failed_reconciliations += 1;
                                }
                            }
                        }

                        // Update operator status
                        {
                            let mut status = operator_status.write().await;
                            status.managed_applications = apps.len();
                            status.last_reconciliation = Some(chrono::Utc::now());
                            status.total_reconciliations += total_reconciliations;
                            status.successful_reconciliations += successful_reconciliations;
                            status.failed_reconciliations += failed_reconciliations;
                        }

                        info!("Reconciliation cycle completed: {} total, {} successful, {} failed",
                              total_reconciliations, successful_reconciliations, failed_reconciliations);
                    }
                    Err(e) => {
                        error!("Failed to list applications: {}", e);
                    }
                }
            }
        });

        Ok(())
    }

    /// Start the monitoring loop
    async fn start_monitoring_loop(&self) -> Result<()> {
        let monitoring_manager = self.monitoring_manager.clone();
        let api: Api<TuskLangApp> = Api::namespaced(self.client.clone(), &self.namespace);

        tokio::spawn(async move {
            let mut interval = interval(Duration::from_secs(60)); // Update metrics every minute
            
            loop {
                interval.tick().await;
                
                debug!("Starting monitoring cycle");
                
                // Update metrics for all applications
                match api.list(&Default::default()).await {
                    Ok(apps) => {
                        for app in apps {
                            if let Err(e) = monitoring_manager.update_application_metrics(&app).await {
                                error!("Failed to update metrics for application {}: {}", 
                                       app.metadata.name.as_ref().unwrap(), e);
                            }
                        }
                    }
                    Err(e) => {
                        error!("Failed to list applications for monitoring: {}", e);
                    }
                }
            }
        });

        Ok(())
    }

    /// Start the health check loop
    async fn start_health_check_loop(&self) -> Result<()> {
        let monitoring_manager = self.monitoring_manager.clone();
        let api: Api<TuskLangApp> = Api::namespaced(self.client.clone(), &self.namespace);

        tokio::spawn(async move {
            let mut interval = interval(Duration::from_secs(300)); // Health check every 5 minutes
            
            loop {
                interval.tick().await;
                
                debug!("Starting health check cycle");
                
                // Perform health checks for all applications
                match api.list(&Default::default()).await {
                    Ok(apps) => {
                        for app in apps {
                            let app_name = app.metadata.name.as_ref().unwrap();
                            match monitoring_manager.perform_health_check(app_name).await {
                                Ok(result) => {
                                    if !result.healthy {
                                        warn!("Health check failed for application {}: {}", 
                                              app_name, result.error_message.as_ref().unwrap_or(&"Unknown error".to_string()));
                                    }
                                }
                                Err(e) => {
                                    error!("Failed to perform health check for application {}: {}", app_name, e);
                                }
                            }
                        }
                    }
                    Err(e) => {
                        error!("Failed to list applications for health check: {}", e);
                    }
                }
            }
        });

        Ok(())
    }

    /// Get operator status
    pub async fn get_status(&self) -> OperatorStatus {
        let status = self.operator_status.read().await;
        status.clone()
    }

    /// Get operator statistics
    pub async fn get_statistics(&self) -> Result<OperatorStatistics> {
        let status = self.get_status().await;
        let configmap_stats = self.configmap_manager.get_statistics().await;
        let secret_stats = self.secret_manager.get_statistics().await;
        let monitoring_stats = self.monitoring_manager.get_statistics().await;
        let deployment_stats = self.deployment_manager.get_deployment_statistics().await?;
        let reconciliation_stats = self.reconciliation_manager.get_reconciliation_statistics().await;

        Ok(OperatorStatistics {
            operator: status,
            configmaps: configmap_stats,
            secrets: secret_stats,
            monitoring: monitoring_stats,
            deployments: deployment_stats,
            reconciliation: reconciliation_stats,
        })
    }

    /// Create a new TuskLang application
    pub async fn create_application(&self, app: TuskLangApp) -> Result<()> {
        let app_name = app.metadata.name.as_ref().unwrap();
        info!("Creating new TuskLang application: {}", app_name);

        let api: Api<TuskLangApp> = Api::namespaced(self.client.clone(), &self.namespace);
        
        api.create(&Default::default(), &app).await
            .context("Failed to create application")?;

        info!("Successfully created TuskLang application: {}", app_name);
        Ok(())
    }

    /// Update an existing TuskLang application
    pub async fn update_application(&self, app: TuskLangApp) -> Result<()> {
        let app_name = app.metadata.name.as_ref().unwrap();
        info!("Updating TuskLang application: {}", app_name);

        let api: Api<TuskLangApp> = Api::namespaced(self.client.clone(), &self.namespace);
        
        api.replace(app_name, &Default::default(), &app).await
            .context("Failed to update application")?;

        info!("Successfully updated TuskLang application: {}", app_name);
        Ok(())
    }

    /// Delete a TuskLang application
    pub async fn delete_application(&self, app_name: &str) -> Result<()> {
        info!("Deleting TuskLang application: {}", app_name);

        // Clean up resources
        self.reconciliation_manager.cleanup_application(app_name).await?;

        // Delete the application
        let api: Api<TuskLangApp> = Api::namespaced(self.client.clone(), &self.namespace);
        api.delete(app_name, &Default::default()).await
            .context("Failed to delete application")?;

        info!("Successfully deleted TuskLang application: {}", app_name);
        Ok(())
    }

    /// Get all TuskLang applications
    pub async fn list_applications(&self) -> Result<Vec<TuskLangApp>> {
        let api: Api<TuskLangApp> = Api::namespaced(self.client.clone(), &self.namespace);
        let apps = api.list(&Default::default()).await
            .context("Failed to list applications")?;
        Ok(apps)
    }

    /// Get a specific TuskLang application
    pub async fn get_application(&self, app_name: &str) -> Result<Option<TuskLangApp>> {
        let api: Api<TuskLangApp> = Api::namespaced(self.client.clone(), &self.namespace);
        
        match api.get(app_name).await {
            Ok(app) => Ok(Some(app)),
            Err(_) => Ok(None),
        }
    }

    /// Force reconciliation of an application
    pub async fn force_reconcile_application(&self, app_name: &str) -> Result<()> {
        info!("Force reconciling application: {}", app_name);

        let api: Api<TuskLangApp> = Api::namespaced(self.client.clone(), &self.namespace);
        
        match api.get(app_name).await {
            Ok(mut app) => {
                self.reconciliation_manager.force_reconcile(&mut app).await?;
                Ok(())
            }
            Err(_) => {
                Err(anyhow::anyhow!("Application not found: {}", app_name))
            }
        }
    }

    /// Scale an application
    pub async fn scale_application(&self, app_name: &str, replicas: i32) -> Result<()> {
        info!("Scaling application {} to {} replicas", app_name, replicas);

        self.deployment_manager.scale_deployment(app_name, replicas).await?;

        info!("Successfully scaled application {} to {} replicas", app_name, replicas);
        Ok(())
    }

    /// Restart an application
    pub async fn restart_application(&self, app_name: &str) -> Result<()> {
        info!("Restarting application: {}", app_name);

        self.deployment_manager.restart_deployment(app_name).await?;

        info!("Successfully restarted application: {}", app_name);
        Ok(())
    }

    /// Export metrics in Prometheus format
    pub async fn export_prometheus_metrics(&self) -> String {
        self.monitoring_manager.export_prometheus_metrics().await
    }

    /// Export metrics in JSON format
    pub async fn export_json_metrics(&self) -> serde_json::Value {
        self.monitoring_manager.export_json_metrics().await
    }

    /// Validate application configuration
    pub async fn validate_application(&self, app: &TuskLangApp) -> Result<Vec<String>> {
        self.reconciliation_manager.validate_application(app).await
    }
}

/// Operator statistics
#[derive(Debug, Serialize, Deserialize)]
pub struct OperatorStatistics {
    /// Operator status
    pub operator: OperatorStatus,
    /// ConfigMap statistics
    pub configmaps: crate::k8s::configmap::ConfigMapStatistics,
    /// Secret statistics
    pub secrets: crate::k8s::secrets::SecretStatistics,
    /// Monitoring statistics
    pub monitoring: crate::k8s::monitoring::MonitoringStatistics,
    /// Deployment statistics
    pub deployments: crate::k8s::deployment::DeploymentStatistics,
    /// Reconciliation statistics
    pub reconciliation: crate::k8s::reconciliation::ReconciliationStatistics,
}

impl OperatorStatus {
    /// Check if operator is healthy
    pub fn is_healthy(&self) -> bool {
        self.running && self.failed_reconciliations < self.successful_reconciliations
    }

    /// Get reconciliation success rate
    pub fn reconciliation_success_rate(&self) -> f64 {
        if self.total_reconciliations == 0 {
            0.0
        } else {
            (self.successful_reconciliations as f64 / self.total_reconciliations as f64) * 100.0
        }
    }

    /// Get uptime
    pub fn uptime(&self) -> chrono::Duration {
        chrono::Utc::now() - self.start_time
    }
} 