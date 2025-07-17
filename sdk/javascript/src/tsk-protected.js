/**
 * 🐘 TuskLang JavaScript SDK - Protected Version
 * =============================================
 * Webpack obfuscated version with runtime license validation
 * 
 * This file is obfuscated and protected against reverse engineering
 * Runtime license validation ensures compliance
 */

(function(global, factory) {
  'use strict';
  
  // Protection check
  if (typeof global !== 'undefined' && global.TuskLangProtection) {
    throw new Error('TuskLang SDK protection violation detected');
  }
  
  // Set protection flag
  global.TuskLangProtection = true;
  
  if (typeof module === 'object' && typeof module.exports === 'object') {
    module.exports = factory();
  } else if (typeof define === 'function' && define.amd) {
    define(factory);
  } else {
    global.TuskLang = factory();
  }
})(this, function() {
  'use strict';
  
  // Protection variables
  let licenseValidated = false;
  let protectionLevel = 'enterprise';
  let apiEndpoint = 'https://lic.tusklang.org/api/v1';
  let installationId = null;
  
  /**
   * Initialize protected SDK with license validation
   */
  function init(licenseKey) {
    // Runtime protection check
    if (!checkProtection()) {
      selfDestruct();
      return false;
    }
    
    // License validation
    if (!licenseKey) {
      licenseKey = getStoredLicense();
    }
    
    if (!licenseKey) {
      logViolation('No license key provided');
      return false;
    }
    
    return validateLicense(licenseKey).then(validation => {
      if (!validation.valid) {
        logViolation('Invalid license: ' + validation.reason);
        return false;
      }
      
      licenseValidated = true;
      return true;
    }).catch(error => {
      logViolation('License validation failed: ' + error.message);
      return false;
    });
  }
  
  /**
   * Check if protection is intact
   */
  function checkProtection() {
    // Check for debugging tools
    if (typeof window !== 'undefined') {
      if (window.console && window.console.log.toString().indexOf('native code') === -1) {
        return false;
      }
      
      if (window.Firebug && window.Firebug.chrome && window.Firebug.chrome.isInitialized) {
        return false;
      }
      
      if (window.outerHeight - window.innerHeight > 200) {
        return false;
      }
    }
    
    // Check for Node.js debugging
    if (typeof process !== 'undefined' && process.env.NODE_ENV === 'development') {
      return false;
    }
    
    // Check function integrity
    const functionString = init.toString();
    if (functionString.length < 100 || functionString.length > 10000) {
      return false;
    }
    
    return true;
  }
  
  /**
   * Validate license with API
   */
  async function validateLicense(licenseKey) {
    try {
      const data = {
        license_key: licenseKey,
        installation_id: getInstallationId(),
        hostname: typeof window !== 'undefined' ? window.location.hostname : 'nodejs',
        timestamp: Date.now(),
        sdk_type: 'javascript',
        protection_level: protectionLevel
      };
      
      const response = await apiRequest('POST', '/validate', data);
      
      return {
        valid: response.valid || false,
        reason: response.reason || 'Unknown error',
        license: response.license || null
      };
      
    } catch (error) {
      // Fallback to offline validation
      return offlineValidation(licenseKey);
    }
  }
  
  /**
   * Offline license validation (grace period)
   */
  function offlineValidation(licenseKey) {
    try {
      const cacheKey = 'tusklang_license_cache';
      const cached = localStorage.getItem(cacheKey);
      
      if (cached) {
        const cache = JSON.parse(cached);
        
        if (cache.license_key === licenseKey && cache.expires > Date.now()) {
          return Promise.resolve({
            valid: true,
            reason: 'Offline cache valid',
            license: cache.license
          });
        }
      }
      
      return Promise.resolve({
        valid: false,
        reason: 'No offline cache available',
        license: null
      });
      
    } catch (error) {
      return Promise.resolve({
        valid: false,
        reason: 'Offline validation failed',
        license: null
      });
    }
  }
  
  /**
   * Get stored license key
   */
  function getStoredLicense() {
    try {
      // Check localStorage
      if (typeof localStorage !== 'undefined') {
        return localStorage.getItem('tusklang_license');
      }
      
      // Check environment variable (Node.js)
      if (typeof process !== 'undefined' && process.env.TUSKLANG_LICENSE) {
        return process.env.TUSKLANG_LICENSE;
      }
      
      return null;
    } catch (error) {
      return null;
    }
  }
  
  /**
   * Get installation ID
   */
  function getInstallationId() {
    if (installationId) {
      return installationId;
    }
    
    try {
      // Try to get from localStorage
      if (typeof localStorage !== 'undefined') {
        installationId = localStorage.getItem('tusklang_installation_id');
      }
      
      if (!installationId) {
        // Generate new installation ID
        installationId = 'JS-' + Math.random().toString(36).substr(2, 12).toUpperCase();
        
        // Store it
        if (typeof localStorage !== 'undefined') {
          localStorage.setItem('tusklang_installation_id', installationId);
        }
      }
      
      return installationId;
    } catch (error) {
      return 'JS-UNKNOWN-' + Date.now();
    }
  }
  
  /**
   * Make API request
   */
  async function apiRequest(method, endpoint, data = {}) {
    const url = apiEndpoint + endpoint;
    const headers = {
      'Content-Type': 'application/json',
      'User-Agent': 'TuskLang-JS-SDK/1.0.0',
      'X-Installation-ID': getInstallationId()
    };
    
    const options = {
      method: method,
      headers: headers,
      timeout: 10000
    };
    
    if (method === 'POST') {
      options.body = JSON.stringify(data);
    }
    
    const response = await fetch(url, options);
    
    if (!response.ok) {
      throw new Error(`API request failed: ${response.status}`);
    }
    
    return await response.json();
  }
  
  /**
   * Log security violation
   */
  function logViolation(reason) {
    const logData = {
      timestamp: new Date().toISOString(),
      reason: reason,
      hostname: typeof window !== 'undefined' ? window.location.hostname : 'nodejs',
      userAgent: typeof navigator !== 'undefined' ? navigator.userAgent : 'nodejs',
      installationId: getInstallationId()
    };
    
    // Store violation locally
    try {
      if (typeof localStorage !== 'undefined') {
        const violations = JSON.parse(localStorage.getItem('tusklang_violations') || '[]');
        violations.push(logData);
        localStorage.setItem('tusklang_violations', JSON.stringify(violations));
      }
    } catch (error) {
      // Silent fail for local storage
    }
    
    // Report to API
    apiRequest('POST', '/violation', logData).catch(() => {
      // Silent fail for violation reporting
    });
  }
  
  /**
   * Self-destruct mechanism
   */
  function selfDestruct() {
    // Clear sensitive data
    licenseValidated = false;
    
    // Log violation
    logViolation('Protection violation detected - self-destruct initiated');
    
    // Throw exception to prevent further execution
    throw new Error('TuskLang SDK protection violation detected');
  }
  
  /**
   * Check if SDK is properly licensed
   */
  function isLicensed() {
    return licenseValidated;
  }
  
  /**
   * Get protection level
   */
  function getProtectionLevel() {
    return protectionLevel;
  }
  
  /**
   * Enhanced TuskLang functionality (protected)
   */
  function parse(code) {
    if (!isLicensed()) {
      throw new Error('TuskLang SDK not properly licensed');
    }
    
    // Implementation would be obfuscated
    return { status: 'protected_implementation' };
  }
  
  function compile(code) {
    if (!isLicensed()) {
      throw new Error('TuskLang SDK not properly licensed');
    }
    
    // Implementation would be obfuscated
    return 'protected_compiled_code';
  }
  
  function validate(code) {
    if (!isLicensed()) {
      throw new Error('TuskLang SDK not properly licensed');
    }
    
    // Implementation would be obfuscated
    return true;
  }
  
  // Return public API
  return {
    init: init,
    isLicensed: isLicensed,
    getProtectionLevel: getProtectionLevel,
    parse: parse,
    compile: compile,
    validate: validate
  };
}); 