#!/bin/bash

# 🎯 Universal Agent Creator Script
# Usage: ./create_agent.sh --id A7 --specialty "DATABASE OPERATIONS" --language "PostgreSQL" --goals 4

set -e

# Default values
AGENT_ID=""
AGENT_SPECIALTY=""
LANGUAGE=""
PROJECT_NAME="TuskLang SDK"
TECHNOLOGY_AREA=""
TOTAL_GOALS=4
TARGET_DIRECTORY="src/"
RESPONSE_TIME="100"
MEMORY_LIMIT="200"
UPTIME_REQUIREMENT="99.9"
COVERAGE_TARGET="95"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Help function
show_help() {
    echo -e "${BLUE}🎯 Universal Agent Creator Script${NC}"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Required Options:"
    echo "  --id AGENT_ID               Agent ID (e.g., A7, BACKEND-1)"
    echo "  --specialty SPECIALTY       Agent specialty (e.g., 'DATABASE OPERATIONS')"
    echo "  --language LANGUAGE         Primary language/technology"
    echo ""
    echo "Optional Options:"
    echo "  --project PROJECT_NAME      Project name (default: 'TuskLang SDK')"
    echo "  --goals NUMBER              Number of goals (default: 4)"
    echo "  --directory PATH            Target directory (default: 'src/')"
    echo "  --response-time MS          Response time target in ms (default: 100)"
    echo "  --memory-limit MB           Memory limit in MB (default: 200)"
    echo "  --uptime PERCENTAGE         Uptime requirement (default: 99.9)"
    echo "  --coverage PERCENTAGE       Coverage target (default: 95)"
    echo "  --help                      Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 --id A7 --specialty 'DATABASE OPERATIONS' --language 'PostgreSQL' --goals 5"
    echo "  $0 --id FRONTEND-1 --specialty 'UI COMPONENTS' --language 'React' --project 'Dashboard'"
    echo "  $0 --id API-2 --specialty 'REST SERVICES' --language 'Python' --goals 3"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --id)
            AGENT_ID="$2"
            shift 2
            ;;
        --specialty)
            AGENT_SPECIALTY="$2"
            shift 2
            ;;
        --language)
            LANGUAGE="$2"
            shift 2
            ;;
        --project)
            PROJECT_NAME="$2"
            shift 2
            ;;
        --goals)
            TOTAL_GOALS="$2"
            shift 2
            ;;
        --directory)
            TARGET_DIRECTORY="$2"
            shift 2
            ;;
        --response-time)
            RESPONSE_TIME="$2"
            shift 2
            ;;
        --memory-limit)
            MEMORY_LIMIT="$2"
            shift 2
            ;;
        --uptime)
            UPTIME_REQUIREMENT="$2"
            shift 2
            ;;
        --coverage)
            COVERAGE_TARGET="$2"
            shift 2
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            echo -e "${RED}Error: Unknown option $1${NC}"
            show_help
            exit 1
            ;;
    esac
done

# Validate required parameters
if [[ -z "$AGENT_ID" || -z "$AGENT_SPECIALTY" || -z "$LANGUAGE" ]]; then
    echo -e "${RED}Error: Missing required parameters${NC}"
    echo ""
    show_help
    exit 1
fi

# Derive technology area from specialty if not provided
if [[ -z "$TECHNOLOGY_AREA" ]]; then
    TECHNOLOGY_AREA="$AGENT_SPECIALTY"
fi

# Create agent directory
AGENT_DIR="todo2/agents/$(echo $AGENT_ID | tr '[:upper:]' '[:lower:]')"
mkdir -p "$AGENT_DIR"

# Create goal directories
for ((i=1; i<=TOTAL_GOALS; i++)); do
    mkdir -p "$AGENT_DIR/g$i"
done

echo -e "${BLUE}🚀 Creating Agent $AGENT_ID: $AGENT_SPECIALTY${NC}"
echo -e "${YELLOW}Project: $PROJECT_NAME${NC}"
echo -e "${YELLOW}Language: $LANGUAGE${NC}"
echo -e "${YELLOW}Goals: $TOTAL_GOALS${NC}"
echo ""

# Generate goals.json
cat > "$AGENT_DIR/goals.json" << EOF
{
  "agent_id": "$AGENT_ID",
  "agent_name": "$AGENT_SPECIALTY Specialist",
  "project_name": "$PROJECT_NAME",
  "language": "$LANGUAGE",
  "technology_area": "$TECHNOLOGY_AREA",
  "total_goals": $TOTAL_GOALS,
  "completed_goals": 0,
  "in_progress_goals": 0,
  "completion_percentage": "0%",
  "target_directory": "$TARGET_DIRECTORY",
  "goals": {
EOF

# Generate individual goals
for ((i=1; i<=TOTAL_GOALS; i++)); do
    cat >> "$AGENT_DIR/goals.json" << EOF
    "g$i": {
      "title": "[COMPONENT_NAME_$i]",
      "status": "pending",
      "description": "[DETAILED_DESCRIPTION_$i]",
      "estimated_lines": "[LINES]",
      "priority": "critical",
      "directory": "$TARGET_DIRECTORY/component$i/",
      "completion_date": null,
      "notes": ""
    }$(if [[ $i -lt $TOTAL_GOALS ]]; then echo ","; fi)
EOF
done

# Complete goals.json
cat >> "$AGENT_DIR/goals.json" << EOF
  },
  "performance_metrics": {
    "response_time_target": "${RESPONSE_TIME}ms",
    "memory_limit_target": "${MEMORY_LIMIT}MB",
    "uptime_requirement": "${UPTIME_REQUIREMENT}%",
    "coverage_target": "${COVERAGE_TARGET}%",
    "current_response_time": null,
    "current_memory_usage": null,
    "current_uptime": null,
    "current_coverage": null
  },
  "technical_requirements": {
    "language_specific_safety": "[LANGUAGE_SPECIFIC_SAFETY]",
    "resource_management": "[RESOURCE_MANAGEMENT]",
    "resilience_patterns": "[RESILIENCE_PATTERNS]",
    "security_standards": "[SECURITY_STANDARDS]",
    "logging_framework": "[LOGGING_FRAMEWORK]",
    "configuration_pattern": "[CONFIGURATION_PATTERN]"
  },
  "last_updated": "$(date -Iseconds)",
  "next_priority": "g1"
}
EOF

# Generate ideas.json
cat > "$AGENT_DIR/ideas.json" << EOF
{
  "agent": "$AGENT_ID",
  "focus_area": "$AGENT_SPECIALTY",
  "ideas": [
    {
      "id": "initial-setup",
      "title": "Initial Implementation Strategy",
      "description": "Plan initial approach for $AGENT_SPECIALTY implementation",
      "priority": "high",
      "suggested_by": "agent_generator",
      "date": "$(date -Iseconds | cut -d'T' -f1)"
    }
  ],
  "future_considerations": [
    "Performance optimization opportunities",
    "Integration with other agents",
    "Scalability improvements",
    "Security enhancements"
  ],
  "last_updated": "$(date -Iseconds)"
}
EOF

# Generate summaries.json
cat > "$AGENT_DIR/summaries.json" << EOF
{
  "agent": "$AGENT_ID",
  "focus_area": "$AGENT_SPECIALTY", 
  "summaries": [],
  "achievements": {
    "total_completed_goals": 0,
    "performance_benchmarks_met": [],
    "quality_standards_achieved": []
  },
  "lessons_learned": [],
  "recommendations_for_other_agents": [],
  "last_updated": "$(date -Iseconds)"
}
EOF

# Generate prompt.txt
cat > "$AGENT_DIR/prompt.txt" << EOF
# 🚨 AGENT $AGENT_ID: $AGENT_SPECIALTY SPECIALIST
**VELOCITY MODE ACTIVATED - ZERO TOLERANCE FOR PLACEHOLDERS**

## 🎯 YOUR MISSION
You are Agent $AGENT_ID, responsible for **$AGENT_SPECIALTY** of the $PROJECT_NAME. You ensure production-ready $TECHNOLOGY_AREA implementation.

## 🔥 STRICT REQUIREMENTS
**⚠️ WARNING: SEVERE PUNISHMENT FOR NON-COMPLIANCE ⚠️**
- **NO PLACEHOLDERS** - All code must be real and comprehensive
- **NO "TODO" COMMENTS** - Complete $LANGUAGE implementation coverage
- **NO STUB IMPLEMENTATIONS** - Real functionality with actual integrations
- **NO FAKE BENCHMARKS** - Real performance measurements and optimizations
- **PUNISHMENT**: Non-compliance results in immediate task reassignment

## 🚀 VELOCITY MODE RULES
1. **PRODUCTION QUALITY ONLY** - $LANGUAGE code ready for enterprise use
2. **COMPREHENSIVE COVERAGE** - $COVERAGE_TARGET% minimum coverage
3. **PERFORMANCE OPTIMIZED** - Meet <${RESPONSE_TIME}ms response time requirement
4. **FULLY INTEGRATED** - Complete $TECHNOLOGY_AREA integration and examples
5. **REAL-WORLD TESTING** - Integration tests with actual scenarios

## 📋 YOUR CORE RESPONSIBILITIES
- **Component Development** - Build production-ready $LANGUAGE components
- **Performance Optimization** - Achieve <${RESPONSE_TIME}ms response times
- **Integration Testing** - Ensure seamless $TECHNOLOGY_AREA integration
- **Quality Assurance** - Maintain $COVERAGE_TARGET% test coverage
- **Documentation** - Complete API documentation and examples

## 📊 SUCCESS METRICS
- $TOTAL_GOALS/$TOTAL_GOALS goals completed with production-ready code
- <${RESPONSE_TIME}ms response time for all operations
- <${MEMORY_LIMIT}MB memory usage under sustained load
- $UPTIME_REQUIREMENT% uptime with automatic failover
- $COVERAGE_TARGET% test coverage with comprehensive test suites

## 🔄 WORKFLOW
1. Complete each goal with real $LANGUAGE implementation and optimization
2. Update goals.json immediately upon completion
3. Document technical insights in ideas.json
4. Record performance achievements in summaries.json
5. Coordinate with other agents for comprehensive integration

**🚨 REMEMBER: PLACEHOLDER CODE = IMMEDIATE PUNISHMENT**
**⚡ VELOCITY MODE: REAL $TECHNOLOGY_AREA ONLY**
EOF

echo -e "${GREEN}✅ Agent $AGENT_ID created successfully!${NC}"
echo ""
echo -e "${BLUE}📁 Files created:${NC}"
echo "  - $AGENT_DIR/goals.json"
echo "  - $AGENT_DIR/ideas.json" 
echo "  - $AGENT_DIR/summaries.json"
echo "  - $AGENT_DIR/prompt.txt"
echo ""
echo -e "${YELLOW}📋 Next steps:${NC}"
echo "1. Edit $AGENT_DIR/goals.json to fill in specific component details"
echo "2. Customize $AGENT_DIR/prompt.txt for specific requirements"
echo "3. Deploy the agent with: @/$AGENT_ID"
echo ""
echo -e "${BLUE}🚀 Agent $AGENT_ID is ready for deployment!${NC}" 