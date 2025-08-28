# Contributing to CsharpDialog

Thank you for your interest in contributing to CsharpDialog! This document provides guidelines and information for contributors.

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio Code with C# extension
- Git
- Windows 10/11

### Development Setup

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/csharpdialog.git
   cd csharpdialog
   ```
3. Add the upstream remote:
   ```bash
   git remote add upstream https://github.com/windowsadmins/csharpdialog.git
   ```
4. Restore packages:
   ```bash
   dotnet restore
   ```
5. Build the project:
   ```bash
   dotnet build
   ```

## Development Guidelines

### Code Style

- Follow standard C# coding conventions
- Use meaningful variable and method names
- Include XML documentation for public APIs
- Keep methods focused and reasonably sized
- Use async/await for I/O operations

### Project Structure

- **CsharpDialog.Core**: Shared models, interfaces, and business logic
- **CsharpDialog.CLI**: Command-line interface and argument parsing
- **CsharpDialog.WPF**: WPF-specific UI components and dialogs

### Branch Naming

Use descriptive branch names:
- `feature/add-markdown-support`
- `bugfix/fix-timeout-issue`
- `docs/update-readme`

### Commit Messages

Write clear, concise commit messages:
- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Move cursor to..." not "Moves cursor to...")
- Reference issues and pull requests when applicable

## Making Changes

### Before You Start

1. Check existing issues and pull requests
2. Create an issue to discuss significant changes
3. Ensure you understand the scope and requirements

### Development Process

1. Create a feature branch:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. Make your changes:
   - Write code following the style guidelines
   - Add tests for new functionality
   - Update documentation as needed

3. Test your changes:
   ```bash
   dotnet build
   dotnet test
   ```

4. Commit your changes:
   ```bash
   git add .
   git commit -m "Add your descriptive commit message"
   ```

5. Push to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

6. Create a pull request on GitHub

### Pull Request Guidelines

- Provide a clear description of the changes
- Reference any related issues
- Include screenshots for UI changes
- Ensure all tests pass
- Update documentation if needed

## Types of Contributions

### Bug Reports

When filing a bug report, please include:
- Clear description of the issue
- Steps to reproduce
- Expected vs actual behavior
- Environment details (Windows version, .NET version)
- Screenshots if applicable

### Feature Requests

For feature requests, please provide:
- Clear description of the desired functionality
- Use case or motivation
- Possible implementation approach
- Compatibility with swiftDialog (if applicable)

### Documentation

Documentation improvements are always welcome:
- Fix typos or unclear explanations
- Add examples or use cases
- Improve code comments
- Create tutorials or guides

### Code Contributions

We welcome:
- Bug fixes
- New features
- Performance improvements
- Code refactoring
- Test coverage improvements

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/CsharpDialog.Tests/
```

### Writing Tests

- Write unit tests for new functionality
- Use descriptive test method names
- Follow AAA pattern (Arrange, Act, Assert)
- Mock external dependencies

## Compatibility

### swiftDialog Compatibility

When adding features, consider compatibility with swiftDialog:
- Use similar command-line argument names
- Maintain consistent behavior where possible
- Document any differences in behavior

### Windows Compatibility

- Ensure features work across supported Windows versions
- Consider different display configurations
- Test with various color themes and accessibility settings

## Release Process

1. Version numbers follow semantic versioning (MAJOR.MINOR.PATCH)
2. Update version in all project files
3. Update CHANGELOG.md
4. Create release notes
5. Tag the release

## Getting Help

If you need help:
- Check existing documentation
- Review similar issues or pull requests
- Ask questions in issue comments
- Reach out to maintainers

## Code of Conduct

Please be respectful and constructive in all interactions. We aim to create a welcoming environment for all contributors.

## Recognition

Contributors will be acknowledged in:
- Release notes
- README.md contributors section
- Git commit history

Thank you for contributing to CsharpDialog!
