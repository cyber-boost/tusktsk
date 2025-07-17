# frozen_string_literal: true

require_relative "lib/tusk_lang/version"

Gem::Specification.new do |spec|
  spec.name = "tusktsk"
  spec.version = TuskLang::VERSION
  spec.authors = ["Cyberboost LLC"]
  spec.email = ["packages@tuskt.sk"]

  spec.summary = "💎 TuskTsk Enhanced - The Freedom Configuration Language for Ruby"
  spec.description = "Ruby SDK with maximum syntax flexibility. Support all syntax styles [], {}, <> with global variables, cross-file communication, and database queries. Perfect for Rails, Jekyll, and DevOps automation."
  spec.homepage = "https://tuskt.sk"
  spec.license = "Nonstandard"
  spec.required_ruby_version = ">= 2.7.0"

  spec.metadata["homepage_uri"] = spec.homepage
  spec.metadata["source_code_uri"] = "https://github.com/cyber-boost/tusktsk"
  spec.metadata["changelog_uri"] = "https://github.com/cyber-boost/tusktsk/blob/main/CHANGELOG.md"
  spec.metadata["documentation_uri"] = "https://tuskt.sk/docs/ruby"
  spec.metadata["bug_tracker_uri"] = "https://github.com/cyber-boost/tusktsk/issues"
  spec.metadata["license_uri"] = "https://tuskt.sk/license"
  spec.metadata["rubygems_mfa_required"] = "true"

  # Specify which files should be added to the gem when it is released.
  spec.files = Dir.glob("{lib,cli,exe}/**/*") + %w[README.md LICENSE CHANGELOG.md]
  spec.bindir = "exe"
  spec.executables = ["tsk"]
  spec.require_paths = ["lib"]

  # Runtime dependencies
  spec.add_dependency "json", "~> 2.6"
  spec.add_dependency "sqlite3", "~> 1.6"
  spec.add_dependency "webrick", "~> 1.8"
  spec.add_dependency "optparse", "~> 0.1"

  # Optional dependencies for advanced features
  spec.add_development_dependency "dalli", "~> 3.2"  # For Memcached support
  spec.add_development_dependency "redis", "~> 5.0"  # For Redis support
  spec.add_development_dependency "httparty", "~> 0.21"  # For HTTP requests
  spec.add_development_dependency "msgpack", "~> 1.7"  # For binary serialization

  # Development dependencies
  spec.add_development_dependency "rake", "~> 13.0"
  spec.add_development_dependency "rspec", "~> 3.0"
  spec.add_development_dependency "rubocop", "~> 1.21"
  spec.add_development_dependency "yard", "~> 0.9"
  spec.add_development_dependency "simplecov", "~> 0.21"
end 