// Package tusktsk provides the Go SDK for the TuskLang ecosystem
package tusktsk

import (
	"github.com/cyber-boost/tusktsk/sdk/go/internal/parser"
	"github.com/cyber-boost/tusktsk/sdk/go/internal/binary"
	"github.com/cyber-boost/tusktsk/sdk/go/internal/error"
	"github.com/cyber-boost/tusktsk/sdk/go/pkg/config"
	"github.com/cyber-boost/tusktsk/sdk/go/pkg/operators"
	"github.com/cyber-boost/tusktsk/sdk/go/pkg/security"
	"github.com/cyber-boost/tusktsk/sdk/go/pkg/utils"
)

// SDK represents the main TuskLang Go SDK
type SDK struct {
	Parser   *parser.Parser
	Binary   *binary.BinaryHandler
	Error    *error.ErrorHandler
	Config   *config.Config
	Security *security.SecurityManager
	Utils    *utils.Utils
}

// New creates a new TuskLang SDK instance
func New() *SDK {
	return &SDK{
		Parser:   parser.New(),
		Binary:   binary.New(),
		Error:    error.New(),
		Config:   config.New(),
		Security: security.New(),
		Utils:    utils.New(),
	}
}

// Parse parses TuskLang code
func (sdk *SDK) Parse(code string) (*parser.ParseResult, error) {
	return sdk.Parser.Parse(code)
}

// Compile compiles TuskLang code to binary
func (sdk *SDK) Compile(code string) (*binary.CompileResult, error) {
	parseResult, err := sdk.Parse(code)
	if err != nil {
		return nil, err
	}
	return sdk.Binary.Compile(parseResult)
}

// Execute executes TuskLang code
func (sdk *SDK) Execute(code string) (*binary.ExecuteResult, error) {
	compileResult, err := sdk.Compile(code)
	if err != nil {
		return nil, err
	}
	return sdk.Binary.Execute(compileResult)
} 