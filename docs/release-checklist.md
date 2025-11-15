# Release Checklist

Use this checklist before publishing a new JSONMorph release.

1. **Update Version** – Bump the `<Version>` element in `JSONMorph/JSONMorph.csproj` following semantic versioning.
2. **Write Release Notes** – Update `<PackageReleaseNotes>` and append to `CHANGELOG.md`.
3. **Verify Documentation** – Ensure README and docs reflect new APIs or breaking changes.
4. **Run Tests** – `dotnet test JSONMorph.sln -c Release`.
5. **Static Analysis** – Address warnings emitted by the .NET analyzers.
6. **Pack Locally** – `dotnet pack JSONMorph/JSONMorph.csproj -c Release` and inspect the `.nupkg`.
7. **Smoke Test** – Reference the package from a sample project to confirm restore and runtime behavior.
8. **Tag Release** – Create an annotated git tag: `git tag -a vX.Y.Z -m "JSONMorph vX.Y.Z"`.
9. **Publish Package** – `dotnet nuget push bin/Release/JSONMorph.X.Y.Z.nupkg --source https://api.nuget.org/v3/index.json`.
10. **Create GitHub Release** – Draft a release with highlights and attach the packaged artifacts if desired.


