# SimpleOpenTelemetry — Copilot Agent Instructions

Trust these instructions first. Only search the repo yourself if something here is
missing or turns out to be wrong for the task at hand.

> **Note on branches:** this file is written against the full project layout (the
> content also present on the `add-nupkg-and-samples` branch at the time this file
> was generated). At the moment `main` contains only `README.md`, `LICENSE`, and a
> stray `testfile.md` — the source tree described below is not present there. If
> your working branch looks like the sparse `main` state, treat this file as the
> target layout to build toward / verify against once the full project is merged,
> and note the discrepancy rather than guessing.

## Repository summary

SimpleOpenTelemetry is a small, fluent **.NET library** (NuGet package `SimpleOpenTelemetry`)
that wraps OpenTelemetry setup behind an `IConfiguration`-driven builder, so consuming apps
add tracing/metrics/logging with minimal hand-written OTel code. It supports `net8.0` and
`net10.0` (and the README also mentions `.NET Standard 2.0` support). License: MIT.

Repo size: small-to-medium (~350 tracked files, dominated by vendored front-end assets —
bootstrap/jquery — under `example-apps/**/wwwroot/lib`). The actual library source is small
(~30 `.cs` files). Single git repo, no submodules.

- **Primary deliverable**: `src/SimpleOpenTelemetry` — the shipped NuGet package.
- **Tests**: `tests/SimpleOpenTelemetry.Tests` (unit) and
  `tests/SimpleOpenTelemetry.IntegrationTests` (integration), both xUnit-style .csproj test
  projects.
- **Example apps**: `example-apps/localdev`, `example-apps/cloud/{aws,azure,gcp}` — ASP.NET
  Core / console apps that demonstrate consuming the package for different hosts/exporters.
  These are demos, not part of the shipped package; don't let changes here block on unrelated
  breakage unless you were asked to touch them.
- **Docs**: `docs/` — configuration reference (exporters, samplers, propagators, instrumentations,
  resource detectors, distros) plus per-cloud example snippets under `docs/configuration/`.

There is already an `AGENTS.md` at the repo root with agent-focused conventions (preserve the
low-code/config-first design, update `src/SimpleOpenTelemetry/README.nuget.md` and `docs/` when
behavior changes, add tests for behavior changes). Read it — it is short and authoritative for
working conventions; this file focuses on the mechanics of building/testing/validating.

## Build, test, and validate

**Runtime/tooling required:** .NET SDK version pinned in `global.json` → `10.0.201`
(supports building/testing both the `net8.0` and `net10.0` targets). CI additionally installs
`8.0.x` and `10.0.x` via `actions/setup-dotnet`. Install the SDK before doing anything else if
it isn't already present — commands below fail immediately without it.

Always run commands from the repo root (where `SimpleOpenTelemetry.sln` lives), in this order:

1. **Restore** (always do this first, and after touching any `.csproj` or `Directory.Packages.props`):

   ```bash
   dotnet restore SimpleOpenTelemetry.sln --force-evaluate
   ```

   The repo uses NuGet **lock files** (`RestorePackagesWithLockFile=true` in
   `Directory.Build.props`, one `packages.lock.json` per project). If you add/change a package
   reference, restore normally (without `--locked-mode`) so the lock file updates, then commit
   the updated `packages.lock.json`. CI restores with `--force-evaluate` but does **not** force
   locked mode explicitly — `ContinuousIntegrationBuild`/`RestoreLockedMode` are auto-set true
   only when the `GITHUB_ACTIONS` env var is `true`. Locally, an out-of-date lock file will
   silently drift; if you change dependencies, regenerate the lock file explicitly:

   ```bash
   dotnet restore SimpleOpenTelemetry.sln --force-evaluate /p:RestorePackagesWithLockFile=true
   ```

2. **Build** (Release, matching CI):

   ```bash
   dotnet build SimpleOpenTelemetry.sln -c Release --no-incremental --no-restore
   ```

   `TreatWarningsAsErrors=true` and `EnforceCodeStyleInBuild=true` are set on the main library
   project (`src/SimpleOpenTelemetry/SimpleOpenTelemetry.csproj`), so **any compiler warning or
   code-style violation there fails the build**, not just errors. This is the single most common
   cause of CI failing on an otherwise-working change — check build output for warnings even if
   it "succeeds" locally without `-warnaserror` semantics.

3. **Test**:

   ```bash
   dotnet test SimpleOpenTelemetry.sln -c Release --no-build --no-restore --verbosity normal
   ```

   (CI additionally passes JUnit logger and coverage-collector flags for Codecov upload — safe
   to omit those locally.) The integration tests in
   `tests/SimpleOpenTelemetry.IntegrationTests` spin up an in-memory/test web host; if you see
   port-conflict failures when running integration tests concurrently with other processes,
   re-run them in isolation.

4. **Lint / format check** (CI runs this as a separate required job — always run before
   finishing a change that touches `src/SimpleOpenTelemetry`):
   ```bash
   dotnet format src/SimpleOpenTelemetry/SimpleOpenTelemetry.csproj --verify-no-changes --verbosity diagnostic
   ```
   Drop `--verify-no-changes` to have it auto-fix formatting instead of just checking.
   A `prettier` job for `**/*.{json,yaml,yml,md}` exists in `.github/workflows/lint.yml` but is
   currently **commented out / disabled** (`TODO enable prettier`) — don't rely on it running in
   CI, but the repo's `.prettierrc`/`.prettierignore` still reflect the intended formatting for
   JSON/YAML/Markdown, and the PR checklist still asks contributors to run
   `npx prettier --check "**/*.{json,yaml,yml,md}"` by hand.

No separate "run" step exists for the library itself (it's a package, not an app). To exercise
it manually, run one of the example apps, e.g.:

```bash
dotnet run --project example-apps/localdev/aspnetcore/AspNetCore.csproj
```

**Environment note:** this sandbox has no outbound access to `nuget.org`/`api.nuget.org`, so the
restore/build/test commands above could not be executed here to pre-validate them; they are
transcribed directly from `.github/workflows/build-and-test.yml`, `.github/workflows/lint.yml`,
and `CONTRIBUTING.md`, which are the authoritative source if behavior differs.

## CI / required checks (must pass before a PR is mergeable)

- `.github/workflows/ci.yml` → calls `build-and-test.yml`: restore → build (Release) → test, on
  every push/PR to `main`.
- `.github/workflows/lint.yml` → `dotnet format ... --verify-no-changes` on
  `src/SimpleOpenTelemetry/SimpleOpenTelemetry.csproj`.
- `.github/workflows/generate-doco.yml` → auto-regenerates `docs/otel-component-versions.md`
  from `Directory.Packages.props` via `scripts/generate-doco.sh` when that file or the script
  changes on `main`. If you change package versions in `Directory.Packages.props`, you can run
  `scripts/generate-doco.sh` locally and commit the resulting diff to `docs/otel-component-versions.md`
  yourself instead of relying on the bot.
- `.github/workflows/release.yml` / `release-please.yml` — release automation (release-please +
  conventional commits); not relevant to normal feature/fix PRs, no action needed.
- Dependabot is configured (`.github/dependabot.yml`) with explicit PR grouping for the shipped
  package vs. everything else — don't manually bump versions in `Directory.Packages.props` unless
  the task specifically asks for a dependency update.

Replicate CI locally before considering a change done: restore → build (Release, no warnings) →
test → `dotnet format --verify-no-changes`.

## Project layout / where to make changes

```
src/SimpleOpenTelemetry/               # the shipped package (this is what ships to NuGet)
  SimpleOpenTelemetryBootstrap.cs      # standalone entry point (non-generic-host apps)
  SimpleOpenTelemetryOptions.cs        # configuration model (IConfiguration binding target)
  Builder/                             # ISimpleOpenTelemetryBuilder / SimpleOpenTelemetryBuilder
                                        #   — core config processing + component wiring
  Extensions/                          # HostApplicationBuilderExtensions (generic-host entry
                                        #   point), ServiceCollectionExtensions,
                                        #   ServiceProviderExtensions, ResourceBuilderExtensions
  OtelComponents/                      # pluggable component loaders, one subfolder per kind:
    Distro/ Exporter/ Extension/ Instrumentation/ Propagator/ Resource/ Sampler/
                                        #   each has an *Enum (supported values), *Assemblies
                                        #   (reflection-loaded assembly/type names), and a
                                        #   *Loader implementing an I*Loader interface —
                                        #   this is the pattern to follow when adding support
                                        #   for a new exporter/instrumentation/etc.
  Reflection/                          # AssemblyExecution — reflection-based plugin loading
  Validation/                          # SimpleOpenTelemetryValidator
  Diagnostics/                         # SimpleOpenTelemetryEventSource (EventSource for internal
                                        #   diagnostics/logging)
  README.nuget.md                      # README packaged into the NuGet — update if usage changes

tests/SimpleOpenTelemetry.Tests/            # unit tests, mirrors src/ folder structure
tests/SimpleOpenTelemetry.IntegrationTests/ # integration tests (generic host, web app scenarios)

example-apps/
  localdev/                            # aspnetcore + console apps + shared code, local OTel
                                        #   collector stack (docker-compose) for manual testing
  cloud/{aws,azure,gcp}/                # per-cloud example deployments (ECS, App Service,
                                        #   Cloud Run) incl. IaC (Terraform) and collector config

docs/                                   # configuration reference consumed by end users of the
                                        #   package — keep in sync with OtelComponents/ changes
scripts/generate-doco.sh                # regenerates docs/otel-component-versions.md

SimpleOpenTelemetry.sln                 # solution file — build/test entrypoint, references all
                                        #   of the above projects
Directory.Build.props                   # shared MSBuild props (lock files, CI build flag)
Directory.Packages.props                # central package version management (CPM) — all package
                                        #   versions are pinned here, not per-csproj
global.json                             # pins .NET SDK version (10.0.201)
AGENTS.md                               # existing agent working-conventions doc — read this too
CONTRIBUTING.md                         # contributor workflow, conventional-commit prefixes
MAINTAINING.md                          # maintainer-only release/dependency process
.editorconfig                           # C# formatting rules enforced by `dotnet format`
.cspell.config.yaml                     # spell-check config (en-GB)
```

## Conventions worth knowing before editing

- **Central Package Management**: never add a `Version` attribute to a `PackageReference` in a
  `.csproj`; add/update the version in `Directory.Packages.props` instead.
- **Reflection-loaded plugin packages**: packages marked `IsReflectionPlugin="true"` in
  `Directory.Packages.props` (exporters, instrumentations, distros, etc.) are _not_ referenced
  directly by `src/SimpleOpenTelemetry` — they're loaded by name via reflection at runtime
  (`OtelComponents/*/​*Loader.cs`) so consumers only pull in what they configure. Tests reference
  them directly to validate loading; the shipped package does not.
  Only `OpenTelemetry`, `OpenTelemetry.Extensions.Hosting`, and
  `OpenTelemetry.Exporter.OpenTelemetryProtocol` are true runtime dependencies of the shipped
  package.
- **Commit messages**: Conventional Commits, enforced by convention (release-please reads them):
  `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`, `test:`, with `!` or a `BREAKING CHANGE:`
  footer for breaking changes.
- **Warnings are errors** in `src/SimpleOpenTelemetry` — don't introduce nullable warnings,
  analyzer warnings, or style violations there.
- **Public API**: public members are expected to have XML doc comments (see PR checklist in
  `.github/pull_request_template.md`); `GenerateDocumentationFile=true` is set, so missing docs
  on public members will surface as build warnings (and thus build failures, per above).
- When you change configuration shape, supported component values (the `*Enum` files), or
  bootstrap behavior, update: the matching `docs/configuration/*.md` file,
  `src/SimpleOpenTelemetry/README.nuget.md` if usage guidance changed, and add/update tests in
  both test projects.
