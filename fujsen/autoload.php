<?php
/**
 * 🚀 FUJSEN Autoloader
 * ===================
 * Auto-loads all FUJSEN components
 */

// Define the base directory
define('FUJSEN_BASE_DIR', __DIR__);

// Auto-load all PHP files in src directory
foreach (glob(FUJSEN_BASE_DIR . '/src/*.php') as $file) {
    require_once $file;
}

// Auto-load db-energy components from src directory
// $dbEnergyDir = FUJSEN_BASE_DIR . '/src/db-energy';
// if (is_dir($dbEnergyDir)) {
//     foreach (glob($dbEnergyDir . '/*.php') as $file) {
//         require_once $file;
//     }
// }

// Set up namespace aliases for convenience
if (!class_exists('TuskLang')) {
    class_alias('\TuskPHP\Utils\TuskLang', 'TuskLang');
}

if (!class_exists('TuskLangWebHandler')) {
    class_alias('\TuskPHP\Utils\TuskLangWebHandler', 'TuskLangWebHandler');
}

if (!class_exists('TuskLangWebParser')) {
    class_alias('\TuskPHP\Utils\TuskLangWebParser', 'TuskLangWebParser');
}

if (!class_exists('TuskLangQueryBridge')) {
    class_alias('\TuskPHP\Utils\TuskLangQueryBridge', 'TuskLangQueryBridge');
}

// echo "🚀 FUJSEN Autoloader: All components loaded successfully!\n"; 