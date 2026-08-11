# Maintaining SimpleOpenTelemetry

Notes for maintainers on release and dependency workflows. This complements [CONTRIBUTING.md](./CONTRIBUTING.md), which covers the general contribution process for everyone.

## OpenTelemetry dependency updates

Dependabot is configured (`.github/dependabot.yaml`) to raise a single, grouped PR each week covering every `OpenTelemetry*` and `Azure.Monitor.OpenTelemetry*` package pinned in `Directory.Packages.props`. This is deliberate: rather than shipping a release per individual package bump, updates accumulate into one PR so the library can ship a single, coordinated release against a known-compatible set of OpenTelemetry component versions.

That PR is opened with a `chore:` commit message, which **does not** trigger a release-please version bump on its own — it's a heads-up, not a release trigger.

### When you're ready to cut a release from it

1. Review the PR — check the diff against `Directory.Packages.props` and skim linked release notes/changelogs for anything relevant (breaking changes, new semantic conventions, security fixes).
2. Before merging, edit the merge commit message (or add a commit to the branch, if not squash-merging) to replace the auto-generated `chore:` message with one that actually describes the bump for the changelog, e.g.:

   ```
   feat: bump OpenTelemetry packages to 1.16.x, Azure.Monitor exporters to 1.5.0/1.8.1
   ```

   Use `fix:` instead of `feat:` if nothing in the bundle is user-visible/new capability from this library's perspective — just bug fixes or maintenance from upstream.

3. Merge. release-please will pick up the commit and include it in its next release PR as normal.

If you want to skip a given week's bundle (e.g. nothing meaningfully changed, or you're waiting for a specific upstream fix to land first), just leave the PR open — Dependabot will update it in place on the next scheduled run rather than opening a duplicate, as long as it stays within the group.
