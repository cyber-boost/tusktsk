use std::collections::HashMap;
use std::sync::Arc;
use tokio::sync::RwLock;
use k8s_openapi::api::core::v1::Secret;
use k8s_openapi::apimachinery::pkg::apis::meta::v1::ObjectMeta;
use kube::{Api, Client, ResourceExt};
use anyhow::{Result, Context};
use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};
use rand::{thread_rng, Rng};
use rand::distributions::Alphanumeric;

use crate::k8s::crd::{TuskLangApp, SecretConfig, SecretGenerationPolicy};

/// Secret manager for TuskLang applications
pub struct SecretManager {
    client: Client,
    namespace: String,
    secrets: Arc<RwLock<HashMap<String, SecretInfo>>>,
}

/// Information about a managed secret
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct SecretInfo {
    /// Secret name
    pub name: String,
    /// Namespace
    pub namespace: String,
    /// Secret type
    pub secret_type: String,
    /// Last rotation time
    pub last_rotation: DateTime<Utc>,
    /// Rotation interval in seconds
    pub rotation_interval: u64,
    /// Whether the secret is healthy
    pub healthy: bool,
    /// Error message if unhealthy
    pub error_message: Option<String>,
    /// Number of keys in the secret
    pub key_count: usize,
}

/// Secret rotation result
#[derive(Debug)]
pub struct SecretRotationResult {
    /// Whether the secret was created
    pub created: bool,
    /// Whether the secret was rotated
    pub rotated: bool,
    /// Secret name
    pub name: String,
    /// Number of keys rotated
    pub keys_rotated: usize,
    /// Error if any
    pub error: Option<String>,
}

impl SecretManager {
    /// Create a new secret manager
    pub async fn new(client: Client, namespace: String) -> Result<Self> {
        Ok(Self {
            client,
            namespace,
            secrets: Arc::new(RwLock::new(HashMap::new())),
        })
    }

    /// Create or rotate secrets for a TuskLang application
    pub async fn reconcile_secrets(&self, app: &TuskLangApp) -> Result<Vec<SecretRotationResult>> {
        let mut results = Vec::new();
        let api: Api<Secret> = Api::namespaced(self.client.clone(), &self.namespace);

        if !app.spec.secrets.enable_rotation {
            return Ok(results);
        }

        for secret_name in &app.spec.secrets.secrets {
            let result = self.create_or_rotate_secret(&api, app, secret_name).await;
            results.push(result);
        }

        Ok(results)
    }

    /// Create or rotate a single secret
    async fn create_or_rotate_secret(
        &self,
        api: &Api<Secret>,
        app: &TuskLangApp,
        secret_name: &str,
    ) -> SecretRotationResult {
        // Check if secret exists
        match api.get(secret_name).await {
            Ok(existing_secret) => {
                // Check if rotation is needed
                if self.needs_rotation(&existing_secret, &app.spec.secrets).await {
                    match self.rotate_secret(api, &existing_secret, app, secret_name).await {
                        Ok(keys_rotated) => {
                            self.update_secret_info(secret_name, app.spec.secrets.rotation_interval).await;
                            SecretRotationResult {
                                created: false,
                                rotated: true,
                                name: secret_name.to_string(),
                                keys_rotated,
                                error: None,
                            }
                        }
                        Err(e) => SecretRotationResult {
                            created: false,
                            rotated: false,
                            name: secret_name.to_string(),
                            keys_rotated: 0,
                            error: Some(e.to_string()),
                        },
                    }
                } else {
                    SecretRotationResult {
                        created: false,
                        rotated: false,
                        name: secret_name.to_string(),
                        keys_rotated: 0,
                        error: None,
                    }
                }
            }
            Err(_) => {
                // Create new secret
                match self.create_secret(api, app, secret_name).await {
                    Ok(keys_created) => {
                        self.update_secret_info(secret_name, app.spec.secrets.rotation_interval).await;
                        SecretRotationResult {
                            created: true,
                            rotated: false,
                            name: secret_name.to_string(),
                            keys_rotated: keys_created,
                            error: None,
                        }
                    }
                    Err(e) => SecretRotationResult {
                        created: false,
                        rotated: false,
                        name: secret_name.to_string(),
                        keys_rotated: 0,
                        error: Some(e.to_string()),
                    },
                }
            }
        }
    }

    /// Create a new secret
    async fn create_secret(
        &self,
        api: &Api<Secret>,
        app: &TuskLangApp,
        secret_name: &str,
    ) -> Result<usize> {
        let mut data = HashMap::new();
        let policy = &app.spec.secrets.generation_policy;

        // Generate default keys
        let default_keys = vec!["api_key", "database_password", "jwt_secret", "encryption_key"];
        let mut keys_created = 0;

        for key in default_keys {
            let value = self.generate_secret_value(policy);
            data.insert(key.to_string(), value.into_bytes());
            keys_created += 1;
        }

        let secret = Secret {
            metadata: ObjectMeta {
                name: Some(secret_name.to_string()),
                namespace: Some(self.namespace.clone()),
                labels: Some(HashMap::from([
                    ("app".to_string(), app.metadata.name.as_ref().unwrap().clone()),
                    ("managed-by".to_string(), "tusklang-operator".to_string()),
                ])),
                annotations: Some(HashMap::from([
                    ("tusklang.io/rotation-interval".to_string(), app.spec.secrets.rotation_interval.to_string()),
                    ("tusklang.io/created-at".to_string(), Utc::now().to_rfc3339()),
                    ("tusklang.io/last-rotation".to_string(), Utc::now().to_rfc3339()),
                ])),
                ..Default::default()
            },
            data: Some(data),
            type_: Some("Opaque".to_string()),
            ..Default::default()
        };

        api.create(&Default::default(), &secret).await
            .context("Failed to create secret")?;

        Ok(keys_created)
    }

    /// Rotate an existing secret
    async fn rotate_secret(
        &self,
        api: &Api<Secret>,
        existing_secret: &Secret,
        app: &TuskLangApp,
        secret_name: &str,
    ) -> Result<usize> {
        let mut updated_secret = existing_secret.clone();
        let policy = &app.spec.secrets.generation_policy;
        let mut keys_rotated = 0;

        if let Some(ref mut data) = updated_secret.data {
            for (key, _) in data.iter_mut() {
                let new_value = self.generate_secret_value(policy);
                *data.get_mut(key).unwrap() = new_value.into_bytes();
                keys_rotated += 1;
            }
        }

        // Update annotations
        if let Some(ref mut annotations) = updated_secret.metadata.annotations {
            annotations.insert("tusklang.io/last-rotation".to_string(), Utc::now().to_rfc3339());
            annotations.insert("tusklang.io/rotated-at".to_string(), Utc::now().to_rfc3339());
        }

        api.replace(secret_name, &Default::default(), &updated_secret).await
            .context("Failed to rotate secret")?;

        Ok(keys_rotated)
    }

    /// Check if secret needs rotation
    async fn needs_rotation(&self, secret: &Secret, config: &SecretConfig) -> bool {
        if let Some(annotations) = &secret.metadata.annotations {
            if let Some(last_rotation_str) = annotations.get("tusklang.io/last-rotation") {
                if let Ok(last_rotation) = DateTime::parse_from_rfc3339(last_rotation_str) {
                    let now = Utc::now();
                    let time_since_rotation = now.signed_duration_since(last_rotation.with_timezone(&Utc));
                    return time_since_rotation.num_seconds() as u64 >= config.rotation_interval;
                }
            }
        }
        true // Default to rotation if we can't determine last rotation time
    }

    /// Generate a secret value based on policy
    fn generate_secret_value(&self, policy: &SecretGenerationPolicy) -> String {
        let mut rng = thread_rng();
        let mut charset = policy.charset.clone();

        if policy.include_special {
            charset.push_str("!@#$%^&*()_+-=[]{}|;:,.<>?");
        }

        let value: String = (0..policy.length)
            .map(|_| {
                let idx = rng.gen_range(0..charset.len());
                charset.chars().nth(idx).unwrap()
            })
            .collect();

        value
    }

    /// Update secret information in memory
    async fn update_secret_info(&self, name: &str, rotation_interval: u64) {
        let mut secrets = self.secrets.write().await;
        secrets.insert(name.to_string(), SecretInfo {
            name: name.to_string(),
            namespace: self.namespace.clone(),
            secret_type: "Opaque".to_string(),
            last_rotation: Utc::now(),
            rotation_interval,
            healthy: true,
            error_message: None,
            key_count: 0, // Will be updated when we fetch the secret
        });
    }

    /// Get secret information
    pub async fn get_secret_info(&self, name: &str) -> Option<SecretInfo> {
        let secrets = self.secrets.read().await;
        secrets.get(name).cloned()
    }

    /// List all managed secrets
    pub async fn list_secrets(&self) -> Vec<SecretInfo> {
        let secrets = self.secrets.read().await;
        secrets.values().cloned().collect()
    }

    /// Delete a secret
    pub async fn delete_secret(&self, name: &str) -> Result<()> {
        let api: Api<Secret> = Api::namespaced(self.client.clone(), &self.namespace);
        
        api.delete(name, &Default::default()).await
            .context("Failed to delete secret")?;

        // Remove from memory
        let mut secrets = self.secrets.write().await;
        secrets.remove(name);

        Ok(())
    }

    /// Clean up secrets for a deleted application
    pub async fn cleanup_application_secrets(&self, app_name: &str) -> Result<()> {
        let api: Api<Secret> = Api::namespaced(self.client.clone(), &self.namespace);
        
        // List secrets with app label
        let secrets = api.list(&Default::default()).await
            .context("Failed to list secrets")?;

        for secret in secrets {
            if let Some(labels) = &secret.metadata.labels {
                if labels.get("app") == Some(app_name) {
                    if let Some(name) = &secret.metadata.name {
                        self.delete_secret(name).await?;
                    }
                }
            }
        }

        Ok(())
    }

    /// Validate secret health
    pub async fn validate_secret_health(&self, name: &str) -> Result<bool> {
        let api: Api<Secret> = Api::namespaced(self.client.clone(), &self.namespace);
        
        match api.get(name).await {
            Ok(secret) => {
                // Check if secret has data
                if let Some(data) = &secret.data {
                    if !data.is_empty() {
                        // Update health status and key count
                        let mut secrets = self.secrets.write().await;
                        if let Some(info) = secrets.get_mut(name) {
                            info.healthy = true;
                            info.error_message = None;
                            info.key_count = data.len();
                        }
                        Ok(true)
                    } else {
                        self.mark_secret_unhealthy(name, "No data found").await;
                        Ok(false)
                    }
                } else {
                    self.mark_secret_unhealthy(name, "No data found").await;
                    Ok(false)
                }
            }
            Err(e) => {
                self.mark_secret_unhealthy(name, &e.to_string()).await;
                Ok(false)
            }
        }
    }

    /// Mark secret as unhealthy
    async fn mark_secret_unhealthy(&self, name: &str, error: &str) {
        let mut secrets = self.secrets.write().await;
        if let Some(info) = secrets.get_mut(name) {
            info.healthy = false;
            info.error_message = Some(error.to_string());
        }
    }

    /// Get secret statistics
    pub async fn get_statistics(&self) -> SecretStatistics {
        let secrets = self.secrets.read().await;
        let total = secrets.len();
        let healthy = secrets.values().filter(|info| info.healthy).count();
        let unhealthy = total - healthy;
        let total_keys: usize = secrets.values().map(|info| info.key_count).sum();

        SecretStatistics {
            total,
            healthy,
            unhealthy,
            total_keys,
            last_update: Utc::now(),
        }
    }

    /// Force rotation of a secret
    pub async fn force_rotate_secret(&self, name: &str, app: &TuskLangApp) -> Result<SecretRotationResult> {
        let api: Api<Secret> = Api::namespaced(self.client.clone(), &self.namespace);
        
        match api.get(name).await {
            Ok(existing_secret) => {
                match self.rotate_secret(&api, &existing_secret, app, name).await {
                    Ok(keys_rotated) => {
                        self.update_secret_info(name, app.spec.secrets.rotation_interval).await;
                        Ok(SecretRotationResult {
                            created: false,
                            rotated: true,
                            name: name.to_string(),
                            keys_rotated,
                            error: None,
                        })
                    }
                    Err(e) => Ok(SecretRotationResult {
                        created: false,
                        rotated: false,
                        name: name.to_string(),
                        keys_rotated: 0,
                        error: Some(e.to_string()),
                    }),
                }
            }
            Err(e) => Ok(SecretRotationResult {
                created: false,
                rotated: false,
                name: name.to_string(),
                keys_rotated: 0,
                error: Some(e.to_string()),
            }),
        }
    }
}

/// Secret statistics
#[derive(Debug, Serialize, Deserialize)]
pub struct SecretStatistics {
    /// Total number of secrets
    pub total: usize,
    /// Number of healthy secrets
    pub healthy: usize,
    /// Number of unhealthy secrets
    pub unhealthy: usize,
    /// Total number of keys across all secrets
    pub total_keys: usize,
    /// Last update time
    pub last_update: DateTime<Utc>,
}

impl SecretInfo {
    /// Check if secret needs rotation based on interval
    pub fn needs_rotation(&self) -> bool {
        let now = Utc::now();
        let time_since_rotation = now.signed_duration_since(self.last_rotation);
        time_since_rotation.num_seconds() as u64 >= self.rotation_interval
    }

    /// Get time until next rotation
    pub fn time_until_rotation(&self) -> std::time::Duration {
        let now = Utc::now();
        let time_since_rotation = now.signed_duration_since(self.last_rotation);
        let seconds_until_rotation = self.rotation_interval.saturating_sub(time_since_rotation.num_seconds() as u64);
        std::time::Duration::from_secs(seconds_until_rotation)
    }
} 