# file() - File Operations Function

The `file()` function provides comprehensive file system operations in TuskLang, from basic reading and writing to advanced file manipulation.

## Basic File Operations

```tusk
# Read entire file
content: file("path/to/file.txt")

# Write to file
file("output.txt", "Hello, World!")

# Append to file
file("log.txt", "New log entry\n", append: true)

# Check if file exists
if (file.exists("config.tusk")) {
    config: file("config.tusk")
}
```

## Reading Files

```tusk
# Read as string
text: file.read("document.txt")

# Read as lines array
lines: file.lines("data.csv")

# Read with options
content: file.read("large.txt", {
    encoding: "UTF-8"
    max_size: 1048576  # 1MB limit
})

# Read specific bytes
chunk: file.read("binary.dat", {
    offset: 1024
    length: 512
})

# Stream large files
file.stream("huge.log", (line) => {
    if (line.contains("ERROR")) {
        errors[] = line
    }
})
```

## Writing Files

```tusk
# Write string
file.write("output.txt", "Content")

# Write with options
file.write("data.json", json_data, {
    encoding: "UTF-8"
    mode: 0644
    create_dirs: true
})

# Atomic write (write to temp, then rename)
file.write_atomic("important.conf", config_data)

# Write lines
lines: ["Line 1", "Line 2", "Line 3"]
file.write_lines("output.txt", lines)

# Append data
file.append("log.txt", timestamp() + ": Event occurred\n")
```

## File Information

```tusk
# Get file info
info: file.info("document.pdf")
/* Returns:
{
    exists: true
    size: 1048576
    size_human: "1.0 MB"
    type: "application/pdf"
    mime: "application/pdf"
    extension: "pdf"
    modified: "2024-01-15 10:30:00"
    created: "2024-01-01 09:00:00"
    accessed: "2024-01-20 14:00:00"
    permissions: 0644
    owner: "user"
    group: "staff"
    is_readable: true
    is_writable: true
    is_executable: false
    is_directory: false
    is_file: true
    is_link: false
}
*/

# Quick checks
if (file.is_readable("data.txt") && file.size("data.txt") < 1000000) {
    content: file.read("data.txt")
}
```

## Directory Operations

```tusk
# List directory contents
files: file.list("./uploads")

# List with filters
images: file.list("./media", {
    pattern: "*.{jpg,png,gif}"
    recursive: false
    include_dirs: false
})

# Recursive directory scan
all_files: file.scan("./src", {
    recursive: true
    follow_links: false
    filter: (path) => !path.contains("node_modules")
})

# Create directory
file.mkdir("path/to/new/directory", {
    recursive: true
    mode: 0755
})

# Remove directory
file.rmdir("old_directory", {
    recursive: true  # Remove contents too
})
```

## File Manipulation

```tusk
# Copy file
file.copy("source.txt", "destination.txt")

# Copy with options
file.copy("important.doc", "backup/important.doc", {
    overwrite: false
    preserve_time: true
    preserve_permissions: true
})

# Move/rename file
file.move("old_name.txt", "new_name.txt")

# Delete file
file.delete("temp.txt")

# Delete multiple files
file.delete(["temp1.txt", "temp2.txt", "*.tmp"])

# Touch file (create or update timestamp)
file.touch("marker.txt")
```

## File Permissions

```tusk
# Get permissions
perms: file.permissions("script.sh")  # Returns octal like 0755

# Set permissions
file.chmod("script.sh", 0755)

# Make executable
file.make_executable("deploy.sh")

# Change owner (requires privileges)
file.chown("file.txt", "www-data", "www-data")

# Check permissions
if (file.is_writable("config.ini")) {
    file.write("config.ini", new_config)
}
```

## Working with Paths

```tusk
# Path manipulation
path: file.path("../uploads/images/photo.jpg")
/* Returns:
{
    full: "/var/www/uploads/images/photo.jpg"
    dirname: "/var/www/uploads/images"
    basename: "photo.jpg"
    filename: "photo"
    extension: "jpg"
    relative: "../uploads/images/photo.jpg"
    absolute: "/var/www/uploads/images/photo.jpg"
}
*/

# Join paths
full_path: file.join("/var/www", "uploads", "images", "photo.jpg")

# Normalize path
clean: file.normalize("./path/../to/./file.txt")  # "to/file.txt"

# Relative path
relative: file.relative("/var/www/html", "/var/www/html/assets/css/style.css")
# Returns: "assets/css/style.css"
```

## CSV Operations

```tusk
# Read CSV
data: file.csv("data.csv")

# Read with options
records: file.csv("users.csv", {
    headers: true  # First row as headers
    delimiter: ","
    enclosure: '"'
    escape: "\\"
})

# Write CSV
file.write_csv("output.csv", [
    ["Name", "Email", "Age"],
    ["John", "john@example.com", 30],
    ["Jane", "jane@example.com", 25]
])

# Stream large CSV
file.stream_csv("large.csv", (row) => {
    if (row.age > 18) {
        process_adult(row)
    }
}, {headers: true})
```

## JSON File Operations

```tusk
# Read JSON
data: file.json("config.json")

# Write JSON
file.write_json("output.json", data, {
    pretty: true  # Pretty print
    indent: 2
})

# Update JSON file
file.update_json("settings.json", (data) => {
    data.updated_at: timestamp()
    data.version: data.version + 1
    return data
})
```

## Archive Operations

```tusk
# Create ZIP archive
file.zip("archive.zip", [
    "file1.txt",
    "file2.txt",
    "directory/"
])

# Extract ZIP
file.unzip("archive.zip", "extracted/")

# Create with options
file.zip("backup.zip", files, {
    compression: 9  # Max compression
    password: "secret"
    comment: "Backup created " + date()
})

# List ZIP contents
contents: file.zip_list("archive.zip")
```

## Temporary Files

```tusk
# Create temp file
temp: file.temp("myapp_")
/* Returns:
{
    path: "/tmp/myapp_a3f4d2"
    handle: <resource>
}
*/

# Write to temp file
file.write(temp.path, "Temporary data")

# Create temp directory
temp_dir: file.temp_dir("myapp_")

# Auto-cleanup temp file
file.with_temp((temp_path) => {
    file.write(temp_path, "Processing data")
    result: process_file(temp_path)
    return result
    # File automatically deleted after block
})
```

## File Locking

```tusk
# Exclusive lock for writing
file.lock("data.txt", "exclusive", () => {
    content: file.read("data.txt")
    updated: transform(content)
    file.write("data.txt", updated)
})

# Shared lock for reading
file.lock("config.txt", "shared", () => {
    config: file.read("config.txt")
    return parse_config(config)
})

# Non-blocking lock
if (file.try_lock("busy.txt", "exclusive")) {
    try {
        # Do work
    } finally {
        file.unlock("busy.txt")
    }
}
```

## File Watching

```tusk
# Watch for changes
file.watch("config.json", (event) => {
    if (event.type == "modified") {
        reload_config()
    }
})

# Watch directory
file.watch_dir("./src", (event) => {
    console.log(event.type + ": " + event.path)
    if (event.path.endsWith(".tusk")) {
        recompile(event.path)
    }
}, {
    recursive: true
    events: ["create", "modify", "delete"]
})
```

## Binary Files

```tusk
# Read binary
data: file.read_binary("image.jpg")

# Write binary
file.write_binary("output.bin", binary_data)

# Read chunks
file.read_chunks("large.bin", 1024, (chunk, offset) => {
    process_chunk(chunk, offset)
})

# Memory-mapped file
mmap: file.mmap("huge.dat", {
    mode: "r+b"
    offset: 0
    length: 1048576
})
```

## Error Handling

```tusk
# Safe file operations
safe_read: (path) => {
    try {
        if (!file.exists(path)) {
            return {error: "File not found"}
        }
        
        if (!file.is_readable(path)) {
            return {error: "File not readable"}
        }
        
        if (file.size(path) > 10485760) {  # 10MB
            return {error: "File too large"}
        }
        
        content: file.read(path)
        return {success: true, content: content}
        
    } catch (e) {
        return {error: e.message}
    }
}

# File operation with retries
retry_write: (path, content, max_retries: 3) => {
    attempts: 0
    
    while (attempts < max_retries) {
        try {
            file.write(path, content)
            return true
        } catch (e) {
            attempts++
            if (attempts < max_retries) {
                sleep(1)  # Wait 1 second
            } else {
                throw e
            }
        }
    }
}
```

## Best Practices

1. **Always check file existence** - Before operations
2. **Handle errors gracefully** - File operations can fail
3. **Use appropriate methods** - Stream large files
4. **Clean up temp files** - Don't leave garbage
5. **Set correct permissions** - Security matters
6. **Use atomic writes** - For critical files
7. **Validate file types** - Don't trust extensions
8. **Consider file locking** - For concurrent access

## Related Functions

- `fopen()` - Low-level file handle
- `glob()` - Pattern matching
- `realpath()` - Resolve path
- `is_uploaded_file()` - Check uploads
- `mime_content_type()` - Detect MIME type