class ValidationEngine {
    constructor(options = {}) {
        this.schemas = new Map();
        this.customValidators = new Map();
        this.strictMode = options.strictMode || false;
        this.autoFix = options.autoFix || false;
    }

    registerSchema(name, schema) {
        this.schemas.set(name, { schema, registeredAt: Date.now() });
        console.log(`✓ Schema registered: ${name}`);
        return true;
    }

    async validate(config, schemaName, options = {}) {
        const schema = this.schemas.get(schemaName);
        if (!schema) {
            throw new Error(`Schema ${schemaName} not found`);
        }

        const errors = [];
        const warnings = [];

        // Basic validation
        if (schema.schema.required) {
            for (const field of schema.schema.required) {
                if (!(field in config)) {
                    errors.push({
                        type: "MISSING_REQUIRED_FIELD",
                        field: field,
                        message: `Required field ${field} is missing`
                    });
                }
            }
        }

        return {
            valid: errors.length === 0,
            errors: errors,
            warnings: warnings,
            schema: schema.schema,
            timestamp: Date.now()
        };
    }

    getStats() {
        return {
            schemas: this.schemas.size,
            validators: this.customValidators.size,
            strictMode: this.strictMode,
            autoFix: this.autoFix
        };
    }
}

module.exports = { ValidationEngine };
