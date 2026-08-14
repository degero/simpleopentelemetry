## Description

<!-- What does this PR change, and why? -->

## Related issue

<!-- Closes #123, or "N/A" -->

## Type of change

- [ ] `feat` — new feature
- [ ] `fix` — bug fix
- [ ] `docs` — documentation only (includes snippets/example configs in /docs)
- [ ] `refactor` — no functional change
- [ ] `test` — test-only change
- [ ] `chore` — tooling / CI / dependencies
- [ ] Breaking change (requires a `!` in the commit type or a `BREAKING CHANGE:` footer)

## Additional context

<!-- Anything else reviewers should know: design decisions, trade-offs, screenshots, etc. -->

## Checklist

- [ ] The PR title follows [the CONTRIBUTING.md](../CONTRIBUTING.md#pull-request-title) conventional commit format (e.g. prefix with the above type of change `feat: add OTLP exporter config`)
- [ ] If you changed a package version, run `dotnet restore --force-evaluate` and commit the updated `packages.lock.json` alongside your `.csproj` change
- [ ] Tests added or updated for the change
- [ ] Public members have XML doc comments
- [ ] Run `dotnet format src/SimpleOpenTelemetry/SimpleOpenTelemetry.csproj --verbosity diagnostic`
- [ ] Run `npx prettier --check "**/*.{json,yaml,yml,md}"`
- [ ] `dotnet build` passes with no warnings
- [ ] `dotnet test` passes
- [ ] Docs (`README.md` / `docs/`) updated if behaviour or configuration changed
- [ ] If this is a breaking change, add a `BREAKING CHANGE: <description>` line to the squash-merge commit message box before confirming the merge (the title's `!` alone won't carry the migration detail into the changelog)

## Merging the PR

Ensure the merge commit message has the PR title and if it is a BREAKING CHANGE that it has that footer message included

---

By submitting this issue, you agree to follow our [Code of Conduct](https://github.com/degero/simpleopentelemetry/blob/main/CODE_OF_CONDUCT.md).
