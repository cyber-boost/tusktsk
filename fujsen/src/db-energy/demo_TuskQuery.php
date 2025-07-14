<?php
/**
 * 🔍 TuskQuery Builder Demo
 * =========================
 * Demonstrates the powerful query builder for database operations
 * Shows fluent interface and elephant-strong performance
 */

require_once __DIR__ . '/../mahout/index.php';

// Mock TuskQuery for demo purposes
class TuskQuery {
    private $table;
    private $conditions = [];
    private $orderBy = [];
    private $limit = null;
    private $offset = null;
    
    public static function table($table) {
        $instance = new self();
        $instance->table = $table;
        return $instance;
    }
    
    public function where($column, $operator, $value = null) {
        if ($value === null) {
            $value = $operator;
            $operator = '=';
        }
        $this->conditions[] = ['type' => 'where', 'column' => $column, 'operator' => $operator, 'value' => $value];
        return $this;
    }
    
    public function whereIn($column, array $values) {
        $this->conditions[] = ['type' => 'whereIn', 'column' => $column, 'values' => $values];
        return $this;
    }
    
    public function orderBy($column, $direction = 'ASC') {
        $this->orderBy[] = ['column' => $column, 'direction' => $direction];
        return $this;
    }
    
    public function limit($limit) {
        $this->limit = $limit;
        return $this;
    }
    
    public function offset($offset) {
        $this->offset = $offset;
        return $this;
    }
    
    public function toSql() {
        $sql = "SELECT * FROM {$this->table}";
        
        if (!empty($this->conditions)) {
            $whereClauses = [];
            foreach ($this->conditions as $condition) {
                if ($condition['type'] === 'where') {
                    $whereClauses[] = "{$condition['column']} {$condition['operator']} ?";
                } elseif ($condition['type'] === 'whereIn') {
                    $placeholders = implode(',', array_fill(0, count($condition['values']), '?'));
                    $whereClauses[] = "{$condition['column']} IN ({$placeholders})";
                }
            }
            $sql .= " WHERE " . implode(' AND ', $whereClauses);
        }
        
        if (!empty($this->orderBy)) {
            $orderClauses = [];
            foreach ($this->orderBy as $order) {
                $orderClauses[] = "{$order['column']} {$order['direction']}";
            }
            $sql .= " ORDER BY " . implode(', ', $orderClauses);
        }
        
        if ($this->limit !== null) {
            $sql .= " LIMIT {$this->limit}";
        }
        
        if ($this->offset !== null) {
            $sql .= " OFFSET {$this->offset}";
        }
        
        return $sql;
    }
    
    public function get() {
        // Mock results for demo
        return [
            ['id' => 1, 'name' => 'Bernie', 'role' => 'Director'],
            ['id' => 2, 'name' => 'Claude', 'role' => 'Assistant'],
            ['id' => 3, 'name' => 'TuskPHP', 'role' => 'Framework']
        ];
    }
}

?>
<!DOCTYPE html>
<html>
<head>
    <title>🔍 TuskQuery Builder Demo</title>
    <style>
        body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; }
        .query-builder { background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .query-section { margin-bottom: 30px; }
        .code-block {
            background: #2D3748;
            color: #A0AEC0;
            padding: 20px;
            border-radius: 8px;
            margin: 15px 0;
            font-family: 'Courier New', monospace;
            overflow-x: auto;
        }
        .code-block .keyword { color: #F6AD55; }
        .code-block .method { color: #4299E1; }
        .code-block .string { color: #48BB78; }
        .code-block .number { color: #ED8936; }
        .result-table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }
        .result-table th, .result-table td {
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid #E2E8F0;
        }
        .result-table th {
            background: #F7FAFC;
            font-weight: bold;
            color: #2D3748;
        }
        .btn {
            display: inline-block;
            padding: 10px 20px;
            background: #4299E1;
            color: white;
            border: none;
            border-radius: 5px;
            cursor: pointer;
            margin-right: 10px;
            margin-bottom: 10px;
            text-decoration: none;
        }
        .btn:hover { background: #3182CE; }
        .query-examples {
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            gap: 20px;
            margin-top: 30px;
        }
        .example-card {
            background: #F7FAFC;
            padding: 20px;
            border-radius: 8px;
            border: 1px solid #E2E8F0;
        }
        .example-card h3 {
            margin-top: 0;
            color: #2D3748;
        }
        .sql-output {
            background: #2D3748;
            color: #68D391;
            padding: 15px;
            border-radius: 5px;
            font-family: monospace;
            font-size: 14px;
            margin-top: 10px;
        }
        .feature-list {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 15px;
            margin-top: 20px;
        }
        .feature {
            background: #EDF2F7;
            padding: 15px;
            border-radius: 5px;
            text-align: center;
        }
        .feature-icon {
            font-size: 2em;
            margin-bottom: 10px;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>🔍 TuskQuery Builder Demo</h1>
        <p>Build complex database queries with an elegant, fluent interface</p>
        
        <div class="query-builder">
            <div class="query-section">
                <h2>🛠️ Query Builder in Action</h2>
                
                <div class="code-block">
<span class="keyword">$users</span> = <span class="method">TuskQuery::table</span>(<span class="string">'users'</span>)
    -><span class="method">where</span>(<span class="string">'active'</span>, <span class="keyword">true</span>)
    -><span class="method">where</span>(<span class="string">'created_at'</span>, <span class="string">'>'</span>, <span class="string">'2024-01-01'</span>)
    -><span class="method">whereIn</span>(<span class="string">'role'</span>, [<span class="string">'admin'</span>, <span class="string">'moderator'</span>])
    -><span class="method">orderBy</span>(<span class="string">'created_at'</span>, <span class="string">'DESC'</span>)
    -><span class="method">limit</span>(<span class="number">10</span>)
    -><span class="method">get</span>();
                </div>
                
                <div class="sql-output">
                    <?php
                    $query = TuskQuery::table('users')
                        ->where('active', true)
                        ->where('created_at', '>', '2024-01-01')
                        ->whereIn('role', ['admin', 'moderator'])
                        ->orderBy('created_at', 'DESC')
                        ->limit(10);
                    echo $query->toSql();
                    ?>
                </div>
            </div>
            
            <div class="query-section">
                <h2>📊 Query Results</h2>
                <table class="result-table">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Name</th>
                            <th>Role</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody id="results-tbody">
                        <?php
                        $results = $query->get();
                        foreach ($results as $row) {
                            echo "<tr>";
                            echo "<td>{$row['id']}</td>";
                            echo "<td>{$row['name']}</td>";
                            echo "<td><span style='background: #4299E1; color: white; padding: 3px 8px; border-radius: 3px;'>{$row['role']}</span></td>";
                            echo "<td><span style='color: #48BB78;'>✓ Active</span></td>";
                            echo "<td><button class='btn' style='padding: 5px 10px; font-size: 12px;'>Edit</button></td>";
                            echo "</tr>";
                        }
                        ?>
                    </tbody>
                </table>
            </div>
            
            <div class="query-section">
                <h2>🎯 Query Builder Examples</h2>
                
                <div class="query-examples">
                    <div class="example-card">
                        <h3>📋 Simple Select</h3>
                        <div class="code-block">
<span class="method">TuskQuery::table</span>(<span class="string">'products'</span>)
    -><span class="method">where</span>(<span class="string">'price'</span>, <span class="string">'<'</span>, <span class="number">100</span>)
    -><span class="method">get</span>();
                        </div>
                        <button class="btn" onclick="runQuery('simple')">Run Query</button>
                    </div>
                    
                    <div class="example-card">
                        <h3>🔗 Join Operations</h3>
                        <div class="code-block">
<span class="method">TuskQuery::table</span>(<span class="string">'orders'</span>)
    -><span class="method">join</span>(<span class="string">'users'</span>, <span class="string">'orders.user_id'</span>, <span class="string">'users.id'</span>)
    -><span class="method">where</span>(<span class="string">'orders.status'</span>, <span class="string">'completed'</span>)
    -><span class="method">get</span>();
                        </div>
                        <button class="btn" onclick="runQuery('join')">Run Query</button>
                    </div>
                    
                    <div class="example-card">
                        <h3>📊 Aggregations</h3>
                        <div class="code-block">
<span class="method">TuskQuery::table</span>(<span class="string">'sales'</span>)
    -><span class="method">select</span>(<span class="string">'category'</span>, <span class="method">DB::raw</span>(<span class="string">'SUM(amount) as total'</span>))
    -><span class="method">groupBy</span>(<span class="string">'category'</span>)
    -><span class="method">having</span>(<span class="string">'total'</span>, <span class="string">'>'</span>, <span class="number">1000</span>)
    -><span class="method">get</span>();
                        </div>
                        <button class="btn" onclick="runQuery('aggregate')">Run Query</button>
                    </div>
                    
                    <div class="example-card">
                        <h3>🔍 Complex Conditions</h3>
                        <div class="code-block">
<span class="method">TuskQuery::table</span>(<span class="string">'posts'</span>)
    -><span class="method">where</span>(<span class="keyword">function</span>(<span class="keyword">$query</span>) {
        <span class="keyword">$query</span>-><span class="method">where</span>(<span class="string">'published'</span>, <span class="keyword">true</span>)
              -><span class="method">orWhere</span>(<span class="string">'featured'</span>, <span class="keyword">true</span>);
    })
    -><span class="method">orderBy</span>(<span class="string">'views'</span>, <span class="string">'DESC'</span>)
    -><span class="method">get</span>();
                        </div>
                        <button class="btn" onclick="runQuery('complex')">Run Query</button>
                    </div>
                </div>
            </div>
            
            <div class="query-section">
                <h2>✨ TuskQuery Features</h2>
                <div class="feature-list">
                    <div class="feature">
                        <div class="feature-icon">⚡</div>
                        <strong>Lightning Fast</strong>
                        <p>Optimized query generation</p>
                    </div>
                    <div class="feature">
                        <div class="feature-icon">🛡️</div>
                        <strong>SQL Injection Safe</strong>
                        <p>Automatic parameter binding</p>
                    </div>
                    <div class="feature">
                        <div class="feature-icon">🔗</div>
                        <strong>Fluent Interface</strong>
                        <p>Chain methods elegantly</p>
                    </div>
                    <div class="feature">
                        <div class="feature-icon">🎯</div>
                        <strong>Type Safety</strong>
                        <p>PHP 8+ type hints</p>
                    </div>
                    <div class="feature">
                        <div class="feature-icon">📊</div>
                        <strong>Aggregations</strong>
                        <p>SUM, COUNT, AVG, etc.</p>
                    </div>
                    <div class="feature">
                        <div class="feature-icon">🐘</div>
                        <strong>Elephant Memory</strong>
                        <p>Query result caching</p>
                    </div>
                </div>
            </div>
            
            <div class="query-section">
                <h2>🚀 Advanced Query Builder</h2>
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
                    <div>
                        <h3>Build Your Query</h3>
                        <div style="margin-bottom: 15px;">
                            <label>Table:</label>
                            <input type="text" id="table-name" value="users" style="width: 100%; padding: 8px; margin-top: 5px;">
                        </div>
                        <div style="margin-bottom: 15px;">
                            <label>Where Clause:</label>
                            <input type="text" id="where-clause" placeholder="e.g., status = 'active'" style="width: 100%; padding: 8px; margin-top: 5px;">
                        </div>
                        <div style="margin-bottom: 15px;">
                            <label>Order By:</label>
                            <input type="text" id="order-by" placeholder="e.g., created_at DESC" style="width: 100%; padding: 8px; margin-top: 5px;">
                        </div>
                        <div style="margin-bottom: 15px;">
                            <label>Limit:</label>
                            <input type="number" id="limit" value="10" style="width: 100%; padding: 8px; margin-top: 5px;">
                        </div>
                        <button class="btn" onclick="buildCustomQuery()">Generate SQL</button>
                    </div>
                    <div>
                        <h3>Generated SQL</h3>
                        <div id="custom-sql" class="sql-output" style="min-height: 200px;">
                            SELECT * FROM users LIMIT 10
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <script>
        function runQuery(type) {
            const results = {
                simple: [
                    {id: 1, name: 'Budget Laptop', price: '$89.99'},
                    {id: 2, name: 'USB Cable', price: '$9.99'},
                    {id: 3, name: 'Mouse Pad', price: '$14.99'}
                ],
                join: [
                    {order_id: 1001, user: 'Bernie', total: '$299.99', status: 'Completed'},
                    {order_id: 1002, user: 'Claude', total: '$149.99', status: 'Completed'},
                    {order_id: 1003, user: 'Alice', total: '$89.99', status: 'Completed'}
                ],
                aggregate: [
                    {category: 'Electronics', total: '$45,299'},
                    {category: 'Clothing', total: '$12,450'},
                    {category: 'Books', total: '$8,799'}
                ],
                complex: [
                    {title: 'TuskPHP Tutorial', views: 15420, published: true, featured: true},
                    {title: 'Elephant Facts', views: 8901, published: true, featured: false},
                    {title: 'PHP Best Practices', views: 7234, published: true, featured: true}
                ]
            };
            
            alert(`Query executed! Found ${results[type].length} results.\n\nTuskQuery says: "Elephant-speed performance! 🐘"`);
        }
        
        function buildCustomQuery() {
            const table = document.getElementById('table-name').value;
            const where = document.getElementById('where-clause').value;
            const orderBy = document.getElementById('order-by').value;
            const limit = document.getElementById('limit').value;
            
            let sql = `SELECT * FROM ${table}`;
            if (where) sql += ` WHERE ${where}`;
            if (orderBy) sql += ` ORDER BY ${orderBy}`;
            if (limit) sql += ` LIMIT ${limit}`;
            
            document.getElementById('custom-sql').textContent = sql;
        }
        
        // Animate query execution
        function animateQuery() {
            const tbody = document.getElementById('results-tbody');
            const rows = tbody.getElementsByTagName('tr');
            
            Array.from(rows).forEach((row, index) => {
                setTimeout(() => {
                    row.style.opacity = '0';
                    row.style.transform = 'translateX(-20px)';
                    setTimeout(() => {
                        row.style.transition = 'all 0.3s ease';
                        row.style.opacity = '1';
                        row.style.transform = 'translateX(0)';
                    }, 100);
                }, index * 100);
            });
        }
        
        // Run animation on load
        window.addEventListener('load', animateQuery);
    </script>
</body>
</html>