# Version Control

TuskLang provides powerful version control capabilities that integrate seamlessly with Git and other version control systems. This guide covers comprehensive version control strategies for Go applications.

## Version Control Philosophy

### Git-First Development
```go
// Git-first development with TuskLang
type GitManager struct {
    config *tusk.Config
    repo   *git.Repository
}

func NewGitManager(config *tusk.Config) (*GitManager, error) {
    repo, err := git.PlainOpen(".")
    if err != nil {
        return nil, fmt.Errorf("failed to open git repository: %w", err)
    }
    
    return &GitManager{
        config: config,
        repo:   repo,
    }, nil
}

// CommitWithTuskLang creates a commit with TuskLang configuration changes
func (gm *GitManager) CommitWithTuskLang(message string, configChanges map[string]interface{}) error {
    // Stage TuskLang configuration files
    if err := gm.stageTuskLangFiles(); err != nil {
        return fmt.Errorf("failed to stage TuskLang files: %w", err)
    }
    
    // Create commit with structured message
    commitMessage := gm.formatCommitMessage(message, configChanges)
    
    // Create commit
    if err := gm.createCommit(commitMessage); err != nil {
        return fmt.Errorf("failed to create commit: %w", err)
    }
    
    return nil
}

func (gm *GitManager) formatCommitMessage(message string, configChanges map[string]interface{}) string {
    // Format commit message with TuskLang changes
    formatted := fmt.Sprintf("feat: %s\n\n", message)
    
    if len(configChanges) > 0 {
        formatted += "TuskLang Changes:\n"
        for key, value := range configChanges {
            formatted += fmt.Sprintf("- %s: %v\n", key, value)
        }
    }
    
    return formatted
}
```

### Semantic Versioning
```go
// Semantic versioning with TuskLang
type SemanticVersioner struct {
    config *tusk.Config
}

type Version struct {
    Major int
    Minor int
    Patch int
    PreRelease string
    Build string
}

func (sv *SemanticVersioner) BumpVersion(bumpType string) error {
    currentVersion := sv.getCurrentVersion()
    
    var newVersion Version
    switch bumpType {
    case "major":
        newVersion = Version{
            Major: currentVersion.Major + 1,
            Minor: 0,
            Patch: 0,
        }
    case "minor":
        newVersion = Version{
            Major: currentVersion.Major,
            Minor: currentVersion.Minor + 1,
            Patch: 0,
        }
    case "patch":
        newVersion = Version{
            Major: currentVersion.Major,
            Minor: currentVersion.Minor,
            Patch: currentVersion.Patch + 1,
        }
    default:
        return fmt.Errorf("invalid bump type: %s", bumpType)
    }
    
    // Update version in TuskLang configuration
    if err := sv.updateVersionInConfig(newVersion); err != nil {
        return fmt.Errorf("failed to update version in config: %w", err)
    }
    
    // Create version tag
    if err := sv.createVersionTag(newVersion); err != nil {
        return fmt.Errorf("failed to create version tag: %w", err)
    }
    
    return nil
}

func (sv *SemanticVersioner) updateVersionInConfig(version Version) error {
    // Update version in TuskLang configuration
    versionString := fmt.Sprintf("%d.%d.%d", version.Major, version.Minor, version.Patch)
    
    // Update version in config file
    configPath := sv.config.GetString("version_control.config_file", "config.tsk")
    config, err := tusk.LoadConfig(configPath)
    if err != nil {
        return err
    }
    
    config.Set("version", versionString)
    
    return config.Save(configPath)
}
```

## TuskLang Version Control Configuration

### Git Integration Configuration
```tsk
# Version control configuration
version_control {
    # Git configuration
    git {
        enabled = true
        auto_commit = true
        auto_push = false
        commit_message_template = "feat: {message}\n\nTuskLang Changes:\n{changes}"
        branch_naming = "feature/{ticket}-{description}"
        tag_naming = "v{major}.{minor}.{patch}"
    }
    
    # Version management
    version_management {
        semantic_versioning = true
        auto_bump = false
        bump_on_feature = "minor"
        bump_on_fix = "patch"
        bump_on_breaking = "major"
        version_file = "VERSION"
        changelog_file = "CHANGELOG.md"
    }
    
    # Branch strategy
    branch_strategy {
        main_branch = "main"
        develop_branch = "develop"
        feature_prefix = "feature/"
        hotfix_prefix = "hotfix/"
        release_prefix = "release/"
        auto_merge = false
    }
    
    # Code review
    code_review {
        required = true
        min_reviewers = 2
        auto_assign = true
        template = "code_review_template.md"
    }
}
```

### Workflow Configuration
```tsk
# Workflow configuration
workflow {
    # Development workflow
    development {
        create_feature_branch = true
        run_tests = true
        run_linting = true
        run_security_scan = true
        auto_rebase = true
    }
    
    # Release workflow
    release {
        create_release_branch = true
        run_integration_tests = true
        run_performance_tests = true
        create_release_notes = true
        auto_deploy = false
    }
    
    # Hotfix workflow
    hotfix {
        create_hotfix_branch = true
        run_critical_tests = true
        fast_track_review = true
        auto_merge = true
    }
}
```

## Go Version Control Implementation

### Git Operations
```go
// Comprehensive Git operations
type GitOperations struct {
    config *tusk.Config
    repo   *git.Repository
}

func (go *GitOperations) CreateFeatureBranch(ticket string, description string) error {
    // Create feature branch
    branchName := fmt.Sprintf("feature/%s-%s", ticket, sanitizeDescription(description))
    
    // Checkout main branch
    if err := go.checkoutBranch("main"); err != nil {
        return fmt.Errorf("failed to checkout main branch: %w", err)
    }
    
    // Pull latest changes
    if err := go.pullLatest(); err != nil {
        return fmt.Errorf("failed to pull latest changes: %w", err)
    }
    
    // Create and checkout feature branch
    if err := go.createAndCheckoutBranch(branchName); err != nil {
        return fmt.Errorf("failed to create feature branch: %w", err)
    }
    
    return nil
}

func (go *GitOperations) CreatePullRequest(title string, description string) error {
    // Create pull request
    pr := &PullRequest{
        Title:       title,
        Description: description,
        SourceBranch: go.getCurrentBranch(),
        TargetBranch: "main",
    }
    
    // Add reviewers
    reviewers := go.config.GetStringSlice("version_control.code_review.reviewers", []string{})
    pr.Reviewers = reviewers
    
    // Create PR via API
    if err := go.createPRViaAPI(pr); err != nil {
        return fmt.Errorf("failed to create pull request: %w", err)
    }
    
    return nil
}

func (go *GitOperations) MergePullRequest(prID string) error {
    // Merge pull request
    if err := go.mergePRViaAPI(prID); err != nil {
        return fmt.Errorf("failed to merge pull request: %w", err)
    }
    
    // Delete feature branch
    if err := go.deleteFeatureBranch(); err != nil {
        return fmt.Errorf("failed to delete feature branch: %w", err)
    }
    
    return nil
}
```

### Branch Management
```go
// Branch management utilities
type BranchManager struct {
    config *tusk.Config
    repo   *git.Repository
}

func (bm *BranchManager) ListBranches() ([]Branch, error) {
    branches, err := bm.repo.Branches()
    if err != nil {
        return nil, fmt.Errorf("failed to list branches: %w", err)
    }
    
    var branchList []Branch
    err = branches.ForEach(func(ref *plumbing.Reference) error {
        branch := Branch{
            Name:   ref.Name().String(),
            Commit: ref.Hash().String(),
        }
        branchList = append(branchList, branch)
        return nil
    })
    
    return branchList, err
}

func (bm *BranchManager) CleanupBranches() error {
    // Get merged branches
    mergedBranches, err := bm.getMergedBranches()
    if err != nil {
        return fmt.Errorf("failed to get merged branches: %w", err)
    }
    
    // Delete merged feature branches
    for _, branch := range mergedBranches {
        if strings.HasPrefix(branch.Name, "feature/") {
            if err := bm.deleteBranch(branch.Name); err != nil {
                log.Printf("Failed to delete branch %s: %v", branch.Name, err)
            }
        }
    }
    
    return nil
}

func (bm *BranchManager) getMergedBranches() ([]Branch, error) {
    // Get branches merged into main
    cmd := exec.Command("git", "branch", "--merged", "main")
    output, err := cmd.Output()
    if err != nil {
        return nil, fmt.Errorf("failed to get merged branches: %w", err)
    }
    
    var branches []Branch
    lines := strings.Split(string(output), "\n")
    for _, line := range lines {
        line = strings.TrimSpace(line)
        if line != "" && line != "* main" {
            branches = append(branches, Branch{Name: line})
        }
    }
    
    return branches, nil
}
```

### Tag Management
```go
// Tag management utilities
type TagManager struct {
    config *tusk.Config
    repo   *git.Repository
}

func (tm *TagManager) CreateVersionTag(version Version) error {
    // Create tag name
    tagName := fmt.Sprintf("v%d.%d.%d", version.Major, version.Minor, version.Patch)
    
    // Create tag message
    tagMessage := fmt.Sprintf("Release version %s", tagName)
    
    // Create tag
    head, err := tm.repo.Head()
    if err != nil {
        return fmt.Errorf("failed to get HEAD: %w", err)
    }
    
    _, err = tm.repo.CreateTag(tagName, head.Hash(), &git.CreateTagOptions{
        Message: tagMessage,
    })
    if err != nil {
        return fmt.Errorf("failed to create tag: %w", err)
    }
    
    // Push tag
    if err := tm.pushTag(tagName); err != nil {
        return fmt.Errorf("failed to push tag: %w", err)
    }
    
    return nil
}

func (tm *TagManager) ListTags() ([]Tag, error) {
    tags, err := tm.repo.Tags()
    if err != nil {
        return nil, fmt.Errorf("failed to list tags: %w", err)
    }
    
    var tagList []Tag
    err = tags.ForEach(func(ref *plumbing.Reference) error {
        tag := Tag{
            Name:   ref.Name().String(),
            Commit: ref.Hash().String(),
        }
        tagList = append(tagList, tag)
        return nil
    })
    
    return tagList, err
}
```

## Advanced Version Control Features

### Automated Workflows
```go
// Automated version control workflows
type AutomatedWorkflow struct {
    config *tusk.Config
    git    *GitOperations
}

func (aw *AutomatedWorkflow) FeatureWorkflow(ticket string, description string) error {
    // Create feature branch
    if err := aw.git.CreateFeatureBranch(ticket, description); err != nil {
        return fmt.Errorf("failed to create feature branch: %w", err)
    }
    
    // Run development checks
    if err := aw.runDevelopmentChecks(); err != nil {
        return fmt.Errorf("development checks failed: %w", err)
    }
    
    // Auto-commit if enabled
    if aw.config.GetBool("version_control.git.auto_commit") {
        if err := aw.autoCommit(); err != nil {
            return fmt.Errorf("auto-commit failed: %w", err)
        }
    }
    
    return nil
}

func (aw *AutomatedWorkflow) runDevelopmentChecks() error {
    // Run tests
    if aw.config.GetBool("workflow.development.run_tests") {
        if err := aw.runTests(); err != nil {
            return fmt.Errorf("tests failed: %w", err)
        }
    }
    
    // Run linting
    if aw.config.GetBool("workflow.development.run_linting") {
        if err := aw.runLinting(); err != nil {
            return fmt.Errorf("linting failed: %w", err)
        }
    }
    
    // Run security scan
    if aw.config.GetBool("workflow.development.run_security_scan") {
        if err := aw.runSecurityScan(); err != nil {
            return fmt.Errorf("security scan failed: %w", err)
        }
    }
    
    return nil
}

func (aw *AutomatedWorkflow) ReleaseWorkflow() error {
    // Create release branch
    if err := aw.createReleaseBranch(); err != nil {
        return fmt.Errorf("failed to create release branch: %w", err)
    }
    
    // Run integration tests
    if aw.config.GetBool("workflow.release.run_integration_tests") {
        if err := aw.runIntegrationTests(); err != nil {
            return fmt.Errorf("integration tests failed: %w", err)
        }
    }
    
    // Run performance tests
    if aw.config.GetBool("workflow.release.run_performance_tests") {
        if err := aw.runPerformanceTests(); err != nil {
            return fmt.Errorf("performance tests failed: %w", err)
        }
    }
    
    // Create release notes
    if aw.config.GetBool("workflow.release.create_release_notes") {
        if err := aw.createReleaseNotes(); err != nil {
            return fmt.Errorf("failed to create release notes: %w", err)
        }
    }
    
    return nil
}
```

### Changelog Generation
```go
// Automated changelog generation
type ChangelogGenerator struct {
    config *tusk.Config
    repo   *git.Repository
}

func (cg *ChangelogGenerator) GenerateChangelog(fromTag, toTag string) (*Changelog, error) {
    // Get commits between tags
    commits, err := cg.getCommitsBetweenTags(fromTag, toTag)
    if err != nil {
        return nil, fmt.Errorf("failed to get commits: %w", err)
    }
    
    // Categorize commits
    categorized := cg.categorizeCommits(commits)
    
    // Generate changelog
    changelog := &Changelog{
        Version:     toTag,
        Date:        time.Now(),
        Categories:  categorized,
    }
    
    return changelog, nil
}

func (cg *ChangelogGenerator) categorizeCommits(commits []Commit) map[string][]Commit {
    categorized := make(map[string][]Commit)
    
    for _, commit := range commits {
        category := cg.getCommitCategory(commit)
        categorized[category] = append(categorized[category], commit)
    }
    
    return categorized
}

func (cg *ChangelogGenerator) getCommitCategory(commit Commit) string {
    // Parse commit message to determine category
    message := commit.Message
    
    if strings.HasPrefix(message, "feat:") {
        return "Features"
    } else if strings.HasPrefix(message, "fix:") {
        return "Bug Fixes"
    } else if strings.HasPrefix(message, "docs:") {
        return "Documentation"
    } else if strings.HasPrefix(message, "style:") {
        return "Style"
    } else if strings.HasPrefix(message, "refactor:") {
        return "Refactoring"
    } else if strings.HasPrefix(message, "test:") {
        return "Tests"
    } else if strings.HasPrefix(message, "chore:") {
        return "Chores"
    }
    
    return "Other"
}

type Changelog struct {
    Version    string
    Date       time.Time
    Categories map[string][]Commit
}
```

### Conflict Resolution
```go
// Conflict resolution utilities
type ConflictResolver struct {
    config *tusk.Config
    repo   *git.Repository
}

func (cr *ConflictResolver) ResolveConflicts() error {
    // Check for conflicts
    conflicts, err := cr.getConflicts()
    if err != nil {
        return fmt.Errorf("failed to get conflicts: %w", err)
    }
    
    if len(conflicts) == 0 {
        return nil
    }
    
    // Resolve each conflict
    for _, conflict := range conflicts {
        if err := cr.resolveConflict(conflict); err != nil {
            return fmt.Errorf("failed to resolve conflict in %s: %w", conflict.File, err)
        }
    }
    
    // Stage resolved files
    if err := cr.stageResolvedFiles(); err != nil {
        return fmt.Errorf("failed to stage resolved files: %w", err)
    }
    
    return nil
}

func (cr *ConflictResolver) getConflicts() ([]Conflict, error) {
    // Get list of conflicted files
    cmd := exec.Command("git", "diff", "--name-only", "--diff-filter=U")
    output, err := cmd.Output()
    if err != nil {
        return nil, fmt.Errorf("failed to get conflicted files: %w", err)
    }
    
    var conflicts []Conflict
    files := strings.Split(string(output), "\n")
    for _, file := range files {
        file = strings.TrimSpace(file)
        if file != "" {
            conflicts = append(conflicts, Conflict{File: file})
        }
    }
    
    return conflicts, nil
}

type Conflict struct {
    File string
}
```

## Version Control Tools and Utilities

### Git Hooks
```go
// Git hooks integration
type GitHooks struct {
    config *tusk.Config
}

func (gh *GitHooks) InstallHooks() error {
    hooks := map[string]string{
        "pre-commit":  gh.generatePreCommitHook(),
        "commit-msg":  gh.generateCommitMsgHook(),
        "post-commit": gh.generatePostCommitHook(),
        "pre-push":    gh.generatePrePushHook(),
    }
    
    for hookName, content := range hooks {
        if err := gh.installHook(hookName, content); err != nil {
            return fmt.Errorf("failed to install %s hook: %w", hookName, err)
        }
    }
    
    return nil
}

func (gh *GitHooks) generatePreCommitHook() string {
    return `#!/bin/sh
# Pre-commit hook
echo "Running pre-commit checks..."

# Run tests
go test ./...

# Run linting
golangci-lint run

# Run security scan
gosec ./...

echo "Pre-commit checks completed"
`
}

func (gh *GitHooks) generateCommitMsgHook() string {
    return `#!/bin/sh
# Commit message hook
commit_msg_file=$1
commit_msg=$(cat $commit_msg_file)

# Validate commit message format
if ! echo "$commit_msg" | grep -qE "^(feat|fix|docs|style|refactor|test|chore)(\(.+\))?: .+"; then
    echo "Invalid commit message format. Use: type(scope): description"
    exit 1
fi
`
}
```

### Repository Management
```go
// Repository management utilities
type RepositoryManager struct {
    config *tusk.Config
}

func (rm *RepositoryManager) InitializeRepository() error {
    // Initialize git repository
    if err := rm.initGit(); err != nil {
        return fmt.Errorf("failed to initialize git: %w", err)
    }
    
    // Create initial commit
    if err := rm.createInitialCommit(); err != nil {
        return fmt.Errorf("failed to create initial commit: %w", err)
    }
    
    // Setup remote
    if err := rm.setupRemote(); err != nil {
        return fmt.Errorf("failed to setup remote: %w", err)
    }
    
    // Install hooks
    if err := rm.installHooks(); err != nil {
        return fmt.Errorf("failed to install hooks: %w", err)
    }
    
    return nil
}

func (rm *RepositoryManager) initGit() error {
    cmd := exec.Command("git", "init")
    return cmd.Run()
}

func (rm *RepositoryManager) createInitialCommit() error {
    // Add all files
    cmd := exec.Command("git", "add", ".")
    if err := cmd.Run(); err != nil {
        return err
    }
    
    // Create initial commit
    cmd = exec.Command("git", "commit", "-m", "Initial commit")
    return cmd.Run()
}
```

## Validation and Error Handling

### Version Control Configuration Validation
```go
// Validate version control configuration
func ValidateVersionControlConfig(config *tusk.Config) error {
    if config == nil {
        return errors.New("version control config cannot be nil")
    }
    
    // Validate git configuration
    if !config.Has("version_control.git") {
        return errors.New("missing git configuration")
    }
    
    // Validate version management
    if !config.Has("version_control.version_management") {
        return errors.New("missing version management configuration")
    }
    
    return nil
}
```

### Error Handling
```go
// Handle version control errors gracefully
func handleVersionControlError(err error, context string) {
    log.Printf("Version control error in %s: %v", context, err)
    
    // Log additional context if available
    if vcErr, ok := err.(*VersionControlError); ok {
        log.Printf("Version control context: %s", vcErr.Context)
    }
}
```

## Performance Considerations

### Version Control Performance
```go
// Optimize version control operations
type VersionControlOptimizer struct {
    config *tusk.Config
}

func (vco *VersionControlOptimizer) OptimizeOperations() error {
    // Enable parallel operations
    if vco.config.GetBool("version_control.performance.parallel") {
        runtime.GOMAXPROCS(runtime.NumCPU())
    }
    
    // Setup caching
    if vco.config.GetBool("version_control.performance.cache") {
        vco.setupCache()
    }
    
    // Optimize file operations
    if err := vco.optimizeFileOperations(); err != nil {
        return fmt.Errorf("failed to optimize file operations: %w", err)
    }
    
    return nil
}

func (vco *VersionControlOptimizer) setupCache() {
    // Setup version control cache
    cacheDir := vco.config.GetString("version_control.performance.cache_dir", ".vccache")
    os.MkdirAll(cacheDir, 0755)
}
```

## Version Control Notes

- **Git Integration**: Seamless integration with Git workflows
- **Semantic Versioning**: Automated semantic versioning
- **Branch Management**: Comprehensive branch management
- **Automated Workflows**: Automated development and release workflows
- **Conflict Resolution**: Automated conflict resolution
- **Changelog Generation**: Automated changelog generation
- **Code Review**: Integrated code review process
- **Repository Management**: Complete repository management

## Best Practices

1. **Git-First**: Use Git as the primary version control system
2. **Semantic Versioning**: Follow semantic versioning principles
3. **Branch Strategy**: Use a clear branch strategy
4. **Automated Workflows**: Automate repetitive tasks
5. **Code Review**: Require code review for all changes
6. **Conflict Resolution**: Handle conflicts promptly
7. **Changelog**: Maintain comprehensive changelogs
8. **Repository Management**: Proper repository setup and maintenance

## Integration with TuskLang

```go
// Load version control configuration from TuskLang
func LoadVersionControlConfig(configPath string) (*tusk.Config, error) {
    config, err := tusk.LoadConfig(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to load version control config: %w", err)
    }
    
    // Validate version control configuration
    if err := ValidateVersionControlConfig(config); err != nil {
        return nil, fmt.Errorf("invalid version control config: %w", err)
    }
    
    return config, nil
}
```

This version control guide provides comprehensive version control capabilities for your Go applications using TuskLang. Remember, good version control is essential for collaborative development. 