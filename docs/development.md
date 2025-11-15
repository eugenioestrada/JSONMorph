# Development Guide

This document summarises the local development workflow, tooling, and project conventions for JSONMorph.

## Toolchain
- Install the .NET 10 SDK (`dotnet --list-sdks` should include `10.0.x`).
- Install PowerShell 7 if you want cross-platform scripts.
- Enable nullable reference types and implicit usings (already configured).

## Building

```pwsh
dotnet build JSONMorph.sln
```

Use `-c Release` when preparing builds for publication.

## Testing

```pwsh
dotnet test JSONMorph.sln
```

Consider running tests under multiple cultures when touching serialization logic:

```pwsh
$env:DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = "false"
$env:DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY = "false"
dotnet test JSONMorph.sln
```

## Coding Standards
- Keep public APIs documented with XML doc comments.
- Favor `Span<T>`/`ReadOnlySpan<T>` operations when manipulating JSON Pointer segments.
- Throw `InvalidOperationException` for invalid patch semantics, `ArgumentException` for malformed JSON.
- Keep classes internal unless they are part of the public surface.
- Add targeted unit tests whenever adding new behaviors.

## Branching
- Use feature branches prefixed with `feature/` for major work, `fix/` for bug fixes, and `docs/` for documentation updates.
- Keep commits small and descriptive. Reference issues in commit messages when applicable.

## Pull Requests
- Link to the issue being addressed.
- Include screenshots when the change affects docs or snippets.
- Ensure all GitHub Actions workflows pass before requesting review.

## Packaging
- Update `JSONMorph.csproj` version and release notes.
- Run `dotnet pack -c Release JSONMorph/JSONMorph.csproj`.
- Inspect the generated `.nupkg` with a tool like `nuget.exe` or `dotnet nuget`.
- Publish the package using `dotnet nuget push` to the configured feed.


