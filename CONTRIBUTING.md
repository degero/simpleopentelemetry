# Contributing

Contributions to SimpleOpenTelemetry are most welcome!

For maintainer-specific processes (releases, dependency bundling), see [MAINTAINING.md](./MAINTAINING.md).

## Table of Contents

- [How Can I Contribute?](#how-can-i-contribute)
- [Pull Request Process](#pull-request-process)
- [Code of Conduct](#code-of-conduct)

## How can I contribute?

### Reporting bugs or requesting feature requests

Please open a [GitHub issue](https://github.com/degero/simpleopentelemetry/issues) and select the appropriate template.

### Getting started

1. Fork the repo and clone your fork.
2. Install the [.NET SDK](https://dotnet.microsoft.com/download) (see `global.json` / the `TargetFrameworks` in the `.csproj` for supported versions).
3. Restore and build:

   ```bash
   dotnet restore
   dotnet build
   ```

4. Run the tests:

   ```bash
   dotnet test
   ```

### Making changes

- Create a branch off `main` for your change eg: `git checkout -b [feat/fix]/short-description`.
- Keep changes focused — smaller, single-purpose pull requests are easier to review and merge.
- See the [pull_request_template.md](./.github/pull_request_template.md) for a checklist of required steps to be done before submitting for approval.

### Pull request title

This repo uses [release-please](https://github.com/googleapis/release-please) to automate versioning and the changelog, which relies on [Conventional Commits](https://www.conventionalcommits.org/). As squash commits to main are enforced, which will drop a commit message using the conventions, please prefix the PR title with these instead:

| Prefix      | Use for                                                 | Version bump                |
| ----------- | ------------------------------------------------------- | --------------------------- |
| `feat:`     | a new feature                                           | minor (`0.X.0`)             |
| `fix:`      | a bug fix                                               | patch (`0.0.X`)             |
| `perf:`     | a performance improvement                               | patch (`0.0.X`)             |
| `docs:`     | documentation only                                      | none — changelog entry only |
| `chore:`    | tooling, CI, dependency bumps, etc.                     | none — changelog entry only |
| `refactor:` | code change that neither fixes a bug nor adds a feature | none — changelog entry only |
| `test:`     | adding or correcting tests                              | none — changelog entry only |
| `style:`    | formatting/whitespace only, no code meaning change      | none — changelog entry only |
| `build:`    | changes to the build system or package dependencies     | none — changelog entry only |
| `ci:`       | changes to CI configuration/scripts                     | none — changelog entry only |
| `revert:`   | reverts a previous commit                               | none — changelog entry only |

Example: `fix: correct resource attribute mapping for cloud exporters`

### Breaking Changes

Add `!` after the type (e.g. `feat!:`) in the PR title and a `BREAKING CHANGE:` footer of the PR description as its own paragraph. Ensure there is sufficient details in the BREAKING CHANGE message as this will be picked up by release-please for CHANGELOG.md

## Pull request process

1. Push your branch and open a PR against `main`.
1. Set the PR title as mentioned in [Pull request title](#pull-request-title)
1. Fill in a short description of what changed and why.
1. Ensure CI (build, test) is green.
1. A maintainer will review/approve
1. Merge the PR. If this is a breaking change, add a `BREAKING CHANGE: <description>` line to the squash-merge commit message box before confirming the merge (the title's `!` alone won't carry the migration detail into the changelog)
1. release-please will pick up your commit automatically on the next release and add an entry of the commit message to the CHANGELOG.md

## Code of conduct

See [CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md)
