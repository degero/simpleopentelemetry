# Contributing

Contributions to SimpleOpenTelemetry are most welcome!


## Table of Contents

- [How Can I Contribute?](#how-can-i-contribute)
- [Pull Request Process](#pull-request-process)
- [Code of Conduct](#code-of-conduct)


## How can I contribute?

### Reporting bugs or requesting feature requests

Please open a [GitHub issue](https://github.com/degero/SimpleOpenTelemetry/issues) and select the appropriate template.


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


### Commit messages

This repo uses [release-please](https://github.com/googleapis/release-please) to automate versioning and the changelog, which relies on [Conventional Commits](https://www.conventionalcommits.org/). Please prefix commit messages accordingly:

| Prefix | Use for |
|---|---|
| `feat:` | a new feature |
| `fix:` | a bug fix |
| `docs:` | documentation only |
| `chore:` | tooling, CI, dependency bumps, etc. |
| `refactor:` | code change that neither fixes a bug nor adds a feature |
| `test:` | adding or correcting tests |

Add `!` after the type (e.g. `feat!:`) or a `BREAKING CHANGE:` footer for breaking changes.

Example: `fix: correct resource attribute mapping for cloud exporters`


## Pull request process

1. Push your branch and open a PR against `main`.
2. Fill in a short description of what changed and why.
3. Ensure CI (build, test) is green.
4. A maintainer will review and merge — release-please will pick up your commit automatically on the next release.


## Code of conduct

See [CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md)
