/**
 * Threat Detection and Security Monitoring Infrastructure
 * Goal 10.3 Implementation
 */

const EventEmitter = require("events");

class ThreatDetectionFramework extends EventEmitter {
    constructor(options = {}) {
        super();
        this.detectors = new Map();
        this.alerts = new Map();
        this.incidents = new Map();
        this.rules = new Map();
        this.monitors = new Map();
        this.alertThreshold = options.alertThreshold || 5;
        this.incidentThreshold = options.incidentThreshold || 10;
        this.monitoringInterval = options.monitoringInterval || 5000; // 5 seconds
        
        this.registerBuiltInDetectors();
        this.startMonitoring();
    }

    /**
     * Register a threat detector
     */
    registerDetector(name, detector) {
        if (typeof detector.detect !== "function") {
            throw new Error("Detector must have a detect method");
        }

        this.detectors.set(name, {
            ...detector,
            registeredAt: Date.now(),
            usageCount: 0,
            alerts: 0
        });

        console.log(`✓ Detector registered: ${name}`);
        this.emit("detectorRegistered", { name });
        
        return true;
    }

    /**
     * Add security rule
     */
    addRule(ruleId, rule) {
        if (typeof rule.evaluate !== "function") {
            throw new Error("Rule must have an evaluate method");
        }

        this.rules.set(ruleId, {
            id: ruleId,
            ...rule,
            createdAt: Date.now(),
            triggeredCount: 0
        });

        console.log(`✓ Security rule added: ${ruleId}`);
        this.emit("ruleAdded", { ruleId });
        
        return true;
    }

    /**
     * Monitor security event
     */
    async monitorEvent(event, options = {}) {
        const eventId = this.generateEventId();
        const timestamp = Date.now();
        
        const eventData = {
            id: eventId,
            type: event.type,
            source: event.source,
            data: event.data,
            timestamp,
            severity: event.severity || "medium",
            metadata: event.metadata || {}
        };

        // Evaluate rules
        const triggeredRules = [];
        for (const [ruleId, rule] of this.rules) {
            try {
                const result = await rule.evaluate(eventData);
                if (result.triggered) {
                    rule.triggeredCount++;
                    triggeredRules.push({ ruleId, result });
                }
            } catch (error) {
                console.error(`Rule evaluation failed: ${error.message}`);
            }
        }

        // Run detectors
        const detections = [];
        for (const [detectorName, detector] of this.detectors) {
            try {
                const detection = await detector.detect(eventData);
                if (detection.threat) {
                    detector.usageCount++;
                    detector.alerts++;
                    detections.push({ detector: detectorName, detection });
                }
            } catch (error) {
                console.error(`Detection failed: ${error.message}`);
            }
        }

        // Create alert if needed
        if (detections.length > 0 || triggeredRules.length > 0) {
            await this.createAlert(eventData, detections, triggeredRules);
        }

        this.emit("eventMonitored", { eventId, detections: detections.length, rules: triggeredRules.length });
        return { eventId, detections, triggeredRules };
    }

    /**
     * Create security alert
     */
    async createAlert(eventData, detections, triggeredRules) {
        const alertId = this.generateAlertId();
        
        const alert = {
            id: alertId,
            eventId: eventData.id,
            detections,
            triggeredRules,
            severity: this.calculateSeverity(detections, triggeredRules),
            timestamp: Date.now(),
            status: "active",
            metadata: {
                source: eventData.source,
                type: eventData.type
            }
        };

        this.alerts.set(alertId, alert);
        console.log(`🚨 Security alert created: ${alertId} (${alert.severity})`);
        this.emit("alertCreated", { alertId, severity: alert.severity });
        
        // Check if incident should be created
        if (this.shouldCreateIncident(alert)) {
            await this.createIncident(alert);
        }
        
        return alertId;
    }

    /**
     * Create security incident
     */
    async createIncident(alert) {
        const incidentId = this.generateIncidentId();
        
        const incident = {
            id: incidentId,
            alertId: alert.id,
            type: "security_incident",
            severity: alert.severity,
            status: "open",
            createdAt: Date.now(),
            alerts: [alert.id],
            description: `Security incident triggered by ${alert.detections.length} detections`,
            metadata: alert.metadata
        };

        this.incidents.set(incidentId, incident);
        console.log(`🚨🚨 Security incident created: ${incidentId}`);
        this.emit("incidentCreated", { incidentId, severity: incident.severity });
        
        return incidentId;
    }

    /**
     * Add security monitor
     */
    addMonitor(monitorId, monitor) {
        if (typeof monitor.check !== "function") {
            throw new Error("Monitor must have a check method");
        }

        this.monitors.set(monitorId, {
            id: monitorId,
            ...monitor,
            createdAt: Date.now(),
            lastCheck: null,
            alerts: 0
        });

        console.log(`✓ Security monitor added: ${monitorId}`);
        this.emit("monitorAdded", { monitorId });
        
        return true;
    }

    /**
     * Start monitoring
     */
    startMonitoring() {
        setInterval(async () => {
            await this.runMonitors();
        }, this.monitoringInterval);
    }

    /**
     * Run all monitors
     */
    async runMonitors() {
        for (const [monitorId, monitor] of this.monitors) {
            try {
                const result = await monitor.check();
                monitor.lastCheck = Date.now();
                
                if (result.alert) {
                    monitor.alerts++;
                    await this.monitorEvent({
                        type: "monitor_alert",
                        source: monitorId,
                        data: result.data,
                        severity: result.severity || "medium"
                    });
                }
            } catch (error) {
                console.error(`Monitor ${monitorId} failed: ${error.message}`);
            }
        }
    }

    /**
     * Register built-in detectors
     */
    registerBuiltInDetectors() {
        // Brute Force Detector
        this.registerDetector("brute_force", {
            detect: async (event) => {
                if (event.type === "login_attempt") {
                    // Check for multiple failed login attempts
                    const recentEvents = this.getRecentEvents(event.source, "login_attempt", 300000); // 5 minutes
                    const failedAttempts = recentEvents.filter(e => e.data.success === false);
                    
                    if (failedAttempts.length >= 5) {
                        return {
                            threat: true,
                            type: "brute_force_attack",
                            confidence: 0.8,
                            details: {
                                failedAttempts: failedAttempts.length,
                                timeWindow: "5 minutes"
                            }
                        };
                    }
                }
                return { threat: false };
            }
        });

        // Anomaly Detector
        this.registerDetector("anomaly", {
            detect: async (event) => {
                // Check for unusual patterns
                const recentEvents = this.getRecentEvents(event.source, event.type, 60000); // 1 minute
                
                if (recentEvents.length > 10) {
                    return {
                        threat: true,
                        type: "anomalous_activity",
                        confidence: 0.7,
                        details: {
                            eventCount: recentEvents.length,
                            timeWindow: "1 minute"
                        }
                    };
                }
                return { threat: false };
            }
        });

        // Data Exfiltration Detector
        this.registerDetector("data_exfiltration", {
            detect: async (event) => {
                if (event.type === "data_access" && event.data.size > 1000000) { // 1MB
                    return {
                        threat: true,
                        type: "potential_data_exfiltration",
                        confidence: 0.6,
                        details: {
                            dataSize: event.data.size,
                            threshold: 1000000
                        }
                    };
                }
                return { threat: false };
            }
        });
    }

    /**
     * Add built-in security rules
     */
    addBuiltInRules() {
        // Rate Limiting Rule
        this.addRule("rate_limiting", {
            name: "Rate Limiting",
            description: "Detect excessive requests",
            evaluate: async (event) => {
                const recentEvents = this.getRecentEvents(event.source, event.type, 60000);
                return {
                    triggered: recentEvents.length > 100,
                    details: {
                        eventCount: recentEvents.length,
                        threshold: 100
                    }
                };
            }
        });

        // Unauthorized Access Rule
        this.addRule("unauthorized_access", {
            name: "Unauthorized Access",
            description: "Detect unauthorized access attempts",
            evaluate: async (event) => {
                if (event.type === "access_attempt" && event.data.authorized === false) {
                    return {
                        triggered: true,
                        details: {
                            resource: event.data.resource,
                            user: event.data.user
                        }
                    };
                }
                return { triggered: false };
            }
        });
    }

    /**
     * Calculate alert severity
     */
    calculateSeverity(detections, triggeredRules) {
        const detectionCount = detections.length;
        const ruleCount = triggeredRules.length;
        
        if (detectionCount >= 3 || ruleCount >= 2) {
            return "critical";
        } else if (detectionCount >= 2 || ruleCount >= 1) {
            return "high";
        } else if (detectionCount >= 1) {
            return "medium";
        }
        return "low";
    }

    /**
     * Check if incident should be created
     */
    shouldCreateIncident(alert) {
        return alert.severity === "critical" || 
               (alert.detections.length + alert.triggeredRules.length) >= this.incidentThreshold;
    }

    /**
     * Get recent events
     */
    getRecentEvents(source, type, timeWindow) {
        const now = Date.now();
        const events = [];
        
        // This would typically query a database or event store
        // For now, we return an empty array as a placeholder
        return events;
    }

    /**
     * Generate unique IDs
     */
    generateEventId() {
        return `event_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }

    generateAlertId() {
        return `alert_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }

    generateIncidentId() {
        return `incident_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }

    /**
     * Get threat detection statistics
     */
    getStats() {
        return {
            detectors: this.detectors.size,
            rules: this.rules.size,
            monitors: this.monitors.size,
            activeAlerts: Array.from(this.alerts.values()).filter(a => a.status === "active").length,
            openIncidents: Array.from(this.incidents.values()).filter(i => i.status === "open").length,
            alertThreshold: this.alertThreshold,
            incidentThreshold: this.incidentThreshold
        };
    }

    /**
     * Get active alerts
     */
    getActiveAlerts() {
        return Array.from(this.alerts.values()).filter(alert => alert.status === "active");
    }

    /**
     * Get open incidents
     */
    getOpenIncidents() {
        return Array.from(this.incidents.values()).filter(incident => incident.status === "open");
    }

    /**
     * Resolve alert
     */
    resolveAlert(alertId, resolution = {}) {
        const alert = this.alerts.get(alertId);
        if (!alert) {
            throw new Error(`Alert ${alertId} not found`);
        }

        alert.status = "resolved";
        alert.resolvedAt = Date.now();
        alert.resolution = resolution;

        console.log(`✓ Alert resolved: ${alertId}`);
        this.emit("alertResolved", { alertId, resolution });
        
        return true;
    }

    /**
     * Close incident
     */
    closeIncident(incidentId, resolution = {}) {
        const incident = this.incidents.get(incidentId);
        if (!incident) {
            throw new Error(`Incident ${incidentId} not found`);
        }

        incident.status = "closed";
        incident.closedAt = Date.now();
        incident.resolution = resolution;

        console.log(`✓ Incident closed: ${incidentId}`);
        this.emit("incidentClosed", { incidentId, resolution });
        
        return true;
    }
}

module.exports = { ThreatDetectionFramework };
