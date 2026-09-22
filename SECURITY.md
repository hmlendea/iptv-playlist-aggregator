# Security Policy

This security policy defines the scope of security vulnerabilities, supported versions, reporting procedures, and coordinated disclosure practices for IPTV Playlist Aggregator. It clarifies which issues are eligible for remediation and establishes clear expectations for responsible vulnerability reporting.

## 📑 Table of Contents

- [Table of Contents](#table-of-contents)
- [Supported Versions](#supported-versions)
- [Reporting a Vulnerability](#reporting-a-vulnerability)
- [Scope](#scope)
- [Disclosure Policy](#disclosure-policy)
- [Safe Harbour](#safe-harbour)
- [Recognition](#recognition)

## 🛡️ Supported Versions

Use this table to indicate which project versions currently receive security maintenance.

| Version | Distribution Channel | Supported |
|---------|----------------------|-----------|
| Latest version | GitHub Releases | ✅ |
| Preceding versions | Any distribution channel | ❌ |

## 🚨 Reporting a Vulnerability

Please do not disclose suspected vulnerabilities publicly before maintainers have had an opportunity to validate and remediate them.

To report a vulnerability:
- [GitHub Security Advisories](https://github.com/hmlendea/iptv-playlist-aggregator/security/advisories)
- Contact the maintainers directly

## 📌 Scope

The subsequent report categories are in scope for this repository:
- Network and data validation vulnerabilities in playlist fetching and parsing
- Configuration security and sensitive data handling
- Dependency vulnerabilities in NuGet packages and the .NET runtime
- Credential and authentication material exposure in logs or output files
- Cache security and unauthorised access to cached data
- Input validation vulnerabilities in channel and group matching
- Stream status checking and validation logic flaws

The subsequent categories are out of scope unless explicitly stated to the contrary:
- IPTV provider blocking, filtering, or legitimate content restrictions
- False positives in stream availability detection or status validation
- User responsibility for configuring insecure provider URLs
- Performance optimisation suggestions or feature requests
- Issues arising from running untrusted or modified application code

## 📢 Disclosure Policy

This project follows coordinated disclosure:
1. Vulnerabilities are investigated privately.
2. A remediation plan is prepared and validated.
3. Public disclosure is published after a fix, mitigation, or agreed risk decision is available.
4. Credit is attributed in accordance with reporter preference and project policy.

## 🧾 Safe Harbour

If your research is conducted in good faith, confined to authorised scope, and disclosed responsibly, the maintainers will not pursue action for policy-compliant activity.

## 🙏 Recognition

We appreciate responsible disclosure. Reporters who desire public attribution may be acknowledged in release notes, advisories, or a dedicated acknowledgements section.
