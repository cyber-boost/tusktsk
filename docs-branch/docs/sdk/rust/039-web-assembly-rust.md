# WebAssembly with TuskLang and Rust

## 🌐 WASM Foundation

WebAssembly (WASM) with TuskLang and Rust enables high-performance web applications by compiling Rust code to run in browsers. This guide covers WASM compilation, browser integration, and performance optimization.

## 🏗️ WASM Architecture

### Compilation Target

```rust
[wasm_architecture]
target_triple: "wasm32-unknown-unknown"
wasm_bindgen: true
js_interop: true
performance: true

[wasm_components]
wasm_module: "compiled_rust"
javascript_glue: "wasm_bindgen"
browser_runtime: "web_apis"
```

### Project Configuration

```toml
# Cargo.toml
[package]
name = "tusklang-wasm"
version = "0.1.0"
edition = "2021"

[lib]
crate-type = ["cdylib"]

[dependencies]
wasm-bindgen = "0.2"
js-sys = "0.3"
web-sys = { version = "0.3", features = ["console"] }
serde = { version = "1.0", features = ["derive"] }
serde-wasm-bindgen = "0.6"

[dev-dependencies]
wasm-bindgen-test = "0.3"
```

## 🔧 WASM Compilation

### Basic WASM Module

```rust
[wasm_module]
wasm_bindgen: true
exported_functions: true
memory_management: true

[wasm_implementation]
use wasm_bindgen::prelude::*;
use serde::{Deserialize, Serialize};

// Basic WASM function
#[wasm_bindgen]
pub fn add(a: i32, b: i32) -> i32 {
    a + b
}

// String handling
#[wasm_bindgen]
pub fn greet(name: &str) -> String {
    format!("Hello, {}!", name)
}

// Array operations
#[wasm_bindgen]
pub fn sum_array(numbers: &[i32]) -> i32 {
    numbers.iter().sum()
}

// Return arrays to JavaScript
#[wasm_bindgen]
pub fn create_array(size: usize) -> Vec<i32> {
    (0..size).collect()
}

// Complex data structures
#[derive(Serialize, Deserialize)]
pub struct Point {
    x: f64,
    y: f64,
}

#[wasm_bindgen]
pub struct Geometry {
    points: Vec<Point>,
}

#[wasm_bindgen]
impl Geometry {
    pub fn new() -> Self {
        Self {
            points: Vec::new(),
        }
    }
    
    pub fn add_point(&mut self, x: f64, y: f64) {
        self.points.push(Point { x, y });
    }
    
    pub fn get_points(&self) -> JsValue {
        serde_wasm_bindgen::to_value(&self.points).unwrap()
    }
    
    pub fn calculate_centroid(&self) -> JsValue {
        if self.points.is_empty() {
            return serde_wasm_bindgen::to_value(&Point { x: 0.0, y: 0.0 }).unwrap();
        }
        
        let sum_x: f64 = self.points.iter().map(|p| p.x).sum();
        let sum_y: f64 = self.points.iter().map(|p| p.y).sum();
        let count = self.points.len() as f64;
        
        let centroid = Point {
            x: sum_x / count,
            y: sum_y / count,
        };
        
        serde_wasm_bindgen::to_value(&centroid).unwrap()
    }
}

// Error handling
#[wasm_bindgen]
pub fn divide(a: f64, b: f64) -> Result<f64, JsValue> {
    if b == 0.0 {
        Err(JsValue::from_str("Division by zero"))
    } else {
        Ok(a / b)
    }
}

// Async operations
#[wasm_bindgen]
pub async fn async_operation(input: String) -> Result<String, JsValue> {
    // Simulate async work
    let result = format!("Processed: {}", input);
    Ok(result)
}
```

### Memory Management

```rust
[memory_management]
wasm_memory: true
allocation_strategies: true
garbage_collection: true

[memory_implementation]
use wasm_bindgen::prelude::*;
use std::alloc::{alloc, dealloc, Layout};

// Custom memory allocator for WASM
pub struct WasmAllocator;

impl WasmAllocator {
    pub fn allocate(size: usize) -> *mut u8 {
        let layout = Layout::from_size_align(size, 8).unwrap();
        unsafe { alloc(layout) }
    }
    
    pub fn deallocate(ptr: *mut u8, size: usize) {
        let layout = Layout::from_size_align(size, 8).unwrap();
        unsafe { dealloc(ptr, layout) }
    }
}

// Memory-efficient data structures
#[wasm_bindgen]
pub struct EfficientBuffer {
    data: Vec<u8>,
    capacity: usize,
}

#[wasm_bindgen]
impl EfficientBuffer {
    pub fn new(capacity: usize) -> Self {
        Self {
            data: Vec::with_capacity(capacity),
            capacity,
        }
    }
    
    pub fn write(&mut self, bytes: &[u8]) -> Result<(), JsValue> {
        if self.data.len() + bytes.len() > self.capacity {
            return Err(JsValue::from_str("Buffer overflow"));
        }
        
        self.data.extend_from_slice(bytes);
        Ok(())
    }
    
    pub fn read(&self, offset: usize, length: usize) -> Result<Vec<u8>, JsValue> {
        if offset + length > self.data.len() {
            return Err(JsValue::from_str("Read out of bounds"));
        }
        
        Ok(self.data[offset..offset + length].to_vec())
    }
    
    pub fn clear(&mut self) {
        self.data.clear();
    }
    
    pub fn size(&self) -> usize {
        self.data.len()
    }
    
    pub fn capacity(&self) -> usize {
        self.capacity
    }
}

// Memory pool for frequent allocations
#[wasm_bindgen]
pub struct MemoryPool {
    chunks: Vec<Vec<u8>>,
    chunk_size: usize,
}

#[wasm_bindgen]
impl MemoryPool {
    pub fn new(chunk_size: usize) -> Self {
        Self {
            chunks: Vec::new(),
            chunk_size,
        }
    }
    
    pub fn allocate(&mut self, size: usize) -> Result<Vec<u8>, JsValue> {
        if size > self.chunk_size {
            return Err(JsValue::from_str("Requested size too large"));
        }
        
        let chunk = vec![0u8; self.chunk_size];
        self.chunks.push(chunk);
        
        Ok(vec![0u8; size])
    }
    
    pub fn get_chunk_count(&self) -> usize {
        self.chunks.len()
    }
    
    pub fn clear(&mut self) {
        self.chunks.clear();
    }
}
```

## 🌐 Browser Integration

### JavaScript Interop

```rust
[javascript_interop]
js_apis: true
dom_manipulation: true
event_handling: true

[js_interop_implementation]
use wasm_bindgen::prelude::*;
use web_sys::{console, Document, Element, HtmlElement, Window};

// Console logging
#[wasm_bindgen]
pub fn log_message(message: &str) {
    console::log_1(&JsValue::from_str(message));
}

#[wasm_bindgen]
pub fn log_number(number: f64) {
    console::log_1(&JsValue::from_f64(number));
}

// DOM manipulation
#[wasm_bindgen]
pub fn create_element(tag_name: &str) -> Result<Element, JsValue> {
    let window = web_sys::window().unwrap();
    let document = window.document().unwrap();
    document.create_element(tag_name)
}

#[wasm_bindgen]
pub fn set_element_text(element: &Element, text: &str) {
    element.set_text_content(Some(text));
}

#[wasm_bindgen]
pub fn add_element_class(element: &Element, class_name: &str) {
    element.class_list().add_1(class_name).unwrap();
}

#[wasm_bindgen]
pub fn remove_element_class(element: &Element, class_name: &str) {
    element.class_list().remove_1(class_name).unwrap();
}

// Canvas operations
#[wasm_bindgen]
pub fn draw_circle(
    canvas: &web_sys::HtmlCanvasElement,
    x: f64,
    y: f64,
    radius: f64,
    color: &str,
) -> Result<(), JsValue> {
    let context = canvas
        .get_context("2d")?
        .unwrap()
        .dyn_into::<web_sys::CanvasRenderingContext2d>()?;
    
    context.begin_path();
    context.arc(x, y, radius, 0.0, 2.0 * std::f64::consts::PI)?;
    context.set_fill_style(&JsValue::from_str(color));
    context.fill();
    
    Ok(())
}

#[wasm_bindgen]
pub fn draw_line(
    canvas: &web_sys::HtmlCanvasElement,
    x1: f64,
    y1: f64,
    x2: f64,
    y2: f64,
    color: &str,
    width: f64,
) -> Result<(), JsValue> {
    let context = canvas
        .get_context("2d")?
        .unwrap()
        .dyn_into::<web_sys::CanvasRenderingContext2d>()?;
    
    context.begin_path();
    context.move_to(x1, y1);
    context.line_to(x2, y2);
    context.set_stroke_style(&JsValue::from_str(color));
    context.set_line_width(width);
    context.stroke();
    
    Ok(())
}

// Event handling
#[wasm_bindgen]
pub fn add_click_listener(
    element: &Element,
    callback: js_sys::Function,
) -> Result<(), JsValue> {
    let closure = Closure::wrap(Box::new(move || {
        let _ = callback.call0(&JsValue::NULL);
    }) as Box<dyn FnMut()>);
    
    element.add_event_listener_with_callback("click", closure.as_ref().unchecked_ref())?;
    closure.forget(); // Prevent closure from being dropped
    
    Ok(())
}

// Local storage
#[wasm_bindgen]
pub fn set_local_storage(key: &str, value: &str) -> Result<(), JsValue> {
    let window = web_sys::window().unwrap();
    let storage = window.local_storage()?.unwrap();
    storage.set_item(key, value)
}

#[wasm_bindgen]
pub fn get_local_storage(key: &str) -> Result<Option<String>, JsValue> {
    let window = web_sys::window().unwrap();
    let storage = window.local_storage()?.unwrap();
    Ok(storage.get_item(key)?)
}

// Fetch API
#[wasm_bindgen]
pub async fn fetch_data(url: &str) -> Result<JsValue, JsValue> {
    let window = web_sys::window().unwrap();
    let response = window.fetch_with_str(url).await?;
    let response: web_sys::Response = response.dyn_into()?;
    response.json().await
}
```

### Performance Optimization

```rust
[performance_optimization]
wasm_optimization: true
memory_efficiency: true
algorithm_optimization: true

[performance_implementation]
use wasm_bindgen::prelude::*;
use std::collections::HashMap;

// Optimized mathematical operations
#[wasm_bindgen]
pub fn fast_fibonacci(n: u32) -> u64 {
    if n <= 1 {
        return n as u64;
    }
    
    let mut a = 0u64;
    let mut b = 1u64;
    
    for _ in 2..=n {
        let temp = a + b;
        a = b;
        b = temp;
    }
    
    b
}

// Optimized sorting
#[wasm_bindgen]
pub fn sort_array(mut numbers: Vec<i32>) -> Vec<i32> {
    numbers.sort_unstable(); // Faster than sort() for integers
    numbers
}

// Optimized string processing
#[wasm_bindgen]
pub fn process_text(text: &str) -> String {
    let mut result = String::with_capacity(text.len());
    
    for ch in text.chars() {
        if ch.is_ascii_alphabetic() {
            result.push(ch.to_ascii_uppercase());
        } else if ch.is_ascii_digit() {
            result.push(ch);
        }
    }
    
    result
}

// Optimized data structures
#[wasm_bindgen]
pub struct OptimizedCache {
    data: HashMap<String, String>,
    max_size: usize,
}

#[wasm_bindgen]
impl OptimizedCache {
    pub fn new(max_size: usize) -> Self {
        Self {
            data: HashMap::with_capacity(max_size),
            max_size,
        }
    }
    
    pub fn get(&self, key: &str) -> Option<String> {
        self.data.get(key).cloned()
    }
    
    pub fn set(&mut self, key: String, value: String) {
        if self.data.len() >= self.max_size {
            // Simple LRU: remove first entry
            if let Some(first_key) = self.data.keys().next().cloned() {
                self.data.remove(&first_key);
            }
        }
        
        self.data.insert(key, value);
    }
    
    pub fn clear(&mut self) {
        self.data.clear();
    }
    
    pub fn size(&self) -> usize {
        self.data.len()
    }
}

// SIMD operations (when available)
#[wasm_bindgen]
pub fn vector_add(a: &[f32], b: &[f32]) -> Vec<f32> {
    let len = a.len().min(b.len());
    let mut result = Vec::with_capacity(len);
    
    for i in 0..len {
        result.push(a[i] + b[i]);
    }
    
    result
}

// Memory-efficient image processing
#[wasm_bindgen]
pub fn process_image_data(
    data: &[u8],
    width: usize,
    height: usize,
    operation: &str,
) -> Vec<u8> {
    let mut result = Vec::with_capacity(data.len());
    
    match operation {
        "invert" => {
            for &pixel in data {
                result.push(255 - pixel);
            }
        }
        "brighten" => {
            for &pixel in data {
                result.push((pixel as u16 + 50).min(255) as u8);
            }
        }
        "darken" => {
            for &pixel in data {
                result.push((pixel as i16 - 50).max(0) as u8);
            }
        }
        _ => {
            result.extend_from_slice(data);
        }
    }
    
    result
}
```

## 🔧 Build and Deployment

### Build Configuration

```rust
[build_configuration]
wasm_pack: true
optimization: true
bundling: true

[build_implementation]
// wasm-pack.toml
[package]
name = "tusklang-wasm"
version = "0.1.0"

[lib]
crate-type = ["cdylib"]

[profile.release]
opt-level = 3
lto = true
codegen-units = 1

// Build script
use std::process::Command;

fn main() {
    // Build WASM
    let status = Command::new("wasm-pack")
        .args(&["build", "--target", "web", "--release"])
        .status()
        .expect("Failed to build WASM");
    
    if !status.success() {
        panic!("WASM build failed");
    }
    
    println!("cargo:rerun-if-changed=src/lib.rs");
}

// JavaScript glue code
#[wasm_bindgen]
pub fn init_panic_hook() {
    console_error_panic_hook::set_once();
}

// Module initialization
#[wasm_bindgen(start)]
pub fn start() -> Result<(), JsValue> {
    init_panic_hook();
    console::log_1(&JsValue::from_str("TuskLang WASM module loaded"));
    Ok(())
}
```

### Web Integration

```html
<!-- index.html -->
<!DOCTYPE html>
<html>
<head>
    <title>TuskLang WASM Demo</title>
</head>
<body>
    <h1>TuskLang WASM Demo</h1>
    
    <div>
        <label for="input1">Number 1:</label>
        <input type="number" id="input1" value="5">
        
        <label for="input2">Number 2:</label>
        <input type="number" id="input2" value="3">
        
        <button onclick="calculate()">Add Numbers</button>
        
        <p>Result: <span id="result">-</span></p>
    </div>
    
    <div>
        <canvas id="canvas" width="400" height="300"></canvas>
        <button onclick="drawShapes()">Draw Shapes</button>
    </div>
    
    <script type="module">
        import init, { add, draw_circle, draw_line } from './pkg/tusklang_wasm.js';
        
        let wasmModule;
        
        async function loadWasm() {
            wasmModule = await init();
            console.log('WASM module loaded');
        }
        
        window.calculate = function() {
            const a = parseInt(document.getElementById('input1').value);
            const b = parseInt(document.getElementById('input2').value);
            const result = add(a, b);
            document.getElementById('result').textContent = result;
        };
        
        window.drawShapes = function() {
            const canvas = document.getElementById('canvas');
            
            // Clear canvas
            const ctx = canvas.getContext('2d');
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            
            // Draw shapes using WASM
            draw_circle(canvas, 100, 100, 50, 'red');
            draw_circle(canvas, 200, 150, 30, 'blue');
            draw_line(canvas, 50, 50, 350, 250, 'green', 3);
        };
        
        // Load WASM module on page load
        loadWasm();
    </script>
</body>
</html>
```

## 🎯 Best Practices

### 1. **Performance Optimization**
- Use `wasm-pack` with release mode
- Minimize memory allocations
- Use efficient algorithms
- Profile WASM performance

### 2. **Memory Management**
- Reuse objects when possible
- Use appropriate data structures
- Monitor memory usage
- Implement proper cleanup

### 3. **JavaScript Integration**
- Use `wasm-bindgen` for type safety
- Handle errors properly
- Use async/await for I/O
- Minimize data copying

### 4. **Build and Deployment**
- Optimize WASM size
- Use proper bundling
- Implement caching strategies
- Test across browsers

### 5. **Debugging and Testing**
- Use browser dev tools
- Implement logging
- Test performance
- Validate functionality

WebAssembly with TuskLang and Rust provides high-performance web applications with the safety and reliability of Rust while maintaining seamless integration with web technologies. 