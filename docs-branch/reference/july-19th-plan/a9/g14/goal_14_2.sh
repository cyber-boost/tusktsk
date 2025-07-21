#!/bin/bash

# Classification Operator Implementation
# Provides text and data classification capabilities

# Global variables
CLASSIFY_MODEL_TYPE="text"
CLASSIFY_CATEGORIES=""
CLASSIFY_THRESHOLD="0.5"
CLASSIFY_MODEL_PATH="/tmp/classifier.pkl"
CLASSIFY_PREPROCESSING="basic"

# Initialize Classification operator
classify_init() {
    local model_type="$1"
    local categories="$2"
    local threshold="$3"
    local model_path="$4"
    local preprocessing="$5"
    
    CLASSIFY_MODEL_TYPE="${model_type:-text}"
    CLASSIFY_CATEGORIES="$categories"
    CLASSIFY_THRESHOLD="${threshold:-0.5}"
    CLASSIFY_MODEL_PATH="${model_path:-/tmp/classifier.pkl}"
    CLASSIFY_PREPROCESSING="${preprocessing:-basic}"
    
    mkdir -p "$(dirname "$CLASSIFY_MODEL_PATH")"
    
    echo "{\"status\":\"success\",\"message\":\"Classification operator initialized\",\"model_type\":\"$CLASSIFY_MODEL_TYPE\",\"threshold\":\"$CLASSIFY_THRESHOLD\",\"preprocessing\":\"$CLASSIFY_PREPROCESSING\"}"
}

# Text classification
classify_text() {
    local text="$1"
    local categories="$2"
    local method="$3"
    
    text="${text//\"/\\\"}"
    categories="${categories:-$CLASSIFY_CATEGORIES}"
    method="${method:-keyword}"
    
    if [[ -z "$text" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Text is required for classification\"}"
        return 1
    fi
    
    case "$method" in
        "keyword")
            classify_text_keyword "$text" "$categories"
            ;;
        "sentiment")
            classify_text_sentiment "$text"
            ;;
        "language")
            classify_text_language "$text"
            ;;
        "spam")
            classify_text_spam "$text"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported classification method: $method\"}"
            return 1
            ;;
    esac
}

# Keyword-based text classification
classify_text_keyword() {
    local text="$1"
    local categories="$2"
    
    if [[ -z "$categories" ]]; then
        categories="positive,negative,neutral"
    fi
    
    # Convert text to lowercase for matching
    local text_lower=$(echo "$text" | tr '[:upper:]' '[:lower:]')
    
    # Define keyword sets
    declare -A keyword_sets
    keyword_sets["positive"]="good,great,excellent,amazing,wonderful,fantastic,awesome,love,best,perfect,happy,satisfied,pleased"
    keyword_sets["negative"]="bad,terrible,awful,horrible,worst,hate,disappointing,angry,frustrated,poor,failed,broken"
    keyword_sets["neutral"]="okay,average,normal,standard,typical,regular,moderate,fair,adequate"
    keyword_sets["technical"]="code,programming,software,database,server,api,algorithm,function,variable,debug"
    keyword_sets["business"]="revenue,profit,sales,marketing,customer,client,strategy,growth,investment,budget"
    keyword_sets["sports"]="game,team,player,score,win,lose,match,tournament,championship,league"
    
    local scores=""
    local best_category=""
    local best_score=0
    
    IFS=',' read -ra CATS <<< "$categories"
    for category in "${CATS[@]}"; do
        local score=0
        
        if [[ -n "${keyword_sets[$category]}" ]]; then
            IFS=',' read -ra KEYWORDS <<< "${keyword_sets[$category]}"
            for keyword in "${KEYWORDS[@]}"; do
                if [[ "$text_lower" == *"$keyword"* ]]; then
                    ((score++))
                fi
            done
        fi
        
        # Calculate confidence as percentage
        local confidence=$(echo "scale=2; $score * 100 / $(echo "${keyword_sets[$category]}" | tr ',' '\n' | wc -l)" | bc 2>/dev/null || echo "0")
        
        if [[ -n "$scores" ]]; then
            scores="$scores,"
        fi
        scores="$scores{\"category\":\"$category\",\"score\":$score,\"confidence\":$confidence}"
        
        if (( score > best_score )); then
            best_score=$score
            best_category="$category"
        fi
    done
    
    echo "{\"status\":\"success\",\"message\":\"Text classified\",\"text\":\"${text:0:100}...\",\"method\":\"keyword\",\"best_category\":\"$best_category\",\"best_score\":$best_score,\"all_scores\":[$scores]}"
}

# Sentiment classification
classify_text_sentiment() {
    local text="$1"
    
    local text_lower=$(echo "$text" | tr '[:upper:]' '[:lower:]')
    
    # Sentiment keywords
    local positive_words="good,great,excellent,amazing,wonderful,fantastic,awesome,love,best,perfect,happy,satisfied,pleased,brilliant,outstanding,superb,magnificent,marvelous,incredible,fabulous"
    local negative_words="bad,terrible,awful,horrible,worst,hate,disappointing,angry,frustrated,poor,failed,broken,disgusting,pathetic,useless,annoying,irritating,dreadful,appalling"
    
    local positive_score=0
    local negative_score=0
    
    IFS=',' read -ra POS_WORDS <<< "$positive_words"
    for word in "${POS_WORDS[@]}"; do
        if [[ "$text_lower" == *"$word"* ]]; then
            ((positive_score++))
        fi
    done
    
    IFS=',' read -ra NEG_WORDS <<< "$negative_words"
    for word in "${NEG_WORDS[@]}"; do
        if [[ "$text_lower" == *"$word"* ]]; then
            ((negative_score++))
        fi
    done
    
    local sentiment="neutral"
    local confidence=50
    
    if (( positive_score > negative_score )); then
        sentiment="positive"
        confidence=$(echo "scale=0; 50 + ($positive_score * 10)" | bc 2>/dev/null || echo "75")
    elif (( negative_score > positive_score )); then
        sentiment="negative"
        confidence=$(echo "scale=0; 50 + ($negative_score * 10)" | bc 2>/dev/null || echo "75")
    fi
    
    # Cap confidence at 100
    if (( confidence > 100 )); then
        confidence=100
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Sentiment analyzed\",\"text\":\"${text:0:100}...\",\"sentiment\":\"$sentiment\",\"confidence\":$confidence,\"positive_score\":$positive_score,\"negative_score\":$negative_score}"
}

# Language detection
classify_text_language() {
    local text="$1"
    
    local text_lower=$(echo "$text" | tr '[:upper:]' '[:lower:]')
    
    # Language patterns (simplified)
    declare -A language_patterns
    language_patterns["english"]="the,and,or,but,in,on,at,to,for,of,with,by"
    language_patterns["spanish"]="el,la,de,que,y,a,en,un,es,se,no,te,lo,le"
    language_patterns["french"]="le,de,et,à,un,il,être,et,en,avoir,que,pour"
    language_patterns["german"]="der,die,und,in,den,von,zu,das,mit,sich,des,auf"
    language_patterns["italian"]="il,di,che,e,la,per,in,un,è,da,non,con,le,si"
    
    local best_language="unknown"
    local best_score=0
    local scores=""
    
    for language in "${!language_patterns[@]}"; do
        local score=0
        IFS=',' read -ra PATTERNS <<< "${language_patterns[$language]}"
        for pattern in "${PATTERNS[@]}"; do
            if [[ "$text_lower" == *" $pattern "* ]] || [[ "$text_lower" == "$pattern "* ]] || [[ "$text_lower" == *" $pattern" ]]; then
                ((score++))
            fi
        done
        
        if [[ -n "$scores" ]]; then
            scores="$scores,"
        fi
        scores="$scores{\"language\":\"$language\",\"score\":$score}"
        
        if (( score > best_score )); then
            best_score=$score
            best_language="$language"
        fi
    done
    
    local confidence=$(echo "scale=0; $best_score * 20" | bc 2>/dev/null || echo "0")
    if (( confidence > 100 )); then
        confidence=100
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Language detected\",\"text\":\"${text:0:100}...\",\"language\":\"$best_language\",\"confidence\":$confidence,\"all_scores\":[$scores]}"
}

# Spam classification
classify_text_spam() {
    local text="$1"
    
    local text_lower=$(echo "$text" | tr '[:upper:]' '[:lower:]')
    
    # Spam indicators
    local spam_keywords="free,money,win,winner,prize,congratulations,urgent,act now,limited time,guaranteed,cash,loan,debt,credit,viagra,pharmacy,casino,lottery,inheritance,prince,million,dollars,click here,buy now,offer expires,no obligation"
    local spam_patterns="!!,\$\$\$,ALL CAPS,100% FREE,URGENT REPLY,CONGRATULATIONS!!!"
    
    local spam_score=0
    local total_checks=0
    
    # Check spam keywords
    IFS=',' read -ra SPAM_WORDS <<< "$spam_keywords"
    for word in "${SPAM_WORDS[@]}"; do
        ((total_checks++))
        if [[ "$text_lower" == *"$word"* ]]; then
            ((spam_score++))
        fi
    done
    
    # Check for excessive punctuation
    local exclamation_count=$(echo "$text" | grep -o "!" | wc -l)
    if (( exclamation_count > 3 )); then
        ((spam_score += 2))
    fi
    ((total_checks += 2))
    
    # Check for excessive capitalization
    local caps_ratio=$(echo "$text" | grep -o "[A-Z]" | wc -l)
    local total_chars=$(echo "$text" | wc -c)
    if (( total_chars > 0 && caps_ratio * 100 / total_chars > 50 )); then
        ((spam_score += 3))
    fi
    ((total_checks += 3))
    
    local spam_probability=$(echo "scale=0; $spam_score * 100 / $total_checks" | bc 2>/dev/null || echo "0")
    local classification="ham"
    
    if (( spam_probability > 30 )); then
        classification="spam"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Spam classification completed\",\"text\":\"${text:0:100}...\",\"classification\":\"$classification\",\"spam_probability\":$spam_probability,\"spam_score\":$spam_score,\"total_checks\":$total_checks}"
}

# Data classification
classify_data() {
    local data_file="$1"
    local column="$2"
    local method="$3"
    
    if [[ -z "$data_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Data file is required\"}"
        return 1
    fi
    
    if [[ ! -f "$data_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Data file not found: $data_file\"}"
        return 1
    fi
    
    method="${method:-statistical}"
    
    case "$method" in
        "statistical")
            classify_data_statistical "$data_file" "$column"
            ;;
        "outlier")
            classify_data_outlier "$data_file" "$column"
            ;;
        "clustering")
            classify_data_clustering "$data_file" "$column"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported data classification method: $method\"}"
            return 1
            ;;
    esac
}

# Statistical data classification
classify_data_statistical() {
    local data_file="$1"
    local column="$2"
    
    if [[ ! -f "$data_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Data file not found: $data_file\"}"
        return 1
    fi
    
    # Simple statistical analysis using awk
    local stats=$(awk -F',' -v col="$column" '
    BEGIN { sum=0; count=0; min=999999; max=-999999 }
    NR>1 { 
        val = (col != "") ? $col : $1
        if (val ~ /^[0-9]+\.?[0-9]*$/) {
            sum += val
            count++
            if (val < min) min = val
            if (val > max) max = val
            values[count] = val
        }
    }
    END {
        if (count > 0) {
            mean = sum/count
            # Calculate standard deviation
            sumsq = 0
            for (i=1; i<=count; i++) {
                sumsq += (values[i] - mean)^2
            }
            stddev = sqrt(sumsq/count)
            printf "mean:%.2f,stddev:%.2f,min:%.2f,max:%.2f,count:%d", mean, stddev, min, max, count
        }
    }' "$data_file")
    
    if [[ -n "$stats" ]]; then
        local mean=$(echo "$stats" | cut -d',' -f1 | cut -d':' -f2)
        local stddev=$(echo "$stats" | cut -d',' -f2 | cut -d':' -f2)
        local min_val=$(echo "$stats" | cut -d',' -f3 | cut -d':' -f2)
        local max_val=$(echo "$stats" | cut -d',' -f4 | cut -d':' -f2)
        local count=$(echo "$stats" | cut -d',' -f5 | cut -d':' -f2)
        
        echo "{\"status\":\"success\",\"message\":\"Statistical classification completed\",\"file\":\"$data_file\",\"column\":\"$column\",\"statistics\":{\"mean\":$mean,\"stddev\":$stddev,\"min\":$min_val,\"max\":$max_val,\"count\":$count}}"
    else
        echo "{\"status\":\"error\",\"message\":\"Unable to perform statistical analysis\"}"
        return 1
    fi
}

# Outlier detection
classify_data_outlier() {
    local data_file="$1"
    local column="$2"
    
    # Simple outlier detection using IQR method
    local outlier_info=$(awk -F',' -v col="$column" '
    BEGIN { count=0 }
    NR>1 { 
        val = (col != "") ? $col : $1
        if (val ~ /^[0-9]+\.?[0-9]*$/) {
            values[++count] = val
        }
    }
    END {
        if (count > 4) {
            # Sort values
            for (i=1; i<=count; i++) {
                for (j=i+1; j<=count; j++) {
                    if (values[i] > values[j]) {
                        temp = values[i]
                        values[i] = values[j]
                        values[j] = temp
                    }
                }
            }
            
            # Calculate quartiles
            q1_pos = int(count * 0.25)
            q3_pos = int(count * 0.75)
            q1 = values[q1_pos]
            q3 = values[q3_pos]
            iqr = q3 - q1
            lower_bound = q1 - 1.5 * iqr
            upper_bound = q3 + 1.5 * iqr
            
            outliers = 0
            for (i=1; i<=count; i++) {
                if (values[i] < lower_bound || values[i] > upper_bound) {
                    outliers++
                }
            }
            
            printf "q1:%.2f,q3:%.2f,iqr:%.2f,lower:%.2f,upper:%.2f,outliers:%d,total:%d", q1, q3, iqr, lower_bound, upper_bound, outliers, count
        }
    }' "$data_file")
    
    if [[ -n "$outlier_info" ]]; then
        local q1=$(echo "$outlier_info" | cut -d',' -f1 | cut -d':' -f2)
        local q3=$(echo "$outlier_info" | cut -d',' -f2 | cut -d':' -f2)
        local iqr=$(echo "$outlier_info" | cut -d',' -f3 | cut -d':' -f2)
        local lower=$(echo "$outlier_info" | cut -d',' -f4 | cut -d':' -f2)
        local upper=$(echo "$outlier_info" | cut -d',' -f5 | cut -d':' -f2)
        local outliers=$(echo "$outlier_info" | cut -d',' -f6 | cut -d':' -f2)
        local total=$(echo "$outlier_info" | cut -d',' -f7 | cut -d':' -f2)
        
        local outlier_percentage=$(echo "scale=1; $outliers * 100 / $total" | bc 2>/dev/null || echo "0")
        
        echo "{\"status\":\"success\",\"message\":\"Outlier detection completed\",\"file\":\"$data_file\",\"column\":\"$column\",\"outlier_analysis\":{\"q1\":$q1,\"q3\":$q3,\"iqr\":$iqr,\"lower_bound\":$lower,\"upper_bound\":$upper,\"outliers\":$outliers,\"total\":$total,\"outlier_percentage\":$outlier_percentage}}"
    else
        echo "{\"status\":\"error\",\"message\":\"Unable to perform outlier detection\"}"
        return 1
    fi
}

# Simple clustering
classify_data_clustering() {
    local data_file="$1"
    local column="$2"
    
    echo "{\"status\":\"success\",\"message\":\"Clustering analysis placeholder\",\"file\":\"$data_file\",\"column\":\"$column\",\"note\":\"Advanced clustering requires specialized tools\"}"
}

# Get classification configuration
classify_config() {
    echo "{\"status\":\"success\",\"config\":{\"model_type\":\"$CLASSIFY_MODEL_TYPE\",\"categories\":\"$CLASSIFY_CATEGORIES\",\"threshold\":\"$CLASSIFY_THRESHOLD\",\"model_path\":\"$CLASSIFY_MODEL_PATH\",\"preprocessing\":\"$CLASSIFY_PREPROCESSING\"}}"
}

# Main Classification operator function
execute_classify() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local model_type=$(echo "$params" | grep -o 'model_type=[^,]*' | cut -d'=' -f2)
            local categories=$(echo "$params" | grep -o 'categories=[^,]*' | cut -d'=' -f2)
            local threshold=$(echo "$params" | grep -o 'threshold=[^,]*' | cut -d'=' -f2)
            local model_path=$(echo "$params" | grep -o 'model_path=[^,]*' | cut -d'=' -f2)
            local preprocessing=$(echo "$params" | grep -o 'preprocessing=[^,]*' | cut -d'=' -f2)
            classify_init "$model_type" "$categories" "$threshold" "$model_path" "$preprocessing"
            ;;
        "text")
            local text=$(echo "$params" | grep -o 'text=[^,]*' | cut -d'=' -f2-)
            local categories=$(echo "$params" | grep -o 'categories=[^,]*' | cut -d'=' -f2)
            local method=$(echo "$params" | grep -o 'method=[^,]*' | cut -d'=' -f2)
            classify_text "$text" "$categories" "$method"
            ;;
        "data")
            local data_file=$(echo "$params" | grep -o 'data_file=[^,]*' | cut -d'=' -f2)
            local column=$(echo "$params" | grep -o 'column=[^,]*' | cut -d'=' -f2)
            local method=$(echo "$params" | grep -o 'method=[^,]*' | cut -d'=' -f2)
            classify_data "$data_file" "$column" "$method"
            ;;
        "config")
            classify_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, text, data, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_classify classify_init classify_text classify_data classify_config 