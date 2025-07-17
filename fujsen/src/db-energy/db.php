<?php
require_once __DIR__ . '/../../config/config.php';

try {
    $dsn = 'pgsql:host=127.0.0.1;port=6432;dbname=v2_test;';
    $pdo = new PDO($dsn, DB_USERNAME, DB_PASSWORD, [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION
    ]);
} catch (PDOException $e) {
    error_log('Database connection failed: ' . $e->getMessage());
    if (defined('DEBUG') && DEBUG) {
        die('Database connection failed. Check error log for details.');
    } else {
        die('Database connection failed.');
    }
}

// Session hijack protection: check session_version
if (!empty($_SESSION['user'])) {
    $stmt = $pdo->prepare('SELECT session_version FROM users WHERE email = ?');
    $stmt->execute([$_SESSION['user']]);
    $row = $stmt->fetch(PDO::FETCH_ASSOC);
    $db_version = $row['session_version'] ?? 0;
    $sess_version = $_SESSION['session_version'] ?? 0;
    if ($db_version > $sess_version) {
        session_unset();
        session_destroy();
        header('Location: /login/');
        exit;
    }
}
?>