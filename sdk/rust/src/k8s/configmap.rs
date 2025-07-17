use std::collections::HashMap;
use std::sync::Arc;
use tokio::sync::RwLock;
use k8s_openapi::api::core::v1::ConfigMap;
use k8s_openapi::apimachinery::pkg::apis::meta::v1::ObjectMeta;
use kube::{Api, Client, ResourceExt};
use anyhow::{Result, Context};
use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

use crate::k8s::crd::{TuskLangApp, ConfigFile};

/// ConfigMap manager for TuskLang applications
pub struct ConfigMapManager {
    client: Client,
    namespace: String,
    configmaps: Arc<RwLock<HashMap<String, ConfigMapInfo>>>,
}

/// Information about a managed ConfigMap
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ConfigMapInfo {
    /// ConfigMap name
    pub name: String,
    /// Namespace
    pub namespace: String,
    /// Configuration content hash
    pub content_hash: String,
    /// Last update time
    pub last_update: DateTime<Utc>,
    /// Update interval in seconds
    pub update_interval: u64,
    /// Whether the ConfigMap is healthy
    pub healthy: bool,
    /// Error message if unhealthy
    pub error_message: Option<String>,
}

/// ConfigMap update result
#[derive(Debug)]
pub struct ConfigMapUpdateResult {
    /// Whether the ConfigMap was created
    pub created: bool,
    /// Whether the ConfigMap was updated
    pub updated: bool,
    /// ConfigMap name
    pub name: String,
    /// Error if any
    pub error: Option<String>,
}

impl ConfigMapManager {
    /// Create a new ConfigMap manager
    pub async fn new(client: Client, namespace: String) -> Result<Self> {
        Ok(Self {
            client,
            namespace,
            configmaps: Arc::new(RwLock::new(HashMap::new())),
        })
    }

    /// Create or update ConfigMaps for a TuskLang application
    pub async fn reconcile_configmaps(&self, app: &TuskLangApp) -> Result<Vec<ConfigMapUpdateResult>> {
        let mut results = Vec::new();
        let api: Api<ConfigMap> = Api::namespaced(self.client.clone(), &self.namespace);

        for config_file in &app.spec.config_files {
            if !config_file.create_configmap {
                continue;
            }

            let result = self.create_or_update_configmap(&api, app, config_file).await;
            results.push(result);
        }

        Ok(results)
    }

    /// Create or update a single ConfigMap
    async fn create_or_update_configmap(
        &self,
        api: &Api<ConfigMap>,
        app: &TuskLangApp,
        config_file: &ConfigFile,
    ) -> ConfigMapUpdateResult {
        let configmap_name = format!("{}-{}", app.metadata.name.as_ref().unwrap(), config_file.name);
        let content_hash = self.calculate_content_hash(&config_file.content);

        // Check if ConfigMap exists
        match api.get(&configmap_name).await {
            Ok(existing_configmap) => {
                // Update existing ConfigMap
                match self.update_configmap(api, &existing_configmap, config_file, &content_hash).await {
                    Ok(_) => {
                        self.update_configmap_info(&configmap_name, &content_hash, config_file.update_interval).await;
                        ConfigMapUpdateResult {
                            created: false,
                            updated: true,
                            name: configmap_name,
                            error: None,
                        }
                    }
                    Err(e) => ConfigMapUpdateResult {
                        created: false,
                        updated: false,
                        name: configmap_name,
                        error: Some(e.to_string()),
                    },
                }
            }
            Err(_) => {
                // Create new ConfigMap
                match self.create_configmap(api, app, config_file, &configmap_name, &content_hash).await {
                    Ok(_) => {
                        self.update_configmap_info(&configmap_name, &content_hash, config_file.update_interval).await;
                        ConfigMapUpdateResult {
                            created: true,
                            updated: false,
                            name: configmap_name,
                            error: None,
                        }
                    }
                    Err(e) => ConfigMapUpdateResult {
                        created: false,
                        updated: false,
                        name: configmap_name,
                        error: Some(e.to_string()),
                    },
                }
            }
        }
    }

    /// Create a new ConfigMap
    async fn create_configmap(
        &self,
        api: &Api<ConfigMap>,
        app: &TuskLangApp,
        config_file: &ConfigFile,
        configmap_name: &str,
        content_hash: &str,
    ) -> Result<()> {
        let mut data = HashMap::new();
        data.insert(config_file.name.clone(), config_file.content.clone());
        data.insert("content_hash".to_string(), content_hash.to_string());

        let configmap = ConfigMap {
            metadata: ObjectMeta {
                name: Some(configmap_name.to_string()),
                namespace: Some(self.namespace.clone()),
                labels: Some(HashMap::from([
                    ("app".to_string(), app.metadata.name.as_ref().unwrap().clone()),
                    ("managed-by".to_string(), "tusklang-operator".to_string()),
                    ("config-file".to_string(), config_file.name.clone()),
                ])),
                annotations: Some(HashMap::from([
                    ("tusklang.io/content-hash".to_string(), content_hash.to_string()),
                    ("tusklang.io/update-interval".to_string(), config_file.update_interval.to_string()),
                    ("tusklang.io/created-at".to_string(), Utc::now().to_rfc3339()),
                ])),
                ..Default::default()
            },
            data: Some(data),
            ..Default::default()
        };

        api.create(&Default::default(), &configmap).await
            .context("Failed to create ConfigMap")?;

        Ok(())
    }

    /// Update an existing ConfigMap
    async fn update_configmap(
        &self,
        api: &Api<ConfigMap>,
        existing_configmap: &ConfigMap,
        config_file: &ConfigFile,
        content_hash: &str,
    ) -> Result<()> {
        let mut updated_configmap = existing_configmap.clone();
        
        // Update data
        if let Some(ref mut data) = updated_configmap.data {
            data.insert(config_file.name.clone(), config_file.content.clone());
            data.insert("content_hash".to_string(), content_hash.to_string());
        }

        // Update annotations
        if let Some(ref mut annotations) = updated_configmap.metadata.annotations {
            annotations.insert("tusklang.io/content-hash".to_string(), content_hash.to_string());
            annotations.insert("tusklang.io/updated-at".to_string(), Utc::now().to_rfc3339());
        }

        api.replace(configmap_name, &Default::default(), &updated_configmap).await
            .context("Failed to update ConfigMap")?;

        Ok(())
    }

    /// Calculate content hash for change detection
    fn calculate_content_hash(&self, content: &str) -> String {
        use sha2::{Sha256, Digest};
        let mut hasher = Sha256::new();
        hasher.update(content.as_bytes());
        format!("{:x}", hasher.finalize())
    }

    /// Update ConfigMap information in memory
    async fn update_configmap_info(&self, name: &str, content_hash: &str, update_interval: u64) {
        let mut configmaps = self.configmaps.write().await;
        configmaps.insert(name.to_string(), ConfigMapInfo {
            name: name.to_string(),
            namespace: self.namespace.clone(),
            content_hash: content_hash.to_string(),
            last_update: Utc::now(),
            update_interval,
            healthy: true,
            error_message: None,
        });
    }

    /// Get ConfigMap information
    pub async fn get_configmap_info(&self, name: &str) -> Option<ConfigMapInfo> {
        let configmaps = self.configmaps.read().await;
        configmaps.get(name).cloned()
    }

    /// List all managed ConfigMaps
    pub async fn list_configmaps(&self) -> Vec<ConfigMapInfo> {
        let configmaps = self.configmaps.read().await;
        configmaps.values().cloned().collect()
    }

    /// Delete a ConfigMap
    pub async fn delete_configmap(&self, name: &str) -> Result<()> {
        let api: Api<ConfigMap> = Api::namespaced(self.client.clone(), &self.namespace);
        
        api.delete(name, &Default::default()).await
            .context("Failed to delete ConfigMap")?;

        // Remove from memory
        let mut configmaps = self.configmaps.write().await;
        configmaps.remove(name);

        Ok(())
    }

    /// Clean up ConfigMaps for a deleted application
    pub async fn cleanup_application_configmaps(&self, app_name: &str) -> Result<()> {
        let api: Api<ConfigMap> = Api::namespaced(self.client.clone(), &self.namespace);
        
        // List ConfigMaps with app label
        let configmaps = api.list(&Default::default()).await
            .context("Failed to list ConfigMaps")?;

        for configmap in configmaps {
            if let Some(labels) = &configmap.metadata.labels {
                if labels.get("app") == Some(app_name) {
                    if let Some(name) = &configmap.metadata.name {
                        self.delete_configmap(name).await?;
                    }
                }
            }
        }

        Ok(())
    }

    /// Validate ConfigMap health
    pub async fn validate_configmap_health(&self, name: &str) -> Result<bool> {
        let api: Api<ConfigMap> = Api::namespaced(self.client.clone(), &self.namespace);
        
        match api.get(name).await {
            Ok(configmap) => {
                // Check if ConfigMap has required data
                if let Some(data) = &configmap.data {
                    if data.contains_key("content_hash") {
                        // Update health status
                        let mut configmaps = self.configmaps.write().await;
                        if let Some(info) = configmaps.get_mut(name) {
                            info.healthy = true;
                            info.error_message = None;
                        }
                        Ok(true)
                    } else {
                        self.mark_configmap_unhealthy(name, "Missing content_hash").await;
                        Ok(false)
                    }
                } else {
                    self.mark_configmap_unhealthy(name, "No data found").await;
                    Ok(false)
                }
            }
            Err(e) => {
                self.mark_configmap_unhealthy(name, &e.to_string()).await;
                Ok(false)
            }
        }
    }

    /// Mark ConfigMap as unhealthy
    async fn mark_configmap_unhealthy(&self, name: &str, error: &str) {
        let mut configmaps = self.configmaps.write().await;
        if let Some(info) = configmaps.get_mut(name) {
            info.healthy = false;
            info.error_message = Some(error.to_string());
        }
    }

    /// Get ConfigMap statistics
    pub async fn get_statistics(&self) -> ConfigMapStatistics {
        let configmaps = self.configmaps.read().await;
        let total = configmaps.len();
        let healthy = configmaps.values().filter(|info| info.healthy).count();
        let unhealthy = total - healthy;

        ConfigMapStatistics {
            total,
            healthy,
            unhealthy,
            last_update: Utc::now(),
        }
    }
}

/// ConfigMap statistics
#[derive(Debug, Serialize, Deserialize)]
pub struct ConfigMapStatistics {
    /// Total number of ConfigMaps
    pub total: usize,
    /// Number of healthy ConfigMaps
    pub healthy: usize,
    /// Number of unhealthy ConfigMaps
    pub unhealthy: usize,
    /// Last update time
    pub last_update: DateTime<Utc>,
}

impl ConfigMapInfo {
    /// Check if ConfigMap needs update based on interval
    pub fn needs_update(&self) -> bool {
        let now = Utc::now();
        let time_since_update = now.signed_duration_since(self.last_update);
        time_since_update.num_seconds() as u64 >= self.update_interval
    }

    /// Get time until next update
    pub fn time_until_update(&self) -> std::time::Duration {
        let now = Utc::now();
        let time_since_update = now.signed_duration_since(self.last_update);
        let seconds_until_update = self.update_interval.saturating_sub(time_since_update.num_seconds() as u64);
        std::time::Duration::from_secs(seconds_until_update)
    }
} 