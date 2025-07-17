const crypto = require('crypto');
const { Pool } = require('pg');
const bcrypt = require('bcrypt');

// Database connection
const pool = new Pool({
  connectionString: 'postgresql://tt_c3b2:Tusk83f8d5e5e2de70aed8a34bd9ef3277a4db4b6d5fLang@178.156.165.85:5432/tusklang_theory',
  ssl: { rejectUnauthorized: false }
});

class LicenseKeyGenerator {
  constructor() {
    this.chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    this.segments = 4;
    this.segmentLength = 4;
  }

  // Generate cryptographically secure random license key
  generateSecureKey() {
    const bytes = crypto.randomBytes(this.segments * this.segmentLength);
    let key = '';
    
    for (let i = 0; i < this.segments * this.segmentLength; i++) {
      key += this.chars[bytes[i] % this.chars.length];
      if ((i + 1) % this.segmentLength === 0 && i < this.segments * this.segmentLength - 1) {
        key += '-';
      }
    }
    
    return key;
  }

  // Generate key with specific pattern (for testing)
  generatePatternKey(pattern = 'XXXX-XXXX-XXXX-XXXX') {
    let key = '';
    for (let char of pattern) {
      if (char === 'X') {
        key += this.chars[Math.floor(Math.random() * this.chars.length)];
      } else {
        key += char;
      }
    }
    return key;
  }

  // Validate license key format
  validateKeyFormat(key) {
    const pattern = /^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$/;
    return pattern.test(key);
  }

  // Check if key already exists in database
  async isKeyUnique(key) {
    try {
      const result = await pool.query(
        'SELECT COUNT(*) FROM licenses WHERE license_key = $1',
        [key]
      );
      return parseInt(result.rows[0].count) === 0;
    } catch (error) {
      console.error('Database error checking key uniqueness:', error);
      throw error;
    }
  }

  // Generate unique license key
  async generateUniqueKey() {
    let attempts = 0;
    const maxAttempts = 100;

    while (attempts < maxAttempts) {
      const key = this.generateSecureKey();
      
      if (await this.isKeyUnique(key)) {
        return key;
      }
      
      attempts++;
    }
    
    throw new Error('Unable to generate unique license key after maximum attempts');
  }

  // Create license in database
  async createLicense(licenseData) {
    const {
      customer_name,
      customer_email,
      license_type = 'standard',
      max_installations = 1,
      expires_at = null,
      created_by = 'system',
      notes = ''
    } = licenseData;

    try {
      const license_key = await this.generateUniqueKey();
      
      const result = await pool.query(
        `INSERT INTO licenses 
         (license_key, customer_name, customer_email, license_type, max_installations, expires_at, created_by, notes)
         VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
         RETURNING *`,
        [license_key, customer_name, customer_email, license_type, max_installations, expires_at, created_by, notes]
      );

      return result.rows[0];
    } catch (error) {
      console.error('Error creating license:', error);
      throw error;
    }
  }

  // Batch generate licenses
  async generateBatch(count, licenseData) {
    const licenses = [];
    
    for (let i = 0; i < count; i++) {
      try {
        const license = await this.createLicense(licenseData);
        licenses.push(license);
        console.log(`Generated license ${i + 1}/${count}: ${license.license_key}`);
      } catch (error) {
        console.error(`Failed to generate license ${i + 1}:`, error);
      }
    }
    
    return licenses;
  }

  // Generate license with specific expiration
  async generateExpiringLicense(licenseData, daysUntilExpiry) {
    const expires_at = new Date();
    expires_at.setDate(expires_at.getDate() + daysUntilExpiry);
    
    return await this.createLicense({
      ...licenseData,
      expires_at: expires_at.toISOString()
    });
  }

  // Generate enterprise license with multiple installations
  async generateEnterpriseLicense(licenseData, maxInstallations = 10) {
    return await this.createLicense({
      ...licenseData,
      license_type: 'enterprise',
      max_installations: maxInstallations
    });
  }

  // Generate trial license (30 days)
  async generateTrialLicense(licenseData) {
    return await this.generateExpiringLicense(licenseData, 30);
  }

  // Generate premium license (1 year)
  async generatePremiumLicense(licenseData) {
    return await this.generateExpiringLicense(licenseData, 365);
  }

  // Verify license key integrity
  async verifyLicenseIntegrity(license_key) {
    try {
      const result = await pool.query(
        'SELECT * FROM licenses WHERE license_key = $1',
        [license_key]
      );
      
      if (result.rows.length === 0) {
        return { valid: false, reason: 'License not found' };
      }
      
      const license = result.rows[0];
      
      // Check if expired
      if (license.expires_at && new Date() > license.expires_at) {
        return { valid: false, reason: 'License expired' };
      }
      
      // Check if revoked
      if (license.status !== 'active') {
        return { valid: false, reason: `License ${license.status}` };
      }
      
      return { valid: true, license };
    } catch (error) {
      console.error('Error verifying license integrity:', error);
      return { valid: false, reason: 'Verification failed' };
    }
  }

  // Get license statistics
  async getLicenseStats() {
    try {
      const stats = await pool.query(`
        SELECT 
          COUNT(*) as total_licenses,
          COUNT(CASE WHEN status = 'active' THEN 1 END) as active_licenses,
          COUNT(CASE WHEN status = 'revoked' THEN 1 END) as revoked_licenses,
          COUNT(CASE WHEN status = 'expired' THEN 1 END) as expired_licenses,
          COUNT(CASE WHEN expires_at < NOW() THEN 1 END) as expired_licenses_time,
          AVG(current_installations) as avg_installations,
          SUM(current_installations) as total_installations
        FROM licenses
      `);
      
      return stats.rows[0];
    } catch (error) {
      console.error('Error getting license stats:', error);
      throw error;
    }
  }
}

// CLI interface for license generation
if (require.main === module) {
  const generator = new LicenseKeyGenerator();
  
  const command = process.argv[2];
  const args = process.argv.slice(3);
  
  switch (command) {
    case 'generate':
      const customerName = args[0] || 'Test Customer';
      const customerEmail = args[1] || 'test@example.com';
      const licenseType = args[2] || 'standard';
      
      generator.createLicense({
        customer_name: customerName,
        customer_email: customerEmail,
        license_type: licenseType
      }).then(license => {
        console.log('Generated license:', license);
      }).catch(error => {
        console.error('Error generating license:', error);
      });
      break;
      
    case 'batch':
      const count = parseInt(args[0]) || 10;
      const batchCustomerName = args[1] || 'Batch Customer';
      const batchCustomerEmail = args[2] || 'batch@example.com';
      
      generator.generateBatch(count, {
        customer_name: batchCustomerName,
        customer_email: batchCustomerEmail,
        license_type: 'standard'
      }).then(licenses => {
        console.log(`Generated ${licenses.length} licenses`);
      }).catch(error => {
        console.error('Error generating batch:', error);
      });
      break;
      
    case 'verify':
      const keyToVerify = args[0];
      if (!keyToVerify) {
        console.error('Please provide a license key to verify');
        process.exit(1);
      }
      
      generator.verifyLicenseIntegrity(keyToVerify).then(result => {
        console.log('Verification result:', result);
      }).catch(error => {
        console.error('Error verifying license:', error);
      });
      break;
      
    case 'stats':
      generator.getLicenseStats().then(stats => {
        console.log('License statistics:', stats);
      }).catch(error => {
        console.error('Error getting stats:', error);
      });
      break;
      
    default:
      console.log(`
Usage: node license-generator.js <command> [args]

Commands:
  generate [name] [email] [type]  Generate a single license
  batch [count] [name] [email]    Generate multiple licenses
  verify [key]                    Verify a license key
  stats                           Show license statistics

Examples:
  node license-generator.js generate "John Doe" "john@example.com" premium
  node license-generator.js batch 5 "Company Inc" "licenses@company.com"
  node license-generator.js verify ABCD-1234-EFGH-5678
  node license-generator.js stats
      `);
  }
}

module.exports = LicenseKeyGenerator; 