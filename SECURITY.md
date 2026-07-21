# Security Policy

## Reporting a vulnerability

Do not open a public issue for an undisclosed vulnerability. Use GitHub's private vulnerability reporting feature for this repository. Include the affected version, reproduction steps, impact, and any suggested mitigation. Please avoid including real credentials, workplace infrastructure, personal network details, or sensitive message content.

Maintainers will acknowledge a complete report as soon as practical, investigate it, and coordinate disclosure after a fix or mitigation is available. No response-time guarantee is offered.

## Supported versions

Before the first public release, only the latest commit on the default branch is eligible for security fixes. A release support table will be added when versioned packages exist.

## Security boundaries

Unskip is local-first and does not use a central service. That design does not make every reachable Windows host trusted. Future delivery code must validate destinations, invoke `msg.exe` without a shell, constrain execution time, sanitize diagnostics, and avoid retaining full message bodies by default.
