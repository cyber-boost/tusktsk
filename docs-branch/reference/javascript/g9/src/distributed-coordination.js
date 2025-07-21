/**
 * Distributed System Coordination and Service Discovery
 * Goal 9.3 Implementation
 */

const EventEmitter = require("events");

class DistributedCoordination extends EventEmitter {
    constructor(options = {}) {
        super();
        this.services = new Map();
        this.nodes = new Map();
        this.loadBalancers = new Map();
        this.heartbeats = new Map();
        this.heartbeatInterval = options.heartbeatInterval || 5000;
        this.serviceTimeout = options.serviceTimeout || 30000;
        this.maxRetries = options.maxRetries || 3;
        this.retryDelay = options.retryDelay || 1000;
        
        this.startHeartbeatMonitor();
    }

    /**
     * Register a service
     */
    registerService(serviceId, serviceInfo, options = {}) {
        if (this.services.has(serviceId)) {
            throw new Error(`Service ${serviceId} already registered`);
        }

        const service = {
            id: serviceId,
            info: serviceInfo,
            status: "active",
            registeredAt: Date.now(),
            lastHeartbeat: Date.now(),
            endpoints: serviceInfo.endpoints || [],
            health: {
                status: "healthy",
                lastCheck: Date.now(),
                failures: 0
            },
            metadata: serviceInfo.metadata || {},
            options: {
                maxRetries: options.maxRetries || this.maxRetries,
                retryDelay: options.retryDelay || this.retryDelay,
                loadBalancing: options.loadBalancing || "round-robin"
            }
        };

        this.services.set(serviceId, service);
        console.log(`✓ Service registered: ${serviceId}`);
        this.emit("serviceRegistered", { serviceId, serviceInfo });
        
        return true;
    }

    /**
     * Discover services
     */
    discoverServices(criteria = {}) {
        const discovered = [];

        for (const [serviceId, service] of this.services) {
            if (service.status !== "active") continue;

            // Check if service matches criteria
            if (this.matchesCriteria(service, criteria)) {
                discovered.push({
                    id: serviceId,
                    info: service.info,
                    health: service.health,
                    metadata: service.metadata
                });
            }
        }

        return discovered;
    }

    /**
     * Get service by ID
     */
    getService(serviceId) {
        const service = this.services.get(serviceId);
        if (!service || service.status !== "active") {
            return null;
        }
        return service;
    }

    /**
     * Add node to cluster
     */
    addNode(nodeId, nodeInfo, options = {}) {
        if (this.nodes.has(nodeId)) {
            throw new Error(`Node ${nodeId} already exists`);
        }

        const node = {
            id: nodeId,
            info: nodeInfo,
            status: "active",
            addedAt: Date.now(),
            lastHeartbeat: Date.now(),
            services: new Set(),
            load: {
                cpu: 0,
                memory: 0,
                connections: 0
            },
            metadata: nodeInfo.metadata || {},
            options: {
                maxLoad: options.maxLoad || 0.8,
                autoScaling: options.autoScaling || false
            }
        };

        this.nodes.set(nodeId, node);
        console.log(`✓ Node added: ${nodeId}`);
        this.emit("nodeAdded", { nodeId, nodeInfo });
        
        return true;
    }

    /**
     * Remove node from cluster
     */
    removeNode(nodeId) {
        const node = this.nodes.get(nodeId);
        if (!node) {
            return false;
        }

        // Migrate services to other nodes
        for (const serviceId of node.services) {
            this.migrateService(serviceId, nodeId);
        }

        this.nodes.delete(nodeId);
        console.log(`✓ Node removed: ${nodeId}`);
        this.emit("nodeRemoved", { nodeId });
        
        return true;
    }

    /**
     * Migrate service to different node
     */
    async migrateService(serviceId, fromNodeId, toNodeId = null) {
        const service = this.services.get(serviceId);
        if (!service) {
            throw new Error(`Service ${serviceId} not found`);
        }

        // Find target node if not specified
        if (!toNodeId) {
            toNodeId = this.findBestNode(service);
        }

        if (!toNodeId) {
            throw new Error(`No suitable node found for service ${serviceId}`);
        }

        const targetNode = this.nodes.get(toNodeId);
        if (!targetNode) {
            throw new Error(`Target node ${toNodeId} not found`);
        }

        // Update service endpoint
        service.info.endpoints = service.info.endpoints.map(endpoint => {
            if (endpoint.includes(fromNodeId)) {
                return endpoint.replace(fromNodeId, toNodeId);
            }
            return endpoint;
        });

        // Update node service lists
        const fromNode = this.nodes.get(fromNodeId);
        if (fromNode) {
            fromNode.services.delete(serviceId);
        }
        targetNode.services.add(serviceId);

        console.log(`✓ Service ${serviceId} migrated from ${fromNodeId} to ${toNodeId}`);
        this.emit("serviceMigrated", { serviceId, fromNodeId, toNodeId });
        
        return true;
    }

    /**
     * Create load balancer
     */
    createLoadBalancer(balancerId, options = {}) {
        if (this.loadBalancers.has(balancerId)) {
            throw new Error(`Load balancer ${balancerId} already exists`);
        }

        const balancer = {
            id: balancerId,
            algorithm: options.algorithm || "round-robin",
            services: new Set(),
            currentIndex: 0,
            healthChecks: options.healthChecks || true,
            stickySessions: options.stickySessions || false,
            sessionTimeout: options.sessionTimeout || 300000,
            createdAt: Date.now(),
            stats: {
                requests: 0,
                errors: 0,
                lastRequest: null
            }
        };

        this.loadBalancers.set(balancerId, balancer);
        console.log(`✓ Load balancer created: ${balancerId}`);
        this.emit("loadBalancerCreated", { balancerId, options });
        
        return true;
    }

    /**
     * Add service to load balancer
     */
    addServiceToBalancer(balancerId, serviceId) {
        const balancer = this.loadBalancers.get(balancerId);
        const service = this.services.get(serviceId);

        if (!balancer) {
            throw new Error(`Load balancer ${balancerId} not found`);
        }
        if (!service) {
            throw new Error(`Service ${serviceId} not found`);
        }

        balancer.services.add(serviceId);
        console.log(`✓ Service ${serviceId} added to load balancer ${balancerId}`);
        this.emit("serviceAddedToBalancer", { balancerId, serviceId });
        
        return true;
    }

    /**
     * Route request through load balancer
     */
    routeRequest(balancerId, request, options = {}) {
        const balancer = this.loadBalancers.get(balancerId);
        if (!balancer) {
            throw new Error(`Load balancer ${balancerId} not found`);
        }

        const availableServices = Array.from(balancer.services)
            .map(serviceId => this.services.get(serviceId))
            .filter(service => service && service.status === "active" && service.health.status === "healthy");

        if (availableServices.length === 0) {
            throw new Error(`No healthy services available in load balancer ${balancerId}`);
        }

        let selectedService;
        switch (balancer.algorithm) {
            case "round-robin":
                selectedService = availableServices[balancer.currentIndex % availableServices.length];
                balancer.currentIndex++;
                break;
            case "least-connections":
                selectedService = availableServices.reduce((min, service) => 
                    service.load.connections < min.load.connections ? service : min
                );
                break;
            case "random":
                selectedService = availableServices[Math.floor(Math.random() * availableServices.length)];
                break;
            default:
                selectedService = availableServices[0];
        }

        balancer.stats.requests++;
        balancer.stats.lastRequest = Date.now();

        this.emit("requestRouted", { balancerId, serviceId: selectedService.id, request });
        return selectedService;
    }

    /**
     * Update service health
     */
    updateServiceHealth(serviceId, healthStatus) {
        const service = this.services.get(serviceId);
        if (!service) {
            return false;
        }

        service.health.status = healthStatus.status || service.health.status;
        service.health.lastCheck = Date.now();
        service.health.failures = healthStatus.failures || service.health.failures;

        this.emit("healthUpdated", { serviceId, health: service.health });
        return true;
    }

    /**
     * Send heartbeat
     */
    sendHeartbeat(nodeId, heartbeatData = {}) {
        const node = this.nodes.get(nodeId);
        if (!node) {
            return false;
        }

        node.lastHeartbeat = Date.now();
        node.load = heartbeatData.load || node.load;

        this.heartbeats.set(nodeId, {
            timestamp: Date.now(),
            data: heartbeatData
        });

        this.emit("heartbeatReceived", { nodeId, heartbeatData });
        return true;
    }

    /**
     * Start heartbeat monitoring
     */
    startHeartbeatMonitor() {
        setInterval(() => {
            this.checkHeartbeats();
        }, this.heartbeatInterval);
    }

    /**
     * Check node heartbeats
     */
    checkHeartbeats() {
        const now = Date.now();

        for (const [nodeId, node] of this.nodes) {
            if (now - node.lastHeartbeat > this.serviceTimeout) {
                console.warn(`Node ${nodeId} heartbeat timeout`);
                this.emit("nodeTimeout", { nodeId, lastHeartbeat: node.lastHeartbeat });
                
                // Mark node as inactive
                node.status = "inactive";
            }
        }
    }

    /**
     * Find best node for service
     */
    findBestNode(service) {
        const availableNodes = Array.from(this.nodes.values())
            .filter(node => node.status === "active" && node.load.cpu < node.options.maxLoad);

        if (availableNodes.length === 0) {
            return null;
        }

        // Simple load balancing: choose node with lowest CPU usage
        return availableNodes.reduce((best, node) => 
            node.load.cpu < best.load.cpu ? node : best
        ).id;
    }

    /**
     * Check if service matches criteria
     */
    matchesCriteria(service, criteria) {
        for (const [key, value] of Object.entries(criteria)) {
            if (key === "type" && service.info.type !== value) {
                return false;
            }
            if (key === "version" && service.info.version !== value) {
                return false;
            }
            if (key === "status" && service.health.status !== value) {
                return false;
            }
        }
        return true;
    }

    /**
     * Get cluster statistics
     */
    getStats() {
        const stats = {
            services: this.services.size,
            nodes: this.nodes.size,
            loadBalancers: this.loadBalancers.size,
            activeServices: 0,
            activeNodes: 0,
            totalLoad: 0
        };

        for (const service of this.services.values()) {
            if (service.status === "active") {
                stats.activeServices++;
            }
        }

        for (const node of this.nodes.values()) {
            if (node.status === "active") {
                stats.activeNodes++;
                stats.totalLoad += node.load.cpu;
            }
        }

        return stats;
    }

    /**
     * Clean up inactive services and nodes
     */
    cleanup() {
        const now = Date.now();
        let cleanedServices = 0;
        let cleanedNodes = 0;

        // Clean up inactive services
        for (const [serviceId, service] of this.services) {
            if (service.status === "inactive" || 
                (now - service.lastHeartbeat > this.serviceTimeout * 2)) {
                this.services.delete(serviceId);
                cleanedServices++;
            }
        }

        // Clean up inactive nodes
        for (const [nodeId, node] of this.nodes) {
            if (node.status === "inactive" || 
                (now - node.lastHeartbeat > this.serviceTimeout * 2)) {
                this.nodes.delete(nodeId);
                cleanedNodes++;
            }
        }

        console.log(`✓ Cleaned up ${cleanedServices} services and ${cleanedNodes} nodes`);
        return { services: cleanedServices, nodes: cleanedNodes };
    }
}

module.exports = { DistributedCoordination };
