# Contributing to Social Media Commander

Thank you for your interest in contributing to Social Media Commander! This guide will help you understand our development process and CI/CD pipeline.

## Development Workflow

### Prerequisites

- .NET 10 SDK
- Node.js 22.x (for frontend components)
- Git
- Visual Studio 2022 or VS Code

### Setting Up Development Environment

1. **Clone the repository:**
   ```bash
   git clone https://github.com/snickler/social-media-command.git
   cd social-media-command
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore SocialMediaCommander.sln
   npm install
   ```

3. **Build and test:**
   ```bash
   dotnet build SocialMediaCommander.sln
   dotnet test SocialMediaCommander.Tests
   ```

4. **Run the application:**
   ```bash
   dotnet run --project SocialMediaCommander.Desktop
   ```

## Contribution Process

### 1. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/issue-description
```

### 2. Make Your Changes

- Follow existing code style and patterns
- Add tests for new functionality
- Update documentation as needed
- Ensure all tests pass locally

### 3. Commit Your Changes

We use [Conventional Commits](https://www.conventionalcommits.org/) for automated versioning:

```bash
# For new features
git commit -m "feat: add OAuth2 integration for Twitter"

# For bug fixes  
git commit -m "fix: resolve null reference in authentication service"

# For documentation
git commit -m "docs: update API documentation"

# For breaking changes
git commit -m "feat: redesign authentication API

BREAKING CHANGE: AuthService.Login() now returns Task<AuthResult> instead of bool"
```

**Commit Types:**
- `feat:` - New features (triggers minor version bump)
- `fix:` - Bug fixes (triggers patch version bump)  
- `docs:` - Documentation changes
- `style:` - Code style changes (no functionality change)
- `refactor:` - Code refactoring
- `test:` - Adding or modifying tests
- `chore:` - Maintenance tasks
- `BREAKING CHANGE:` - Breaking changes (triggers major version bump)

### 4. Push and Create Pull Request

```bash
git push origin feature/your-feature-name
```

Then create a pull request on GitHub.

## CI/CD Pipeline

Our automated pipeline will run when you create a PR:

### ✅ Automated Checks

1. **Code Quality:**
   - Code formatting verification
   - Static analysis
   - Security scanning

2. **Testing:**
   - Unit tests (184+ tests)
   - Integration tests
   - Code coverage analysis

3. **Builds:**
   - Multi-platform builds (Windows, Linux, macOS)
   - Multi-architecture (x64, arm64)

4. **Security:**
   - Dependency vulnerability scanning
   - CodeQL security analysis

### 📋 PR Requirements

Your PR must:
- ✅ Pass all automated tests
- ✅ Meet code coverage requirements (60%+ minimum)
- ✅ Pass security scans
- ✅ Follow code formatting standards
- ✅ Include appropriate tests for new features
- ✅ Update documentation if needed

### 🔄 Review Process

1. Automated checks must pass
2. Code review by maintainers
3. Approval and merge to develop/main
4. Automated release process (if merged to main)

## Code Style Guidelines

### .NET Code

- Follow Microsoft C# coding conventions
- Use `dotnet format` for automatic formatting
- Prefer explicit types over `var` for complex types
- Use meaningful variable and method names
- Add XML documentation for public APIs

### Project Structure

```
SocialMediaCommander.Core/       # Domain models and core logic
SocialMediaCommander.Services/   # Business services and implementations  
SocialMediaCommander.Desktop/    # Avalonia UI application
SocialMediaCommander.Tests/      # Unit and integration tests
```

### Testing

- Write unit tests for new functionality
- Aim for 80%+ code coverage on new code
- Use descriptive test method names
- Follow AAA pattern (Arrange, Act, Assert)

Example:
```csharp
[Fact]
public async Task AuthenticateAsync_WithValidCredentials_ReturnsSuccessResult()
{
    // Arrange
    var service = new AuthenticationService();
    var credentials = new UserCredentials("user", "pass");
    
    // Act
    var result = await service.AuthenticateAsync(credentials);
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Token);
}
```

## Release Process

### Automated Releases

Releases are automatically created when:
- Changes are merged to `main` branch
- Semantic versioning determines version bump based on commits
- Build artifacts are generated for all platforms
- Release notes are auto-generated

### Manual Releases

Maintainers can trigger manual releases:
1. Go to Actions → Release workflow
2. Select release type (patch/minor/major/prerelease)
3. Run workflow

## Getting Help

### Documentation

- [CI/CD Documentation](.github/CI_CD_DOCUMENTATION.md)
- [Security Implementation](docs/security/secure-storage-implementation.md)
- [Performance Optimizations](PERFORMANCE_OPTIMIZATIONS.md)

### Communication

- Open an issue for bug reports or feature requests
- Start a discussion for questions or ideas
- Join our community discussions

### Development Tips

1. **Local Testing:**
   ```bash
   # Run specific tests
   dotnet test SocialMediaCommander.Tests --filter "Category=Unit"
   
   # Build for specific platform
   dotnet publish -r win-x64 --self-contained
   ```

2. **Debugging:**
   - Use the provided launch configurations
   - Check logs in `%LOCALAPPDATA%\SocialMediaCommander\Logs`
   - Enable verbose logging in development

3. **Performance:**
   - Profile memory usage for large operations
   - Use async/await properly
   - Follow performance guidelines in documentation

## Code of Conduct

- Be respectful and inclusive
- Focus on constructive feedback
- Help others learn and grow
- Follow the Golden Rule

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.

---

Thank you for contributing to Social Media Commander! 🚀