use std::collections::HashMap;
use k8s_openapi::api::apps::v1::Deployment;
use k8s_openapi::api::core::v1::{Container, ContainerPort, EnvVar, EnvVarSource, SecretKeySelector, ConfigMapKeySelector, ResourceRequirements, Probe, ExecAction};
use k8s_openapi::apimachinery::pkg::apis::meta::v1::{ObjectMeta, LabelSelector};
use k8s_openapi::apimachinery::pkg::util::intstr::IntOrString;
use kube::{Api, Client, ResourceExt};
use anyhow::{Result, Context};
use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

use crate::k8s::crd::{TuskLangApp, ScalingConfig, ResourceConfig, MonitoringConfig};

/// Deployment manager for TuskLang applications
pub struct DeploymentManager {
    client: Client,
    namespace: String,
}

/// Deployment status
#[derive(Debug, Serialize, Deserialize)]
pub struct DeploymentStatus {
    /// Deployment name
    pub name: String,
    /// Number of desired replicas
    pub desired_replicas: i32,
    /// Number of ready replicas
    pub ready_replicas: i32,
    /// Number of available replicas
    pub available_replicas: i32,
    /// Number of updated replicas
    pub updated_replicas: i32,
    /// Deployment conditions
    pub conditions: Vec<DeploymentCondition>,
    /// Last update time
    pub last_update_time: Option<DateTime<Utc>>,
}

/// Deployment condition
#[derive(Debug, Serialize, Deserialize)]
pub struct DeploymentCondition {
    /// Condition type
    pub type_: String,
    /// Condition status
    pub status: String,
    /// Last transition time
    pub last_transition_time: DateTime<Utc>,
    /// Reason for the condition
    pub reason: String,
    /// Human-readable message
    pub message: String,
}

impl DeploymentManager {
    /// Create a new deployment manager
    pub fn new(client: Client, namespace: String) -> Self {
        Self {
            client,
            namespace,
        }
    }

    /// Reconcile deployment for a TuskLang application
    pub async fn reconcile_deployment(&self, app: &TuskLangApp) -> Result<()> {
        let app_name = app.metadata.name.as_ref().unwrap();
        let deployment_name = format!("{}-deployment", app_name);
        
        let api: Api<Deployment> = Api::namespaced(self.client.clone(), &self.namespace);

        // Check if deployment exists
        match api.get(&deployment_name).await {
            Ok(existing_deployment) => {
                // Update existing deployment
                self.update_deployment(&api, &existing_deployment, app).await?;
            }
            Err(_) => {
                // Create new deployment
                self.create_deployment(&api, app, &deployment_name).await?;
            }
        }

        Ok(())
    }

    /// Create a new deployment
    async fn create_deployment(
        &self,
        api: &Api<Deployment>,
        app: &TuskLangApp,
        deployment_name: &str,
    ) -> Result<()> {
        let app_name = app.metadata.name.as_ref().unwrap();
        
        let deployment = Deployment {
            metadata: ObjectMeta {
                name: Some(deployment_name.to_string()),
                namespace: Some(self.namespace.clone()),
                labels: Some(HashMap::from([
                    ("app".to_string(), app_name.clone()),
                    ("managed-by".to_string(), "tusklang-operator".to_string()),
                ])),
                annotations: Some(HashMap::from([
                    ("tusklang.io/created-at".to_string(), Utc::now().to_rfc3339()),
                    ("tusklang.io/version".to_string(), app.spec.version.clone()),
                ])),
                ..Default::default()
            },
            spec: Some(k8s_openapi::api::apps::v1::DeploymentSpec {
                replicas: Some(app.spec.scaling.min_replicas),
                selector: Some(LabelSelector {
                    match_labels: Some(HashMap::from([
                        ("app".to_string(), app_name.clone()),
                    ])),
                    ..Default::default()
                }),
                template: k8s_openapi::api::core::v1::PodTemplateSpec {
                    metadata: Some(ObjectMeta {
                        labels: Some(HashMap::from([
                            ("app".to_string(), app_name.clone()),
                        ])),
                        ..Default::default()
                    }),
                    spec: Some(k8s_openapi::api::core::v1::PodSpec {
                        containers: vec![self.create_container(app)],
                        ..Default::default()
                    }),
                },
                ..Default::default()
            }),
            ..Default::default()
        };

        api.create(&Default::default(), &deployment).await
            .context("Failed to create deployment")?;

        Ok(())
    }

    /// Update an existing deployment
    async fn update_deployment(
        &self,
        api: &Api<Deployment>,
        existing_deployment: &Deployment,
        app: &TuskLangApp,
    ) -> Result<()> {
        let mut updated_deployment = existing_deployment.clone();
        let app_name = app.metadata.name.as_ref().unwrap();

        // Update spec
        if let Some(ref mut spec) = updated_deployment.spec {
            spec.replicas = Some(app.spec.scaling.min_replicas);
            
            // Update container template
            if let Some(ref mut template) = spec.template.spec {
                template.containers = vec![self.create_container(app)];
            }
        }

        // Update annotations
        if let Some(ref mut annotations) = updated_deployment.metadata.annotations {
            annotations.insert("tusklang.io/updated-at".to_string(), Utc::now().to_rfc3339());
            annotations.insert("tusklang.io/version".to_string(), app.spec.version.clone());
        }

        api.replace(deployment_name, &Default::default(), &updated_deployment).await
            .context("Failed to update deployment")?;

        Ok(())
    }

    /// Create container specification
    fn create_container(&self, app: &TuskLangApp) -> Container {
        let app_name = app.metadata.name.as_ref().unwrap();
        
        Container {
            name: app_name.clone(),
            image: Some(format!("tusklang/{}:{}", app_name, app.spec.version)),
            ports: Some(vec![
                ContainerPort {
                    name: Some("http".to_string()),
                    container_port: 8080,
                    protocol: Some("TCP".to_string()),
                    ..Default::default()
                },
                ContainerPort {
                    name: Some("metrics".to_string()),
                    container_port: app.spec.monitoring.metrics_port,
                    protocol: Some("TCP".to_string()),
                    ..Default::default()
                },
            ]),
            env: Some(self.create_environment_variables(app)),
            resources: Some(self.create_resource_requirements(&app.spec.resources)),
            liveness_probe: Some(self.create_liveness_probe(&app.spec.monitoring)),
            readiness_probe: Some(self.create_readiness_probe(&app.spec.monitoring)),
            volume_mounts: Some(self.create_volume_mounts(app)),
            ..Default::default()
        }
    }

    /// Create environment variables
    fn create_environment_variables(&self, app: &TuskLangApp) -> Vec<EnvVar> {
        let mut env_vars = vec![
            EnvVar {
                name: "APP_NAME".to_string(),
                value: Some(app.metadata.name.as_ref().unwrap().clone()),
                ..Default::default()
            },
            EnvVar {
                name: "APP_VERSION".to_string(),
                value: Some(app.spec.version.clone()),
                ..Default::default()
            },
            EnvVar {
                name: "LOG_LEVEL".to_string(),
                value: Some(app.spec.monitoring.log_level.clone()),
                ..Default::default()
            },
        ];

        // Add ConfigMap references
        for (i, config_file) in app.spec.config_files.iter().enumerate() {
            env_vars.push(EnvVar {
                name: format!("CONFIG_{}", i.to_uppercase()),
                value_from: Some(EnvVarSource {
                    config_map_key_ref: Some(ConfigMapKeySelector {
                        name: Some(format!("{}-{}", app.metadata.name.as_ref().unwrap(), config_file.name)),
                        key: Some(config_file.name.clone()),
                        ..Default::default()
                    }),
                    ..Default::default()
                }),
                ..Default::default()
            });
        }

        // Add secret references
        for (i, secret_name) in app.spec.secrets.secrets.iter().enumerate() {
            env_vars.push(EnvVar {
                name: format!("SECRET_{}", i.to_uppercase()),
                value_from: Some(EnvVarSource {
                    secret_key_ref: Some(SecretKeySelector {
                        name: Some(secret_name.clone()),
                        key: Some("api_key".to_string()),
                        ..Default::default()
                    }),
                    ..Default::default()
                }),
                ..Default::default()
            });
        }

        env_vars
    }

    /// Create resource requirements
    fn create_resource_requirements(&self, resources: &ResourceConfig) -> ResourceRequirements {
        let mut requests = HashMap::new();
        requests.insert("cpu".to_string(), resources.cpu_request.clone());
        requests.insert("memory".to_string(), resources.memory_request.clone());

        let mut limits = HashMap::new();
        limits.insert("cpu".to_string(), resources.cpu_limit.clone());
        limits.insert("memory".to_string(), resources.memory_limit.clone());

        ResourceRequirements {
            requests: Some(requests),
            limits: Some(limits),
        }
    }

    /// Create liveness probe
    fn create_liveness_probe(&self, monitoring: &MonitoringConfig) -> Probe {
        Probe {
            exec: Some(ExecAction {
                command: Some(vec![
                    "curl".to_string(),
                    "-f".to_string(),
                    format!("http://localhost:8080{}", monitoring.health_endpoint),
                ]),
            }),
            initial_delay_seconds: Some(30),
            period_seconds: Some(10),
            timeout_seconds: Some(5),
            failure_threshold: Some(3),
            ..Default::default()
        }
    }

    /// Create readiness probe
    fn create_readiness_probe(&self, monitoring: &MonitoringConfig) -> Probe {
        Probe {
            exec: Some(ExecAction {
                command: Some(vec![
                    "curl".to_string(),
                    "-f".to_string(),
                    format!("http://localhost:8080{}", monitoring.health_endpoint),
                ]),
            }),
            initial_delay_seconds: Some(5),
            period_seconds: Some(5),
            timeout_seconds: Some(3),
            failure_threshold: Some(3),
            ..Default::default()
        }
    }

    /// Create volume mounts
    fn create_volume_mounts(&self, app: &TuskLangApp) -> Vec<k8s_openapi::api::core::v1::VolumeMount> {
        let mut volume_mounts = Vec::new();

        // Mount ConfigMaps
        for config_file in &app.spec.config_files {
            volume_mounts.push(k8s_openapi::api::core::v1::VolumeMount {
                name: format!("config-{}", config_file.name),
                mount_path: config_file.path.clone(),
                sub_path: Some(config_file.name.clone()),
                ..Default::default()
            });
        }

        volume_mounts
    }

    /// Get deployment status
    pub async fn get_deployment_status(&self, app_name: &str) -> Result<Option<DeploymentStatus>> {
        let deployment_name = format!("{}-deployment", app_name);
        let api: Api<Deployment> = Api::namespaced(self.client.clone(), &self.namespace);

        match api.get(&deployment_name).await {
            Ok(deployment) => {
                let status = if let Some(spec) = &deployment.spec {
                    let status = deployment.status.as_ref();
                    
                    DeploymentStatus {
                        name: deployment_name,
                        desired_replicas: spec.replicas.unwrap_or(0),
                        ready_replicas: status.and_then(|s| s.ready_replicas).unwrap_or(0),
                        available_replicas: status.and_then(|s| s.available_replicas).unwrap_or(0),
                        updated_replicas: status.and_then(|s| s.updated_replicas).unwrap_or(0),
                        conditions: status
                            .map(|s| s.conditions.as_ref().unwrap_or(&Vec::new()))
                            .unwrap_or(&Vec::new())
                            .iter()
                            .map(|c| DeploymentCondition {
                                type_: c.type_.clone(),
                                status: c.status.clone(),
                                last_transition_time: c.last_transition_time.as_ref()
                                    .and_then(|t| DateTime::parse_from_rfc3339(t).ok())
                                    .unwrap_or_else(|| Utc::now()),
                                reason: c.reason.as_ref().unwrap_or(&"Unknown".to_string()).clone(),
                                message: c.message.as_ref().unwrap_or(&"".to_string()).clone(),
                            })
                            .collect(),
                        last_update_time: deployment.metadata.creation_timestamp.as_ref()
                            .and_then(|t| DateTime::parse_from_rfc3339(t).ok()),
                    }
                } else {
                    DeploymentStatus {
                        name: deployment_name,
                        desired_replicas: 0,
                        ready_replicas: 0,
                        available_replicas: 0,
                        updated_replicas: 0,
                        conditions: Vec::new(),
                        last_update_time: None,
                    }
                };

                Ok(Some(status))
            }
            Err(_) => Ok(None),
        }
    }

    /// Scale deployment
    pub async fn scale_deployment(&self, app_name: &str, replicas: i32) -> Result<()> {
        let deployment_name = format!("{}-deployment", app_name);
        let api: Api<Deployment> = Api::namespaced(self.client.clone(), &self.namespace);

        let mut deployment = api.get(&deployment_name).await
            .context("Failed to get deployment")?;

        if let Some(ref mut spec) = deployment.spec {
            spec.replicas = Some(replicas);
        }

        api.replace(&deployment_name, &Default::default(), &deployment).await
            .context("Failed to scale deployment")?;

        Ok(())
    }

    /// Clean up deployment for a deleted application
    pub async fn cleanup_application_deployment(&self, app_name: &str) -> Result<()> {
        let deployment_name = format!("{}-deployment", app_name);
        let api: Api<Deployment> = Api::namespaced(self.client.clone(), &self.namespace);

        api.delete(&deployment_name, &Default::default()).await
            .context("Failed to delete deployment")?;

        Ok(())
    }

    /// Restart deployment
    pub async fn restart_deployment(&self, app_name: &str) -> Result<()> {
        let deployment_name = format!("{}-deployment", app_name);
        let api: Api<Deployment> = Api::namespaced(self.client.clone(), &self.namespace);

        let mut deployment = api.get(&deployment_name).await
            .context("Failed to get deployment")?;

        // Add restart annotation
        if let Some(ref mut annotations) = deployment.metadata.annotations {
            annotations.insert("kubectl.kubernetes.io/restartedAt".to_string(), Utc::now().to_rfc3339());
        } else {
            deployment.metadata.annotations = Some(HashMap::from([
                ("kubectl.kubernetes.io/restartedAt".to_string(), Utc::now().to_rfc3339()),
            ]));
        }

        api.replace(&deployment_name, &Default::default(), &deployment).await
            .context("Failed to restart deployment")?;

        Ok(())
    }

    /// Get deployment statistics
    pub async fn get_deployment_statistics(&self) -> Result<DeploymentStatistics> {
        let api: Api<Deployment> = Api::namespaced(self.client.clone(), &self.namespace);
        let deployments = api.list(&Default::default()).await
            .context("Failed to list deployments")?;

        let total_deployments = deployments.len();
        let mut ready_deployments = 0;
        let mut failed_deployments = 0;
        let mut total_replicas = 0;
        let mut ready_replicas = 0;

        for deployment in deployments {
            if let Some(spec) = &deployment.spec {
                let desired = spec.replicas.unwrap_or(0);
                total_replicas += desired;

                if let Some(status) = &deployment.status {
                    let ready = status.ready_replicas.unwrap_or(0);
                    ready_replicas += ready;

                    if ready > 0 {
                        ready_deployments += 1;
                    } else {
                        failed_deployments += 1;
                    }
                } else {
                    failed_deployments += 1;
                }
            }
        }

        Ok(DeploymentStatistics {
            total_deployments,
            ready_deployments,
            failed_deployments,
            total_replicas,
            ready_replicas,
            last_update: Utc::now(),
        })
    }
}

/// Deployment statistics
#[derive(Debug, Serialize, Deserialize)]
pub struct DeploymentStatistics {
    /// Total number of deployments
    pub total_deployments: usize,
    /// Number of ready deployments
    pub ready_deployments: usize,
    /// Number of failed deployments
    pub failed_deployments: usize,
    /// Total number of replicas
    pub total_replicas: i32,
    /// Number of ready replicas
    pub ready_replicas: i32,
    /// Last update time
    pub last_update: DateTime<Utc>,
}

impl DeploymentStatus {
    /// Check if deployment is ready
    pub fn is_ready(&self) -> bool {
        self.ready_replicas >= self.desired_replicas
    }

    /// Get deployment health percentage
    pub fn health_percentage(&self) -> f64 {
        if self.desired_replicas == 0 {
            0.0
        } else {
            (self.ready_replicas as f64 / self.desired_replicas as f64) * 100.0
        }
    }

    /// Check if deployment is healthy
    pub fn is_healthy(&self) -> bool {
        self.health_percentage() >= 80.0
    }
} 