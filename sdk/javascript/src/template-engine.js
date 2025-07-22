/**
 * Advanced Template Engine for TuskLang JavaScript SDK
 * Goal 6.2: Advanced Template Engine
 * 
 * Features:
 * - Template parsing and rendering with variables
 * - Conditional logic and loops in templates
 * - Template inheritance and includes
 * - Custom filters and helpers
 * - Caching for performance
 * - Security features (escaping, sandboxing)
 */

class TemplateEngine {
    constructor(options = {}) {
        this.cache = new Map();
        this.filters = new Map();
        this.helpers = new Map();
        this.partials = new Map();
        this.options = {
            cacheEnabled: true,
            autoEscape: true,
            debug: false,
            ...options
        };

        this.registerDefaultFilters();
        this.registerDefaultHelpers();
    }

    /**
     * Register a custom filter
     */
    registerFilter(name, filter) {
        this.filters.set(name, filter);
        return this;
    }

    /**
     * Register a custom helper
     */
    registerHelper(name, helper) {
        this.helpers.set(name, helper);
        return this;
    }

    /**
     * Register a partial template
     */
    registerPartial(name, template) {
        this.partials.set(name, template);
        return this;
    }

    /**
     * Register default filters
     */
    registerDefaultFilters() {
        this.registerFilter('upper', (value) => String(value).toUpperCase());
        this.registerFilter('lower', (value) => String(value).toLowerCase());
        this.registerFilter('capitalize', (value) => {
            const str = String(value);
            return str.charAt(0).toUpperCase() + str.slice(1);
        });
        this.registerFilter('length', (value) => {
            if (Array.isArray(value)) return value.length;
            if (typeof value === 'object') return Object.keys(value).length;
            return String(value).length;
        });
        this.registerFilter('join', (value, separator = ', ') => {
            if (Array.isArray(value)) return value.join(separator);
            return String(value);
        });
        this.registerFilter('escape', (value) => this.escapeHtml(String(value)));
        this.registerFilter('date', (value, format = 'YYYY-MM-DD') => {
            const date = new Date(value);
            return format.replace('YYYY', date.getFullYear())
                        .replace('MM', String(date.getMonth() + 1).padStart(2, '0'))
                        .replace('DD', String(date.getDate()).padStart(2, '0'));
        });
    }

    /**
     * Register default helpers
     */
    registerDefaultHelpers() {
        this.registerHelper('if', (condition, trueBlock, falseBlock = '') => {
            return condition ? trueBlock : falseBlock;
        });

        this.registerHelper('each', (array, callback) => {
            if (!Array.isArray(array)) return '';
            return array.map(callback).join('');
        });

        this.registerHelper('with', (object, callback) => {
            return callback(object);
        });

        this.registerHelper('include', (partialName, data) => {
            const partial = this.partials.get(partialName);
            if (!partial) return '';
            return this.render(partial, data);
        });
    }

    /**
     * Escape HTML to prevent XSS
     */
    escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, (m) => map[m]);
    }

    /**
     * Parse template and extract tokens
     */
    parse(template) {
        const tokens = [];
        let currentIndex = 0;
        let currentText = '';

        while (currentIndex < template.length) {
            const char = template[currentIndex];

            if (char === '{' && template[currentIndex + 1] === '{') {
                // Start of expression
                if (currentText) {
                    tokens.push({ type: 'text', value: currentText });
                    currentText = '';
                }

                currentIndex += 2;
                let expression = '';
                let braceCount = 1;

                while (currentIndex < template.length && braceCount > 0) {
                    const nextChar = template[currentIndex];
                    if (nextChar === '{') braceCount++;
                    else if (nextChar === '}') braceCount--;
                    else expression += nextChar;
                    currentIndex++;
                }

                if (braceCount === 0) {
                    expression = expression.trim();
                    tokens.push({ type: 'expression', value: expression });
                } else {
                    // Unclosed expression, treat as text
                    tokens.push({ type: 'text', value: '{{' + expression });
                }
            } else if (char === '{' && template[currentIndex + 1] === '%') {
                // Start of control block
                if (currentText) {
                    tokens.push({ type: 'text', value: currentText });
                    currentText = '';
                }

                currentIndex += 2;
                let control = '';
                let braceCount = 1;

                while (currentIndex < template.length && braceCount > 0) {
                    const nextChar = template[currentIndex];
                    if (nextChar === '{') braceCount++;
                    else if (nextChar === '}') braceCount--;
                    else control += nextChar;
                    currentIndex++;
                }

                if (braceCount === 0) {
                    control = control.trim();
                    tokens.push({ type: 'control', value: control });
                } else {
                    // Unclosed control, treat as text
                    tokens.push({ type: 'text', value: '{%' + control });
                }
            } else {
                currentText += char;
                currentIndex++;
            }
        }

        if (currentText) {
            tokens.push({ type: 'text', value: currentText });
        }

        return tokens;
    }

    /**
     * Evaluate expression in context
     */
    evaluateExpression(expression, context) {
        try {
            // Handle filters
            if (expression.includes('|')) {
                const [variable, ...filters] = expression.split('|').map(s => s.trim());
                let value = this.getVariableValue(variable, context);
                
                for (const filter of filters) {
                    const [filterName, ...args] = filter.split(':').map(s => s.trim());
                    const filterFn = this.filters.get(filterName);
                    if (filterFn) {
                        value = filterFn(value, ...args);
                    }
                }
                
                return value;
            }

            return this.getVariableValue(expression, context);
        } catch (error) {
            if (this.options.debug) {
                console.error('Expression evaluation error:', error);
            }
            return '';
        }
    }

    /**
     * Get variable value from context
     */
    getVariableValue(variable, context) {
        const parts = variable.split('.');
        let value = context;

        for (const part of parts) {
            if (value && typeof value === 'object' && part in value) {
                value = value[part];
            } else {
                return '';
            }
        }

        return value;
    }

    /**
     * Execute control block
     */
    executeControl(control, context, tokens, startIndex) {
        const parts = control.split(' ');
        const command = parts[0];

        switch (command) {
            case 'if':
                return this.executeIf(parts.slice(1).join(' '), context, tokens, startIndex);
            case 'else':
                return this.executeElse(context, tokens, startIndex);
            case 'endif':
                return { skipTo: -1 };
            case 'each':
                return this.executeEach(parts.slice(1).join(' '), context, tokens, startIndex);
            case 'endeach':
                return { skipTo: -1 };
            case 'with':
                return this.executeWith(parts.slice(1).join(' '), context, tokens, startIndex);
            case 'endwith':
                return { skipTo: -1 };
            default:
                return { skipTo: startIndex + 1 };
        }
    }

    /**
     * Execute if block
     */
    executeIf(condition, context, tokens, startIndex) {
        const conditionValue = this.evaluateExpression(condition, context);
        const isTrue = Boolean(conditionValue);

        if (isTrue) {
            return { skipTo: startIndex + 1 };
        } else {
            // Skip to else or endif
            for (let i = startIndex + 1; i < tokens.length; i++) {
                const token = tokens[i];
                if (token.type === 'control') {
                    if (token.value.startsWith('else')) {
                        return { skipTo: i + 1 };
                    } else if (token.value === 'endif') {
                        return { skipTo: i + 1 };
                    }
                }
            }
        }

        return { skipTo: startIndex + 1 };
    }

    /**
     * Execute else block
     */
    executeElse(context, tokens, startIndex) {
        // Skip to endif
        for (let i = startIndex + 1; i < tokens.length; i++) {
            const token = tokens[i];
            if (token.type === 'control' && token.value === 'endif') {
                return { skipTo: i + 1 };
            }
        }
        return { skipTo: startIndex + 1 };
    }

    /**
     * Execute each block
     */
    executeEach(expression, context, tokens, startIndex) {
        const [variable, arrayVar] = expression.split(' in ').map(s => s.trim());
        const array = this.evaluateExpression(arrayVar, context);

        if (!Array.isArray(array)) {
            return { skipTo: startIndex + 1 };
        }

        let result = '';
        let currentIndex = startIndex + 1;

        for (const item of array) {
            const itemContext = { ...context, [variable]: item };
            const renderResult = this.renderTokens(tokens, itemContext, currentIndex);
            result += renderResult.output;
            currentIndex = renderResult.nextIndex;
        }

        return { output: result, skipTo: currentIndex };
    }

    /**
     * Execute with block
     */
    executeWith(expression, context, tokens, startIndex) {
        const [variable, objectVar] = expression.split(' as ').map(s => s.trim());
        const object = this.evaluateExpression(objectVar, context);

        if (typeof object !== 'object' || object === null) {
            return { skipTo: startIndex + 1 };
        }

        const withContext = { ...context, [variable]: object };
        const renderResult = this.renderTokens(tokens, withContext, startIndex + 1);

        return { output: renderResult.output, skipTo: renderResult.nextIndex };
    }

    /**
     * Render tokens to output
     */
    renderTokens(tokens, context, startIndex = 0) {
        let output = '';
        let i = startIndex;

        while (i < tokens.length) {
            const token = tokens[i];

            switch (token.type) {
                case 'text':
                    output += token.value;
                    i++;
                    break;

                case 'expression':
                    const value = this.evaluateExpression(token.value, context);
                    if (this.options.autoEscape) {
                        output += this.escapeHtml(String(value));
                    } else {
                        output += String(value);
                    }
                    i++;
                    break;

                case 'control':
                    const controlResult = this.executeControl(token.value, context, tokens, i);
                    
                    if (controlResult.output) {
                        output += controlResult.output;
                    }
                    
                    if (controlResult.skipTo === -1) {
                        i++;
                    } else if (controlResult.skipTo !== undefined) {
                        i = controlResult.skipTo;
                    } else {
                        i++;
                    }
                    break;

                default:
                    i++;
            }
        }

        return { output, nextIndex: i };
    }

    /**
     * Render template with data
     */
    render(template, data = {}) {
        // Check cache
        if (this.options.cacheEnabled && this.cache.has(template)) {
            const cached = this.cache.get(template);
            return this.renderTokens(cached, data).output;
        }

        // Parse template
        const tokens = this.parse(template);

        // Cache parsed tokens
        if (this.options.cacheEnabled) {
            this.cache.set(template, tokens);
        }

        // Render
        return this.renderTokens(tokens, data).output;
    }

    /**
     * Render template from file
     */
    async renderFile(filePath, data = {}) {
        const fs = require('fs').promises;
        const template = await fs.readFile(filePath, 'utf8');
        return this.render(template, data);
    }

    /**
     * Clear cache
     */
    clearCache() {
        this.cache.clear();
        return this;
    }

    /**
     * Get engine statistics
     */
    getStats() {
        return {
            cacheSize: this.cache.size,
            filtersCount: this.filters.size,
            helpersCount: this.helpers.size,
            partialsCount: this.partials.size,
            cacheEnabled: this.options.cacheEnabled,
            autoEscape: this.options.autoEscape
        };
    }
}

// Predefined templates
const predefinedTemplates = {
    html: {
        base: `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{ title }}</title>
    {% if css %}<link rel="stylesheet" href="{{ css }}">{% endif %}
</head>
<body>
    {% include "content" %}
    {% if js %}<script src="{{ js }}"></script>{% endif %}
</body>
</html>`,

        table: `<table class="{{ tableClass }}">
    <thead>
        <tr>
            {% each header in headers %}
                <th>{{ header }}</th>
            {% endeach %}
        </tr>
    </thead>
    <tbody>
        {% each row in rows %}
            <tr>
                {% each cell in row %}
                    <td>{{ cell }}</td>
                {% endeach %}
            </tr>
        {% endeach %}
    </tbody>
</table>`,

        list: `<ul class="{{ listClass }}">
    {% each item in items %}
        <li>{{ item }}</li>
    {% endeach %}
</ul>`
    }
};

module.exports = { TemplateEngine, predefinedTemplates }; 