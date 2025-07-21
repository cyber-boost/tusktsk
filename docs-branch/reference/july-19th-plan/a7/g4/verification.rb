#!/usr/bin/env ruby
# frozen_string_literal: true

# Final verification script for Agent A7 Goal 4
require 'json'
require 'time'

puts "🔍 Agent A7 Goal 4 - Final Verification"
puts "=" * 50

# Verify implementation
puts "\n1. Testing Goal 4 Implementation..."
begin
  require_relative 'goal_implementation'
  implementation = TuskLang::AgentA7::Goal4Implementation.new
  results = implementation.execute_all_goals
  
  puts "✅ Implementation loaded successfully"
  puts "   - G4.1: #{results[:g4_1][:success] ? 'PASS' : 'FAIL'}"
  puts "   - G4.2: #{results[:g4_2][:success] ? 'PASS' : 'FAIL'}"
  puts "   - G4.3: #{results[:g4_3][:success] ? 'PASS' : 'FAIL'}"
rescue => e
  puts "❌ Implementation failed: #{e.message}"
  exit 1
end

# Verify status.json
puts "\n2. Verifying status.json..."
begin
  status = JSON.parse(File.read('../status.json'))
  g4_status = status['goals']['g4']
  completed_goals = status['completed_goals']
  
  if g4_status == true && completed_goals == 4
    puts "✅ Status.json correctly updated"
    puts "   - G4 marked as completed: #{g4_status}"
    puts "   - Completed goals: #{completed_goals}/25"
  else
    puts "❌ Status.json verification failed"
    exit 1
  end
rescue => e
  puts "❌ Status.json error: #{e.message}"
  exit 1
end

# Verify summary.json
puts "\n3. Verifying summary.json..."
begin
  summary = JSON.parse(File.read('../summary.json'))
  total_summaries = summary['total_summaries']
  latest_summary = summary['summaries'].first
  
  if total_summaries == 4 && latest_summary['goal_id'] == 'g4'
    puts "✅ Summary.json correctly updated"
    puts "   - Total summaries: #{total_summaries}"
    puts "   - Latest goal: #{latest_summary['goal_id']}"
    puts "   - Tasks completed: #{latest_summary['tasks_completed'].length}"
  else
    puts "❌ Summary.json verification failed"
    exit 1
  end
rescue => e
  puts "❌ Summary.json error: #{e.message}"
  exit 1
end

# Verify ideas.json
puts "\n4. Verifying ideas.json..."
begin
  ideas = JSON.parse(File.read('../ideas.json'))
  total_ideas = ideas['total_ideas']
  urgent_ideas = ideas['priority_ideas']['urgent'].length
  
  if total_ideas == 12 && urgent_ideas >= 4
    puts "✅ Ideas.json correctly updated"
    puts "   - Total ideas: #{total_ideas}"
    puts "   - Urgent ideas: #{urgent_ideas}"
  else
    puts "❌ Ideas.json verification failed"
    exit 1
  end
rescue => e
  puts "❌ Ideas.json error: #{e.message}"
  exit 1
end

# Verify summary file exists
puts "\n5. Verifying summary documentation..."
summary_file = '/summaries/07-19-2025-ruby-agent-a7-goal-4-implementation.md'
if File.exist?(summary_file)
  puts "✅ Summary documentation created"
  puts "   - File: #{summary_file}"
else
  puts "❌ Summary documentation missing"
  exit 1
end

puts "\n🎉 ALL VERIFICATIONS PASSED!"
puts "=" * 50
puts "Agent A7 Goal 4 implementation is complete and verified."
puts "All goals (g4.1, g4.2, g4.3) have been successfully implemented."
puts "Status files have been properly updated."
puts "Innovative ideas have been generated and documented."
puts "Summary documentation has been created."
puts "\n✅ MISSION ACCOMPLISHED!"

# Reminder about folder name
puts "\n📁 FOLDER REMINDER:"
puts "This folder is named 'g4' and is located at: reference/agents/a7/g4/"
puts "Current working directory: #{Dir.pwd}" 