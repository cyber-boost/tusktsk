#!/usr/bin/env node
/**
 * License Commands for TuskLang CLI
 * =================================
 * License validation and management commands
 * 
 * Commands:
 * - check: Check current license status
 * - activate: Activate license with key
 * - validate: Validate license with server
 * - info: Display license information
 * - transfer: Transfer license to another user
 * - revoke: Revoke license
 */

const { Command } = require('commander');
const fs = require('fs').promises;
const path = require('path');
const crypto = require('crypto');
const https = require('https');
const os = require('os');

/**
 * License Manager Class
 */
class LicenseManager {
  constructor() {
    this.licenseFile = path.join(os.homedir(), '.tusktsk', 'license.json');
    this.serverUrl = 'https://license.tuskt.sk';
    this.license = null;
  }

  /**
   * Initialize license manager
   */
  async init() {
    try {
      // Ensure license directory exists
      const licenseDir = path.dirname(this.licenseFile);
      await fs.mkdir(licenseDir, { recursive: true });
      
      // Load existing license if available
      await this.loadLicense();
    } catch (error) {
      // License file doesn't exist, that's okay
    }
  }

  /**
   * Load license from file
   */
  async loadLicense() {
    try {
      const data = await fs.readFile(this.licenseFile, 'utf8');
      this.license = JSON.parse(data);
      return this.license;
    } catch (error) {
      this.license = null;
      return null;
    }
  }

  /**
   * Save license to file
   */
  async saveLicense(licenseData) {
    this.license = licenseData;
    await fs.writeFile(this.licenseFile, JSON.stringify(licenseData, null, 2), 'utf8');
  }

  /**
   * Generate machine fingerprint
   */
  generateMachineFingerprint() {
    const platform = os.platform();
    const arch = os.arch();
    const hostname = os.hostname();
    const cpus = os.cpus().length;
    const totalMem = os.totalmem();
    
    const fingerprint = crypto.createHash('sha256')
      .update(`${platform}-${arch}-${hostname}-${cpus}-${totalMem}`)
      .digest('hex');
    
    return fingerprint;
  }

  /**
   * Make HTTPS request to license server
   */
  async makeLicenseRequest(endpoint, data = {}) {
    return new Promise((resolve, reject) => {
      const postData = JSON.stringify({
        ...data,
        machineId: this.generateMachineFingerprint(),
        timestamp: Date.now()
      });

      const options = {
        hostname: 'license.tuskt.sk',
        port: 443,
        path: endpoint,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(postData),
          'User-Agent': 'TuskTsk-CLI/2.0.0'
        }
      };

      const req = https.request(options, (res) => {
        let responseData = '';
        
        res.on('data', (chunk) => {
          responseData += chunk;
        });
        
        res.on('end', () => {
          try {
            const response = JSON.parse(responseData);
            resolve(response);
          } catch (error) {
            reject(new Error('Invalid server response'));
          }
        });
      });

      req.on('error', (error) => {
        reject(new Error(`Network error: ${error.message}`));
      });

      req.write(postData);
      req.end();
    });
  }

  /**
   * Check license status
   */
  async checkStatus() {
    await this.init();
    
    if (!this.license) {
      return {
        valid: false,
        status: 'NO_LICENSE',
        message: 'No license found. Use "tsk license activate <key>" to activate a license.',
        expires: null,
        features: []
      };
    }

    const now = Date.now();
    const expiresAt = this.license.expiresAt;
    
    if (expiresAt && now > expiresAt) {
      return {
        valid: false,
        status: 'EXPIRED',
        message: 'License has expired.',
        expires: new Date(expiresAt),
        features: this.license.features || []
      };
    }

    return {
      valid: true,
      status: 'ACTIVE',
      message: 'License is active and valid.',
      expires: expiresAt ? new Date(expiresAt) : null,
      features: this.license.features || [],
      license: this.license
    };
  }

  /**
   * Activate license
   */
  async activate(key) {
    if (!key) {
      throw new Error('License key is required');
    }

    try {
      const response = await this.makeLicenseRequest('/activate', {
        licenseKey: key
      });

      if (response.success) {
        const licenseData = {
          key: key,
          activatedAt: Date.now(),
          expiresAt: response.expiresAt,
          features: response.features || [],
          user: response.user || 'Unknown',
          type: response.type || 'standard',
          serverValidated: true
        };

        await this.saveLicense(licenseData);
        
        return {
          success: true,
          message: 'License activated successfully',
          license: licenseData
        };
      } else {
        throw new Error(response.error || 'License activation failed');
      }
    } catch (error) {
      throw new Error(`Activation failed: ${error.message}`);
    }
  }

  /**
   * Validate license with server
   */
  async validate(key = null) {
    const licenseKey = key || (this.license ? this.license.key : null);
    
    if (!licenseKey) {
      throw new Error('No license key provided or found');
    }

    try {
      const response = await this.makeLicenseRequest('/validate', {
        licenseKey: licenseKey
      });

      if (response.success) {
        // Update local license with server data
        if (this.license) {
          this.license.serverValidated = true;
          this.license.lastValidated = Date.now();
          this.license.features = response.features || this.license.features;
          await this.saveLicense(this.license);
        }

        return {
          success: true,
          message: 'License validated successfully',
          features: response.features || [],
          user: response.user || 'Unknown',
          expiresAt: response.expiresAt
        };
      } else {
        throw new Error(response.error || 'License validation failed');
      }
    } catch (error) {
      throw new Error(`Validation failed: ${error.message}`);
    }
  }

  /**
   * Get license information
   */
  async getInfo() {
    await this.init();
    
    if (!this.license) {
      return {
        hasLicense: false,
        message: 'No license found'
      };
    }

    const status = await this.checkStatus();
    
    return {
      hasLicense: true,
      status: status.status,
      valid: status.valid,
      key: this.license.key ? `${this.license.key.substring(0, 8)}...` : 'Unknown',
      activatedAt: this.license.activatedAt ? new Date(this.license.activatedAt) : null,
      expiresAt: this.license.expiresAt ? new Date(this.license.expiresAt) : null,
      features: this.license.features || [],
      user: this.license.user || 'Unknown',
      type: this.license.type || 'standard',
      serverValidated: this.license.serverValidated || false,
      lastValidated: this.license.lastValidated ? new Date(this.license.lastValidated) : null,
      machineId: this.generateMachineFingerprint()
    };
  }

  /**
   * Transfer license
   */
  async transfer(newUser, newEmail) {
    if (!this.license) {
      throw new Error('No license to transfer');
    }

    try {
      const response = await this.makeLicenseRequest('/transfer', {
        licenseKey: this.license.key,
        newUser: newUser,
        newEmail: newEmail
      });

      if (response.success) {
        // Update local license
        this.license.user = newUser;
        this.license.transferredAt = Date.now();
        await this.saveLicense(this.license);

        return {
          success: true,
          message: 'License transferred successfully',
          newUser: newUser
        };
      } else {
        throw new Error(response.error || 'License transfer failed');
      }
    } catch (error) {
      throw new Error(`Transfer failed: ${error.message}`);
    }
  }

  /**
   * Revoke license
   */
  async revoke() {
    if (!this.license) {
      throw new Error('No license to revoke');
    }

    try {
      const response = await this.makeLicenseRequest('/revoke', {
        licenseKey: this.license.key
      });

      if (response.success) {
        // Remove local license
        await fs.unlink(this.licenseFile);
        this.license = null;

        return {
          success: true,
          message: 'License revoked successfully'
        };
      } else {
        throw new Error(response.error || 'License revocation failed');
      }
    } catch (error) {
      throw new Error(`Revocation failed: ${error.message}`);
    }
  }
}

// Global license manager instance
const licenseManager = new LicenseManager();

// Command definitions
const check = new Command('check')
  .description('Check current license status')
  .option('-v, --verbose', 'Show detailed license information')
  .action(async (options) => {
    try {
      const status = await licenseManager.checkStatus();
      
      if (status.valid) {
        console.log(`✅ ${status.message}`);
        console.log(`   Status: ${status.status}`);
        console.log(`   Features: ${status.features.length}`);
        
        if (status.expires) {
          console.log(`   Expires: ${status.expires.toISOString()}`);
        }
        
        if (options.verbose) {
          const info = await licenseManager.getInfo();
          console.log(`   User: ${info.user}`);
          console.log(`   Type: ${info.type}`);
          console.log(`   Machine ID: ${info.machineId}`);
        }
      } else {
        console.log(`❌ ${status.message}`);
        if (status.expires) {
          console.log(`   Expired: ${status.expires.toISOString()}`);
        }
      }
    } catch (error) {
      console.error(`❌ Error checking license: ${error.message}`);
    }
  });

const activate = new Command('activate')
  .description('Activate license with key')
  .argument('<key>', 'License key')
  .action(async (key) => {
    try {
      const result = await licenseManager.activate(key);
      console.log(`✅ ${result.message}`);
      console.log(`   License type: ${result.license.type}`);
      console.log(`   Features: ${result.license.features.length}`);
      
      if (result.license.expiresAt) {
        console.log(`   Expires: ${new Date(result.license.expiresAt).toISOString()}`);
      }
    } catch (error) {
      console.error(`❌ ${error.message}`);
    }
  });

const validate = new Command('validate')
  .description('Validate license with server')
  .argument('[key]', 'License key (uses current license if not provided)')
  .action(async (key) => {
    try {
      const result = await licenseManager.validate(key);
      console.log(`✅ ${result.message}`);
      console.log(`   User: ${result.user}`);
      console.log(`   Features: ${result.features.length}`);
      
      if (result.expiresAt) {
        console.log(`   Expires: ${new Date(result.expiresAt).toISOString()}`);
      }
    } catch (error) {
      console.error(`❌ ${error.message}`);
    }
  });

const info = new Command('info')
  .description('Display license information')
  .action(async () => {
    try {
      const info = await licenseManager.getInfo();
      
      if (!info.hasLicense) {
        console.log(`❌ ${info.message}`);
        return;
      }

      console.log(`📋 License Information:`);
      console.log(`   Status: ${info.status}`);
      console.log(`   Valid: ${info.valid ? 'Yes' : 'No'}`);
      console.log(`   Key: ${info.key}`);
      console.log(`   User: ${info.user}`);
      console.log(`   Type: ${info.type}`);
      console.log(`   Features: ${info.features.length}`);
      console.log(`   Machine ID: ${info.machineId}`);
      
      if (info.activatedAt) {
        console.log(`   Activated: ${info.activatedAt.toISOString()}`);
      }
      
      if (info.expiresAt) {
        console.log(`   Expires: ${info.expiresAt.toISOString()}`);
      }
      
      if (info.lastValidated) {
        console.log(`   Last validated: ${info.lastValidated.toISOString()}`);
      }
      
      console.log(`   Server validated: ${info.serverValidated ? 'Yes' : 'No'}`);
    } catch (error) {
      console.error(`❌ Error getting license info: ${error.message}`);
    }
  });

const transfer = new Command('transfer')
  .description('Transfer license to another user')
  .argument('<user>', 'New user name')
  .argument('<email>', 'New user email')
  .action(async (user, email) => {
    try {
      const result = await licenseManager.transfer(user, email);
      console.log(`✅ ${result.message}`);
      console.log(`   Transferred to: ${result.newUser}`);
    } catch (error) {
      console.error(`❌ ${error.message}`);
    }
  });

const revoke = new Command('revoke')
  .description('Revoke license')
  .option('-f, --force', 'Force revocation without confirmation')
  .action(async (options) => {
    try {
      if (!options.force) {
        console.log('⚠️  This will permanently revoke your license. Use --force to confirm.');
        return;
      }

      const result = await licenseManager.revoke();
      console.log(`✅ ${result.message}`);
    } catch (error) {
      console.error(`❌ ${error.message}`);
    }
  });

module.exports = {
  check,
  activate,
  validate,
  info,
  transfer,
  revoke,
  
  // Export for testing
  _LicenseManager: LicenseManager,
  _licenseManager: licenseManager
}; 