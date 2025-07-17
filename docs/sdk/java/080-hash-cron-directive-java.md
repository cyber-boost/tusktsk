# #cron - Cron Directive (Java)

The `#cron` directive provides enterprise-grade scheduled task capabilities for Java applications, enabling automated job execution with Spring Boot integration and comprehensive scheduling features.

## Basic Syntax

```tusk
# Basic cron job - every minute
#cron "* * * * *" {
    @cleanup_temp_files()
}

# Cron with custom schedule
#cron "0 2 * * *" {
    @backup_database()
}

# Cron with conditions
#cron "0 0 * * *" if: @is_production() {
    @generate_daily_report()
}
```

## Java Implementation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.CronDirective;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;
import org.springframework.stereotype.Service;
import org.springframework.context.annotation.Configuration;
import org.springframework.scheduling.annotation.EnableScheduling;

@Configuration
@EnableScheduling
public class SchedulingConfiguration {
    // Spring Boot scheduling configuration
}

@Component
public class CronJobs {
    
    private final TuskLang tuskLang;
    private final CronDirective cronDirective;
    private final CleanupService cleanupService;
    private final BackupService backupService;
    private final ReportService reportService;
    
    public CronJobs(TuskLang tuskLang, 
                   CleanupService cleanupService,
                   BackupService backupService,
                   ReportService reportService) {
        this.tuskLang = tuskLang;
        this.cronDirective = new CronDirective();
        this.cleanupService = cleanupService;
        this.backupService = backupService;
        this.reportService = reportService;
    }
    
    // Basic cron job - every minute
    @Scheduled(cron = "* * * * *")
    public void cleanupTempFiles() {
        cleanupService.cleanupTempFiles();
    }
    
    // Cron with custom schedule - daily at 2 AM
    @Scheduled(cron = "0 2 * * *")
    public void backupDatabase() {
        backupService.backupDatabase();
    }
    
    // Cron with conditions - daily at midnight, only in production
    @Scheduled(cron = "0 0 * * *")
    public void generateDailyReport() {
        if (environmentService.isProduction()) {
            reportService.generateDailyReport();
        }
    }
}
```

## Cron Configuration

```tusk
# Detailed cron configuration
#cron {
    schedule: "0 0 * * *"     # Cron expression
    timezone: "UTC"           # Timezone
    enabled: true             # Enable/disable job
    retry: 3                  # Retry attempts
    timeout: 300              # Timeout in seconds
} {
    @complex_scheduled_task()
}

# Multiple schedules
#cron {
    schedules: [
        "0 0 * * *",          # Daily at midnight
        "0 6 * * *",          # Daily at 6 AM
        "0 12 * * *"          # Daily at noon
    ]
} {
    @multi_schedule_task()
}

# Conditional scheduling
#cron {
    schedule: "0 2 * * *"
    condition: @is_backup_window()
    fallback: "0 3 * * *"
} {
    @conditional_backup()
}
```

## Java Cron Configuration

```java
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;
import org.springframework.scheduling.annotation.Scheduled;
import java.util.List;
import java.util.Map;
import java.util.TimeZone;

@Component
@ConfigurationProperties(prefix = "tusk.cron")
public class CronConfig {
    
    private String defaultTimezone = "UTC";
    private boolean defaultEnabled = true;
    private int defaultRetry = 3;
    private int defaultTimeout = 300;
    
    private Map<String, CronJobDefinition> jobs;
    private List<String> enabledJobs;
    private Map<String, String> timezones;
    
    // Getters and setters
    public String getDefaultTimezone() { return defaultTimezone; }
    public void setDefaultTimezone(String defaultTimezone) { this.defaultTimezone = defaultTimezone; }
    
    public boolean isDefaultEnabled() { return defaultEnabled; }
    public void setDefaultEnabled(boolean defaultEnabled) { this.defaultEnabled = defaultEnabled; }
    
    public int getDefaultRetry() { return defaultRetry; }
    public void setDefaultRetry(int defaultRetry) { this.defaultRetry = defaultRetry; }
    
    public int getDefaultTimeout() { return defaultTimeout; }
    public void setDefaultTimeout(int defaultTimeout) { this.defaultTimeout = defaultTimeout; }
    
    public Map<String, CronJobDefinition> getJobs() { return jobs; }
    public void setJobs(Map<String, CronJobDefinition> jobs) { this.jobs = jobs; }
    
    public List<String> getEnabledJobs() { return enabledJobs; }
    public void setEnabledJobs(List<String> enabledJobs) { this.enabledJobs = enabledJobs; }
    
    public Map<String, String> getTimezones() { return timezones; }
    public void setTimezones(Map<String, String> timezones) { this.timezones = timezones; }
    
    public static class CronJobDefinition {
        private String schedule;
        private String timezone;
        private boolean enabled = true;
        private int retry = 3;
        private int timeout = 300;
        private String condition;
        private String fallback;
        private List<String> schedules;
        
        // Getters and setters
        public String getSchedule() { return schedule; }
        public void setSchedule(String schedule) { this.schedule = schedule; }
        
        public String getTimezone() { return timezone; }
        public void setTimezone(String timezone) { this.timezone = timezone; }
        
        public boolean isEnabled() { return enabled; }
        public void setEnabled(boolean enabled) { this.enabled = enabled; }
        
        public int getRetry() { return retry; }
        public void setRetry(int retry) { this.retry = retry; }
        
        public int getTimeout() { return timeout; }
        public void setTimeout(int timeout) { this.timeout = timeout; }
        
        public String getCondition() { return condition; }
        public void setCondition(String condition) { this.condition = condition; }
        
        public String getFallback() { return fallback; }
        public void setFallback(String fallback) { this.fallback = fallback; }
        
        public List<String> getSchedules() { return schedules; }
        public void setSchedules(List<String> schedules) { this.schedules = schedules; }
    }
}

@Service
public class CronJobService {
    
    private final CronConfig config;
    private final TaskScheduler taskScheduler;
    private final Map<String, ScheduledTask> scheduledTasks;
    
    public CronJobService(CronConfig config, TaskScheduler taskScheduler) {
        this.config = config;
        this.taskScheduler = taskScheduler;
        this.scheduledTasks = new ConcurrentHashMap<>();
    }
    
    public void scheduleJob(String jobName, CronConfig.CronJobDefinition definition, Runnable task) {
        if (!definition.isEnabled()) {
            return;
        }
        
        CronTrigger trigger = createCronTrigger(definition);
        ScheduledTask scheduledTask = taskScheduler.schedule(task, trigger);
        scheduledTasks.put(jobName, scheduledTask);
    }
    
    private CronTrigger createCronTrigger(CronConfig.CronJobDefinition definition) {
        String schedule = definition.getSchedule();
        String timezone = definition.getTimezone() != null ? definition.getTimezone() : config.getDefaultTimezone();
        
        return new CronTrigger(schedule, TimeZone.getTimeZone(timezone));
    }
    
    public void cancelJob(String jobName) {
        ScheduledTask task = scheduledTasks.get(jobName);
        if (task != null) {
            task.cancel();
            scheduledTasks.remove(jobName);
        }
    }
    
    public boolean isJobScheduled(String jobName) {
        return scheduledTasks.containsKey(jobName);
    }
}
```

## Database Maintenance Jobs

```tusk
# Database cleanup - daily at 3 AM
#cron "0 3 * * *" {
    @cleanup_old_records()
    @optimize_tables()
    @update_statistics()
}

# Database backup - daily at 2 AM
#cron "0 2 * * *" {
    @backup_database("main_db")
    @backup_database("analytics_db")
    @cleanup_old_backups()
}

# Database maintenance with conditions
#cron {
    schedule: "0 4 * * 0"  # Weekly on Sunday at 4 AM
    condition: @is_maintenance_window()
} {
    @full_database_maintenance()
}
```

## Java Database Maintenance Jobs

```java
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

@Component
public class DatabaseMaintenanceJobs {
    
    private final DatabaseService databaseService;
    private final BackupService backupService;
    private final MaintenanceService maintenanceService;
    
    public DatabaseMaintenanceJobs(DatabaseService databaseService,
                                 BackupService backupService,
                                 MaintenanceService maintenanceService) {
        this.databaseService = databaseService;
        this.backupService = backupService;
        this.maintenanceService = maintenanceService;
    }
    
    // Database cleanup - daily at 3 AM
    @Scheduled(cron = "0 3 * * *")
    @Transactional
    public void cleanupDatabase() {
        try {
            // Cleanup old records
            databaseService.cleanupOldRecords();
            
            // Optimize tables
            databaseService.optimizeTables();
            
            // Update statistics
            databaseService.updateStatistics();
            
            logger.info("Database cleanup completed successfully");
        } catch (Exception e) {
            logger.error("Database cleanup failed", e);
            throw e;
        }
    }
    
    // Database backup - daily at 2 AM
    @Scheduled(cron = "0 2 * * *")
    public void backupDatabases() {
        try {
            // Backup main database
            backupService.backupDatabase("main_db");
            
            // Backup analytics database
            backupService.backupDatabase("analytics_db");
            
            // Cleanup old backups
            backupService.cleanupOldBackups();
            
            logger.info("Database backup completed successfully");
        } catch (Exception e) {
            logger.error("Database backup failed", e);
            throw e;
        }
    }
    
    // Weekly maintenance - Sunday at 4 AM
    @Scheduled(cron = "0 4 * * 0")
    public void weeklyMaintenance() {
        if (maintenanceService.isMaintenanceWindow()) {
            try {
                maintenanceService.performFullMaintenance();
                logger.info("Weekly maintenance completed successfully");
            } catch (Exception e) {
                logger.error("Weekly maintenance failed", e);
                throw e;
            }
        } else {
            logger.info("Skipping weekly maintenance - not in maintenance window");
        }
    }
}

@Service
public class DatabaseService {
    
    private final JdbcTemplate jdbcTemplate;
    private final DataSource dataSource;
    
    public DatabaseService(JdbcTemplate jdbcTemplate, DataSource dataSource) {
        this.jdbcTemplate = jdbcTemplate;
        this.dataSource = dataSource;
    }
    
    @Transactional
    public void cleanupOldRecords() {
        // Delete old log records (older than 90 days)
        jdbcTemplate.update("DELETE FROM logs WHERE created_at < DATE_SUB(NOW(), INTERVAL 90 DAY)");
        
        // Delete old session data (older than 30 days)
        jdbcTemplate.update("DELETE FROM sessions WHERE last_accessed < DATE_SUB(NOW(), INTERVAL 30 DAY)");
        
        // Delete old temporary files
        jdbcTemplate.update("DELETE FROM temp_files WHERE created_at < DATE_SUB(NOW(), INTERVAL 7 DAY)");
    }
    
    public void optimizeTables() {
        // Get all table names
        List<String> tables = jdbcTemplate.queryForList(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = DATABASE()",
            String.class
        );
        
        for (String table : tables) {
            try {
                jdbcTemplate.execute("OPTIMIZE TABLE " + table);
                logger.debug("Optimized table: {}", table);
            } catch (Exception e) {
                logger.warn("Failed to optimize table: {}", table, e);
            }
        }
    }
    
    public void updateStatistics() {
        // Update table statistics
        jdbcTemplate.execute("ANALYZE TABLE");
        
        // Update index statistics
        jdbcTemplate.execute("ANALYZE TABLE");
    }
}

@Service
public class BackupService {
    
    private final DataSource dataSource;
    private final FileService fileService;
    
    public BackupService(DataSource dataSource, FileService fileService) {
        this.dataSource = dataSource;
        this.fileService = fileService;
    }
    
    public void backupDatabase(String databaseName) {
        String backupPath = "/backups/" + databaseName + "_" + 
                           LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss")) + ".sql";
        
        try {
            // Create backup directory if it doesn't exist
            fileService.createDirectoryIfNotExists("/backups");
            
            // Perform database backup
            ProcessBuilder pb = new ProcessBuilder(
                "mysqldump",
                "--host=" + getDatabaseHost(),
                "--port=" + getDatabasePort(),
                "--user=" + getDatabaseUser(),
                "--password=" + getDatabasePassword(),
                "--single-transaction",
                "--routines",
                "--triggers",
                databaseName
            );
            
            Process process = pb.start();
            
            // Write backup to file
            try (FileOutputStream fos = new FileOutputStream(backupPath)) {
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = process.getInputStream().read(buffer)) != -1) {
                    fos.write(buffer, 0, bytesRead);
                }
            }
            
            int exitCode = process.waitFor();
            if (exitCode != 0) {
                throw new RuntimeException("Database backup failed with exit code: " + exitCode);
            }
            
            logger.info("Database backup completed: {}", backupPath);
        } catch (Exception e) {
            logger.error("Database backup failed for: {}", databaseName, e);
            throw new RuntimeException("Database backup failed", e);
        }
    }
    
    public void cleanupOldBackups() {
        try {
            // Keep backups for 30 days
            LocalDateTime cutoffDate = LocalDateTime.now().minusDays(30);
            
            File backupDir = new File("/backups");
            if (backupDir.exists()) {
                File[] backupFiles = backupDir.listFiles((dir, name) -> name.endsWith(".sql"));
                
                if (backupFiles != null) {
                    for (File backupFile : backupFiles) {
                        if (backupFile.lastModified() < cutoffDate.toInstant(ZoneOffset.UTC).toEpochMilli()) {
                            backupFile.delete();
                            logger.info("Deleted old backup: {}", backupFile.getName());
                        }
                    }
                }
            }
        } catch (Exception e) {
            logger.error("Failed to cleanup old backups", e);
        }
    }
    
    private String getDatabaseHost() {
        // Extract host from datasource
        return "localhost"; // Simplified
    }
    
    private String getDatabasePort() {
        return "3306"; // Simplified
    }
    
    private String getDatabaseUser() {
        return "root"; // Simplified
    }
    
    private String getDatabasePassword() {
        return "password"; // Simplified
    }
}
```

## Email and Notification Jobs

```tusk
# Daily digest email - 8 AM daily
#cron "0 8 * * *" {
    @send_daily_digest()
}

# Weekly newsletter - Monday at 9 AM
#cron "0 9 * * 1" {
    @send_weekly_newsletter()
}

# Notification cleanup - every 6 hours
#cron "0 */6 * * *" {
    @cleanup_old_notifications()
}
```

## Java Email and Notification Jobs

```java
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;
import org.springframework.mail.javamail.JavaMailSender;

@Component
public class EmailNotificationJobs {
    
    private final EmailService emailService;
    private final NotificationService notificationService;
    private final DigestService digestService;
    private final NewsletterService newsletterService;
    
    public EmailNotificationJobs(EmailService emailService,
                               NotificationService notificationService,
                               DigestService digestService,
                               NewsletterService newsletterService) {
        this.emailService = emailService;
        this.notificationService = notificationService;
        this.digestService = digestService;
        this.newsletterService = newsletterService;
    }
    
    // Daily digest email - 8 AM daily
    @Scheduled(cron = "0 8 * * *")
    public void sendDailyDigest() {
        try {
            List<User> users = userService.getUsersWithDigestEnabled();
            
            for (User user : users) {
                DailyDigest digest = digestService.generateDailyDigest(user.getId());
                emailService.sendDailyDigest(user.getEmail(), digest);
            }
            
            logger.info("Daily digest sent to {} users", users.size());
        } catch (Exception e) {
            logger.error("Failed to send daily digest", e);
            throw e;
        }
    }
    
    // Weekly newsletter - Monday at 9 AM
    @Scheduled(cron = "0 9 * * 1")
    public void sendWeeklyNewsletter() {
        try {
            List<User> subscribers = userService.getNewsletterSubscribers();
            
            WeeklyNewsletter newsletter = newsletterService.generateWeeklyNewsletter();
            
            for (User user : subscribers) {
                emailService.sendWeeklyNewsletter(user.getEmail(), newsletter);
            }
            
            logger.info("Weekly newsletter sent to {} subscribers", subscribers.size());
        } catch (Exception e) {
            logger.error("Failed to send weekly newsletter", e);
            throw e;
        }
    }
    
    // Notification cleanup - every 6 hours
    @Scheduled(cron = "0 */6 * * *")
    public void cleanupOldNotifications() {
        try {
            int deletedCount = notificationService.cleanupOldNotifications();
            logger.info("Cleaned up {} old notifications", deletedCount);
        } catch (Exception e) {
            logger.error("Failed to cleanup old notifications", e);
            throw e;
        }
    }
}

@Service
public class EmailService {
    
    private final JavaMailSender mailSender;
    private final TemplateEngine templateEngine;
    
    public EmailService(JavaMailSender mailSender, TemplateEngine templateEngine) {
        this.mailSender = mailSender;
        this.templateEngine = templateEngine;
    }
    
    public void sendDailyDigest(String email, DailyDigest digest) {
        try {
            MimeMessage message = mailSender.createMimeMessage();
            MimeMessageHelper helper = new MimeMessageHelper(message, true);
            
            helper.setTo(email);
            helper.setSubject("Your Daily Digest");
            helper.setFrom("noreply@example.com");
            
            // Generate HTML content
            Context context = new Context();
            context.setVariable("digest", digest);
            String htmlContent = templateEngine.process("daily-digest", context);
            
            helper.setText(htmlContent, true);
            
            mailSender.send(message);
            
            logger.debug("Daily digest sent to: {}", email);
        } catch (Exception e) {
            logger.error("Failed to send daily digest to: {}", email, e);
            throw new RuntimeException("Failed to send daily digest", e);
        }
    }
    
    public void sendWeeklyNewsletter(String email, WeeklyNewsletter newsletter) {
        try {
            MimeMessage message = mailSender.createMimeMessage();
            MimeMessageHelper helper = new MimeMessageHelper(message, true);
            
            helper.setTo(email);
            helper.setSubject("Weekly Newsletter");
            helper.setFrom("newsletter@example.com");
            
            // Generate HTML content
            Context context = new Context();
            context.setVariable("newsletter", newsletter);
            String htmlContent = templateEngine.process("weekly-newsletter", context);
            
            helper.setText(htmlContent, true);
            
            mailSender.send(message);
            
            logger.debug("Weekly newsletter sent to: {}", email);
        } catch (Exception e) {
            logger.error("Failed to send weekly newsletter to: {}", email, e);
            throw new RuntimeException("Failed to send weekly newsletter", e);
        }
    }
}

@Service
public class NotificationService {
    
    private final JdbcTemplate jdbcTemplate;
    
    public NotificationService(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }
    
    public int cleanupOldNotifications() {
        // Delete notifications older than 30 days
        return jdbcTemplate.update(
            "DELETE FROM notifications WHERE created_at < DATE_SUB(NOW(), INTERVAL 30 DAY)"
        );
    }
}
```

## Data Processing Jobs

```tusk
# Data aggregation - hourly
#cron "0 * * * *" {
    @aggregate_hourly_data()
}

# Report generation - daily at 6 AM
#cron "0 6 * * *" {
    @generate_daily_reports()
}

# Data synchronization - every 15 minutes
#cron "*/15 * * * *" {
    @sync_external_data()
}
```

## Java Data Processing Jobs

```java
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;
import org.springframework.batch.core.Job;
import org.springframework.batch.core.JobParameters;
import org.springframework.batch.core.JobParametersBuilder;
import org.springframework.batch.core.launch.JobLauncher;

@Component
public class DataProcessingJobs {
    
    private final JobLauncher jobLauncher;
    private final Job hourlyAggregationJob;
    private final Job dailyReportJob;
    private final DataSyncService dataSyncService;
    
    public DataProcessingJobs(JobLauncher jobLauncher,
                            Job hourlyAggregationJob,
                            Job dailyReportJob,
                            DataSyncService dataSyncService) {
        this.jobLauncher = jobLauncher;
        this.hourlyAggregationJob = hourlyAggregationJob;
        this.dailyReportJob = dailyReportJob;
        this.dataSyncService = dataSyncService;
    }
    
    // Data aggregation - hourly
    @Scheduled(cron = "0 * * * *")
    public void aggregateHourlyData() {
        try {
            JobParameters jobParameters = new JobParametersBuilder()
                .addLong("timestamp", System.currentTimeMillis())
                .toJobParameters();
            
            jobLauncher.run(hourlyAggregationJob, jobParameters);
            
            logger.info("Hourly data aggregation completed");
        } catch (Exception e) {
            logger.error("Hourly data aggregation failed", e);
            throw e;
        }
    }
    
    // Report generation - daily at 6 AM
    @Scheduled(cron = "0 6 * * *")
    public void generateDailyReports() {
        try {
            JobParameters jobParameters = new JobParametersBuilder()
                .addLong("timestamp", System.currentTimeMillis())
                .toJobParameters();
            
            jobLauncher.run(dailyReportJob, jobParameters);
            
            logger.info("Daily report generation completed");
        } catch (Exception e) {
            logger.error("Daily report generation failed", e);
            throw e;
        }
    }
    
    // Data synchronization - every 15 minutes
    @Scheduled(cron = "*/15 * * * *")
    public void syncExternalData() {
        try {
            dataSyncService.syncExternalData();
            logger.info("External data synchronization completed");
        } catch (Exception e) {
            logger.error("External data synchronization failed", e);
            throw e;
        }
    }
}

@Service
public class DataSyncService {
    
    private final ExternalApiClient externalApiClient;
    private final DataRepository dataRepository;
    
    public DataSyncService(ExternalApiClient externalApiClient, DataRepository dataRepository) {
        this.externalApiClient = externalApiClient;
        this.dataRepository = dataRepository;
    }
    
    @Transactional
    public void syncExternalData() {
        try {
            // Fetch data from external API
            List<ExternalData> externalData = externalApiClient.fetchData();
            
            // Process and save data
            for (ExternalData data : externalData) {
                DataEntity entity = convertToEntity(data);
                dataRepository.save(entity);
            }
            
            logger.debug("Synced {} external data records", externalData.size());
        } catch (Exception e) {
            logger.error("Failed to sync external data", e);
            throw new RuntimeException("External data sync failed", e);
        }
    }
    
    private DataEntity convertToEntity(ExternalData externalData) {
        DataEntity entity = new DataEntity();
        entity.setExternalId(externalData.getId());
        entity.setName(externalData.getName());
        entity.setValue(externalData.getValue());
        entity.setLastSynced(LocalDateTime.now());
        return entity;
    }
}
```

## System Maintenance Jobs

```tusk
# System health check - every 5 minutes
#cron "*/5 * * * *" {
    @check_system_health()
}

# Log rotation - daily at 1 AM
#cron "0 1 * * *" {
    @rotate_logs()
}

# Cache cleanup - every 2 hours
#cron "0 */2 * * *" {
    @cleanup_cache()
}
```

## Java System Maintenance Jobs

```java
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;
import org.springframework.cache.CacheManager;

@Component
public class SystemMaintenanceJobs {
    
    private final HealthCheckService healthCheckService;
    private final LogRotationService logRotationService;
    private final CacheManager cacheManager;
    
    public SystemMaintenanceJobs(HealthCheckService healthCheckService,
                               LogRotationService logRotationService,
                               CacheManager cacheManager) {
        this.healthCheckService = healthCheckService;
        this.logRotationService = logRotationService;
        this.cacheManager = cacheManager;
    }
    
    // System health check - every 5 minutes
    @Scheduled(cron = "*/5 * * * *")
    public void checkSystemHealth() {
        try {
            HealthStatus healthStatus = healthCheckService.checkSystemHealth();
            
            if (!healthStatus.isHealthy()) {
                logger.warn("System health check failed: {}", healthStatus.getIssues());
                notificationService.sendHealthAlert(healthStatus);
            } else {
                logger.debug("System health check passed");
            }
        } catch (Exception e) {
            logger.error("System health check failed", e);
            throw e;
        }
    }
    
    // Log rotation - daily at 1 AM
    @Scheduled(cron = "0 1 * * *")
    public void rotateLogs() {
        try {
            logRotationService.rotateLogs();
            logger.info("Log rotation completed");
        } catch (Exception e) {
            logger.error("Log rotation failed", e);
            throw e;
        }
    }
    
    // Cache cleanup - every 2 hours
    @Scheduled(cron = "0 */2 * * *")
    public void cleanupCache() {
        try {
            cacheManager.getCacheNames().forEach(cacheName -> {
                Cache cache = cacheManager.getCache(cacheName);
                if (cache != null) {
                    cache.clear();
                    logger.debug("Cleared cache: {}", cacheName);
                }
            });
            
            logger.info("Cache cleanup completed");
        } catch (Exception e) {
            logger.error("Cache cleanup failed", e);
            throw e;
        }
    }
}

@Service
public class HealthCheckService {
    
    private final DataSource dataSource;
    private final RedisTemplate<String, String> redisTemplate;
    
    public HealthCheckService(DataSource dataSource, RedisTemplate<String, String> redisTemplate) {
        this.dataSource = dataSource;
        this.redisTemplate = redisTemplate;
    }
    
    public HealthStatus checkSystemHealth() {
        HealthStatus status = new HealthStatus();
        List<String> issues = new ArrayList<>();
        
        // Check database connectivity
        try {
            dataSource.getConnection().close();
        } catch (Exception e) {
            issues.add("Database connectivity issue: " + e.getMessage());
        }
        
        // Check Redis connectivity
        try {
            redisTemplate.opsForValue().get("health_check");
        } catch (Exception e) {
            issues.add("Redis connectivity issue: " + e.getMessage());
        }
        
        // Check disk space
        File root = new File("/");
        long freeSpace = root.getFreeSpace();
        long totalSpace = root.getTotalSpace();
        double usagePercent = (double) (totalSpace - freeSpace) / totalSpace * 100;
        
        if (usagePercent > 90) {
            issues.add("Disk space usage high: " + String.format("%.1f%%", usagePercent));
        }
        
        // Check memory usage
        Runtime runtime = Runtime.getRuntime();
        long maxMemory = runtime.maxMemory();
        long usedMemory = runtime.totalMemory() - runtime.freeMemory();
        double memoryUsagePercent = (double) usedMemory / maxMemory * 100;
        
        if (memoryUsagePercent > 85) {
            issues.add("Memory usage high: " + String.format("%.1f%%", memoryUsagePercent));
        }
        
        status.setHealthy(issues.isEmpty());
        status.setIssues(issues);
        
        return status;
    }
}

@Service
public class LogRotationService {
    
    private final String logDirectory = "/var/log/application";
    
    public void rotateLogs() {
        try {
            File logDir = new File(logDirectory);
            if (!logDir.exists()) {
                return;
            }
            
            File[] logFiles = logDir.listFiles((dir, name) -> name.endsWith(".log"));
            if (logFiles == null) {
                return;
            }
            
            for (File logFile : logFiles) {
                rotateLogFile(logFile);
            }
            
            // Cleanup old rotated logs (keep for 30 days)
            cleanupOldRotatedLogs();
        } catch (Exception e) {
            logger.error("Log rotation failed", e);
            throw new RuntimeException("Log rotation failed", e);
        }
    }
    
    private void rotateLogFile(File logFile) throws IOException {
        String timestamp = LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss"));
        File rotatedFile = new File(logFile.getParent(), 
                                   logFile.getName() + "." + timestamp);
        
        // Move current log to rotated file
        Files.move(logFile.toPath(), rotatedFile.toPath());
        
        // Create new empty log file
        logFile.createNewFile();
        
        // Compress rotated file
        compressFile(rotatedFile);
        
        logger.info("Rotated log file: {} -> {}", logFile.getName(), rotatedFile.getName());
    }
    
    private void compressFile(File file) throws IOException {
        try (GZIPOutputStream gzipOut = new GZIPOutputStream(
                new FileOutputStream(file.getAbsolutePath() + ".gz"))) {
            Files.copy(file.toPath(), gzipOut);
        }
        
        // Delete original file after compression
        file.delete();
    }
    
    private void cleanupOldRotatedLogs() {
        LocalDateTime cutoffDate = LocalDateTime.now().minusDays(30);
        
        File logDir = new File(logDirectory);
        File[] rotatedFiles = logDir.listFiles((dir, name) -> name.endsWith(".gz"));
        
        if (rotatedFiles != null) {
            for (File rotatedFile : rotatedFiles) {
                if (rotatedFile.lastModified() < 
                    cutoffDate.toInstant(ZoneOffset.UTC).toEpochMilli()) {
                    rotatedFile.delete();
                    logger.info("Deleted old rotated log: {}", rotatedFile.getName());
                }
            }
        }
    }
}
```

## Cron Job Testing

```java
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.TestPropertySource;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.test.context.ActiveProfiles;

@SpringBootTest
@TestPropertySource(properties = {
    "tusk.cron.default-enabled=true",
    "spring.task.scheduling.pool.size=5"
})
@ActiveProfiles("test")
public class CronJobTest {
    
    @Autowired
    private CronJobs cronJobs;
    
    @MockBean
    private CleanupService cleanupService;
    
    @MockBean
    private BackupService backupService;
    
    @MockBean
    private ReportService reportService;
    
    @Test
    public void testCleanupTempFiles() {
        // Test cleanup job
        cronJobs.cleanupTempFiles();
        
        verify(cleanupService).cleanupTempFiles();
    }
    
    @Test
    public void testBackupDatabase() {
        // Test backup job
        cronJobs.backupDatabase();
        
        verify(backupService).backupDatabase();
    }
    
    @Test
    public void testGenerateDailyReport() {
        // Test report generation job
        when(environmentService.isProduction()).thenReturn(true);
        
        cronJobs.generateDailyReport();
        
        verify(reportService).generateDailyReport();
    }
    
    @Test
    public void testGenerateDailyReportNotProduction() {
        // Test report generation job when not in production
        when(environmentService.isProduction()).thenReturn(false);
        
        cronJobs.generateDailyReport();
        
        verify(reportService, never()).generateDailyReport();
    }
}
```

## Configuration Properties

```yaml
# application.yml
tusk:
  cron:
    default-timezone: "UTC"
    default-enabled: true
    default-retry: 3
    default-timeout: 300
    
    jobs:
      database-cleanup:
        schedule: "0 3 * * *"
        timezone: "UTC"
        enabled: true
        retry: 3
        timeout: 600
      
      database-backup:
        schedule: "0 2 * * *"
        timezone: "UTC"
        enabled: true
        retry: 2
        timeout: 1800
      
      daily-digest:
        schedule: "0 8 * * *"
        timezone: "America/New_York"
        enabled: true
        retry: 3
        timeout: 300
      
      weekly-newsletter:
        schedule: "0 9 * * 1"
        timezone: "America/New_York"
        enabled: true
        retry: 2
        timeout: 600
      
      system-health:
        schedule: "*/5 * * * *"
        timezone: "UTC"
        enabled: true
        retry: 1
        timeout: 60
    
    enabled-jobs:
      - "database-cleanup"
      - "database-backup"
      - "daily-digest"
      - "weekly-newsletter"
      - "system-health"
    
    timezones:
      UTC: "UTC"
      EST: "America/New_York"
      PST: "America/Los_Angeles"

spring:
  task:
    scheduling:
      pool:
        size: 10
      thread-name-prefix: "cron-"
  
  mail:
    host: smtp.gmail.com
    port: 587
    username: ${MAIL_USERNAME}
    password: ${MAIL_PASSWORD}
    properties:
      mail:
        smtp:
          auth: true
          starttls:
            enable: true
```

## Summary

The `#cron` directive in TuskLang provides comprehensive scheduled task capabilities for Java applications. With Spring Boot integration, flexible scheduling, and support for various job types, you can implement sophisticated automated processes that enhance your application's functionality.

Key features include:
- **Multiple job types**: Database maintenance, email notifications, data processing, and system maintenance
- **Spring Boot integration**: Seamless integration with Spring Boot scheduling
- **Flexible configuration**: Configurable cron expressions with timezone support
- **Conditional scheduling**: Execute jobs based on conditions
- **Error handling**: Retry mechanisms and timeout configuration
- **Monitoring**: Built-in logging and monitoring capabilities
- **Testing support**: Comprehensive testing utilities

The Java implementation provides enterprise-grade scheduling that integrates seamlessly with Spring Boot applications while maintaining the simplicity and power of TuskLang's declarative syntax. 