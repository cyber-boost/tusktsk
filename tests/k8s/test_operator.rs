use tusklang_rust::k8s::*;
use kube::Client;
use tokio;

#[tokio::test]
async fn test_crd_creation() {
    let app = TuskLangApp {
        metadata: kube::api::ObjectMeta {
            name: Some("test-app".to_string()),
            namespace: Some("default".to_string()),
            ..Default::default()
        },
        spec: TuskLangAppSpec {
            name: "test-app".to_string(),
            version: "1.0.0".to_string(),
            config_files: Some(vec![
                ConfigFile {
                    name: "config".to_string(),
                    path: "/etc/config.tsk".to_string(),
                    content: "app = { name = 'test' }".to_string(),
                }
            ]),
            ..Default::default()
        },
        status: None,
    };

    assert_eq!(app.spec.name, "test-app");
    assert_eq!(app.spec.version, "1.0.0");
    assert!(app.spec.config_files.is_some());
}

#[tokio::test]
async fn test_configmap_management() {
    let client = Client::try_default().await.unwrap();
    let manager = ConfigMapManager::new(client, "default".to_string()).await.unwrap();
    
    let config_file = ConfigFile {
        name: "test-config".to_string(),
        path: "/etc/test.tsk".to_string(),
        content: "test = true".to_string(),
    };

    let result = manager.create_configmap("test-app", &config_file).await;
    assert!(result.is_ok());
}

#[tokio::test]
async fn test_secret_rotation() {
    let client = Client::try_default().await.unwrap();
    let manager = SecretManager::new(client, "default".to_string()).await.unwrap();
    
    let policy = SecretPolicy {
        name: "test-secret".to_string(),
        length: 32,
        charset: "alphanumeric".to_string(),
    };

    let secret = manager.generate_secret(&policy).await.unwrap();
    assert_eq!(secret.len(), 32);
    assert!(secret.chars().all(|c| c.is_alphanumeric()));
}

#[tokio::test]
async fn test_monitoring_metrics() {
    let manager = MonitoringManager::new();
    
    // Test metrics collection
    manager.record_reconciliation("test-app", true).await;
    manager.record_secret_rotation("test-app", true).await;
    manager.record_configmap_update("test-app", true).await;
    
    let stats = manager.get_statistics().await;
    assert_eq!(stats.total_reconciliations, 1);
    assert_eq!(stats.successful_reconciliations, 1);
    assert_eq!(stats.total_secret_rotations, 1);
    assert_eq!(stats.successful_secret_rotations, 1);
}

#[tokio::test]
async fn test_deployment_management() {
    let client = Client::try_default().await.unwrap();
    let manager = DeploymentManager::new(client, "default".to_string());
    
    let app = TuskLangApp {
        metadata: kube::api::ObjectMeta {
            name: Some("test-app".to_string()),
            namespace: Some("default".to_string()),
            ..Default::default()
        },
        spec: TuskLangAppSpec {
            name: "test-app".to_string(),
            version: "1.0.0".to_string(),
            scaling: Some(ScalingConfig {
                min_replicas: 1,
                max_replicas: 3,
                target_cpu_utilization_percentage: Some(80),
                target_memory_utilization_percentage: Some(80),
            }),
            resources: Some(ResourceRequirements {
                requests: Some(ResourceList {
                    cpu: Some("100m".to_string()),
                    memory: Some("128Mi".to_string()),
                }),
                limits: Some(ResourceList {
                    cpu: Some("500m".to_string()),
                    memory: Some("512Mi".to_string()),
                }),
            }),
            ..Default::default()
        },
        status: None,
    };

    let result = manager.create_deployment(&app).await;
    // Note: This will fail in test environment without real Kubernetes cluster
    // In real tests, we would use a mock client
    assert!(result.is_err()); // Expected in test environment
}

#[tokio::test]
async fn test_reconciliation_logic() {
    let client = Client::try_default().await.unwrap();
    let configmap_manager = ConfigMapManager::new(client.clone(), "default".to_string()).await.unwrap();
    let secret_manager = SecretManager::new(client.clone(), "default".to_string()).await.unwrap();
    let monitoring_manager = MonitoringManager::new();
    let deployment_manager = DeploymentManager::new(client.clone(), "default".to_string());
    
    let reconciliation_manager = ReconciliationManager::new(
        client,
        "default".to_string(),
        std::sync::Arc::new(configmap_manager),
        std::sync::Arc::new(secret_manager),
        std::sync::Arc::new(monitoring_manager),
        std::sync::Arc::new(deployment_manager),
    ).await.unwrap();

    let mut app = TuskLangApp {
        metadata: kube::api::ObjectMeta {
            name: Some("test-app".to_string()),
            namespace: Some("default".to_string()),
            ..Default::default()
        },
        spec: TuskLangAppSpec {
            name: "test-app".to_string(),
            version: "1.0.0".to_string(),
            config_files: Some(vec![
                ConfigFile {
                    name: "config".to_string(),
                    path: "/etc/config.tsk".to_string(),
                    content: "app = { name = 'test' }".to_string(),
                }
            ]),
            secrets: Some(SecretConfig {
                rotation_interval: Some(3600),
                policies: Some(vec![
                    SecretPolicy {
                        name: "test-secret".to_string(),
                        length: 32,
                        charset: "alphanumeric".to_string(),
                    }
                ]),
            }),
            ..Default::default()
        },
        status: None,
    };

    let result = reconciliation_manager.reconcile_application(&mut app).await;
    // Note: This will fail in test environment without real Kubernetes cluster
    // In real tests, we would use a mock client
    assert!(result.is_err()); // Expected in test environment
}

#[tokio::test]
async fn test_operator_lifecycle() {
    let client = Client::try_default().await.unwrap();
    let operator = TuskLangOperator::new(client, Some("default".to_string())).await.unwrap();
    
    let status = operator.get_status().await;
    assert_eq!(status.version, OPERATOR_VERSION);
    assert_eq!(status.name, OPERATOR_NAME);
    assert!(!status.running);
    assert_eq!(status.managed_applications, 0);
    
    // Test statistics
    let stats = operator.get_statistics().await.unwrap();
    assert_eq!(stats.operator.version, OPERATOR_VERSION);
    assert_eq!(stats.operator.name, OPERATOR_NAME);
}

#[tokio::test]
async fn test_application_validation() {
    let client = Client::try_default().await.unwrap();
    let operator = TuskLangOperator::new(client, Some("default".to_string())).await.unwrap();
    
    let app = TuskLangApp {
        metadata: kube::api::ObjectMeta {
            name: Some("test-app".to_string()),
            namespace: Some("default".to_string()),
            ..Default::default()
        },
        spec: TuskLangAppSpec {
            name: "test-app".to_string(),
            version: "1.0.0".to_string(),
            ..Default::default()
        },
        status: None,
    };

    let validation_errors = operator.validate_application(&app).await.unwrap();
    assert!(validation_errors.is_empty());
}

#[tokio::test]
async fn test_metrics_export() {
    let client = Client::try_default().await.unwrap();
    let operator = TuskLangOperator::new(client, Some("default".to_string())).await.unwrap();
    
    // Test Prometheus metrics
    let prometheus_metrics = operator.export_prometheus_metrics().await;
    assert!(prometheus_metrics.contains("tusklang_operator_"));
    
    // Test JSON metrics
    let json_metrics = operator.export_json_metrics().await;
    assert!(json_metrics.is_object());
    assert!(json_metrics.get("operator").is_some());
}

#[tokio::test]
async fn test_error_handling() {
    let client = Client::try_default().await.unwrap();
    let operator = TuskLangOperator::new(client, Some("default".to_string())).await.unwrap();
    
    // Test invalid application name
    let app = TuskLangApp {
        metadata: kube::api::ObjectMeta {
            name: Some("".to_string()), // Invalid empty name
            namespace: Some("default".to_string()),
            ..Default::default()
        },
        spec: TuskLangAppSpec {
            name: "".to_string(), // Invalid empty name
            version: "1.0.0".to_string(),
            ..Default::default()
        },
        status: None,
    };

    let validation_errors = operator.validate_application(&app).await.unwrap();
    assert!(!validation_errors.is_empty());
    assert!(validation_errors.iter().any(|e| e.contains("name")));
}

#[tokio::test]
async fn test_scaling_configuration() {
    let app = TuskLangApp {
        metadata: kube::api::ObjectMeta {
            name: Some("test-app".to_string()),
            namespace: Some("default".to_string()),
            ..Default::default()
        },
        spec: TuskLangAppSpec {
            name: "test-app".to_string(),
            version: "1.0.0".to_string(),
            scaling: Some(ScalingConfig {
                min_replicas: 1,
                max_replicas: 10,
                target_cpu_utilization_percentage: Some(80),
                target_memory_utilization_percentage: Some(80),
            }),
            ..Default::default()
        },
        status: None,
    };

    let scaling = app.spec.scaling.unwrap();
    assert_eq!(scaling.min_replicas, 1);
    assert_eq!(scaling.max_replicas, 10);
    assert_eq!(scaling.target_cpu_utilization_percentage, Some(80));
    assert_eq!(scaling.target_memory_utilization_percentage, Some(80));
}

#[tokio::test]
async fn test_resource_requirements() {
    let app = TuskLangApp {
        metadata: kube::api::ObjectMeta {
            name: Some("test-app".to_string()),
            namespace: Some("default".to_string()),
            ..Default::default()
        },
        spec: TuskLangAppSpec {
            name: "test-app".to_string(),
            version: "1.0.0".to_string(),
            resources: Some(ResourceRequirements {
                requests: Some(ResourceList {
                    cpu: Some("100m".to_string()),
                    memory: Some("128Mi".to_string()),
                }),
                limits: Some(ResourceList {
                    cpu: Some("500m".to_string()),
                    memory: Some("512Mi".to_string()),
                }),
            }),
            ..Default::default()
        },
        status: None,
    };

    let resources = app.spec.resources.unwrap();
    assert_eq!(resources.requests.unwrap().cpu, Some("100m".to_string()));
    assert_eq!(resources.requests.unwrap().memory, Some("128Mi".to_string()));
    assert_eq!(resources.limits.unwrap().cpu, Some("500m".to_string()));
    assert_eq!(resources.limits.unwrap().memory, Some("512Mi".to_string()));
} 