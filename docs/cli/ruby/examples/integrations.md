# Framework Integrations

Integration examples for popular Ruby frameworks.

## Rails Integration

```ruby
# config/application.rb
class Application < Rails::Application
  # Load TuskLang configuration
  config_loader = PeanutConfig.new
  app_config = config_loader.load(Rails.root.join('config'))
  
  # Apply configuration
  config.app_name = app_config['app']['name']
  config.debug_mode = app_config['app']['debug']
end
```

## Jekyll Integration

```ruby
# _plugins/tusk_lang.rb
Jekyll::Hooks.register :site, :after_init do |site|
  config = TuskLang::TSK.from_file('_config.tsk')
  site.config.merge!(config.to_hash)
end
```

## Sinatra Integration

```ruby
# app.rb
require 'sinatra'
require 'tusk_lang'

class MyApp < Sinatra::Base
  configure do
    config = TuskLang::TSK.from_file('config.tsk')
    set :port, config.get_value('server', 'port')
  end
end
```
