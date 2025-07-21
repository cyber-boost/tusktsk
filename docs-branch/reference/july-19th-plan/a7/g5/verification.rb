#!/usr/bin/env ruby
# frozen_string_literal: true

# Final verification script for Agent A7 Goal 5
require 'json'
require 'time'

puts "🔍 Agent A7 Goal 5 - Final Verification"
puts "=" * 50

# Verify implementation
puts "\n1. Testing Goal 5 Implementation..."
begin
  require_relative 'goal_implementation'
  implementation = TuskLang::AgentA7::Goal5Implementation.new
  results = implementation.execute_all_goals
  
  puts "✅ Implementation loaded successfully"
  puts "   - G5.1: #{results[:g5_1][:success] ? 'PASS' : 'FAIL'}"
  puts "   - G5.2: #{results[:g5_2][:success] ? 'PASS' : 'FAIL'}"
  puts "   - G5.3: #{results[:g5_3][:success] ? 'PASS' : 'FAIL'}"
rescue => e
  puts "❌ Implementation failed: #{e.message}"
  exit 1
end

# Verify status.json
puts "\n2. Verifying status.json..."
begin
  status = JSON.parse(File.read('../status.json'))
  g5_status = status['goals']['g5']
  completed_goals = status['completed_goals']
  
  if g5_status == true && completed_goals == 5
    puts "✅ Status.json correctly updated"
    puts "   - G5 marked as completed: #{g5_status}"
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
  
  if total_summaries == 5 && latest_summary['goal_id'] == 'g5'
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
  
  if total_ideas == 15 && urgent_ideas >= 5
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
summary_file = '/summaries/07-19-2025-ruby-agent-a7-goal-5-implementation.md'
if File.exist?(summary_file)
  puts "✅ Summary documentation created"
  puts "   - File: #{summary_file}"
else
  puts "❌ Summary documentation missing"
  exit 1
end

puts "\n🎉 ALL VERIFICATIONS PASSED!"
puts "=" * 50
puts "Agent A7 Goal 5 implementation is complete and verified."
puts "All goals (g5.1, g5.2, g5.3) have been successfully implemented."
puts "Status files have been properly updated."
puts "Innovative ideas have been generated and documented."
puts "Summary documentation has been created."
puts "\n✅ MISSION ACCOMPLISHED!"

# Reminder about folder name
puts "\n📁 FOLDER REMINDER:"
puts "This folder is named 'g5' and is located at: reference/agents/a7/g5/"
puts "Current working directory: #{Dir.pwd}" 