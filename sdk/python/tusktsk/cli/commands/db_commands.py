#!/usr/bin/env python3
"""
Database Commands for TuskLang Python CLI
=========================================
Implements all database-related commands
"""

import os
import sqlite3
import subprocess
import datetime
import time
import psutil
import threading
from pathlib import Path
from typing import Any, Dict, Optional, List, Tuple
import json

# Import adapters with try/except to handle missing dependencies gracefully
try:
    from ...adapters import SQLiteAdapter, PostgreSQLAdapter, MongoDBAdapter, RedisAdapter
    ADAPTERS_AVAILABLE = True
except ImportError:
    ADAPTERS_AVAILABLE = False
    # Create dummy classes for when adapters are not available
    class SQLiteAdapter:
        def __init__(self, config): pass
        def is_connected(self): return False
    class PostgreSQLAdapter:
        def __init__(self, config): pass
        def is_connected(self): return False
    class MongoDBAdapter:
        def __init__(self, config): pass
        def is_connected(self): return False
    class RedisAdapter:
        def __init__(self, config): pass
        def is_connected(self): return False

from ..utils.output_formatter import OutputFormatter
from ..utils.error_handler import ErrorHandler
from ..utils.config_loader import ConfigLoader


def handle_db_command(args: Any, cli: Any) -> int:
    """Handle database commands"""
    formatter = OutputFormatter(cli.json_output, cli.quiet, cli.verbose)
    error_handler = ErrorHandler(cli.json_output, cli.verbose)
    config_loader = ConfigLoader(cli.config_path)
    
    try:
        if args.db_command == 'status':
            return _handle_db_status(formatter, config_loader)
        elif args.db_command == 'health':
            return _handle_db_health(formatter, config_loader)
        elif args.db_command == 'query':
            return _handle_db_query(args, formatter, error_handler, config_loader)
        elif args.db_command == 'migrate':
            return _handle_db_migrate(args, formatter, error_handler)
        elif args.db_command == 'rollback':
            return _handle_db_rollback(args, formatter, error_handler, config_loader)
        elif args.db_command == 'optimize':
            return _handle_db_optimize(formatter, error_handler, config_loader)
        elif args.db_command == 'vacuum':
            return _handle_db_vacuum(formatter, error_handler, config_loader)
        elif args.db_command == 'reindex':
            return _handle_db_reindex(formatter, error_handler, config_loader)
        elif args.db_command == 'analyze':
            return _handle_db_analyze(formatter, error_handler, config_loader)
        elif args.db_command == 'connections':
            return _handle_db_connections(formatter, error_handler, config_loader)
        elif args.db_command == 'console':
            return _handle_db_console(formatter, config_loader)
        elif args.db_command == 'backup':
            return _handle_db_backup(args, formatter, error_handler, config_loader)
        elif args.db_command == 'restore':
            return _handle_db_restore(args, formatter, error_handler)
        elif args.db_command == 'init':
            return _handle_db_init(formatter, config_loader)
        else:
            formatter.error("Unknown database command")
            return ErrorHandler.INVALID_ARGS
            
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_db_status(formatter: OutputFormatter, config_loader: ConfigLoader) -> int:
    """Handle db status command"""
    formatter.loading("Checking database connections...")
    
    # Load configuration
    config = config_loader.load_config()
    db_config = config.get('database', {})
    default_db = db_config.get('default', 'sqlite')
    
    status_results = []
    
    # Check SQLite
    if 'sqlite' in db_config or default_db == 'sqlite':
        try:
            sqlite_config = db_config.get('sqlite', {})
            db_path = sqlite_config.get('database', './tusklang.db')
            
            adapter = SQLiteAdapter({'database': db_path})
            if adapter.is_connected():
                status_results.append(['SQLite', '✅ Connected', db_path])
            else:
                status_results.append(['SQLite', '❌ Disconnected', db_path])
        except Exception as e:
            status_results.append(['SQLite', f'❌ Error: {str(e)}', 'N/A'])
    
    # Check PostgreSQL
    if 'postgresql' in db_config or default_db == 'postgresql':
        try:
            pg_config = db_config.get('postgresql', {})
            adapter = PostgreSQLAdapter(pg_config)
            if adapter.is_connected():
                status_results.append(['PostgreSQL', '✅ Connected', f"{pg_config.get('host', 'localhost')}:{pg_config.get('port', 5432)}"])
            else:
                status_results.append(['PostgreSQL', '❌ Disconnected', f"{pg_config.get('host', 'localhost')}:{pg_config.get('port', 5432)}"])
        except Exception as e:
            status_results.append(['PostgreSQL', f'❌ Error: {str(e)}', 'N/A'])
    
    # Check MongoDB
    if 'mongodb' in db_config or default_db == 'mongodb':
        try:
            mongo_config = db_config.get('mongodb', {})
            adapter = MongoDBAdapter(mongo_config)
            if adapter.is_connected():
                status_results.append(['MongoDB', '✅ Connected', mongo_config.get('url', 'mongodb://localhost:27017')])
            else:
                status_results.append(['MongoDB', '❌ Disconnected', mongo_config.get('url', 'mongodb://localhost:27017')])
        except Exception as e:
            status_results.append(['MongoDB', f'❌ Error: {str(e)}', 'N/A'])
    
    # Check Redis
    if 'redis' in db_config or default_db == 'redis':
        try:
            redis_config = db_config.get('redis', {})
            adapter = RedisAdapter(redis_config)
            adapter.connect()
            status_results.append(['Redis', '✅ Connected', f"{redis_config.get('host', 'localhost')}:{redis_config.get('port', 6379)}"])
        except Exception as e:
            status_results.append(['Redis', f'❌ Error: {str(e)}', 'N/A'])
    
    # Display results
    formatter.table(
        ['Database', 'Status', 'Connection'],
        status_results,
        'Database Connection Status'
    )
    
    return ErrorHandler.SUCCESS


def _handle_db_health(formatter: OutputFormatter, config_loader: ConfigLoader) -> int:
    """Handle db health command with comprehensive health checks"""
    formatter.loading("Performing comprehensive database health check...")
    
    # Load configuration
    config = config_loader.load_config()
    db_config = config.get('database', {})
    default_db = db_config.get('default', 'sqlite')
    
    health_results = []
    
    # Check SQLite health
    if 'sqlite' in db_config or default_db == 'sqlite':
        try:
            sqlite_config = db_config.get('sqlite', {})
            db_path = sqlite_config.get('database', './tusklang.db')
            
            if not Path(db_path).exists():
                health_results.append(['SQLite', '❌ Database file not found', 'Critical'])
                return 0
            
            # Check file size and permissions
            db_file = Path(db_path)
            file_size = db_file.stat().st_size
            file_permissions = oct(db_file.stat().st_mode)[-3:]
            
            # Test connection and basic operations
            adapter = SQLiteAdapter({'database': db_path})
            if not adapter.is_connected():
                adapter.connect()
            
            # Test basic query
            start_time = time.time()
            result = adapter.query("SELECT 1 as test")
            query_time = time.time() - start_time
            
            # Check database integrity
            integrity_result = adapter.query("PRAGMA integrity_check")
            integrity_ok = integrity_result and len(integrity_result) > 0 and integrity_result[0].get('integrity_check') == 'ok'
            
            # Get database statistics
            stats_result = adapter.query("PRAGMA database_list")
            page_count_result = adapter.query("PRAGMA page_count")
            page_count = page_count_result[0]['page_count'] if page_count_result else 0
            page_size_result = adapter.query("PRAGMA page_size")
            page_size = page_size_result[0]['page_size'] if page_size_result else 0
            db_size = page_count * page_size
            
            # Determine health status
            if integrity_ok and query_time < 0.1:
                status = "✅ Healthy"
                severity = "Good"
            elif integrity_ok:
                status = "⚠️  Slow"
                severity = "Warning"
            else:
                status = "❌ Corrupted"
                severity = "Critical"
            
            health_results.append([
                'SQLite', 
                status,
                f"Size: {file_size/1024:.1f}KB, Query: {query_time*1000:.1f}ms, Integrity: {'OK' if integrity_ok else 'FAIL'}"
            ])
            
            adapter.close()
            
        except Exception as e:
            health_results.append(['SQLite', f'❌ Error: {str(e)}', 'Critical'])
    
    # Check PostgreSQL health
    if 'postgresql' in db_config or default_db == 'postgresql':
        try:
            pg_config = db_config.get('postgresql', {})
            adapter = PostgreSQLAdapter(pg_config)
            
            if not adapter.is_connected():
                adapter.connect()
            
            # Test connection and performance
            start_time = time.time()
            result = adapter.query("SELECT 1 as test")
            query_time = time.time() - start_time
            
            # Get PostgreSQL statistics
            version_result = adapter.query("SELECT version()")
            version = version_result[0]['version'] if version_result else 'Unknown'
            
            # Check active connections
            connections_result = adapter.query("SELECT count(*) as active_connections FROM pg_stat_activity WHERE state = 'active'")
            active_connections = connections_result[0]['active_connections'] if connections_result else 0
            
            # Check database size
            size_result = adapter.query("SELECT pg_size_pretty(pg_database_size(current_database())) as db_size")
            db_size = size_result[0]['db_size'] if size_result else 'Unknown'
            
            # Determine health status
            if query_time < 0.1 and active_connections < 100:
                status = "✅ Healthy"
                severity = "Good"
            elif query_time < 0.5:
                status = "⚠️  Slow"
                severity = "Warning"
            else:
                status = "❌ Performance Issues"
                severity = "Critical"
            
            health_results.append([
                'PostgreSQL',
                status,
                f"Version: {version.split()[1]}, Connections: {active_connections}, Size: {db_size}, Query: {query_time*1000:.1f}ms"
            ])
            
            adapter.close()
            
        except Exception as e:
            health_results.append(['PostgreSQL', f'❌ Error: {str(e)}', 'Critical'])
    
    # Check MongoDB health
    if 'mongodb' in db_config or default_db == 'mongodb':
        try:
            mongo_config = db_config.get('mongodb', {})
            adapter = MongoDBAdapter(mongo_config)
            
            if not adapter.is_connected():
                adapter.connect()
            
            # Test basic operation
            start_time = time.time()
            result = adapter.query("test.find", {"_id": {"$exists": False}})
            query_time = time.time() - start_time
            
            # Get MongoDB server info
            server_info = adapter.query("admin.command", {"serverStatus": 1})
            
            # Check connection pool
            connections = server_info.get('connections', {}) if server_info else {}
            active_connections = connections.get('active', 0)
            available_connections = connections.get('available', 0)
            
            # Determine health status
            if query_time < 0.1 and active_connections < 100:
                status = "✅ Healthy"
                severity = "Good"
            elif query_time < 0.5:
                status = "⚠️  Slow"
                severity = "Warning"
            else:
                status = "❌ Performance Issues"
                severity = "Critical"
            
            health_results.append([
                'MongoDB',
                status,
                f"Connections: {active_connections}/{available_connections}, Query: {query_time*1000:.1f}ms"
            ])
            
            adapter.close()
            
        except Exception as e:
            health_results.append(['MongoDB', f'❌ Error: {str(e)}', 'Critical'])
    
    # Check Redis health
    if 'redis' in db_config or default_db == 'redis':
        try:
            redis_config = db_config.get('redis', {})
            adapter = RedisAdapter(redis_config)
            adapter.connect()
            
            # Test basic operations
            start_time = time.time()
            adapter.query("SET", "health_test", "ok")
            result = adapter.query("GET", "health_test")
            adapter.query("DEL", "health_test")
            query_time = time.time() - start_time
            
            # Get Redis info
            info_result = adapter.query("INFO", "server")
            info_lines = info_result.split('\n') if info_result else []
            redis_version = "Unknown"
            for line in info_lines:
                if line.startswith('redis_version:'):
                    redis_version = line.split(':')[1]
                    break
            
            # Get memory usage
            memory_result = adapter.query("INFO", "memory")
            memory_lines = memory_result.split('\n') if memory_result else []
            used_memory = "Unknown"
            for line in memory_lines:
                if line.startswith('used_memory_human:'):
                    used_memory = line.split(':')[1]
                    break
            
            # Determine health status
            if query_time < 0.01:
                status = "✅ Healthy"
                severity = "Good"
            elif query_time < 0.1:
                status = "⚠️  Slow"
                severity = "Warning"
            else:
                status = "❌ Performance Issues"
                severity = "Critical"
            
            health_results.append([
                'Redis',
                status,
                f"Version: {redis_version}, Memory: {used_memory}, Query: {query_time*1000:.1f}ms"
            ])
            
            adapter.close()
            
        except Exception as e:
            health_results.append(['Redis', f'❌ Error: {str(e)}', 'Critical'])
    
    # Display results
    formatter.table(
        ['Database', 'Health Status', 'Details'],
        health_results,
        'Database Health Check Results'
    )
    
    # Summary
    healthy_count = len([r for r in health_results if '✅' in r[1]])
    total_count = len(health_results)
    
    if healthy_count == total_count:
        formatter.success(f"All databases ({total_count}) are healthy!")
        return ErrorHandler.SUCCESS
    elif healthy_count > 0:
        formatter.warning(f"{healthy_count}/{total_count} databases are healthy")
        return ErrorHandler.GENERAL_ERROR
    else:
        formatter.error("All databases have health issues!")
        return ErrorHandler.CONNECTION_ERROR


def _handle_db_query(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle db query command with parameter support and result formatting"""
    if not args.sql:
        formatter.error("SQL query is required")
        return ErrorHandler.INVALID_ARGS
    
    formatter.loading(f"Executing query: {args.sql[:50]}{'...' if len(args.sql) > 50 else ''}")
    
    try:
        # Load configuration
        config = config_loader.load_config()
        db_config = config.get('database', {})
        default_db = db_config.get('default', 'sqlite')
        
        # Parse parameters if provided
        params = []
        if args.params:
            try:
                params = json.loads(args.params)
                if not isinstance(params, list):
                    params = [params]
            except json.JSONDecodeError:
                formatter.error("Invalid JSON parameters")
                return ErrorHandler.INVALID_ARGS
        
        # Connect to database
        if default_db == 'sqlite':
            adapter = SQLiteAdapter({'database': './tusklang.db'})
        elif default_db == 'postgresql':
            adapter = PostgreSQLAdapter(db_config.get('postgresql', {}))
        elif default_db == 'mongodb':
            adapter = MongoDBAdapter(db_config.get('mongodb', {}))
        elif default_db == 'redis':
            adapter = RedisAdapter(db_config.get('redis', {}))
        else:
            formatter.error(f"Unsupported database type: {default_db}")
            return ErrorHandler.CONFIG_ERROR
        
        if not adapter.is_connected():
            adapter.connect()
        
        # Execute query with timing
        start_time = time.time()
        
        if default_db in ['sqlite', 'postgresql']:
            result = adapter.query(args.sql, params)
        elif default_db == 'mongodb':
            # Parse MongoDB-style query
            if args.sql.startswith('db.'):
                collection = args.sql.split('.')[1].split('(')[0]
                operation = args.sql.split('(')[0].split('.')[-1]
                query_part = args.sql.split('(', 1)[1].rstrip(')')
                
                if operation == 'find':
                    # Parse find query
                    try:
                        query_dict = json.loads(query_part) if query_part else {}
                    except json.JSONDecodeError:
                        query_dict = {}
                    result = adapter.query(f"{collection}.find", query_dict)
                elif operation == 'insert':
                    try:
                        doc = json.loads(query_part)
                        result = adapter.query(f"{collection}.insert", doc)
                    except json.JSONDecodeError:
                        formatter.error("Invalid JSON document for insert")
                        return ErrorHandler.INVALID_ARGS
                else:
                    formatter.error(f"Unsupported MongoDB operation: {operation}")
                    return ErrorHandler.INVALID_ARGS
            else:
                formatter.error("MongoDB queries must start with 'db.collection.operation'")
                return ErrorHandler.INVALID_ARGS
        elif default_db == 'redis':
            # Parse Redis command
            parts = args.sql.split()
            command = parts[0].upper()
            cmd_params = parts[1:] if len(parts) > 1 else []
            result = adapter.query(command, *cmd_params)
        
        query_time = time.time() - start_time
        
        # Format and display results
        if result is None:
            formatter.success("Query executed successfully (no results)")
        elif isinstance(result, list):
            if len(result) == 0:
                formatter.success("Query executed successfully (no rows returned)")
            else:
                # Display as table
                if len(result) > 0 and isinstance(result[0], dict):
                    headers = list(result[0].keys())
                    rows = [[str(row.get(col, '')) for col in headers] for row in result]
                    
                    formatter.table(
                        headers,
                        rows,
                        f'Query Results ({len(result)} rows, {query_time*1000:.1f}ms)'
                    )
                else:
                    formatter.success(f"Query returned {len(result)} rows in {query_time*1000:.1f}ms")
                    for i, row in enumerate(result[:10]):  # Show first 10 rows
                        formatter.info(f"Row {i+1}: {row}")
                    if len(result) > 10:
                        formatter.info(f"... and {len(result) - 10} more rows")
        else:
            formatter.success(f"Query result: {result} (executed in {query_time*1000:.1f}ms)")
        
        adapter.close()
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_db_rollback(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle db rollback command with safety checks and confirmation"""
    formatter.loading("Preparing migration rollback...")
    
    try:
        # Load configuration
        config = config_loader.load_config()
        db_config = config.get('database', {})
        default_db = db_config.get('default', 'sqlite')
        
        if default_db not in ['sqlite', 'postgresql']:
            formatter.error(f"Rollback not supported for database type: {default_db}")
            return ErrorHandler.CONFIG_ERROR
        
        # Get migration history
        if default_db == 'sqlite':
            adapter = SQLiteAdapter({'database': './tusklang.db'})
        else:
            adapter = PostgreSQLAdapter(db_config.get('postgresql', {}))
        
        if not adapter.is_connected():
            adapter.connect()
        
        # Check if migrations table exists
        try:
            migrations = adapter.query("SELECT * FROM migrations ORDER BY applied_at DESC")
        except Exception:
            formatter.error("No migrations table found. Cannot perform rollback.")
            return ErrorHandler.CONFIG_ERROR
        
        if not migrations:
            formatter.warning("No migrations found to rollback")
            return ErrorHandler.SUCCESS
        
        # Show recent migrations
        formatter.subsection("Recent Migrations")
        migration_rows = []
        for i, migration in enumerate(migrations[:10]):
            migration_rows.append([
                str(i + 1),
                migration.get('name', 'Unknown'),
                migration.get('applied_at', 'Unknown')
            ])
        
        formatter.table(
            ['#', 'Migration Name', 'Applied At'],
            migration_rows,
            'Recent Migrations'
        )
        
        # Safety confirmation
        if not args.force:
            formatter.warning("⚠️  WARNING: This will rollback the most recent migration!")
            formatter.warning("This operation cannot be undone and may result in data loss.")
            
            try:
                confirm = input("Type 'YES' to confirm rollback: ").strip()
                if confirm != 'YES':
                    formatter.info("Rollback cancelled")
                    return ErrorHandler.SUCCESS
            except KeyboardInterrupt:
                formatter.info("Rollback cancelled")
                return ErrorHandler.SUCCESS
        
        # Perform rollback
        latest_migration = migrations[0]
        migration_name = latest_migration.get('name', 'Unknown')
        
        formatter.loading(f"Rolling back migration: {migration_name}")
        
        # For SQLite, we'll create a backup before rollback
        if default_db == 'sqlite':
            backup_path = f"backup_before_rollback_{int(time.time())}.db"
            import shutil
            shutil.copy2('./tusklang.db', backup_path)
            formatter.info(f"Database backed up to: {backup_path}")
        
        # Remove the migration record
        adapter.query("DELETE FROM migrations WHERE name = ?", [migration_name])
        
        formatter.success(f"Successfully rolled back migration: {migration_name}")
        formatter.info("Note: You may need to manually reverse schema changes")
        
        adapter.close()
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_db_optimize(formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle db optimize command with index analysis and performance tuning"""
    formatter.loading("Analyzing database performance and optimizing...")
    
    try:
        # Load configuration
        config = config_loader.load_config()
        db_config = config.get('database', {})
        default_db = db_config.get('default', 'sqlite')
        
        if default_db not in ['sqlite', 'postgresql']:
            formatter.error(f"Optimization not supported for database type: {default_db}")
            return ErrorHandler.CONFIG_ERROR
        
        # Connect to database
        if default_db == 'sqlite':
            adapter = SQLiteAdapter({'database': './tusklang.db'})
        else:
            adapter = PostgreSQLAdapter(db_config.get('postgresql', {}))
        
        if not adapter.is_connected():
            adapter.connect()
        
        optimization_results = []
        
        if default_db == 'sqlite':
            # SQLite optimizations
            formatter.subsection("SQLite Optimizations")
            
            # Analyze current state
            page_count_result = adapter.query("PRAGMA page_count")
            page_count_before = page_count_result[0]['page_count'] if page_count_result else 0
            page_size_result = adapter.query("PRAGMA page_size")
            page_size_before = page_size_result[0]['page_size'] if page_size_result else 0
            cache_size_result = adapter.query("PRAGMA cache_size")
            cache_size_before = cache_size_result[0]['cache_size'] if cache_size_result else 0
            
            # Optimize settings
            adapter.query("PRAGMA optimize")
            adapter.query("PRAGMA cache_size = 10000")  # Increase cache size
            adapter.query("PRAGMA temp_store = memory")  # Use memory for temp tables
            adapter.query("PRAGMA journal_mode = WAL")  # Use WAL mode for better concurrency
            
            # Get optimized state
            page_count_result = adapter.query("PRAGMA page_count")
            page_count_after = page_count_result[0]['page_count'] if page_count_result else 0
            page_size_result = adapter.query("PRAGMA page_size")
            page_size_after = page_size_result[0]['page_size'] if page_size_result else 0
            cache_size_result = adapter.query("PRAGMA cache_size")
            cache_size_after = cache_size_result[0]['cache_size'] if cache_size_result else 0
            
            optimization_results.extend([
                ['Cache Size', f"{cache_size_before} -> {cache_size_after}", 'Increased for better performance'],
                ['Journal Mode', 'DELETE -> WAL', 'Better concurrency'],
                ['Temp Store', 'DEFAULT -> MEMORY', 'Faster temporary operations'],
                ['Page Count', f"{page_count_before} -> {page_count_after}", 'Optimized']
            ])
            
        elif default_db == 'postgresql':
            # PostgreSQL optimizations
            formatter.subsection("PostgreSQL Optimizations")
            
            # Analyze tables
            tables = adapter.query("""
                SELECT schemaname, tablename, n_tup_ins, n_tup_upd, n_tup_del, n_live_tup, n_dead_tup
                FROM pg_stat_user_tables
                ORDER BY n_dead_tup DESC
            """)
            
            # Show table statistics
            table_stats = []
            for table in tables[:10]:  # Show top 10 tables
                dead_tup_ratio = (table['n_dead_tup'] / (table['n_live_tup'] + table['n_dead_tup'])) * 100 if (table['n_live_tup'] + table['n_dead_tup']) > 0 else 0
                table_stats.append([
                    f"{table['schemaname']}.{table['tablename']}",
                    str(table['n_live_tup']),
                    str(table['n_dead_tup']),
                    f"{dead_tup_ratio:.1f}%"
                ])
            
            formatter.table(
                ['Table', 'Live Tuples', 'Dead Tuples', 'Dead Ratio'],
                table_stats,
                'Table Statistics'
            )
            
            # VACUUM tables with high dead tuple ratio
            vacuumed_tables = []
            for table in tables:
                if table['n_dead_tup'] > 1000:  # Only vacuum if significant dead tuples
                    table_name = f"{table['schemaname']}.{table['tablename']}"
                    try:
                        adapter.query(f"VACUUM ANALYZE {table_name}")
                        vacuumed_tables.append(table_name)
                    except Exception as e:
                        formatter.warning(f"Failed to vacuum {table_name}: {e}")
            
            if vacuumed_tables:
                optimization_results.append(['VACUUM', f"{len(vacuumed_tables)} tables", 'Cleaned up dead tuples'])
            
            # Analyze index usage
            index_stats = adapter.query("""
                SELECT schemaname, tablename, indexname, idx_scan, idx_tup_read, idx_tup_fetch
                FROM pg_stat_user_indexes
                ORDER BY idx_scan DESC
            """)
            
            unused_indexes = [idx for idx in index_stats if idx['idx_scan'] == 0]
            if unused_indexes:
                optimization_results.append(['Unused Indexes', f"{len(unused_indexes)} found", 'Consider dropping for better performance'])
        
        # Display optimization results
        formatter.table(
            ['Optimization', 'Change', 'Impact'],
            optimization_results,
            'Optimization Results'
        )
        
        formatter.success("Database optimization completed successfully!")
        
        adapter.close()
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_db_vacuum(formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle db vacuum command for SQLite databases"""
    formatter.loading("Performing database VACUUM operation...")
    
    try:
        # Load configuration
        config = config_loader.load_config()
        db_config = config.get('database', {})
        default_db = db_config.get('default', 'sqlite')
        
        if default_db != 'sqlite':
            formatter.error("VACUUM is only supported for SQLite databases")
            return ErrorHandler.CONFIG_ERROR
        
        # Connect to SQLite database
        adapter = SQLiteAdapter({'database': './tusklang.db'})
        if not adapter.is_connected():
            adapter.connect()
        
        # Get database size before vacuum
        page_count_result = adapter.query("PRAGMA page_count")
        page_count_before = page_count_result[0]['page_count'] if page_count_result else 0
        page_size_result = adapter.query("PRAGMA page_size")
        page_size_before = page_size_result[0]['page_size'] if page_size_result else 0
        size_before = page_count_before * page_size_before
        
        # Perform VACUUM
        formatter.info("VACUUM operation may take a while for large databases...")
        adapter.query("VACUUM")
        
        # Get database size after vacuum
        page_count_result = adapter.query("PRAGMA page_count")
        page_count_after = page_count_result[0]['page_count'] if page_count_result else 0
        page_size_result = adapter.query("PRAGMA page_size")
        page_size_after = page_size_result[0]['page_size'] if page_size_result else 0
        size_after = page_count_after * page_size_after
        
        # Calculate space saved
        space_saved = size_before - size_after
        space_saved_mb = space_saved / (1024 * 1024)
        
        formatter.success("VACUUM operation completed successfully!")
        formatter.info(f"Database size: {size_before/1024:.1f}KB -> {size_after/1024:.1f}KB")
        
        if space_saved > 0:
            formatter.success(f"Space saved: {space_saved_mb:.2f}MB")
        else:
            formatter.info("No space was reclaimed (database was already optimized)")
        
        adapter.close()
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_db_reindex(formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle db reindex command for rebuilding indexes"""
    formatter.loading("Rebuilding database indexes...")
    
    try:
        # Load configuration
        config = config_loader.load_config()
        db_config = config.get('database', {})
        default_db = db_config.get('default', 'sqlite')
        
        if default_db not in ['sqlite', 'postgresql']:
            formatter.error(f"Reindex not supported for database type: {default_db}")
            return ErrorHandler.CONFIG_ERROR
        
        # Connect to database
        if default_db == 'sqlite':
            adapter = SQLiteAdapter({'database': './tusklang.db'})
        else:
            adapter = PostgreSQLAdapter(db_config.get('postgresql', {}))
        
        if not adapter.is_connected():
            adapter.connect()
        
        reindex_results = []
        
        if default_db == 'sqlite':
            # SQLite REINDEX
            formatter.info("Rebuilding all SQLite indexes...")
            adapter.query("REINDEX")
            reindex_results.append(['All Indexes', 'Rebuilt', 'Complete'])
            
        elif default_db == 'postgresql':
            # PostgreSQL REINDEX
            formatter.info("Rebuilding PostgreSQL indexes...")
            
            # Get all user indexes
            indexes = adapter.query("""
                SELECT schemaname, tablename, indexname
                FROM pg_indexes
                WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
                ORDER BY schemaname, tablename, indexname
            """)
            
            reindexed_count = 0
            for index in indexes:
                index_name = f"{index['schemaname']}.{index['indexname']}"
                try:
                    adapter.query(f"REINDEX INDEX {index_name}")
                    reindexed_count += 1
                except Exception as e:
                    formatter.warning(f"Failed to reindex {index_name}: {e}")
            
            reindex_results.append(['User Indexes', f"{reindexed_count} rebuilt", 'Complete'])
        
        # Display results
        formatter.table(
            ['Operation', 'Status', 'Details'],
            reindex_results,
            'Reindex Results'
        )
        
        formatter.success("Index rebuilding completed successfully!")
        
        adapter.close()
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_db_analyze(formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle db analyze command for collecting database statistics"""
    formatter.loading("Collecting database statistics and analysis...")
    
    try:
        # Load configuration
        config = config_loader.load_config()
        db_config = config.get('database', {})
        default_db = db_config.get('default', 'sqlite')
        
        if default_db not in ['sqlite', 'postgresql']:
            formatter.error(f"Analyze not supported for database type: {default_db}")
            return ErrorHandler.CONFIG_ERROR
        
        # Connect to database
        if default_db == 'sqlite':
            adapter = SQLiteAdapter({'database': './tusklang.db'})
        else:
            adapter = PostgreSQLAdapter(db_config.get('postgresql', {}))
        
        if not adapter.is_connected():
            adapter.connect()
        
        analysis_results = []
        
        if default_db == 'sqlite':
            # SQLite analysis
            formatter.subsection("SQLite Database Analysis")
            
            # Get database statistics
            page_count_result = adapter.query("PRAGMA page_count")
            page_count = page_count_result[0]['page_count'] if page_count_result else 0
            page_size_result = adapter.query("PRAGMA page_size")
            page_size = page_size_result[0]['page_size'] if page_size_result else 0
            cache_size_result = adapter.query("PRAGMA cache_size")
            cache_size = cache_size_result[0]['cache_size'] if cache_size_result else 0
            journal_mode_result = adapter.query("PRAGMA journal_mode")
            journal_mode = journal_mode_result[0]['journal_mode'] if journal_mode_result else 'delete'
            synchronous_result = adapter.query("PRAGMA synchronous")
            synchronous = synchronous_result[0]['synchronous'] if synchronous_result else 1
            
            # Get table information
            tables = adapter.query("SELECT name FROM sqlite_master WHERE type='table'")
            table_count = len(tables)
            
            # Get index information
            indexes = adapter.query("SELECT name FROM sqlite_master WHERE type='index'")
            index_count = len(indexes)
            
            analysis_results.extend([
                ['Database Size', f"{page_count * page_size / 1024:.1f}KB", 'Total size'],
                ['Page Count', str(page_count), 'Number of pages'],
                ['Page Size', f"{page_size} bytes", 'Page size'],
                ['Cache Size', f"{cache_size} pages", 'Cache configuration'],
                ['Journal Mode', journal_mode, 'Journal mode'],
                ['Synchronous', str(synchronous), 'Synchronous mode'],
                ['Tables', str(table_count), 'Number of tables'],
                ['Indexes', str(index_count), 'Number of indexes']
            ])
            
            # Show table details
            if tables:
                formatter.subsection("Table Details")
                table_details = []
                for table in tables:
                    table_name = table['name']
                    try:
                        row_count = adapter.query(f"SELECT COUNT(*) as count FROM {table_name}")[0]['count']
                        table_details.append([table_name, str(row_count), 'rows'])
                    except Exception:
                        table_details.append([table_name, 'Error', 'Could not count rows'])
                
                formatter.table(
                    ['Table', 'Rows', 'Status'],
                    table_details,
                    'Table Statistics'
                )
            
        elif default_db == 'postgresql':
            # PostgreSQL analysis
            formatter.subsection("PostgreSQL Database Analysis")
            
            # Get database size
            size_result = adapter.query("SELECT pg_size_pretty(pg_database_size(current_database())) as db_size")
            db_size = size_result[0]['db_size'] if size_result else 'Unknown'
            
            # Get table statistics
            table_stats = adapter.query("""
                SELECT schemaname, tablename, n_tup_ins, n_tup_upd, n_tup_del, n_live_tup, n_dead_tup,
                       last_vacuum, last_autovacuum, last_analyze, last_autoanalyze
                FROM pg_stat_user_tables
                ORDER BY n_live_tup DESC
            """)
            
            # Get index statistics
            index_stats = adapter.query("""
                SELECT schemaname, tablename, indexname, idx_scan, idx_tup_read, idx_tup_fetch
                FROM pg_stat_user_indexes
                ORDER BY idx_scan DESC
            """)
            
            analysis_results.extend([
                ['Database Size', db_size, 'Total size'],
                ['Tables', str(len(table_stats)), 'User tables'],
                ['Indexes', str(len(index_stats)), 'User indexes'],
                ['Active Connections', 'See details below', 'Connection info']
            ])
            
            # Show top tables by size
            if table_stats:
                formatter.subsection("Top Tables by Row Count")
                top_tables = []
                for table in table_stats[:10]:
                    dead_tup_ratio = (table['n_dead_tup'] / (table['n_live_tup'] + table['n_dead_tup'])) * 100 if (table['n_live_tup'] + table['n_dead_tup']) > 0 else 0
                    top_tables.append([
                        f"{table['schemaname']}.{table['tablename']}",
                        str(table['n_live_tup']),
                        str(table['n_dead_tup']),
                        f"{dead_tup_ratio:.1f}%"
                    ])
                
                formatter.table(
                    ['Table', 'Live Tuples', 'Dead Tuples', 'Dead Ratio'],
                    top_tables,
                    'Table Statistics'
                )
            
            # Show index usage
            if index_stats:
                formatter.subsection("Index Usage Statistics")
                index_usage = []
                for index in index_stats[:10]:
                    index_usage.append([
                        f"{index['schemaname']}.{index['indexname']}",
                        str(index['idx_scan']),
                        str(index['idx_tup_read']),
                        str(index['idx_tup_fetch'])
                    ])
                
                formatter.table(
                    ['Index', 'Scans', 'Tuples Read', 'Tuples Fetched'],
                    index_usage,
                    'Index Usage'
                )
        
        # Display analysis results
        formatter.table(
            ['Metric', 'Value', 'Description'],
            analysis_results,
            'Database Analysis Results'
        )
        
        formatter.success("Database analysis completed successfully!")
        
        adapter.close()
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_db_connections(formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle db connections command for monitoring active connections"""
    formatter.loading("Monitoring database connections...")
    
    try:
        # Load configuration
        config = config_loader.load_config()
        db_config = config.get('database', {})
        default_db = db_config.get('default', 'sqlite')
        
        if default_db not in ['sqlite', 'postgresql']:
            formatter.error(f"Connection monitoring not supported for database type: {default_db}")
            return ErrorHandler.CONFIG_ERROR
        
        # Connect to database
        if default_db == 'sqlite':
            adapter = SQLiteAdapter({'database': './tusklang.db'})
        else:
            adapter = PostgreSQLAdapter(db_config.get('postgresql', {}))
        
        if not adapter.is_connected():
            adapter.connect()
        
        connection_results = []
        
        if default_db == 'sqlite':
            # SQLite connection info
            formatter.subsection("SQLite Connection Information")
            
            # Get current connection info
            busy_timeout = adapter.query("PRAGMA busy_timeout")[0]['busy_timeout']
            foreign_keys = adapter.query("PRAGMA foreign_keys")[0]['foreign_keys']
            journal_mode = adapter.query("PRAGMA journal_mode")[0]['journal_mode']
            
            connection_results.extend([
                ['Busy Timeout', f"{busy_timeout}ms", 'Connection timeout'],
                ['Foreign Keys', 'Enabled' if foreign_keys else 'Disabled', 'FK support'],
                ['Journal Mode', journal_mode, 'Journal mode'],
                ['Connection Type', 'File-based', 'SQLite database file'],
                ['Concurrent Access', 'Limited', 'SQLite has limited concurrency']
            ])
            
        elif default_db == 'postgresql':
            # PostgreSQL connection monitoring
            formatter.subsection("PostgreSQL Connection Monitoring")
            
            # Get active connections
            active_connections = adapter.query("""
                SELECT pid, usename, application_name, client_addr, state, 
                       query_start, state_change, wait_event_type, wait_event
                FROM pg_stat_activity 
                WHERE state IS NOT NULL
                ORDER BY query_start DESC
            """)
            
            # Get connection statistics
            conn_stats = adapter.query("""
                SELECT state, count(*) as count
                FROM pg_stat_activity 
                WHERE state IS NOT NULL
                GROUP BY state
            """)
            
            # Get max connections
            max_conn_result = adapter.query("SHOW max_connections")
            max_connections = max_conn_result[0]['max_connections'] if max_conn_result else 'Unknown'
            
            total_active = len(active_connections)
            
            connection_results.extend([
                ['Max Connections', max_connections, 'Maximum allowed connections'],
                ['Active Connections', str(total_active), 'Currently active connections'],
                ['Connection Usage', f"{(total_active/int(max_connections)*100):.1f}%" if max_connections != 'Unknown' else 'Unknown', 'Usage percentage']
            ])
            
            # Show connection states
            if conn_stats:
                formatter.subsection("Connection States")
                state_rows = []
                for stat in conn_stats:
                    state_rows.append([stat['state'], str(stat['count']), 'connections'])
                
                formatter.table(
                    ['State', 'Count', 'Description'],
                    state_rows,
                    'Connection States'
                )
            
            # Show active queries
            if active_connections:
                formatter.subsection("Active Queries (Last 10)")
                query_rows = []
                for conn in active_connections[:10]:
                    query_rows.append([
                        str(conn['pid']),
                        conn['usename'] or 'Unknown',
                        conn['state'],
                        conn['client_addr'] or 'Local',
                        conn['wait_event_type'] or 'None'
                    ])
                
                formatter.table(
                    ['PID', 'User', 'State', 'Client', 'Wait Event'],
                    query_rows,
                    'Active Connections'
                )
        
        # Display connection results
        formatter.table(
            ['Metric', 'Value', 'Description'],
            connection_results,
            'Connection Information'
        )
        
        formatter.success("Connection monitoring completed!")
        
        adapter.close()
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_db_console(formatter: OutputFormatter, config_loader: ConfigLoader) -> int:
    """Handle db console command"""
    formatter.info("Starting interactive database console...")
    formatter.info("Type 'exit' to quit, 'help' for commands")
    
    # Load configuration
    config = config_loader.load_config()
    db_config = config.get('database', {})
    default_db = db_config.get('default', 'sqlite')
    
    try:
        if default_db == 'sqlite':
            adapter = SQLiteAdapter({'database': './tusklang.db'})
        elif default_db == 'postgresql':
            adapter = PostgreSQLAdapter(db_config.get('postgresql', {}))
        elif default_db == 'mongodb':
            adapter = MongoDBAdapter(db_config.get('mongodb', {}))
        elif default_db == 'redis':
            adapter = RedisAdapter(db_config.get('redis', {}))
        else:
            formatter.error(f"Unsupported database type: {default_db}")
            return ErrorHandler.CONFIG_ERROR
        
        adapter.connect()
        
        # Simple console loop
        while True:
            try:
                query = input(f"{default_db}> ").strip()
                
                if query.lower() in ['exit', 'quit']:
                    break
                elif query.lower() == 'help':
                    print("Available commands: SELECT, INSERT, UPDATE, DELETE, exit, help")
                    continue
                elif not query:
                    continue
                
                # Execute query
                if default_db in ['sqlite', 'postgresql']:
                    result = adapter.query(query)
                    for row in result:
                        print(row)
                elif default_db == 'mongodb':
                    # Simple MongoDB query parsing
                    if query.startswith('find'):
                        collection = query.split()[1] if len(query.split()) > 1 else 'test'
                        result = adapter.query(f"{collection}.find", {})
                        for doc in result:
                            print(doc)
                    else:
                        print("MongoDB commands: find <collection>, exit, help")
                elif default_db == 'redis':
                    # Simple Redis command parsing
                    parts = query.split()
                    if parts[0].upper() in ['GET', 'SET', 'DEL', 'KEYS']:
                        result = adapter.query(parts[0].upper(), *parts[1:])
                        print(result)
                    else:
                        print("Redis commands: GET <key>, SET <key> <value>, DEL <key>, KEYS <pattern>")
                
            except KeyboardInterrupt:
                print("\nExiting...")
                break
            except Exception as e:
                print(f"Error: {e}")
        
        adapter.close()
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        formatter.error(f"Failed to start database console: {e}")
        return ErrorHandler.CONNECTION_ERROR


def _handle_db_backup(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle db backup command"""
    # Generate backup filename if not provided
    if args.file:
        backup_file = Path(args.file)
    else:
        timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        backup_file = Path(f"tusklang_backup_{timestamp}.sql")
    
    formatter.loading(f"Creating database backup: {backup_file}")
    
    try:
        # Load configuration
        config = config_loader.load_config()
        db_config = config.get('database', {})
        default_db = db_config.get('default', 'sqlite')
        
        if default_db == 'sqlite':
            # SQLite backup
            source_db = db_config.get('sqlite', {}).get('database', './tusklang.db')
            
            if not Path(source_db).exists():
                formatter.warning(f"Source database not found: {source_db}")
                return ErrorHandler.FILE_NOT_FOUND
            
            # Use SQLite backup API
            source_conn = sqlite3.connect(source_db)
            backup_conn = sqlite3.connect(str(backup_file))
            
            source_conn.backup(backup_conn)
            source_conn.close()
            backup_conn.close()
            
        elif default_db == 'postgresql':
            # PostgreSQL backup using pg_dump
            pg_config = db_config.get('postgresql', {})
            host = pg_config.get('host', 'localhost')
            port = pg_config.get('port', 5432)
            database = pg_config.get('database', 'tusklang')
            user = pg_config.get('user', 'postgres')
            
            cmd = [
                'pg_dump',
                f'--host={host}',
                f'--port={port}',
                f'--dbname={database}',
                f'--username={user}',
                '--no-password',
                f'--file={backup_file}'
            ]
            
            result = subprocess.run(cmd, capture_output=True, text=True)
            if result.returncode != 0:
                raise Exception(f"pg_dump failed: {result.stderr}")
        
        else:
            formatter.error(f"Backup not supported for database type: {default_db}")
            return ErrorHandler.CONFIG_ERROR
        
        formatter.success(f"Database backup created successfully: {backup_file}")
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_db_restore(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle db restore command"""
    backup_file = Path(args.file)
    
    if not backup_file.exists():
        return error_handler.handle_file_not_found(str(backup_file))
    
    formatter.loading(f"Restoring database from backup: {backup_file}")
    
    try:
        # Determine backup type and restore
        if backup_file.suffix == '.sql':
            # SQL backup file
            with open(backup_file, 'r') as f:
                restore_sql = f.read()
            
            adapter = SQLiteAdapter({'database': './tusklang.db'})
            adapter.connect()
            
            # Split and execute SQL statements
            statements = [stmt.strip() for stmt in restore_sql.split(';') if stmt.strip()]
            
            for statement in statements:
                adapter.query(statement)
            
            adapter.close()
            
        elif backup_file.suffix == '.db':
            # SQLite database file
            import shutil
            shutil.copy2(backup_file, './tusklang.db')
            
        else:
            formatter.error(f"Unsupported backup file format: {backup_file.suffix}")
            return ErrorHandler.INVALID_ARGS
        
        formatter.success(f"Database restored successfully from: {backup_file}")
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_db_init(formatter: OutputFormatter, config_loader: ConfigLoader) -> int:
    """Handle db init command"""
    formatter.loading("Initializing SQLite database...")
    
    try:
        # Create SQLite database with basic schema
        adapter = SQLiteAdapter({'database': './tusklang.db'})
        adapter.connect()
        
        # Create basic tables
        init_sql = """
        CREATE TABLE IF NOT EXISTS config (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            key TEXT UNIQUE NOT NULL,
            value TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );
        
        CREATE TABLE IF NOT EXISTS migrations (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT UNIQUE NOT NULL,
            applied_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );
        
        CREATE TABLE IF NOT EXISTS cache (
            key TEXT PRIMARY KEY,
            value TEXT,
            expires_at DATETIME,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );
        """
        
        statements = [stmt.strip() for stmt in init_sql.split(';') if stmt.strip()]
        
        for statement in statements:
            adapter.query(statement)
        
        adapter.close()
        
        formatter.success("SQLite database initialized successfully")
        formatter.info("Created tables: config, migrations, cache")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        formatter.error(f"Failed to initialize database: {e}")
        return ErrorHandler.CONNECTION_ERROR


def _handle_db_migrate(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle db migrate command"""
    migration_file = Path(args.file)
    
    if not migration_file.exists():
        return error_handler.handle_file_not_found(str(migration_file))
    
    formatter.loading(f"Running migration: {migration_file}")
    
    try:
        # Read migration file
        with open(migration_file, 'r') as f:
            migration_sql = f.read()
        
        # Execute migration (using SQLite as default)
        adapter = SQLiteAdapter({'database': './tusklang.db'})
        adapter.connect()
        
        # Split SQL statements
        statements = [stmt.strip() for stmt in migration_sql.split(';') if stmt.strip()]
        
        for statement in statements:
            adapter.query(statement)
        
        formatter.success(f"Migration completed successfully: {len(statements)} statements executed")
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e) 