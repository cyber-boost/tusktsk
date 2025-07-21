/**
 * Real-time Data Streaming and Event Processing Pipeline
 * Goal 9.2 Implementation
 */

const EventEmitter = require("events");

class DataStreaming extends EventEmitter {
    constructor(options = {}) {
        super();
        this.streams = new Map();
        this.processors = new Map();
        this.buffers = new Map();
        this.maxBufferSize = options.maxBufferSize || 1000;
        this.bufferTimeout = options.bufferTimeout || 1000;
        this.backpressureThreshold = options.backpressureThreshold || 0.8;
        this.processingWorkers = options.processingWorkers || 4;
        
        this.registerBuiltInProcessors();
    }

    /**
     * Create a new data stream
     */
    createStream(streamId, options = {}) {
        if (this.streams.has(streamId)) {
            throw new Error(`Stream ${streamId} already exists`);
        }

        const stream = {
            id: streamId,
            buffer: [],
            processors: [],
            subscribers: new Set(),
            status: "active",
            createdAt: Date.now(),
            lastActivity: Date.now(),
            options: {
                maxBufferSize: options.maxBufferSize || this.maxBufferSize,
                bufferTimeout: options.bufferTimeout || this.bufferTimeout,
                backpressureThreshold: options.backpressureThreshold || this.backpressureThreshold
            }
        };

        this.streams.set(streamId, stream);
        console.log(`✓ Stream created: ${streamId}`);
        this.emit("streamCreated", { streamId, options });
        
        return streamId;
    }

    /**
     * Add data to stream
     */
    async addData(streamId, data, options = {}) {
        const stream = this.streams.get(streamId);
        if (!stream) {
            throw new Error(`Stream ${streamId} not found`);
        }

        if (stream.status !== "active") {
            throw new Error(`Stream ${streamId} is not active`);
        }

        // Check backpressure
        const bufferUsage = stream.buffer.length / stream.options.maxBufferSize;
        if (bufferUsage > stream.options.backpressureThreshold) {
            this.emit("backpressure", { streamId, bufferUsage });
            
            if (options.waitForSpace) {
                await this.waitForBufferSpace(streamId);
            } else {
                throw new Error(`Backpressure detected: buffer usage ${(bufferUsage * 100).toFixed(1)}%`);
            }
        }

        // Add data to buffer
        const dataEntry = {
            id: this.generateDataId(),
            data,
            timestamp: Date.now(),
            metadata: options.metadata || {}
        };

        stream.buffer.push(dataEntry);
        stream.lastActivity = Date.now();

        // Process data if processors are attached
        if (stream.processors.length > 0) {
            await this.processStreamData(streamId, dataEntry);
        }

        // Notify subscribers
        this.notifySubscribers(streamId, dataEntry);

        this.emit("dataAdded", { streamId, dataId: dataEntry.id, dataSize: JSON.stringify(data).length });
        return dataEntry.id;
    }

    /**
     * Process stream data through processors
     */
    async processStreamData(streamId, dataEntry) {
        const stream = this.streams.get(streamId);
        if (!stream) return;

        for (const processorId of stream.processors) {
            const processor = this.processors.get(processorId);
            if (!processor) continue;

            try {
                const processedData = await processor.handler(dataEntry.data, {
                    streamId,
                    dataId: dataEntry.id,
                    metadata: dataEntry.metadata
                });

                // Update data entry with processed result
                dataEntry.processed = processedData;
                dataEntry.processedBy = processorId;

                this.emit("dataProcessed", {
                    streamId,
                    dataId: dataEntry.id,
                    processorId,
                    result: processedData
                });
            } catch (error) {
                this.emit("processingError", {
                    streamId,
                    dataId: dataEntry.id,
                    processorId,
                    error: error.message
                });
            }
        }
    }

    /**
     * Add processor to stream
     */
    addProcessor(streamId, processorId, options = {}) {
        const stream = this.streams.get(streamId);
        const processor = this.processors.get(processorId);

        if (!stream) {
            throw new Error(`Stream ${streamId} not found`);
        }
        if (!processor) {
            throw new Error(`Processor ${processorId} not found`);
        }

        stream.processors.push(processorId);
        console.log(`✓ Processor ${processorId} added to stream ${streamId}`);
        this.emit("processorAdded", { streamId, processorId });
        
        return true;
    }

    /**
     * Register a data processor
     */
    registerProcessor(processorId, handler, options = {}) {
        if (typeof handler !== "function") {
            throw new Error("Processor handler must be a function");
        }

        this.processors.set(processorId, {
            id: processorId,
            handler,
            options,
            registeredAt: Date.now(),
            usageCount: 0
        });

        console.log(`✓ Processor registered: ${processorId}`);
        this.emit("processorRegistered", { processorId });
        
        return true;
    }

    /**
     * Subscribe to stream
     */
    subscribeToStream(streamId, subscriber, options = {}) {
        const stream = this.streams.get(streamId);
        if (!stream) {
            throw new Error(`Stream ${streamId} not found`);
        }

        stream.subscribers.add(subscriber);
        console.log(`✓ Subscriber added to stream ${streamId}`);
        this.emit("subscriberAdded", { streamId, subscriber });
        
        return true;
    }

    /**
     * Unsubscribe from stream
     */
    unsubscribeFromStream(streamId, subscriber) {
        const stream = this.streams.get(streamId);
        if (!stream) {
            return false;
        }

        const removed = stream.subscribers.delete(subscriber);
        if (removed) {
            console.log(`✓ Subscriber removed from stream ${streamId}`);
            this.emit("subscriberRemoved", { streamId, subscriber });
        }
        
        return removed;
    }

    /**
     * Notify subscribers of new data
     */
    notifySubscribers(streamId, dataEntry) {
        const stream = this.streams.get(streamId);
        if (!stream) return;

        for (const subscriber of stream.subscribers) {
            try {
                if (typeof subscriber === "function") {
                    subscriber(dataEntry);
                } else if (subscriber.emit) {
                    subscriber.emit("data", dataEntry);
                }
            } catch (error) {
                console.error(`Subscriber notification failed: ${error.message}`);
            }
        }
    }

    /**
     * Get stream data
     */
    getStreamData(streamId, options = {}) {
        const stream = this.streams.get(streamId);
        if (!stream) {
            throw new Error(`Stream ${streamId} not found`);
        }

        const { limit = 100, offset = 0, filter } = options;
        let data = stream.buffer.slice(offset, offset + limit);

        if (filter) {
            data = data.filter(item => filter(item));
        }

        return data;
    }

    /**
     * Wait for buffer space
     */
    async waitForBufferSpace(streamId) {
        const stream = this.streams.get(streamId);
        if (!stream) return;

        return new Promise((resolve) => {
            const checkBuffer = () => {
                const bufferUsage = stream.buffer.length / stream.options.maxBufferSize;
                if (bufferUsage <= stream.options.backpressureThreshold) {
                    resolve();
                } else {
                    setTimeout(checkBuffer, 100);
                }
            };
            checkBuffer();
        });
    }

    /**
     * Register built-in processors
     */
    registerBuiltInProcessors() {
        // JSON Parser
        this.registerProcessor("json-parser", (data, context) => {
            if (typeof data === "string") {
                try {
                    return JSON.parse(data);
                } catch (error) {
                    throw new Error(`JSON parsing failed: ${error.message}`);
                }
            }
            return data;
        });

        // Data Filter
        this.registerProcessor("data-filter", (data, context) => {
            if (typeof data === "object" && data !== null) {
                return Object.fromEntries(
                    Object.entries(data).filter(([key, value]) => 
                        value !== null && value !== undefined
                    )
                );
            }
            return data;
        });

        // Data Transformer
        this.registerProcessor("data-transformer", (data, context) => {
            if (typeof data === "object" && data !== null) {
                return {
                    ...data,
                    _processed: true,
                    _timestamp: Date.now(),
                    _streamId: context.streamId
                };
            }
            return data;
        });

        // Analytics Aggregator
        this.registerProcessor("analytics-aggregator", (data, context) => {
            return {
                count: 1,
                sum: typeof data === "number" ? data : 0,
                average: typeof data === "number" ? data : 0,
                timestamp: Date.now(),
                streamId: context.streamId
            };
        });
    }

    /**
     * Generate unique data ID
     */
    generateDataId() {
        return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }

    /**
     * Get streaming statistics
     */
    getStats() {
        const stats = {
            streams: this.streams.size,
            processors: this.processors.size,
            totalSubscribers: 0,
            totalBufferSize: 0
        };

        for (const stream of this.streams.values()) {
            stats.totalSubscribers += stream.subscribers.size;
            stats.totalBufferSize += stream.buffer.length;
        }

        return stats;
    }

    /**
     * Clean up old data
     */
    cleanup(maxAge = 3600000) { // 1 hour default
        const now = Date.now();
        let cleanedCount = 0;

        for (const [streamId, stream] of this.streams) {
            const originalLength = stream.buffer.length;
            stream.buffer = stream.buffer.filter(entry => 
                now - entry.timestamp < maxAge
            );
            cleanedCount += originalLength - stream.buffer.length;
        }

        console.log(`✓ Cleaned up ${cleanedCount} old data entries`);
        return cleanedCount;
    }
}

module.exports = { DataStreaming };
