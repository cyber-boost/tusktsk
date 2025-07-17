# frozen_string_literal: true

Gem::Specification.new do |spec|
  spec.name = 'peanut_config'
  spec.version = '1.0.0'
  spec.authors = ['Cyberboost LLC']
  spec.email = ['team@tusklang.org']

  spec.summary = 'Hierarchical configuration with binary compilation for TuskTsk'
  spec.description = <<~DESC
    PeanutConfig provides hierarchical configuration management with CSS-like inheritance
    and binary compilation for 85% performance improvement. Part of the TuskTsk ecosystem.
  DESC
  spec.homepage = 'https://tuskt.sk'
  spec.license = 'BBL'
  spec.required_ruby_version = '>= 3.0.0'

  spec.metadata['homepage_uri'] = spec.homepage
  spec.metadata['source_code_uri'] = 'https://github.com/cyber-boost/tusktsk'
  spec.metadata['changelog_uri'] = 'https://github.com/cyber-boost/tusktsk/blob/main/CHANGELOG.md'

  # Specify which files should be added to the gem when it is released.
  spec.files = Dir.chdir(__dir__) do
    `git ls-files -z`.split("\x0").reject do |f|
      (File.expand_path(f) == __FILE__) ||
        f.start_with?(*%w[bin/ test/ spec/ features/ .git .circleci appveyor Gemfile])
    end
  end
  spec.bindir = 'exe'
  spec.executables = spec.files.grep(%r{\Aexe/}) { |f| File.basename(f) }
  spec.require_paths = ['lib']

  # Runtime dependencies
  spec.add_dependency 'json', '~> 2.6'

  # Development dependencies
  spec.add_development_dependency 'rake', '~> 13.0'
  spec.add_development_dependency 'rspec', '~> 3.12'
  spec.add_development_dependency 'rubocop', '~> 1.50'
  spec.add_development_dependency 'rubocop-rspec', '~> 2.20'
  spec.add_development_dependency 'yard', '~> 0.9'

  # For more information and examples about making a new gem, check out our
  # guide at: https://bundler.io/guides/creating_gem.html
end