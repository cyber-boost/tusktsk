# TuskLang JavaScript Documentation: #deployment Directive

## Overview

The `#deployment` directive in TuskLang defines deployment configurations and strategies, enabling declarative deployment management with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#deployment docker
  image: node:18-alpine
  ports: ["3000:3000"]
  environment:
    NODE_ENV: production
    PORT: 3000
  volumes: ["/app/data:/data"]
  health_check: /health
  restart_policy: unless-stopped

#deployment kubernetes
  replicas: 3
  resources:
    requests:
      memory: 256Mi
      cpu: 250m
    limits:
      memory: 512Mi
      cpu: 500m
  ingress:
    host: api.example.com
    tls: true
  autoscaling:
    min: 2
    max: 10
    target_cpu: 70

#deployment aws
  region: us-east-1
  instance_type: t3.micro
  security_groups: ["sg-12345678"]
  load_balancer: true
  auto_scaling: true
  ssl_certificate: arn:aws:acm:us-east-1:123456789012:certificate/abcd1234

#deployment heroku
  app_name: my-tusklang-app
  buildpacks: ["heroku/nodejs"]
  config_vars:
    NODE_ENV: production
    DATABASE_URL: ${DATABASE_URL}
  addons:
    - plan: heroku-postgresql:hobby-dev
    - plan: heroku-redis:hobby-dev
```

## JavaScript Integration

### Docker Deployment Handler

```javascript
// docker-deployment-handler.js
const Docker = require('dockerode');
const fs = require('fs');
const path = require('path');

class DockerDeploymentHandler {
  constructor(config) {
    this.config = config;
    this.docker = new Docker();
    this.image = config.image || 'node:18-alpine';
    this.ports = config.ports || ['3000:3000'];
    this.environment = config.environment || {};
    this.volumes = config.volumes || [];
    this.healthCheck = config.health_check;
    this.restartPolicy = config.restart_policy || 'unless-stopped';
  }

  async buildImage(dockerfilePath = 'Dockerfile', tag = 'app:latest') {
    try {
      const stream = await this.docker.buildImage({
        context: process.cwd(),
        src: ['Dockerfile', 'package.json', 'src/']
      }, { t: tag });

      return new Promise((resolve, reject) => {
        this.docker.modem.followProgress(stream, (err, res) => {
          if (err) reject(err);
          else resolve(res);
        });
      });
    } catch (error) {
      throw new Error(`Failed to build image: ${error.message}`);
    }
  }

  async runContainer(containerName = 'tusklang-app') {
    try {
      const portBindings = {};
      this.ports.forEach(portMapping => {
        const [hostPort, containerPort] = portMapping.split(':');
        portBindings[containerPort] = [{ HostPort: hostPort }];
      });

      const volumeBindings = {};
      this.volumes.forEach(volumeMapping => {
        const [hostPath, containerPath] = volumeMapping.split(':');
        volumeBindings[hostPath] = { bind: containerPath, mode: 'rw' };
      });

      const container = await this.docker.createContainer({
        Image: this.image,
        name: containerName,
        Env: Object.entries(this.environment).map(([key, value]) => `${key}=${value}`),
        ExposedPorts: this.ports.reduce((acc, portMapping) => {
          const containerPort = portMapping.split(':')[1];
          acc[`${containerPort}/tcp`] = {};
          return acc;
        }, {}),
        HostConfig: {
          PortBindings: portBindings,
          Binds: this.volumes,
          RestartPolicy: {
            Name: this.restartPolicy
          }
        },
        Healthcheck: this.healthCheck ? {
          Test: ['CMD', 'curl', '-f', this.healthCheck],
          Interval: 30000000000, // 30 seconds
          Timeout: 3000000000,    // 3 seconds
          Retries: 3
        } : undefined
      });

      await container.start();
      return container;
    } catch (error) {
      throw new Error(`Failed to run container: ${error.message}`);
    }
  }

  async stopContainer(containerName = 'tusklang-app') {
    try {
      const container = this.docker.getContainer(containerName);
      await container.stop();
      await container.remove();
    } catch (error) {
      throw new Error(`Failed to stop container: ${error.message}`);
    }
  }

  async getContainerStatus(containerName = 'tusklang-app') {
    try {
      const container = this.docker.getContainer(containerName);
      const info = await container.inspect();
      return {
        id: info.Id,
        name: info.Name,
        status: info.State.Status,
        running: info.State.Running,
        ports: info.NetworkSettings.Ports,
        health: info.State.Health
      };
    } catch (error) {
      return { error: error.message };
    }
  }

  async listContainers() {
    try {
      const containers = await this.docker.listContainers({ all: true });
      return containers.map(container => ({
        id: container.Id,
        name: container.Names[0],
        image: container.Image,
        status: container.Status,
        ports: container.Ports
      }));
    } catch (error) {
      throw new Error(`Failed to list containers: ${error.message}`);
    }
  }

  createDockerfile() {
    const dockerfile = `
FROM ${this.image}

WORKDIR /app

COPY package*.json ./
RUN npm ci --only=production

COPY . .

EXPOSE ${this.ports[0].split(':')[1]}

CMD ["npm", "start"]
    `.trim();

    fs.writeFileSync('Dockerfile', dockerfile);
    return 'Dockerfile';
  }
}

module.exports = DockerDeploymentHandler;
```

### Kubernetes Deployment Handler

```javascript
// kubernetes-deployment-handler.js
const k8s = require('@kubernetes/client-node');

class KubernetesDeploymentHandler {
  constructor(config) {
    this.config = config;
    this.kc = new k8s.KubeConfig();
    this.kc.loadFromDefault();
    this.k8sApi = this.kc.makeApiClient(k8s.AppsV1Api);
    this.k8sCoreApi = this.kc.makeApiClient(k8s.CoreV1Api);
    this.k8sNetworkingApi = this.kc.makeApiClient(k8s.NetworkingV1Api);
    
    this.replicas = config.replicas || 3;
    this.resources = config.resources || {};
    this.ingress = config.ingress;
    this.autoscaling = config.autoscaling;
  }

  async createDeployment(name, image, namespace = 'default') {
    try {
      const deployment = {
        apiVersion: 'apps/v1',
        kind: 'Deployment',
        metadata: {
          name: name,
          namespace: namespace
        },
        spec: {
          replicas: this.replicas,
          selector: {
            matchLabels: {
              app: name
            }
          },
          template: {
            metadata: {
              labels: {
                app: name
              }
            },
            spec: {
              containers: [{
                name: name,
                image: image,
                ports: [{
                  containerPort: 3000
                }],
                resources: this.resources,
                livenessProbe: {
                  httpGet: {
                    path: '/health',
                    port: 3000
                  },
                  initialDelaySeconds: 30,
                  periodSeconds: 10
                },
                readinessProbe: {
                  httpGet: {
                    path: '/ready',
                    port: 3000
                  },
                  initialDelaySeconds: 5,
                  periodSeconds: 5
                }
              }]
            }
          }
        }
      };

      const result = await this.k8sApi.createNamespacedDeployment(namespace, deployment);
      return result.body;
    } catch (error) {
      throw new Error(`Failed to create deployment: ${error.message}`);
    }
  }

  async createService(name, namespace = 'default') {
    try {
      const service = {
        apiVersion: 'v1',
        kind: 'Service',
        metadata: {
          name: `${name}-service`,
          namespace: namespace
        },
        spec: {
          selector: {
            app: name
          },
          ports: [{
            port: 80,
            targetPort: 3000
          }],
          type: 'ClusterIP'
        }
      };

      const result = await this.k8sCoreApi.createNamespacedService(namespace, service);
      return result.body;
    } catch (error) {
      throw new Error(`Failed to create service: ${error.message}`);
    }
  }

  async createIngress(name, host, namespace = 'default') {
    if (!this.ingress) return null;

    try {
      const ingress = {
        apiVersion: 'networking.k8s.io/v1',
        kind: 'Ingress',
        metadata: {
          name: `${name}-ingress`,
          namespace: namespace,
          annotations: {
            'kubernetes.io/ingress.class': 'nginx',
            'cert-manager.io/cluster-issuer': 'letsencrypt-prod'
          }
        },
        spec: {
          tls: this.ingress.tls ? [{
            hosts: [host],
            secretName: `${name}-tls`
          }] : undefined,
          rules: [{
            host: host,
            http: {
              paths: [{
                path: '/',
                pathType: 'Prefix',
                backend: {
                  service: {
                    name: `${name}-service`,
                    port: {
                      number: 80
                    }
                  }
                }
              }]
            }
          }]
        }
      };

      const result = await this.k8sNetworkingApi.createNamespacedIngress(namespace, ingress);
      return result.body;
    } catch (error) {
      throw new Error(`Failed to create ingress: ${error.message}`);
    }
  }

  async createHorizontalPodAutoscaler(name, namespace = 'default') {
    if (!this.autoscaling) return null;

    try {
      const hpa = {
        apiVersion: 'autoscaling/v2',
        kind: 'HorizontalPodAutoscaler',
        metadata: {
          name: `${name}-hpa`,
          namespace: namespace
        },
        spec: {
          scaleTargetRef: {
            apiVersion: 'apps/v1',
            kind: 'Deployment',
            name: name
          },
          minReplicas: this.autoscaling.min,
          maxReplicas: this.autoscaling.max,
          metrics: [{
            type: 'Resource',
            resource: {
              name: 'cpu',
              target: {
                type: 'Utilization',
                averageUtilization: this.autoscaling.target_cpu
              }
            }
          }]
        }
      };

      const result = await this.k8sApi.createNamespacedHorizontalPodAutoscaler(namespace, hpa);
      return result.body;
    } catch (error) {
      throw new Error(`Failed to create HPA: ${error.message}`);
    }
  }

  async deploy(name, image, namespace = 'default') {
    try {
      await this.createDeployment(name, image, namespace);
      await this.createService(name, namespace);
      await this.createIngress(name, this.ingress?.host, namespace);
      await this.createHorizontalPodAutoscaler(name, namespace);
      
      return { success: true, message: 'Deployment completed successfully' };
    } catch (error) {
      throw new Error(`Deployment failed: ${error.message}`);
    }
  }

  async getDeploymentStatus(name, namespace = 'default') {
    try {
      const result = await this.k8sApi.readNamespacedDeployment(name, namespace);
      const deployment = result.body;
      
      return {
        name: deployment.metadata.name,
        replicas: deployment.spec.replicas,
        availableReplicas: deployment.status.availableReplicas,
        readyReplicas: deployment.status.readyReplicas,
        updatedReplicas: deployment.status.updatedReplicas,
        conditions: deployment.status.conditions
      };
    } catch (error) {
      return { error: error.message };
    }
  }

  async scaleDeployment(name, replicas, namespace = 'default') {
    try {
      const result = await this.k8sApi.patchNamespacedDeployment(
        name,
        namespace,
        {
          spec: {
            replicas: replicas
          }
        },
        undefined,
        undefined,
        undefined,
        undefined,
        { headers: { 'Content-Type': 'application/strategic-merge-patch+json' } }
      );
      
      return result.body;
    } catch (error) {
      throw new Error(`Failed to scale deployment: ${error.message}`);
    }
  }
}

module.exports = KubernetesDeploymentHandler;
```

### AWS Deployment Handler

```javascript
// aws-deployment-handler.js
const AWS = require('aws-sdk');

class AWSDeploymentHandler {
  constructor(config) {
    this.config = config;
    this.region = config.region || 'us-east-1';
    this.instanceType = config.instance_type || 't3.micro';
    this.securityGroups = config.security_groups || [];
    this.loadBalancer = config.load_balancer || false;
    this.autoScaling = config.auto_scaling || false;
    this.sslCertificate = config.ssl_certificate;
    
    AWS.config.update({ region: this.region });
    this.ec2 = new AWS.EC2();
    this.elbv2 = new AWS.ELBv2();
    this.autoscaling = new AWS.AutoScaling();
    this.iam = new AWS.IAM();
  }

  async createLaunchTemplate(name, imageId, userData) {
    try {
      const params = {
        LaunchTemplateName: name,
        VersionDescription: 'Initial version',
        LaunchTemplateData: {
          ImageId: imageId,
          InstanceType: this.instanceType,
          SecurityGroupIds: this.securityGroups,
          UserData: Buffer.from(userData).toString('base64'),
          IamInstanceProfile: {
            Name: `${name}-instance-profile`
          }
        }
      };

      const result = await this.ec2.createLaunchTemplate(params).promise();
      return result.LaunchTemplate;
    } catch (error) {
      throw new Error(`Failed to create launch template: ${error.message}`);
    }
  }

  async createLoadBalancer(name, subnets) {
    if (!this.loadBalancer) return null;

    try {
      const params = {
        Name: name,
        Subnets: subnets,
        SecurityGroups: this.securityGroups,
        Scheme: 'internet-facing',
        Type: 'application'
      };

      const result = await this.elbv2.createLoadBalancer(params).promise();
      return result.LoadBalancers[0];
    } catch (error) {
      throw new Error(`Failed to create load balancer: ${error.message}`);
    }
  }

  async createTargetGroup(name, vpcId) {
    try {
      const params = {
        Name: name,
        Protocol: 'HTTP',
        Port: 3000,
        VpcId: vpcId,
        TargetType: 'instance',
        HealthCheckProtocol: 'HTTP',
        HealthCheckPath: '/health',
        HealthCheckIntervalSeconds: 30,
        HealthCheckTimeoutSeconds: 5,
        HealthyThresholdCount: 2,
        UnhealthyThresholdCount: 2
      };

      const result = await this.elbv2.createTargetGroup(params).promise();
      return result.TargetGroups[0];
    } catch (error) {
      throw new Error(`Failed to create target group: ${error.message}`);
    }
  }

  async createAutoScalingGroup(name, launchTemplateId, targetGroupArn, subnets) {
    if (!this.autoScaling) return null;

    try {
      const params = {
        AutoScalingGroupName: name,
        LaunchTemplate: {
          LaunchTemplateId: launchTemplateId,
          Version: '$Latest'
        },
        MinSize: 2,
        MaxSize: 10,
        DesiredCapacity: 3,
        VPCZoneIdentifier: subnets,
        TargetGroupARNs: [targetGroupArn],
        HealthCheckType: 'ELB',
        HealthCheckGracePeriod: 300
      };

      const result = await this.autoscaling.createAutoScalingGroup(params).promise();
      return result;
    } catch (error) {
      throw new Error(`Failed to create auto scaling group: ${error.message}`);
    }
  }

  async deploy(name, imageId, userData, vpcId, subnets) {
    try {
      // Create launch template
      const launchTemplate = await this.createLaunchTemplate(name, imageId, userData);
      
      // Create load balancer if enabled
      let loadBalancer = null;
      if (this.loadBalancer) {
        loadBalancer = await this.createLoadBalancer(name, subnets);
      }
      
      // Create target group
      const targetGroup = await this.createTargetGroup(name, vpcId);
      
      // Create auto scaling group if enabled
      let autoScalingGroup = null;
      if (this.autoScaling) {
        autoScalingGroup = await this.createAutoScalingGroup(
          name,
          launchTemplate.LaunchTemplateId,
          targetGroup.TargetGroupArn,
          subnets
        );
      }
      
      return {
        success: true,
        launchTemplate,
        loadBalancer,
        targetGroup,
        autoScalingGroup
      };
    } catch (error) {
      throw new Error(`Deployment failed: ${error.message}`);
    }
  }

  async getDeploymentStatus(name) {
    try {
      const [asgResult, lbResult] = await Promise.all([
        this.autoscaling.describeAutoScalingGroups({
          AutoScalingGroupNames: [name]
        }).promise(),
        this.elbv2.describeLoadBalancers({
          Names: [name]
        }).promise()
      ]);

      return {
        autoScalingGroup: asgResult.AutoScalingGroups[0] || null,
        loadBalancer: lbResult.LoadBalancers[0] || null
      };
    } catch (error) {
      return { error: error.message };
    }
  }

  async scaleDeployment(name, desiredCapacity) {
    try {
      const params = {
        AutoScalingGroupName: name,
        DesiredCapacity: desiredCapacity
      };

      await this.autoscaling.setDesiredCapacity(params).promise();
      return { success: true };
    } catch (error) {
      throw new Error(`Failed to scale deployment: ${error.message}`);
    }
  }
}

module.exports = AWSDeploymentHandler;
```

### Heroku Deployment Handler

```javascript
// heroku-deployment-handler.js
const { execSync } = require('child_process');

class HerokuDeploymentHandler {
  constructor(config) {
    this.config = config;
    this.appName = config.app_name;
    this.buildpacks = config.buildpacks || ['heroku/nodejs'];
    this.configVars = config.config_vars || {};
    this.addons = config.addons || [];
  }

  async createApp() {
    try {
      const command = `heroku create ${this.appName} --json`;
      const result = execSync(command, { encoding: 'utf8' });
      return JSON.parse(result);
    } catch (error) {
      throw new Error(`Failed to create Heroku app: ${error.message}`);
    }
  }

  async setBuildpacks() {
    try {
      for (const buildpack of this.buildpacks) {
        const command = `heroku buildpacks:add ${buildpack} --app ${this.appName}`;
        execSync(command, { encoding: 'utf8' });
      }
    } catch (error) {
      throw new Error(`Failed to set buildpacks: ${error.message}`);
    }
  }

  async setConfigVars() {
    try {
      for (const [key, value] of Object.entries(this.configVars)) {
        const command = `heroku config:set ${key}="${value}" --app ${this.appName}`;
        execSync(command, { encoding: 'utf8' });
      }
    } catch (error) {
      throw new Error(`Failed to set config vars: ${error.message}`);
    }
  }

  async addAddons() {
    try {
      for (const addon of this.addons) {
        const command = `heroku addons:create ${addon.plan} --app ${this.appName}`;
        execSync(command, { encoding: 'utf8' });
      }
    } catch (error) {
      throw new Error(`Failed to add addons: ${error.message}`);
    }
  }

  async deploy() {
    try {
      // Create app if it doesn't exist
      await this.createApp();
      
      // Set buildpacks
      await this.setBuildpacks();
      
      // Set config vars
      await this.setConfigVars();
      
      // Add addons
      await this.addAddons();
      
      // Deploy
      const command = `git push heroku main --app ${this.appName}`;
      execSync(command, { encoding: 'utf8' });
      
      return { success: true, appName: this.appName };
    } catch (error) {
      throw new Error(`Deployment failed: ${error.message}`);
    }
  }

  async getAppInfo() {
    try {
      const command = `heroku apps:info --app ${this.appName} --json`;
      const result = execSync(command, { encoding: 'utf8' });
      return JSON.parse(result);
    } catch (error) {
      return { error: error.message };
    }
  }

  async getLogs() {
    try {
      const command = `heroku logs --app ${this.appName}`;
      const result = execSync(command, { encoding: 'utf8' });
      return result;
    } catch (error) {
      throw new Error(`Failed to get logs: ${error.message}`);
    }
  }

  async scaleDynos(processType, quantity) {
    try {
      const command = `heroku ps:scale ${processType}=${quantity} --app ${this.appName}`;
      execSync(command, { encoding: 'utf8' });
      return { success: true };
    } catch (error) {
      throw new Error(`Failed to scale dynos: ${error.message}`);
    }
  }
}

module.exports = HerokuDeploymentHandler;
```

## TypeScript Implementation

```typescript
// deployment-handler.types.ts
export interface DeploymentConfig {
  image?: string;
  ports?: string[];
  environment?: Record<string, string>;
  volumes?: string[];
  health_check?: string;
  restart_policy?: string;
  replicas?: number;
  resources?: {
    requests?: {
      memory?: string;
      cpu?: string;
    };
    limits?: {
      memory?: string;
      cpu?: string;
    };
  };
  ingress?: {
    host: string;
    tls?: boolean;
  };
  autoscaling?: {
    min: number;
    max: number;
    target_cpu: number;
  };
  region?: string;
  instance_type?: string;
  security_groups?: string[];
  load_balancer?: boolean;
  auto_scaling?: boolean;
  ssl_certificate?: string;
  app_name?: string;
  buildpacks?: string[];
  config_vars?: Record<string, string>;
  addons?: Array<{ plan: string }>;
}

export interface DeploymentResult {
  success: boolean;
  message?: string;
  resources?: any[];
  error?: string;
}

export interface DeploymentHandler {
  deploy(name: string, ...args: any[]): Promise<DeploymentResult>;
  getStatus(name: string): Promise<any>;
  scale(name: string, replicas: number): Promise<any>;
}

// deployment-handler.ts
import { DeploymentConfig, DeploymentHandler, DeploymentResult } from './deployment-handler.types';

export class TypeScriptDeploymentHandler implements DeploymentHandler {
  protected config: DeploymentConfig;

  constructor(config: DeploymentConfig) {
    this.config = config;
  }

  async deploy(name: string, ...args: any[]): Promise<DeploymentResult> {
    throw new Error('Method not implemented');
  }

  async getStatus(name: string): Promise<any> {
    throw new Error('Method not implemented');
  }

  async scale(name: string, replicas: number): Promise<any> {
    throw new Error('Method not implemented');
  }
}

export class TypeScriptDockerHandler extends TypeScriptDeploymentHandler {
  private docker: any;

  constructor(config: DeploymentConfig) {
    super(config);
    this.docker = require('dockerode');
  }

  async deploy(name: string, image: string): Promise<DeploymentResult> {
    try {
      const container = await this.docker.createContainer({
        Image: image,
        name: name,
        ExposedPorts: { '3000/tcp': {} },
        HostConfig: {
          PortBindings: { '3000/tcp': [{ HostPort: '3000' }] }
        }
      });

      await container.start();

      return {
        success: true,
        message: 'Container deployed successfully',
        resources: [container]
      };
    } catch (error: any) {
      return {
        success: false,
        error: error.message
      };
    }
  }

  async getStatus(name: string): Promise<any> {
    try {
      const container = this.docker.getContainer(name);
      const info = await container.inspect();
      return {
        id: info.Id,
        status: info.State.Status,
        running: info.State.Running
      };
    } catch (error: any) {
      return { error: error.message };
    }
  }

  async scale(name: string, replicas: number): Promise<any> {
    // Docker doesn't have built-in scaling, would need orchestration
    throw new Error('Scaling not supported for single container');
  }
}
```

## Advanced Usage Scenarios

### Multi-Platform Deployment

```javascript
// multi-platform-deployment.js
class MultiPlatformDeployment {
  constructor(configs) {
    this.handlers = new Map();
    this.initializeHandlers(configs);
  }

  initializeHandlers(configs) {
    if (configs.docker) {
      const DockerHandler = require('./docker-deployment-handler');
      this.handlers.set('docker', new DockerHandler(configs.docker));
    }

    if (configs.kubernetes) {
      const KubernetesHandler = require('./kubernetes-deployment-handler');
      this.handlers.set('kubernetes', new KubernetesHandler(configs.kubernetes));
    }

    if (configs.aws) {
      const AWSHandler = require('./aws-deployment-handler');
      this.handlers.set('aws', new AWSHandler(configs.aws));
    }

    if (configs.heroku) {
      const HerokuHandler = require('./heroku-deployment-handler');
      this.handlers.set('heroku', new HerokuHandler(configs.heroku));
    }
  }

  async deployToAll(name, image, ...args) {
    const results = {};
    
    for (const [platform, handler] of this.handlers.entries()) {
      try {
        results[platform] = await handler.deploy(name, image, ...args);
      } catch (error) {
        results[platform] = { success: false, error: error.message };
      }
    }
    
    return results;
  }

  async deployToPlatform(platform, name, image, ...args) {
    const handler = this.handlers.get(platform);
    if (!handler) {
      throw new Error(`Platform '${platform}' not found`);
    }
    
    return await handler.deploy(name, image, ...args);
  }

  async getStatusFromAll(name) {
    const results = {};
    
    for (const [platform, handler] of this.handlers.entries()) {
      try {
        results[platform] = await handler.getStatus(name);
      } catch (error) {
        results[platform] = { error: error.message };
      }
    }
    
    return results;
  }
}
```

### Blue-Green Deployment

```javascript
// blue-green-deployment.js
class BlueGreenDeployment {
  constructor(deploymentHandler) {
    this.handler = deploymentHandler;
  }

  async deploy(name, image, version) {
    const blueName = `${name}-blue`;
    const greenName = `${name}-green`;
    
    // Check which environment is currently active
    const blueStatus = await this.handler.getStatus(blueName);
    const greenStatus = await this.handler.getStatus(greenName);
    
    const activeEnv = blueStatus.running ? 'blue' : 'green';
    const targetEnv = activeEnv === 'blue' ? 'green' : 'blue';
    const targetName = `${name}-${targetEnv}`;
    
    // Deploy to inactive environment
    await this.handler.deploy(targetName, image);
    
    // Wait for health check
    await this.waitForHealth(targetName);
    
    // Switch traffic
    await this.switchTraffic(name, targetEnv);
    
    // Clean up old environment
    if (activeEnv === 'blue') {
      await this.handler.stopContainer(blueName);
    } else {
      await this.handler.stopContainer(greenName);
    }
    
    return { success: true, activeEnvironment: targetEnv };
  }

  async waitForHealth(containerName, maxAttempts = 30) {
    for (let i = 0; i < maxAttempts; i++) {
      const status = await this.handler.getStatus(containerName);
      if (status.running && status.health === 'healthy') {
        return true;
      }
      await new Promise(resolve => setTimeout(resolve, 2000));
    }
    throw new Error('Health check failed');
  }

  async switchTraffic(appName, targetEnv) {
    // Implementation depends on the deployment platform
    // This could involve updating load balancer rules, DNS, etc.
    console.log(`Switching traffic to ${targetEnv} environment`);
  }
}
```

## Real-World Examples

### Express.js Deployment Setup

```javascript
// express-deployment-setup.js
const express = require('express');
const MultiPlatformDeployment = require('./multi-platform-deployment');

class ExpressDeploymentSetup {
  constructor(app, config) {
    this.app = app;
    this.deployment = new MultiPlatformDeployment(config);
  }

  setupHealthChecks() {
    this.app.get('/health', (req, res) => {
      res.json({ status: 'healthy', timestamp: new Date().toISOString() });
    });

    this.app.get('/ready', (req, res) => {
      // Add readiness checks (database, cache, etc.)
      res.json({ status: 'ready' });
    });
  }

  async deploy(name, image, platform = 'docker') {
    return await this.deployment.deployToPlatform(platform, name, image);
  }

  async deployToAll(name, image) {
    return await this.deployment.deployToAll(name, image);
  }

  async getStatus(name, platform = 'docker') {
    return await this.deployment.getStatusFromAll(name);
  }
}

// Usage
const app = express();
const deploymentConfig = {
  docker: {
    image: 'node:18-alpine',
    ports: ['3000:3000'],
    environment: { NODE_ENV: 'production' }
  },
  kubernetes: {
    replicas: 3,
    resources: {
      requests: { memory: '256Mi', cpu: '250m' },
      limits: { memory: '512Mi', cpu: '500m' }
    }
  }
};

const deploymentSetup = new ExpressDeploymentSetup(app, deploymentConfig);
deploymentSetup.setupHealthChecks();
```

### Microservices Deployment

```javascript
// microservices-deployment.js
class MicroservicesDeployment {
  constructor(deploymentHandler) {
    this.handler = deploymentHandler;
    this.services = new Map();
  }

  addService(name, config) {
    this.services.set(name, config);
  }

  async deployAll() {
    const results = {};
    
    for (const [name, config] of this.services.entries()) {
      try {
        results[name] = await this.handler.deploy(name, config.image, config);
      } catch (error) {
        results[name] = { success: false, error: error.message };
      }
    }
    
    return results;
  }

  async deployService(name) {
    const config = this.services.get(name);
    if (!config) {
      throw new Error(`Service '${name}' not found`);
    }
    
    return await this.handler.deploy(name, config.image, config);
  }

  async getServiceStatus(name) {
    return await this.handler.getStatus(name);
  }

  async getAllStatuses() {
    const statuses = {};
    
    for (const [name] of this.services.entries()) {
      statuses[name] = await this.getServiceStatus(name);
    }
    
    return statuses;
  }
}

// Usage
const dockerHandler = new DockerDeploymentHandler({});
const microservices = new MicroservicesDeployment(dockerHandler);

microservices.addService('api', {
  image: 'api:latest',
  ports: ['3000:3000']
});

microservices.addService('worker', {
  image: 'worker:latest',
  ports: ['3001:3001']
});

microservices.addService('frontend', {
  image: 'frontend:latest',
  ports: ['80:80']
});

await microservices.deployAll();
```

## Performance Considerations

### Deployment Monitoring

```javascript
// deployment-monitor.js
class DeploymentMonitor {
  constructor(deploymentHandler) {
    this.handler = deploymentHandler;
    this.metrics = {
      deployments: 0,
      failures: 0,
      avgDeploymentTime: 0
    };
  }

  async monitorDeployment(name) {
    const start = Date.now();
    
    try {
      const result = await this.handler.deploy(name);
      const duration = Date.now() - start;
      
      this.metrics.deployments++;
      this.metrics.avgDeploymentTime = 
        (this.metrics.avgDeploymentTime * (this.metrics.deployments - 1) + duration) / this.metrics.deployments;
      
      return { ...result, duration };
    } catch (error) {
      this.metrics.failures++;
      throw error;
    }
  }

  getMetrics() {
    return this.metrics;
  }
}
```

### Deployment Caching

```javascript
// deployment-cache.js
class DeploymentCache {
  constructor() {
    this.cache = new Map();
  }

  get(key) {
    const entry = this.cache.get(key);
    if (!entry) return null;
    
    if (Date.now() > entry.expires) {
      this.cache.delete(key);
      return null;
    }
    
    return entry.value;
  }

  set(key, value, ttl = 300000) {
    this.cache.set(key, {
      value,
      expires: Date.now() + ttl
    });
  }

  clear() {
    this.cache.clear();
  }
}
```

## Security Notes

### Deployment Security

```javascript
// deployment-security.js
class DeploymentSecurity {
  constructor() {
    this.secrets = new Map();
  }

  encryptSecret(secret) {
    const crypto = require('crypto');
    const algorithm = 'aes-256-gcm';
    const key = crypto.randomBytes(32);
    const iv = crypto.randomBytes(16);
    
    const cipher = crypto.createCipher(algorithm, key);
    let encrypted = cipher.update(secret, 'utf8', 'hex');
    encrypted += cipher.final('hex');
    
    return {
      encrypted,
      key: key.toString('hex'),
      iv: iv.toString('hex')
    };
  }

  validateDeploymentConfig(config) {
    const issues = [];
    
    // Check for hardcoded secrets
    if (config.environment) {
      for (const [key, value] of Object.entries(config.environment)) {
        if (key.toLowerCase().includes('secret') || key.toLowerCase().includes('password')) {
          if (value && !value.startsWith('${') && !value.startsWith('@env')) {
            issues.push(`Hardcoded secret found in environment variable: ${key}`);
          }
        }
      }
    }
    
    return issues;
  }
}
```

## Best Practices

### Deployment Configuration Management

```javascript
// deployment-config-manager.js
class DeploymentConfigManager {
  constructor() {
    this.configs = new Map();
  }

  setConfig(environment, config) {
    this.configs.set(environment, this.validateConfig(config));
  }

  getConfig(environment) {
    const config = this.configs.get(environment);
    if (!config) {
      throw new Error(`No configuration found for environment: ${environment}`);
    }
    return config;
  }

  validateConfig(config) {
    if (!config.platform) {
      throw new Error('Deployment platform is required');
    }
    
    return config;
  }

  getCurrentConfig() {
    const environment = process.env.NODE_ENV || 'development';
    return this.getConfig(environment);
  }
}
```

### Deployment Health Monitoring

```javascript
// deployment-health-monitor.js
class DeploymentHealthMonitor {
  constructor(deploymentHandler) {
    this.handler = deploymentHandler;
    this.metrics = {
      deployments: 0,
      failures: 0,
      avgDeploymentTime: 0
    };
  }

  async checkHealth() {
    try {
      const testDeployment = await this.handler.deploy('health-check', 'nginx:alpine');
      return {
        status: 'healthy',
        metrics: this.metrics
      };
    } catch (error) {
      this.metrics.failures++;
      return {
        status: 'unhealthy',
        error: error.message,
        metrics: this.metrics
      };
    }
  }

  getMetrics() {
    return this.metrics;
  }
}
```

## Related Topics

- [@deploy Operator](./59-tsklang-javascript-operator-deploy.md)
- [@scale Operator](./60-tsklang-javascript-operator-scale.md)
- [@monitor Operator](./51-tsklang-javascript-operator-monitor.md)
- [@metrics Operator](./49-tsklang-javascript-operator-metrics.md)
- [@monitoring Directive](./87-tsklang-javascript-directives-monitoring.md) 