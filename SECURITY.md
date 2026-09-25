# Security Policy

## Supported Versions

SimpleOpenTelemetry is currently in Beta. Security fixes are made against the
latest published NuGet release. Older pre-1.0 versions are not separately
patched — please upgrade to the latest version if you receive a fix.

| Version        | Supported          |
| -------------- | ------------------ |
| Latest release | :white_check_mark: |
| Older releases | :x:                |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues,
discussions, or pull requests.**

Instead, please report them privately using GitHub's Security Advisories:

1. Go to the [Security tab](https://github.com/degero/simpleopentelemetry/security) of this repository.
2. Click **"Report a vulnerability"** under Advisories.
3. Provide as much detail as you can: affected version(s), a description of
   the issue, steps to reproduce, and potential impact.

This creates a private discussion between you and the maintainer, and lets us
coordinate a fix and a disclosure timeline before any details are made public.

Alternatively, you can email the maintainer directly — see the GitHub profile
at [@degero](https://github.com/degero) for contact details.

## What to Expect

- **Acknowledgement:** within a few days of your report.
- **Triage:** we'll confirm the issue, assess severity, and discuss a fix
  timeline with you.
- **Fix & disclosure:** once a fix is released, we'll publish a GitHub
  Security Advisory crediting you (unless you prefer to stay anonymous), and
  reference the advisory in the [CHANGELOG](./CHANGELOG.md).

## Scope

This policy covers the SimpleOpenTelemetry library source code
(`src/SimpleOpenTelemetry`) and its published NuGet package. Vulnerabilities
in third-party dependencies (e.g. the OpenTelemetry SDK itself) should be
reported to their respective maintainers, though we're happy to be notified
too so we can track/mitigate on our side (e.g. via a pinned version bump).

Issues in the example apps (`example-apps/`) or documentation are welcome as
regular bug reports rather than security advisories, unless they demonstrate
an exploitable issue in the library itself.
