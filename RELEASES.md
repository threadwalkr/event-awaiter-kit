# Repository versions and releases

`EventAwaiterKit/EventAwaiterKit.csproj` contains the library `Version`. The explicit starting value, `1.0.0`, preserves the SDK's previous default version; it is not a claim that a public package exists. Keep assembly/build identity independent of whether the library is distributed as a package.

Use patch increments for compatible fixes, minor increments for compatible additions, and major increments for intentional breaking changes. Record behavior changes and compatibility implications in release notes. The current lifecycle correction and typed APIs must be described when choosing the next repository release version.

Before a repository release:

1. Run all checks in [CONTRIBUTING.md](CONTRIBUTING.md), including Windows context checks.
2. Review [LIFECYCLE.md](LIFECYCLE.md) and examples against the implementation.
3. Select the version deliberately and review that version change.
4. Tag the accepted commit as `vX.Y.Z` and describe changes and tested environments in repository release notes.

Consumers reference the library project or build its assembly. Package generation is disabled. There is no package-publishing workflow or package-registry credential requirement for repository releases.
