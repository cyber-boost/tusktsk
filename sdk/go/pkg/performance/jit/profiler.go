package jit

import (
	"fmt"
	"runtime"
	"runtime/pprof"
	"runtime/trace"
	"sync"
	"time"
)

// Profiler provides comprehensive performance profiling for JIT compilation
type Profiler struct {
	mu           sync.RWMutex
	profiles     map[string]*ProfileData
	activeTraces map[string]*traceData
	stats        *ProfilerStats
	enabled      bool
}

// ProfileData contains profiling information for a specific function or path
type ProfileData struct {
	ID              string
	Name            string
	CallCount       int64
	TotalTime       time.Duration
	AverageTime     time.Duration
	MinTime         time.Duration
	MaxTime         time.Duration
	MemoryUsage     uint64
	CPUUsage        float64
	GoroutineCount  int
	HotPath         bool
	OptimizationScore float64
	LastProfile     time.Time
}

// traceData tracks active trace sessions
type traceData struct {
	ID       string
	Start    time.Time
	File     string
	Enabled  bool
}

// ProfilerStats tracks overall profiler performance
type ProfilerStats struct {
	TotalProfiles    int64
	ActiveTraces     int64
	ProfilingTime    time.Duration
	MemoryProfiled   uint64
	HotPathsDetected int64
	Optimizations    int64
}

// NewProfiler creates a new profiler instance
func NewProfiler() *Profiler {
	return &Profiler{
		profiles:     make(map[string]*ProfileData),
		activeTraces: make(map[string]*traceData),
		stats:        &ProfilerStats{},
		enabled:      true,
	}
}

// StartProfile begins profiling for a specific function or path
func (p *Profiler) StartProfile(id, name string) *ProfileSession {
	if !p.enabled {
		return &ProfileSession{profiler: p, id: id, enabled: false}
	}

	p.mu.Lock()
	profile, exists := p.profiles[id]
	if !exists {
		profile = &ProfileData{
			ID:         id,
			Name:       name,
			MinTime:    time.Hour, // Initialize to a large value
			LastProfile: time.Now(),
		}
		p.profiles[id] = profile
		p.stats.TotalProfiles++
	}
	p.mu.Unlock()

	return &ProfileSession{
		profiler: p,
		id:       id,
		start:    time.Now(),
		enabled:  true,
	}
}

// ProfileSession represents an active profiling session
type ProfileSession struct {
	profiler *Profiler
	id       string
	start    time.Time
	enabled  bool
}

// End completes the profiling session
func (ps *ProfileSession) End() {
	if !ps.enabled {
		return
	}

	duration := time.Since(ps.start)
	
	ps.profiler.mu.Lock()
	defer ps.profiler.mu.Unlock()
	
	profile, exists := ps.profiler.profiles[ps.id]
	if !exists {
		return
	}
	
	// Update profile statistics
	profile.CallCount++
	profile.TotalTime += duration
	profile.AverageTime = profile.TotalTime / time.Duration(profile.CallCount)
	
	if duration < profile.MinTime {
		profile.MinTime = duration
	}
	if duration > profile.MaxTime {
		profile.MaxTime = duration
	}
	
	// Update memory usage
	var m runtime.MemStats
	runtime.ReadMemStats(&m)
	profile.MemoryUsage = m.Alloc
	
	// Update goroutine count
	profile.GoroutineCount = runtime.NumGoroutine()
	
	// Check if this is a hot path
	if profile.CallCount >= 100 && !profile.HotPath {
		profile.HotPath = true
		ps.profiler.stats.HotPathsDetected++
	}
	
	// Calculate optimization score
	profile.OptimizationScore = ps.calculateOptimizationScore(profile)
	profile.LastProfile = time.Now()
}

// calculateOptimizationScore calculates how much optimization potential exists
func (ps *ProfileSession) calculateOptimizationScore(profile *ProfileData) float64 {
	// Base score on call frequency and execution time
	frequencyScore := float64(profile.CallCount) / 1000.0 // Normalize to 0-1
	timeScore := float64(profile.AverageTime) / float64(time.Millisecond) // Normalize to ms
	
	// Higher scores for frequently called, slow functions
	return frequencyScore * timeScore
}

// StartTrace begins a trace session
func (p *Profiler) StartTrace(id string) error {
	if !p.enabled {
		return nil
	}

	p.mu.Lock()
	defer p.mu.Unlock()
	
	if _, exists := p.activeTraces[id]; exists {
		return fmt.Errorf("trace %s already active", id)
	}
	
	traceFile := fmt.Sprintf("/tmp/tusk_jit_trace_%s.trace", id)
	
	// Start trace
	if err := trace.Start(traceFile); err != nil {
		return fmt.Errorf("failed to start trace: %v", err)
	}
	
	p.activeTraces[id] = &traceData{
		ID:      id,
		Start:   time.Now(),
		File:    traceFile,
		Enabled: true,
	}
	p.stats.ActiveTraces++
	
	return nil
}

// StopTrace stops a trace session
func (p *Profiler) StopTrace(id string) error {
	if !p.enabled {
		return nil
	}

	p.mu.Lock()
	defer p.mu.Unlock()
	
	traceData, exists := p.activeTraces[id]
	if !exists {
		return fmt.Errorf("trace %s not found", id)
	}
	
	// Stop trace
	trace.Stop()
	
	// Update statistics
	p.stats.ActiveTraces--
	p.stats.ProfilingTime += time.Since(traceData.Start)
	
	// Remove from active traces
	delete(p.activeTraces, id)
	
	return nil
}

// StartCPUProfile starts CPU profiling
func (p *Profiler) StartCPUProfile(filename string) error {
	if !p.enabled {
		return nil
	}
	
	file, err := pprof.StartCPUProfile(filename)
	if err != nil {
		return fmt.Errorf("failed to start CPU profile: %v", err)
	}
	
	// Store file reference for cleanup
	p.mu.Lock()
	p.activeTraces["cpu_profile"] = &traceData{
		ID:      "cpu_profile",
		Start:   time.Now(),
		File:    filename,
		Enabled: true,
	}
	p.mu.Unlock()
	
	return nil
}

// StopCPUProfile stops CPU profiling
func (p *Profiler) StopCPUProfile() {
	if !p.enabled {
		return
	}
	
	pprof.StopCPUProfile()
	
	p.mu.Lock()
	delete(p.activeTraces, "cpu_profile")
	p.mu.Unlock()
}

// StartMemoryProfile starts memory profiling
func (p *Profiler) StartMemoryProfile(filename string) error {
	if !p.enabled {
		return nil
	}
	
	file, err := pprof.WriteHeapProfile(filename)
	if err != nil {
		return fmt.Errorf("failed to write memory profile: %v", err)
	}
	
	p.mu.Lock()
	p.activeTraces["memory_profile"] = &traceData{
		ID:      "memory_profile",
		Start:   time.Now(),
		File:    filename,
		Enabled: true,
	}
	p.mu.Unlock()
	
	return nil
}

// GetHotPaths returns functions that are candidates for JIT compilation
func (p *Profiler) GetHotPaths() []*ProfileData {
	p.mu.RLock()
	defer p.mu.RUnlock()
	
	var hotPaths []*ProfileData
	for _, profile := range p.profiles {
		if profile.HotPath {
			hotPaths = append(hotPaths, profile)
		}
	}
	
	return hotPaths
}

// GetOptimizationCandidates returns functions with high optimization potential
func (p *Profiler) GetOptimizationCandidates(threshold float64) []*ProfileData {
	p.mu.RLock()
	defer p.mu.RUnlock()
	
	var candidates []*ProfileData
	for _, profile := range p.profiles {
		if profile.OptimizationScore >= threshold {
			candidates = append(candidates, profile)
		}
	}
	
	return candidates
}

// GetProfile returns profiling data for a specific function
func (p *Profiler) GetProfile(id string) (*ProfileData, bool) {
	p.mu.RLock()
	defer p.mu.RUnlock()
	
	profile, exists := p.profiles[id]
	return profile, exists
}

// GetAllProfiles returns all profiling data
func (p *Profiler) GetAllProfiles() map[string]*ProfileData {
	p.mu.RLock()
	defer p.mu.RUnlock()
	
	profiles := make(map[string]*ProfileData)
	for k, v := range p.profiles {
		profiles[k] = v
	}
	
	return profiles
}

// GetStats returns profiler statistics
func (p *Profiler) GetStats() *ProfilerStats {
	p.mu.RLock()
	defer p.mu.RUnlock()
	
	stats := *p.stats
	return &stats
}

// Enable enables profiling
func (p *Profiler) Enable() {
	p.mu.Lock()
	defer p.mu.Unlock()
	p.enabled = true
}

// Disable disables profiling
func (p *Profiler) Disable() {
	p.mu.Lock()
	defer p.mu.Unlock()
	p.enabled = false
}

// Clear clears all profiling data
func (p *Profiler) Clear() {
	p.mu.Lock()
	defer p.mu.Unlock()
	
	p.profiles = make(map[string]*ProfileData)
	p.stats = &ProfilerStats{}
}

// GetMemoryStats returns current memory statistics
func (p *Profiler) GetMemoryStats() runtime.MemStats {
	var m runtime.MemStats
	runtime.ReadMemStats(&m)
	return m
}

// GetGoroutineStats returns goroutine statistics
func (p *Profiler) GetGoroutineStats() map[string]interface{} {
	return map[string]interface{}{
		"count": runtime.NumGoroutine(),
		"cpu_count": runtime.NumCPU(),
		"version": runtime.Version(),
	}
}

// ProfileFunction is a convenience function for profiling a function
func (p *Profiler) ProfileFunction(id, name string, fn func() interface{}) interface{} {
	session := p.StartProfile(id, name)
	defer session.End()
	
	return fn()
}

// ProfileFunctionWithArgs is a convenience function for profiling a function with arguments
func (p *Profiler) ProfileFunctionWithArgs(id, name string, fn func(args ...interface{}) interface{}, args ...interface{}) interface{} {
	session := p.StartProfile(id, name)
	defer session.End()
	
	return fn(args...)
} 