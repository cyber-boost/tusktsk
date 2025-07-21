#!/usr/bin/env ruby
require_relative 'goal_implementation'
require 'test/unit'

class TestDevOpsFramework < Test::Unit::TestCase
  def setup
    @devops = UnifiedDevOpsSystem.new
  end

  def test_dockerfile_creation
    @devops.orchestrator.create_dockerfile('myapp', 'ruby:3.2')
    containers = @devops.orchestrator.instance_variable_get(:@containers)
    
    assert_not_nil containers['myapp']
    assert_includes containers['myapp'], 'FROM ruby:3.2'
    assert_includes containers['myapp'], 'COPY . /app'
    assert_includes containers['myapp'], 'WORKDIR /app'
    assert_includes containers['myapp'], 'RUN bundle install'
    assert_includes containers['myapp'], 'CMD ruby app.rb'
  end

  def test_kubernetes_deployment
    @devops.orchestrator.kubernetes_deploy('webapp', 5)
    deployments = @devops.orchestrator.instance_variable_get(:@deployments)
    
    deployment = deployments['webapp']
    assert_not_nil deployment
    assert_equal 'apps/v1', deployment[:apiVersion]
    assert_equal 'Deployment', deployment[:kind]
    assert_equal 'webapp', deployment[:metadata][:name]
    assert_equal 5, deployment[:spec][:replicas]
    assert_equal 'webapp', deployment[:spec][:selector][:matchLabels][:app]
  end

  def test_service_mesh_creation
    @devops.orchestrator.service_mesh('api-service')
    services = @devops.orchestrator.instance_variable_get(:@services)
    
    service = services['api-service']
    assert_not_nil service
    assert_equal 'v1', service[:apiVersion]
    assert_equal 'Service', service[:kind]
    assert_equal 'api-service', service[:metadata][:name]
    assert_equal 'api-service', service[:spec][:selector][:app]
    assert_equal 80, service[:spec][:ports][0][:port]
    assert_equal 3000, service[:spec][:ports][0][:targetPort]
    assert_equal 'LoadBalancer', service[:spec][:type]
  end

  def test_terraform_configuration
    config = { provider: 'aws', region: 'us-west-2', instance_type: 't3.micro' }
    @devops.iac.terraform_config('web_server', config)
    
    terraform = @devops.iac.instance_variable_get(:@terraform)
    assert_equal config, terraform['web_server']
  end

  def test_ansible_playbook
    tasks = [
      { name: 'Install nginx', apt: { name: 'nginx', state: 'present' } },
      { name: 'Start nginx', service: { name: 'nginx', state: 'started' } }
    ]
    
    @devops.iac.ansible_playbook('nginx_setup', tasks)
    ansible = @devops.iac.instance_variable_get(:@ansible)
    
    playbook = ansible['nginx_setup']
    assert_not_nil playbook
    assert_equal 'all', playbook[:hosts]
    assert_equal tasks, playbook[:tasks]
  end

  def test_infrastructure_provisioning
    @devops.iac.terraform_config('database', { type: 'postgresql' })
    @devops.iac.terraform_config('loadbalancer', { type: 'alb' })
    
    # Should not raise errors
    assert_nothing_raised do
      @devops.iac.provision_infrastructure
    end
  end

  def test_github_actions_pipeline
    steps = [
      { uses: 'actions/checkout@v2' },
      { run: 'bundle install' },
      { run: 'rspec' },
      { run: 'docker build -t myapp .' }
    ]
    
    @devops.cicd.github_actions('myapp_pipeline', steps)
    pipelines = @devops.cicd.instance_variable_get(:@pipelines)
    
    pipeline = pipelines['myapp_pipeline']
    assert_not_nil pipeline
    assert_equal 'myapp_pipeline', pipeline[:name]
    assert_equal ['push'], pipeline[:on]
    assert_equal 'ubuntu-latest', pipeline[:jobs][:build][:runs_on]
    assert_equal steps, pipeline[:jobs][:build][:steps]
  end

  def test_pipeline_stages
    @devops.cicd.add_stage('deploy_pipeline', 'test', 'rspec --format json')
    @devops.cicd.add_stage('deploy_pipeline', 'build', 'docker build -t app .')
    @devops.cicd.add_stage('deploy_pipeline', 'deploy', 'kubectl apply -f k8s/')
    
    stages = @devops.cicd.instance_variable_get(:@stages)
    pipeline_stages = stages['deploy_pipeline']
    
    assert_equal 3, pipeline_stages.length
    assert_equal 'test', pipeline_stages[0][:name]
    assert_equal 'rspec --format json', pipeline_stages[0][:run]
    assert_equal 'build', pipeline_stages[1][:name]
    assert_equal 'deploy', pipeline_stages[2][:name]
  end

  def test_pipeline_deployment
    @devops.cicd.github_actions('test_pipeline', [{ run: 'echo "test"' }])
    
    # Should not raise errors
    assert_nothing_raised do
      @devops.cicd.deploy_pipeline('test_pipeline')
    end
  end

  def test_full_deployment_integration
    app_name = 'fullstack_app'
    
    # Should create dockerfile, k8s deployment, and CI/CD pipeline
    assert_nothing_raised do
      @devops.full_deployment(app_name)
    end
    
    # Verify dockerfile was created
    containers = @devops.orchestrator.instance_variable_get(:@containers)
    assert_not_nil containers[app_name]
    
    # Verify k8s deployment was created
    deployments = @devops.orchestrator.instance_variable_get(:@deployments)
    assert_not_nil deployments[app_name]
    
    # Verify CI/CD pipeline was created
    pipelines = @devops.cicd.instance_variable_get(:@pipelines)
    assert_not_nil pipelines[app_name]
  end

  def test_multiple_environment_deployments
    environments = ['development', 'staging', 'production']
    
    environments.each do |env|
      @devops.orchestrator.kubernetes_deploy("myapp-#{env}", env == 'production' ? 5 : 2)
    end
    
    deployments = @devops.orchestrator.instance_variable_get(:@deployments)
    assert_equal 5, deployments['myapp-production'][:spec][:replicas]
    assert_equal 2, deployments['myapp-staging'][:spec][:replicas]
    assert_equal 2, deployments['myapp-development'][:spec][:replicas]
  end

  def test_container_orchestrator_state_management
    assert_empty @devops.orchestrator.instance_variable_get(:@containers)
    assert_empty @devops.orchestrator.instance_variable_get(:@services)
    assert_empty @devops.orchestrator.instance_variable_get(:@deployments)
    
    @devops.orchestrator.create_dockerfile('test', 'node:16')
    @devops.orchestrator.kubernetes_deploy('test', 1)
    @devops.orchestrator.service_mesh('test')
    
    refute_empty @devops.orchestrator.instance_variable_get(:@containers)
    refute_empty @devops.orchestrator.instance_variable_get(:@services)
    refute_empty @devops.orchestrator.instance_variable_get(:@deployments)
  end
end

if __FILE__ == $0
  puts "🔥 RUNNING G16 PRODUCTION TESTS..."
  Test::Unit::AutoRunner.run
end 