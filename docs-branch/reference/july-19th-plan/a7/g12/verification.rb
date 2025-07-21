#!/usr/bin/env ruby

require 'test/unit'
require_relative 'goal_implementation'
require_relative 'test_implementation'

class Goal12Verification < Test::Unit::TestCase
  def test_all
    suite = Test::Unit::TestSuite.new("Goal 12 Verification Suite")
    suite << TestAdvancedAPIFramework.new
    suite << TestServiceRegistry.new
    suite << TestInterServiceCommunicator.new
    suite << TestAPIGateway.new
    suite << TestLoadBalancer.new

    runner = Test::Unit::UI::Console::TestRunner.new(suite)
    runner.start
  end
end

if __FILE__ == $0
  Goal12Verification.new.test_all
end 