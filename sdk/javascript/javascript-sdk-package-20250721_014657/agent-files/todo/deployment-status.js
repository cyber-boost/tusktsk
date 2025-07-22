#!/usr/bin/env node
/**
 * TuskLang JavaScript SDK - 5-Agent Deployment Status
 * Real-time progress tracking and visualization
 */

const fs = require('fs');
const path = require('path');

const AGENTS = ['a1', 'a2', 'a3', 'a4', 'a5'];
const COLORS = {
  RESET: '\x1b[0m',
  BRIGHT: '\x1b[1m',
  RED: '\x1b[31m',
  GREEN: '\x1b[32m',
  YELLOW: '\x1b[33m',
  BLUE: '\x1b[34m',
  MAGENTA: '\x1b[35m',
  CYAN: '\x1b[36m'
};

function displayHeader() {
  console.log(`${COLORS.CYAN}${COLORS.BRIGHT}`);
  console.log('╔══════════════════════════════════════════════════════════════════╗');
  console.log('║               TUSKLANG JAVASCRIPT SDK DEPLOYMENT                 ║');
  console.log('║                  5-AGENT PARALLEL EXECUTION                      ║');
  console.log('╚══════════════════════════════════════════════════════════════════╝');
  console.log(`${COLORS.RESET}\n`);
}

function loadAgentGoals(agentId) {
  try {
    const goalsPath = path.join(__dirname, agentId, 'goals.json');
    const data = fs.readFileSync(goalsPath, 'utf8');
    return JSON.parse(data);
  } catch (error) {
    console.error(`Failed to load goals for agent ${agentId}:`, error.message);
    return null;
  }
}

function getStatusIcon(status) {
  switch (status) {
    case 'completed': return `${COLORS.GREEN}✅${COLORS.RESET}`;
    case 'in-progress': return `${COLORS.YELLOW}⚡${COLORS.RESET}`;
    case 'pending': return `${COLORS.RED}🔴${COLORS.RESET}`;
    default: return `${COLORS.RED}❓${COLORS.RESET}`;
  }
}

function getPriorityColor(priority) {
  switch (priority) {
    case 'critical': return COLORS.RED;
    case 'high': return COLORS.YELLOW;
    case 'medium': return COLORS.BLUE;
    default: return COLORS.RESET;
  }
}

function displayAgentStatus(agentId, goals) {
  if (!goals) return;

  const agentName = goals.agent_name;
  const completedGoals = goals.completed_goals;
  const totalGoals = goals.total_goals;
  const completionPercentage = goals.completion_percentage;

  console.log(`${COLORS.BRIGHT}${COLORS.MAGENTA}AGENT ${agentId.toUpperCase()}: ${agentName}${COLORS.RESET}`);
  console.log(`Progress: ${completedGoals}/${totalGoals} goals (${completionPercentage})`);
  console.log(`Next Priority: ${goals.next_priority}\n`);

  Object.entries(goals.goals).forEach(([goalId, goal]) => {
    const statusIcon = getStatusIcon(goal.status);
    const priorityColor = getPriorityColor(goal.priority);
    const lines = `${goal.estimated_lines} lines`;
    
    console.log(`  ${statusIcon} ${priorityColor}${goalId.toUpperCase()}${COLORS.RESET}: ${goal.title}`);
    console.log(`     📝 ${goal.description.substring(0, 80)}...`);
    console.log(`     📊 ${lines} | Priority: ${priorityColor}${goal.priority}${COLORS.RESET}`);
    console.log('');
  });
  
  console.log('─'.repeat(70) + '\n');
}

function calculateOverallProgress() {
  let totalGoals = 0;
  let completedGoals = 0;
  let totalLines = 0;
  
  AGENTS.forEach(agentId => {
    const goals = loadAgentGoals(agentId);
    if (goals) {
      totalGoals += goals.total_goals;
      completedGoals += goals.completed_goals;
      
      Object.values(goals.goals).forEach(goal => {
        totalLines += goal.estimated_lines;
      });
    }
  });
  
  const overallPercentage = totalGoals > 0 ? Math.round((completedGoals / totalGoals) * 100) : 0;
  
  console.log(`${COLORS.BRIGHT}${COLORS.CYAN}OVERALL PROJECT STATUS${COLORS.RESET}`);
  console.log(`🎯 Goals: ${completedGoals}/${totalGoals} (${overallPercentage}%)`);
  console.log(`📝 Estimated Code: ${totalLines.toLocaleString()} lines`);
  console.log(`🔥 Agents: ${AGENTS.length} parallel workers`);
  
  if (overallPercentage === 0) {
    console.log(`${COLORS.RED}${COLORS.BRIGHT}STATUS: READY FOR DEPLOYMENT${COLORS.RESET}`);
  } else if (overallPercentage < 100) {
    console.log(`${COLORS.YELLOW}${COLORS.BRIGHT}STATUS: IN PROGRESS${COLORS.RESET}`);
  } else {
    console.log(`${COLORS.GREEN}${COLORS.BRIGHT}STATUS: MISSION COMPLETE${COLORS.RESET}`);
  }
  
  console.log('\n' + '═'.repeat(70));
}

function main() {
  displayHeader();
  
  AGENTS.forEach(agentId => {
    const goals = loadAgentGoals(agentId);
    displayAgentStatus(agentId, goals);
  });
  
  calculateOverallProgress();
  
  console.log(`\n${COLORS.BRIGHT}Ready to deploy 5 agents for parallel execution!${COLORS.RESET}`);
  console.log(`Run: ${COLORS.GREEN}node todo/deployment-status.js${COLORS.RESET} to check progress\n`);
}

if (require.main === module) {
  main();
}

module.exports = { displayAgentStatus, calculateOverallProgress }; 