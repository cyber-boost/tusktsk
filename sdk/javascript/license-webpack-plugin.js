/**
 * TuskLang License Webpack Plugin
 * Validates license during build process
 */

class LicenseWebpackPlugin {
  constructor(options = {}) {
    this.licenseKey = options.licenseKey;
    this.apiEndpoint = options.apiEndpoint || 'https://lic.tusklang.org/api/v1';
  }
  
  apply(compiler) {
    compiler.hooks.beforeCompile.tapAsync('LicenseWebpackPlugin', async (params, callback) => {
      try {
        if (!this.licenseKey) {
          console.warn('⚠️  No license key provided for TuskLang SDK build');
          callback();
          return;
        }
        
        // Validate license before compilation
        const isValid = await this.validateLicense(this.licenseKey);
        
        if (!isValid) {
          throw new Error('TuskLang SDK license validation failed');
        }
        
        console.log('✅ TuskLang SDK license validated');
        callback();
        
      } catch (error) {
        console.error('❌ TuskLang SDK license validation failed:', error.message);
        callback(error);
      }
    });
    
    compiler.hooks.afterEmit.tap('LicenseWebpackPlugin', (compilation) => {
      console.log('🔒 TuskLang SDK build completed with protection');
    });
  }
  
  async validateLicense(licenseKey) {
    try {
      const response = await fetch(this.apiEndpoint + '/validate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'User-Agent': 'TuskLang-Webpack-Plugin/1.0.0'
        },
        body: JSON.stringify({
          license_key: licenseKey,
          build_type: 'webpack',
          timestamp: Date.now()
        })
      });
      
      if (!response.ok) {
        return false;
      }
      
      const result = await response.json();
      return result.valid || false;
      
    } catch (error) {
      console.warn('License validation failed, continuing with build:', error.message);
      return true; // Allow build to continue
    }
  }
}

module.exports = LicenseWebpackPlugin; 