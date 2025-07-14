# Database Migrations in TuskLang

Migrations are version control for your database, allowing you to define and share your application's database schema. They work hand-in-hand with TuskLang's schema builder to manage your database structure.

## Creating Migrations

```tusk
# Basic migration structure
#migration create_users_table {
    # Run the migration
    up() {
        @schema.create("users", (table) => {
            table.id()
            table.string("name")
            table.string("email").unique()
            table.string("password")
            table.timestamps()
        })
    }
    
    # Reverse the migration
    down() {
        @schema.dropIfExists("users")
    }
}

# Migration with custom timestamp
#migration 2024_01_15_120000_create_posts_table {
    up() {
        @schema.create("posts", (table) => {
            table.id()
            table.string("title")
            table.text("content")
            table.unsignedBigInteger("user_id")
            table.foreign("user_id").references("users.id")
            table.timestamps()
        })
    }
    
    down() {
        @schema.dropIfExists("posts")
    }
}
```

## Table Operations

```tusk
# Creating tables
#migration create_products_table {
    up() {
        @schema.create("products", (table) => {
            table.id()
            table.string("name", 100)
            table.text("description").nullable()
            table.decimal("price", 10, 2)
            table.integer("stock").default(0)
            table.boolean("active").default(true)
            table.timestamps()
            
            # Indexes
            table.index("name")
            table.index(["active", "created_at"])
        })
    }
    
    down() {
        @schema.drop("products")
    }
}

# Modifying tables
#migration add_fields_to_users_table {
    up() {
        @schema.table("users", (table) => {
            table.string("phone").nullable().after("email")
            table.date("birth_date").nullable()
            table.enum("role", ["user", "admin", "moderator"]).default("user")
            table.json("preferences").nullable()
        })
    }
    
    down() {
        @schema.table("users", (table) => {
            table.dropColumn(["phone", "birth_date", "role", "preferences"])
        })
    }
}

# Renaming tables
#migration rename_products_to_items {
    up() {
        @schema.rename("products", "items")
    }
    
    down() {
        @schema.rename("items", "products")
    }
}
```

## Column Types

```tusk
#migration create_comprehensive_table {
    up() {
        @schema.create("examples", (table) => {
            # Numeric types
            table.id()  # Auto-incrementing UNSIGNED BIGINT
            table.integer("count")
            table.tinyInteger("status")
            table.smallInteger("quantity")
            table.mediumInteger("size")
            table.bigInteger("views")
            table.unsignedInteger("positive_only")
            table.float("percentage", 8, 2)
            table.double("amount", 15, 2)
            table.decimal("price", 10, 2)
            
            # String types
            table.char("code", 10)
            table.string("name", 255)  # VARCHAR
            table.text("description")
            table.mediumText("content")
            table.longText("body")
            
            # Date/Time types
            table.date("birth_date")
            table.dateTime("published_at", 0)  # Precision
            table.dateTimeTz("scheduled_at", 0)
            table.time("duration", 0)
            table.timeTz("meeting_time", 0)
            table.timestamp("created_at", 0)
            table.timestampTz("updated_at", 0)
            table.timestamps()  # created_at and updated_at
            table.timestampsTz()
            table.year("birth_year")
            
            # Binary types
            table.binary("data")
            table.blob("file_content")
            
            # Boolean
            table.boolean("is_active")
            
            # JSON
            table.json("metadata")
            table.jsonb("settings")  # Binary JSON (PostgreSQL)
            
            # UUID
            table.uuid("uuid")
            
            # IP Address
            table.ipAddress("visitor_ip")
            table.macAddress("device_mac")
            
            # Geometry (MySQL/PostgreSQL)
            table.geometry("position")
            table.point("location")
            table.lineString("route")
            table.polygon("area")
            
            # Special columns
            table.rememberToken()  # remember_token VARCHAR(100)
            table.morphs("taggable")  # taggable_type, taggable_id
            table.uuidMorphs("tokenable")  # UUID morphs
            table.softDeletes()  # deleted_at timestamp
            table.softDeletesTz()  # deleted_at timestampTz
        })
    }
    
    down() {
        @schema.dropIfExists("examples")
    }
}
```

## Column Modifiers

```tusk
#migration create_table_with_modifiers {
    up() {
        @schema.create("users", (table) => {
            # Modifiers
            table.string("email").unique()
            table.string("username").nullable()
            table.integer("age").unsigned()
            table.decimal("balance").default(0.00)
            table.text("bio").comment("User biography")
            table.timestamp("verified_at").useCurrent()
            table.timestamp("updated_at").useCurrentOnUpdate()
            
            # Column placement
            table.string("middle_name").after("first_name")
            table.boolean("is_admin").first()  # Place first
            
            # Multiple modifiers
            table.string("status")
                .default("pending")
                .nullable()
                .comment("User account status")
            
            # Invisible columns (MySQL 8.0.23+)
            table.string("secret_key").invisible()
            
            # Virtual/Stored columns
            table.string("full_name").virtualAs("CONCAT(first_name, ' ', last_name)")
            table.integer("age").storedAs("YEAR(CURDATE()) - YEAR(birth_date)")
        })
    }
    
    down() {
        @schema.dropIfExists("users")
    }
}
```

## Indexes and Constraints

```tusk
#migration create_indexes_and_constraints {
    up() {
        @schema.create("posts", (table) => {
            table.id()
            table.string("slug")
            table.string("title")
            table.text("content")
            table.unsignedBigInteger("user_id")
            table.unsignedBigInteger("category_id").nullable()
            table.integer("views").default(0)
            table.boolean("published").default(false)
            table.timestamps()
            
            # Simple index
            table.index("slug")
            
            # Named index
            table.index("user_id", "posts_author_index")
            
            # Composite index
            table.index(["published", "created_at"])
            
            # Unique index
            table.unique("slug")
            
            # Spatial index (MySQL/PostgreSQL)
            table.spatialIndex("location")
            
            # Full text index (MySQL)
            table.fullText(["title", "content"])
            
            # Foreign key constraints
            table.foreign("user_id")
                .references("id")
                .on("users")
                .onDelete("cascade")
                .onUpdate("cascade")
            
            table.foreign("category_id")
                .references("id")
                .on("categories")
                .onDelete("set null")
        })
    }
    
    down() {
        @schema.table("posts", (table) => {
            # Drop foreign keys first
            table.dropForeign(["user_id"])
            table.dropForeign(["category_id"])
        })
        
        @schema.dropIfExists("posts")
    }
}

# Adding/dropping indexes to existing tables
#migration add_indexes_to_users {
    up() {
        @schema.table("users", (table) => {
            # Add indexes
            table.index("email", "users_email_index")
            table.unique(["email", "deleted_at"], "unique_active_email")
        })
    }
    
    down() {
        @schema.table("users", (table) => {
            # Drop indexes
            table.dropIndex("users_email_index")
            table.dropUnique("unique_active_email")
        })
    }
}
```

## Modifying Columns

```tusk
#migration modify_users_table {
    up() {
        @schema.table("users", (table) => {
            # Change column type
            table.string("name", 200).change()
            
            # Add nullable
            table.date("birth_date").nullable().change()
            
            # Remove nullable
            table.string("email").nullable(false).change()
            
            # Change default
            table.boolean("active").default(true).change()
            
            # Rename column
            table.renameColumn("name", "full_name")
            
            # Drop column
            table.dropColumn("unnecessary_field")
            
            # Drop multiple columns
            table.dropColumn(["field1", "field2", "field3"])
            
            # Drop timestamps
            table.dropTimestamps()
            
            # Drop soft deletes
            table.dropSoftDeletes()
        })
    }
    
    down() {
        @schema.table("users", (table) => {
            # Reverse changes
            table.string("full_name", 255).change()
            table.date("birth_date").nullable(false).change()
            table.string("email").nullable().change()
            table.boolean("active").default(false).change()
            table.renameColumn("full_name", "name")
            table.string("unnecessary_field")
            table.timestamps()
            table.softDeletes()
        })
    }
}
```

## Advanced Migration Features

```tusk
# Conditional migrations
#migration conditional_changes {
    up() {
        # Only create if doesn't exist
        if (!@schema.hasTable("settings")) {
            @schema.create("settings", (table) => {
                table.id()
                table.string("key").unique()
                table.text("value")
            })
        }
        
        # Only add column if doesn't exist
        if (!@schema.hasColumn("users", "avatar")) {
            @schema.table("users", (table) => {
                table.string("avatar").nullable()
            })
        }
    }
    
    down() {
        @schema.dropIfExists("settings")
        
        if (@schema.hasColumn("users", "avatar")) {
            @schema.table("users", (table) => {
                table.dropColumn("avatar")
            })
        }
    }
}

# Database-specific operations
#migration database_specific {
    up() {
        # PostgreSQL specific
        if (@db.getDriverName() == "pgsql") {
            @db.statement("CREATE EXTENSION IF NOT EXISTS 'uuid-ossp'")
            
            @schema.create("documents", (table) => {
                table.uuid("id").primary().default(@db.raw("uuid_generate_v4()"))
                table.jsonb("data")
                table.tsvector("search_vector")
            })
        }
        
        # MySQL specific
        if (@db.getDriverName() == "mysql") {
            @db.statement("SET FOREIGN_KEY_CHECKS=0")
            // Perform operations
            @db.statement("SET FOREIGN_KEY_CHECKS=1")
        }
    }
    
    down() {
        @schema.dropIfExists("documents")
    }
}

# Data migrations
#migration migrate_user_data {
    up() {
        # Add new column
        @schema.table("users", (table) => {
            table.string("full_name").nullable()
        })
        
        # Migrate data
        users: @db.table("users").get()
        for (user in users) {
            @db.table("users")
                .where("id", user.id)
                .update({
                    full_name: user.first_name + " " + user.last_name
                })
        }
        
        # Drop old columns
        @schema.table("users", (table) => {
            table.dropColumn(["first_name", "last_name"])
        })
    }
    
    down() {
        # Reverse the process
        @schema.table("users", (table) => {
            table.string("first_name").nullable()
            table.string("last_name").nullable()
        })
        
        users: @db.table("users").get()
        for (user in users) {
            parts: user.full_name?.split(" ") || ["", ""]
            @db.table("users")
                .where("id", user.id)
                .update({
                    first_name: parts[0],
                    last_name: parts[1] || ""
                })
        }
        
        @schema.table("users", (table) => {
            table.dropColumn("full_name")
        })
    }
}
```

## Running Migrations

```tusk
# CLI commands
@cli.command("migrate", () => {
    @migrate.run()
})

@cli.command("migrate:rollback", () => {
    @migrate.rollback()
})

@cli.command("migrate:fresh", () => {
    @migrate.fresh()  # Drop all tables and re-run
})

@cli.command("migrate:status", () => {
    @migrate.status()  # Show migration status
})

# Programmatic usage
#web /admin/migrate {
    #auth role: "super-admin"
    
    if (@request.method == "POST") {
        try {
            @migrate.run()
            @flash("success", "Migrations completed successfully")
        } catch (e) {
            @flash("error", "Migration failed: " + e.message)
        }
    }
    
    status: @migrate.getStatus()
    @render("admin/migrations.tusk", {status})
}
```

## Migration Batches and Rollback

```tusk
# Migrations are run in batches
# Each run creates a new batch number
# Rollback removes the last batch

# Custom batch handling
#migration complex_migration {
    batch: 5  # Force specific batch number
    
    up() {
        // Migration logic
    }
    
    down() {
        // Rollback logic
    }
}

# Step-by-step rollback
@migrate.rollback({steps: 3})  # Rollback last 3 migrations

# Rollback to specific batch
@migrate.rollbackTo({batch: 5})
```

## Best Practices

1. **Always provide down() method** - Make migrations reversible
2. **Test migrations** - Run up and down in development
3. **Keep migrations focused** - One concern per migration
4. **Use descriptive names** - Clear migration purpose
5. **Don't modify old migrations** - Create new ones instead
6. **Handle data carefully** - Backup before data migrations
7. **Consider performance** - Large tables need careful handling
8. **Use transactions** - Wrap complex operations

## Related Topics

- `database-overview` - Database configuration
- `schema-builder` - Schema building methods
- `seeding` - Database seeding
- `testing-databases` - Testing with migrations
- `deployment` - Migration strategies