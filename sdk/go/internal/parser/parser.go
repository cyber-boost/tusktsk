// Package parser provides TuskLang parsing functionality
package parser

import (
	"fmt"
	"strings"
)

// Parser represents a TuskLang parser
type Parser struct{}

// New creates a new Parser instance
func New() *Parser {
	return &Parser{}
}

// ParseResult represents the result of parsing
type ParseResult struct {
	Tokens []Token
	AST    []ASTNode
	Errors []ParseError
}

// Token represents a lexical token
type Token struct {
	Type    TokenType
	Value   string
	Line    int
	Column  int
}

// TokenType represents the type of a token
type TokenType int

const (
	TokenEOF TokenType = iota
	TokenIdentifier
	TokenString
	TokenNumber
	TokenOperator
	TokenKeyword
	TokenPunctuation
	TokenComment
	TokenWhitespace
)

// ASTNode represents a node in the Abstract Syntax Tree
type ASTNode struct {
	Type     string
	Value    string
	Children []ASTNode
	Line     int
	Column   int
}

// ParseError represents a parsing error
type ParseError struct {
	Message string
	Line    int
	Column  int
}

// Parse parses TuskLang code
func (p *Parser) Parse(code string) (*ParseResult, error) {
	result := &ParseResult{
		Tokens: []Token{},
		AST:    []ASTNode{},
		Errors: []ParseError{},
	}

	// Tokenize the code
	tokens, err := p.tokenize(code)
	if err != nil {
		return nil, fmt.Errorf("tokenization failed: %v", err)
	}
	result.Tokens = tokens

	// Build AST
	ast, err := p.buildAST(tokens)
	if err != nil {
		return nil, fmt.Errorf("AST building failed: %v", err)
	}
	result.AST = ast

	return result, nil
}

// tokenize converts code into tokens
func (p *Parser) tokenize(code string) ([]Token, error) {
	var tokens []Token
	lines := strings.Split(code, "\n")
	
	for lineNum, line := range lines {
		lineNum++ // 1-based line numbers
		column := 1
		
		// Simple tokenization for now
		words := strings.Fields(line)
		for _, word := range words {
			token := Token{
				Line:   lineNum,
				Column: column,
			}
			
			// Determine token type
			switch {
			case strings.HasPrefix(word, "//"):
				token.Type = TokenComment
				token.Value = word
			case strings.HasPrefix(word, "\"") && strings.HasSuffix(word, "\""):
				token.Type = TokenString
				token.Value = word
			case p.isNumber(word):
				token.Type = TokenNumber
				token.Value = word
			case p.isKeyword(word):
				token.Type = TokenKeyword
				token.Value = word
			case p.isOperator(word):
				token.Type = TokenOperator
				token.Value = word
			default:
				token.Type = TokenIdentifier
				token.Value = word
			}
			
			tokens = append(tokens, token)
			column += len(word) + 1 // +1 for space
		}
	}
	
	// Add EOF token
	tokens = append(tokens, Token{
		Type: TokenEOF,
		Line: len(lines),
		Column: 1,
	})
	
	return tokens, nil
}

// buildAST builds the Abstract Syntax Tree from tokens
func (p *Parser) buildAST(tokens []Token) ([]ASTNode, error) {
	var ast []ASTNode
	
	for _, token := range tokens {
		if token.Type == TokenEOF {
			break
		}
		
		node := ASTNode{
			Type:  p.tokenTypeToString(token.Type),
			Value: token.Value,
			Line:  token.Line,
			Column: token.Column,
		}
		
		ast = append(ast, node)
	}
	
	return ast, nil
}

// Helper methods
func (p *Parser) isNumber(s string) bool {
	// Simple number detection
	return strings.ContainsAny(s, "0123456789")
}

func (p *Parser) isKeyword(s string) bool {
	keywords := []string{"if", "else", "for", "while", "function", "return", "var", "let", "const"}
	for _, keyword := range keywords {
		if s == keyword {
			return true
		}
	}
	return false
}

func (p *Parser) isOperator(s string) bool {
	operators := []string{"+", "-", "*", "/", "=", "==", "!=", "<", ">", "<=", ">="}
	for _, op := range operators {
		if s == op {
			return true
		}
	}
	return false
}

func (p *Parser) tokenTypeToString(t TokenType) string {
	switch t {
	case TokenEOF:
		return "EOF"
	case TokenIdentifier:
		return "Identifier"
	case TokenString:
		return "String"
	case TokenNumber:
		return "Number"
	case TokenOperator:
		return "Operator"
	case TokenKeyword:
		return "Keyword"
	case TokenPunctuation:
		return "Punctuation"
	case TokenComment:
		return "Comment"
	case TokenWhitespace:
		return "Whitespace"
	default:
		return "Unknown"
	}
} 
		return "Whitespace"
	default:
		return "Unknown"
	}
} 