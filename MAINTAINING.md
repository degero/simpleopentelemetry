# Maintaining SimpleOpenTelemetry

Notes for maintainers on release and dependency workflows. This complements [CONTRIBUTING.md](./CONTRIBUTING.md), which covers the general contribution process for everyone.

## Versioning

This is handled via git tags of `vX.X.X` and MinVer setting assembly / nupkg version from this. Tags are created automatically by release-please based on the conventions noted in [CONTRIBUTING.md](CONTRIBUTING.md#pull-request-title)

## Release process

SimpleOpenTelemetry aims to release a new minor version inline for each OpenTelemetry minor version release as noted in [OpenTelemetry dependency updates](#opentelemetry-dependency-updates). This is not required to be explicitly 1:1 (eg. only a new minor for SimpleOpenTelemetry when OpenTelemetry does) as there may be other changes within SimpleOpenTelemetry.

Once 'main' has PRs merged. To initiate a new release:

1. Determine the new version number based on either the active release-please PR tag name or determine it from the [conventions](CONTRIBUTING.md#pull-request-title)
1. Tag a -rc.# of latest main for the new version eg `git tag v0.3.0-rc.1`
1. Push this tag `git push origin v0.3.0-rc.1`
1. Wait for the Github Actions run, MinVer reads the v1.3.0-rc.1 tag to stamp the package version as 1.3.0-rc.1.
1. Confirm everything for the version is correct and shows as prerelease on [nuget.org](https://www.nuget.org/packages/SimpleOpenTelemetry)
1. Notify maintainers to test the new rc
1. If there are any issues initiate fixes and follow the prior steps again with an incremented rc eg `v0.3.0-rc.2`
1. Find the open **release-please PR** (it auto-opens/updates on every push to `main` with a "release: x.y.z" title). If it's not open, check the `release-please` workflow ran on your latest merge to `main`.
1. Review the PR diff — confirm `CHANGELOG.md` and `.release-please-manifest.json` look right (correct version bump, all expected entries present).
1. **Merge the release-please PR**
1. On merge, release-please:
   - Bumps `.release-please-manifest.json` to the new version.
   - Creates the git tag eg `v0.3.0` (final, no `-rc` suffix) and a GitHub Release.
1. The pushed `v0.3.0` tag triggers `Release.yml`, which packs and pushes the real package to NuGet.org.
1. Check `nuget.org/packages/YourPackage/1.3.0` shows as the latest stable version.
1. Check the GitHub Release notes match `CHANGELOG.md`.

## OpenTelemetry dependency updates

Dependabot is configured (`.github/dependabot.yaml`) to raise a single, grouped PR each week covering every `OpenTelemetry*` and `Azure.Monitor.OpenTelemetry*` package pinned in `Directory.Packages.props`. This is deliberate: rather than shipping a release per individual package bump, updates accumulate into one PR so the library can ship a single, coordinated release against a known-compatible set of OpenTelemetry component versions.

That PR is opened with a `chore:` commit message, which **does not** trigger a release-please version bump on its own — it's a heads-up, not a release trigger.

The release workflow will update [docs/otel-component-versions.md](docs/otel-component-versions.md) using [scripts/generate-doco.sh](scripts/generate-doco.sh)

Checklist:

- Verify version changes are all in [Directory.Packages.props](Directory.Packages.props) as pinned versions and each projects package lock files have updated
- Ensure README's compatibility sections are updated [README.md](./README.md#compatibility) [src/SimpleOpenTelemetry/README.nuget.md#compatibility](src/SimpleOpenTelemetry/README.nuget.md#compatibility)
- Ensure README's dependencies versions are updated [README.md](./README.md#dependencies)
- Verify package lock files have updated. if not run `dotnet restore --force-evaluate`

NOTE: OpenTelemetry tends to release new versions of all distro packages with a a new release. To manually update use [scripts/update-otel-packages.sh](scripts/update-otel-packages.sh)

### When you're ready to cut a release from it

1. Review the PR — check the diff against `Directory.Packages.props` and skim linked release notes/changelogs for anything relevant (breaking changes, new semantic conventions, security fixes).

1. Check/update the [docs/configuration//examples/localdev/aspnetcore-csproj-snippet.xml](docs/configuration//examples/localdev/aspnetcore-csproj-snippet.xml) package versions that are used for the [Nuget quickstart](src/SimpleOpenTelemetry/README.nuget.md#quickstart)

1. Before merging, edit the merge commit message to replace the auto-generated `chore:` message with one that actually describes the bump for the changelog, e.g.:

   ```
   feat: bump OpenTelemetry packages to 1.16.x, Azure.Monitor exporters to 1.5.0/1.8.1
   ```

   Use `fix:` instead of `feat:` if nothing in the bundle is user-visible/new capability from this library's perspective — just bug fixes or maintenance from upstream.

1. Merge. release-please will pick up the commit and include it in its next release PR as normal.

If you want to skip a given week's bundle (e.g. nothing meaningfully changed, or you're waiting for a specific upstream fix to land first), just leave the PR open — Dependabot will update it in place on the next scheduled run rather than opening a duplicate, as long as it stays within the group.
