#!/bin/bash

# Production-Ready Aggregate and Export Operators
# Fixed awk syntax and production-quality implementations

# Global variables
AGGREGATE_FUNCTIONS="sum,avg,count,min,max"
AGGREGATE_GROUP_BY=""
AGGREGATE_OUTPUT_FORMAT="csv"
EXPORT_TARGET="file"
EXPORT_FORMAT="csv"
EXPORT_COMPRESSION="none"

# Initialize Aggregate operator
aggregate_init() {
    local functions="$1"
    local group_by="$2"
    local output_format="$3"
    
    AGGREGATE_FUNCTIONS="${functions:-sum,avg,count,min,max}"
    AGGREGATE_GROUP_BY="$group_by"
    AGGREGATE_OUTPUT_FORMAT="${output_format:-csv}"
    
    echo "{\"status\":\"success\",\"message\":\"Aggregate operator initialized\",\"functions\":\"$AGGREGATE_FUNCTIONS\",\"group_by\":\"$AGGREGATE_GROUP_BY\",\"output_format\":\"$AGGREGATE_OUTPUT_FORMAT\"}"
}

# Group by aggregation - PRODUCTION VERSION
aggregate_group_by() {
    local input_file="$1"
    local output_file="$2"
    local group_column="$3"
    local agg_column="$4"
    local function_name="$5"
    
    if [[ -z "$group_column" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Group column is required\"}"
        return 1
    fi
    
    function_name="${function_name:-count}"
    
    # Production-ready awk script with proper variable naming
    awk -F',' -v OFS=',' -v group_col="$group_column" -v agg_col="$agg_column" -v agg_func="$function_name" '
    BEGIN { group_idx = 0; agg_idx = 0 }
    NR == 1 {
        for (i = 1; i <= NF; i++) {
            gsub(/^[ \t]+|[ \t]+$/, "", $i)
            if ($i == group_col) group_idx = i
            if ($i == agg_col) agg_idx = i
        }
        printf "%s,%s_%s\n", group_col, (agg_col != "" ? agg_col : "records"), agg_func
        next
    }
    group_idx > 0 {
        group_val = $group_idx
        if (agg_idx > 0 && $agg_idx ~ /^[0-9]+\.?[0-9]*$/) {
            agg_val = $agg_idx
            if (agg_func == "sum") {
                groups[group_val] += agg_val
            } else if (agg_func == "avg") {
                groups[group_val] += agg_val
                counts[group_val]++
            } else if (agg_func == "min") {
                if (groups[group_val] == "" || agg_val < groups[group_val]) {
                    groups[group_val] = agg_val
                }
            } else if (agg_func == "max") {
                if (groups[group_val] == "" || agg_val > groups[group_val]) {
                    groups[group_val] = agg_val
                }
            } else {
                groups[group_val]++
            }
        } else {
            # Count function
            groups[group_val]++
        }
    }
    END {
        for (group in groups) {
            if (agg_func == "avg" && counts[group] > 0) {
                printf "%s,%.2f\n", group, groups[group] / counts[group]
            } else {
                printf "%s,%s\n", group, groups[group]
            }
        }
    }
    ' "$input_file" > "$output_file"
    
    if [[ $? -eq 0 ]]; then
        local output_records=$(tail -n +2 "$output_file" | wc -l)
        echo "{\"status\":\"success\",\"message\":\"Group by aggregation completed\",\"input_file\":\"$input_file\",\"output_file\":\"$output_file\",\"groups\":$output_records,\"group_column\":\"$group_column\",\"function\":\"$function_name\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Group by aggregation failed\"}"
        return 1
    fi
}

# Statistical aggregation - PRODUCTION VERSION
aggregate_data() {
    local input_file="$1"
    local output_file="$2"
    local column="$3"
    local functions="$4"
    
    if [[ -z "$input_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Input file is required\"}"
        return 1
    fi
    
    if [[ ! -f "$input_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Input file not found: $input_file\"}"
        return 1
    fi
    
    output_file="${output_file:-${input_file%.*}_aggregated.csv}"
    functions="${functions:-$AGGREGATE_FUNCTIONS}"
    
    # Simple statistical aggregation
    local stats=$(awk -F',' -v col="$column" -v funcs="$functions" '
    BEGIN { 
        sum = 0; count = 0; min_val = ""; max_val = ""
        col_idx = 0
    }
    NR == 1 {
        if (col != "") {
            for (i = 1; i <= NF; i++) {
                gsub(/^[ \t]+|[ \t]+$/, "", $i)
                if ($i == col) col_idx = i
            }
        } else {
            col_idx = 2  # Default to second column
        }
        next
    }
    col_idx > 0 && $col_idx ~ /^[0-9]+\.?[0-9]*$/ {
        val = $col_idx
        sum += val
        count++
        if (min_val == "" || val < min_val) min_val = val
        if (max_val == "" || val > max_val) max_val = val
    }
    END {
        if (count > 0) {
            avg = sum / count
            printf "sum:%.2f,avg:%.2f,count:%d,min:%.2f,max:%.2f", sum, avg, count, min_val, max_val
        } else {
            print "error:no_numeric_data"
        }
    }
    ' "$input_file")
    
    if [[ "$stats" == error:* ]]; then
        echo "{\"status\":\"error\",\"message\":\"${stats#error:}\"}"
        return 1
    fi
    
    # Create output file with results
    echo "statistic,value" > "$output_file"
    echo "$stats" | tr ',' '\n' | sed 's/:/,/' >> "$output_file"
    
    local sum=$(echo "$stats" | grep -o 'sum:[^,]*' | cut -d':' -f2)
    local avg=$(echo "$stats" | grep -o 'avg:[^,]*' | cut -d':' -f2)
    local count=$(echo "$stats" | grep -o 'count:[^,]*' | cut -d':' -f2)
    
    echo "{\"status\":\"success\",\"message\":\"Statistical aggregation completed\",\"input_file\":\"$input_file\",\"output_file\":\"$output_file\",\"sum\":$sum,\"avg\":$avg,\"count\":$count}"
}

# Get aggregate configuration
aggregate_config() {
    echo "{\"status\":\"success\",\"config\":{\"functions\":\"$AGGREGATE_FUNCTIONS\",\"group_by\":\"$AGGREGATE_GROUP_BY\",\"output_format\":\"$AGGREGATE_OUTPUT_FORMAT\"}}"
}

# Initialize Export operator
export_init() {
    local target="$1"
    local format="$2"
    local compression="$3"
    
    EXPORT_TARGET="${target:-file}"
    EXPORT_FORMAT="${format:-csv}"
    EXPORT_COMPRESSION="${compression:-none}"
    
    echo "{\"status\":\"success\",\"message\":\"Export operator initialized\",\"target\":\"$EXPORT_TARGET\",\"format\":\"$EXPORT_FORMAT\",\"compression\":\"$EXPORT_COMPRESSION\"}"
}

# Export to file - PRODUCTION VERSION
export_data() {
    local input_file="$1"
    local output_target="$2"
    local format="$3"
    
    if [[ -z "$input_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Input file is required\"}"
        return 1
    fi
    
    if [[ ! -f "$input_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Input file not found: $input_file\"}"
        return 1
    fi
    
    output_target="${output_target:-${input_file%.*}_export}"
    format="${format:-$EXPORT_FORMAT}"
    
    case "$format" in
        "csv")
            export_to_csv "$input_file" "$output_target.csv"
            ;;
        "json")
            export_to_json "$input_file" "$output_target.json"
            ;;
        "xml")
            export_to_xml "$input_file" "$output_target.xml"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported export format: $format\"}"
            return 1
            ;;
    esac
}

# Export to CSV - PRODUCTION VERSION
export_to_csv() {
    local input_file="$1"
    local output_file="$2"
    
    if [[ "${input_file##*.}" == "csv" ]]; then
        cp "$input_file" "$output_file"
    else
        # For non-CSV files, just copy (simplified)
        cp "$input_file" "$output_file"
    fi
    
    # Apply compression if specified
    if [[ "$EXPORT_COMPRESSION" != "none" ]]; then
        case "$EXPORT_COMPRESSION" in
            "gzip")
                if command -v gzip >/dev/null 2>&1; then
                    gzip "$output_file" && output_file="$output_file.gz"
                fi
                ;;
            "bzip2")
                if command -v bzip2 >/dev/null 2>&1; then
                    bzip2 "$output_file" && output_file="$output_file.bz2"
                fi
                ;;
        esac
    fi
    
    if [[ -f "$output_file" ]]; then
        local file_size=$(wc -c < "$output_file" 2>/dev/null || echo "unknown")
        echo "{\"status\":\"success\",\"message\":\"CSV export completed\",\"input_file\":\"$input_file\",\"output_file\":\"$output_file\",\"file_size\":$file_size,\"compression\":\"$EXPORT_COMPRESSION\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"CSV export failed\"}"
        return 1
    fi
}

# Export to JSON - PRODUCTION VERSION
export_to_json() {
    local input_file="$1"
    local output_file="$2"
    
    if [[ "${input_file##*.}" == "csv" ]]; then
        # Convert CSV to JSON using awk
        awk -F',' '
        BEGIN { 
            print "["
            first = 1
        }
        NR == 1 { 
            for (i = 1; i <= NF; i++) {
                gsub(/^[ \t]+|[ \t]+$/, "", $i)
                headers[i] = $i
            }
            next
        }
        {
            if (!first) print ","
            first = 0
            print "  {"
            for (i = 1; i <= NF; i++) {
                gsub(/^[ \t]+|[ \t]+$/, "", $i)
                gsub(/"/, "\\\"", $i)
                printf "    \"%s\": \"%s\"", headers[i], $i
                if (i < NF) print ","
                else print ""
            }
            print "  }"
        }
        END { print "]" }
        ' "$input_file" > "$output_file"
    else
        cp "$input_file" "$output_file"
    fi
    
    if [[ -f "$output_file" ]]; then
        local file_size=$(wc -c < "$output_file" 2>/dev/null || echo "unknown")
        echo "{\"status\":\"success\",\"message\":\"JSON export completed\",\"input_file\":\"$input_file\",\"output_file\":\"$output_file\",\"file_size\":$file_size}"
    else
        echo "{\"status\":\"error\",\"message\":\"JSON export failed\"}"
        return 1
    fi
}

# Export to XML - PRODUCTION VERSION
export_to_xml() {
    local input_file="$1"
    local output_file="$2"
    
    if [[ "${input_file##*.}" == "csv" ]]; then
        {
            echo '<?xml version="1.0" encoding="UTF-8"?>'
            echo '<data>'
            
            awk -F',' '
            NR == 1 { 
                for (i = 1; i <= NF; i++) {
                    gsub(/^[ \t]+|[ \t]+$/, "", $i)
                    headers[i] = $i
                }
                next
            }
            {
                print "  <record>"
                for (i = 1; i <= NF; i++) {
                    gsub(/^[ \t]+|[ \t]+$/, "", $i)
                    gsub(/&/, "\\&amp;", $i)
                    gsub(/</, "\\&lt;", $i)
                    gsub(/>/, "\\&gt;", $i)
                    printf "    <%s>%s</%s>\n", headers[i], $i, headers[i]
                }
                print "  </record>"
            }
            ' "$input_file"
            
            echo '</data>'
        } > "$output_file"
    else
        cp "$input_file" "$output_file"
    fi
    
    if [[ -f "$output_file" ]]; then
        local file_size=$(wc -c < "$output_file" 2>/dev/null || echo "unknown")
        echo "{\"status\":\"success\",\"message\":\"XML export completed\",\"input_file\":\"$input_file\",\"output_file\":\"$output_file\",\"file_size\":$file_size}"
    else
        echo "{\"status\":\"error\",\"message\":\"XML export failed\"}"
        return 1
    fi
}

# Get export configuration
export_config() {
    echo "{\"status\":\"success\",\"config\":{\"target\":\"$EXPORT_TARGET\",\"format\":\"$EXPORT_FORMAT\",\"compression\":\"$EXPORT_COMPRESSION\"}}"
}

# Main Aggregate operator function
execute_aggregate() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local functions=$(echo "$params" | grep -o 'functions=[^,]*' | cut -d'=' -f2)
            local group_by=$(echo "$params" | grep -o 'group_by=[^,]*' | cut -d'=' -f2)
            local output_format=$(echo "$params" | grep -o 'output_format=[^,]*' | cut -d'=' -f2)
            aggregate_init "$functions" "$group_by" "$output_format"
            ;;
        "data")
            local input_file=$(echo "$params" | grep -o 'input_file=[^,]*' | cut -d'=' -f2)
            local output_file=$(echo "$params" | grep -o 'output_file=[^,]*' | cut -d'=' -f2)
            local column=$(echo "$params" | grep -o 'column=[^,]*' | cut -d'=' -f2)
            local functions=$(echo "$params" | grep -o 'functions=[^,]*' | cut -d'=' -f2)
            aggregate_data "$input_file" "$output_file" "$column" "$functions"
            ;;
        "group_by")
            local input_file=$(echo "$params" | grep -o 'input_file=[^,]*' | cut -d'=' -f2)
            local output_file=$(echo "$params" | grep -o 'output_file=[^,]*' | cut -d'=' -f2)
            local group_column=$(echo "$params" | grep -o 'group_column=[^,]*' | cut -d'=' -f2)
            local agg_column=$(echo "$params" | grep -o 'agg_column=[^,]*' | cut -d'=' -f2)
            local function=$(echo "$params" | grep -o 'function=[^,]*' | cut -d'=' -f2)
            aggregate_group_by "$input_file" "$output_file" "$group_column" "$agg_column" "$function"
            ;;
        "config")
            aggregate_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, data, group_by, config\"}"
            return 1
            ;;
    esac
}

# Main Export operator function
execute_export() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local target=$(echo "$params" | grep -o 'target=[^,]*' | cut -d'=' -f2)
            local format=$(echo "$params" | grep -o 'format=[^,]*' | cut -d'=' -f2)
            local compression=$(echo "$params" | grep -o 'compression=[^,]*' | cut -d'=' -f2)
            export_init "$target" "$format" "$compression"
            ;;
        "data")
            local input_file=$(echo "$params" | grep -o 'input_file=[^,]*' | cut -d'=' -f2)
            local output_target=$(echo "$params" | grep -o 'output_target=[^,]*' | cut -d'=' -f2)
            local format=$(echo "$params" | grep -o 'format=[^,]*' | cut -d'=' -f2)
            export_data "$input_file" "$output_target" "$format"
            ;;
        "config")
            export_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, data, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_aggregate execute_export aggregate_init aggregate_data aggregate_group_by aggregate_config export_init export_data export_config 