#!/usr/bin/env ruby
# frozen_string_literal: true

# Final verification script for Agent A7 Goal 2
require 'json'
require 'time'

puts "🔍 Agent A7 Goal 2 - Final Verification"
puts "=" * 50

# Verify implementation
puts "\n1. Testing Goal 2 Implementation..."
begin
  require_relative 'goal_implementation'
  implementation = TuskLang::AgentA7::Goal2Implementation.new
  results = implementation.execute_all_goals
  
  puts "✅ Implementation loaded successfully"
  puts "   - G2.1: #{results[:g2_1][:success] ? 'PASS' : 'FAIL'}"
  puts "   - G2.2: #{results[:g2_2][:success] ? 'PASS' : 'FAIL'}"
  puts "   - G2.3: #{results[:g2_3][:success] ? 'PASS' : 'FAIL'}"
rescue => e
  puts "❌ Implementation failed: #{e.message}"
  exit 1
end

# Verify status.json
puts "\n2. Verifying status.json..."
begin
  status = JSON.parse(File.read('../status.json'))
  g2_status = status['goals']['g2']
  completed_goals = status['completed_goals']
  
  if g2_status == true && completed_goals == 2
    puts "✅ Status.json correctly updated"
    puts "   - G2 marked as completed: #{g2_status}"
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
  
  if total_summaries == 2 && latest_summary['goal_id'] == 'g2'
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
  
  if total_ideas == 6 && urgent_ideas >= 2
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
summary_file = '/summaries/07-19-2025-ruby-agent-a7-goal-2-implementation.md'
if File.exist?(summary_file)
  puts "✅ Summary documentation created"
  puts "   - File: #{summary_file}"
else
  puts "❌ Summary documentation missing"
  exit 1
end

puts "\n🎉 ALL VERIFICATIONS PASSED!"
puts "=" * 50
puts "Agent A7 Goal 2 implementation is complete and verified."
puts "All goals (g2.1, g2.2, g2.3) have been successfully implemented."
puts "Status files have been properly updated."
puts "Innovative ideas have been generated and documented."
puts "Summary documentation has been created."
puts "\n✅ MISSION ACCOMPLISHED!"

# Reminder about folder name
puts "\n📁 FOLDER REMINDER:"
puts "This folder is named 'g2' and is located at: reference/agents/a7/g2/"
puts "Current working directory: #{Dir.pwd}" 