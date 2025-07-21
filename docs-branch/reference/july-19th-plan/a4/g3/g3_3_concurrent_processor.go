package main

import (
	"context"
	"encoding/json"
	"fmt"
	"sync"
	"time"
)

// Job represents a task to be processed
type Job struct {
	ID          string                 `json:"id"`
	Type        string                 `json:"type"`
	Data        map[string]interface{} `json:"data"`
	Priority    int                    `json:"priority"`
	CreatedAt   time.Time              `json:"created_at"`
	StartedAt   *time.Time             `json:"started_at,omitempty"`
	CompletedAt *time.Time             `json:"completed_at,omitempty"`
	Status      JobStatus              `json:"status"`
	Result      interface{}            `json:"result,omitempty"`
	Error       string                 `json:"error,omitempty"`
}

// JobStatus represents the current status of a job
type JobStatus string

const (
	JobPending   JobStatus = "pending"
	JobRunning   JobStatus = "running"
	JobCompleted JobStatus = "completed"
	JobFailed    JobStatus = "failed"
	JobCancelled JobStatus = "cancelled"
)

// JobResult represents the result of a job execution
type JobResult struct {
	JobID    string      `json:"job_id"`
	Status   JobStatus   `json:"status"`
	Result   interface{} `json:"result,omitempty"`
	Error    string      `json:"error,omitempty"`
	Duration time.Duration `json:"duration"`
}

// Processor defines the interface for job processing
type Processor interface {
	Process(ctx context.Context, job *Job) (*JobResult, error)
	CanHandle(jobType string) bool
}

// ConcurrentProcessor manages concurrent job processing
type ConcurrentProcessor struct {
	mu           sync.RWMutex
	workers      int
	jobQueue     chan *Job
	results      chan *JobResult
	processors   map[string]Processor
	jobs         map[string]*Job
	ctx          context.Context
	cancel       context.CancelFunc
	wg           sync.WaitGroup
	stats        *ProcessorStats
}

// ProcessorStats tracks processing statistics
type ProcessorStats struct {
	mu              sync.RWMutex
	TotalJobs       int64         `json:"total_jobs"`
	CompletedJobs   int64         `json:"completed_jobs"`
	FailedJobs      int64         `json:"failed_jobs"`
	AverageDuration time.Duration `json:"average_duration"`
	StartTime       time.Time     `json:"start_time"`
	LastJobTime     time.Time     `json:"last_job_time"`
}

// NewConcurrentProcessor creates a new concurrent processor
func NewConcurrentProcessor(workers int) *ConcurrentProcessor {
	ctx, cancel := context.WithCancel(context.Background())
	
	return &ConcurrentProcessor{
		workers:    workers,
		jobQueue:   make(chan *Job, workers*2),
		results:    make(chan *JobResult, workers*2),
		processors: make(map[string]Processor),
		jobs:       make(map[string]*Job),
		ctx:        ctx,
		cancel:     cancel,
		stats: &ProcessorStats{
			StartTime: time.Now(),
		},
	}
}

// RegisterProcessor registers a processor for a specific job type
func (cp *ConcurrentProcessor) RegisterProcessor(jobType string, processor Processor) {
	cp.mu.Lock()
	defer cp.mu.Unlock()
	cp.processors[jobType] = processor
}

// Start starts the concurrent processor
func (cp *ConcurrentProcessor) Start() {
	// Start worker goroutines
	for i := 0; i < cp.workers; i++ {
		cp.wg.Add(1)
		go cp.worker(i)
	}

	// Start result handler
	cp.wg.Add(1)
	go cp.resultHandler()
}

// Stop stops the concurrent processor
func (cp *ConcurrentProcessor) Stop() {
	cp.cancel()
	cp.wg.Wait()
	close(cp.jobQueue)
	close(cp.results)
}

// SubmitJob submits a job for processing
func (cp *ConcurrentProcessor) SubmitJob(job *Job) error {
	cp.mu.Lock()
	defer cp.mu.Unlock()

	if job.ID == "" {
		return fmt.Errorf("job ID is required")
	}

	if _, exists := cp.jobs[job.ID]; exists {
		return fmt.Errorf("job with ID %s already exists", job.ID)
	}

	// Set job metadata
	job.CreatedAt = time.Now()
	job.Status = JobPending

	// Store job
	cp.jobs[job.ID] = job

	// Update stats
	cp.stats.mu.Lock()
	cp.stats.TotalJobs++
	cp.stats.LastJobTime = time.Now()
	cp.stats.mu.Unlock()

	// Submit to queue
	select {
	case cp.jobQueue <- job:
		return nil
	case <-cp.ctx.Done():
		return fmt.Errorf("processor is stopped")
	default:
		return fmt.Errorf("job queue is full")
	}
}

// GetJob retrieves a job by ID
func (cp *ConcurrentProcessor) GetJob(jobID string) (*Job, error) {
	cp.mu.RLock()
	defer cp.mu.RUnlock()

	job, exists := cp.jobs[jobID]
	if !exists {
		return nil, fmt.Errorf("job not found: %s", jobID)
	}

	return job, nil
}

// CancelJob cancels a job
func (cp *ConcurrentProcessor) CancelJob(jobID string) error {
	cp.mu.Lock()
	defer cp.mu.Unlock()

	job, exists := cp.jobs[jobID]
	if !exists {
		return fmt.Errorf("job not found: %s", jobID)
	}

	if job.Status == JobCompleted || job.Status == JobFailed {
		return fmt.Errorf("cannot cancel completed or failed job")
	}

	job.Status = JobCancelled
	now := time.Now()
	job.CompletedAt = &now

	return nil
}

// GetStats returns current processing statistics
func (cp *ConcurrentProcessor) GetStats() *ProcessorStats {
	cp.stats.mu.RLock()
	defer cp.stats.mu.RUnlock()

	stats := &ProcessorStats{
		TotalJobs:       cp.stats.TotalJobs,
		CompletedJobs:   cp.stats.CompletedJobs,
		FailedJobs:      cp.stats.FailedJobs,
		AverageDuration: cp.stats.AverageDuration,
		StartTime:       cp.stats.StartTime,
		LastJobTime:     cp.stats.LastJobTime,
	}

	return stats
}

// worker processes jobs from the queue
func (cp *ConcurrentProcessor) worker(id int) {
	defer cp.wg.Done()

	for {
		select {
		case job := <-cp.jobQueue:
			cp.processJob(job)
		case <-cp.ctx.Done():
			return
		}
	}
}

// processJob processes a single job
func (cp *ConcurrentProcessor) processJob(job *Job) {
	// Update job status
	cp.mu.Lock()
	job.Status = JobRunning
	now := time.Now()
	job.StartedAt = &now
	cp.mu.Unlock()

	// Find appropriate processor
	cp.mu.RLock()
	processor, exists := cp.processors[job.Type]
	cp.mu.RUnlock()

	if !exists {
		result := &JobResult{
			JobID:  job.ID,
			Status: JobFailed,
			Error:  fmt.Sprintf("no processor found for job type: %s", job.Type),
		}
		cp.results <- result
		return
	}

	// Process job
	start := time.Now()
	jobResult, err := processor.Process(cp.ctx, job)
	duration := time.Since(start)

	if err != nil {
		jobResult = &JobResult{
			JobID:    job.ID,
			Status:   JobFailed,
			Error:    err.Error(),
			Duration: duration,
		}
	}

	// Send result
	cp.results <- jobResult
}

// resultHandler handles job results
func (cp *ConcurrentProcessor) resultHandler() {
	defer cp.wg.Done()

	for {
		select {
		case result := <-cp.results:
			cp.handleResult(result)
		case <-cp.ctx.Done():
			return
		}
	}
}

// handleResult processes a job result
func (cp *ConcurrentProcessor) handleResult(result *JobResult) {
	cp.mu.Lock()
	defer cp.mu.Unlock()

	job, exists := cp.jobs[result.JobID]
	if !exists {
		return
	}

	// Update job with result
	job.Status = result.Status
	job.Result = result.Result
	job.Error = result.Error
	now := time.Now()
	job.CompletedAt = &now

	// Update stats
	cp.stats.mu.Lock()
	if result.Status == JobCompleted {
		cp.stats.CompletedJobs++
	} else if result.Status == JobFailed {
		cp.stats.FailedJobs++
	}
	cp.stats.mu.Unlock()
}

// Example processors
type ParseProcessor struct{}

func (pp *ParseProcessor) CanHandle(jobType string) bool {
	return jobType == "parse"
}

func (pp *ParseProcessor) Process(ctx context.Context, job *Job) (*JobResult, error) {
	// Simulate parsing work
	time.Sleep(100 * time.Millisecond)

	data, ok := job.Data["code"].(string)
	if !ok {
		return nil, fmt.Errorf("code data is required")
	}

	result := map[string]interface{}{
		"parsed_tokens": len(data),
		"ast_nodes":     len(data) / 10,
		"syntax_valid":  true,
	}

	return &JobResult{
		JobID:  job.ID,
		Status: JobCompleted,
		Result: result,
	}, nil
}

type CompileProcessor struct{}

func (cp *CompileProcessor) CanHandle(jobType string) bool {
	return jobType == "compile"
}

func (cp *CompileProcessor) Process(ctx context.Context, job *Job) (*JobResult, error) {
	// Simulate compilation work
	time.Sleep(200 * time.Millisecond)

	result := map[string]interface{}{
		"bytecode_size": 1024,
		"optimizations": 5,
		"compiled":      true,
	}

	return &JobResult{
		JobID:  job.ID,
		Status: JobCompleted,
		Result: result,
	}, nil
}

// Example usage
func main() {
	// Create concurrent processor
	processor := NewConcurrentProcessor(4)

	// Register processors
	processor.RegisterProcessor("parse", &ParseProcessor{})
	processor.RegisterProcessor("compile", &CompileProcessor{})

	// Start processor
	processor.Start()
	defer processor.Stop()

	// Submit jobs
	jobs := []*Job{
		{
			ID:   "job1",
			Type: "parse",
			Data: map[string]interface{}{
				"code": "let x = 42; let y = x + 1;",
			},
			Priority: 1,
		},
		{
			ID:   "job2",
			Type: "compile",
			Data: map[string]interface{}{
				"ast": "abstract_syntax_tree_data",
			},
			Priority: 2,
		},
		{
			ID:   "job3",
			Type: "parse",
			Data: map[string]interface{}{
				"code": "function add(a, b) { return a + b; }",
			},
			Priority: 1,
		},
	}

	fmt.Println("Submitting jobs...")
	for _, job := range jobs {
		if err := processor.SubmitJob(job); err != nil {
			fmt.Printf("Error submitting job %s: %v\n", job.ID, err)
		}
	}

	// Wait for completion
	time.Sleep(1 * time.Second)

	// Display results
	fmt.Println("\nJob Results:")
	for _, job := range jobs {
		if result, err := processor.GetJob(job.ID); err == nil {
			fmt.Printf("Job %s: %s\n", job.ID, result.Status)
			if result.Result != nil {
				if data, err := json.MarshalIndent(result.Result, "", "  "); err == nil {
					fmt.Printf("  Result: %s\n", string(data))
				}
			}
			if result.Error != "" {
				fmt.Printf("  Error: %s\n", result.Error)
			}
		}
	}

	// Display stats
	stats := processor.GetStats()
	fmt.Printf("\nProcessor Stats:\n")
	fmt.Printf("  Total Jobs: %d\n", stats.TotalJobs)
	fmt.Printf("  Completed: %d\n", stats.CompletedJobs)
	fmt.Printf("  Failed: %d\n", stats.FailedJobs)
	fmt.Printf("  Running Time: %v\n", time.Since(stats.StartTime))
} 