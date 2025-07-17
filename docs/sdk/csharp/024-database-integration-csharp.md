# 🗄️ Database Integration - TuskLang for C# - "Data Mastery"

**Master database integration with TuskLang in your C# applications!**

Database integration is crucial for data-driven applications. This guide covers ORM integration, query optimization, migrations, and real-world database scenarios for TuskLang in C# environments.

## 🗃️ Database Philosophy

### "We Don't Bow to Any King"
- **Type-safe queries** - Compile-time query validation
- **Performance first** - Optimize for speed and efficiency
- **Migration safety** - Safe, reversible database changes
- **Connection pooling** - Efficient resource management
- **Real-time data** - Live data integration with configs

## 🔗 Entity Framework Integration

### Example: EF Core with TuskLang
```csharp
// DatabaseService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class DatabaseService
{
    private readonly ApplicationDbContext _context;
    private readonly TuskLang _parser;
    private readonly ILogger<DatabaseService> _logger;
    
    public DatabaseService(ApplicationDbContext context, ILogger<DatabaseService> logger)
    {
        _context = context;
        _parser = new TuskLang();
        _logger = logger;
    }
    
    public async Task<Dictionary<string, object>> LoadDatabaseConfigurationAsync()
    {
        var config = new Dictionary<string, object>();
        
        // Load user statistics
        config["total_users"] = await GetTotalUsersAsync();
        config["active_users"] = await GetActiveUsersAsync();
        config["user_growth_rate"] = await GetUserGrowthRateAsync();
        
        // Load system metrics
        config["database_size"] = await GetDatabaseSizeAsync();
        config["table_counts"] = await GetTableCountsAsync();
        
        return config;
    }
    
    private async Task<int> GetTotalUsersAsync()
    {
        return await _context.Users.CountAsync();
    }
    
    private async Task<int> GetActiveUsersAsync()
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        return await _context.Users
            .Where(u => u.LastLoginDate >= thirtyDaysAgo)
            .CountAsync();
    }
    
    private async Task<double> GetUserGrowthRateAsync()
    {
        var currentMonth = await _context.Users
            .Where(u => u.CreatedDate >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();
            
        var previousMonth = await _context.Users
            .Where(u => u.CreatedDate >= DateTime.UtcNow.AddDays(-60) && 
                       u.CreatedDate < DateTime.UtcNow.AddDays(-30))
            .CountAsync();
            
        if (previousMonth == 0) return 0;
        return ((double)(currentMonth - previousMonth) / previousMonth) * 100;
    }
    
    private async Task<long> GetDatabaseSizeAsync()
    {
        var result = await _context.Database
            .SqlQueryRaw<long>("SELECT pg_database_size(current_database())")
            .FirstOrDefaultAsync();
        return result;
    }
    
    private async Task<Dictionary<string, int>> GetTableCountsAsync()
    {
        var tableCounts = new Dictionary<string, int>();
        
        tableCounts["users"] = await _context.Users.CountAsync();
        tableCounts["orders"] = await _context.Orders.CountAsync();
        tableCounts["products"] = await _context.Products.CountAsync();
        
        return tableCounts;
    }
}
```

## 🔍 Query Optimization

### Example: Optimized Database Queries
```csharp
// QueryOptimizationService.cs
public class QueryOptimizationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QueryOptimizationService> _logger;
    
    public QueryOptimizationService(ApplicationDbContext context, ILogger<QueryOptimizationService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<List<User>> GetUsersWithOptimizedQueryAsync()
    {
        // Use projection to select only needed fields
        return await _context.Users
            .Select(u => new User
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email
            })
            .AsNoTracking() // Don't track entities for read-only operations
            .ToListAsync();
    }
    
    public async Task<Dictionary<string, object>> GetAggregatedDataAsync()
    {
        var aggregatedData = new Dictionary<string, object>();
        
        // Use raw SQL for complex aggregations
        var userStats = await _context.Database
            .SqlQueryRaw<UserStats>(@"
                SELECT 
                    COUNT(*) as TotalUsers,
                    COUNT(CASE WHEN last_login_date >= NOW() - INTERVAL '30 days' THEN 1 END) as ActiveUsers,
                    AVG(EXTRACT(EPOCH FROM (NOW() - created_date))/86400) as AverageUserAge
                FROM users")
            .FirstOrDefaultAsync();
            
        aggregatedData["total_users"] = userStats?.TotalUsers ?? 0;
        aggregatedData["active_users"] = userStats?.ActiveUsers ?? 0;
        aggregatedData["average_user_age_days"] = userStats?.AverageUserAge ?? 0;
        
        return aggregatedData;
    }
}

public class UserStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public double AverageUserAge { get; set; }
}
```

## 🔄 Database Migrations

### Example: TuskLang-Driven Migrations
```csharp
// MigrationService.cs
public class MigrationService
{
    private readonly ApplicationDbContext _context;
    private readonly TuskLang _parser;
    private readonly ILogger<MigrationService> _logger;
    
    public MigrationService(ApplicationDbContext context, ILogger<MigrationService> logger)
    {
        _context = context;
        _parser = new TuskLang();
        _logger = logger;
    }
    
    public async Task RunMigrationsAsync()
    {
        var migrationConfig = _parser.ParseFile("migrations/migration-config.tsk");
        
        foreach (var migration in migrationConfig["migrations"] as List<object>)
        {
            var migrationDict = migration as Dictionary<string, object>;
            await ExecuteMigrationAsync(migrationDict);
        }
    }
    
    private async Task ExecuteMigrationAsync(Dictionary<string, object> migration)
    {
        var migrationName = migration["name"].ToString();
        var sql = migration["sql"].ToString();
        var isReversible = (bool)migration["reversible"];
        
        _logger.LogInformation("Executing migration: {MigrationName}", migrationName);
        
        try
        {
            await _context.Database.ExecuteSqlRawAsync(sql);
            _logger.LogInformation("Migration {MigrationName} completed successfully", migrationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration {MigrationName} failed", migrationName);
            throw;
        }
    }
}
```

## 📊 Real-Time Data Integration

### Example: Live Data with TuskLang
```csharp
// RealTimeDataService.cs
public class RealTimeDataService
{
    private readonly ApplicationDbContext _context;
    private readonly TuskLang _parser;
    private readonly ILogger<RealTimeDataService> _logger;
    
    public RealTimeDataService(ApplicationDbContext context, ILogger<RealTimeDataService> logger)
    {
        _context = context;
        _parser = new TuskLang();
        _logger = logger;
    }
    
    public async Task<Dictionary<string, object>> GetLiveConfigurationAsync()
    {
        var config = new Dictionary<string, object>();
        
        // Real-time user metrics
        config["current_online_users"] = await GetCurrentOnlineUsersAsync();
        config["recent_orders"] = await GetRecentOrdersAsync();
        config["system_health"] = await GetSystemHealthAsync();
        
        return config;
    }
    
    private async Task<int> GetCurrentOnlineUsersAsync()
    {
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
        return await _context.Users
            .Where(u => u.LastActivity >= fiveMinutesAgo)
            .CountAsync();
    }
    
    private async Task<List<Order>> GetRecentOrdersAsync()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        return await _context.Orders
            .Where(o => o.CreatedDate >= oneHourAgo)
            .OrderByDescending(o => o.CreatedDate)
            .Take(10)
            .ToListAsync();
    }
    
    private async Task<Dictionary<string, object>> GetSystemHealthAsync()
    {
        var health = new Dictionary<string, object>();
        
        // Database connection health
        health["database_connected"] = await TestDatabaseConnectionAsync();
        
        // Query performance
        health["avg_query_time_ms"] = await GetAverageQueryTimeAsync();
        
        // Connection pool status
        health["active_connections"] = await GetActiveConnectionsAsync();
        
        return health;
    }
    
    private async Task<bool> TestDatabaseConnectionAsync()
    {
        try
        {
            await _context.Database.OpenConnectionAsync();
            await _context.Database.CloseConnectionAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<double> GetAverageQueryTimeAsync()
    {
        // This would typically come from your monitoring system
        // For now, we'll return a placeholder
        return 15.5;
    }
    
    private async Task<int> GetActiveConnectionsAsync()
    {
        var result = await _context.Database
            .SqlQueryRaw<int>("SELECT count(*) FROM pg_stat_activity WHERE state = 'active'")
            .FirstOrDefaultAsync();
        return result;
    }
}
```

## 🛠️ Real-World Database Scenarios
- **E-commerce analytics**: Real-time sales and inventory data
- **User management**: Live user statistics and metrics
- **System monitoring**: Database health and performance metrics
- **Reporting**: Aggregated data for dashboards

## 🧩 Best Practices
- Use connection pooling
- Optimize queries with projections
- Implement proper indexing
- Use transactions for data consistency
- Monitor query performance

## 🏁 You're Ready!

You can now:
- Integrate databases with C# TuskLang apps
- Optimize queries for performance
- Implement database migrations
- Use real-time data in configurations

**Next:** [API Integration](025-api-integration-csharp.md)

---

**"We don't bow to any king" - Your data mastery, your query optimization, your database excellence.**

Master your data. Optimize your queries. 🗄️ 