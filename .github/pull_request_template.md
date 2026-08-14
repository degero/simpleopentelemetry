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

- [ ] A commit message that follows [the CONTRIBUTING.md](../CONTRIBUTING.md#commit-messages) guideliness
- [ ] Tests added or updated for the change
- [ ] Public members have XML doc comments
- [ ] Run `dotnet format src/SimpleOpenTelemetry/SimpleOpenTelemetry.csproj --verbosity diagnostic`
- [ ] Run `npx prettier --check "**/*.{json,yaml,yml,md}"`
- [ ] `dotnet build` passes with no warnings
- [ ] `dotnet test` passes
- [ ] Docs (`README.md` / `docs/`) updated if behaviour or configuration changed

By submitting this issue, you agree to follow our [Code of Conduct](https://github.com/degero/simpleopentelemetry/blob/main/CODE_OF_CONDUCT.md).
