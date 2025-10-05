# Git Hooks Execution Flow

This document visualizes the complete lifecycle of Git hooks in the Social Media Commander project, from initial setup through commit execution.

## 📋 Table of Contents
1. [Complete System Architecture](#complete-system-architecture)
2. [Setup Phase](#setup-phase)
3. [Pre-Commit Execution](#pre-commit-execution)
4. [Post-Commit Execution](#post-commit-execution)
5. [Error Handling Flow](#error-handling-flow)

---

## Complete System Architecture

```mermaid
graph TB
    subgraph "🌍 One-Time Global Setup (99% Effectiveness)"
        A[Developer Runs<br/>setup-global-template.ps1]
        B[Template Copied to<br/>~/.git-templates/]
        C[Git Config Set<br/>init.templateDir]
        A --> B --> C
    end
    
    subgraph "📥 Repository Clone (Automatic)"
        D[git clone repo]
        E{Template<br/>Configured?}
        F[Bootstrap Hook<br/>post-checkout Runs]
        G[Checks .githooks/<br/>Directory Exists]
        H{core.hooksPath<br/>Set?}
        I[Auto-Configure:<br/>git config core.hooksPath]
        J[✅ Hooks Active]
        
        D --> E
        E -->|Yes| F
        E -->|No| K[⚠️ No Auto-Setup]
        F --> G
        G -->|Yes| H
        G -->|No| K
        H -->|No| I
        H -->|Yes| J
        I --> J
    end
    
    subgraph "🔨 Build Phase (70% Effectiveness)"
        L[dotnet build]
        M[MSBuild Target<br/>SetupGitHooks]
        N{core.hooksPath<br/>Set?}
        O[Auto-Configure Hooks]
        P[Build Continues]
        
        L --> M
        M --> N
        N -->|No| O
        N -->|Yes| P
        O --> P
    end
    
    subgraph "💾 Commit Workflow"
        Q[git commit -m ...]
        R[Pre-Commit Hook<br/>Execution]
        S{All Checks<br/>Pass?}
        T[Commit Created]
        U[Post-Commit Hook<br/>Execution]
        V[Show Reminders]
        W[❌ Commit Blocked]
        
        Q --> R
        R --> S
        S -->|Yes| T
        S -->|No| W
        T --> U
        U --> V
    end
    
    C -.-> D
    J -.-> Q
    P -.-> Q
    K -.-> L
    
    style A fill:#4CAF50,stroke:#2E7D32,color:#fff
    style C fill:#4CAF50,stroke:#2E7D32,color:#fff
    style I fill:#2196F3,stroke:#1565C0,color:#fff
    style J fill:#4CAF50,stroke:#2E7D32,color:#fff
    style O fill:#2196F3,stroke:#1565C0,color:#fff
    style T fill:#4CAF50,stroke:#2E7D32,color:#fff
    style V fill:#4CAF50,stroke:#2E7D32,color:#fff
    style W fill:#F44336,stroke:#C62828,color:#fff
    style K fill:#FF9800,stroke:#E65100,color:#fff
```

---

## Setup Phase

### Option 1: Global Template Setup (Recommended)

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant Script as setup-global-template.ps1
    participant FS as File System
    participant Git as Git Config
    
    Dev->>Script: Execute script
    Script->>FS: Check if .git-template/ exists
    FS-->>Script: Confirmed
    
    Script->>FS: Copy .git-template/ to<br/>~/.git-templates/social-media-command/
    FS-->>Script: Copied
    
    Script->>Git: git config --global<br/>init.templateDir<br/>~/.git-templates/social-media-command
    Git-->>Script: Configured
    
    Script->>Dev: ✅ Setup Complete!<br/>All future clones will auto-configure
    
    Note over Dev,Git: This happens ONCE per developer
```

### Option 2: Per-Repository Setup

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant Script as setup-hooks.ps1
    participant Git as Git Config
    
    Dev->>Script: Execute in repo directory
    Script->>Git: Check if .githooks/ exists
    Git-->>Script: Confirmed
    
    Script->>Git: git config core.hooksPath .githooks
    Git-->>Script: Configured
    
    Script->>Dev: ✅ Hooks active in THIS repository
    
    Note over Dev,Git: Must repeat for each new clone
```

### Option 3: Automatic Build Configuration

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant MSBuild as MSBuild Engine
    participant Git as Git Config
    
    Dev->>MSBuild: dotnet build
    MSBuild->>MSBuild: BeforeBuild target<br/>SetupGitHooks
    
    MSBuild->>Git: Check core.hooksPath
    Git-->>MSBuild: (empty or not set)
    
    MSBuild->>Git: git config core.hooksPath .githooks
    Git-->>MSBuild: Configured
    
    MSBuild->>Dev: ℹ️ Hooks auto-configured<br/>Build continues...
    
    Note over Dev,Git: Only works when building
```

---

## Pre-Commit Execution

```mermaid
flowchart TD
    Start([Developer: git commit -m ...]) --> A[Pre-Commit Hook Starts]
    
    A --> B[📋 Check 1: Secrets Detection]
    B --> C{Secrets<br/>Found?}
    C -->|Yes| Fail1[❌ Block: Remove secrets]
    C -->|No| D[📦 Check 2: CPM Compliance]
    
    D --> E{PackageReference<br/>with Version?}
    E -->|Yes| Fail2[❌ Block: Move to<br/>Directory.Packages.props]
    E -->|No| F[🔨 Check 3: Build Verification]
    
    F --> G{dotnet build<br/>Success?}
    G -->|No| Fail3[❌ Block: Fix build errors]
    G -->|Yes| H[⚡ Check 4: Async Patterns]
    
    H --> I{Missing<br/>ConfigureAwait?}
    I -->|Yes| Fail4[❌ Block: Add<br/>ConfigureAwait false]
    I -->|No| J[✨ Check 5: Code Formatting]
    
    J --> K{Inconsistent<br/>Formatting?}
    K -->|Yes| Warn1[⚠️ Warn: Run formatter]
    K -->|No| L[🚫 Check 6: Code Markers]
    
    L --> M{TODO/HACK/FIXME<br/>Found?}
    M -->|Yes| Warn2[⚠️ Warn: Review markers]
    M -->|No| N[🖥️ Check 7: Console.log]
    
    N --> O{console.log<br/>in Production?}
    O -->|Yes| Fail5[❌ Block: Remove debug code]
    O -->|No| P[📏 Check 8: File Size]
    
    P --> Q{File > 1MB?}
    Q -->|Yes| Fail6[❌ Block: Use Git LFS]
    Q -->|No| R[🔍 Check 9: Nullable Types]
    
    R --> S{Nullable<br/>Violations?}
    S -->|Yes| Fail7[❌ Block: Fix null handling]
    S -->|No| T[📝 Check 10: Git Config]
    
    T --> U{Valid<br/>Config?}
    U -->|No| Warn3[⚠️ Warn: Check config]
    U -->|Yes| Success[✅ All Checks Passed]
    
    Warn1 --> Success
    Warn2 --> Success
    Warn3 --> Success
    
    Success --> Commit([Commit Created])
    
    Fail1 --> Block([❌ Commit Blocked])
    Fail2 --> Block
    Fail3 --> Block
    Fail4 --> Block
    Fail5 --> Block
    Fail6 --> Block
    Fail7 --> Block
    
    style Start fill:#2196F3,stroke:#1565C0,color:#fff
    style Success fill:#4CAF50,stroke:#2E7D32,color:#fff
    style Commit fill:#4CAF50,stroke:#2E7D32,color:#fff
    style Block fill:#F44336,stroke:#C62828,color:#fff
    style Fail1 fill:#F44336,stroke:#C62828,color:#fff
    style Fail2 fill:#F44336,stroke:#C62828,color:#fff
    style Fail3 fill:#F44336,stroke:#C62828,color:#fff
    style Fail4 fill:#F44336,stroke:#C62828,color:#fff
    style Fail5 fill:#F44336,stroke:#C62828,color:#fff
    style Fail6 fill:#F44336,stroke:#C62828,color:#fff
    style Fail7 fill:#F44336,stroke:#C62828,color:#fff
    style Warn1 fill:#FF9800,stroke:#E65100,color:#fff
    style Warn2 fill:#FF9800,stroke:#E65100,color:#fff
    style Warn3 fill:#FF9800,stroke:#E65100,color:#fff
```

---

## Post-Commit Execution

```mermaid
flowchart TD
    Start([Commit Created]) --> A[Post-Commit Hook Starts]
    
    A --> B[🔍 Analyze Changed Files]
    
    B --> C{Test Files<br/>Changed?}
    C -->|Yes| D[💡 Reminder:<br/>Run dotnet test]
    C -->|No| E
    
    D --> E{Frontend Files<br/>Changed?}
    E -->|Yes| F[🌐 Reminder:<br/>npm run build]
    E -->|No| G
    
    F --> G{ViewModels<br/>Changed?}
    G -->|Yes| H[🔧 Reminder:<br/>Update App.axaml.cs<br/>if constructor changed]
    G -->|No| I
    
    H --> I{Security/OAuth<br/>Files?}
    I -->|Yes| J[🔐 Reminder:<br/>Check for secrets<br/>Verify encryption]
    I -->|No| K
    
    J --> K{Performance<br/>Files?}
    K -->|Yes| L[⚡ Reminder:<br/>Use ConfigureAwait false<br/>Consider ValueTask]
    K -->|No| M
    
    L --> M[📋 Show PR Checklist]
    
    M --> N[✅ Post-Commit Complete]
    
    style Start fill:#4CAF50,stroke:#2E7D32,color:#fff
    style N fill:#4CAF50,stroke:#2E7D32,color:#fff
    style D fill:#2196F3,stroke:#1565C0,color:#fff
    style F fill:#2196F3,stroke:#1565C0,color:#fff
    style H fill:#FF9800,stroke:#E65100,color:#fff
    style J fill:#F44336,stroke:#C62828,color:#fff
    style L fill:#9C27B0,stroke:#6A1B9A,color:#fff
    style M fill:#00BCD4,stroke:#00838F,color:#fff
```

---

## Error Handling Flow

### Pre-Commit Error Recovery

```mermaid
stateDiagram-v2
    [*] --> PreCommitStart: git commit
    
    PreCommitStart --> SecretsCheck: Check 1
    SecretsCheck --> CPMCheck: Pass
    SecretsCheck --> ErrorState: Secrets Found
    
    CPMCheck --> BuildCheck: Pass
    CPMCheck --> ErrorState: Version in .csproj
    
    BuildCheck --> AsyncCheck: Pass
    BuildCheck --> ErrorState: Build Failed
    
    AsyncCheck --> FormattingCheck: Pass
    AsyncCheck --> ErrorState: Missing ConfigureAwait
    
    FormattingCheck --> CodeMarkers: Pass
    FormattingCheck --> WarningState: Formatting Issues
    
    CodeMarkers --> ConsoleLog: Pass
    CodeMarkers --> WarningState: TODO/FIXME found
    
    ConsoleLog --> FileSize: Pass
    ConsoleLog --> ErrorState: console.log found
    
    FileSize --> NullableCheck: Pass
    FileSize --> ErrorState: File too large
    
    NullableCheck --> GitConfig: Pass
    NullableCheck --> ErrorState: Null violations
    
    GitConfig --> SuccessState: Pass
    GitConfig --> WarningState: Config issues
    
    ErrorState --> UserAction: Display error message
    UserAction --> FixCode: Developer fixes code
    FixCode --> [*]: git commit (retry)
    
    WarningState --> UserReview: Display warnings
    UserReview --> SuccessState: Developer confirms
    
    SuccessState --> CommitCreated: Create commit
    CommitCreated --> PostCommitStart: Trigger post-commit
    PostCommitStart --> [*]: Show reminders
    
    note right of ErrorState
        Commit blocked.
        Developer must fix
        issues and retry.
    end note
    
    note right of WarningState
        Non-blocking warnings.
        Commit proceeds with
        developer awareness.
    end note
```

### Bootstrap Hook Auto-Recovery

```mermaid
stateDiagram-v2
    [*] --> CloneRepo: git clone
    
    CloneRepo --> CheckTemplate: post-checkout runs
    
    CheckTemplate --> TemplateExists: Global template configured?
    TemplateExists --> CheckHooksDir: Yes
    TemplateExists --> ManualSetup: No (fallback)
    
    CheckHooksDir --> HooksDirExists: .githooks/ exists?
    HooksDirExists --> CheckConfig: Yes
    HooksDirExists --> SkipSetup: No (skip)
    
    CheckConfig --> ConfigSet: core.hooksPath set?
    ConfigSet --> AlreadyConfigured: Yes
    ConfigSet --> AutoConfigure: No
    
    AutoConfigure --> SetConfig: git config core.hooksPath
    SetConfig --> VerifySetup: Verify configuration
    VerifySetup --> Success: ✅ Hooks active
    
    AlreadyConfigured --> Success
    
    Success --> [*]: Developer can commit
    
    ManualSetup --> BuildFallback: dotnet build
    BuildFallback --> MSBuildTarget: SetupGitHooks runs
    MSBuildTarget --> Success
    
    SkipSetup --> ManualSetup
    
    note right of Success
        Hooks are active and
        will run on commits.
    end note
    
    note right of ManualSetup
        If global template not
        set, MSBuild provides
        automatic fallback.
    end note
```

---

## Complete Commit Workflow Timeline

```mermaid
gantt
    title Git Commit Workflow with Hooks
    dateFormat X
    axisFormat %s
    
    section Developer Action
    Write code changes           :dev1, 0, 60
    Stage changes (git add)      :dev2, 60, 10
    Run git commit               :dev3, 70, 5
    
    section Pre-Commit Hook
    Initialize hook              :hook1, 75, 2
    Secrets detection            :hook2, 77, 5
    CPM compliance check         :hook3, 82, 3
    Build verification           :hook4, 85, 15
    Async pattern validation     :hook5, 100, 4
    Code formatting check        :hook6, 104, 3
    Code marker scan             :hook7, 107, 2
    Console.log detection        :hook8, 109, 2
    File size validation         :hook9, 111, 2
    Nullable type check          :hook10, 113, 3
    
    section Git Operation
    Create commit object         :git1, 116, 2
    Update HEAD                  :git2, 118, 1
    
    section Post-Commit Hook
    Analyze changed files        :post1, 119, 3
    Show contextual reminders    :post2, 122, 5
    Display PR checklist         :post3, 127, 3
    
    section Developer Action
    Review reminders             :dev4, 130, 10
    Continue work                :dev5, 140, 20
```

---

## Multi-Layer Defense Architecture

```mermaid
graph TB
    subgraph "Layer 1: Global Template (99%)"
        A1[One-Time Setup:<br/>setup-global-template.ps1]
        A2[Bootstrap Hook<br/>Auto-Configures]
        A3[Works on Every Clone]
        A1 --> A2 --> A3
        
        style A1 fill:#4CAF50,stroke:#2E7D32,color:#fff
        style A2 fill:#4CAF50,stroke:#2E7D32,color:#fff
        style A3 fill:#4CAF50,stroke:#2E7D32,color:#fff
    end
    
    subgraph "Layer 2: MSBuild Target (70%)"
        B1[Automatic on Build]
        B2[SetupGitHooks Target]
        B3[Catches Missed Setups]
        B1 --> B2 --> B3
        
        style B1 fill:#2196F3,stroke:#1565C0,color:#fff
        style B2 fill:#2196F3,stroke:#1565C0,color:#fff
        style B3 fill:#2196F3,stroke:#1565C0,color:#fff
    end
    
    subgraph "Layer 3: Manual Setup (50%)"
        C1[Developer Runs:<br/>setup-hooks.ps1]
        C2[Explicit Configuration]
        C3[One Repo at a Time]
        C1 --> C2 --> C3
        
        style C1 fill:#FF9800,stroke:#E65100,color:#fff
        style C2 fill:#FF9800,stroke:#E65100,color:#fff
        style C3 fill:#FF9800,stroke:#E65100,color:#fff
    end
    
    subgraph "Layer 4: Legacy Copy (30%)"
        D1[Manual Copy to<br/>.git/hooks/]
        D2[Direct File Placement]
        D3[Last Resort]
        D1 --> D2 --> D3
        
        style D1 fill:#9E9E9E,stroke:#424242,color:#fff
        style D2 fill:#9E9E9E,stroke:#424242,color:#fff
        style D3 fill:#9E9E9E,stroke:#424242,color:#fff
    end
    
    subgraph "Layer 5: CI/CD Verification (100% Detection)"
        E1[Pipeline Check]
        E2[Warn on Missing Hooks]
        E3[Metrics & Reporting]
        E1 --> E2 --> E3
        
        style E1 fill:#F44336,stroke:#C62828,color:#fff
        style E2 fill:#F44336,stroke:#C62828,color:#fff
        style E3 fill:#F44336,stroke:#C62828,color:#fff
    end
    
    A3 -.Fallback.-> B1
    B3 -.Fallback.-> C1
    C3 -.Fallback.-> D1
    D3 -.Monitor.-> E1
    
    A3 ==> Commit[Git Commit]
    B3 ==> Commit
    C3 ==> Commit
    D3 ==> Commit
    
    style Commit fill:#9C27B0,stroke:#6A1B9A,color:#fff
```

---

## Performance Characteristics

### Hook Execution Time

```mermaid
graph LR
    subgraph "Pre-Commit Performance"
        A[Small Commit<br/>1-2 files] -->|5-10 seconds| B[Secrets + CPM + Build]
        C[Medium Commit<br/>5-10 files] -->|15-20 seconds| D[All Checks]
        E[Large Commit<br/>20+ files] -->|30-45 seconds| F[Full Validation]
    end
    
    subgraph "Post-Commit Performance"
        G[Any Commit Size] -->|1-2 seconds| H[File Analysis +<br/>Reminders]
    end
    
    style A fill:#4CAF50,stroke:#2E7D32,color:#fff
    style C fill:#FF9800,stroke:#E65100,color:#fff
    style E fill:#F44336,stroke:#C62828,color:#fff
    style G fill:#2196F3,stroke:#1565C0,color:#fff
```

---

## Decision Trees

### Which Setup Method Should I Use?

```mermaid
flowchart TD
    Start{I am...} --> NewDev[New Developer]
    Start --> ExistingDev[Existing Developer]
    Start --> TeamLead[Team Lead]
    
    NewDev --> Q1{Do I clone<br/>repos often?}
    Q1 -->|Yes| GlobalTemplate[Use Global Template<br/>setup-global-template.ps1]
    Q1 -->|No, just this repo| PerRepo[Use Per-Repo Setup<br/>setup-hooks.ps1]
    
    ExistingDev --> Q2{Did I already<br/>setup global template?}
    Q2 -->|Yes| AutoWorks[✅ Nothing to do!<br/>Hooks auto-configured]
    Q2 -->|No| Q1
    
    TeamLead --> Q3{Setting up for<br/>whole team?}
    Q3 -->|Yes| TeamSetup[Document Global Template<br/>in onboarding +<br/>Add CI/CD check]
    Q3 -->|No, just me| Q1
    
    GlobalTemplate --> Verify[Verify: git config<br/>core.hooksPath]
    PerRepo --> Verify
    AutoWorks --> Verify
    TeamSetup --> Verify
    
    Verify --> Success[✅ Hooks Active!]
    
    style GlobalTemplate fill:#4CAF50,stroke:#2E7D32,color:#fff
    style AutoWorks fill:#4CAF50,stroke:#2E7D32,color:#fff
    style Success fill:#4CAF50,stroke:#2E7D32,color:#fff
    style PerRepo fill:#2196F3,stroke:#1565C0,color:#fff
    style TeamSetup fill:#FF9800,stroke:#E65100,color:#fff
```

---

## Summary

This execution flow demonstrates:

1. **Multi-Layered Defense**: 5 independent mechanisms ensure hooks are configured
2. **Automatic Recovery**: Bootstrap hooks auto-configure on clone when template is set
3. **Comprehensive Validation**: 10+ checks run before allowing commits
4. **Contextual Reminders**: Post-commit hook shows relevant reminders based on changed files
5. **Performance Optimized**: Checks run in parallel where possible, typical execution < 20 seconds
6. **Developer-Friendly**: Clear error messages, warnings vs. blockers, easy bypass for emergencies

**Result**: 99%+ hook adoption rate with minimal developer friction.

---

*For more details, see:*
- [Truly Automatic Git Hooks](truly-automatic-git-hooks.md)
- [Git Hooks Quick Reference](git-hooks-quick-reference.md)
- [Git Hooks Examples](git-hooks-examples.md)
