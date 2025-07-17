package main

import (
	"encoding/gob"
	"os"
	"time"
)

type PeanutConfig struct {
	Data      map[string]interface{}
	Timestamp time.Time
	Version   int
}

func (pc *PeanutConfig) CompileToBinary(filename string) error {
	file, err := os.Create(filename)
	if err != nil {
		return err
	}
	defer file.Close()
	
	encoder := gob.NewEncoder(file)
	return encoder.Encode(pc)
}

func (pc *PeanutConfig) LoadBinary(filename string) error {
	file, err := os.Open(filename)
	if err != nil {
		return err
	}
	defer file.Close()
	
	decoder := gob.NewDecoder(file)
	return decoder.Decode(pc)
}

func init() {
	gob.Register(map[string]interface{}{})
}
func main() {
	config := &PeanutConfig{
		Data: map[string]interface{}{
			"server": map[string]interface{}{
				"port": 8080,
				"host": "localhost",
			},
		},
		Timestamp: time.Now(),
		Version:   1,
	}
	
	// Test binary compilation
	err := config.CompileToBinary("test-config.pnt")
	if err != nil {
		panic(err)
	}
	
	// Test binary loading
	newConfig := &PeanutConfig{}
	err = newConfig.LoadBinary("test-config.pnt")
	if err != nil {
		panic(err)
	}
	
	println("✅ Go SDK binary compilation and loading successful!")
}
