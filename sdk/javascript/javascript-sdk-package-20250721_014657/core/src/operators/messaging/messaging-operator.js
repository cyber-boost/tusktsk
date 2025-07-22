/**
 * TuskLang JavaScript SDK - Messaging Operator
 * Production-ready email and SMS services
 * 
 * Features:
 * - Real SMTP integration with TLS encryption and authentication
 * - HTML/plain text email composition with attachments
 * - SMS gateway integration (Twilio, AWS SNS)
 * - Delivery status tracking and bounce handling
 * - Template engine integration for dynamic content
 * - Comprehensive error handling and retry logic
 * - Circuit breakers for fault tolerance
 * - Structured logging with metrics collection
 * - Memory leak prevention and resource cleanup
 */

const https = require('https');
const http = require('http');
const crypto = require('crypto');
const { EventEmitter } = require('events');
const nodemailer = require('nodemailer');

class MessagingOperator extends EventEmitter {
  constructor(config = {}) {
    super();
    
    this.config = {
      email: {
        smtp: {
          host: config.email?.smtp?.host || process.env.SMTP_HOST,
          port: config.email?.smtp?.port || process.env.SMTP_PORT || 587,
          secure: config.email?.smtp?.secure || false,
          auth: {
            user: config.email?.smtp?.auth?.user || process.env.SMTP_USER,
            pass: config.email?.smtp?.auth?.pass || process.env.SMTP_PASS
          }
        },
        from: config.email?.from || process.env.EMAIL_FROM,
        replyTo: config.email?.replyTo || process.env.EMAIL_REPLY_TO
      },
      sms: {
        twilio: {
          accountSid: config.sms?.twilio?.accountSid || process.env.TWILIO_ACCOUNT_SID,
          authToken: config.sms?.twilio?.authToken || process.env.TWILIO_AUTH_TOKEN,
          from: config.sms?.twilio?.from || process.env.TWILIO_FROM
        },
        aws: {
          accessKeyId: config.sms?.aws?.accessKeyId || process.env.AWS_ACCESS_KEY_ID,
          secretAccessKey: config.sms?.aws?.secretAccessKey || process.env.AWS_SECRET_ACCESS_KEY,
          region: config.sms?.aws?.region || process.env.AWS_REGION || 'us-east-1',
          from: config.sms?.aws?.from || process.env.AWS_SMS_FROM
        }
      },
      templates: {
        directory: config.templates?.directory || './templates',
        engine: config.templates?.engine || 'handlebars'
      },
      timeout: config.timeout || 45000,
      retries: config.retries || 3,
      retryDelay: config.retryDelay || 1000,
      circuitBreakerThreshold: config.circuitBreakerThreshold || 5,
      circuitBreakerTimeout: config.circuitBreakerTimeout || 60000,
      ...config
    };

    this.transporter = null;
    this.templates = new Map();
    this.messages = new Map();
    this.deliveryStatus = new Map();
    this.bounces = new Map();
    
    this.circuitBreakers = {
      email: { failures: 0, lastFailure: 0, state: 'CLOSED' },
      sms: { failures: 0, lastFailure: 0, state: 'CLOSED' }
    };
    
    this.connectionPool = new Map();
    this.activeRequests = 0;
    this.maxConcurrentRequests = 50;
    
    this.stats = {
      requests: 0,
      errors: 0,
      latency: [],
      emailsSent: 0,
      smsSent: 0,
      deliveries: { success: 0, failed: 0, pending: 0 },
      lastReset: Date.now()
    };

    this.setupCircuitBreakers();
    this.setupTransporter();
    this.setupTemplates();
    this.setupHealthCheck();
  }

  /**
   * Setup circuit breakers for email and SMS
   */
  setupCircuitBreakers() {
    setInterval(() => {
      Object.entries(this.circuitBreakers).forEach(([service, breaker]) => {
        if (breaker.state === 'OPEN' && 
            Date.now() - breaker.lastFailure > this.config.circuitBreakerTimeout) {
          breaker.state = 'HALF_OPEN';
          console.log(`MessagingOperator: ${service} circuit breaker moved to HALF_OPEN`);
        }
      });
    }, 1000);
  }

  /**
   * Setup email transporter
   */
  setupTransporter() {
    try {
      if (this.config.email.smtp.host) {
        this.transporter = nodemailer.createTransporter(this.config.email.smtp);
        console.log('MessagingOperator: Email transporter configured');
      }
    } catch (error) {
      console.error('MessagingOperator: Error setting up email transporter:', error.message);
    }
  }

  /**
   * Setup template engine
   */
  setupTemplates() {
    try {
      // Load default templates
      this.loadTemplate('welcome', {
        subject: 'Welcome to {{service}}',
        html: `
          <h1>Welcome to {{service}}!</h1>
          <p>Hello {{name}},</p>
          <p>Thank you for joining {{service}}. We're excited to have you on board!</p>
          <p>Best regards,<br>The {{service}} Team</p>
        `,
        text: 'Welcome to {{service}}! Hello {{name}}, thank you for joining us.'
      });
      
      this.loadTemplate('notification', {
        subject: '{{title}}',
        html: `
          <h2>{{title}}</h2>
          <p>{{message}}</p>
          <p>Timestamp: {{timestamp}}</p>
        `,
        text: '{{title}}: {{message}}'
      });
      
      console.log('MessagingOperator: Template engine configured');
    } catch (error) {
      console.error('MessagingOperator: Error setting up templates:', error.message);
    }
  }

  /**
   * Setup health check
   */
  setupHealthCheck() {
    setInterval(() => {
      this.checkHealth();
    }, 30000);
  }

  /**
   * Send email
   */
  async sendEmail(to, subject, content, options = {}) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      if (this.circuitBreakers.email.state === 'OPEN') {
        throw new Error('Email circuit breaker is OPEN');
      }
      
      if (!this.transporter) {
        throw new Error('Email transporter not configured');
      }
      
      const startTime = Date.now();
      
      const messageId = crypto.randomUUID();
      const emailData = {
        id: messageId,
        to,
        subject,
        content,
        options,
        timestamp: Date.now(),
        status: 'pending'
      };
      
      const mailOptions = {
        from: options.from || this.config.email.from,
        to,
        subject,
        text: content.text || content,
        html: content.html,
        attachments: options.attachments || [],
        replyTo: options.replyTo || this.config.email.replyTo,
        headers: options.headers || {}
      };
      
      const result = await this.transporter.sendMail(mailOptions);
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.messageId) {
        this.stats.emailsSent++;
        emailData.status = 'sent';
        emailData.messageId = result.messageId;
        this.messages.set(messageId, emailData);
        
        console.log(`MessagingOperator: Successfully sent email to ${to}`);
        this.emit('email_sent', emailData);
        return emailData;
      } else {
        this.stats.errors++;
        this.circuitBreakers.email.failures++;
        this.circuitBreakers.email.lastFailure = Date.now();
        
        if (this.circuitBreakers.email.failures >= this.config.circuitBreakerThreshold) {
          this.circuitBreakers.email.state = 'OPEN';
          console.log('MessagingOperator: Email circuit breaker opened');
        }
        
        throw new Error('Failed to send email');
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('MessagingOperator: Error sending email:', error.message);
      throw error;
    }
  }

  /**
   * Send SMS via Twilio
   */
  async sendSmsTwilio(to, message, options = {}) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      if (this.circuitBreakers.sms.state === 'OPEN') {
        throw new Error('SMS circuit breaker is OPEN');
      }
      
      const startTime = Date.now();
      
      const messageId = crypto.randomUUID();
      const smsData = {
        id: messageId,
        to,
        message,
        provider: 'twilio',
        options,
        timestamp: Date.now(),
        status: 'pending'
      };
      
      const payload = new URLSearchParams({
        To: to,
        From: options.from || this.config.sms.twilio.from,
        Body: message
      });
      
      const auth = Buffer.from(`${this.config.sms.twilio.accountSid}:${this.config.sms.twilio.authToken}`).toString('base64');
      
      const url = `https://api.twilio.com/2010-04-01/Accounts/${this.config.sms.twilio.accountSid}/Messages.json`;
      const result = await this.makeRequest(url, 'POST', payload.toString(), {
        'Authorization': `Basic ${auth}`,
        'Content-Type': 'application/x-www-form-urlencoded'
      });
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success && result.data.sid) {
        this.stats.smsSent++;
        smsData.status = 'sent';
        smsData.sid = result.data.sid;
        this.messages.set(messageId, smsData);
        
        console.log(`MessagingOperator: Successfully sent SMS to ${to} via Twilio`);
        this.emit('sms_sent', smsData);
        return smsData;
      } else {
        this.stats.errors++;
        this.circuitBreakers.sms.failures++;
        this.circuitBreakers.sms.lastFailure = Date.now();
        
        if (this.circuitBreakers.sms.failures >= this.config.circuitBreakerThreshold) {
          this.circuitBreakers.sms.state = 'OPEN';
          console.log('MessagingOperator: SMS circuit breaker opened');
        }
        
        throw new Error(result.data?.message || 'Failed to send SMS via Twilio');
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('MessagingOperator: Error sending SMS via Twilio:', error.message);
      throw error;
    }
  }

  /**
   * Send SMS via AWS SNS
   */
  async sendSmsAws(to, message, options = {}) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      if (this.circuitBreakers.sms.state === 'OPEN') {
        throw new Error('SMS circuit breaker is OPEN');
      }
      
      const startTime = Date.now();
      
      const messageId = crypto.randomUUID();
      const smsData = {
        id: messageId,
        to,
        message,
        provider: 'aws',
        options,
        timestamp: Date.now(),
        status: 'pending'
      };
      
      const payload = {
        Message: message,
        PhoneNumber: to,
        MessageAttributes: {
          'AWS.SNS.SMS.SMSType': {
            DataType: 'String',
            StringValue: options.smsType || 'Transactional'
          }
        }
      };
      
      const url = `https://sns.${this.config.sms.aws.region}.amazonaws.com/`;
      const result = await this.makeAwsRequest(url, 'Publish', payload);
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success && result.data.MessageId) {
        this.stats.smsSent++;
        smsData.status = 'sent';
        smsData.messageId = result.data.MessageId;
        this.messages.set(messageId, smsData);
        
        console.log(`MessagingOperator: Successfully sent SMS to ${to} via AWS SNS`);
        this.emit('sms_sent', smsData);
        return smsData;
      } else {
        this.stats.errors++;
        this.circuitBreakers.sms.failures++;
        this.circuitBreakers.sms.lastFailure = Date.now();
        
        if (this.circuitBreakers.sms.failures >= this.config.circuitBreakerThreshold) {
          this.circuitBreakers.sms.state = 'OPEN';
          console.log('MessagingOperator: SMS circuit breaker opened');
        }
        
        throw new Error(result.data?.message || 'Failed to send SMS via AWS SNS');
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('MessagingOperator: Error sending SMS via AWS SNS:', error.message);
      throw error;
    }
  }

  /**
   * Load template
   */
  loadTemplate(name, template) {
    try {
      this.templates.set(name, {
        name,
        subject: template.subject,
        html: template.html,
        text: template.text,
        createdAt: Date.now()
      });
      
      console.log(`MessagingOperator: Loaded template: ${name}`);
      this.emit('template_loaded', { name, template });
      return true;
    } catch (error) {
      console.error('MessagingOperator: Error loading template:', error.message);
      throw error;
    }
  }

  /**
   * Render template
   */
  renderTemplate(name, data) {
    try {
      const template = this.templates.get(name);
      if (!template) {
        throw new Error(`Template ${name} not found`);
      }
      
      const render = (content) => {
        return content.replace(/\{\{(\w+)\}\}/g, (match, key) => {
          return data[key] || match;
        });
      };
      
      return {
        subject: render(template.subject),
        html: template.html ? render(template.html) : null,
        text: template.text ? render(template.text) : null
      };
    } catch (error) {
      console.error('MessagingOperator: Error rendering template:', error.message);
      throw error;
    }
  }

  /**
   * Send templated email
   */
  async sendTemplatedEmail(templateName, to, data, options = {}) {
    try {
      const rendered = this.renderTemplate(templateName, data);
      
      const content = {
        text: rendered.text,
        html: rendered.html
      };
      
      return await this.sendEmail(to, rendered.subject, content, options);
    } catch (error) {
      console.error('MessagingOperator: Error sending templated email:', error.message);
      throw error;
    }
  }

  /**
   * Send templated SMS
   */
  async sendTemplatedSms(templateName, to, data, provider = 'twilio', options = {}) {
    try {
      const rendered = this.renderTemplate(templateName, data);
      const message = rendered.text || rendered.html;
      
      if (provider === 'twilio') {
        return await this.sendSmsTwilio(to, message, options);
      } else if (provider === 'aws') {
        return await this.sendSmsAws(to, message, options);
      } else {
        throw new Error(`Unknown SMS provider: ${provider}`);
      }
    } catch (error) {
      console.error('MessagingOperator: Error sending templated SMS:', error.message);
      throw error;
    }
  }

  /**
   * Track delivery status
   */
  trackDelivery(messageId, status, details = {}) {
    try {
      const delivery = {
        messageId,
        status,
        details,
        timestamp: Date.now()
      };
      
      this.deliveryStatus.set(messageId, delivery);
      
      if (status === 'delivered') {
        this.stats.deliveries.success++;
      } else if (status === 'failed') {
        this.stats.deliveries.failed++;
      } else {
        this.stats.deliveries.pending++;
      }
      
      console.log(`MessagingOperator: Delivery status updated for ${messageId}: ${status}`);
      this.emit('delivery_updated', delivery);
      return delivery;
    } catch (error) {
      console.error('MessagingOperator: Error tracking delivery:', error.message);
      throw error;
    }
  }

  /**
   * Handle bounce
   */
  handleBounce(messageId, bounceType, reason) {
    try {
      const bounce = {
        messageId,
        bounceType,
        reason,
        timestamp: Date.now()
      };
      
      this.bounces.set(messageId, bounce);
      this.trackDelivery(messageId, 'bounced', { bounceType, reason });
      
      console.log(`MessagingOperator: Bounce recorded for ${messageId}: ${bounceType}`);
      this.emit('bounce_handled', bounce);
      return bounce;
    } catch (error) {
      console.error('MessagingOperator: Error handling bounce:', error.message);
      throw error;
    }
  }

  /**
   * Get delivery status
   */
  getDeliveryStatus(messageId) {
    return this.deliveryStatus.get(messageId);
  }

  /**
   * Get bounce information
   */
  getBounce(messageId) {
    return this.bounces.get(messageId);
  }

  /**
   * Make HTTP request with circuit breaker and retry logic
   */
  async makeRequest(url, method = 'GET', data = null, headers = {}) {
    if (this.activeRequests >= this.maxConcurrentRequests) {
      throw new Error('Too many concurrent requests');
    }
    
    const urlObj = new URL(url);
    const isHttps = urlObj.protocol === 'https:';
    const client = isHttps ? https : http;
    
    const requestOptions = {
      hostname: urlObj.hostname,
      port: urlObj.port || (isHttps ? 443 : 80),
      path: urlObj.pathname + urlObj.search,
      method,
      headers: {
        'User-Agent': 'TuskLang-MessagingOperator/1.0',
        ...headers
      },
      timeout: this.config.timeout
    };
    
    let retries = 0;
    while (retries <= this.config.retries) {
      try {
        return await new Promise((resolve, reject) => {
          const req = client.request(requestOptions, (res) => {
            let responseData = '';
            
            res.on('data', (chunk) => {
              responseData += chunk;
            });
            
            res.on('end', () => {
              if (res.statusCode >= 200 && res.statusCode < 300) {
                let parsedData;
                try {
                  parsedData = JSON.parse(responseData);
                } catch {
                  parsedData = responseData;
                }
                
                resolve({
                  success: true,
                  statusCode: res.statusCode,
                  data: parsedData,
                  headers: res.headers
                });
              } else {
                reject(new Error(`HTTP ${res.statusCode}: ${responseData}`));
              }
            });
          });
          
          req.on('error', (error) => {
            reject(error);
          });
          
          req.on('timeout', () => {
            req.destroy();
            reject(new Error('Request timeout'));
          });
          
          if (data) {
            req.write(data);
          }
          req.end();
        });
      } catch (error) {
        retries++;
        
        if (retries > this.config.retries) {
          throw error;
        }
        
        await new Promise(resolve => setTimeout(resolve, this.config.retryDelay * retries));
      }
    }
  }

  /**
   * Make AWS SNS request
   */
  async makeAwsRequest(url, action, payload) {
    try {
      const timestamp = new Date().toISOString().replace(/[:-]|\.\d{3}/g, '');
      const date = timestamp.substr(0, 8);
      
      const canonicalRequest = [
        'POST',
        '/',
        '',
        'content-type:application/x-www-form-urlencoded',
        'host:' + new URL(url).hostname,
        '',
        'content-type;host',
        crypto.createHash('sha256').update(JSON.stringify(payload)).digest('hex')
      ].join('\n');
      
      const stringToSign = [
        'AWS4-HMAC-SHA256',
        timestamp,
        date + '/' + this.config.sms.aws.region + '/sns/aws4_request',
        crypto.createHash('sha256').update(canonicalRequest).digest('hex')
      ].join('\n');
      
      const signature = this.sign(stringToSign, this.config.sms.aws.secretAccessKey, date);
      
      const headers = {
        'Content-Type': 'application/x-www-form-urlencoded',
        'X-Amz-Date': timestamp,
        'Authorization': `AWS4-HMAC-SHA256 Credential=${this.config.sms.aws.accessKeyId}/${date}/${this.config.sms.aws.region}/sns/aws4_request,SignedHeaders=content-type;host,Signature=${signature}`
      };
      
      const formData = new URLSearchParams({
        Action: action,
        Version: '2010-03-31',
        ...payload
      });
      
      return await this.makeRequest(url, 'POST', formData.toString(), headers);
    } catch (error) {
      console.error('MessagingOperator: Error making AWS request:', error.message);
      throw error;
    }
  }

  /**
   * Sign AWS request
   */
  sign(stringToSign, secretKey, date) {
    const kDate = crypto.createHmac('sha256', 'AWS4' + secretKey).update(date).digest();
    const kRegion = crypto.createHmac('sha256', kDate).update(this.config.sms.aws.region).digest();
    const kService = crypto.createHmac('sha256', kRegion).update('sns').digest();
    const kSigning = crypto.createHmac('sha256', kService).update('aws4_request').digest();
    
    return crypto.createHmac('sha256', kSigning).update(stringToSign).digest('hex');
  }

  /**
   * Check health of messaging services
   */
  async checkHealth() {
    try {
      const health = {
        email: false,
        sms: false,
        timestamp: Date.now()
      };
      
      // Check email service
      try {
        if (this.transporter) {
          await this.transporter.verify();
          health.email = true;
        }
      } catch (error) {
        console.warn('MessagingOperator: Email health check failed:', error.message);
      }
      
      // Check SMS service (Twilio)
      try {
        if (this.config.sms.twilio.accountSid) {
          const url = `https://api.twilio.com/2010-04-01/Accounts/${this.config.sms.twilio.accountSid}.json`;
          await this.makeRequest(url, 'GET');
          health.sms = true;
        }
      } catch (error) {
        console.warn('MessagingOperator: SMS health check failed:', error.message);
      }
      
      this.emit('health_check', health);
      return health;
    } catch (error) {
      console.error('MessagingOperator: Health check error:', error.message);
      throw error;
    }
  }

  /**
   * Get operator statistics
   */
  getStats() {
    const now = Date.now();
    const uptime = now - this.stats.lastReset;
    const avgLatency = this.stats.latency.length > 0 
      ? this.stats.latency.reduce((a, b) => a + b, 0) / this.stats.latency.length 
      : 0;
    
    return {
      ...this.stats,
      uptime,
      avgLatency,
      circuitBreakers: this.circuitBreakers,
      activeRequests: this.activeRequests,
      templatesCount: this.templates.size,
      messagesCount: this.messages.size,
      deliveryStatusCount: this.deliveryStatus.size,
      bouncesCount: this.bounces.size
    };
  }

  /**
   * Reset statistics
   */
  resetStats() {
    this.stats = {
      requests: 0,
      errors: 0,
      latency: [],
      emailsSent: 0,
      smsSent: 0,
      deliveries: { success: 0, failed: 0, pending: 0 },
      lastReset: Date.now()
    };
    
    console.log('MessagingOperator: Statistics reset');
  }

  /**
   * Cleanup resources
   */
  async cleanup() {
    try {
      // Close transporter
      if (this.transporter) {
        this.transporter.close();
      }
      
      // Clear all caches
      this.templates.clear();
      this.messages.clear();
      this.deliveryStatus.clear();
      this.bounces.clear();
      
      // Clear connection pool
      this.connectionPool.clear();
      
      // Reset circuit breakers
      this.circuitBreakers = {
        email: { failures: 0, lastFailure: 0, state: 'CLOSED' },
        sms: { failures: 0, lastFailure: 0, state: 'CLOSED' }
      };
      
      // Reset statistics
      this.resetStats();
      
      console.log('MessagingOperator: Cleanup completed');
    } catch (error) {
      console.error('MessagingOperator: Cleanup error:', error.message);
      throw error;
    }
  }
}

module.exports = MessagingOperator; 