<?php
// CSS command handler for TuskLang CLI (PHP)

function handleCssCommand($subcommand, $args) {
    global $json, $verbose, $quiet;
    $exitCode = 0;
    try {
        switch ($subcommand) {
            case 'expand':
                $input = $args[0] ?? null;
                $output = $args[1] ?? null;
                if (!$input || !file_exists($input)) {
                    echo status('error', 'Input file not found') . "\n";
                    exit(3);
                }
                $expanded = expandCssShortcodes($input);
                if ($output) {
                    file_put_contents($output, $expanded);
                    if ($json) jsonOutput(['expanded' => $output]);
                    echo status('success', "CSS expanded to: $output") . "\n";
                } else {
                    if ($json) jsonOutput(['css' => $expanded]);
                    echo $expanded . "\n";
                }
                break;
            case 'map':
                $mappings = getCssMappings();
                if ($json) jsonOutput($mappings);
                echo status('success', 'CSS shortcode mappings') . "\n";
                foreach ($mappings as $shortcode => $property) {
                    echo status('info', "$shortcode → $property") . "\n";
                }
                break;
            default:
                echo status('info', 'Available CSS commands: expand <input> [output], map') . "\n";
                $exitCode = 2;
        }
    } catch (Exception $e) {
        $exitCode = 1;
        if ($json) jsonOutput(['error' => $e->getMessage()], false);
        echo status('error', $e->getMessage()) . "\n";
    }
    exit($exitCode);
}

function expandCssShortcodes($file) {
    $content = file_get_contents($file);
    $mappings = getCssMappings();
    foreach ($mappings as $shortcode => $property) {
        $content = str_replace($shortcode, $property, $content);
    }
    return $content;
}

function getCssMappings() {
    return [
        'mh' => 'max-height',
        'mw' => 'max-width',
        'mh=100px' => 'max-height: 100px',
        'mw=50%' => 'max-width: 50%',
        'p=10px' => 'padding: 10px',
        'm=20px' => 'margin: 20px',
        'bg=red' => 'background: red',
        'c=blue' => 'color: blue'
    ];
} 