#!/usr/bin/env ruby

require 'concurrent-ruby'
require 'thread'
require 'fiber'
require 'socket'
require 'json'
require 'logger'
require 'securerandom'
require 'timeout'
require 'drb/drb'

# Advanced Concurrency and Parallelism Framework
class ConcurrencyFramework
  attr_reader :thread_pool, :async_executor, :parallel_processor

  def initialize(options = {})
    @thread_pool_size = options[:thread_pool_size] || 10
    @async_pool_size = options[:async_pool_size] || 5
    @logger = Logger.new(STDOUT)
    @logger.level = Logger::INFO
    
    initialize_thread_pool
    initialize_async_executor
    initialize_parallel_processor
  end

  private

  def initialize_thread_pool
    @thread_pool = Concurrent::FixedThreadPool.new(@thread_pool_size)
    @logger.info("Initialized thread pool with #{@thread_pool_size} threads")
  end

  def initialize_async_executor
    @async_executor = Concurrent::ThreadPoolExecutor.new(
      min_threads: 1,
      max_threads: @async_pool_size,
      max_queue: 100,
      fallback_policy: :caller_runs
    )
    @logger.info("Initialized async executor with #{@async_pool_size} max threads")
  end

  def initialize_parallel_processor
    @parallel_processor = ParallelProcessor.new
    @logger.info("Initialized parallel processor")
  end

  public

  def execute_concurrent(tasks)
    futures = tasks.map do |task|
      Concurrent::Future.execute(executor: @thread_pool) do
        task.call
      end
    end
    
    futures.map(&:value)
  end

  def execute_async(task)
    Concurrent::Promise.execute(executor: @async_executor) do
      task.call
    end
  end

  def execute_parallel(data, &block)
    @parallel_processor.process(data, &block)
  end

  def shutdown
    @thread_pool.shutdown
    @async_executor.shutdown
    @logger.info("Concurrency framework shutdown complete")
  end
end

# Multi-threading with Advanced Features
class ThreadManager
  def initialize
    @threads = {}
    @thread_groups = {}
    @mutex = Mutex.new
    @logger = Logger.new(STDOUT)
  end

  def create_thread(name, &block)
    @mutex.synchronize do
      @threads[name] = Thread.new do
        Thread.current[:name] = name
        begin
          block.call
        rescue => e
          @logger.error("Thread #{name} failed: #{e.message}")
          raise
        end
      end
    end
    @logger.info("Created thread: #{name}")
  end

  def create_thread_group(group_name, thread_count, &block)
    @mutex.synchronize do
      @thread_groups[group_name] = ThreadGroup.new
      
      thread_count.times do |i|
        thread_name = "#{group_name}_#{i}"
        thread = Thread.new do
          Thread.current[:name] = thread_name
          Thread.current[:group] = group_name
          block.call(i)
        end
        @thread_groups[group_name].add(thread)
        @threads[thread_name] = thread
      end
    end
    @logger.info("Created thread group: #{group_name} with #{thread_count} threads")
  end

  def join_all
    @threads.values.each(&:join)
  end

  def join_group(group_name)
    return unless @thread_groups[group_name]
    
    @thread_groups[group_name].list.each(&:join)
  end

  def kill_thread(name)
    @mutex.synchronize do
      if @threads[name]
        @threads[name].kill
        @threads.delete(name)
        @logger.info("Killed thread: #{name}")
      end
    end
  end

  def thread_status(name)
    @threads[name]&.status
  end

  def active_threads
    @threads.select { |_, thread| thread.alive? }.keys
  end
end

# Async I/O Implementation
class AsyncIOManager
  def initialize
    @io_operations = {}
    @completion_callbacks = {}
    @logger = Logger.new(STDOUT)
  end

  def async_read(file_path, callback = nil)
    operation_id = SecureRandom.uuid
    
    fiber = Fiber.new do
      begin
        content = File.read(file_path)
        @io_operations[operation_id] = { status: :completed, result: content }
        callback&.call(content)
        content
      rescue => e
        @io_operations[operation_id] = { status: :failed, error: e.message }
        @logger.error("Async read failed: #{e.message}")
        nil
      end
    end
    
    @io_operations[operation_id] = { status: :pending, fiber: fiber }
    fiber.resume
    operation_id
  end

  def async_write(file_path, content, callback = nil)
    operation_id = SecureRandom.uuid
    
    fiber = Fiber.new do
      begin
        File.write(file_path, content)
        @io_operations[operation_id] = { status: :completed, result: true }
        callback&.call(true)
        true
      rescue => e
        @io_operations[operation_id] = { status: :failed, error: e.message }
        @logger.error("Async write failed: #{e.message}")
        false
      end
    end
    
    @io_operations[operation_id] = { status: :pending, fiber: fiber }
    fiber.resume
    operation_id
  end

  def async_http_request(url, callback = nil)
    operation_id = SecureRandom.uuid
    
    fiber = Fiber.new do
      begin
        # Simulate HTTP request
        sleep(0.1) # Simulate network delay
        response = { status: 200, body: "Response from #{url}" }
        @io_operations[operation_id] = { status: :completed, result: response }
        callback&.call(response)
        response
      rescue => e
        @io_operations[operation_id] = { status: :failed, error: e.message }
        @logger.error("Async HTTP request failed: #{e.message}")
        nil
      end
    end
    
    @io_operations[operation_id] = { status: :pending, fiber: fiber }
    fiber.resume
    operation_id
  end

  def operation_status(operation_id)
    @io_operations[operation_id]
  end

  def wait_for_completion(operation_id, timeout = 10)
    start_time = Time.now
    while @io_operations[operation_id][:status] == :pending
      return false if Time.now - start_time > timeout
      sleep(0.01)
    end
    @io_operations[operation_id][:status] == :completed
  end
end

# Parallel Processing Engine
class ParallelProcessor
  def initialize(worker_count = nil)
    @worker_count = worker_count || Concurrent.processor_count
    @logger = Logger.new(STDOUT)
  end

  def process(data, &block)
    return [] if data.empty?
    
    chunk_size = (data.length.to_f / @worker_count).ceil
    chunks = data.each_slice(chunk_size).to_a
    
    futures = chunks.map do |chunk|
      Concurrent::Future.execute do
        chunk.map(&block)
      end
    end
    
    results = futures.map(&:value)
    results.flatten
  end

  def map_reduce(data, map_func, reduce_func, initial_value = nil)
    # Map phase
    mapped_results = process(data, &map_func)
    
    # Reduce phase
    if initial_value
      mapped_results.reduce(initial_value, &reduce_func)
    else
      mapped_results.reduce(&reduce_func)
    end
  end

  def parallel_each(data, &block)
    process(data, &block)
    nil
  end

  def parallel_select(data, &block)
    results = process(data) { |item| [item, block.call(item)] }
    results.select { |_, selected| selected }.map { |item, _| item }
  end

  def parallel_sort(data, &block)
    return data if data.length <= 1
    
    # Parallel merge sort implementation
    parallel_merge_sort(data, &block)
  end

  private

  def parallel_merge_sort(data, &block)
    return data if data.length <= 1
    
    mid = data.length / 2
    left_future = Concurrent::Future.execute { parallel_merge_sort(data[0...mid], &block) }
    right_future = Concurrent::Future.execute { parallel_merge_sort(data[mid..-1], &block) }
    
    merge(left_future.value, right_future.value, &block)
  end

  def merge(left, right, &block)
    result = []
    i, j = 0, 0
    
    while i < left.length && j < right.length
      if block ? block.call(left[i], right[j]) <= 0 : left[i] <= right[j]
        result << left[i]
        i += 1
      else
        result << right[j]
        j += 1
      end
    end
    
    result.concat(left[i..-1]) if i < left.length
    result.concat(right[j..-1]) if j < right.length
    result
  end
end

# Distributed Computing Framework
class DistributedComputingFramework
  attr_reader :nodes, :task_distributor, :load_balancer

  def initialize
    @nodes = {}
    @task_distributor = TaskDistributor.new
    @load_balancer = LoadBalancer.new
    @logger = Logger.new(STDOUT)
  end

  def register_node(node_id, config)
    @nodes[node_id] = {
      host: config[:host],
      port: config[:port],
      capacity: config[:capacity] || 10,
      current_load: 0,
      status: :active,
      last_heartbeat: Time.now
    }
    @logger.info("Registered node: #{node_id}")
  end

  def distribute_task(task, requirements = {})
    available_nodes = @nodes.select { |_, node| node[:status] == :active }
    return nil if available_nodes.empty?
    
    selected_node = @load_balancer.select_node(available_nodes, requirements)
    @task_distributor.send_task(selected_node, task)
  end

  def distribute_tasks(tasks, strategy = :round_robin)
    results = []
    
    case strategy
    when :round_robin
      tasks.each_with_index do |task, index|
        node_id = @nodes.keys[index % @nodes.length]
        result = distribute_task(task, { preferred_node: node_id })
        results << result
      end
    when :load_balanced
      tasks.each do |task|
        result = distribute_task(task)
        results << result
      end
    end
    
    results
  end

  def check_node_health
    @nodes.each do |node_id, node|
      if Time.now - node[:last_heartbeat] > 30
        node[:status] = :inactive
        @logger.warn("Node #{node_id} marked as inactive")
      end
    end
  end

  def heartbeat(node_id)
    if @nodes[node_id]
      @nodes[node_id][:last_heartbeat] = Time.now
      @nodes[node_id][:status] = :active
    end
  end

  def get_cluster_stats
    {
      total_nodes: @nodes.length,
      active_nodes: @nodes.count { |_, node| node[:status] == :active },
      total_capacity: @nodes.sum { |_, node| node[:capacity] },
      current_load: @nodes.sum { |_, node| node[:current_load] }
    }
  end
end

class TaskDistributor
  def initialize
    @logger = Logger.new(STDOUT)
  end

  def send_task(node, task)
    begin
      # Simulate task distribution
      @logger.info("Sending task to node: #{node}")
      
      # In a real implementation, this would use network protocols
      result = execute_remote_task(node, task)
      { status: :completed, result: result, node: node }
    rescue => e
      @logger.error("Task distribution failed: #{e.message}")
      { status: :failed, error: e.message, node: node }
    end
  end

  private

  def execute_remote_task(node, task)
    # Simulate remote task execution
    sleep(0.1)
    "Task executed on #{node}"
  end
end

class LoadBalancer
  def initialize
    @last_selected = {}
  end

  def select_node(nodes, requirements = {})
    strategy = requirements[:strategy] || :least_loaded
    
    case strategy
    when :round_robin
      select_round_robin(nodes)
    when :least_loaded
      select_least_loaded(nodes)
    when :random
      nodes.keys.sample
    else
      select_least_loaded(nodes)
    end
  end

  private

  def select_round_robin(nodes)
    @last_selected[:round_robin] ||= 0
    node_keys = nodes.keys
    selected = node_keys[@last_selected[:round_robin] % node_keys.length]
    @last_selected[:round_robin] += 1
    selected
  end

  def select_least_loaded(nodes)
    nodes.min_by { |_, node| node[:current_load] }&.first
  end
end

# Actor Model Implementation
class ActorSystem
  def initialize
    @actors = {}
    @mailboxes = {}
    @supervisors = {}
    @logger = Logger.new(STDOUT)
  end

  def spawn_actor(actor_class, name = nil, supervisor = nil)
    actor_id = name || SecureRandom.uuid
    actor = actor_class.new(actor_id, self)
    
    @actors[actor_id] = actor
    @mailboxes[actor_id] = Queue.new
    
    if supervisor
      @supervisors[actor_id] = supervisor
      supervisor.add_child(actor_id)
    end
    
    start_actor_loop(actor_id)
    @logger.info("Spawned actor: #{actor_id}")
    actor_id
  end

  def send_message(actor_id, message)
    if @mailboxes[actor_id]
      @mailboxes[actor_id] << message
      true
    else
      @logger.error("Actor not found: #{actor_id}")
      false
    end
  end

  def stop_actor(actor_id)
    if @actors[actor_id]
      @actors[actor_id].stop
      # Give actor thread time to finish before cleaning up
      sleep(0.01)
      @actors.delete(actor_id)
      @mailboxes.delete(actor_id)
      @supervisors.delete(actor_id)
      @logger.info("Stopped actor: #{actor_id}")
    end
  end

  def get_actor_stats
    {
      total_actors: @actors.length,
      active_actors: @actors.count { |_, actor| actor.alive? },
      total_messages: @mailboxes.sum { |_, mailbox| mailbox.length }
    }
  end

  private

  def start_actor_loop(actor_id)
    Thread.new do
      actor = @actors[actor_id]
      mailbox = @mailboxes[actor_id]
      
      while actor && actor.alive?
        begin
          message = mailbox.pop(true) # Non-blocking pop
          actor.handle_message(message) if message && actor
        rescue ThreadError
          # No messages, continue loop
          sleep(0.001)
        rescue => e
          @logger.error("Actor #{actor_id} error: #{e.message}")
          handle_actor_failure(actor_id, e)
          break
        end
      end
    end
  end

  def handle_actor_failure(actor_id, error)
    supervisor = @supervisors[actor_id]
    if supervisor
      supervisor.handle_child_failure(actor_id, error)
    else
      @logger.error("Unhandled actor failure: #{actor_id} - #{error.message}")
    end
  end
end

class Actor
  attr_reader :id, :system
  attr_accessor :state

  def initialize(id, system)
    @id = id
    @system = system
    @state = {}
    @alive = true
  end

  def alive?
    @alive
  end

  def stop
    @alive = false
  end

  def send(actor_id, message)
    @system.send_message(actor_id, message)
  end

  def handle_message(message)
    # Override in subclasses
    case message[:type]
    when :ping
      { type: :pong, from: @id }
    when :get_state
      { type: :state, state: @state, from: @id }
    when :set_state
      @state = message[:state]
      { type: :ack, from: @id }
    else
      { type: :unknown_message, from: @id }
    end
  end
end

class Supervisor < Actor
  def initialize(id, system)
    super
    @children = []
    @restart_strategy = :one_for_one
  end

  def add_child(child_id)
    @children << child_id
  end

  def handle_child_failure(child_id, error)
    case @restart_strategy
    when :one_for_one
      restart_child(child_id)
    when :one_for_all
      restart_all_children
    end
  end

  private

  def restart_child(child_id)
    @system.stop_actor(child_id)
    # In a real implementation, would respawn with original configuration
    @system.logger.info("Would restart child: #{child_id}")
  end

  def restart_all_children
    @children.each { |child_id| restart_child(child_id) }
  end
end

# Main Framework Integration
class AdvancedConcurrencySystem
  attr_reader :concurrency_framework, :distributed_framework, :actor_system

  def initialize(options = {})
    @concurrency_framework = ConcurrencyFramework.new(options)
    @distributed_framework = DistributedComputingFramework.new
    @actor_system = ActorSystem.new
    @thread_manager = ThreadManager.new
    @async_io = AsyncIOManager.new
    @logger = Logger.new(STDOUT)
    
    @logger.info("Advanced Concurrency System initialized")
  end

  def execute_concurrent_pipeline(tasks)
    # Execute tasks using different concurrency models
    results = {}
    
    # Thread-based execution
    results[:threaded] = @concurrency_framework.execute_concurrent(tasks)
    
    # Actor-based execution
    results[:actor_based] = execute_with_actors(tasks)
    
    # Distributed execution
    results[:distributed] = @distributed_framework.distribute_tasks(tasks)
    
    results
  end

  def create_high_performance_computation(data, computation)
    # Combine multiple concurrency approaches for maximum performance
    parallel_result = @concurrency_framework.execute_parallel(data, &computation)
    
    # Use actors for state management
    state_actor = @actor_system.spawn_actor(Actor, 'computation_state')
    @actor_system.send_message(state_actor, { 
      type: :set_state, 
      state: { result: parallel_result, timestamp: Time.now } 
    })
    
    parallel_result
  end

  def shutdown_all
    @concurrency_framework.shutdown
    @logger.info("Advanced Concurrency System shutdown complete")
  end

  private

  def execute_with_actors(tasks)
    results = []
    
    tasks.each_with_index do |task, index|
      actor_id = @actor_system.spawn_actor(Actor, "task_actor_#{index}")
      @actor_system.send_message(actor_id, { type: :execute, task: task })
      results << "Actor #{actor_id} processing task"
    end
    
    results
  end
end

# Example Usage and Demonstration
if __FILE__ == $0
  puts "Ruby Advanced Concurrency & Parallelism Framework - Goal 13 Implementation"
  puts "=" * 80

  # Create the advanced concurrency system
  system = AdvancedConcurrencySystem.new(thread_pool_size: 8, async_pool_size: 4)

  # Example 1: Concurrent task execution
  puts "\n1. Concurrent Task Execution:"
  tasks = [
    -> { sleep(0.1); "Task 1 completed" },
    -> { sleep(0.1); "Task 2 completed" },
    -> { sleep(0.1); "Task 3 completed" }
  ]
  
  start_time = Time.now
  results = system.concurrency_framework.execute_concurrent(tasks)
  end_time = Time.now
  
  puts "Results: #{results}"
  puts "Execution time: #{(end_time - start_time).round(3)}s"

  # Example 2: Parallel data processing
  puts "\n2. Parallel Data Processing:"
  data = (1..100).to_a
  result = system.concurrency_framework.execute_parallel(data) { |x| x * x }
  puts "Processed #{data.length} items, first 10 results: #{result[0..9]}"

  # Example 3: Distributed computing
  puts "\n3. Distributed Computing Setup:"
  system.distributed_framework.register_node('node1', { host: 'localhost', port: 3001, capacity: 10 })
  system.distributed_framework.register_node('node2', { host: 'localhost', port: 3002, capacity: 15 })
  
  cluster_stats = system.distributed_framework.get_cluster_stats
  puts "Cluster stats: #{cluster_stats}"

  # Example 4: Actor system
  puts "\n4. Actor System:"
  actor_id = system.actor_system.spawn_actor(Actor, 'demo_actor')
  system.actor_system.send_message(actor_id, { type: :ping })
  
  actor_stats = system.actor_system.get_actor_stats
  puts "Actor system stats: #{actor_stats}"

  # Example 5: High-performance computation
  puts "\n5. High-Performance Computation:"
  computation_data = (1..1000).to_a
  result = system.create_high_performance_computation(computation_data) { |x| Math.sqrt(x) }
  puts "Computed square roots for #{computation_data.length} numbers"
  puts "Sample results: #{result[0..4].map { |x| x.round(3) }}"

  # Shutdown
  system.shutdown_all
  
  puts "\nGoal 13 Implementation Complete!"
  puts "Advanced Concurrency & Parallelism Framework Ready!"
end 