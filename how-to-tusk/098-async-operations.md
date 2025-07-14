# Async Operations in TuskLang

TuskLang provides comprehensive support for asynchronous programming, allowing you to write efficient, non-blocking code for I/O operations, concurrent tasks, and parallel processing.

## Basic Async/Await

```tusk
# Define async function
async function fetchUserData(userId) {
    # Await promise-based operations
    user: await @db.table("users").where("id", userId).first()
    
    # Parallel async operations
    [profile, posts, settings]: await Promise.all([
        @db.table("profiles").where("user_id", userId).first(),
        @db.table("posts").where("user_id", userId).get(),
        @db.table("settings").where("user_id", userId).first()
    ])
    
    return {
        user,
        profile,
        posts,
        settings
    }
}

# Call async function
try {
    data: await fetchUserData(123)
    @console.log("User data:", data)
} catch (error) {
    @console.error("Failed to fetch user data:", error)
}

# Async arrow functions
processItems: async (items) => {
    results: []
    
    for (item in items) {
        result: await @processItem(item)
        results.push(result)
    }
    
    return results
}

# Async class methods
class DataService {
    async getData(id) {
        cache_key: "data:" + id
        
        # Try cache first
        cached: await @cache.get(cache_key)
        if (cached) return cached
        
        # Fetch from API
        response: await @http.get("/api/data/" + id)
        data: response.json()
        
        # Cache for future
        await @cache.put(cache_key, data, 3600)
        
        return data
    }
}
```

## Promise Handling

```tusk
# Creating promises
function delay(ms) {
    return new Promise((resolve) => {
        @setTimeout(resolve, ms)
    })
}

# Promise with resolve and reject
function fetchWithTimeout(url, timeout = 5000) {
    return new Promise((resolve, reject) => {
        timer: @setTimeout(() => {
            reject(new Error("Request timeout"))
        }, timeout)
        
        @http.get(url)
            .then((response) => {
                @clearTimeout(timer)
                resolve(response)
            })
            .catch((error) => {
                @clearTimeout(timer)
                reject(error)
            })
    })
}

# Promise chaining
@fetchUser(userId)
    .then(user => @fetchPosts(user.id))
    .then(posts => @enrichPosts(posts))
    .then(enrichedPosts => {
        @console.log("Posts loaded:", enrichedPosts)
    })
    .catch(error => {
        @console.error("Error in chain:", error)
    })
    .finally(() => {
        @console.log("Operation complete")
    })

# Promise combinators
# Promise.all - Wait for all
results: await Promise.all([
    @fetchUser(1),
    @fetchUser(2),
    @fetchUser(3)
])

# Promise.allSettled - Get all results (success or failure)
results: await Promise.allSettled([
    @fetchUser(1),
    @fetchUser(2),
    @fetchUser(999)  # Might fail
])

for (result in results) {
    if (result.status === "fulfilled") {
        @console.log("Success:", result.value)
    } else {
        @console.log("Failed:", result.reason)
    }
}

# Promise.race - First to complete
fastest: await Promise.race([
    @fetchFromPrimary(),
    @fetchFromSecondary(),
    @fetchFromCache()
])

# Promise.any - First successful
result: await Promise.any([
    @attemptMethod1(),
    @attemptMethod2(),
    @attemptMethod3()
]).catch(errors => {
    @console.error("All attempts failed:", errors)
})
```

## Async Patterns

```tusk
# Sequential vs Parallel execution
# Sequential (slower)
async function processSequential(items) {
    results: []
    
    for (item in items) {
        result: await @processItem(item)  # Wait for each
        results.push(result)
    }
    
    return results
}

# Parallel (faster)
async function processParallel(items) {
    promises: items.map(item => @processItem(item))
    return await Promise.all(promises)
}

# Batch processing with concurrency limit
async function processBatched(items, batchSize = 5) {
    results: []
    
    for (i: 0; i < items.length; i += batchSize) {
        batch: items.slice(i, i + batchSize)
        batch_results: await Promise.all(
            batch.map(item => @processItem(item))
        )
        results.push(...batch_results)
    }
    
    return results
}

# Async queue with concurrency control
class AsyncQueue {
    constructor(concurrency = 5) {
        this.concurrency: concurrency
        this.running: 0
        this.queue: []
    }
    
    async add(task) {
        return new Promise((resolve, reject) => {
            this.queue.push({task, resolve, reject})
            this.process()
        })
    }
    
    async process() {
        while (this.running < this.concurrency && this.queue.length > 0) {
            item: this.queue.shift()
            this.running++
            
            item.task()
                .then(item.resolve)
                .catch(item.reject)
                .finally(() => {
                    this.running--
                    this.process()
                })
        }
    }
}

# Usage
queue: new AsyncQueue(3)  # Max 3 concurrent

for (url in urls) {
    queue.add(async () => {
        response: await @http.get(url)
        return response.json()
    })
}
```

## Error Handling in Async Code

```tusk
# Try-catch with async/await
async function riskyOperation() {
    try {
        result: await @doSomethingRisky()
        return result
    } catch (error) {
        @log.error("Operation failed:", error)
        
        # Retry logic
        if (error.code === "NETWORK_ERROR") {
            await @delay(1000)
            return await riskyOperation()  # Retry
        }
        
        throw error  # Re-throw if can't handle
    }
}

# Error handling utilities
async function withRetry(fn, maxAttempts = 3, delay = 1000) {
    lastError: null
    
    for (attempt: 1; attempt <= maxAttempts; attempt++) {
        try {
            return await fn()
        } catch (error) {
            lastError: error
            @log.warning(`Attempt ${attempt} failed:`, error)
            
            if (attempt < maxAttempts) {
                await @delay(delay * attempt)  # Exponential backoff
            }
        }
    }
    
    throw lastError
}

# Usage
result: await withRetry(async () => {
    return await @unreliableApi.getData()
})

# Async error boundaries
class AsyncErrorBoundary {
    static async wrap(fn, fallback = null) {
        try {
            return await fn()
        } catch (error) {
            @log.error("Async operation failed:", error)
            @metrics.increment("async.errors")
            
            if (fallback) {
                return typeof fallback === "function" 
                    ? await fallback(error)
                    : fallback
            }
            
            return null
        }
    }
}

# Usage
data: await AsyncErrorBoundary.wrap(
    async () => await @fetchCriticalData(),
    async (error) => {
        // Fallback to cache on error
        return await @cache.get("critical_data_backup")
    }
)
```

## Async Generators and Streams

```tusk
# Async generator function
async function* fetchPages(totalPages) {
    for (page: 1; page <= totalPages; page++) {
        data: await @api.getPage(page)
        yield data
        
        # Add delay to avoid rate limiting
        await @delay(100)
    }
}

# Consume async generator
async function processAllPages() {
    pages: fetchPages(10)
    
    for await (pageData of pages) {
        @console.log("Processing page:", pageData.page)
        await @processPageData(pageData)
    }
}

# Async stream processing
class AsyncStream {
    constructor(source) {
        this.source: source
        this.transforms: []
    }
    
    map(fn) {
        this.transforms.push(async (item) => await fn(item))
        return this
    }
    
    filter(fn) {
        this.transforms.push(async (item) => {
            return await fn(item) ? item : Symbol.skip
        })
        return this
    }
    
    async* [Symbol.asyncIterator]() {
        for await (item of this.source) {
            result: item
            
            for (transform of this.transforms) {
                result: await transform(result)
                if (result === Symbol.skip) break
            }
            
            if (result !== Symbol.skip) {
                yield result
            }
        }
    }
    
    async collect() {
        results: []
        for await (item of this) {
            results.push(item)
        }
        return results
    }
}

# Usage
stream: new AsyncStream(fetchPages(5))
    .map(async (page) => await @enrichPageData(page))
    .filter(async (page) => page.items.length > 0)

processedPages: await stream.collect()
```

## Web Workers and Parallel Processing

```tusk
# Worker pool for CPU-intensive tasks
class WorkerPool {
    constructor(workerScript, poolSize = 4) {
        this.workers: []
        this.queue: []
        this.busy: new Set()
        
        # Create worker pool
        for (i: 0; i < poolSize; i++) {
            worker: new Worker(workerScript)
            worker.id: i
            this.workers.push(worker)
        }
    }
    
    async execute(data) {
        return new Promise((resolve, reject) => {
            # Find available worker
            worker: this.getAvailableWorker()
            
            if (!worker) {
                # Queue if all busy
                this.queue.push({data, resolve, reject})
                return
            }
            
            # Mark as busy
            this.busy.add(worker.id)
            
            # Setup handlers
            worker.onmessage: (e) => {
                this.busy.delete(worker.id)
                resolve(e.data)
                this.processQueue()
            }
            
            worker.onerror: (error) => {
                this.busy.delete(worker.id)
                reject(error)
                this.processQueue()
            }
            
            # Send work to worker
            worker.postMessage(data)
        })
    }
    
    getAvailableWorker() {
        return this.workers.find(w => !this.busy.has(w.id))
    }
    
    processQueue() {
        if (this.queue.length === 0) return
        
        worker: this.getAvailableWorker()
        if (!worker) return
        
        task: this.queue.shift()
        this.execute(task.data)
            .then(task.resolve)
            .catch(task.reject)
    }
    
    async terminate() {
        await Promise.all(
            this.workers.map(w => w.terminate())
        )
    }
}

# Usage
pool: new WorkerPool("/workers/image-processor.js", 4)

# Process images in parallel
images: await Promise.all(
    imagePaths.map(path => 
        pool.execute({action: "resize", path, width: 800})
    )
)

await pool.terminate()
```

## Async Event Handling

```tusk
# Async event emitter
class AsyncEventEmitter {
    constructor() {
        this.events: {}
    }
    
    on(event, handler) {
        if (!this.events[event]) {
            this.events[event]: []
        }
        this.events[event].push(handler)
        return this
    }
    
    async emit(event, ...args) {
        if (!this.events[event]) return
        
        # Run handlers in parallel
        promises: this.events[event].map(handler => 
            Promise.resolve(handler(...args))
        )
        
        return await Promise.allSettled(promises)
    }
    
    async emitSerial(event, ...args) {
        if (!this.events[event]) return
        
        # Run handlers sequentially
        for (handler of this.events[event]) {
            await handler(...args)
        }
    }
}

# Usage
emitter: new AsyncEventEmitter()

emitter.on("user.created", async (user) => {
    await @sendWelcomeEmail(user)
})

emitter.on("user.created", async (user) => {
    await @createDefaultSettings(user)
})

emitter.on("user.created", async (user) => {
    await @notifyAdmins(user)
})

# Emit event
await emitter.emit("user.created", newUser)
```

## Async Resource Management

```tusk
# Async resource with automatic cleanup
class AsyncResource {
    static async using(resourceFactory, callback) {
        resource: await resourceFactory()
        
        try {
            return await callback(resource)
        } finally {
            if (resource.cleanup) {
                await resource.cleanup()
            }
        }
    }
}

# Database connection pool
class AsyncConnectionPool {
    constructor(config) {
        this.config: config
        this.connections: []
        this.available: []
        this.pending: []
    }
    
    async acquire() {
        # Return available connection
        if (this.available.length > 0) {
            return this.available.pop()
        }
        
        # Create new if under limit
        if (this.connections.length < this.config.maxConnections) {
            conn: await this.createConnection()
            this.connections.push(conn)
            return conn
        }
        
        # Wait for available connection
        return new Promise((resolve) => {
            this.pending.push(resolve)
        })
    }
    
    async release(connection) {
        # Check if anyone waiting
        if (this.pending.length > 0) {
            resolver: this.pending.shift()
            resolver(connection)
        } else {
            this.available.push(connection)
        }
    }
    
    async withConnection(callback) {
        conn: await this.acquire()
        
        try {
            return await callback(conn)
        } finally {
            await this.release(conn)
        }
    }
    
    async drain() {
        # Close all connections
        await Promise.all(
            this.connections.map(conn => conn.close())
        )
        
        this.connections: []
        this.available: []
    }
}

# Usage
pool: new AsyncConnectionPool({maxConnections: 10})

result: await pool.withConnection(async (conn) => {
    return await conn.query("SELECT * FROM users")
})
```

## Async Testing

```tusk
# Async test utilities
class AsyncTest {
    static async waitFor(condition, timeout = 5000, interval = 100) {
        start: Date.now()
        
        while (Date.now() - start < timeout) {
            if (await condition()) {
                return true
            }
            await @delay(interval)
        }
        
        throw new Error("Timeout waiting for condition")
    }
    
    static async expectAsync(asyncFn) {
        return {
            toResolve: async () => {
                try {
                    await asyncFn()
                    return true
                } catch {
                    throw new Error("Expected promise to resolve")
                }
            },
            
            toReject: async () => {
                try {
                    await asyncFn()
                    throw new Error("Expected promise to reject")
                } catch {
                    return true
                }
            },
            
            toRejectWith: async (expectedError) => {
                try {
                    await asyncFn()
                    throw new Error("Expected promise to reject")
                } catch (error) {
                    if (error.message !== expectedError.message) {
                        throw new Error(`Expected error: ${expectedError.message}, got: ${error.message}`)
                    }
                    return true
                }
            }
        }
    }
}

# Async test example
#test "Async operation completes successfully" async {
    result: await @performAsyncOperation()
    @assert.equals(result.status, "success")
    
    # Wait for side effect
    await AsyncTest.waitFor(async () => {
        cached: await @cache.get("operation_result")
        return cached !== null
    })
    
    # Test rejection
    await AsyncTest.expectAsync(
        async () => await @failingOperation()
    ).toReject()
}
```

## Best Practices

1. **Always handle errors** - Use try-catch or .catch()
2. **Avoid blocking operations** - Keep async functions truly async
3. **Use Promise.all() for parallel operations** - Don't await in loops
4. **Set timeouts for network operations** - Prevent hanging
5. **Clean up resources** - Use finally blocks
6. **Avoid callback hell** - Use async/await instead
7. **Test async code thoroughly** - Including error cases
8. **Monitor async performance** - Track execution times

## Related Topics

- `promises` - Promise fundamentals
- `event-loop` - Understanding the event loop
- `web-workers` - Parallel processing
- `streaming` - Stream processing
- `error-handling` - Async error patterns