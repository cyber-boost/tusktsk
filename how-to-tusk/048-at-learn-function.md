# @learn() - Machine Learning Integration

The `@learn()` function provides built-in machine learning capabilities, enabling pattern recognition, predictions, and intelligent automation directly within TuskLang.

## Basic Syntax

```tusk
# Train a model
model: @learn.train("classifier", @training_data, {
    features: ["age", "income", "history"]
    target: "approved"
})

# Make predictions
prediction: @model.predict({
    age: 25
    income: 50000
    history: "good"
})

# Confidence score
confidence: @prediction.confidence
result: @prediction.class
```

## Classification

```tusk
# Customer churn prediction
churn_model: @learn.train("classifier", {
    data: @query("SELECT * FROM customer_history")
    features: [
        "months_active",
        "total_purchases", 
        "avg_order_value",
        "support_tickets",
        "last_login_days"
    ]
    target: "churned"
    algorithm: "random_forest"
    split: 0.8  # 80% training, 20% testing
})

# Predict churn risk
#api /customers/{id}/churn-risk {
    customer: @query("SELECT * FROM customers WHERE id = ?", [@id])
    
    risk: @churn_model.predict({
        months_active: @customer.months_active
        total_purchases: @customer.total_purchases
        avg_order_value: @customer.avg_order_value
        support_tickets: @customer.support_tickets
        last_login_days: @days_since(@customer.last_login)
    })
    
    @json({
        customer_id: @id
        churn_probability: @risk.probability
        risk_level: @risk.probability > 0.7 ? "high" : 
                   @risk.probability > 0.4 ? "medium" : "low"
        factors: @risk.feature_importance
    })
}
```

## Regression

```tusk
# Price prediction model
price_model: @learn.train("regression", {
    data: @query("SELECT * FROM products WHERE price IS NOT NULL")
    features: [
        "category_id",
        "brand_popularity",
        "features_count",
        "weight",
        "competitor_avg_price"
    ]
    target: "price"
    algorithm: "gradient_boosting"
})

# Suggest pricing
#api /products/suggest-price {
    features: @request.post
    
    prediction: @price_model.predict(@features)
    
    @json({
        suggested_price: @round(@prediction.value, 2)
        confidence_interval: {
            low: @round(@prediction.lower_bound, 2)
            high: @round(@prediction.upper_bound, 2)
        }
        similar_products: @find_similar_products(@features)
    })
}
```

## Clustering

```tusk
# Customer segmentation
segmentation_model: @learn.train("clustering", {
    data: @query("
        SELECT 
            customer_id,
            avg_order_value,
            order_frequency,
            total_spent,
            categories_purchased
        FROM customer_analytics
    ")
    features: ["avg_order_value", "order_frequency", "total_spent"]
    algorithm: "kmeans"
    clusters: 5
})

# Assign customer to segment
#api /customers/{id}/segment {
    customer: @get_customer_analytics(@id)
    
    segment: @segmentation_model.predict({
        avg_order_value: @customer.avg_order_value
        order_frequency: @customer.order_frequency
        total_spent: @customer.total_spent
    })
    
    # Get segment characteristics
    segment_info: @segmentation_model.cluster_centers[@segment.cluster]
    
    @json({
        customer_id: @id
        segment: @segment.cluster
        segment_name: @get_segment_name(@segment.cluster)
        characteristics: @segment_info
        marketing_recommendations: @get_marketing_strategy(@segment.cluster)
    })
}
```

## Recommendation Engine

```tusk
# Collaborative filtering
recommendation_model: @learn.train("recommender", {
    interactions: @query("
        SELECT user_id, product_id, rating 
        FROM reviews 
        WHERE rating IS NOT NULL
    ")
    algorithm: "matrix_factorization"
    factors: 50
})

# Get recommendations
#api /users/{id}/recommendations {
    limit: @request.get.limit|10
    
    # Get user's purchase history
    purchased: @query("
        SELECT DISTINCT product_id 
        FROM orders 
        WHERE user_id = ?
    ", [@id])
    
    # Get recommendations
    recommendations: @recommendation_model.recommend(@id, {
        exclude: @purchased
        limit: @limit
    })
    
    # Enhance with product details
    products: @query("
        SELECT * FROM products 
        WHERE id IN (?)
    ", [@recommendations.map(r => r.product_id)])
    
    @json({
        user_id: @id
        recommendations: @products.map((product, index) => {
            score: @recommendations[@index].score
            product: @product
            reason: @get_recommendation_reason(@id, @product.id)
        })
    })
}
```

## Natural Language Processing

```tusk
# Sentiment analysis
sentiment_model: @learn.load("sentiment_analysis")

# Analyze reviews
#api /products/{id}/sentiment {
    reviews: @query("
        SELECT review_text, rating 
        FROM reviews 
        WHERE product_id = ?
    ", [@id])
    
    sentiments: @reviews.map(review => {
        analysis: @sentiment_model.analyze(@review.review_text)
        return {
            text: @review.review_text
            rating: @review.rating
            sentiment: @analysis.sentiment  # positive, negative, neutral
            confidence: @analysis.confidence
            keywords: @analysis.keywords
        }
    })
    
    # Aggregate results
    summary: {
        total_reviews: @count(@reviews)
        positive: @sentiments.filter(s => s.sentiment == "positive").length
        negative: @sentiments.filter(s => s.sentiment == "negative").length
        neutral: @sentiments.filter(s => s.sentiment == "neutral").length
        average_confidence: @average(@sentiments.map(s => s.confidence))
    }
    
    @json({
        product_id: @id
        summary: @summary
        recent_sentiments: @sentiments.slice(0, 5)
    })
}
```

## Time Series Forecasting

```tusk
# Sales forecasting
forecast_model: @learn.train("timeseries", {
    data: @query("
        SELECT date, total_sales 
        FROM daily_sales 
        ORDER BY date
    ")
    algorithm: "prophet"
    seasonality: ["weekly", "yearly"]
})

# Generate forecast
#api /forecast/sales {
    days: @request.get.days|30
    
    forecast: @forecast_model.predict(@days)
    
    @json({
        forecast: @forecast.map(day => {
            date: @day.date
            predicted_sales: @round(@day.value, 2)
            lower_bound: @round(@day.lower, 2)
            upper_bound: @round(@day.upper, 2)
        })
        total_predicted: @sum(@forecast.map(d => d.value))
        trend: @forecast_model.trend
        seasonality: @forecast_model.seasonality_components
    })
}
```

## Anomaly Detection

```tusk
# Fraud detection
fraud_model: @learn.train("anomaly_detection", {
    data: @query("
        SELECT * FROM transactions 
        WHERE fraud_label IS NOT NULL
    ")
    features: [
        "amount",
        "merchant_category",
        "time_since_last_transaction",
        "distance_from_home",
        "unusual_time"
    ]
    algorithm: "isolation_forest"
    contamination: 0.01  # Expected 1% fraud rate
})

# Real-time fraud detection
#api /transactions/check {
    transaction: @request.post
    
    # Enhance with calculated features
    features: {
        ...@transaction,
        time_since_last: @calculate_time_since_last(@transaction.user_id),
        distance_from_home: @calculate_distance(@transaction.location),
        unusual_time: @is_unusual_time(@transaction.timestamp)
    }
    
    result: @fraud_model.predict(@features)
    
    @if(@result.is_anomaly) {
        # Flag for review
        @query("
            INSERT INTO fraud_alerts (transaction_id, score, features)
            VALUES (?, ?, ?)
        ", [@transaction.id, @result.score, @json_encode(@features)])
        
        # Send alert
        @send_fraud_alert(@transaction)
    }
    
    @json({
        transaction_id: @transaction.id
        is_suspicious: @result.is_anomaly
        risk_score: @result.score
        risk_factors: @result.contributing_features
        action: @result.is_anomaly ? "review_required" : "approved"
    })
}
```

## Model Management

```tusk
# Model versioning and deployment
model_registry: {
    # Save model
    save: (name, model, metadata) => {
        version: @generate_version()
        
        @query("
            INSERT INTO ml_models (name, version, model_data, metadata)
            VALUES (?, ?, ?, ?)
        ", [@name, @version, @serialize(@model), @json_encode(@metadata)])
        
        return @version
    }
    
    # Load specific version
    load: (name, version: null) => {
        @if(@version) {
            model_data: @query("
                SELECT model_data FROM ml_models 
                WHERE name = ? AND version = ?
            ", [@name, @version])
        } else {
            # Load latest
            model_data: @query("
                SELECT model_data FROM ml_models 
                WHERE name = ? 
                ORDER BY created_at DESC 
                LIMIT 1
            ", [@name])
        }
        
        return @deserialize(@model_data.model_data)
    }
    
    # A/B testing models
    ab_test: (name, traffic_split: 0.5) => {
        models: @query("
            SELECT version FROM ml_models 
            WHERE name = ? AND active = 1 
            ORDER BY created_at DESC 
            LIMIT 2
        ", [@name])
        
        # Randomly assign model based on traffic split
        use_new_model: @random() < @traffic_split
        
        return @use_new_model ? @models[0] : @models[1]
    }
}
```

## AutoML

```tusk
# Automatic model selection and tuning
automl: @learn.auto({
    data: @training_data
    target: "conversion"
    task: "classification"
    time_budget: 300  # 5 minutes
    metric: "auc"
})

# Best model is automatically selected
best_model: @automl.best_model
results: @automl.leaderboard

# Deploy best model
@model_registry.save("conversion_predictor", @best_model, {
    performance: @results[0],
    features: @automl.feature_importance,
    training_date: @timestamp
})
```

## Real-time Learning

```tusk
# Online learning for personalization
personalization_model: @learn.online("contextual_bandit")

# Learn from user interactions
#api /content/interact {
    user_id: @request.post.user_id
    content_id: @request.post.content_id
    action: @request.post.action  # click, ignore, share, etc.
    
    # Update model with feedback
    reward: @calculate_reward(@action)
    @personalization_model.update(@user_id, @content_id, @reward)
    
    # Store for batch retraining
    @query("
        INSERT INTO interactions (user_id, content_id, action, reward)
        VALUES (?, ?, ?, ?)
    ", [@user_id, @content_id, @action, @reward])
}

# Get personalized content
#api /content/personalized/{user_id} {
    available_content: @get_available_content()
    
    # Get personalized ranking
    rankings: @personalization_model.rank(@user_id, @available_content)
    
    @json({
        user_id: @user_id
        recommendations: @rankings.slice(0, 10)
        exploration_rate: @personalization_model.exploration_rate
    })
}
```

## Best Practices

1. **Data quality** - Clean and preprocess data properly
2. **Feature engineering** - Create meaningful features
3. **Model validation** - Use proper train/test splits
4. **Monitor performance** - Track model accuracy over time
5. **Retrain regularly** - Keep models up to date
6. **Explain predictions** - Provide interpretability

## Related Features

- `@analyze()` - Data analysis
- `@optimize()` - Optimization algorithms
- `@metrics` - Performance tracking
- `@experiment()` - A/B testing
- `@pipeline()` - Data pipelines