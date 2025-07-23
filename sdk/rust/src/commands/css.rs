use clap::Subcommand;
use tusktsk::{TuskResult, TuskError};
use std::fs;
use std::path::Path;

#[derive(Subcommand)]
pub enum CssCommand {
    Minify { file: String },
    Format { file: String },
    Validate { file: String },
    Optimize { file: String },
}

pub fn run(cmd: CssCommand) -> TuskResult<()> {
    match cmd {
        CssCommand::Minify { file } => {
            css_minify(&file)?;
            Ok(())
        }
        CssCommand::Format { file } => {
            css_format(&file)?;
            Ok(())
        }
        CssCommand::Validate { file } => {
            css_validate(&file)?;
            Ok(())
        }
        CssCommand::Optimize { file } => {
            css_optimize(&file)?;
            Ok(())
        }
    }
}

/// Minify CSS file
fn css_minify(file: &str) -> TuskResult<()> {
    println!("📦 Minifying CSS file...");
    
    let content = fs::read_to_string(file)
        .map_err(|e| TuskError::parse_error(0, format!("File not found: {}", file)))?;
    
    let minified = minify_css(&content);
    
    // Create output filename
    let input_path = Path::new(file);
    let stem = input_path.file_stem().unwrap_or_default();
    let ext = input_path.extension().unwrap_or_default();
    let output_file = format!("{}.min.{}", stem.to_string_lossy(), ext.to_string_lossy());
    
    // Write minified output
    fs::write(&output_file, minified)
        .map_err(|e| TuskError::parse_error(0, format!("Failed to write minified file: {}", e)))?;
    
    println!("✅ Successfully minified '{}' to '{}'", file, output_file);
    Ok(())
}

/// Format CSS file with proper indentation
fn css_format(file: &str) -> TuskResult<()> {
    println!("🎨 Formatting CSS file...");
    
    let content = fs::read_to_string(file)
        .map_err(|e| TuskError::parse_error(0, format!("File not found: {}", file)))?;
    
    let formatted = format_css(&content);
    
    // Create output filename
    let input_path = Path::new(file);
    let stem = input_path.file_stem().unwrap_or_default();
    let ext = input_path.extension().unwrap_or_default();
    let output_file = format!("{}.formatted.{}", stem.to_string_lossy(), ext.to_string_lossy());
    
    // Write formatted output
    fs::write(&output_file, formatted)
        .map_err(|e| TuskError::parse_error(0, format!("Failed to write formatted file: {}", e)))?;
    
    println!("✅ Successfully formatted '{}' to '{}'", file, output_file);
    Ok(())
}

/// Validate CSS syntax
fn css_validate(file: &str) -> TuskResult<()> {
    println!("🔍 Validating CSS syntax...");
    
    let content = fs::read_to_string(file)
        .map_err(|e| TuskError::parse_error(0, format!("File not found: {}", file)))?;
    
    match validate_css(&content) {
        Ok(_) => {
            println!("✅ CSS file '{}' is valid", file);
            Ok(())
        }
        Err(e) => {
            eprintln!("❌ CSS validation failed: {}", e);
            std::process::exit(1); // General error
        }
    }
}

/// Optimize CSS for performance
fn css_optimize(file: &str) -> TuskResult<()> {
    println!("⚡ Optimizing CSS for performance...");
    
    let content = fs::read_to_string(file)
        .map_err(|e| TuskError::parse_error(0, format!("File not found: {}", file)))?;
    
    let optimized = optimize_css(&content);
    
    // Create output filename
    let input_path = Path::new(file);
    let stem = input_path.file_stem().unwrap_or_default();
    let ext = input_path.extension().unwrap_or_default();
    let output_file = format!("{}.optimized.{}", stem.to_string_lossy(), ext.to_string_lossy());
    
    // Write optimized output
    fs::write(&output_file, optimized)
        .map_err(|e| TuskError::parse_error(0, format!("Failed to write optimized file: {}", e)))?;
    
    println!("✅ Successfully optimized '{}' to '{}'", file, output_file);
    Ok(())
}

/// Minify CSS content
fn minify_css(content: &str) -> String {
    let lines: Vec<&str> = content.lines().collect();
    let mut minified = String::new();
    
    for line in lines {
        let trimmed = line.trim();
        if !trimmed.is_empty() && !trimmed.starts_with("/*") {
            minified.push_str(trimmed);
        }
    }
    
    minified
}

/// Format CSS content with proper indentation
fn format_css(content: &str) -> String {
    let lines: Vec<&str> = content.lines().collect();
    let mut formatted = String::new();
    let mut indent_level: i32 = 0;
    
    for line in lines {
        let trimmed_line = line.trim();
        
        if trimmed_line.is_empty() {
            formatted.push('\n');
            continue;
        }
        
        // Handle closing braces
        if trimmed_line.starts_with('}') {
            indent_level = indent_level.saturating_sub(1);
        }
        
        // Add indentation
        if indent_level > 0 {
            formatted.push_str(&"    ".repeat(indent_level.try_into().unwrap()));
        }
        formatted.push_str(trimmed_line);
        formatted.push('\n');
        
        // Handle opening braces
        if trimmed_line.ends_with('{') {
            indent_level += 1;
        }
    }
    
    formatted
}

/// Validate CSS syntax (simplified)
fn validate_css(content: &str) -> TuskResult<()> {
    let lines: Vec<&str> = content.lines().collect();
    let mut brace_count = 0;
    
    for (line_num, line) in lines.iter().enumerate() {
        let trimmed = line.trim();
        
        // Skip comments and empty lines
        if trimmed.is_empty() || trimmed.starts_with("/*") {
            continue;
        }
        
        // Count braces
        for ch in trimmed.chars() {
            match ch {
                '{' => brace_count += 1,
                '}' => {
                    brace_count -= 1;
                    if brace_count < 0 {
                        return Err(TuskError::ParseError {
                            line: line_num + 1,
                            column: 0,
                            message: "Unmatched closing brace".to_string(),
                            context: line.to_string(),
                            suggestion: None,
                        });
                    }
                }
                _ => {}
            }
        }
    }
    
    if brace_count != 0 {
        return Err(TuskError::ParseError {
            line: lines.len(),
            column: 0,
            message: "Unmatched opening brace".to_string(),
            context: "End of file".to_string(),
            suggestion: None,
        });
    }
    
    Ok(())
}

/// Optimize CSS for performance
fn optimize_css(content: &str) -> String {
    let mut optimized = content.to_string();
    
    // Remove comments
    optimized = optimized.lines()
        .filter(|line| !line.trim().starts_with("/*"))
        .collect::<Vec<_>>()
        .join("\n");
    
    // Remove extra whitespace
    optimized = optimized.split_whitespace().collect::<Vec<_>>().join(" ");
    
    // Remove unnecessary semicolons
    optimized = optimized.replace(";;", ";");
    
    optimized
} 