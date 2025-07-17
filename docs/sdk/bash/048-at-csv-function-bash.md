# 📊 TuskLang Bash @csv Function Guide

**"We don't bow to any king" – CSV is your configuration's data.**

The @csv function in TuskLang is your CSV processing powerhouse, enabling dynamic CSV parsing, generation, and manipulation directly within your configuration files. Whether you're working with data exports, processing CSV files, or generating reports, @csv provides the flexibility and power to handle tabular data seamlessly.

## 🎯 What is @csv?
The @csv function provides CSV processing capabilities in TuskLang. It offers:
- **CSV parsing** - Parse CSV strings and files
- **CSV generation** - Create CSV documents dynamically
- **CSV transformation** - Transform CSV data between formats
- **Data filtering** - Filter and query CSV data
- **Report generation** - Generate CSV reports and exports

## 📝 Basic @csv Syntax

### Simple CSV Parsing
```ini
[simple_csv]
# Parse CSV string
$csv_data: "name,email,age\nJohn,john@example.com,30\nAlice,alice@example.com,25"
parsed_csv: @csv.parse($csv_data)
user_count: @array.length($parsed_csv)
first_user: @array.first($parsed_csv)
```

### CSV File Processing
```ini
[csv_file_processing]
# Parse CSV file
users_csv: @csv.parse(@file.read("/var/data/users.csv"))
orders_csv: @csv.parse(@file.read("/var/data/orders.csv"))

# Get headers
user_headers: @array.first($users_csv)
order_headers: @array.first($orders_csv)
```

### CSV Generation
```ini
[csv_generation]
# Generate CSV dynamically
$user_data: [
    {"name": "John", "email": "john@example.com", "age": 30},
    {"name": "Alice", "email": "alice@example.com", "age": 25}
]
user_csv: @csv.generate($user_data)

# Generate CSV with custom headers
$custom_headers: ["Full Name", "Email Address", "User Age"]
custom_csv: @csv.generate($user_data, $custom_headers)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > csv-quickstart.tsk << 'EOF'
[csv_parsing]
# Parse CSV data
$csv_string: "id,name,email,status\n1,John Doe,john@example.com,active\n2,Jane Smith,jane@example.com,inactive\n3,Bob Johnson,bob@example.com,active"
parsed_data: @csv.parse($csv_string)

# Extract data
headers: @array.first($parsed_data)
data_rows: @array.slice($parsed_data, 1)
row_count: @array.length($data_rows)

[csv_generation]
# Generate CSV
$user_data: [
    {"id": 1, "name": "Alice", "email": "alice@example.com", "role": "admin"},
    {"id": 2, "name": "Bob", "email": "bob@example.com", "role": "user"},
    {"id": 3, "name": "Charlie", "email": "charlie@example.com", "role": "user"}
]
user_csv: @csv.generate($user_data)

[csv_transformation]
# Transform CSV data
$source_csv: "product_id,name,price,category\n1,Laptop,999.99,Electronics\n2,Book,19.99,Education\n3,Phone,699.99,Electronics"
transformed_csv: @csv.transform($source_csv, {
    "filter": "category == 'Electronics'",
    "columns": ["product_id", "name", "price"],
    "sort": "price DESC"
})
EOF

config=$(tusk_parse csv-quickstart.tsk)

echo "=== CSV Parsing ==="
echo "Headers: $(tusk_get "$config" csv_parsing.headers)"
echo "Row Count: $(tusk_get "$config" csv_parsing.row_count)"

echo ""
echo "=== CSV Generation ==="
echo "User CSV: $(tusk_get "$config" csv_generation.user_csv)"

echo ""
echo "=== CSV Transformation ==="
echo "Transformed CSV: $(tusk_get "$config" csv_transformation.transformed_csv)"
```

## 🔗 Real-World Use Cases

### 1. Data Export and Reporting
```ini
[data_export]
# Export database data to CSV
$user_data: @query("SELECT id, name, email, created_at FROM users WHERE active = 1")
user_csv: @csv.generate($user_data)

# Export with custom formatting
$order_data: @query("SELECT o.id, u.name, o.amount, o.created_at FROM orders o JOIN users u ON o.user_id = u.id WHERE o.created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)")
$custom_headers: ["Order ID", "Customer Name", "Order Amount", "Order Date"]
order_csv: @csv.generate($order_data, $custom_headers)

# Save exports
@file.write("/var/exports/users-" + @date("Y-m-d") + ".csv", $user_csv)
@file.write("/var/exports/orders-" + @date("Y-m-d") + ".csv", $order_csv)

# Generate summary report
$summary_data: [
    {"metric": "Total Users", "value": @array.length($user_data)},
    {"metric": "Total Orders", "value": @array.length($order_data)},
    {"metric": "Total Revenue", "value": @array.sum(@array.map($order_data, "$.amount"))},
    {"metric": "Export Date", "value": @date("Y-m-d H:i:s")}
]
summary_csv: @csv.generate($summary_data)
```

### 2. Data Import and Processing
```ini
[data_import]
# Import CSV data
$import_csv: @csv.parse(@file.read("/var/imports/new-users.csv"))
$headers: @array.first($import_csv)
$data_rows: @array.slice($import_csv, 1)

# Process imported data
$processed_users: @array.map($data_rows, {
    "name": item[0],
    "email": item[1],
    "role": item[2],
    "imported_at": @date("Y-m-d H:i:s")
})

# Validate imported data
$validation_results: @array.map($processed_users, {
    "user": item,
    "valid": @validate.email(item.email) && @validate.required([item.name, item.email]),
    "errors": @if(@validate.email(item.email), [], ["Invalid email"])
})

# Filter valid users
$valid_users: @array.filter($validation_results, "item.valid")
$invalid_users: @array.filter($validation_results, "!item.valid")

# Generate validation report
$validation_report: [
    {"status": "Valid", "count": @array.length($valid_users)},
    {"status": "Invalid", "count": @array.length($invalid_users)},
    {"status": "Total", "count": @array.length($processed_users)}
]
validation_csv: @csv.generate($validation_report)
```

### 3. Data Analysis and Filtering
```ini
[data_analysis]
# Analyze CSV data
$sales_data: @csv.parse(@file.read("/var/data/sales.csv"))

# Filter data by criteria
$high_value_sales: @csv.filter($sales_data, "amount > 1000")
$recent_sales: @csv.filter($sales_data, "date >= '2024-01-01'")
$category_sales: @csv.filter($sales_data, "category == 'Electronics'")

# Aggregate data
$sales_summary: {
    "total_sales": @array.sum(@array.map($sales_data, "$.amount")),
    "high_value_count": @array.length($high_value_sales),
    "recent_count": @array.length($recent_sales),
    "electronics_count": @array.length($category_sales)
}

# Generate analysis report
$analysis_report: [
    {"metric": "Total Sales", "value": $sales_summary.total_sales},
    {"metric": "High Value Sales", "value": $sales_summary.high_value_count},
    {"metric": "Recent Sales", "value": $sales_summary.recent_count},
    {"metric": "Electronics Sales", "value": $sales_summary.electronics_count}
]
analysis_csv: @csv.generate($analysis_report)
```

### 4. Configuration and Settings Management
```ini
[config_management]
# Export configuration to CSV
$config_data: [
    {"setting": "database_host", "value": @env("DB_HOST"), "category": "database"},
    {"setting": "database_port", "value": @env("DB_PORT"), "category": "database"},
    {"setting": "api_url", "value": @env("API_URL"), "category": "api"},
    {"setting": "cache_ttl", "value": @env("CACHE_TTL"), "category": "cache"}
]
config_csv: @csv.generate($config_data)

# Import configuration from CSV
$imported_config: @csv.parse(@file.read("/var/config/settings.csv"))
$config_settings: @array.map(@array.slice($imported_config, 1), {
    "setting": item[0],
    "value": item[1],
    "category": item[2]
})

# Apply configuration
@array.for_each($config_settings, {
    "action": "set_config",
    "setting": item.setting,
    "value": item.value,
    "category": item.category
})
```

## 🧠 Advanced @csv Patterns

### CSV Data Transformation Pipeline
```ini
[csv_pipeline]
# Multi-step CSV transformation pipeline
$source_csv: @csv.parse(@file.read("/var/data/source.csv"))

# Step 1: Clean and validate data
$cleaned_data: @csv.clean($source_csv, {
    "remove_duplicates": true,
    "trim_whitespace": true,
    "validate_emails": true
})

# Step 2: Transform data structure
$transformed_data: @csv.transform($cleaned_data, {
    "mapping": {
        "first_name": "firstName",
        "last_name": "lastName",
        "email_address": "email",
        "phone_number": "phone"
    },
    "add_columns": {
        "full_name": "firstName + ' ' + lastName",
        "imported_at": @date("Y-m-d H:i:s")
    }
})

# Step 3: Filter and sort
$filtered_data: @csv.filter($transformed_data, "email_address != ''")
$sorted_data: @csv.sort($filtered_data, "full_name ASC")

# Final output
final_csv: @csv.generate($sorted_data)
@file.write("/var/output/processed-data.csv", $final_csv)
```

### CSV Data Validation and Quality Control
```ini
[csv_validation]
# Comprehensive CSV validation
$csv_data: @csv.parse(@file.read("/var/data/import.csv"))

# Define validation rules
$validation_rules: {
    "email": "email",
    "phone": "phone",
    "age": "range:0:120",
    "required_fields": ["name", "email"]
}

# Validate each row
$validation_results: @array.map($csv_data, {
    "row": item,
    "row_number": index + 1,
    "validations": {
        "email_valid": @validate.email(item.email),
        "phone_valid": @validate.phone(item.phone),
        "age_valid": @validate.range(item.age, 0, 120),
        "required_complete": @validate.required([item.name, item.email])
    }
})

# Generate validation report
$validation_report: @array.map($validation_results, {
    "row": item.row_number,
    "email_valid": item.validations.email_valid,
    "phone_valid": item.validations.phone_valid,
    "age_valid": item.validations.age_valid,
    "required_complete": item.validations.required_complete,
    "overall_valid": item.validations.email_valid && item.validations.phone_valid && item.validations.age_valid && item.validations.required_complete
})

validation_csv: @csv.generate($validation_report)
```

### CSV Performance Optimization
```ini
[csv_optimization]
# Optimize CSV processing for large files
$large_csv: @csv.parse(@file.read("/var/data/large-dataset.csv"))

# Process in chunks
$chunk_size: 1000
$total_rows: @array.length($large_csv)
$chunks: @array.chunk($large_csv, $chunk_size)

# Process each chunk
$processed_chunks: @array.map($chunks, {
    "chunk_number": index + 1,
    "data": @csv.process_chunk(item, {
        "filter": "status == 'active'",
        "transform": {"processed": true, "chunk_id": index + 1}
    })
})

# Combine processed chunks
$combined_data: @array.flatten(@array.map($processed_chunks, "$.data"))

# Generate optimized output
optimized_csv: @csv.generate($combined_data)
```

## 🛡️ Security & Performance Notes
- **Data validation:** Validate CSV input to prevent injection attacks
- **Memory usage:** Monitor memory consumption when processing large CSV files
- **File permissions:** Ensure proper file permissions for CSV operations
- **Data sanitization:** Sanitize CSV data to prevent malicious content
- **Performance optimization:** Use chunking for large CSV files
- **Error handling:** Implement proper error handling for CSV operations

## 🐞 Troubleshooting
- **Parsing errors:** Check CSV syntax and delimiter consistency
- **Encoding issues:** Ensure proper character encoding (UTF-8 recommended)
- **Memory problems:** Use chunking for large CSV files
- **Data corruption:** Validate CSV data integrity
- **Performance issues:** Optimize CSV processing with chunking and caching

## 💡 Best Practices
- **Use UTF-8 encoding:** Always use UTF-8 for CSV files
- **Validate data:** Implement comprehensive data validation
- **Handle large files:** Use chunking for large CSV processing
- **Error handling:** Implement proper error handling and logging
- **Document format:** Document CSV format and expected structure
- **Backup data:** Always backup original data before processing

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@file Function](038-at-file-function-bash.md)
- [@validate Function](036-at-validate-function-bash.md)
- [Data Processing](094-data-processing-bash.md)
- [Report Generation](102-report-generation-bash.md)

---

**Master @csv in TuskLang and process tabular data with precision. 📊** 