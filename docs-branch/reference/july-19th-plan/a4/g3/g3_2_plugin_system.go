package main

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"path/filepath"
	"plugin"
	"sync"
)

// PluginInterface defines the contract that all plugins must implement
type PluginInterface interface {
	Name() string
	Version() string
	Description() string
	Execute(args map[string]interface{}) (interface{}, error)
	Validate(args map[string]interface{}) error
}

// PluginMetadata contains information about a plugin
type PluginMetadata struct {
	Name        string                 `json:"name"`
	Version     string                 `json:"version"`
	Description string                 `json:"description"`
	Author      string                 `json:"author"`
	Tags        []string               `json:"tags"`
	Config      map[string]interface{} `json:"config"`
}

// Plugin represents a loaded plugin instance
type Plugin struct {
	Path     string
	Metadata PluginMetadata
	Instance PluginInterface
	Enabled  bool
}

// PluginManager manages the loading and execution of plugins
type PluginManager struct {
	mu      sync.RWMutex
	plugins map[string]*Plugin
	config  map[string]interface{}
}

// NewPluginManager creates a new plugin manager instance
func NewPluginManager() *PluginManager {
	return &PluginManager{
		plugins: make(map[string]*Plugin),
		config:  make(map[string]interface{}),
	}
}

// LoadPlugin loads a plugin from the specified path
func (pm *PluginManager) LoadPlugin(pluginPath string) error {
	pm.mu.Lock()
	defer pm.mu.Unlock()

	// Check if plugin already loaded
	if _, exists := pm.plugins[pluginPath]; exists {
		return fmt.Errorf("plugin already loaded: %s", pluginPath)
	}

	// Open the plugin
	p, err := plugin.Open(pluginPath)
	if err != nil {
		return fmt.Errorf("failed to open plugin %s: %v", pluginPath, err)
	}

	// Look up the plugin symbol
	symbol, err := p.Lookup("Plugin")
	if err != nil {
		return fmt.Errorf("plugin symbol not found in %s: %v", pluginPath, err)
	}

	// Type assert to PluginInterface
	pluginInstance, ok := symbol.(PluginInterface)
	if !ok {
		return fmt.Errorf("plugin does not implement PluginInterface: %s", pluginPath)
	}

	// Create plugin instance
	plugin := &Plugin{
		Path:     pluginPath,
		Instance: pluginInstance,
		Enabled:  true,
		Metadata: PluginMetadata{
			Name:        pluginInstance.Name(),
			Version:     pluginInstance.Version(),
			Description: pluginInstance.Description(),
		},
	}

	pm.plugins[pluginPath] = plugin
	return nil
}

// LoadPluginsFromDirectory loads all plugins from a directory
func (pm *PluginManager) LoadPluginsFromDirectory(dirPath string) error {
	files, err := ioutil.ReadDir(dirPath)
	if err != nil {
		return fmt.Errorf("failed to read directory %s: %v", dirPath, err)
	}

	var errors []string
	for _, file := range files {
		if filepath.Ext(file.Name()) == ".so" {
			pluginPath := filepath.Join(dirPath, file.Name())
			if err := pm.LoadPlugin(pluginPath); err != nil {
				errors = append(errors, fmt.Sprintf("failed to load %s: %v", file.Name(), err))
			}
		}
	}

	if len(errors) > 0 {
		return fmt.Errorf("some plugins failed to load: %v", errors)
	}

	return nil
}

// ExecutePlugin executes a plugin with the given arguments
func (pm *PluginManager) ExecutePlugin(pluginPath string, args map[string]interface{}) (interface{}, error) {
	pm.mu.RLock()
	plugin, exists := pm.plugins[pluginPath]
	pm.mu.RUnlock()

	if !exists {
		return nil, fmt.Errorf("plugin not found: %s", pluginPath)
	}

	if !plugin.Enabled {
		return nil, fmt.Errorf("plugin is disabled: %s", pluginPath)
	}

	// Validate arguments
	if err := plugin.Instance.Validate(args); err != nil {
		return nil, fmt.Errorf("plugin validation failed: %v", err)
	}

	// Execute plugin
	return plugin.Instance.Execute(args)
}

// ExecutePluginByName executes a plugin by name
func (pm *PluginManager) ExecutePluginByName(name string, args map[string]interface{}) (interface{}, error) {
	pm.mu.RLock()
	defer pm.mu.RUnlock()

	for _, plugin := range pm.plugins {
		if plugin.Metadata.Name == name && plugin.Enabled {
			// Validate arguments
			if err := plugin.Instance.Validate(args); err != nil {
				return nil, fmt.Errorf("plugin validation failed: %v", err)
			}

			// Execute plugin
			return plugin.Instance.Execute(args)
		}
	}

	return nil, fmt.Errorf("plugin not found: %s", name)
}

// ListPlugins returns information about all loaded plugins
func (pm *PluginManager) ListPlugins() []PluginMetadata {
	pm.mu.RLock()
	defer pm.mu.RUnlock()

	var plugins []PluginMetadata
	for _, plugin := range pm.plugins {
		plugins = append(plugins, plugin.Metadata)
	}

	return plugins
}

// EnablePlugin enables a plugin
func (pm *PluginManager) EnablePlugin(pluginPath string) error {
	pm.mu.Lock()
	defer pm.mu.Unlock()

	plugin, exists := pm.plugins[pluginPath]
	if !exists {
		return fmt.Errorf("plugin not found: %s", pluginPath)
	}

	plugin.Enabled = true
	return nil
}

// DisablePlugin disables a plugin
func (pm *PluginManager) DisablePlugin(pluginPath string) error {
	pm.mu.Lock()
	defer pm.mu.Unlock()

	plugin, exists := pm.plugins[pluginPath]
	if !exists {
		return fmt.Errorf("plugin not found: %s", pluginPath)
	}

	plugin.Enabled = false
	return nil
}

// UnloadPlugin unloads a plugin
func (pm *PluginManager) UnloadPlugin(pluginPath string) error {
	pm.mu.Lock()
	defer pm.mu.Unlock()

	if _, exists := pm.plugins[pluginPath]; !exists {
		return fmt.Errorf("plugin not found: %s", pluginPath)
	}

	delete(pm.plugins, pluginPath)
	return nil
}

// GetPluginInfo returns detailed information about a plugin
func (pm *PluginManager) GetPluginInfo(pluginPath string) (*Plugin, error) {
	pm.mu.RLock()
	defer pm.mu.RUnlock()

	plugin, exists := pm.plugins[pluginPath]
	if !exists {
		return nil, fmt.Errorf("plugin not found: %s", pluginPath)
	}

	return plugin, nil
}

// SavePluginConfig saves plugin configuration to a file
func (pm *PluginManager) SavePluginConfig(filename string) error {
	pm.mu.RLock()
	config := make(map[string]interface{})
	for path, plugin := range pm.plugins {
		config[path] = map[string]interface{}{
			"enabled":  plugin.Enabled,
			"metadata": plugin.Metadata,
		}
	}
	pm.mu.RUnlock()

	data, err := json.MarshalIndent(config, "", "  ")
	if err != nil {
		return fmt.Errorf("failed to marshal config: %v", err)
	}

	return ioutil.WriteFile(filename, data, 0644)
}

// LoadPluginConfig loads plugin configuration from a file
func (pm *PluginManager) LoadPluginConfig(filename string) error {
	data, err := ioutil.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read config file: %v", err)
	}

	var config map[string]interface{}
	if err := json.Unmarshal(data, &config); err != nil {
		return fmt.Errorf("failed to unmarshal config: %v", err)
	}

	pm.mu.Lock()
	defer pm.mu.Unlock()

	for path, pluginData := range config {
		if plugin, exists := pm.plugins[path]; exists {
			if pluginConfig, ok := pluginData.(map[string]interface{}); ok {
				if enabled, ok := pluginConfig["enabled"].(bool); ok {
					plugin.Enabled = enabled
				}
			}
		}
	}

	return nil
}

// Example plugin implementation
type ExamplePlugin struct{}

func (ep *ExamplePlugin) Name() string {
	return "example_plugin"
}

func (ep *ExamplePlugin) Version() string {
	return "1.0.0"
}

func (ep *ExamplePlugin) Description() string {
	return "An example plugin for demonstration"
}

func (ep *ExamplePlugin) Validate(args map[string]interface{}) error {
	if _, ok := args["input"]; !ok {
		return fmt.Errorf("input parameter is required")
	}
	return nil
}

func (ep *ExamplePlugin) Execute(args map[string]interface{}) (interface{}, error) {
	input, ok := args["input"].(string)
	if !ok {
		return nil, fmt.Errorf("input must be a string")
	}

	result := map[string]interface{}{
		"processed_input": fmt.Sprintf("Processed: %s", input),
		"length":          len(input),
		"uppercase":       fmt.Sprintf("%s", input),
	}

	return result, nil
}

// Example usage
func main() {
	// Create plugin manager
	pm := NewPluginManager()

	// Example of how plugins would be loaded (in real scenario, these would be compiled .so files)
	fmt.Println("Plugin System Demo")
	fmt.Println("==================")

	// Simulate loading a plugin
	examplePlugin := &ExamplePlugin{}
	
	// In a real scenario, you would load from .so files:
	// err := pm.LoadPlugin("./plugins/example_plugin.so")
	
	fmt.Printf("Plugin loaded: %s v%s\n", examplePlugin.Name(), examplePlugin.Version())
	fmt.Printf("Description: %s\n", examplePlugin.Description())

	// Execute plugin
	args := map[string]interface{}{
		"input": "Hello, Plugin System!",
	}

	result, err := examplePlugin.Execute(args)
	if err != nil {
		fmt.Printf("Error executing plugin: %v\n", err)
		return
	}

	fmt.Printf("Plugin result: %+v\n", result)

	// Save configuration
	if err := pm.SavePluginConfig("plugin_config.json"); err != nil {
		fmt.Printf("Error saving config: %v\n", err)
	}
} 