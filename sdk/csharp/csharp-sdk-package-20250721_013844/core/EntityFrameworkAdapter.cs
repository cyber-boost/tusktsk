using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace TuskLang.Database
{
    /// <summary>
    /// Production-ready Entity Framework Core adapter for TuskTsk
    /// Provides full ORM capabilities with real database connectivity
    /// </summary>
    public class EntityFrameworkAdapter<TContext> : IDisposable where TContext : DbContext
    {
        private readonly TContext _context;
        private readonly ILogger<EntityFrameworkAdapter<TContext>> _logger;
        private readonly DatabaseFacade _database;
        private bool _disposed = false;

        public EntityFrameworkAdapter(TContext context, ILogger<EntityFrameworkAdapter<TContext>> logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<EntityFrameworkAdapter<TContext>>.Instance;
            _database = _context.Database;
        }

        #region Query Operations

        /// <summary>
        /// Get all entities of type T
        /// </summary>
        public async Task<List<T>> GetAllAsync<T>() where T : class
        {
            try
            {
                var entities = await _context.Set<T>().ToListAsync();
                _logger.LogDebug($"Retrieved {entities.Count} entities of type {typeof(T).Name}");
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving all entities of type {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Get entity by ID
        /// </summary>
        public async Task<T> GetByIdAsync<T>(object id) where T : class
        {
            try
            {
                var entity = await _context.Set<T>().FindAsync(id);
                _logger.LogDebug($"Retrieved entity of type {typeof(T).Name} with ID: {id}");
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving entity of type {typeof(T).Name} with ID: {id}");
                throw;
            }
        }

        /// <summary>
        /// Get entities with filtering, sorting, and paging
        /// </summary>
        public async Task<PagedResult<T>> GetPagedAsync<T>(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "",
            int page = 1,
            int pageSize = 10) where T : class
        {
            try
            {
                IQueryable<T> query = _context.Set<T>();

                if (filter != null)
                {
                    query = query.Where(filter);
                }

                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }

                var totalCount = await query.CountAsync();

                if (orderBy != null)
                {
                    query = orderBy(query);
                }

                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogDebug($"Retrieved {items.Count} entities of type {typeof(T).Name} (page {page}, size {pageSize})");

                return new PagedResult<T>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving paged entities of type {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Execute raw SQL query and return entities
        /// </summary>
        public async Task<List<T>> FromSqlAsync<T>(string sql, params object[] parameters) where T : class
        {
            try
            {
                var entities = await _context.Set<T>().FromSqlRaw(sql, parameters).ToListAsync();
                _logger.LogDebug($"Raw SQL query returned {entities.Count} entities of type {typeof(T).Name}");
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing raw SQL query for type {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Check if any entities match the condition
        /// </summary>
        public async Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class
        {
            try
            {
                var exists = predicate == null 
                    ? await _context.Set<T>().AnyAsync()
                    : await _context.Set<T>().AnyAsync(predicate);
                
                _logger.LogDebug($"Existence check for type {typeof(T).Name}: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking existence for type {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Count entities matching condition
        /// </summary>
        public async Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class
        {
            try
            {
                var count = predicate == null
                    ? await _context.Set<T>().CountAsync()
                    : await _context.Set<T>().CountAsync(predicate);

                _logger.LogDebug($"Count for type {typeof(T).Name}: {count}");
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error counting entities of type {typeof(T).Name}");
                throw;
            }
        }

        #endregion

        #region Command Operations

        /// <summary>
        /// Add new entity
        /// </summary>
        public async Task<T> AddAsync<T>(T entity) where T : class
        {
            try
            {
                var entry = await _context.Set<T>().AddAsync(entity);
                await _context.SaveChangesAsync();
                
                _logger.LogDebug($"Added new entity of type {typeof(T).Name}");
                return entry.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding entity of type {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Add multiple entities
        /// </summary>
        public async Task<List<T>> AddRangeAsync<T>(IEnumerable<T> entities) where T : class
        {
            try
            {
                var entityList = entities.ToList();
                await _context.Set<T>().AddRangeAsync(entityList);
                await _context.SaveChangesAsync();
                
                _logger.LogDebug($"Added {entityList.Count} entities of type {typeof(T).Name}");
                return entityList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding multiple entities of type {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Update entity
        /// </summary>
        public async Task<T> UpdateAsync<T>(T entity) where T : class
        {
            try
            {
                var entry = _context.Set<T>().Update(entity);
                await _context.SaveChangesAsync();
                
                _logger.LogDebug($"Updated entity of type {typeof(T).Name}");
                return entry.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity of type {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Update multiple entities
        /// </summary>
        public async Task<List<T>> UpdateRangeAsync<T>(IEnumerable<T> entities) where T : class
        {
            try
            {
                var entityList = entities.ToList();
                _context.Set<T>().UpdateRange(entityList);
                await _context.SaveChangesAsync();
                
                _logger.LogDebug($"Updated {entityList.Count} entities of type {typeof(T).Name}");
                return entityList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating multiple entities of type {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Delete entity
        /// </summary>
        public async Task<bool> DeleteAsync<T>(T entity) where T : class
        {
            try
            {
                _context.Set<T>().Remove(entity);
                var rowsAffected = await _context.SaveChangesAsync();
                
                _logger.LogDebug($"Deleted entity of type {typeof(T).Name}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity of type {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Delete entity by ID
        /// </summary>
        public async Task<bool> DeleteByIdAsync<T>(object id) where T : class
        {
            try
            {
                var entity = await _context.Set<T>().FindAsync(id);
                if (entity == null)
                    return false;

                _context.Set<T>().Remove(entity);
                var rowsAffected = await _context.SaveChangesAsync();
                
                _logger.LogDebug($"Deleted entity of type {typeof(T).Name} with ID: {id}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity of type {typeof(T).Name} with ID: {id}");
                throw;
            }
        }

        /// <summary>
        /// Delete multiple entities
        /// </summary>
        public async Task<int> DeleteRangeAsync<T>(IEnumerable<T> entities) where T : class
        {
            try
            {
                var entityList = entities.ToList();
                _context.Set<T>().RemoveRange(entityList);
                var rowsAffected = await _context.SaveChangesAsync();
                
                _logger.LogDebug($"Deleted {rowsAffected} entities of type {typeof(T).Name}");
                return rowsAffected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting multiple entities of type {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Execute raw SQL command
        /// </summary>
        public async Task<int> ExecuteSqlAsync(string sql, params object[] parameters)
        {
            try
            {
                var rowsAffected = await _database.ExecuteSqlRawAsync(sql, parameters);
                _logger.LogDebug($"Raw SQL command affected {rowsAffected} rows");
                return rowsAffected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing raw SQL command");
                throw;
            }
        }

        #endregion

        #region Transaction Support

        /// <summary>
        /// Execute operations within a transaction
        /// </summary>
        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
        {
            using var transaction = await _database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                
                _logger.LogDebug("Transaction committed successfully");
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction rolled back due to error");
                throw;
            }
        }

        /// <summary>
        /// Begin transaction manually
        /// </summary>
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            var transaction = await _database.BeginTransactionAsync();
            _logger.LogDebug("Transaction started");
            return transaction;
        }

        #endregion

        #region Database Operations

        /// <summary>
        /// Ensure database is created
        /// </summary>
        public async Task<bool> EnsureCreatedAsync()
        {
            try
            {
                var created = await _database.EnsureCreatedAsync();
                _logger.LogInformation($"Database ensure created: {created}");
                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring database is created");
                throw;
            }
        }

        /// <summary>
        /// Apply pending migrations
        /// </summary>
        public async Task MigrateAsync()
        {
            try
            {
                await _database.MigrateAsync();
                _logger.LogInformation("Database migrations applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying database migrations");
                throw;
            }
        }

        /// <summary>
        /// Check if database exists
        /// </summary>
        public async Task<bool> CanConnectAsync()
        {
            try
            {
                var canConnect = await _database.CanConnectAsync();
                _logger.LogDebug($"Database connectivity check: {canConnect}");
                return canConnect;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database connectivity");
                return false;
            }
        }

        /// <summary>
        /// Get pending migrations
        /// </summary>
        public async Task<IEnumerable<string>> GetPendingMigrationsAsync()
        {
            try
            {
                var pendingMigrations = await _database.GetPendingMigrationsAsync();
                _logger.LogDebug($"Found {pendingMigrations.Count()} pending migrations");
                return pendingMigrations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending migrations");
                throw;
            }
        }

        /// <summary>
        /// Get applied migrations
        /// </summary>
        public async Task<IEnumerable<string>> GetAppliedMigrationsAsync()
        {
            try
            {
                var appliedMigrations = await _database.GetAppliedMigrationsAsync();
                _logger.LogDebug($"Found {appliedMigrations.Count()} applied migrations");
                return appliedMigrations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applied migrations");
                throw;
            }
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Bulk insert entities (more efficient for large datasets)
        /// </summary>
        public async Task BulkInsertAsync<T>(IEnumerable<T> entities, int batchSize = 1000) where T : class
        {
            try
            {
                var entityList = entities.ToList();
                var batches = entityList.Batch(batchSize);

                foreach (var batch in batches)
                {
                    await _context.Set<T>().AddRangeAsync(batch);
                    await _context.SaveChangesAsync();
                    
                    // Clear change tracker to avoid memory issues
                    _context.ChangeTracker.Clear();
                }

                _logger.LogDebug($"Bulk inserted {entityList.Count} entities of type {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error bulk inserting entities of type {typeof(T).Name}");
                throw;
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _context?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Base DbContext for TuskTsk data models
    /// </summary>
    public class TuskTskDbContext : DbContext
    {
        public TuskTskDbContext(DbContextOptions<TuskTskDbContext> options) : base(options)
        {
        }

        // Define your entity sets here
        public DbSet<TuskConfiguration> Configurations { get; set; }
        public DbSet<TuskData> TuskData { get; set; }
        public DbSet<TuskLog> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity mappings
            modelBuilder.Entity<TuskConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Value).IsRequired();
                entity.HasIndex(e => e.Key).IsUnique();
            });

            modelBuilder.Entity<TuskData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Data).IsRequired();
            });

            modelBuilder.Entity<TuskLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Level).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }

    /// <summary>
    /// Entity models
    /// </summary>
    [Table("tusk_configurations")]
    public class TuskConfiguration
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Key { get; set; }
        
        [Required]
        public string Value { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
    }

    [Table("tusk_data")]
    public class TuskData
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; }
        
        [Required]
        public string Data { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("tusk_logs")]
    public class TuskLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Level { get; set; }
        
        [Required]
        public string Message { get; set; }
        
        public string Exception { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Paged result container
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    /// <summary>
    /// Extension methods
    /// </summary>
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
        {
            T[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                    bucket = new T[size];

                bucket[count++] = item;

                if (count != size)
                    continue;

                yield return bucket.Take(count);

                bucket = null;
                count = 0;
            }

            // Return the last bucket with all remaining elements
            if (bucket != null && count > 0)
            {
                yield return bucket.Take(count);
            }
        }
    }
} 