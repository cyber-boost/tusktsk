use crate::error::{TuskError, TuskResult};
use crate::value::Value;
use nom::{
    branch::alt,
    bytes::complete::{take_while1, take_while, is_not},
    character::complete::{char, digit1, space0, space1},
    combinator::{map, map_res, recognize, value},
    multi::many1,
    sequence::{delimited, separated_pair, tuple, preceded},
    IResult,
};
use regex::Regex;
use std::collections::HashMap;
use std::str::FromStr;

/// Ultra-fast TuskLang parser with zero-copy operations
pub struct Parser {
    variables: HashMap<String, Value>,
    enable_variables: bool,
}

/// Builder for configuring parser options
pub struct ParserBuilder {
    variables: HashMap<String, Value>,
    enable_variables: bool,
}

impl ParserBuilder {
    /// Create a new parser builder
    pub fn new() -> Self {
        Self {
            variables: HashMap::new(),
            enable_variables: true,
        }
    }

    /// Add a variable for interpolation
    pub fn variable(mut self, name: impl Into<String>, value: impl Into<Value>) -> Self {
        self.variables.insert(name.into(), value.into());
        self
    }

    /// Enable or disable variable interpolation
    pub fn enable_variables(mut self, enable: bool) -> Self {
        self.enable_variables = enable;
        self
    }

    /// Build the parser
    pub fn build(self) -> Parser {
        Parser {
            variables: self.variables,
            enable_variables: self.enable_variables,
        }
    }
}

impl Default for ParserBuilder {
    fn default() -> Self {
        Self::new()
    }
}

impl Parser {
    /// Create a new parser with default settings
    pub fn new() -> Self {
        ParserBuilder::new().build()
    }

    /// Create a new parser with variables
    pub fn with_variables(variables: HashMap<String, Value>) -> Self {
        ParserBuilder::new()
            .enable_variables(true)
            .build()
    }

    /// Parse a TuskLang string into a Config
    pub fn parse(&mut self, input: &str) -> TuskResult<HashMap<String, Value>> {
        let lines: Vec<&str> = input.lines().collect();
        let mut config = HashMap::new();
        let mut current_indent = 0;
        let mut current_key = None;
        let mut current_value = None;

        for (line_num, line) in lines.iter().enumerate() {
            let line_num = line_num + 1;
            let trimmed = line.trim();

            // Skip empty lines and comments
            if trimmed.is_empty() || trimmed.starts_with('#') {
                continue;
            }

            // Parse the line
            match parse_line(trimmed) {
                Ok((_, (key, value))) => {
                    // Handle indentation
                    let indent = line.len() - line.trim_start().len();
                    
                    if indent > current_indent {
                        // Nested structure
                        if let Some(key) = current_key.take() {
                            if let Some(value) = current_value.take() {
                                config.insert(key, value);
                            }
                        }
                        current_indent = indent;
                    } else if indent < current_indent {
                        // End of nested structure
                        if let Some(key) = current_key.take() {
                            if let Some(value) = current_value.take() {
                                config.insert(key, value);
                            }
                        }
                        current_indent = indent;
                    }

                    // Handle array items
                    if key.is_empty() {
                        // This is an array item
                        if let Some(current_array) = current_value.as_mut() {
                            if let Value::Array(arr) = current_array {
                                arr.push(value);
                            }
                        }
                    } else {
                        // This is a key-value pair
                        if let Some(key) = current_key.take() {
                            if let Some(value) = current_value.take() {
                                config.insert(key, value);
                            }
                        }
                        current_key = Some(key.to_string());
                        current_value = Some(value);
                    }
                }
                Err(_) => {
                    return Err(TuskError::parse_error(
                        line_num,
                        format!("Invalid syntax: {}", line),
                    ));
                }
            }
        }

        // Insert the last key-value pair
        if let Some(key) = current_key {
            if let Some(value) = current_value {
                config.insert(key, value);
            }
        }

        // Process variable interpolation if enabled
        if self.enable_variables {
            self.interpolate_variables(&mut config)?;
        }

        Ok(config)
    }

    /// Set a variable for interpolation
    pub fn set_variable(&mut self, name: impl Into<String>, value: impl Into<Value>) {
        self.variables.insert(name.into(), value.into());
    }

    /// Interpolate variables in the configuration
    fn interpolate_variables(&self, config: &mut HashMap<String, Value>) -> TuskResult<()> {
        let var_regex = Regex::new(r"\$(\w+)").unwrap();
        
        for value in config.values_mut() {
            self.interpolate_value(value, &var_regex)?;
        }
        
        Ok(())
    }

    /// Interpolate variables in a single value
    fn interpolate_value(&self, value: &mut Value, var_regex: &Regex) -> TuskResult<()> {
        match value {
            Value::String(s) => {
                let mut result = s.clone();
                for cap in var_regex.captures_iter(s) {
                    if let Some(var_name) = cap.get(1) {
                        let var_name = var_name.as_str();
                        if let Some(var_value) = self.variables.get(var_name) {
                            result = result.replace(&cap[0], &var_value.to_string());
                        }
                    }
                }
                *s = result;
            }
            Value::Array(arr) => {
                for item in arr {
                    self.interpolate_value(item, var_regex)?;
                }
            }
            Value::Object(obj) => {
                for item in obj.values_mut() {
                    self.interpolate_value(item, var_regex)?;
                }
            }
            _ => {}
        }
        Ok(())
    }
}

impl Default for Parser {
    fn default() -> Self {
        Self::new()
    }
}

// Nom parsers

/// Parse a complete TuskLang line
fn parse_line(input: &str) -> IResult<&str, (&str, Value)> {
    alt((parse_key_value, parse_array_item))(input)
}

/// Parse a key-value pair
fn parse_key_value(input: &str) -> IResult<&str, (&str, Value)> {
    separated_pair(
        parse_key,
        delimited(space0, char(':'), space0),
        parse_value,
    )(input)
}

/// Parse an array item
fn parse_array_item(input: &str) -> IResult<&str, (&str, Value)> {
    map(
        preceded(
            tuple((space0, char('-'), space1)),
            parse_value,
        ),
        |value| ("", value),
    )(input)
}

/// Parse a key (identifier)
fn parse_key(input: &str) -> IResult<&str, &str> {
    take_while1(|c: char| c.is_alphanumeric() || c == '_' || c == '-')(input)
}

/// Parse a value
fn parse_value(input: &str) -> IResult<&str, Value> {
    alt((
        parse_string,
        parse_number,
        parse_boolean,
        parse_null,
    ))(input)
}

/// Parse a string value
fn parse_string(input: &str) -> IResult<&str, Value> {
    alt((
        // Quoted string
        map(
            delimited(
                char('"'),
                is_not("\""),
                char('"'),
            ),
            |s: &str| Value::String(s.to_string()),
        ),
        // Unquoted string (identifier)
        map(
            take_while(|c: char| c.is_alphanumeric() || c == '_' || c == '-' || c == '.'),
            |s: &str| Value::String(s.to_string()),
        ),
    ))(input)
}

/// Parse a number value
fn parse_number(input: &str) -> IResult<&str, Value> {
    map_res(
        recognize(digit1),
        |s: &str| s.parse::<f64>().map(Value::Number),
    )(input)
}

/// Parse a boolean value
fn parse_boolean(input: &str) -> IResult<&str, Value> {
    alt((
        map(recognize(tuple((char('t'), char('r'), char('u'), char('e')))), |_| Value::Boolean(true)),
        map(recognize(tuple((char('f'), char('a'), char('l'), char('s'), char('e')))), |_| Value::Boolean(false)),
    ))(input)
}

/// Parse a null value
fn parse_null(input: &str) -> IResult<&str, Value> {
    map(recognize(tuple((char('n'), char('u'), char('l'), char('l')))), |_| Value::Null)(input)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_key_value() {
        let result = parse_key_value("app_name: \"Test App\"");
        if let Err(e) = &result {
            println!("Parse error: {:?}", e);
        }
        assert!(result.is_ok());
        let (_, (key, value)) = result.unwrap();
        assert_eq!(key, "app_name");
        assert_eq!(value, Value::String("Test App".to_string()));
    }

    #[test]
    fn test_parse_number() {
        let result = parse_number("42");
        assert!(result.is_ok());
        let (_, value) = result.unwrap();
        assert_eq!(value, Value::Number(42.0));
    }

    #[test]
    fn test_parse_boolean() {
        let result = parse_boolean("true");
        assert!(result.is_ok());
        let (_, value) = result.unwrap();
        assert_eq!(value, Value::Boolean(true));
    }

    #[test]
    fn test_parse_array_item() {
        let result = parse_array_item("- logging");
        assert!(result.is_ok());
        let (_, (key, value)) = result.unwrap();
        assert_eq!(key, "");
        assert_eq!(value, Value::String("logging".to_string()));
    }

    #[test]
    fn test_parser_with_variables() {
        let mut parser = Parser::new();
        parser.set_variable("base_url", "https://api.example.com");
        
        let input = r#"
app_name: "Test App"
endpoint: "$base_url/v1/users"
"#;
        
        let result = parser.parse(input).unwrap();
        assert_eq!(
            result.get("endpoint").unwrap(),
            &Value::String("https://api.example.com/v1/users".to_string())
        );
    }
} 