# frozen_string_literal: true

require_relative "lib/tusk_lang/version"

Gem::Specification.new do |spec|
  spec.name = "tusk_lang"
  spec.version = TuskLang::VERSION
  spec.authors = ["Bernard Stepgen Gengel II"]
  spec.email = ["hello@tuskt.sk"]

  spec.summary = "TuskLang - Smart configuration language for Ruby"
  spec.description = "Write intelligent configuration files with TuskLang's powerful @ operators and dynamic features"
  spec.homepage = "https://tuskt.sk"
  spec.license = "BBL-1.0"
  spec.required_ruby_version = ">= 3.0.0"

  spec.metadata["allowed_push_host"] = "https://rubygems.org"
  spec.metadata["homepage_uri"] = spec.homepage
  spec.metadata["source_code_uri"] = "https://github.com/cyber-boost/tusktsk"
  spec.metadata["changelog_uri"] = "https://github.com/cyber-boost/tusktsk/blob/main/CHANGELOG.md"
  spec.metadata["bug_tracker_uri"] = "https://github.com/cyber-boost/tusktsk/issues"
  spec.metadata["documentation_uri"] = "https://docs.tuskt.sk"

  spec.files = Dir.glob("{bin,lib}/**/*") + %w[README.md LICENSE CHANGELOG.md]
  spec.bindir = "exe"
  spec.executables = spec.files.grep(%r{\Aexe/}) { |f| File.basename(f) }
  spec.require_paths = ["lib"]

  spec.add_dependency "yaml", "~> 0.2"
  spec.add_dependency "thor", "~> 1.2"
  spec.add_dependency "colorize", "~> 1.1"

  spec.add_development_dependency "bundler", "~> 2.4"
  spec.add_development_dependency "rake", "~> 13.0"
  spec.add_development_dependency "rspec", "~> 3.12"
  spec.add_development_dependency "rubocop", "~> 1.50"
  spec.add_development_dependency "rubocop-rspec", "~> 2.20"
  spec.add_development_dependency "simplecov", "~> 0.22"
end 