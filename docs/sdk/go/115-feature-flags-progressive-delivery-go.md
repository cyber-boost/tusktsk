# 🚩 Feature Flags & Progressive Delivery with TuskLang & Go

## Introduction
Feature flags and progressive delivery are the keys to safe, rapid deployment. TuskLang and Go let you implement sophisticated feature management with config-driven flags, A/B testing, canary deployments, and gradual rollouts that minimize risk and maximize learning.

## Key Features
- **Feature flag management and targeting**
- **A/B testing and experimentation**
- **Canary deployments and gradual rollouts**
- **Targeting rules and audience segmentation**
- **Experimentation frameworks**
- **Metrics collection and analysis**

## Example: Feature Flags Config
```ini
[feature_flags]
backend: redis
redis_uri: @env.secure("REDIS_URI")
default_state: @env("DEFAULT_FLAG_STATE", false)
metrics: @metrics("feature_flag_evaluations", 0)
targeting: @go("flags.EvaluateTargeting")
experimentation: @go("flags.RunExperiment")
```

## Go: Feature Flag Manager
```go
package flags

import (
    "context"
    "encoding/json"
    "time"
    "github.com/go-redis/redis/v8"
)

type FeatureFlag struct {
    Key         string                 `json:"key"`
    Enabled     bool                   `json:"enabled"`
    Percentage  int                    `json:"percentage"`
    Targeting   map[string]interface{} `json:"targeting"`
    Experiment  *Experiment            `json:"experiment"`
    CreatedAt   time.Time              `json:"created_at"`
    UpdatedAt   time.Time              `json:"updated_at"`
}

type Experiment struct {
    ID          string                 `json:"id"`
    Variants    []Variant              `json:"variants"`
    Metrics     []string               `json:"metrics"`
    Duration    time.Duration          `json:"duration"`
    StartDate   time.Time              `json:"start_date"`
}

type Variant struct {
    ID          string                 `json:"id"`
    Name        string                 `json:"name"`
    Percentage  int                    `json:"percentage"`
    Config      map[string]interface{} `json:"config"`
}

type FeatureFlagManager struct {
    redis *redis.Client
    cache map[string]*FeatureFlag
}

func (ffm *FeatureFlagManager) IsEnabled(ctx context.Context, flagKey string, user User) (bool, error) {
    // Get flag from cache or Redis
    flag, err := ffm.getFlag(ctx, flagKey)
    if err != nil {
        return false, err
    }
    
    if !flag.Enabled {
        return false, nil
    }
    
    // Check targeting rules
    if flag.Targeting != nil {
        if !ffm.evaluateTargeting(ctx, flag.Targeting, user) {
            return false, nil
        }
    }
    
    // Check percentage rollout
    if flag.Percentage < 100 {
        if !ffm.isInPercentage(ctx, flagKey, user.ID, flag.Percentage) {
            return false, nil
        }
    }
    
    // Record evaluation
    ffm.recordEvaluation(ctx, flagKey, user.ID, true)
    
    return true, nil
}

func (ffm *FeatureFlagManager) getFlag(ctx context.Context, key string) (*FeatureFlag, error) {
    // Check cache first
    if flag, exists := ffm.cache[key]; exists {
        return flag, nil
    }
    
    // Get from Redis
    data, err := ffm.redis.Get(ctx, "flag:"+key).Result()
    if err != nil {
        return nil, err
    }
    
    var flag FeatureFlag
    if err := json.Unmarshal([]byte(data), &flag); err != nil {
        return nil, err
    }
    
    // Cache flag
    ffm.cache[key] = &flag
    return &flag, nil
}
```

## A/B Testing Implementation
```go
func (ffm *FeatureFlagManager) GetVariant(ctx context.Context, flagKey string, user User) (string, error) {
    flag, err := ffm.getFlag(ctx, flagKey)
    if err != nil {
        return "", err
    }
    
    if flag.Experiment == nil {
        return "", ErrNoExperiment
    }
    
    // Check if user is already assigned to a variant
    variant, err := ffm.getUserVariant(ctx, flag.Experiment.ID, user.ID)
    if err == nil {
        return variant, nil
    }
    
    // Assign user to variant based on percentages
    variant = ffm.assignVariant(ctx, flag.Experiment.Variants, user.ID)
    
    // Store assignment
    ffm.setUserVariant(ctx, flag.Experiment.ID, user.ID, variant)
    
    return variant, nil
}

func (ffm *FeatureFlagManager) assignVariant(ctx context.Context, variants []Variant, userID string) string {
    // Use consistent hashing to ensure user gets same variant
    hash := hashString(userID)
    cumulative := 0
    
    for _, variant := range variants {
        cumulative += variant.Percentage
        if hash%100 < cumulative {
            return variant.ID
        }
    }
    
    // Fallback to first variant
    return variants[0].ID
}
```

## Canary Deployments
```go
type CanaryDeployment struct {
    ID          string
    Service     string
    Version     string
    Percentage  int
    Duration    time.Duration
    Metrics     []string
    Thresholds  map[string]float64
    StartTime   time.Time
}

func (ffm *FeatureFlagManager) DeployCanary(ctx context.Context, deployment CanaryDeployment) error {
    // Create canary flag
    flag := &FeatureFlag{
        Key:        fmt.Sprintf("canary_%s_%s", deployment.Service, deployment.Version),
        Enabled:    true,
        Percentage: deployment.Percentage,
        CreatedAt:  time.Now(),
    }
    
    // Store flag
    return ffm.setFlag(ctx, flag)
}

func (ffm *FeatureFlagManager) MonitorCanary(ctx context.Context, deploymentID string) error {
    deployment, err := ffm.getCanaryDeployment(ctx, deploymentID)
    if err != nil {
        return err
    }
    
    // Check metrics against thresholds
    for metric, threshold := range deployment.Thresholds {
        currentValue, err := ffm.getMetricValue(ctx, deployment.Service, metric)
        if err != nil {
            continue
        }
        
        if currentValue > threshold {
            // Rollback canary
            return ffm.rollbackCanary(ctx, deploymentID)
        }
    }
    
    // Check if canary duration has passed
    if time.Since(deployment.StartTime) > deployment.Duration {
        // Promote canary to full deployment
        return ffm.promoteCanary(ctx, deploymentID)
    }
    
    return nil
}
```

## Gradual Rollouts
```go
func (ffm *FeatureFlagManager) GradualRollout(ctx context.Context, flagKey string, rollout RolloutPlan) error {
    // Start with small percentage
    currentPercentage := rollout.InitialPercentage
    
    for currentPercentage <= rollout.TargetPercentage {
        // Update flag percentage
        flag, err := ffm.getFlag(ctx, flagKey)
        if err != nil {
            return err
        }
        
        flag.Percentage = currentPercentage
        if err := ffm.setFlag(ctx, flag); err != nil {
            return err
        }
        
        // Wait for evaluation period
        time.Sleep(rollout.EvaluationPeriod)
        
        // Check metrics
        if !ffm.checkRolloutMetrics(ctx, flagKey, rollout.Metrics) {
            // Rollback if metrics are poor
            flag.Percentage = rollout.InitialPercentage
            return ffm.setFlag(ctx, flag)
        }
        
        // Increase percentage
        currentPercentage += rollout.IncrementPercentage
    }
    
    return nil
}
```

## Targeting Rules
```go
func (ffm *FeatureFlagManager) evaluateTargeting(ctx context.Context, targeting map[string]interface{}, user User) bool {
    for rule, value := range targeting {
        switch rule {
        case "user_id":
            if userIDs, ok := value.([]string); ok {
                if !contains(userIDs, user.ID) {
                    return false
                }
            }
        case "email_domain":
            if domains, ok := value.([]string); ok {
                userDomain := extractDomain(user.Email)
                if !contains(domains, userDomain) {
                    return false
                }
            }
        case "user_type":
            if userType, ok := value.(string); ok {
                if user.Type != userType {
                    return false
                }
            }
        case "location":
            if locations, ok := value.([]string); ok {
                if !contains(locations, user.Location) {
                    return false
                }
            }
        case "custom_attribute":
            if attr, ok := value.(map[string]interface{}); ok {
                if !ffm.evaluateCustomAttribute(ctx, attr, user) {
                    return false
                }
            }
        }
    }
    
    return true
}
```

## Metrics Collection
```go
func (ffm *FeatureFlagManager) recordEvaluation(ctx context.Context, flagKey, userID string, enabled bool) {
    // Record evaluation event
    event := map[string]interface{}{
        "flag_key":  flagKey,
        "user_id":   userID,
        "enabled":   enabled,
        "timestamp": time.Now(),
    }
    
    eventJSON, _ := json.Marshal(event)
    ffm.redis.LPush(ctx, "flag_evaluations", eventJSON)
    
    // Increment metrics
    if enabled {
        ffm.redis.Incr(ctx, fmt.Sprintf("flag:%s:enabled", flagKey))
    } else {
        ffm.redis.Incr(ctx, fmt.Sprintf("flag:%s:disabled", flagKey))
    }
}

func (ffm *FeatureFlagManager) GetFlagMetrics(ctx context.Context, flagKey string) (map[string]int64, error) {
    enabled, err := ffm.redis.Get(ctx, fmt.Sprintf("flag:%s:enabled", flagKey)).Int64()
    if err != nil && err != redis.Nil {
        return nil, err
    }
    
    disabled, err := ffm.redis.Get(ctx, fmt.Sprintf("flag:%s:disabled", flagKey)).Int64()
    if err != nil && err != redis.Nil {
        return nil, err
    }
    
    return map[string]int64{
        "enabled":  enabled,
        "disabled": disabled,
        "total":    enabled + disabled,
    }, nil
}
```

## Best Practices
- **Use consistent hashing for A/B test assignments**
- **Implement proper targeting rules**
- **Monitor feature flag performance**
- **Use gradual rollouts for risky changes**
- **Implement automatic rollback mechanisms**
- **Collect comprehensive metrics**

## Performance Optimization
- **Cache feature flags locally**
- **Use Redis for distributed flag storage**
- **Implement efficient targeting evaluation**
- **Batch metrics collection**

## Security Considerations
- **Validate targeting rules**
- **Implement proper access controls**
- **Audit feature flag changes**
- **Protect sensitive targeting data**

## Troubleshooting
- **Monitor flag evaluation performance**
- **Check targeting rule logic**
- **Verify A/B test assignments**
- **Monitor canary deployment metrics**

## Conclusion
TuskLang + Go = feature flags that are powerful, safe, and data-driven. Deploy with confidence, learn from every change. 