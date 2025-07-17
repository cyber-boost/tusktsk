use clap::Subcommand;
use crate::TuskResult;
use std::fs;
use std::path::Path;

#[derive(Subcommand)]
pub enum CssCommand {
    Compile { input: String, output: Option<String>, minify: bool },
    Watch { input: String, output: Option<String> },
    Optimize { input: String, output: Option<String> },
    Validate { input: String },
    Lint { input: String, strict: bool },
    Format { input: String, output: Option<String> },
    Stats { input: String },
}

pub fn run(cmd: CssCommand) -> TuskResult<()> {
    match cmd {
        CssCommand::Compile { input, output, minify } => {
            css_compile(&input, output.as_deref(), minify)?;
            Ok(())
        }
        CssCommand::Watch { input, output } => {
            css_watch(&input, output.as_deref())?;
            Ok(())
        }
        CssCommand::Optimize { input, output } => {
            css_optimize(&input, output.as_deref())?;
            Ok(())
        }
        CssCommand::Validate { input } => {
            css_validate(&input)?;
            Ok(())
        }
        CssCommand::Lint { input, strict } => {
            css_lint(&input, strict)?;
            Ok(())
        }
        CssCommand::Format { input, output } => {
            css_format(&input, output.as_deref())?;
            Ok(())
        }
        CssCommand::Stats { input } => {
            css_stats(&input)?;
            Ok(())
        }
    }
}

/// Compile CSS files
fn css_compile(input: &str, output: Option<&str>, minify: bool) -> TuskResult<()> {
    let output_path = output.unwrap_or("output.css");
    
    if !Path::new(input).exists() {
        eprintln!("❌ Input file '{}' not found", input);
        std::process::exit(3); // File not found
    }
    
    // Read input file
    let content = fs::read_to_string(input)?;
    
    // Basic CSS compilation (placeholder for actual CSS processing)
    let compiled = if minify {
        // Simple minification (remove comments, whitespace)
        content
            .lines()
            .map(|line| line.trim())
            .filter(|line| !line.is_empty() && !line.starts_with("/*") && !line.ends_with("*/"))
            .collect::<Vec<_>>()
            .join("")
    } else {
        content
    };
    
    // Write output
    fs::write(output_path, compiled)?;
    println!("✅ CSS compiled to '{}'", output_path);
    
    Ok(())
}

/// Watch CSS files for changes
fn css_watch(input: &str, output: Option<&str>) -> TuskResult<()> {
    let output_path = output.unwrap_or("output.css");
    
    println!("👀 Watching '{}' for changes...", input);
    println!("📝 Output: '{}'", output_path);
    println!("🔄 Press Ctrl+C to stop watching");
    
    // Placeholder for file watching implementation
    println!("⚠️  File watching not yet implemented");
    
    Ok(())
}

/// Optimize CSS files
fn css_optimize(input: &str, output: Option<&str>) -> TuskResult<()> {
    let output_path = output.unwrap_or("optimized.css");
    
    if !Path::new(input).exists() {
        eprintln!("❌ Input file '{}' not found", input);
        std::process::exit(3);
    }
    
    let content = fs::read_to_string(input)?;
    
    // Basic optimization (remove comments, minify)
    let optimized = content
        .lines()
        .map(|line| line.trim())
        .filter(|line| !line.is_empty() && !line.starts_with("/*") && !line.ends_with("*/"))
        .collect::<Vec<_>>()
        .join("");
    
    fs::write(output_path, optimized)?;
    println!("✅ CSS optimized to '{}'", output_path);
    
    Ok(())
}

/// Validate CSS files
fn css_validate(input: &str) -> TuskResult<()> {
    if !Path::new(input).exists() {
        eprintln!("❌ Input file '{}' not found", input);
        std::process::exit(3);
    }
    
    let content = fs::read_to_string(input)?;
    
    // Basic CSS validation (check for basic syntax)
    let lines: Vec<&str> = content.lines().collect();
    let mut errors = 0;
    
    for (line_num, line) in lines.iter().enumerate() {
        let trimmed = line.trim();
        if !trimmed.is_empty() && !trimmed.starts_with("/*") && !trimmed.starts_with("@") {
            // Check for basic CSS rule structure
            if trimmed.contains('{') && !trimmed.contains('}') {
                eprintln!("⚠️  Line {}: Unclosed brace", line_num + 1);
                errors += 1;
            }
        }
    }
    
    if errors == 0 {
        println!("✅ CSS validation passed");
    } else {
        eprintln!("❌ CSS validation failed with {} errors", errors);
        std::process::exit(1);
    }
    
    Ok(())
}

/// Lint CSS files
fn css_lint(input: &str, strict: bool) -> TuskResult<()> {
    if !Path::new(input).exists() {
        eprintln!("❌ Input file '{}' not found", input);
        std::process::exit(3);
    }
    
    let content = fs::read_to_string(input)?;
    let lines: Vec<&str> = content.lines().collect();
    let mut warnings = 0;
    
    for (line_num, line) in lines.iter().enumerate() {
        let trimmed = line.trim();
        
        // Basic linting rules
        if trimmed.ends_with(';') && !trimmed.contains(':') {
            eprintln!("⚠️  Line {}: Unexpected semicolon", line_num + 1);
            warnings += 1;
        }
        
        if strict && trimmed.contains("!important") {
            eprintln!("⚠️  Line {}: Use of !important discouraged", line_num + 1);
            warnings += 1;
        }
    }
    
    if warnings == 0 {
        println!("✅ CSS linting passed");
    } else {
        println!("⚠️  CSS linting completed with {} warnings", warnings);
        if strict {
            std::process::exit(1);
        }
    }
    
    Ok(())
}

/// Format CSS files
fn css_format(input: &str, output: Option<&str>) -> TuskResult<()> {
    let output_path = output.unwrap_or(input);
    
    if !Path::new(input).exists() {
        eprintln!("❌ Input file '{}' not found", input);
        std::process::exit(3);
    }
    
    let content = fs::read_to_string(input)?;
    
    // Basic CSS formatting (indent rules)
    let lines: Vec<&str> = content.lines().collect();
    let mut formatted = String::new();
    let mut indent_level = 0;
    
    for line in lines {
        let trimmed = line.trim();
        if trimmed.is_empty() {
            formatted.push('\n');
            continue;
        }
        
        if trimmed.contains('}') {
            indent_level = indent_level.saturating_sub(1);
        }
        
        if indent_level > 0 {
            formatted.push_str(&"    ".repeat(indent_level));
        }
        formatted.push_str(trimmed);
        formatted.push('\n');
        
        if trimmed.contains('{') {
            indent_level += 1;
        }
    }
    
    fs::write(output_path, formatted)?;
    println!("✅ CSS formatted to '{}'", output_path);
    
    Ok(())
}

/// Get CSS statistics
fn css_stats(input: &str) -> TuskResult<()> {
    if !Path::new(input).exists() {
        eprintln!("❌ Input file '{}' not found", input);
        std::process::exit(3);
    }
    
    let content = fs::read_to_string(input)?;
    let lines: Vec<&str> = content.lines().collect();
    
    let total_lines = lines.len();
    let non_empty_lines = lines.iter().filter(|line| !line.trim().is_empty()).count();
    let comment_lines = lines.iter().filter(|line| line.trim().starts_with("/*") || line.trim().starts_with("//")).count();
    let rule_count = lines.iter().filter(|line| line.trim().contains('{')).count();
    
    println!("📊 CSS Statistics for '{}':", input);
    println!("   Total lines: {}", total_lines);
    println!("   Non-empty lines: {}", non_empty_lines);
    println!("   Comment lines: {}", comment_lines);
    println!("   CSS rules: {}", rule_count);
    println!("   File size: {} bytes", content.len());
    
    Ok(())
} 