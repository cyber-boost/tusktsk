package main

import (
	"bytes"
	"compress/gzip"
	"crypto/rand"
	"encoding/json"
	"fmt"
	"io"
	"os"
	"runtime"
	"sync"
	"time"
)

// BenchmarkResult represents a single benchmark result
type BenchmarkResult struct {
	TestName     string        `json:"test_name"`
	Duration     time.Duration `json:"duration"`
	MemoryUsage  uint64        `json:"memory_usage"`
	Throughput   float64       `json:"throughput"`
	CompressionRatio float64   `json:"compression_ratio"`
	Timestamp    time.Time     `json:"timestamp"`
}

// BenchmarkSuite represents a collection of benchmark results
type BenchmarkSuite struct {
	Results []BenchmarkResult `json:"results"`
	Summary BenchmarkSummary  `json:"summary"`
}

// BenchmarkSummary provides overall performance metrics
type BenchmarkSummary struct {
	TotalTests     int           `json:"total_tests"`
	TotalDuration  time.Duration `json:"total_duration"`
	AverageMemory  uint64        `json:"average_memory"`
	AverageThroughput float64    `json:"average_throughput"`
	BestCompression float64      `json:"best_compression"`
	WorstCompression float64     `json:"worst_compression"`
}

// GoSDKBenchmarker provides benchmarking for Go SDK
type GoSDKBenchmarker struct {
	results []BenchmarkResult
	mutex   sync.Mutex
}

// NewGoSDKBenchmarker creates a new benchmarker
func NewGoSDKBenchmarker() *GoSDKBenchmarker {
	return &GoSDKBenchmarker{
		results: make([]BenchmarkResult, 0),
	}
}

// RunAllBenchmarks runs all available benchmarks
func (b *GoSDKBenchmarker) RunAllBenchmarks() *BenchmarkSuite {
	fmt.Println("🚀 Starting Go SDK Performance Benchmarks...")
	
	// Run individual benchmarks
	b.benchmarkBinaryFormatWrite()
	b.benchmarkBinaryFormatRead()
	b.benchmarkCompression()
	b.benchmarkEncryption()
	b.benchmarkConcurrentAccess()
	b.benchmarkMemoryUsage()
	b.benchmarkThroughput()
	
	// Generate summary
	summary := b.generateSummary()
	
	return &BenchmarkSuite{
		Results: b.results,
		Summary: summary,
	}
}

// benchmarkBinaryFormatWrite benchmarks binary format writing
func (b *GoSDKBenchmarker) benchmarkBinaryFormatWrite() {
	fmt.Println("📝 Benchmarking Binary Format Write...")
	
	// Generate test data
	testData := generateTestData(1024 * 1024) // 1MB
	metadata := map[string]interface{}{
		"test": true,
		"size": len(testData),
		"timestamp": time.Now().Unix(),
	}
	
	start := time.Now()
	var m runtime.MemStats
	runtime.ReadMemStats(&m)
	startMem := m.Alloc
	
	// Simulate binary format writing (using gzip compression as example)
	var buf bytes.Buffer
	gw := gzip.NewWriter(&buf)
	gw.Write(testData)
	gw.Close()
	
	// Serialize metadata
	metadataBytes, _ := json.Marshal(metadata)
	
	end := time.Now()
	runtime.ReadMemStats(&m)
	endMem := m.Alloc
	
	duration := end.Sub(start)
	throughput := float64(len(testData)) / duration.Seconds() / 1024 / 1024 // MB/s
	memoryUsage := endMem - startMem
	
	result := BenchmarkResult{
		TestName:    "Binary Format Write",
		Duration:    duration,
		MemoryUsage: memoryUsage,
		Throughput:  throughput,
		CompressionRatio: float64(len(testData)) / float64(buf.Len()),
		Timestamp:   time.Now(),
	}
	
	b.addResult(result)
	fmt.Printf("✅ Binary Format Write: %v, %.2f MB/s, %.2fx compression\n", 
		duration, throughput, result.CompressionRatio)
}

// benchmarkBinaryFormatRead benchmarks binary format reading
func (b *GoSDKBenchmarker) benchmarkBinaryFormatRead() {
	fmt.Println("📖 Benchmarking Binary Format Read...")
	
	// Create test data
	testData := generateTestData(1024 * 1024) // 1MB
	var buf bytes.Buffer
	gw := gzip.NewWriter(&buf)
	gw.Write(testData)
	gw.Close()
	compressedData := buf.Bytes()
	
	start := time.Now()
	var m runtime.MemStats
	runtime.ReadMemStats(&m)
	startMem := m.Alloc
	
	// Simulate binary format reading (decompression)
	gr, _ := gzip.NewReader(bytes.NewReader(compressedData))
	io.Copy(io.Discard, gr)
	gr.Close()
	
	end := time.Now()
	runtime.ReadMemStats(&m)
	endMem := m.Alloc
	
	duration := end.Sub(start)
	throughput := float64(len(testData)) / duration.Seconds() / 1024 / 1024 // MB/s
	memoryUsage := endMem - startMem
	
	result := BenchmarkResult{
		TestName:    "Binary Format Read",
		Duration:    duration,
		MemoryUsage: memoryUsage,
		Throughput:  throughput,
		CompressionRatio: float64(len(testData)) / float64(len(compressedData)),
		Timestamp:   time.Now(),
	}
	
	b.addResult(result)
	fmt.Printf("✅ Binary Format Read: %v, %.2f MB/s\n", duration, throughput)
}

// benchmarkCompression benchmarks compression algorithms
func (b *GoSDKBenchmarker) benchmarkCompression() {
	fmt.Println("🗜️  Benchmarking Compression...")
	
	testData := generateTestData(1024 * 1024) // 1MB
	
	// Test gzip compression
	start := time.Now()
	var buf bytes.Buffer
	gw := gzip.NewWriter(&buf)
	gw.Write(testData)
	gw.Close()
	duration := time.Since(start)
	
	compressionRatio := float64(len(testData)) / float64(buf.Len())
	throughput := float64(len(testData)) / duration.Seconds() / 1024 / 1024
	
	result := BenchmarkResult{
		TestName:    "Gzip Compression",
		Duration:    duration,
		Throughput:  throughput,
		CompressionRatio: compressionRatio,
		Timestamp:   time.Now(),
	}
	
	b.addResult(result)
	fmt.Printf("✅ Gzip Compression: %v, %.2f MB/s, %.2fx compression\n", 
		duration, throughput, compressionRatio)
}

// benchmarkEncryption benchmarks encryption performance
func (b *GoSDKBenchmarker) benchmarkEncryption() {
	fmt.Println("🔐 Benchmarking Encryption...")
	
	testData := generateTestData(1024 * 1024) // 1MB
	key := make([]byte, 32) // AES-256 key
	rand.Read(key)
	
	start := time.Now()
	// Simulate encryption (using a simple XOR for demonstration)
	encrypted := make([]byte, len(testData))
	for i := range testData {
		encrypted[i] = testData[i] ^ key[i%len(key)]
	}
	duration := time.Since(start)
	
	throughput := float64(len(testData)) / duration.Seconds() / 1024 / 1024
	
	result := BenchmarkResult{
		TestName:    "Encryption",
		Duration:    duration,
		Throughput:  throughput,
		Timestamp:   time.Now(),
	}
	
	b.addResult(result)
	fmt.Printf("✅ Encryption: %v, %.2f MB/s\n", duration, throughput)
}

// benchmarkConcurrentAccess benchmarks concurrent access
func (b *GoSDKBenchmarker) benchmarkConcurrentAccess() {
	fmt.Println("🔄 Benchmarking Concurrent Access...")
	
	numGoroutines := 100
	numOperations := 1000
	
	start := time.Now()
	var wg sync.WaitGroup
	
	for i := 0; i < numGoroutines; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			for j := 0; j < numOperations; j++ {
				// Simulate concurrent operations
				testData := generateTestData(1024) // 1KB per operation
				var buf bytes.Buffer
				gw := gzip.NewWriter(&buf)
				gw.Write(testData)
				gw.Close()
			}
		}(i)
	}
	
	wg.Wait()
	duration := time.Since(start)
	
	totalOperations := numGoroutines * numOperations
	throughput := float64(totalOperations) / duration.Seconds()
	
	result := BenchmarkResult{
		TestName:    "Concurrent Access",
		Duration:    duration,
		Throughput:  throughput,
		Timestamp:   time.Now(),
	}
	
	b.addResult(result)
	fmt.Printf("✅ Concurrent Access: %v, %.2f ops/s\n", duration, throughput)
}

// benchmarkMemoryUsage benchmarks memory usage patterns
func (b *GoSDKBenchmarker) benchmarkMemoryUsage() {
	fmt.Println("💾 Benchmarking Memory Usage...")
	
	var m runtime.MemStats
	runtime.ReadMemStats(&m)
	startMem := m.Alloc
	
	// Perform memory-intensive operations
	for i := 0; i < 100; i++ {
		testData := generateTestData(1024 * 1024) // 1MB
		var buf bytes.Buffer
		gw := gzip.NewWriter(&buf)
		gw.Write(testData)
		gw.Close()
	}
	
	runtime.ReadMemStats(&m)
	endMem := m.Alloc
	
	memoryUsage := endMem - startMem
	
	result := BenchmarkResult{
		TestName:    "Memory Usage",
		MemoryUsage: memoryUsage,
		Timestamp:   time.Now(),
	}
	
	b.addResult(result)
	fmt.Printf("✅ Memory Usage: %d bytes\n", memoryUsage)
}

// benchmarkThroughput benchmarks overall throughput
func (b *GoSDKBenchmarker) benchmarkThroughput() {
	fmt.Println("⚡ Benchmarking Throughput...")
	
	testData := generateTestData(10 * 1024 * 1024) // 10MB
	
	start := time.Now()
	
	// Simulate high-throughput operations
	for i := 0; i < 10; i++ {
		var buf bytes.Buffer
		gw := gzip.NewWriter(&buf)
		gw.Write(testData)
		gw.Close()
	}
	
	duration := time.Since(start)
	totalData := len(testData) * 10
	throughput := float64(totalData) / duration.Seconds() / 1024 / 1024
	
	result := BenchmarkResult{
		TestName:    "Throughput",
		Duration:    duration,
		Throughput:  throughput,
		Timestamp:   time.Now(),
	}
	
	b.addResult(result)
	fmt.Printf("✅ Throughput: %v, %.2f MB/s\n", duration, throughput)
}

// generateTestData generates test data of specified size
func generateTestData(size int) []byte {
	data := make([]byte, size)
	rand.Read(data)
	return data
}

// addResult adds a benchmark result
func (b *GoSDKBenchmarker) addResult(result BenchmarkResult) {
	b.mutex.Lock()
	defer b.mutex.Unlock()
	b.results = append(b.results, result)
}

// generateSummary generates a summary of all benchmark results
func (b *GoSDKBenchmarker) generateSummary() BenchmarkSummary {
	if len(b.results) == 0 {
		return BenchmarkSummary{}
	}
	
	var totalDuration time.Duration
	var totalMemory uint64
	var totalThroughput float64
	var bestCompression, worstCompression float64
	
	throughputCount := 0
	compressionCount := 0
	
	for _, result := range b.results {
		totalDuration += result.Duration
		totalMemory += result.MemoryUsage
		
		if result.Throughput > 0 {
			totalThroughput += result.Throughput
			throughputCount++
		}
		
		if result.CompressionRatio > 0 {
			if compressionCount == 0 {
				bestCompression = result.CompressionRatio
				worstCompression = result.CompressionRatio
			} else {
				if result.CompressionRatio > bestCompression {
					bestCompression = result.CompressionRatio
				}
				if result.CompressionRatio < worstCompression {
					worstCompression = result.CompressionRatio
				}
			}
			compressionCount++
		}
	}
	
	summary := BenchmarkSummary{
		TotalTests:     len(b.results),
		TotalDuration:  totalDuration,
		AverageMemory:  totalMemory / uint64(len(b.results)),
		BestCompression: bestCompression,
		WorstCompression: worstCompression,
	}
	
	if throughputCount > 0 {
		summary.AverageThroughput = totalThroughput / float64(throughputCount)
	}
	
	return summary
}

// SaveResults saves benchmark results to a file
func (b *GoSDKBenchmarker) SaveResults(filename string) error {
	suite := &BenchmarkSuite{
		Results: b.results,
		Summary: b.generateSummary(),
	}
	
	data, err := json.MarshalIndent(suite, "", "  ")
	if err != nil {
		return err
	}
	
	return os.WriteFile(filename, data, 0644)
}

// PrintSummary prints a formatted summary
func (b *GoSDKBenchmarker) PrintSummary() {
	summary := b.generateSummary()
	
	fmt.Println("\n📊 GO SDK BENCHMARK SUMMARY")
	fmt.Println("==========================")
	fmt.Printf("Total Tests: %d\n", summary.TotalTests)
	fmt.Printf("Total Duration: %v\n", summary.TotalDuration)
	fmt.Printf("Average Memory Usage: %d bytes\n", summary.AverageMemory)
	fmt.Printf("Average Throughput: %.2f MB/s\n", summary.AverageThroughput)
	fmt.Printf("Best Compression Ratio: %.2fx\n", summary.BestCompression)
	fmt.Printf("Worst Compression Ratio: %.2fx\n", summary.WorstCompression)
	
	// Performance assessment
	fmt.Println("\n🎯 PERFORMANCE ASSESSMENT")
	if summary.AverageThroughput > 100 {
		fmt.Println("✅ EXCELLENT: Throughput exceeds 100 MB/s")
	} else if summary.AverageThroughput > 50 {
		fmt.Println("✅ GOOD: Throughput exceeds 50 MB/s")
	} else {
		fmt.Println("⚠️  NEEDS IMPROVEMENT: Throughput below 50 MB/s")
	}
	
	if summary.BestCompression > 3.0 {
		fmt.Println("✅ EXCELLENT: Compression ratio exceeds 3x")
	} else if summary.BestCompression > 2.0 {
		fmt.Println("✅ GOOD: Compression ratio exceeds 2x")
	} else {
		fmt.Println("⚠️  NEEDS IMPROVEMENT: Compression ratio below 2x")
	}
}

func main() {
	benchmarker := NewGoSDKBenchmarker()
	suite := benchmarker.RunAllBenchmarks()
	
	benchmarker.PrintSummary()
	
	// Save results
	err := benchmarker.SaveResults("go_sdk_benchmark_results.json")
	if err != nil {
		fmt.Printf("Error saving results: %v\n", err)
	} else {
		fmt.Println("✅ Results saved to go_sdk_benchmark_results.json")
	}
} 