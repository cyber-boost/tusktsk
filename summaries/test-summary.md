# Test Summary Document

This is a test markdown file to verify the new .gitTrick feature in gitTrick.sh.

## Features Tested

- File copying to summaries/ directory
- Content extraction for commit message
- Git commit and push functionality
- Error handling for non-existent files

## Expected Behavior

The script should:
1. Copy this file to summaries/test-summary.md
2. Extract the first 100 characters for commit message
3. Commit with message: "docs: Add test-summary.md to summaries - Test Summary Document This is a test markdown file to verify the new .gitTrick feature in gitTrick.sh..."
4. Push to remote repository

This test will verify that the new .gitTrick command works correctly. 