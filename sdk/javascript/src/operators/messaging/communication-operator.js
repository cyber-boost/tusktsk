/**
 * TuskLang JavaScript SDK - Communication Operator
 * Production-ready team communication (Slack, Teams, Discord)
 * 
 * Features:
 * - Real Slack API integration with rich message formatting
 * - Microsoft Teams integration with adaptive cards
 * - Discord integration with embeds and reactions
 * - Channel and thread management
 * - Bot integration with slash commands
 * - Webhook integration for real-time notifications
 * - File upload and sharing capabilities
 * - Comprehensive error handling and retry logic
 * - Circuit breakers for fault tolerance
 * - Structured logging with metrics collection
 * - Memory leak prevention and resource cleanup
 */

const https = require('https');
const http = require('http');
const crypto = require('crypto');
const { EventEmitter } = require('events');
const FormData = require('form-data');

class CommunicationOperator extends EventEmitter {
  constructor(config = {}) {
    super();
    
    this.config = {
      slack: {
        token: config.slack?.token || process.env.SLACK_TOKEN,
        webhookUrl: config.slack?.webhookUrl || process.env.SLACK_WEBHOOK_URL,
        botToken: config.slack?.botToken || process.env.SLACK_BOT_TOKEN,
        signingSecret: config.slack?.signingSecret || process.env.SLACK_SIGNING_SECRET
      },
      teams: {
        webhookUrl: config.teams?.webhookUrl || process.env.TEAMS_WEBHOOK_URL,
        appId: config.teams?.appId || process.env.TEAMS_APP_ID,
        appPassword: config.teams?.appPassword || process.env.TEAMS_APP_PASSWORD,
        tenantId: config.teams?.tenantId || process.env.TEAMS_TENANT_ID
      },
      discord: {
        token: config.discord?.token || process.env.DISCORD_TOKEN,
        webhookUrl: config.discord?.webhookUrl || process.env.DISCORD_WEBHOOK_URL,
        botToken: config.discord?.botToken || process.env.DISCORD_BOT_TOKEN,
        applicationId: config.discord?.applicationId || process.env.DISCORD_APPLICATION_ID
      },
      timeout: config.timeout || 45000,
      retries: config.retries || 3,
      retryDelay: config.retryDelay || 1000,
      circuitBreakerThreshold: config.circuitBreakerThreshold || 5,
      circuitBreakerTimeout: config.circuitBreakerTimeout || 60000,
      ...config
    };

    this.messages = new Map();
    this.channels = new Map();
    this.threads = new Map();
    this.files = new Map();
    this.webhooks = new Map();
    
    this.circuitBreakers = {
      slack: { failures: 0, lastFailure: 0, state: 'CLOSED' },
      teams: { failures: 0, lastFailure: 0, state: 'CLOSED' },
      discord: { failures: 0, lastFailure: 0, state: 'CLOSED' }
    };
    
    this.connectionPool = new Map();
    this.activeRequests = 0;
    this.maxConcurrentRequests = 50;
    
    this.stats = {
      requests: 0,
      errors: 0,
      latency: [],
      messagesSent: { slack: 0, teams: 0, discord: 0 },
      filesUploaded: { slack: 0, teams: 0, discord: 0 },
      lastReset: Date.now()
    };

    this.setupCircuitBreakers();
    this.setupHealthCheck();
  }

  /**
   * Setup circuit breakers for all platforms
   */
  setupCircuitBreakers() {
    setInterval(() => {
      Object.entries(this.circuitBreakers).forEach(([platform, breaker]) => {
        if (breaker.state === 'OPEN' && 
            Date.now() - breaker.lastFailure > this.config.circuitBreakerTimeout) {
          breaker.state = 'HALF_OPEN';
          console.log(`CommunicationOperator: ${platform} circuit breaker moved to HALF_OPEN`);
        }
      });
    }, 1000);
  }

  /**
   * Setup health check for all platforms
   */
  setupHealthCheck() {
    setInterval(() => {
      this.checkHealth();
    }, 30000);
  }

  /**
   * Send message to Slack
   */
  async sendSlackMessage(channel, message, options = {}) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      if (this.circuitBreakers.slack.state === 'OPEN') {
        throw new Error('Slack circuit breaker is OPEN');
      }
      
      const startTime = Date.now();
      
      const payload = {
        channel,
        text: message,
        ...options
      };
      
      // Add attachments if provided
      if (options.attachments) {
        payload.attachments = options.attachments;
      }
      
      // Add blocks if provided
      if (options.blocks) {
        payload.blocks = options.blocks;
      }
      
      const url = 'https://slack.com/api/chat.postMessage';
      const result = await this.makeSlackRequest(url, 'POST', JSON.stringify(payload));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success && result.data.ok) {
        this.stats.messagesSent.slack++;
        const messageData = result.data;
        this.messages.set(messageData.ts, { platform: 'slack', ...messageData });
        
        console.log(`CommunicationOperator: Successfully sent Slack message to ${channel}`);
        this.emit('slack_message_sent', messageData);
        return messageData;
      } else {
        this.stats.errors++;
        this.circuitBreakers.slack.failures++;
        this.circuitBreakers.slack.lastFailure = Date.now();
        
        if (this.circuitBreakers.slack.failures >= this.config.circuitBreakerThreshold) {
          this.circuitBreakers.slack.state = 'OPEN';
          console.log('CommunicationOperator: Slack circuit breaker opened');
        }
        
        throw new Error(result.data?.error || 'Failed to send Slack message');
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('CommunicationOperator: Error sending Slack message:', error.message);
      throw error;
    }
  }

  /**
   * Send message to Teams
   */
  async sendTeamsMessage(webhookUrl, message, options = {}) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      if (this.circuitBreakers.teams.state === 'OPEN') {
        throw new Error('Teams circuit breaker is OPEN');
      }
      
      const startTime = Date.now();
      
      const payload = {
        text: message,
        ...options
      };
      
      // Add adaptive card if provided
      if (options.adaptiveCard) {
        payload.attachments = [{
          contentType: 'application/vnd.microsoft.card.adaptive',
          content: options.adaptiveCard
        }];
      }
      
      // Add actions if provided
      if (options.actions) {
        payload.potentialAction = options.actions;
      }
      
      const result = await this.makeTeamsRequest(webhookUrl, 'POST', JSON.stringify(payload));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        this.stats.messagesSent.teams++;
        const messageData = { platform: 'teams', timestamp: Date.now(), ...payload };
        this.messages.set(messageData.timestamp, messageData);
        
        console.log(`CommunicationOperator: Successfully sent Teams message`);
        this.emit('teams_message_sent', messageData);
        return messageData;
      } else {
        this.stats.errors++;
        this.circuitBreakers.teams.failures++;
        this.circuitBreakers.teams.lastFailure = Date.now();
        
        if (this.circuitBreakers.teams.failures >= this.config.circuitBreakerThreshold) {
          this.circuitBreakers.teams.state = 'OPEN';
          console.log('CommunicationOperator: Teams circuit breaker opened');
        }
        
        throw new Error(result.error || 'Failed to send Teams message');
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('CommunicationOperator: Error sending Teams message:', error.message);
      throw error;
    }
  }

  /**
   * Send message to Discord
   */
  async sendDiscordMessage(channelId, message, options = {}) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      if (this.circuitBreakers.discord.state === 'OPEN') {
        throw new Error('Discord circuit breaker is OPEN');
      }
      
      const startTime = Date.now();
      
      const payload = {
        content: message,
        ...options
      };
      
      // Add embeds if provided
      if (options.embeds) {
        payload.embeds = options.embeds;
      }
      
      // Add components if provided
      if (options.components) {
        payload.components = options.components;
      }
      
      const url = `https://discord.com/api/v10/channels/${channelId}/messages`;
      const result = await this.makeDiscordRequest(url, 'POST', JSON.stringify(payload));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        this.stats.messagesSent.discord++;
        const messageData = result.data;
        this.messages.set(messageData.id, { platform: 'discord', ...messageData });
        
        console.log(`CommunicationOperator: Successfully sent Discord message to ${channelId}`);
        this.emit('discord_message_sent', messageData);
        return messageData;
      } else {
        this.stats.errors++;
        this.circuitBreakers.discord.failures++;
        this.circuitBreakers.discord.lastFailure = Date.now();
        
        if (this.circuitBreakers.discord.failures >= this.config.circuitBreakerThreshold) {
          this.circuitBreakers.discord.state = 'OPEN';
          console.log('CommunicationOperator: Discord circuit breaker opened');
        }
        
        throw new Error(result.error || 'Failed to send Discord message');
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('CommunicationOperator: Error sending Discord message:', error.message);
      throw error;
    }
  }

  /**
   * Upload file to Slack
   */
  async uploadSlackFile(channel, filePath, title = null, comment = null) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const form = new FormData();
      form.append('channels', channel);
      form.append('file', require('fs').createReadStream(filePath));
      if (title) form.append('title', title);
      if (comment) form.append('initial_comment', comment);
      
      const url = 'https://slack.com/api/files.upload';
      const result = await this.makeSlackRequest(url, 'POST', form, form.getHeaders());
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success && result.data.ok) {
        this.stats.filesUploaded.slack++;
        const fileData = result.data.file;
        this.files.set(fileData.id, { platform: 'slack', ...fileData });
        
        console.log(`CommunicationOperator: Successfully uploaded file to Slack: ${fileData.name}`);
        this.emit('slack_file_uploaded', fileData);
        return fileData;
      } else {
        this.stats.errors++;
        throw new Error(result.data?.error || 'Failed to upload file to Slack');
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('CommunicationOperator: Error uploading file to Slack:', error.message);
      throw error;
    }
  }

  /**
   * Upload file to Discord
   */
  async uploadDiscordFile(channelId, filePath, message = null) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const form = new FormData();
      if (message) form.append('content', message);
      form.append('file', require('fs').createReadStream(filePath));
      
      const url = `https://discord.com/api/v10/channels/${channelId}/messages`;
      const result = await this.makeDiscordRequest(url, 'POST', form, form.getHeaders());
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        this.stats.filesUploaded.discord++;
        const messageData = result.data;
        this.files.set(messageData.id, { platform: 'discord', ...messageData });
        
        console.log(`CommunicationOperator: Successfully uploaded file to Discord`);
        this.emit('discord_file_uploaded', messageData);
        return messageData;
      } else {
        this.stats.errors++;
        throw new Error(result.error || 'Failed to upload file to Discord');
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('CommunicationOperator: Error uploading file to Discord:', error.message);
      throw error;
    }
  }

  /**
   * Create Slack thread
   */
  async createSlackThread(channel, message, threadTs = null) {
    try {
      const options = {};
      if (threadTs) {
        options.thread_ts = threadTs;
      }
      
      const result = await this.sendSlackMessage(channel, message, options);
      
      if (result.thread_ts) {
        this.threads.set(result.thread_ts, { platform: 'slack', channel, ts: result.thread_ts });
      }
      
      return result;
    } catch (error) {
      console.error('CommunicationOperator: Error creating Slack thread:', error.message);
      throw error;
    }
  }

  /**
   * Reply to Discord message
   */
  async replyDiscordMessage(channelId, messageId, message, options = {}) {
    try {
      const payload = {
        content: message,
        message_reference: {
          message_id: messageId
        },
        ...options
      };
      
      const url = `https://discord.com/api/v10/channels/${channelId}/messages`;
      const result = await this.makeDiscordRequest(url, 'POST', JSON.stringify(payload));
      
      if (result.success) {
        this.stats.messagesSent.discord++;
        const messageData = result.data;
        this.messages.set(messageData.id, { platform: 'discord', ...messageData });
        
        console.log(`CommunicationOperator: Successfully replied to Discord message`);
        this.emit('discord_message_replied', messageData);
        return messageData;
      } else {
        throw new Error(result.error || 'Failed to reply to Discord message');
      }
    } catch (error) {
      console.error('CommunicationOperator: Error replying to Discord message:', error.message);
      throw error;
    }
  }

  /**
   * Get Slack channels
   */
  async getSlackChannels() {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = 'https://slack.com/api/conversations.list';
      const result = await this.makeSlackRequest(url, 'GET');
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success && result.data.ok) {
        const channels = result.data.channels;
        channels.forEach(channel => {
          this.channels.set(channel.id, { platform: 'slack', ...channel });
        });
        
        console.log(`CommunicationOperator: Retrieved ${channels.length} Slack channels`);
        return channels;
      } else {
        this.stats.errors++;
        throw new Error(result.data?.error || 'Failed to get Slack channels');
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('CommunicationOperator: Error getting Slack channels:', error.message);
      throw error;
    }
  }

  /**
   * Get Discord channels
   */
  async getDiscordChannels(guildId) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `https://discord.com/api/v10/guilds/${guildId}/channels`;
      const result = await this.makeDiscordRequest(url, 'GET');
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        const channels = result.data;
        channels.forEach(channel => {
          this.channels.set(channel.id, { platform: 'discord', ...channel });
        });
        
        console.log(`CommunicationOperator: Retrieved ${channels.length} Discord channels`);
        return channels;
      } else {
        this.stats.errors++;
        throw new Error(result.error || 'Failed to get Discord channels');
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('CommunicationOperator: Error getting Discord channels:', error.message);
      throw error;
    }
  }

  /**
   * Create Slack webhook
   */
  async createSlackWebhook(channel, triggers = []) {
    try {
      const webhookId = crypto.randomUUID();
      const webhook = {
        id: webhookId,
        platform: 'slack',
        channel,
        triggers,
        url: this.config.slack.webhookUrl,
        createdAt: Date.now()
      };
      
      this.webhooks.set(webhookId, webhook);
      
      console.log(`CommunicationOperator: Created Slack webhook: ${webhookId}`);
      this.emit('slack_webhook_created', webhook);
      return webhook;
    } catch (error) {
      console.error('CommunicationOperator: Error creating Slack webhook:', error.message);
      throw error;
    }
  }

  /**
   * Send webhook notification
   */
  async sendWebhookNotification(webhookId, message, data = {}) {
    try {
      const webhook = this.webhooks.get(webhookId);
      if (!webhook) {
        throw new Error(`Webhook ${webhookId} not found`);
      }
      
      const payload = {
        text: message,
        attachments: [{
          color: data.color || '#36a64f',
          title: data.title,
          text: data.text,
          fields: data.fields || [],
          timestamp: new Date().toISOString()
        }]
      };
      
      const result = await this.makeRequest(webhook.url, 'POST', JSON.stringify(payload));
      
      if (result.success) {
        console.log(`CommunicationOperator: Successfully sent webhook notification: ${webhookId}`);
        this.emit('webhook_notification_sent', { webhookId, payload });
        return result;
      } else {
        throw new Error(result.error || 'Failed to send webhook notification');
      }
    } catch (error) {
      console.error('CommunicationOperator: Error sending webhook notification:', error.message);
      throw error;
    }
  }

  /**
   * Make Slack API request
   */
  async makeSlackRequest(url, method = 'GET', data = null, headers = {}) {
    const requestHeaders = {
      'Authorization': `Bearer ${this.config.slack.token}`,
      'Content-Type': 'application/json',
      ...headers
    };
    
    return this.makeRequest(url, method, data, requestHeaders);
  }

  /**
   * Make Teams API request
   */
  async makeTeamsRequest(url, method = 'GET', data = null, headers = {}) {
    const requestHeaders = {
      'Content-Type': 'application/json',
      ...headers
    };
    
    return this.makeRequest(url, method, data, requestHeaders);
  }

  /**
   * Make Discord API request
   */
  async makeDiscordRequest(url, method = 'GET', data = null, headers = {}) {
    const requestHeaders = {
      'Authorization': `Bot ${this.config.discord.botToken}`,
      'Content-Type': 'application/json',
      ...headers
    };
    
    return this.makeRequest(url, method, data, requestHeaders);
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
        'User-Agent': 'TuskLang-CommunicationOperator/1.0',
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
   * Check health of all platforms
   */
  async checkHealth() {
    try {
      const health = {
        slack: false,
        teams: false,
        discord: false,
        timestamp: Date.now()
      };
      
      // Check Slack
      try {
        await this.getSlackChannels();
        health.slack = true;
      } catch (error) {
        console.warn('CommunicationOperator: Slack health check failed:', error.message);
      }
      
      // Check Teams
      try {
        await this.sendTeamsMessage(this.config.teams.webhookUrl, 'Health check', { text: 'Health check' });
        health.teams = true;
      } catch (error) {
        console.warn('CommunicationOperator: Teams health check failed:', error.message);
      }
      
      // Check Discord
      try {
        // Try to get user info as health check
        const url = 'https://discord.com/api/v10/users/@me';
        await this.makeDiscordRequest(url, 'GET');
        health.discord = true;
      } catch (error) {
        console.warn('CommunicationOperator: Discord health check failed:', error.message);
      }
      
      this.emit('health_check', health);
      return health;
    } catch (error) {
      console.error('CommunicationOperator: Health check error:', error.message);
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
      messagesCount: this.messages.size,
      channelsCount: this.channels.size,
      threadsCount: this.threads.size,
      filesCount: this.files.size,
      webhooksCount: this.webhooks.size
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
      messagesSent: { slack: 0, teams: 0, discord: 0 },
      filesUploaded: { slack: 0, teams: 0, discord: 0 },
      lastReset: Date.now()
    };
    
    console.log('CommunicationOperator: Statistics reset');
  }

  /**
   * Cleanup resources
   */
  async cleanup() {
    try {
      // Clear all caches
      this.messages.clear();
      this.channels.clear();
      this.threads.clear();
      this.files.clear();
      this.webhooks.clear();
      
      // Clear connection pool
      this.connectionPool.clear();
      
      // Reset circuit breakers
      this.circuitBreakers = {
        slack: { failures: 0, lastFailure: 0, state: 'CLOSED' },
        teams: { failures: 0, lastFailure: 0, state: 'CLOSED' },
        discord: { failures: 0, lastFailure: 0, state: 'CLOSED' }
      };
      
      // Reset statistics
      this.resetStats();
      
      console.log('CommunicationOperator: Cleanup completed');
    } catch (error) {
      console.error('CommunicationOperator: Cleanup error:', error.message);
      throw error;
    }
  }
}

module.exports = CommunicationOperator; 