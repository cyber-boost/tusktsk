<?tusk
/**
 * 🌳 TRUNK CLASS DEMO - THE ELEPHANT'S MEMORY
 * ==========================================
 * 
 * WHAT IS TRUNK?
 * --------------
 * Trunk is TuskPHP's powerful data persistence and caching layer, providing
 * an elephant-strong memory system that never forgets (unless you want it to).
 * Think of it as the elephant's trunk - versatile, powerful, and essential.
 * 
 * KEY FEATURES:
 * - Multi-tier caching (Memory → Redis → Database)
 * - Automatic cache invalidation and warming
 * - Data compression and serialization
 * - TTL (Time To Live) management
 * - Cache tagging and bulk operations
 * - Performance monitoring and statistics
 * 
 * HOW TO USE:
 * -----------
 * 1. Basic storage and retrieval:
 *    $trunk = new Trunk();
 *    $trunk->store('user:123', $userData, 3600); // 1 hour TTL
 *    $data = $trunk->fetch('user:123');
 * 
 * 2. Tagged caching for bulk operations:
 *    $trunk->storeTagged(['users', 'active'], 'list', $activeUsers);
 *    $trunk->flushTags(['users']); // Invalidate all user caches
 * 
 * 3. Remember pattern for expensive operations:
 *    $result = $trunk->remember('report:monthly', 86400, function() {
 *        return generateExpensiveReport();
 *    });
 * 
 * 4. Performance monitoring:
 *    $stats = $trunk->getStats();
 *    echo "Hit rate: " . $stats['hit_rate'] . "%";
 * 
 * STORAGE DRIVERS:
 * - Memory: Ultra-fast in-process storage
 * - Redis: Distributed caching with persistence
 * - Memcached: High-performance distributed memory caching
 * - Database: Persistent storage with query capabilities
 * - File: Simple file-based caching
 * 
 * COMMON USE CASES:
 * - Session storage
 * - API response caching
 * - Database query results
 * - Computed values and calculations
 * - Configuration caching
 * - Asset compilation results
 * 
 * @package TuskPHP\Class
 * @author Bernie G. (Senior Elephant Keeper)
 * @version 3.0.0-Elder
 * @elephant-wisdom "An elephant never forgets, but knows when to let go"
 */
?>
<!DOCTYPE html>
<html>
<head>
    <title>🌳 Trunk - Data Persistence & Caching | TuskPHP</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: #0a0e27;
            color: #e2e8f0;
            line-height: 1.6;
            min-height: 100vh;
        }
        
        /* TuskPHP Signature Header */
        .tusk-header {
            background: linear-gradient(135deg, #1e3a8a 0%, #3730a3 50%, #6d28d9 100%);
            padding: 60px 0;
            text-align: center;
            position: relative;
            overflow: hidden;
            box-shadow: 0 10px 40px rgba(0,0,0,0.5);
        }
        
        .tusk-header::before {
            content: '<?tusk>';
            position: absolute;
            top: 20px;
            right: 40px;
            font-family: 'Courier New', monospace;
            font-size: 1.2em;
            color: rgba(255,255,255,0.3);
            font-weight: bold;
        }
        
        .tusk-header::after {
            content: '🐘';
            position: absolute;
            font-size: 400px;
            opacity: 0.03;
            bottom: -150px;
            left: -100px;
            transform: rotate(-20deg);
        }
        
        .tusk-header h1 {
            font-size: 4em;
            font-weight: 900;
            margin-bottom: 15px;
            text-shadow: 3px 3px 6px rgba(0,0,0,0.3);
            letter-spacing: -1px;
            position: relative;
            z-index: 1;
        }
        
        .tusk-header .tagline {
            font-size: 1.4em;
            opacity: 0.9;
            font-weight: 300;
            position: relative;
            z-index: 1;
        }
        
        .container {
            max-width: 1400px;
            margin: 0 auto;
            padding: 30px;
        }
        
        /* Enhanced Usage Guide */
        .usage-section {
            background: #1a1f3a;
            border-radius: 16px;
            padding: 40px;
            margin: 40px 0;
            border: 1px solid #2d3561;
            position: relative;
            box-shadow: 0 10px 30px rgba(0,0,0,0.3);
        }
        
        .usage-section::before {
            content: attr(data-section);
            position: absolute;
            top: -12px;
            left: 30px;
            background: #6d28d9;
            color: white;
            padding: 4px 16px;
            border-radius: 20px;
            font-size: 0.85em;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 1px;
        }
        
        .usage-section h2 {
            color: #a78bfa;
            margin-bottom: 25px;
            font-size: 2em;
            display: flex;
            align-items: center;
            gap: 15px;
        }
        
        .usage-section h3 {
            color: #c4b5fd;
            margin: 30px 0 15px 0;
            font-size: 1.4em;
        }
        
        /* Code Examples with Syntax Highlighting */
        .code-block {
            background: #0f172a;
            border: 1px solid #1e293b;
            border-radius: 12px;
            padding: 25px;
            margin: 20px 0;
            overflow-x: auto;
            position: relative;
            font-family: 'Fira Code', 'Courier New', monospace;
            font-size: 0.95em;
            line-height: 1.7;
        }
        
        .code-block::before {
            content: 'PHP';
            position: absolute;
            top: 10px;
            right: 15px;
            color: #64748b;
            font-size: 0.8em;
            text-transform: uppercase;
            letter-spacing: 1px;
        }
        
        .code-block pre { margin: 0; }
        .comment { color: #64748b; font-style: italic; }
        .keyword { color: #c084fc; font-weight: bold; }
        .string { color: #86efac; }
        .function { color: #60a5fa; }
        .variable { color: #fbbf24; }
        .operator { color: #f472b6; }
        .number { color: #fb923c; }
        
        /* Interactive Cache Dashboard */
        .cache-dashboard {
            background: #1a1f3a;
            border-radius: 16px;
            padding: 40px;
            margin: 40px 0;
            border: 1px solid #2d3561;
        }
        
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 25px;
            margin-top: 30px;
        }
        
        .stat-card {
            background: #0f172a;
            padding: 30px;
            border-radius: 12px;
            text-align: center;
            border: 1px solid #1e293b;
            transition: all 0.3s ease;
            position: relative;
            overflow: hidden;
        }
        
        .stat-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 15px 40px rgba(109, 40, 217, 0.2);
            border-color: #6d28d9;
        }
        
        .stat-card::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 4px;
            background: linear-gradient(90deg, #6d28d9 0%, #a78bfa 100%);
            transform: scaleX(0);
            transition: transform 0.3s ease;
        }
        
        .stat-card:hover::before {
            transform: scaleX(1);
        }
        
        .stat-icon {
            font-size: 3em;
            margin-bottom: 15px;
            display: block;
        }
        
        .stat-value {
            font-size: 3em;
            font-weight: 900;
            color: #a78bfa;
            margin: 10px 0;
            font-variant-numeric: tabular-nums;
        }
        
        .stat-label {
            color: #94a3b8;
            text-transform: uppercase;
            font-size: 0.85em;
            letter-spacing: 1px;
            font-weight: 600;
        }
        
        /* Cache Operations Interface */
        .cache-interface {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 30px;
            margin-top: 40px;
        }
        
        .operation-panel {
            background: #0f172a;
            border-radius: 12px;
            padding: 30px;
            border: 1px solid #1e293b;
        }
        
        .operation-panel h3 {
            color: #a78bfa;
            margin-bottom: 20px;
            font-size: 1.5em;
        }
        
        .form-group {
            margin-bottom: 20px;
        }
        
        .form-group label {
            display: block;
            margin-bottom: 8px;
            color: #c4b5fd;
            font-weight: 600;
            font-size: 0.95em;
        }
        
        .form-control {
            width: 100%;
            padding: 12px 16px;
            background: #1a1f3a;
            border: 2px solid #2d3561;
            border-radius: 8px;
            color: #e2e8f0;
            font-size: 16px;
            font-family: inherit;
            transition: all 0.3s ease;
        }
        
        .form-control:focus {
            outline: none;
            border-color: #6d28d9;
            box-shadow: 0 0 0 3px rgba(109, 40, 217, 0.1);
            background: #1e293b;
        }
        
        .btn {
            padding: 14px 28px;
            background: linear-gradient(135deg, #6d28d9 0%, #7c3aed 100%);
            color: white;
            border: none;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 700;
            cursor: pointer;
            transition: all 0.3s ease;
            display: inline-flex;
            align-items: center;
            gap: 10px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            box-shadow: 0 4px 15px rgba(109, 40, 217, 0.3);
        }
        
        .btn:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 25px rgba(109, 40, 217, 0.4);
        }
        
        .btn:active {
            transform: translateY(0);
        }
        
        .btn-secondary {
            background: linear-gradient(135deg, #475569 0%, #64748b 100%);
            box-shadow: 0 4px 15px rgba(71, 85, 105, 0.3);
        }
        
        .btn-danger {
            background: linear-gradient(135deg, #dc2626 0%, #ef4444 100%);
            box-shadow: 0 4px 15px rgba(220, 38, 38, 0.3);
        }
        
        /* Cache Entries Display */
        .cache-entries {
            background: #0f172a;
            border-radius: 12px;
            padding: 30px;
            margin-top: 30px;
            border: 1px solid #1e293b;
            max-height: 600px;
            overflow-y: auto;
        }
        
        .cache-entry {
            background: #1a1f3a;
            padding: 20px;
            border-radius: 8px;
            margin-bottom: 15px;
            border: 1px solid #2d3561;
            transition: all 0.3s ease;
            cursor: pointer;
        }
        
        .cache-entry:hover {
            border-color: #6d28d9;
            transform: translateX(5px);
        }
        
        .entry-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 10px;
        }
        
        .entry-key {
            font-family: 'Courier New', monospace;
            color: #60a5fa;
            font-weight: bold;
            font-size: 1.1em;
        }
        
        .entry-meta {
            display: flex;
            gap: 20px;
            font-size: 0.9em;
            color: #94a3b8;
        }
        
        .entry-ttl {
            color: #86efac;
        }
        
        .entry-size {
            color: #fbbf24;
        }
        
        .entry-preview {
            background: #0f172a;
            padding: 10px;
            border-radius: 4px;
            margin-top: 10px;
            font-family: monospace;
            font-size: 0.85em;
            color: #94a3b8;
            max-height: 100px;
            overflow: hidden;
        }
        
        /* Memory Visualization */
        .memory-viz {
            background: #1a1f3a;
            border-radius: 16px;
            padding: 40px;
            margin: 40px 0;
            border: 1px solid #2d3561;
        }
        
        .memory-tiers {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 20px;
            margin-top: 30px;
        }
        
        .tier-card {
            background: #0f172a;
            padding: 25px;
            border-radius: 12px;
            text-align: center;
            border: 2px solid #1e293b;
            position: relative;
            transition: all 0.3s ease;
        }
        
        .tier-card.active {
            border-color: #22c55e;
            box-shadow: 0 0 20px rgba(34, 197, 94, 0.2);
        }
        
        .tier-icon {
            font-size: 3em;
            margin-bottom: 15px;
        }
        
        .tier-name {
            font-size: 1.2em;
            font-weight: bold;
            color: #e2e8f0;
            margin-bottom: 10px;
        }
        
        .tier-stats {
            color: #94a3b8;
            font-size: 0.9em;
        }
        
        .tier-speed {
            display: inline-block;
            background: #1a1f3a;
            padding: 4px 12px;
            border-radius: 20px;
            margin-top: 10px;
            font-size: 0.85em;
            color: #86efac;
            border: 1px solid #22c55e;
        }
        
        /* Performance Chart */
        .perf-chart {
            background: #0f172a;
            border-radius: 12px;
            padding: 30px;
            margin-top: 30px;
            border: 1px solid #1e293b;
            height: 300px;
            position: relative;
            overflow: hidden;
        }
        
        .chart-bars {
            display: flex;
            align-items: flex-end;
            justify-content: space-around;
            height: 200px;
            margin-top: 30px;
        }
        
        .chart-bar {
            width: 40px;
            background: linear-gradient(to top, #6d28d9 0%, #a78bfa 100%);
            border-radius: 4px 4px 0 0;
            transition: height 0.5s ease;
            position: relative;
        }
        
        .chart-bar:hover {
            opacity: 0.8;
        }
        
        .chart-bar::after {
            content: attr(data-value);
            position: absolute;
            top: -25px;
            left: 50%;
            transform: translateX(-50%);
            color: #e2e8f0;
            font-size: 0.9em;
            font-weight: bold;
        }
        
        /* Alert Messages */
        .alert {
            padding: 16px 24px;
            border-radius: 8px;
            margin: 20px 0;
            display: flex;
            align-items: center;
            gap: 12px;
            font-weight: 500;
        }
        
        .alert-success {
            background: #064e3b;
            border: 1px solid #10b981;
            color: #86efac;
        }
        
        .alert-info {
            background: #1e3a8a;
            border: 1px solid #3b82f6;
            color: #93bbfc;
        }
        
        .alert-warning {
            background: #713f12;
            border: 1px solid #f59e0b;
            color: #fcd34d;
        }
        
        .alert-error {
            background: #7f1d1d;
            border: 1px solid #ef4444;
            color: #fca5a5;
        }
        
        /* Cache Strategy Selector */
        .strategy-selector {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 20px;
            margin: 30px 0;
        }
        
        .strategy-card {
            background: #0f172a;
            padding: 25px;
            border-radius: 12px;
            border: 2px solid #1e293b;
            cursor: pointer;
            transition: all 0.3s ease;
            text-align: center;
        }
        
        .strategy-card:hover {
            border-color: #6d28d9;
            transform: translateY(-3px);
        }
        
        .strategy-card.selected {
            border-color: #22c55e;
            background: #064e3b;
        }
        
        .strategy-icon {
            font-size: 2.5em;
            margin-bottom: 10px;
        }
        
        .strategy-name {
            font-weight: bold;
            color: #e2e8f0;
            margin-bottom: 5px;
        }
        
        .strategy-desc {
            font-size: 0.9em;
            color: #94a3b8;
        }
        
        /* Footer */
        .tusk-footer {
            text-align: center;
            padding: 60px 20px;
            border-top: 1px solid #2d3561;
            margin-top: 80px;
            color: #64748b;
        }
        
        .tusk-footer .logo {
            font-size: 4em;
            margin-bottom: 20px;
            opacity: 0.3;
        }
        
        .tusk-footer .wisdom {
            font-style: italic;
            font-size: 1.1em;
            color: #94a3b8;
            margin-bottom: 30px;
        }
        
        /* Animations */
        @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.5; }
        }
        
        @keyframes slideIn {
            from { opacity: 0; transform: translateY(20px); }
            to { opacity: 1; transform: translateY(0); }
        }
        
        .animate-in {
            animation: slideIn 0.5s ease forwards;
        }
        
        /* Scrollbar Styling */
        ::-webkit-scrollbar {
            width: 10px;
        }
        
        ::-webkit-scrollbar-track {
            background: #1a1f3a;
        }
        
        ::-webkit-scrollbar-thumb {
            background: #6d28d9;
            border-radius: 5px;
        }
        
        ::-webkit-scrollbar-thumb:hover {
            background: #7c3aed;
        }
    </style>
</head>
<body>
    <div class="tusk-header">
        <h1>🌳 Trunk</h1>
        <p class="tagline">The Elephant's Memory - Never Forgets, Always Performs</p>
    </div>
    
    <div class="container">
        <!-- Complete Usage Guide -->
        <div class="usage-section" data-section="Usage Guide">
            <h2>📖 Complete Usage Documentation</h2>
            
            <h3>🚀 Quick Start</h3>
            <div class="code-block">
                <pre><span class="comment">// Initialize Trunk with default configuration</span>
<span class="keyword">use</span> <span class="function">TuskPHP\Class\Trunk</span>;

<span class="variable">$trunk</span> = <span class="keyword">new</span> <span class="function">Trunk</span>();

<span class="comment">// Store data with 1 hour TTL</span>
<span class="variable">$trunk</span><span class="operator">-></span><span class="function">store</span>(<span class="string">'user:123'</span>, <span class="variable">$userData</span>, <span class="number">3600</span>);

<span class="comment">// Retrieve data</span>
<span class="variable">$user</span> = <span class="variable">$trunk</span><span class="operator">-></span><span class="function">fetch</span>(<span class="string">'user:123'</span>);

<span class="comment">// Check if key exists</span>
<span class="keyword">if</span> (<span class="variable">$trunk</span><span class="operator">-></span><span class="function">has</span>(<span class="string">'user:123'</span>)) {
    <span class="comment">// Key exists in cache</span>
}

<span class="comment">// Delete from cache</span>
<span class="variable">$trunk</span><span class="operator">-></span><span class="function">forget</span>(<span class="string">'user:123'</span>);</pre>
            </div>
            
            <h3>⚙️ Advanced Configuration</h3>
            <div class="code-block">
                <pre><span class="comment">// Configure with multiple drivers</span>
<span class="variable">$trunk</span> = <span class="keyword">new</span> <span class="function">Trunk</span>([
    <span class="string">'default'</span> <span class="operator">=></span> <span class="string">'redis'</span>,
    <span class="string">'drivers'</span> <span class="operator">=></span> [
        <span class="string">'memory'</span> <span class="operator">=></span> [
            <span class="string">'driver'</span> <span class="operator">=></span> <span class="string">'array'</span>,
            <span class="string">'limit'</span> <span class="operator">=></span> <span class="number">1000</span>
        ],
        <span class="string">'redis'</span> <span class="operator">=></span> [
            <span class="string">'driver'</span> <span class="operator">=></span> <span class="string">'redis'</span>,
            <span class="string">'host'</span> <span class="operator">=></span> <span class="string">'localhost'</span>,
            <span class="string">'port'</span> <span class="operator">=></span> <span class="number">6379</span>,
            <span class="string">'database'</span> <span class="operator">=></span> <span class="number">0</span>,
            <span class="string">'prefix'</span> <span class="operator">=></span> <span class="string">'trunk_'</span>
        ],
        <span class="string">'file'</span> <span class="operator">=></span> [
            <span class="string">'driver'</span> <span class="operator">=></span> <span class="string">'file'</span>,
            <span class="string">'path'</span> <span class="operator">=></span> <span class="string">'/var/cache/trunk'</span>
        ]
    ],
    <span class="string">'ttl'</span> <span class="operator">=></span> <span class="number">3600</span>, <span class="comment">// Default TTL</span>
    <span class="string">'compress'</span> <span class="operator">=></span> <span class="keyword">true</span>,
    <span class="string">'serialize'</span> <span class="operator">=></span> <span class="string">'json'</span> <span class="comment">// json, php, msgpack</span>
]);</pre>
            </div>
            
            <h3>🏷️ Tagged Caching</h3>
            <div class="code-block">
                <pre><span class="comment">// Store with tags for grouped invalidation</span>
<span class="variable">$trunk</span><span class="operator">-></span><span class="function">tags</span>([<span class="string">'users'</span>, <span class="string">'premium'</span>])<span class="operator">-></span><span class="function">store</span>(<span class="string">'user:123'</span>, <span class="variable">$data</span>);
<span class="variable">$trunk</span><span class="operator">-></span><span class="function">tags</span>([<span class="string">'users'</span>, <span class="string">'basic'</span>])<span class="operator">-></span><span class="function">store</span>(<span class="string">'user:456'</span>, <span class="variable">$data</span>);

<span class="comment">// Flush all users</span>
<span class="variable">$trunk</span><span class="operator">-></span><span class="function">tags</span>([<span class="string">'users'</span>])<span class="operator">-></span><span class="function">flush</span>();

<span class="comment">// Flush only premium users</span>
<span class="variable">$trunk</span><span class="operator">-></span><span class="function">tags</span>([<span class="string">'premium'</span>])<span class="operator">-></span><span class="function">flush</span>();</pre>
            </div>
            
            <h3>🔄 Remember Pattern</h3>
            <div class="code-block">
                <pre><span class="comment">// Cache expensive operations automatically</span>
<span class="variable">$report</span> = <span class="variable">$trunk</span><span class="operator">-></span><span class="function">remember</span>(<span class="string">'monthly-report'</span>, <span class="number">86400</span>, <span class="keyword">function</span>() {
    <span class="comment">// This closure only runs if cache miss</span>
    <span class="keyword">return</span> <span class="function">generateExpensiveReport</span>();
});

<span class="comment">// Remember forever</span>
<span class="variable">$config</span> = <span class="variable">$trunk</span><span class="operator">-></span><span class="function">rememberForever</span>(<span class="string">'app-config'</span>, <span class="keyword">function</span>() {
    <span class="keyword">return</span> <span class="function">loadConfiguration</span>();
});</pre>
            </div>
            
            <h3>🔐 Atomic Operations</h3>
            <div class="code-block">
                <pre><span class="comment">// Increment/decrement counters atomically</span>
<span class="variable">$views</span> = <span class="variable">$trunk</span><span class="operator">-></span><span class="function">increment</span>(<span class="string">'post:123:views'</span>);
<span class="variable">$trunk</span><span class="operator">-></span><span class="function">increment</span>(<span class="string">'post:123:views'</span>, <span class="number">5</span>); <span class="comment">// Increment by 5</span>

<span class="comment">// Atomic add (only if not exists)</span>
<span class="variable">$added</span> = <span class="variable">$trunk</span><span class="operator">-></span><span class="function">add</span>(<span class="string">'lock:process'</span>, <span class="keyword">true</span>, <span class="number">300</span>);
<span class="keyword">if</span> (<span class="variable">$added</span>) {
    <span class="comment">// Got the lock, process...</span>
    <span class="variable">$trunk</span><span class="operator">-></span><span class="function">forget</span>(<span class="string">'lock:process'</span>);
}</pre>
            </div>
        </div>
        
        <!-- Live Cache Dashboard -->
        <div class="cache-dashboard">
            <h2>📊 Live Cache Statistics</h2>
            
            <div class="stats-grid">
                <div class="stat-card">
                    <span class="stat-icon">💾</span>
                    <div class="stat-value" id="total-keys">1,247</div>
                    <div class="stat-label">Total Keys</div>
                </div>
                
                <div class="stat-card">
                    <span class="stat-icon">🎯</span>
                    <div class="stat-value" id="hit-rate">94.2%</div>
                    <div class="stat-label">Hit Rate</div>
                </div>
                
                <div class="stat-card">
                    <span class="stat-icon">📦</span>
                    <div class="stat-value" id="memory-used">128MB</div>
                    <div class="stat-label">Memory Used</div>
                </div>
                
                <div class="stat-card">
                    <span class="stat-icon">⚡</span>
                    <div class="stat-value" id="avg-response">0.8ms</div>
                    <div class="stat-label">Avg Response</div>
                </div>
            </div>
            
            <!-- Performance Chart -->
            <div class="perf-chart">
                <h3 style="margin: 0; color: #a78bfa;">Performance Over Time</h3>
                <div class="chart-bars">
                    <?php for ($i = 0; $i < 12; $i++): ?>
                        <div class="chart-bar" data-value="<?php echo rand(70, 99); ?>%" style="height: <?php echo rand(40, 90); ?>%"></div>
                    <?php endfor; ?>
                </div>
            </div>
        </div>
        
        <!-- Memory Tier Visualization -->
        <div class="memory-viz">
            <h2>🏗️ Multi-Tier Cache Architecture</h2>
            
            <div class="memory-tiers">
                <div class="tier-card active">
                    <div class="tier-icon">💨</div>
                    <div class="tier-name">L1: Memory</div>
                    <div class="tier-stats">
                        <div>Capacity: 100MB</div>
                        <div>Items: 523</div>
                        <div class="tier-speed">< 0.1ms</div>
                    </div>
                </div>
                
                <div class="tier-card active">
                    <div class="tier-icon">🚀</div>
                    <div class="tier-name">L2: Redis</div>
                    <div class="tier-stats">
                        <div>Capacity: 4GB</div>
                        <div>Items: 12,847</div>
                        <div class="tier-speed">< 1ms</div>
                    </div>
                </div>
                
                <div class="tier-card">
                    <div class="tier-icon">💾</div>
                    <div class="tier-name">L3: Database</div>
                    <div class="tier-stats">
                        <div>Capacity: ∞</div>
                        <div>Items: 1.2M</div>
                        <div class="tier-speed">< 10ms</div>
                    </div>
                </div>
                
                <div class="tier-card">
                    <div class="tier-icon">📁</div>
                    <div class="tier-name">L4: File</div>
                    <div class="tier-stats">
                        <div>Capacity: 100GB</div>
                        <div>Items: 45,231</div>
                        <div class="tier-speed">< 50ms</div>
                    </div>
                </div>
            </div>
            
            <div class="alert alert-info" style="margin-top: 30px;">
                <span>💡</span>
                <div>
                    <strong>Smart Tier Management:</strong> Trunk automatically promotes hot data to faster tiers 
                    and demotes cold data to save memory. The most accessed items bubble up to L1 cache.
                </div>
            </div>
        </div>
        
        <!-- Interactive Cache Operations -->
        <div class="usage-section" data-section="Try It Live">
            <h2>🎮 Interactive Cache Operations</h2>
            
            <div class="cache-interface">
                <!-- Store Operation -->
                <div class="operation-panel">
                    <h3>📥 Store Data</h3>
                    
                    <div class="form-group">
                        <label>Cache Key</label>
                        <input type="text" class="form-control" id="store-key" 
                               placeholder="e.g., user:123, product:456" value="demo:elephant">
                    </div>
                    
                    <div class="form-group">
                        <label>Value (JSON)</label>
                        <textarea class="form-control" id="store-value" rows="4">{
  "name": "Dumbo",
  "type": "Flying Elephant",
  "skills": ["fly", "remember", "cache"]
}</textarea>
                    </div>
                    
                    <div class="form-group">
                        <label>TTL (seconds)</label>
                        <input type="number" class="form-control" id="store-ttl" value="3600">
                    </div>
                    
                    <div class="form-group">
                        <label>Tags (comma separated)</label>
                        <input type="text" class="form-control" id="store-tags" 
                               placeholder="users, premium" value="demo, elephants">
                    </div>
                    
                    <button class="btn" onclick="storeData()">
                        <span>💾</span> Store in Cache
                    </button>
                </div>
                
                <!-- Fetch Operation -->
                <div class="operation-panel">
                    <h3>📤 Fetch Data</h3>
                    
                    <div class="form-group">
                        <label>Cache Key</label>
                        <input type="text" class="form-control" id="fetch-key" 
                               placeholder="Enter key to fetch" value="demo:elephant">
                    </div>
                    
                    <button class="btn" onclick="fetchData()">
                        <span>🔍</span> Fetch from Cache
                    </button>
                    
                    <button class="btn btn-secondary" onclick="checkExists()">
                        <span>❓</span> Check Exists
                    </button>
                    
                    <button class="btn btn-danger" onclick="deleteKey()">
                        <span>🗑️</span> Delete Key
                    </button>
                    
                    <div id="fetch-result" style="margin-top: 20px;"></div>
                </div>
            </div>
            
            <!-- Cache Strategy Selector -->
            <h3 style="margin-top: 40px; color: #a78bfa;">🎯 Cache Strategies</h3>
            <div class="strategy-selector">
                <div class="strategy-card selected" onclick="selectStrategy(this, 'lazy')">
                    <div class="strategy-icon">😴</div>
                    <div class="strategy-name">Lazy Loading</div>
                    <div class="strategy-desc">Load on demand, cache on first access</div>
                </div>
                
                <div class="strategy-card" onclick="selectStrategy(this, 'eager')">
                    <div class="strategy-icon">🏃</div>
                    <div class="strategy-name">Eager Loading</div>
                    <div class="strategy-desc">Pre-warm cache before requests</div>
                </div>
                
                <div class="strategy-card" onclick="selectStrategy(this, 'refresh')">
                    <div class="strategy-icon">🔄</div>
                    <div class="strategy-name">Refresh Ahead</div>
                    <div class="strategy-desc">Refresh before expiration</div>
                </div>
            </div>
        </div>
        
        <!-- Current Cache Entries -->
        <div class="cache-entries">
            <h2>📚 Current Cache Entries</h2>
            
            <div id="entries-list">
                <div class="cache-entry">
                    <div class="entry-header">
                        <span class="entry-key">user:123</span>
                        <div class="entry-meta">
                            <span class="entry-ttl">TTL: 45m</span>
                            <span class="entry-size">Size: 2.4KB</span>
                            <span>Tags: users, premium</span>
                        </div>
                    </div>
                    <div class="entry-preview">{"id": 123, "name": "Bernie", "role": "admin", "preferences": {...}}</div>
                </div>
                
                <div class="cache-entry">
                    <div class="entry-header">
                        <span class="entry-key">config:app</span>
                        <div class="entry-meta">
                            <span class="entry-ttl">TTL: ∞</span>
                            <span class="entry-size">Size: 8.7KB</span>
                            <span>Tags: config</span>
                        </div>
                    </div>
                    <div class="entry-preview">{"name": "TuskPHP", "version": "3.0", "debug": false, ...}</div>
                </div>
                
                <div class="cache-entry">
                    <div class="entry-header">
                        <span class="entry-key">posts:trending</span>
                        <div class="entry-meta">
                            <span class="entry-ttl">TTL: 5m</span>
                            <span class="entry-size">Size: 12.3KB</span>
                            <span>Tags: posts, homepage</span>
                        </div>
                    </div>
                    <div class="entry-preview">[{"id": 456, "title": "Understanding Elephants", "views": 1247}, ...]</div>
                </div>
            </div>
        </div>
        
        <!-- Advanced Features -->
        <div class="usage-section" data-section="Advanced">
            <h2>🔧 Advanced Features</h2>
            
            <h3>🔒 Cache Locking</h3>
            <div class="code-block">
                <pre><span class="comment">// Prevent cache stampede with locks</span>
<span class="variable">$lock</span> = <span class="variable">$trunk</span><span class="operator">-></span><span class="function">lock</span>(<span class="string">'expensive-operation'</span>, <span class="number">10</span>);

<span class="keyword">if</span> (<span class="variable">$lock</span><span class="operator">-></span><span class="function">get</span>()) {
    <span class="keyword">try</span> {
        <span class="variable">$result</span> = <span class="function">performExpensiveOperation</span>();
        <span class="variable">$trunk</span><span class="operator">-></span><span class="function">put</span>(<span class="string">'result'</span>, <span class="variable">$result</span>, <span class="number">3600</span>);
    } <span class="keyword">finally</span> {
        <span class="variable">$lock</span><span class="operator">-></span><span class="function">release</span>();
    }
}</pre>
            </div>
            
            <h3>📊 Cache Events</h3>
            <div class="code-block">
                <pre><span class="comment">// Listen to cache events</span>
<span class="variable">$trunk</span><span class="operator">-></span><span class="function">listen</span>(<span class="string">'cache:hit'</span>, <span class="keyword">function</span>(<span class="variable">$key</span>, <span class="variable">$value</span>) {
    <span class="function">Log</span>::<span class="function">info</span>(<span class="string">"Cache hit: {$key}"</span>);
});

<span class="variable">$trunk</span><span class="operator">-></span><span class="function">listen</span>(<span class="string">'cache:miss'</span>, <span class="keyword">function</span>(<span class="variable">$key</span>) {
    <span class="function">Log</span>::<span class="function">warning</span>(<span class="string">"Cache miss: {$key}"</span>);
    <span class="comment">// Optionally warm the cache</span>
});

<span class="variable">$trunk</span><span class="operator">-></span><span class="function">listen</span>(<span class="string">'cache:delete'</span>, <span class="keyword">function</span>(<span class="variable">$key</span>) {
    <span class="comment">// Invalidate related caches</span>
});</pre>
            </div>
            
            <h3>🎨 Custom Serializers</h3>
            <div class="code-block">
                <pre><span class="comment">// Register custom serializer</span>
<span class="variable">$trunk</span><span class="operator">-></span><span class="function">extend</span>(<span class="string">'serializer'</span>, <span class="string">'protobuf'</span>, <span class="keyword">function</span>() {
    <span class="keyword">return new</span> <span class="function">ProtobufSerializer</span>();
});

<span class="comment">// Use custom serializer</span>
<span class="variable">$trunk</span><span class="operator">-></span><span class="function">serializer</span>(<span class="string">'protobuf'</span>)<span class="operator">-></span><span class="function">put</span>(<span class="string">'data'</span>, <span class="variable">$complexObject</span>);</pre>
            </div>
        </div>
        
        <!-- Best Practices -->
        <div class="usage-section" data-section="Best Practices">
            <h2>✅ Best Practices & Tips</h2>
            
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px;">
                <div class="alert alert-success">
                    <span>✓</span>
                    <div>
                        <strong>Use Appropriate TTLs</strong><br>
                        Set TTLs based on data volatility. User sessions: hours, 
                        API responses: minutes, static content: days.
                    </div>
                </div>
                
                <div class="alert alert-success">
                    <span>✓</span>
                    <div>
                        <strong>Tag Related Data</strong><br>
                        Use tags to group related cache entries for easy bulk invalidation.
                    </div>
                </div>
                
                <div class="alert alert-warning">
                    <span>⚠️</span>
                    <div>
                        <strong>Avoid Cache Stampede</strong><br>
                        Use locks or jitter in TTLs to prevent simultaneous cache rebuilds.
                    </div>
                </div>
                
                <div class="alert alert-info">
                    <span>💡</span>
                    <div>
                        <strong>Monitor Hit Rates</strong><br>
                        Aim for >90% hit rate. Low rates indicate poor key design or TTLs.
                    </div>
                </div>
                
                <div class="alert alert-success">
                    <span>✓</span>
                    <div>
                        <strong>Implement Graceful Degradation</strong><br>
                        Always have fallbacks when cache is unavailable.
                    </div>
                </div>
                
                <div class="alert alert-warning">
                    <span>⚠️</span>
                    <div>
                        <strong>Size Your Cache Appropriately</strong><br>
                        Monitor memory usage and implement eviction policies.
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <div class="tusk-footer">
        <div class="logo">🐘</div>
        <p class="wisdom">"An elephant's memory is its greatest strength - Trunk never forgets what matters"</p>
        <p>TuskPHP Trunk © <?php echo date('Y'); ?> - Built with Elephant Wisdom</p>
    </div>
    
    <script>
        // Simulate real-time stats updates
        function updateStats() {
            // Update hit rate
            const hitRate = document.getElementById('hit-rate');
            const currentRate = parseFloat(hitRate.textContent);
            const newRate = Math.max(85, Math.min(99, currentRate + (Math.random() - 0.5) * 2));
            hitRate.textContent = newRate.toFixed(1) + '%';
            
            // Update total keys
            const totalKeys = document.getElementById('total-keys');
            const currentKeys = parseInt(totalKeys.textContent.replace(/,/g, ''));
            const change = Math.floor(Math.random() * 10) - 3;
            totalKeys.textContent = (currentKeys + change).toLocaleString();
            
            // Update avg response time
            const avgResponse = document.getElementById('avg-response');
            const baseTime = 0.8;
            const variance = (Math.random() - 0.5) * 0.4;
            avgResponse.textContent = Math.max(0.1, baseTime + variance).toFixed(1) + 'ms';
        }
        
        setInterval(updateStats, 2000);
        
        // Cache operations
        let cacheStorage = {
            'demo:elephant': {
                value: {name: "Dumbo", type: "Flying Elephant", skills: ["fly", "remember", "cache"]},
                ttl: 3600,
                tags: ['demo', 'elephants'],
                stored: new Date()
            }
        };
        
        function storeData() {
            const key = document.getElementById('store-key').value;
            const value = document.getElementById('store-value').value;
            const ttl = document.getElementById('store-ttl').value;
            const tags = document.getElementById('store-tags').value.split(',').map(t => t.trim());
            
            try {
                const parsedValue = JSON.parse(value);
                cacheStorage[key] = {
                    value: parsedValue,
                    ttl: parseInt(ttl),
                    tags: tags,
                    stored: new Date()
                };
                
                addCacheEntry(key, parsedValue, ttl, tags);
                showAlert('success', `✅ Successfully stored key: ${key}`);
                
                // Update memory used
                const memoryUsed = document.getElementById('memory-used');
                const currentSize = parseInt(memoryUsed.textContent);
                memoryUsed.textContent = (currentSize + 1) + 'MB';
                
            } catch (e) {
                showAlert('error', `❌ Invalid JSON: ${e.message}`);
            }
        }
        
        function fetchData() {
            const key = document.getElementById('fetch-key').value;
            const resultDiv = document.getElementById('fetch-result');
            
            if (cacheStorage[key]) {
                const data = cacheStorage[key];
                const ttlRemaining = Math.max(0, data.ttl - Math.floor((new Date() - data.stored) / 1000));
                
                resultDiv.innerHTML = `
                    <div class="alert alert-success">
                        <strong>✅ Cache Hit!</strong><br>
                        TTL Remaining: ${ttlRemaining}s<br>
                        Tags: ${data.tags.join(', ')}
                    </div>
                    <div class="code-block" style="margin-top: 10px;">
                        <pre>${JSON.stringify(data.value, null, 2)}</pre>
                    </div>
                `;
            } else {
                resultDiv.innerHTML = `
                    <div class="alert alert-warning">
                        <strong>❌ Cache Miss</strong><br>
                        Key not found: ${key}
                    </div>
                `;
            }
        }
        
        function checkExists() {
            const key = document.getElementById('fetch-key').value;
            if (cacheStorage[key]) {
                showAlert('info', `✅ Key exists: ${key}`);
            } else {
                showAlert('warning', `❌ Key not found: ${key}`);
            }
        }
        
        function deleteKey() {
            const key = document.getElementById('fetch-key').value;
            if (cacheStorage[key]) {
                delete cacheStorage[key];
                showAlert('success', `🗑️ Deleted key: ${key}`);
                // Remove from UI
                document.querySelectorAll('.entry-key').forEach(el => {
                    if (el.textContent === key) {
                        el.closest('.cache-entry').remove();
                    }
                });
            } else {
                showAlert('warning', `❌ Key not found: ${key}`);
            }
        }
        
        function selectStrategy(element, strategy) {
            document.querySelectorAll('.strategy-card').forEach(card => {
                card.classList.remove('selected');
            });
            element.classList.add('selected');
            showAlert('info', `🎯 Switched to ${strategy} caching strategy`);
        }
        
        function addCacheEntry(key, value, ttl, tags) {
            const entriesList = document.getElementById('entries-list');
            const entry = document.createElement('div');
            entry.className = 'cache-entry animate-in';
            entry.innerHTML = `
                <div class="entry-header">
                    <span class="entry-key">${key}</span>
                    <div class="entry-meta">
                        <span class="entry-ttl">TTL: ${ttl}s</span>
                        <span class="entry-size">Size: ${(JSON.stringify(value).length / 1024).toFixed(1)}KB</span>
                        <span>Tags: ${tags.join(', ')}</span>
                    </div>
                </div>
                <div class="entry-preview">${JSON.stringify(value, null, 2).substring(0, 100)}...</div>
            `;
            entriesList.insertBefore(entry, entriesList.firstChild);
        }
        
        function showAlert(type, message) {
            const alertDiv = document.createElement('div');
            alertDiv.className = `alert alert-${type} animate-in`;
            alertDiv.style.position = 'fixed';
            alertDiv.style.top = '20px';
            alertDiv.style.right = '20px';
            alertDiv.style.zIndex = '1000';
            alertDiv.style.minWidth = '300px';
            alertDiv.innerHTML = message;
            
            document.body.appendChild(alertDiv);
            
            setTimeout(() => {
                alertDiv.style.opacity = '0';
                setTimeout(() => alertDiv.remove(), 300);
            }, 3000);
        }
        
        // Animate tier cards
        setInterval(() => {
            const tiers = document.querySelectorAll('.tier-card');
            const activeIndex = Math.floor(Math.random() * tiers.length);
            
            tiers.forEach((tier, index) => {
                if (index === activeIndex && Math.random() > 0.5) {
                    tier.classList.add('active');
                    setTimeout(() => tier.classList.remove('active'), 1000);
                }
            });
        }, 3000);
        
        // Update chart periodically
        setInterval(() => {
            document.querySelectorAll('.chart-bar').forEach(bar => {
                const newHeight = Math.floor(Math.random() * 50 + 40);
                const newValue = Math.floor(Math.random() * 20 + 75);
                bar.style.height = newHeight + '%';
                bar.setAttribute('data-value', newValue + '%');
            });
        }, 5000);
    </script>
</body>
</html>