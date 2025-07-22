#!/usr/bin/env node
/**
 * CSS Commands for TuskLang CLI
 * =============================
 * CSS processing and optimization commands
 * 
 * Commands:
 * - expand: Expand CSS shorthand properties
 * - map: Generate CSS source maps
 */

const { Command } = require('commander');
const fs = require('fs').promises;
const path = require('path');

/**
 * CSS shorthand patterns and their expansions
 */
const CSS_SHORTHANDS = {
  // Margin shorthand
  'm': 'margin',
  'mt': 'margin-top',
  'mr': 'margin-right', 
  'mb': 'margin-bottom',
  'ml': 'margin-left',
  'mx': ['margin-left', 'margin-right'],
  'my': ['margin-top', 'margin-bottom'],
  
  // Padding shorthand
  'p': 'padding',
  'pt': 'padding-top',
  'pr': 'padding-right',
  'pb': 'padding-bottom', 
  'pl': 'padding-left',
  'px': ['padding-left', 'padding-right'],
  'py': ['padding-top', 'padding-bottom'],
  
  // Border shorthand
  'b': 'border',
  'bt': 'border-top',
  'br': 'border-right',
  'bb': 'border-bottom',
  'bl': 'border-left',
  'bx': ['border-left', 'border-right'],
  'by': ['border-top', 'border-bottom'],
  
  // Background shorthand
  'bg': 'background',
  'bgc': 'background-color',
  'bgi': 'background-image',
  'bgr': 'background-repeat',
  'bgp': 'background-position',
  'bgs': 'background-size',
  
  // Font shorthand
  'f': 'font',
  'fs': 'font-size',
  'fw': 'font-weight',
  'ff': 'font-family',
  'fc': 'font-color',
  
  // Display shorthand
  'd': 'display',
  'f': 'float',
  'p': 'position',
  't': 'top',
  'r': 'right',
  'b': 'bottom',
  'l': 'left',
  
  // Width/Height shorthand
  'w': 'width',
  'h': 'height',
  'mw': 'max-width',
  'mh': 'max-height',
  'minw': 'min-width',
  'minh': 'min-height',
  
  // Flexbox shorthand
  'fd': 'flex-direction',
  'fw': 'flex-wrap',
  'fg': 'flex-grow',
  'fs': 'flex-shrink',
  'fb': 'flex-basis',
  
  // Grid shorthand
  'gc': 'grid-column',
  'gr': 'grid-row',
  'ga': 'grid-area',
  
  // Text shorthand
  'ta': 'text-align',
  'td': 'text-decoration',
  'tt': 'text-transform',
  'ls': 'letter-spacing',
  'ws': 'word-spacing',
  'lh': 'line-height',
  
  // Box model shorthand
  'box': 'box-sizing',
  'ov': 'overflow',
  'ovx': 'overflow-x',
  'ovy': 'overflow-y',
  'z': 'z-index',
  
  // Animation shorthand
  'anim': 'animation',
  'tr': 'transition',
  'tf': 'transform',
  
  // Other common shorthand
  'cur': 'cursor',
  'op': 'opacity',
  'vis': 'visibility',
  'clip': 'clip-path',
  'filter': 'filter'
};

/**
 * Expand CSS shorthand properties
 */
function expandCSSShorthand(css) {
  let expanded = css;
  let changes = 0;
  
  // Process each line
  const lines = expanded.split('\n');
  const processedLines = [];
  
  for (let line of lines) {
    const originalLine = line;
    
    // Find CSS property declarations
    const propertyMatch = line.match(/([a-zA-Z-]+)\s*:\s*([^;]+);/);
    if (propertyMatch) {
      const [, property, value] = propertyMatch;
      const trimmedProperty = property.trim();
      
      // Check if this is a shorthand property
      if (CSS_SHORTHANDS[trimmedProperty]) {
        const expansion = CSS_SHORTHANDS[trimmedProperty];
        
        if (Array.isArray(expansion)) {
          // Multiple properties
          const newProperties = expansion.map(prop => `  ${prop}: ${value};`);
          line = line.replace(propertyMatch[0], newProperties.join('\n'));
          changes += expansion.length;
        } else {
          // Single property
          line = line.replace(trimmedProperty, expansion);
          changes++;
        }
      }
    }
    
    processedLines.push(line);
  }
  
  return {
    css: processedLines.join('\n'),
    changes,
    original: css
  };
}

/**
 * Generate CSS source map
 */
function generateCSSSourceMap(css, sourceFile, outputFile) {
  const sourceMap = {
    version: 3,
    file: path.basename(outputFile),
    sourceRoot: '',
    sources: [path.basename(sourceFile)],
    names: [],
    mappings: ''
  };
  
  // Generate mappings (simplified - in production would use proper source map library)
  const lines = css.split('\n');
  let currentLine = 0;
  let currentColumn = 0;
  const mappings = [];
  
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const lineLength = line.length;
    
    // Add mapping for this line
    mappings.push(`${currentColumn},${i},${currentColumn},${i}`);
    
    currentLine++;
    currentColumn += lineLength + 1; // +1 for newline
  }
  
  sourceMap.mappings = mappings.join(';');
  
  return sourceMap;
}

/**
 * Expand CSS file
 */
async function expandCSSFile(inputFile, outputFile = null) {
  try {
    // Validate input file
    if (!inputFile) {
      throw new Error('Input file is required');
    }

    const inputPath = path.resolve(inputFile);
    const inputExt = path.extname(inputPath);
    
    if (inputExt !== '.css') {
      throw new Error('Input file must have .css extension');
    }

    // Generate output filename if not provided
    if (!outputFile) {
      const baseName = path.basename(inputPath, '.css');
      const outputPath = path.join(path.dirname(inputPath), `${baseName}.expanded.css`);
      outputFile = outputPath;
    }

    // Read CSS file
    const css = await fs.readFile(inputPath, 'utf8');
    
    // Expand shorthand properties
    const result = expandCSSShorthand(css);
    
    // Write expanded CSS
    await fs.writeFile(outputFile, result.css, 'utf8');

    return {
      success: true,
      input: inputPath,
      output: outputFile,
      originalSize: result.original.length,
      expandedSize: result.css.length,
      changes: result.changes,
      message: `✅ Successfully expanded ${inputPath} to ${outputFile} (${result.changes} properties expanded)`
    };

  } catch (error) {
    return {
      success: false,
      error: error.message,
      message: `❌ Failed to expand ${inputFile}: ${error.message}`
    };
  }
}

/**
 * Generate CSS source map
 */
async function generateCSSMap(inputFile, outputFile = null) {
  try {
    // Validate input file
    if (!inputFile) {
      throw new Error('Input file is required');
    }

    const inputPath = path.resolve(inputFile);
    const inputExt = path.extname(inputPath);
    
    if (inputExt !== '.css') {
      throw new Error('Input file must have .css extension');
    }

    // Generate output filename if not provided
    if (!outputFile) {
      const outputPath = `${inputPath}.map`;
      outputFile = outputPath;
    }

    // Read CSS file
    const css = await fs.readFile(inputPath, 'utf8');
    
    // Generate source map
    const sourceMap = generateCSSSourceMap(css, inputPath, outputFile);
    
    // Write source map
    await fs.writeFile(outputFile, JSON.stringify(sourceMap, null, 2), 'utf8');

    // Add source map comment to CSS if it doesn't exist
    const sourceMapComment = `\n/*# sourceMappingURL=${path.basename(outputFile)} */`;
    const cssWithMap = css + sourceMapComment;
    await fs.writeFile(inputPath, cssWithMap, 'utf8');

    return {
      success: true,
      input: inputPath,
      output: outputFile,
      cssSize: css.length,
      mapSize: JSON.stringify(sourceMap).length,
      message: `✅ Successfully generated source map ${outputFile} for ${inputPath}`
    };

  } catch (error) {
    return {
      success: false,
      error: error.message,
      message: `❌ Failed to generate source map for ${inputFile}: ${error.message}`
    };
  }
}

// Command definitions
const expand = new Command('expand')
  .description('Expand CSS shorthand properties')
  .argument('<input>', 'Input CSS file')
  .option('-o, --output <file>', 'Output CSS file (auto-generated if not specified)')
  .option('-v, --verbose', 'Show detailed expansion information')
  .action(async (input, options) => {
    const result = await expandCSSFile(input, options.output);
    console.log(result.message);
    
    if (result.success) {
      console.log(`   Input: ${result.input}`);
      console.log(`   Output: ${result.output}`);
      console.log(`   Original size: ${result.originalSize} bytes`);
      console.log(`   Expanded size: ${result.expandedSize} bytes`);
      console.log(`   Properties expanded: ${result.changes}`);
      
      if (options.verbose) {
        console.log('\nShorthand patterns supported:');
        Object.keys(CSS_SHORTHANDS).forEach(shorthand => {
          const expansion = CSS_SHORTHANDS[shorthand];
          if (Array.isArray(expansion)) {
            console.log(`   ${shorthand} → ${expansion.join(', ')}`);
          } else {
            console.log(`   ${shorthand} → ${expansion}`);
          }
        });
      }
    }
  });

const map = new Command('map')
  .description('Generate CSS source map')
  .argument('<input>', 'Input CSS file')
  .option('-o, --output <file>', 'Output source map file (auto-generated if not specified)')
  .option('-n, --no-comment', 'Do not add source map comment to CSS file')
  .action(async (input, options) => {
    const result = await generateCSSMap(input, options.output);
    console.log(result.message);
    
    if (result.success) {
      console.log(`   Input: ${result.input}`);
      console.log(`   Output: ${result.output}`);
      console.log(`   CSS size: ${result.cssSize} bytes`);
      console.log(`   Map size: ${result.mapSize} bytes`);
      
      if (!options.comment) {
        console.log(`   Source map comment added to CSS file`);
      }
    }
  });

module.exports = {
  expand,
  map,
  
  // Export functions for testing
  _expandCSSShorthand: expandCSSShorthand,
  _generateCSSSourceMap: generateCSSSourceMap,
  _expandCSSFile: expandCSSFile,
  _generateCSSMap: generateCSSMap,
  _CSS_SHORTHANDS: CSS_SHORTHANDS
}; 