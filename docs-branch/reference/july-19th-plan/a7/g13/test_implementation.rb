#!/usr/bin/env ruby

require 'test/unit'
require_relative 'goal_implementation'

class TestConcurrencyFramework < Test::Unit::TestCase
  def setup
    @framework = ConcurrencyFramework.new(thread_pool_size: 4, async_pool_size: 2)
  end

  def teardown
    @framework.shutdown
  end

  def test_framework_initialization
    assert_not_nil @framework.thread_pool
    assert_not_nil @framework.async_executor
    assert_not_nil @framework.parallel_processor
  end

  def test_execute_concurrent
    tasks = [
      -> { sleep(0.01); "Task 1" },
      -> { sleep(0.01); "Task 2" },
      -> { sleep(0.01); "Task 3" }
    ]
    
    start_time = Time.now
    results = @framework.execute_concurrent(tasks)
    end_time = Time.now
    
    assert_equal 3, results.length
    assert results.include?("Task 1")
    assert results.include?("Task 2")
    assert results.include?("Task 3")
    
    # Should be faster than sequential execution
    assert (end_time - start_time) < 0.1
  end

  def test_execute_async
    task = -> { sleep(0.01); "Async result" }
    promise = @framework.execute_async(task)
    
    assert_not_nil promise
    result = promise.value
    assert_equal "Async result", result
  end

  def test_execute_parallel
    data = [1, 2, 3, 4, 5]
    results = @framework.execute_parallel(data) { |x| x * 2 }
    
    assert_equal [2, 4, 6, 8, 10], results
  end
end

class TestThreadManager < Test::Unit::TestCase
  def setup
    @manager = ThreadManager.new
  end

  def teardown
    # Clean up any remaining threads
    @manager.active_threads.each { |name| @manager.kill_thread(name) }
  end

  def test_create_thread
    result = nil
    @manager.create_thread('test_thread') { result = "Thread executed" }
    
    sleep(0.01) # Give thread time to execute
    assert_equal "Thread executed", result
    assert @manager.active_threads.include?('test_thread')
  end

  def test_create_thread_group
    results = []
    @manager.create_thread_group('test_group', 3) do |index|
      results << "Thread #{index}"
    end
    
    @manager.join_group('test_group')
    assert_equal 3, results.length
    assert results.include?("Thread 0")
    assert results.include?("Thread 1")
    assert results.include?("Thread 2")
  end

  def test_thread_status
    @manager.create_thread('status_test') { sleep(0.1) }
    
    status = @manager.thread_status('status_test')
    assert ['run', 'sleep'].include?(status)
  end

  def test_kill_thread
    @manager.create_thread('kill_test') { sleep(1) }
    assert @manager.active_threads.include?('kill_test')
    
    @manager.kill_thread('kill_test')
    sleep(0.01)
    assert !@manager.active_threads.include?('kill_test')
  end
end

class TestAsyncIOManager < Test::Unit::TestCase
  def setup
    @io_manager = AsyncIOManager.new
    @test_file = '/tmp/test_async_io.txt'
    @test_content = 'Test async content'
  end

  def teardown
    File.delete(@test_file) if File.exist?(@test_file)
  end

  def test_async_write_and_read
    # Test async write
    write_id = @io_manager.async_write(@test_file, @test_content)
    assert_not_nil write_id
    
    # Wait for completion
    assert @io_manager.wait_for_completion(write_id, 1)
    
    # Test async read
    read_id = @io_manager.async_read(@test_file)
    assert_not_nil read_id
    
    # Wait for completion
    assert @io_manager.wait_for_completion(read_id, 1)
    
    # Verify result
    operation = @io_manager.operation_status(read_id)
    assert_equal :completed, operation[:status]
    assert_equal @test_content, operation[:result]
  end

  def test_async_http_request
    request_id = @io_manager.async_http_request('http://example.com')
    assert_not_nil request_id
    
    # Wait for completion
    assert @io_manager.wait_for_completion(request_id, 1)
    
    operation = @io_manager.operation_status(request_id)
    assert_equal :completed, operation[:status]
    assert_equal 200, operation[:result][:status]
  end

  def test_operation_status
    write_id = @io_manager.async_write(@test_file, @test_content)
    status = @io_manager.operation_status(write_id)
    
    assert_not_nil status
    assert [:pending, :completed, :failed].include?(status[:status])
  end
end

class TestParallelProcessor < Test::Unit::TestCase
  def setup
    @processor = ParallelProcessor.new(4)
  end

  def test_process
    data = [1, 2, 3, 4, 5, 6, 7, 8]
    results = @processor.process(data) { |x| x * x }
    
    expected = [1, 4, 9, 16, 25, 36, 49, 64]
    assert_equal expected, results
  end

  def test_map_reduce
    data = [1, 2, 3, 4, 5]
    result = @processor.map_reduce(
      data,
      ->(x) { x * x },  # Map: square each number
      ->(acc, x) { acc + x },  # Reduce: sum all results
      0  # Initial value
    )
    
    # 1² + 2² + 3² + 4² + 5² = 1 + 4 + 9 + 16 + 25 = 55
    assert_equal 55, result
  end

  def test_parallel_select
    data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
    results = @processor.parallel_select(data) { |x| x.even? }
    
    assert_equal [2, 4, 6, 8, 10], results
  end

  def test_parallel_sort
    data = [5, 2, 8, 1, 9, 3]
    results = @processor.parallel_sort(data)
    
    assert_equal [1, 2, 3, 5, 8, 9], results
  end

  def test_parallel_sort_with_custom_comparator
    data = ['apple', 'pie', 'banana', 'cat']
    results = @processor.parallel_sort(data) { |a, b| a.length <=> b.length }
    
    assert_equal ['pie', 'cat', 'apple', 'banana'], results
  end
end

class TestDistributedComputingFramework < Test::Unit::TestCase
  def setup
    @framework = DistributedComputingFramework.new
  end

  def test_register_node
    @framework.register_node('node1', { host: 'localhost', port: 3001, capacity: 10 })
    
    assert @framework.nodes.key?('node1')
    assert_equal 'localhost', @framework.nodes['node1'][:host]
    assert_equal 3001, @framework.nodes['node1'][:port]
    assert_equal 10, @framework.nodes['node1'][:capacity]
  end

  def test_distribute_task
    @framework.register_node('node1', { host: 'localhost', port: 3001, capacity: 10 })
    
    task = -> { "Test task" }
    result = @framework.distribute_task(task)
    
    assert_not_nil result
    assert_equal :completed, result[:status]
  end

  def test_distribute_tasks
    @framework.register_node('node1', { host: 'localhost', port: 3001 })
    @framework.register_node('node2', { host: 'localhost', port: 3002 })
    
    tasks = [-> { "Task 1" }, -> { "Task 2" }, -> { "Task 3" }]
    results = @framework.distribute_tasks(tasks, :round_robin)
    
    assert_equal 3, results.length
    results.each { |result| assert_equal :completed, result[:status] }
  end

  def test_heartbeat
    @framework.register_node('node1', { host: 'localhost', port: 3001 })
    
    # Simulate old heartbeat
    @framework.nodes['node1'][:last_heartbeat] = Time.now - 60
    @framework.check_node_health
    assert_equal :inactive, @framework.nodes['node1'][:status]
    
    # Send heartbeat
    @framework.heartbeat('node1')
    assert_equal :active, @framework.nodes['node1'][:status]
  end

  def test_get_cluster_stats
    @framework.register_node('node1', { host: 'localhost', port: 3001, capacity: 10 })
    @framework.register_node('node2', { host: 'localhost', port: 3002, capacity: 15 })
    
    stats = @framework.get_cluster_stats
    
    assert_equal 2, stats[:total_nodes]
    assert_equal 2, stats[:active_nodes]
    assert_equal 25, stats[:total_capacity]
    assert_equal 0, stats[:current_load]
  end
end

class TestTaskDistributor < Test::Unit::TestCase
  def setup
    @distributor = TaskDistributor.new
  end

  def test_send_task
    node = 'test_node'
    task = -> { "Test task execution" }
    
    result = @distributor.send_task(node, task)
    
    assert_not_nil result
    assert_equal :completed, result[:status]
    assert_equal node, result[:node]
  end
end

class TestLoadBalancer < Test::Unit::TestCase
  def setup
    @load_balancer = LoadBalancer.new
    @nodes = {
      'node1' => { current_load: 5 },
      'node2' => { current_load: 3 },
      'node3' => { current_load: 7 }
    }
  end

  def test_select_least_loaded
    selected = @load_balancer.select_node(@nodes, { strategy: :least_loaded })
    assert_equal 'node2', selected
  end

  def test_select_round_robin
    selections = []
    3.times do
      selected = @load_balancer.select_node(@nodes, { strategy: :round_robin })
      selections << selected
    end
    
    # Should cycle through all nodes
    assert_equal 3, selections.uniq.length
  end

  def test_select_random
    selected = @load_balancer.select_node(@nodes, { strategy: :random })
    assert @nodes.keys.include?(selected)
  end
end

class TestActorSystem < Test::Unit::TestCase
  def setup
    @actor_system = ActorSystem.new
  end

  def teardown
    # Clean up actors
    @actor_system.instance_variable_get(:@actors).keys.each do |actor_id|
      @actor_system.stop_actor(actor_id)
    end
  end

  def test_spawn_actor
    actor_id = @actor_system.spawn_actor(Actor, 'test_actor')
    
    assert_equal 'test_actor', actor_id
    assert @actor_system.instance_variable_get(:@actors).key?(actor_id)
    assert @actor_system.instance_variable_get(:@mailboxes).key?(actor_id)
  end

  def test_send_message
    actor_id = @actor_system.spawn_actor(Actor, 'message_test')
    
    result = @actor_system.send_message(actor_id, { type: :ping })
    assert_equal true, result
  end

  def test_stop_actor
    actor_id = @actor_system.spawn_actor(Actor, 'stop_test')
    assert @actor_system.instance_variable_get(:@actors).key?(actor_id)
    
    @actor_system.stop_actor(actor_id)
    assert !@actor_system.instance_variable_get(:@actors).key?(actor_id)
  end

  def test_get_actor_stats
    @actor_system.spawn_actor(Actor, 'stats_test_1')
    @actor_system.spawn_actor(Actor, 'stats_test_2')
    
    stats = @actor_system.get_actor_stats
    
    assert_equal 2, stats[:total_actors]
    assert stats[:active_actors] >= 0
    assert stats[:total_messages] >= 0
  end
end

class TestActor < Test::Unit::TestCase
  def setup
    @actor_system = ActorSystem.new
    @actor = Actor.new('test_actor', @actor_system)
  end

  def test_actor_initialization
    assert_equal 'test_actor', @actor.id
    assert_equal @actor_system, @actor.system
    assert_equal true, @actor.alive?
    assert_equal({}, @actor.state)
  end

  def test_handle_ping_message
    response = @actor.handle_message({ type: :ping })
    
    assert_equal :pong, response[:type]
    assert_equal 'test_actor', response[:from]
  end

  def test_handle_state_messages
    # Test set state
    @actor.handle_message({ type: :set_state, state: { key: 'value' } })
    assert_equal({ key: 'value' }, @actor.state)
    
    # Test get state
    response = @actor.handle_message({ type: :get_state })
    assert_equal :state, response[:type]
    assert_equal({ key: 'value' }, response[:state])
  end

  def test_stop_actor
    assert_equal true, @actor.alive?
    @actor.stop
    assert_equal false, @actor.alive?
  end
end

class TestSupervisor < Test::Unit::TestCase
  def setup
    @actor_system = ActorSystem.new
    @supervisor = Supervisor.new('test_supervisor', @actor_system)
  end

  def test_supervisor_initialization
    assert_equal 'test_supervisor', @supervisor.id
    assert_equal [], @supervisor.instance_variable_get(:@children)
    assert_equal :one_for_one, @supervisor.instance_variable_get(:@restart_strategy)
  end

  def test_add_child
    @supervisor.add_child('child_1')
    @supervisor.add_child('child_2')
    
    children = @supervisor.instance_variable_get(:@children)
    assert_equal ['child_1', 'child_2'], children
  end

  def test_handle_child_failure
    @supervisor.add_child('failing_child')
    
    # This should trigger restart logic (mocked)
    assert_nothing_raised do
      @supervisor.handle_child_failure('failing_child', StandardError.new('Test error'))
    end
  end
end

class TestAdvancedConcurrencySystem < Test::Unit::TestCase
  def setup
    @system = AdvancedConcurrencySystem.new(thread_pool_size: 4)
  end

  def teardown
    @system.shutdown_all
  end

  def test_system_initialization
    assert_not_nil @system.concurrency_framework
    assert_not_nil @system.distributed_framework
    assert_not_nil @system.actor_system
  end

  def test_execute_concurrent_pipeline
    tasks = [
      -> { "Pipeline task 1" },
      -> { "Pipeline task 2" }
    ]
    
    results = @system.execute_concurrent_pipeline(tasks)
    
    assert_not_nil results[:threaded]
    assert_not_nil results[:actor_based]
    assert_not_nil results[:distributed]
  end

  def test_create_high_performance_computation
    data = [1, 2, 3, 4, 5]
    result = @system.create_high_performance_computation(data) { |x| x * x }
    
    assert_equal [1, 4, 9, 16, 25], result
  end
end

# Performance and Integration Tests
class TestPerformanceAndIntegration < Test::Unit::TestCase
  def test_concurrent_vs_sequential_performance
    tasks = 10.times.map { -> { sleep(0.01); "Task completed" } }
    
    # Sequential execution
    start_time = Time.now
    sequential_results = tasks.map(&:call)
    sequential_time = Time.now - start_time
    
    # Concurrent execution
    framework = ConcurrencyFramework.new(thread_pool_size: 10)
    start_time = Time.now
    concurrent_results = framework.execute_concurrent(tasks)
    concurrent_time = Time.now - start_time
    framework.shutdown
    
    # Concurrent should be significantly faster
    assert concurrent_time < sequential_time
    assert_equal sequential_results.length, concurrent_results.length
  end

  def test_parallel_processing_scalability
    data_sizes = [100, 1000, 10000]
    processor = ParallelProcessor.new
    
    data_sizes.each do |size|
      data = (1..size).to_a
      
      start_time = Time.now
      result = processor.process(data) { |x| x * x }
      end_time = Time.now
      
      assert_equal size, result.length
      assert (end_time - start_time) < 1.0  # Should complete within 1 second
    end
  end

  def test_actor_system_message_passing
    system = ActorSystem.new
    
    # Create multiple actors
    actors = 5.times.map { |i| system.spawn_actor(Actor, "actor_#{i}") }
    
    # Send messages to all actors
    actors.each do |actor_id|
      assert system.send_message(actor_id, { type: :ping })
    end
    
    # Verify system stats
    stats = system.get_actor_stats
    assert_equal 5, stats[:total_actors]
    
    # Cleanup
    actors.each { |actor_id| system.stop_actor(actor_id) }
  end

  def test_distributed_system_fault_tolerance
    framework = DistributedComputingFramework.new
    
    # Register nodes
    framework.register_node('node1', { host: 'localhost', port: 3001, capacity: 10 })
    framework.register_node('node2', { host: 'localhost', port: 3002, capacity: 10 })
    
    # Simulate node failure
    framework.nodes['node1'][:last_heartbeat] = Time.now - 60
    framework.check_node_health
    
    # Verify fault detection
    assert_equal :inactive, framework.nodes['node1'][:status]
    assert_equal :active, framework.nodes['node2'][:status]
    
    # Test task distribution still works with remaining nodes
    task = -> { "Fault tolerance test" }
    result = framework.distribute_task(task)
    assert_not_nil result
  end

  def test_end_to_end_integration
    system = AdvancedConcurrencySystem.new
    
    # Register distributed nodes
    system.distributed_framework.register_node('node1', { host: 'localhost', port: 3001 })
    
    # Create computation tasks
    computation_data = (1..100).to_a
    result = system.create_high_performance_computation(computation_data) { |x| Math.sqrt(x) }
    
    # Verify results
    assert_equal 100, result.length
    assert_equal 1.0, result[0]
    assert_equal 10.0, result[99]
    
    # Test concurrent pipeline
    tasks = [-> { "Task A" }, -> { "Task B" }]
    pipeline_results = system.execute_concurrent_pipeline(tasks)
    
    assert_not_nil pipeline_results[:threaded]
    assert_not_nil pipeline_results[:actor_based]
    assert_not_nil pipeline_results[:distributed]
    
    system.shutdown_all
  end
end

# Run all tests
if __FILE__ == $0
  puts "Running Goal 13 Tests..."
  puts "=" * 50
  
  # Run Test::Unit tests
  Test::Unit::AutoRunner.run
end 