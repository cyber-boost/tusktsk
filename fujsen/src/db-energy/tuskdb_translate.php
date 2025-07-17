<?php
/**
 * TuskPHP Database Schema Translator
 * Converts PostgreSQL schemas to SQLite-compatible format
 */

function translateToSQLite($pgSQL) {
    // Replace SERIAL with INTEGER PRIMARY KEY AUTOINCREMENT
    $sql = preg_replace('/\bSERIAL\b/i', 'INTEGER PRIMARY KEY AUTOINCREMENT', $pgSQL);
    
    // Replace BIGSERIAL
    $sql = preg_replace('/\bBIGSERIAL\b/i', 'INTEGER PRIMARY KEY AUTOINCREMENT', $sql);
    
    // Remove or replace PostgreSQL-specific types
    $sql = preg_replace('/\bUUID\b/i', 'VARCHAR(36)', $sql);
    $sql = preg_replace('/\bJSONB?\b/i', 'TEXT', $sql);
    $sql = preg_replace('/\bTIMESTAMPTZ\b/i', 'DATETIME', $sql);
    $sql = preg_replace('/\bTIMESTAMP\s+WITH\s+TIME\s+ZONE\b/i', 'DATETIME', $sql);
    
    // Replace BOOLEAN with INTEGER
    $sql = preg_replace('/\bBOOLEAN\b/i', 'INTEGER', $sql);
    
    // Remove ON UPDATE CASCADE (SQLite doesn't support it well)
    $sql = preg_replace('/\bON\s+UPDATE\s+CASCADE\b/i', '', $sql);
    
    // Replace NOW() with CURRENT_TIMESTAMP
    $sql = preg_replace('/\bNOW\(\)/i', 'CURRENT_TIMESTAMP', $sql);
    
    // Remove any PostgreSQL-specific commands
    $sql = preg_replace('/^\s*\\\\[^;]+;/m', '', $sql);
    
    return $sql;
}

// Process all .sql files in the directory
$files = glob(__DIR__ . '/*.sql');
foreach ($files as $file) {
    // Skip if already a SQLite file
    if (strpos($file, '_sqlite.sql') !== false) {
        continue;
    }
    
    $content = file_get_contents($file);
    $sqliteContent = translateToSQLite($content);
    
    $sqliteFile = str_replace('.sql', '_sqlite.sql', $file);
    file_put_contents($sqliteFile, $sqliteContent);
    echo "Created SQLite version: " . basename($sqliteFile) . "\n";
}
