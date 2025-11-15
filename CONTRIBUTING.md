# Contributing

Thank you for your interest in JSONMorph! Contributions of all sizes are welcome. This guide explains how to get started.

## Ways to Contribute
- Report bugs or request features through GitHub issues.
- Improve documentation in the README or `docs/`.
- Add unit tests that cover missing scenarios.
- Submit pull requests that fix bugs or introduce new functionality.

## Ground Rules
- Follow the [Code of Conduct](CODE_OF_CONDUCT.md).
- Keep discussions respectful and assume positive intent.
- Prefer small, focused pull requests that are easy to review.

## Development Workflow
1. Fork and clone the repository.
2. Create a descriptive branch name (for example, `feature/json-pointer-validation`).
3. Restore dependencies and build the solution:
   ```pwsh
   dotnet restore
   dotnet build JSONMorph.sln
   ```
4. Run the test suite:
   ```pwsh
   dotnet test JSONMorph.sln
   ```
5. Commit your changes with meaningful messages.
6. Push the branch and open a pull request.

## Coding Guidelines
- Keep the public API documented with XML comments.
- Favor expressive variable names over comments.
- Maintain consistent formatting (the project relies on the default .NET formatter).
- Add or update tests whenever behavior changes.

## Pull Request Checklist
- [ ] Tests pass locally.
- [ ] Added or updated documentation.
- [ ] Included changes in `CHANGELOG.md` when user-facing.
- [ ] Linked the relevant issue in the PR description.

## Need Help?
If you run into a problem, open a draft PR or start a discussion thread. Maintainers are happy to help.


