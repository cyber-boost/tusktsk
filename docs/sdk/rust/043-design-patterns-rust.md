# Design Patterns in TuskLang with Rust

## 🏗️ Design Pattern Foundation

Design patterns with TuskLang and Rust provide proven solutions to common software design problems. This guide covers creational, structural, and behavioral patterns with practical implementations and TuskLang integration.

## 🏗️ Pattern Architecture

### Pattern Categories

```rust
[pattern_architecture]
creational_patterns: true
structural_patterns: true
behavioral_patterns: true
rust_specific_patterns: true

[pattern_components]
singleton: "single_instance"
factory: "object_creation"
observer: "event_notification"
strategy: "algorithm_selection"
```

### Pattern Configuration

```rust
[pattern_configuration]
pattern_registry: true
pattern_factory: true
pattern_validation: true
pattern_metrics: true

[pattern_implementation]
use std::collections::HashMap;
use std::sync::Arc;
use tokio::sync::RwLock;

// Pattern registry
pub struct PatternRegistry {
    patterns: Arc<RwLock<HashMap<String, Box<dyn Pattern + Send + Sync>>>>,
}

pub trait Pattern {
    fn name(&self) -> &str;
    fn description(&self) -> &str;
    fn execute(&self, context: &PatternContext) -> Result<PatternResult, PatternError>;
}

#[derive(Debug, Clone)]
pub struct PatternContext {
    pub data: HashMap<String, String>,
    pub config: PatternConfig,
}

#[derive(Debug, Clone)]
pub struct PatternConfig {
    pub timeout: std::time::Duration,
    pub retry_count: usize,
    pub enable_logging: bool,
}

#[derive(Debug)]
pub struct PatternResult {
    pub success: bool,
    pub data: HashMap<String, String>,
    pub metrics: PatternMetrics,
}

#[derive(Debug)]
pub struct PatternMetrics {
    pub execution_time: std::time::Duration,
    pub memory_usage: usize,
    pub error_count: usize,
}

#[derive(Debug, thiserror::Error)]
pub enum PatternError {
    #[error("Pattern execution failed: {message}")]
    Execution { message: String },
    #[error("Pattern not found: {name}")]
    NotFound { name: String },
    #[error("Invalid configuration: {message}")]
    Configuration { message: String },
}

impl PatternRegistry {
    pub fn new() -> Self {
        Self {
            patterns: Arc::new(RwLock::new(HashMap::new())),
        }
    }
    
    pub async fn register_pattern(&self, pattern: Box<dyn Pattern + Send + Sync>) {
        let mut patterns = self.patterns.write().await;
        patterns.insert(pattern.name().to_string(), pattern);
    }
    
    pub async fn get_pattern(&self, name: &str) -> Option<Box<dyn Pattern + Send + Sync>> {
        let patterns = self.patterns.read().await;
        patterns.get(name).cloned()
    }
    
    pub async fn execute_pattern(&self, name: &str, context: PatternContext) -> Result<PatternResult, PatternError> {
        let pattern = self.get_pattern(name).await
            .ok_or_else(|| PatternError::NotFound { name: name.to_string() })?;
        
        pattern.execute(&context)
    }
}
```

## 🔧 Creational Patterns

### Singleton Pattern

```rust
[creational_patterns]
singleton: true
factory: true
builder: true
prototype: true

[creational_implementation]
use std::sync::Once;
use std::sync::Mutex;

// Thread-safe singleton
pub struct Singleton {
    data: String,
}

impl Singleton {
    static INSTANCE: Once = Once::new();
    static mut INSTANCE_PTR: Option<Mutex<Singleton>> = None;
    
    pub fn instance() -> &'static Mutex<Singleton> {
        unsafe {
            INSTANCE.call_once(|| {
                INSTANCE_PTR = Some(Mutex::new(Singleton {
                    data: "Singleton data".to_string(),
                }));
            });
            INSTANCE_PTR.as_ref().unwrap()
        }
    }
    
    pub fn get_data(&self) -> &str {
        &self.data
    }
    
    pub fn set_data(&mut self, data: String) {
        self.data = data;
    }
}

// Singleton with TuskLang configuration
pub struct ConfigurableSingleton {
    config: HashMap<String, String>,
    data: String,
}

impl ConfigurableSingleton {
    static INSTANCE: Once = Once::new();
    static mut INSTANCE_PTR: Option<Mutex<ConfigurableSingleton>> = None;
    
    pub fn instance() -> &'static Mutex<ConfigurableSingleton> {
        unsafe {
            INSTANCE.call_once(|| {
                // Load configuration from TuskLang
                let config = Self::load_config();
                INSTANCE_PTR = Some(Mutex::new(ConfigurableSingleton {
                    config,
                    data: "Default data".to_string(),
                }));
            });
            INSTANCE_PTR.as_ref().unwrap()
        }
    }
    
    fn load_config() -> HashMap<String, String> {
        let mut config = HashMap::new();
        config.insert("database_url".to_string(), "postgresql://localhost/db".to_string());
        config.insert("api_key".to_string(), "secret_key".to_string());
        config.insert("timeout".to_string(), "30".to_string());
        config
    }
    
    pub fn get_config(&self, key: &str) -> Option<&String> {
        self.config.get(key)
    }
    
    pub fn get_data(&self) -> &str {
        &self.data
    }
    
    pub fn set_data(&mut self, data: String) {
        self.data = data;
    }
}
```

### Factory Pattern

```rust
[factory_pattern]
factory_method: true
abstract_factory: true
object_creation: true

[factory_implementation]
use std::collections::HashMap;

// Product trait
pub trait Product {
    fn name(&self) -> &str;
    fn price(&self) -> f64;
    fn description(&self) -> &str;
}

// Concrete products
pub struct Book {
    title: String,
    author: String,
    price: f64,
}

impl Product for Book {
    fn name(&self) -> &str {
        &self.title
    }
    
    fn price(&self) -> f64 {
        self.price
    }
    
    fn description(&self) -> &str {
        &format!("Book: {} by {}", self.title, self.author)
    }
}

pub struct Electronics {
    name: String,
    brand: String,
    price: f64,
}

impl Product for Electronics {
    fn name(&self) -> &str {
        &self.name
    }
    
    fn price(&self) -> f64 {
        self.price
    }
    
    fn description(&self) -> &str {
        &format!("Electronics: {} by {}", self.name, self.brand)
    }
}

// Factory trait
pub trait ProductFactory {
    fn create_product(&self, product_type: &str, config: HashMap<String, String>) -> Box<dyn Product>;
}

// Concrete factory
pub struct BookFactory;

impl ProductFactory for BookFactory {
    fn create_product(&self, product_type: &str, config: HashMap<String, String>) -> Box<dyn Product> {
        match product_type {
            "fiction" => Box::new(Book {
                title: config.get("title").unwrap_or(&"Unknown".to_string()).clone(),
                author: config.get("author").unwrap_or(&"Unknown".to_string()).clone(),
                price: config.get("price").unwrap_or(&"0.0".to_string()).parse().unwrap_or(0.0),
            }),
            "non_fiction" => Box::new(Book {
                title: config.get("title").unwrap_or(&"Unknown".to_string()).clone(),
                author: config.get("author").unwrap_or(&"Unknown".to_string()).clone(),
                price: config.get("price").unwrap_or(&"0.0".to_string()).parse().unwrap_or(0.0),
            }),
            _ => panic!("Unknown book type: {}", product_type),
        }
    }
}

pub struct ElectronicsFactory;

impl ProductFactory for ElectronicsFactory {
    fn create_product(&self, product_type: &str, config: HashMap<String, String>) -> Box<dyn Product> {
        match product_type {
            "phone" => Box::new(Electronics {
                name: config.get("name").unwrap_or(&"Unknown".to_string()).clone(),
                brand: config.get("brand").unwrap_or(&"Unknown".to_string()).clone(),
                price: config.get("price").unwrap_or(&"0.0".to_string()).parse().unwrap_or(0.0),
            }),
            "laptop" => Box::new(Electronics {
                name: config.get("name").unwrap_or(&"Unknown".to_string()).clone(),
                brand: config.get("brand").unwrap_or(&"Unknown".to_string()).clone(),
                price: config.get("price").unwrap_or(&"0.0".to_string()).parse().unwrap_or(0.0),
            }),
            _ => panic!("Unknown electronics type: {}", product_type),
        }
    }
}

// Factory with TuskLang configuration
pub struct TuskLangProductFactory {
    factories: HashMap<String, Box<dyn ProductFactory + Send + Sync>>,
}

impl TuskLangProductFactory {
    pub fn new() -> Self {
        let mut factories = HashMap::new();
        factories.insert("book".to_string(), Box::new(BookFactory));
        factories.insert("electronics".to_string(), Box::new(ElectronicsFactory));
        
        Self { factories }
    }
    
    pub fn create_product(&self, category: &str, product_type: &str, config: HashMap<String, String>) -> Option<Box<dyn Product>> {
        self.factories.get(category)
            .map(|factory| factory.create_product(product_type, config))
    }
    
    pub fn load_config_from_tusk(&self, config_path: &str) -> HashMap<String, String> {
        // Load configuration from TuskLang file
        let mut config = HashMap::new();
        config.insert("title".to_string(), "Default Title".to_string());
        config.insert("author".to_string(), "Default Author".to_string());
        config.insert("price".to_string(), "0.0".to_string());
        config
    }
}
```

### Builder Pattern

```rust
[builder_pattern]
fluent_interface: true
step_by_step: true
validation: true

[builder_implementation]
// Product to build
pub struct User {
    name: String,
    email: String,
    age: Option<u32>,
    address: Option<String>,
    phone: Option<String>,
}

impl User {
    pub fn new(name: String, email: String) -> Self {
        Self {
            name,
            email,
            age: None,
            address: None,
            phone: None,
        }
    }
    
    pub fn name(&self) -> &str {
        &self.name
    }
    
    pub fn email(&self) -> &str {
        &self.email
    }
    
    pub fn age(&self) -> Option<u32> {
        self.age
    }
    
    pub fn address(&self) -> Option<&str> {
        self.address.as_deref()
    }
    
    pub fn phone(&self) -> Option<&str> {
        self.phone.as_deref()
    }
}

// Builder
pub struct UserBuilder {
    name: Option<String>,
    email: Option<String>,
    age: Option<u32>,
    address: Option<String>,
    phone: Option<String>,
}

impl UserBuilder {
    pub fn new() -> Self {
        Self {
            name: None,
            email: None,
            age: None,
            address: None,
            phone: None,
        }
    }
    
    pub fn name(mut self, name: String) -> Self {
        self.name = Some(name);
        self
    }
    
    pub fn email(mut self, email: String) -> Self {
        self.email = Some(email);
        self
    }
    
    pub fn age(mut self, age: u32) -> Self {
        self.age = Some(age);
        self
    }
    
    pub fn address(mut self, address: String) -> Self {
        self.address = Some(address);
        self
    }
    
    pub fn phone(mut self, phone: String) -> Self {
        self.phone = Some(phone);
        self
    }
    
    pub fn build(self) -> Result<User, String> {
        let name = self.name.ok_or("Name is required")?;
        let email = self.email.ok_or("Email is required")?;
        
        // Validate email format
        if !email.contains('@') {
            return Err("Invalid email format".to_string());
        }
        
        // Validate age if provided
        if let Some(age) = self.age {
            if age > 150 {
                return Err("Invalid age".to_string());
            }
        }
        
        Ok(User {
            name,
            email,
            age: self.age,
            address: self.address,
            phone: self.phone,
        })
    }
}

// Builder with TuskLang configuration
pub struct TuskLangUserBuilder {
    config: HashMap<String, String>,
}

impl TuskLangUserBuilder {
    pub fn new(config_path: &str) -> Self {
        let config = Self::load_config(config_path);
        Self { config }
    }
    
    fn load_config(config_path: &str) -> HashMap<String, String> {
        // Load configuration from TuskLang file
        let mut config = HashMap::new();
        config.insert("default_age".to_string(), "25".to_string());
        config.insert("default_address".to_string(), "Unknown".to_string());
        config.insert("max_age".to_string(), "150".to_string());
        config
    }
    
    pub fn build_from_config(&self, name: String, email: String) -> Result<User, String> {
        let mut builder = UserBuilder::new()
            .name(name)
            .email(email);
        
        // Apply default values from configuration
        if let Some(default_age) = self.config.get("default_age") {
            if let Ok(age) = default_age.parse::<u32>() {
                builder = builder.age(age);
            }
        }
        
        if let Some(default_address) = self.config.get("default_address") {
            builder = builder.address(default_address.clone());
        }
        
        builder.build()
    }
}
```

## 🏗️ Structural Patterns

### Adapter Pattern

```rust
[structural_patterns]
adapter: true
bridge: true
composite: true
decorator: true

[structural_implementation]
// Target interface
pub trait PaymentProcessor {
    fn process_payment(&self, amount: f64, currency: &str) -> Result<PaymentResult, PaymentError>;
}

#[derive(Debug)]
pub struct PaymentResult {
    pub transaction_id: String,
    pub status: PaymentStatus,
    pub amount: f64,
    pub currency: String,
}

#[derive(Debug)]
pub enum PaymentStatus {
    Success,
    Failed,
    Pending,
}

#[derive(Debug, thiserror::Error)]
pub enum PaymentError {
    #[error("Payment failed: {message}")]
    PaymentFailed { message: String },
    #[error("Invalid amount: {amount}")]
    InvalidAmount { amount: f64 },
    #[error("Unsupported currency: {currency}")]
    UnsupportedCurrency { currency: String },
}

// Legacy payment system
pub struct LegacyPaymentSystem {
    api_key: String,
}

impl LegacyPaymentSystem {
    pub fn new(api_key: String) -> Self {
        Self { api_key }
    }
    
    pub fn charge(&self, amount: f64, currency: &str) -> LegacyPaymentResult {
        // Simulate legacy payment processing
        if amount <= 0.0 {
            return LegacyPaymentResult::Failed("Invalid amount".to_string());
        }
        
        if currency != "USD" && currency != "EUR" {
            return LegacyPaymentResult::Failed("Unsupported currency".to_string());
        }
        
        LegacyPaymentResult::Success {
            transaction_id: format!("legacy_{}", uuid::Uuid::new_v4()),
            amount,
            currency: currency.to_string(),
        }
    }
}

#[derive(Debug)]
pub enum LegacyPaymentResult {
    Success {
        transaction_id: String,
        amount: f64,
        currency: String,
    },
    Failed(String),
}

// Adapter
pub struct LegacyPaymentAdapter {
    legacy_system: LegacyPaymentSystem,
}

impl LegacyPaymentAdapter {
    pub fn new(api_key: String) -> Self {
        Self {
            legacy_system: LegacyPaymentSystem::new(api_key),
        }
    }
}

impl PaymentProcessor for LegacyPaymentAdapter {
    fn process_payment(&self, amount: f64, currency: &str) -> Result<PaymentResult, PaymentError> {
        let result = self.legacy_system.charge(amount, currency);
        
        match result {
            LegacyPaymentResult::Success { transaction_id, amount, currency } => {
                Ok(PaymentResult {
                    transaction_id,
                    status: PaymentStatus::Success,
                    amount,
                    currency,
                })
            }
            LegacyPaymentResult::Failed(message) => {
                Err(PaymentError::PaymentFailed { message })
            }
        }
    }
}

// Modern payment system
pub struct ModernPaymentSystem {
    api_key: String,
    config: HashMap<String, String>,
}

impl ModernPaymentSystem {
    pub fn new(api_key: String) -> Self {
        let mut config = HashMap::new();
        config.insert("supported_currencies".to_string(), "USD,EUR,GBP,JPY".to_string());
        config.insert("max_amount".to_string(), "10000.0".to_string());
        
        Self { api_key, config }
    }
    
    pub fn process(&self, amount: f64, currency: &str) -> Result<PaymentResult, PaymentError> {
        // Validate amount
        if let Some(max_amount) = self.config.get("max_amount") {
            if let Ok(max) = max_amount.parse::<f64>() {
                if amount > max {
                    return Err(PaymentError::InvalidAmount { amount });
                }
            }
        }
        
        // Validate currency
        if let Some(supported) = self.config.get("supported_currencies") {
            if !supported.split(',').any(|c| c == currency) {
                return Err(PaymentError::UnsupportedCurrency { currency: currency.to_string() });
            }
        }
        
        Ok(PaymentResult {
            transaction_id: format!("modern_{}", uuid::Uuid::new_v4()),
            status: PaymentStatus::Success,
            amount,
            currency: currency.to_string(),
        })
    }
}

impl PaymentProcessor for ModernPaymentSystem {
    fn process_payment(&self, amount: f64, currency: &str) -> Result<PaymentResult, PaymentError> {
        self.process(amount, currency)
    }
}
```

### Decorator Pattern

```rust
[decorator_pattern]
component_wrapping: true
dynamic_behavior: true
composition: true

[decorator_implementation]
// Component trait
pub trait Coffee {
    fn cost(&self) -> f64;
    fn description(&self) -> &str;
}

// Concrete component
pub struct SimpleCoffee;

impl Coffee for SimpleCoffee {
    fn cost(&self) -> f64 {
        2.0
    }
    
    fn description(&self) -> &str {
        "Simple coffee"
    }
}

// Decorator base
pub struct CoffeeDecorator {
    coffee: Box<dyn Coffee>,
}

impl CoffeeDecorator {
    pub fn new(coffee: Box<dyn Coffee>) -> Self {
        Self { coffee }
    }
}

impl Coffee for CoffeeDecorator {
    fn cost(&self) -> f64 {
        self.coffee.cost()
    }
    
    fn description(&self) -> &str {
        self.coffee.description()
    }
}

// Concrete decorators
pub struct MilkDecorator {
    coffee: Box<dyn Coffee>,
}

impl MilkDecorator {
    pub fn new(coffee: Box<dyn Coffee>) -> Self {
        Self { coffee }
    }
}

impl Coffee for MilkDecorator {
    fn cost(&self) -> f64 {
        self.coffee.cost() + 0.5
    }
    
    fn description(&self) -> &str {
        &format!("{} with milk", self.coffee.description())
    }
}

pub struct SugarDecorator {
    coffee: Box<dyn Coffee>,
}

impl SugarDecorator {
    pub fn new(coffee: Box<dyn Coffee>) -> Self {
        Self { coffee }
    }
}

impl Coffee for SugarDecorator {
    fn cost(&self) -> f64 {
        self.coffee.cost() + 0.2
    }
    
    fn description(&self) -> &str {
        &format!("{} with sugar", self.coffee.description())
    }
}

pub struct WhipDecorator {
    coffee: Box<dyn Coffee>,
}

impl WhipDecorator {
    pub fn new(coffee: Box<dyn Coffee>) -> Self {
        Self { coffee }
    }
}

impl Coffee for WhipDecorator {
    fn cost(&self) -> f64 {
        self.coffee.cost() + 0.8
    }
    
    fn description(&self) -> &str {
        &format!("{} with whip", self.coffee.description())
    }
}

// Coffee builder with decorators
pub struct CoffeeBuilder {
    coffee: Box<dyn Coffee>,
}

impl CoffeeBuilder {
    pub fn new() -> Self {
        Self {
            coffee: Box::new(SimpleCoffee),
        }
    }
    
    pub fn add_milk(mut self) -> Self {
        self.coffee = Box::new(MilkDecorator::new(self.coffee));
        self
    }
    
    pub fn add_sugar(mut self) -> Self {
        self.coffee = Box::new(SugarDecorator::new(self.coffee));
        self
    }
    
    pub fn add_whip(mut self) -> Self {
        self.coffee = Box::new(WhipDecorator::new(self.coffee));
        self
    }
    
    pub fn build(self) -> Box<dyn Coffee> {
        self.coffee
    }
}
```

## 🔄 Behavioral Patterns

### Observer Pattern

```rust
[behavioral_patterns]
observer: true
strategy: true
command: true
state: true

[behavioral_implementation]
use std::collections::HashMap;
use tokio::sync::broadcast;

// Observer trait
pub trait Observer {
    fn update(&self, event: &Event);
}

// Event types
#[derive(Debug, Clone)]
pub struct Event {
    pub event_type: String,
    pub data: HashMap<String, String>,
    pub timestamp: std::time::Instant,
}

impl Event {
    pub fn new(event_type: String, data: HashMap<String, String>) -> Self {
        Self {
            event_type,
            data,
            timestamp: std::time::Instant::now(),
        }
    }
}

// Subject trait
pub trait Subject {
    fn attach(&mut self, observer: Box<dyn Observer + Send + Sync>);
    fn detach(&mut self, observer_id: &str);
    fn notify(&self, event: &Event);
}

// Concrete subject
pub struct EventManager {
    observers: HashMap<String, Box<dyn Observer + Send + Sync>>,
    event_sender: broadcast::Sender<Event>,
}

impl EventManager {
    pub fn new() -> Self {
        let (event_sender, _) = broadcast::channel(100);
        Self {
            observers: HashMap::new(),
            event_sender,
        }
    }
    
    pub fn subscribe(&self) -> broadcast::Receiver<Event> {
        self.event_sender.subscribe()
    }
    
    pub fn publish_event(&self, event: Event) {
        let _ = self.event_sender.send(event.clone());
        self.notify(&event);
    }
}

impl Subject for EventManager {
    fn attach(&mut self, observer: Box<dyn Observer + Send + Sync>) {
        let observer_id = uuid::Uuid::new_v4().to_string();
        self.observers.insert(observer_id, observer);
    }
    
    fn detach(&mut self, observer_id: &str) {
        self.observers.remove(observer_id);
    }
    
    fn notify(&self, event: &Event) {
        for observer in self.observers.values() {
            observer.update(event);
        }
    }
}

// Concrete observers
pub struct LoggingObserver {
    name: String,
}

impl LoggingObserver {
    pub fn new(name: String) -> Self {
        Self { name }
    }
}

impl Observer for LoggingObserver {
    fn update(&self, event: &Event) {
        println!("[{}] Event: {:?}", self.name, event);
    }
}

pub struct MetricsObserver {
    metrics: Arc<RwLock<HashMap<String, u64>>>,
}

impl MetricsObserver {
    pub fn new() -> Self {
        Self {
            metrics: Arc::new(RwLock::new(HashMap::new())),
        }
    }
    
    pub async fn get_metrics(&self) -> HashMap<String, u64> {
        self.metrics.read().await.clone()
    }
}

impl Observer for MetricsObserver {
    fn update(&self, event: &Event) {
        let metrics = Arc::clone(&self.metrics);
        tokio::spawn(async move {
            let mut metrics = metrics.write().await;
            let count = metrics.entry(event.event_type.clone()).or_insert(0);
            *count += 1;
        });
    }
}

// Event manager with TuskLang configuration
pub struct TuskLangEventManager {
    event_manager: EventManager,
    config: HashMap<String, String>,
}

impl TuskLangEventManager {
    pub fn new(config_path: &str) -> Self {
        let config = Self::load_config(config_path);
        let mut event_manager = EventManager::new();
        
        // Set up observers based on configuration
        if config.get("enable_logging").map(|v| v == "true").unwrap_or(false) {
            let logger_name = config.get("logger_name").unwrap_or(&"default".to_string()).clone();
            event_manager.attach(Box::new(LoggingObserver::new(logger_name)));
        }
        
        if config.get("enable_metrics").map(|v| v == "true").unwrap_or(false) {
            event_manager.attach(Box::new(MetricsObserver::new()));
        }
        
        Self {
            event_manager,
            config,
        }
    }
    
    fn load_config(config_path: &str) -> HashMap<String, String> {
        let mut config = HashMap::new();
        config.insert("enable_logging".to_string(), "true".to_string());
        config.insert("enable_metrics".to_string(), "true".to_string());
        config.insert("logger_name".to_string(), "tusklang_logger".to_string());
        config.insert("max_events".to_string(), "1000".to_string());
        config
    }
    
    pub fn publish_event(&self, event_type: String, data: HashMap<String, String>) {
        let event = Event::new(event_type, data);
        self.event_manager.publish_event(event);
    }
}
```

### Strategy Pattern

```rust
[strategy_pattern]
algorithm_selection: true
runtime_switching: true
composition: true

[strategy_implementation]
// Strategy trait
pub trait SortingStrategy {
    fn sort(&self, data: &mut [i32]) -> Vec<i32>;
    fn name(&self) -> &str;
}

// Concrete strategies
pub struct BubbleSort;

impl SortingStrategy for BubbleSort {
    fn sort(&self, data: &mut [i32]) -> Vec<i32> {
        let mut result = data.to_vec();
        let len = result.len();
        
        for i in 0..len {
            for j in 0..len - i - 1 {
                if result[j] > result[j + 1] {
                    result.swap(j, j + 1);
                }
            }
        }
        
        result
    }
    
    fn name(&self) -> &str {
        "Bubble Sort"
    }
}

pub struct QuickSort;

impl SortingStrategy for QuickSort {
    fn sort(&self, data: &mut [i32]) -> Vec<i32> {
        let mut result = data.to_vec();
        Self::quick_sort(&mut result);
        result
    }
    
    fn name(&self) -> &str {
        "Quick Sort"
    }
}

impl QuickSort {
    fn quick_sort(data: &mut [i32]) {
        if data.len() <= 1 {
            return;
        }
        
        let pivot = data[data.len() / 2];
        let mut left = Vec::new();
        let mut right = Vec::new();
        let mut equal = Vec::new();
        
        for &item in data.iter() {
            match item.cmp(&pivot) {
                std::cmp::Ordering::Less => left.push(item),
                std::cmp::Ordering::Equal => equal.push(item),
                std::cmp::Ordering::Greater => right.push(item),
            }
        }
        
        Self::quick_sort(&mut left);
        Self::quick_sort(&mut right);
        
        data[..left.len()].copy_from_slice(&left);
        data[left.len()..left.len() + equal.len()].copy_from_slice(&equal);
        data[left.len() + equal.len()..].copy_from_slice(&right);
    }
}

pub struct MergeSort;

impl SortingStrategy for MergeSort {
    fn sort(&self, data: &mut [i32]) -> Vec<i32> {
        let mut result = data.to_vec();
        Self::merge_sort(&mut result);
        result
    }
    
    fn name(&self) -> &str {
        "Merge Sort"
    }
}

impl MergeSort {
    fn merge_sort(data: &mut [i32]) {
        if data.len() <= 1 {
            return;
        }
        
        let mid = data.len() / 2;
        let (left, right) = data.split_at_mut(mid);
        
        Self::merge_sort(left);
        Self::merge_sort(right);
        
        let mut merged = Vec::with_capacity(data.len());
        let mut left_iter = left.iter();
        let mut right_iter = right.iter();
        
        let mut left_next = left_iter.next();
        let mut right_next = right_iter.next();
        
        while let (Some(&l), Some(&r)) = (left_next, right_next) {
            if l <= r {
                merged.push(l);
                left_next = left_iter.next();
            } else {
                merged.push(r);
                right_next = right_iter.next();
            }
        }
        
        // Add remaining elements
        while let Some(&l) = left_next {
            merged.push(l);
            left_next = left_iter.next();
        }
        
        while let Some(&r) = right_next {
            merged.push(r);
            right_next = right_iter.next();
        }
        
        data.copy_from_slice(&merged);
    }
}

// Context
pub struct SortContext {
    strategy: Box<dyn SortingStrategy>,
    config: HashMap<String, String>,
}

impl SortContext {
    pub fn new(strategy: Box<dyn SortingStrategy>) -> Self {
        let mut config = HashMap::new();
        config.insert("enable_logging".to_string(), "true".to_string());
        config.insert("performance_threshold".to_string(), "1000".to_string());
        
        Self { strategy, config }
    }
    
    pub fn set_strategy(&mut self, strategy: Box<dyn SortingStrategy>) {
        self.strategy = strategy;
    }
    
    pub fn execute_sort(&self, data: &mut [i32]) -> Vec<i32> {
        let start = std::time::Instant::now();
        let result = self.strategy.sort(data);
        let duration = start.elapsed();
        
        if self.config.get("enable_logging").map(|v| v == "true").unwrap_or(false) {
            println!("{} completed in {:?} for {} elements", 
                    self.strategy.name(), duration, data.len());
        }
        
        result
    }
    
    pub fn auto_select_strategy(&mut self, data: &[i32]) {
        let threshold: usize = self.config.get("performance_threshold")
            .unwrap_or(&"1000".to_string())
            .parse()
            .unwrap_or(1000);
        
        if data.len() < threshold {
            self.set_strategy(Box::new(BubbleSort));
        } else if data.len() < threshold * 10 {
            self.set_strategy(Box::new(MergeSort));
        } else {
            self.set_strategy(Box::new(QuickSort));
        }
    }
}
```

## 🎯 Best Practices

### 1. **Pattern Selection**
- Choose patterns based on problem requirements
- Consider Rust's ownership and borrowing rules
- Use patterns that enhance code clarity
- Avoid over-engineering with unnecessary patterns

### 2. **Rust-Specific Considerations**
- Leverage Rust's type system for pattern safety
- Use traits for interface definitions
- Consider async patterns for concurrent code
- Implement proper error handling in patterns

### 3. **TuskLang Integration**
- Use TuskLang configuration for pattern parameters
- Implement pattern factories with TuskLang
- Configure pattern behavior through TuskLang files
- Use TuskLang for pattern validation

### 4. **Performance Optimization**
- Profile pattern implementations
- Use appropriate data structures
- Consider memory allocation patterns
- Optimize for Rust's zero-cost abstractions

### 5. **Testing and Maintenance**
- Test pattern implementations thoroughly
- Document pattern usage and configuration
- Maintain pattern consistency across codebase
- Refactor patterns when requirements change

Design patterns with TuskLang and Rust provide powerful abstractions for building maintainable, scalable applications with proper separation of concerns and flexible architecture. 