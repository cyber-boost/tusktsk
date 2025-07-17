<?php
/**
 * 🐘 TuskLang Binary Obfuscator
 * =============================
 * Advanced binary protection and machine code obfuscation
 * Protects compiled binaries from reverse engineering
 */

namespace TuskPHP\License;

class BinaryObfuscator
{
    private static $obfuscationLevel = 'maximum';
    private static $protectedBinaries = [];
    private static $tools = [
        'upx' => 'UPX - Ultimate Packer for eXecutables',
        'strip' => 'GNU Strip - Symbol removal',
        'objcopy' => 'GNU Objcopy - Binary manipulation',
        'patchelf' => 'PatchELF - ELF binary patcher'
    ];
    
    /**
     * Initialize binary obfuscator
     */
    public static function init(string $level = 'maximum'): void
    {
        self::$obfuscationLevel = $level;
    }
    
    /**
     * Obfuscate binary file
     */
    public static function obfuscateBinary(string $binaryPath, string $outputPath = null): bool
    {
        if (!file_exists($binaryPath)) {
            return false;
        }
        
        if ($outputPath === null) {
            $outputPath = $binaryPath . '.obfuscated';
        }
        
        // Copy original binary
        copy($binaryPath, $outputPath);
        
        // Apply obfuscation techniques
        $success = true;
        
        // Strip debug symbols
        if (self::hasTool('strip')) {
            $success &= self::stripSymbols($outputPath);
        }
        
        // Apply UPX compression
        if (self::hasTool('upx')) {
            $success &= self::compressBinary($outputPath);
        }
        
        // Apply additional obfuscation
        if (self::$obfuscationLevel === 'maximum') {
            $success &= self::applyAdvancedObfuscation($outputPath);
        }
        
        if ($success) {
            chmod($outputPath, 0755);
            self::$protectedBinaries[$binaryPath] = $outputPath;
        }
        
        return $success;
    }
    
    /**
     * Strip debug symbols from binary
     */
    private static function stripSymbols(string $binaryPath): bool
    {
        $command = "strip --strip-all '$binaryPath' 2>/dev/null";
        exec($command, $output, $returnCode);
        
        return $returnCode === 0;
    }
    
    /**
     * Compress binary with UPX
     */
    private static function compressBinary(string $binaryPath): bool
    {
        $command = "upx --best --ultra-brute '$binaryPath' 2>/dev/null";
        exec($command, $output, $returnCode);
        
        return $returnCode === 0;
    }
    
    /**
     * Apply advanced obfuscation techniques
     */
    private static function applyAdvancedObfuscation(string $binaryPath): bool
    {
        $success = true;
        
        // Check if it's an ELF binary
        if (self::isElfBinary($binaryPath)) {
            $success &= self::obfuscateElfBinary($binaryPath);
        }
        
        // Check if it's a PE binary (Windows)
        if (self::isPeBinary($binaryPath)) {
            $success &= self::obfuscatePeBinary($binaryPath);
        }
        
        // Check if it's a Mach-O binary (macOS)
        if (self::isMachOBinary($binaryPath)) {
            $success &= self::obfuscateMachOBinary($binaryPath);
        }
        
        return $success;
    }
    
    /**
     * Obfuscate ELF binary
     */
    private static function obfuscateElfBinary(string $binaryPath): bool
    {
        $success = true;
        
        // Remove section headers
        if (self::hasTool('objcopy')) {
            $command = "objcopy --strip-all '$binaryPath' 2>/dev/null";
            exec($command, $output, $returnCode);
            $success &= ($returnCode === 0);
        }
        
        // Modify ELF headers
        if (self::hasTool('patchelf')) {
            // Change section names
            $command = "patchelf --set-section-flags .text=alloc,exec '$binaryPath' 2>/dev/null";
            exec($command, $output, $returnCode);
            $success &= ($returnCode === 0);
        }
        
        return $success;
    }
    
    /**
     * Obfuscate PE binary (Windows)
     */
    private static function obfuscatePeBinary(string $binaryPath): bool
    {
        // For Windows PE binaries, we would use tools like:
        // - pefile (Python library)
        // - PE Explorer
        // - Resource Hacker
        
        // For now, just apply basic protection
        return self::applyBasicProtection($binaryPath);
    }
    
    /**
     * Obfuscate Mach-O binary (macOS)
     */
    private static function obfuscateMachOBinary(string $binaryPath): bool
    {
        // For macOS Mach-O binaries, we would use tools like:
        // - install_name_tool
        // - codesign
        // - otool
        
        // For now, just apply basic protection
        return self::applyBasicProtection($binaryPath);
    }
    
    /**
     * Apply basic protection to any binary
     */
    private static function applyBasicProtection(string $binaryPath): bool
    {
        // Add custom header
        $customHeader = self::generateCustomHeader();
        $binaryContent = file_get_contents($binaryPath);
        
        $protectedContent = $customHeader . $binaryContent;
        
        return file_put_contents($binaryPath, $protectedContent) !== false;
    }
    
    /**
     * Generate custom binary header
     */
    private static function generateCustomHeader(): string
    {
        $header = [
            'magic' => 'TUSKLANG',
            'version' => '1.0',
            'protected' => true,
            'timestamp' => time(),
            'checksum' => hash('sha256', uniqid())
        ];
        
        return json_encode($header) . "\n";
    }
    
    /**
     * Check if binary is ELF format
     */
    private static function isElfBinary(string $binaryPath): bool
    {
        $content = file_get_contents($binaryPath, false, null, 0, 4);
        return $content === "\x7fELF";
    }
    
    /**
     * Check if binary is PE format (Windows)
     */
    private static function isPeBinary(string $binaryPath): bool
    {
        $content = file_get_contents($binaryPath, false, null, 0, 2);
        return $content === "MZ";
    }
    
    /**
     * Check if binary is Mach-O format (macOS)
     */
    private static function isMachOBinary(string $binaryPath): bool
    {
        $content = file_get_contents($binaryPath, false, null, 0, 4);
        $magic = unpack('N', $content)[1];
        return in_array($magic, [0xfeedface, 0xcefaedfe, 0xfeedfacf, 0xcffaedfe]);
    }
    
    /**
     * Check if tool is available
     */
    private static function hasTool(string $tool): bool
    {
        $command = "which $tool 2>/dev/null";
        exec($command, $output, $returnCode);
        return $returnCode === 0;
    }
    
    /**
     * Obfuscate multiple binaries
     */
    public static function obfuscateBinaries(array $binaryPaths, string $outputDir = null): array
    {
        $results = [
            'obfuscated' => 0,
            'failed' => 0,
            'files' => []
        ];
        
        foreach ($binaryPaths as $binaryPath) {
            if ($outputDir) {
                $outputPath = $outputDir . '/' . basename($binaryPath) . '.obfuscated';
            } else {
                $outputPath = null;
            }
            
            if (self::obfuscateBinary($binaryPath, $outputPath)) {
                $results['obfuscated']++;
                $results['files'][] = [
                    'original' => $binaryPath,
                    'obfuscated' => $outputPath ?: $binaryPath . '.obfuscated'
                ];
            } else {
                $results['failed']++;
            }
        }
        
        return $results;
    }
    
    /**
     * Add license validation to binary
     */
    public static function addLicenseValidation(string $binaryPath, string $licenseKey): bool
    {
        if (!file_exists($binaryPath)) {
            return false;
        }
        
        // Create license validation stub
        $validationStub = self::generateValidationStub($licenseKey);
        
        // Append to binary
        $binaryContent = file_get_contents($binaryPath);
        $protectedContent = $binaryContent . $validationStub;
        
        return file_put_contents($binaryPath, $protectedContent) !== false;
    }
    
    /**
     * Generate license validation stub
     */
    private static function generateValidationStub(string $licenseKey): string
    {
        $stub = [
            'type' => 'license_validation',
            'license_key' => $licenseKey,
            'timestamp' => time(),
            'checksum' => hash('sha256', $licenseKey . time())
        ];
        
        return "\n" . json_encode($stub);
    }
    
    /**
     * Get obfuscation statistics
     */
    public static function getStats(): array
    {
        return [
            'obfuscation_level' => self::$obfuscationLevel,
            'protected_binaries' => count(self::$protectedBinaries),
            'available_tools' => array_filter(self::$tools, [self::class, 'hasTool'])
        ];
    }
    
    /**
     * Clean up obfuscated binaries
     */
    public static function cleanup(): int
    {
        $cleaned = 0;
        
        foreach (self::$protectedBinaries as $original => $obfuscated) {
            if (file_exists($obfuscated)) {
                unlink($obfuscated);
                $cleaned++;
            }
        }
        
        self::$protectedBinaries = [];
        
        return $cleaned;
    }
} 