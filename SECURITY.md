# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| Latest  | Yes       |
| < 1.0   | No        |

## Reporting a Vulnerability

Please **do not** file a public GitHub issue for security vulnerabilities.

Instead, report vulnerabilities privately via [GitHub Security Advisories](https://github.com/Neftedollar/multiagent-template/security/advisories/new).

You can expect an acknowledgement within 48 hours and a fix or mitigation plan within 7 days for confirmed issues.

## Scope

This project is a local developer tool (CLI and workspace templates). The primary attack surface is:

- **Template extraction**: path traversal via maliciously crafted embedded resources
- **Project name input**: path traversal via `../` in project names
- **Hook system**: command injection via user-controlled `lint.json`
- **Connection strings**: sensitive credentials passed via CLI flags

Known protections implemented as of v1.9:
- Project names containing `/`, `\`, or `..` are rejected
- Template output paths are validated to stay within the workspace root
- Connection strings are redacted before printing to stdout
