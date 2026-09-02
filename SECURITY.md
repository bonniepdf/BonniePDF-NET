# Security Policy

BonniePDF for .NET (`BonniePDF`) is maintained by CodeFocus Limited. We welcome good-faith reports that
help protect BonniePDF users.

## Reporting a vulnerability

**Please do not open a public issue for a suspected vulnerability.**

Email **security@bonniepdf.dev** with:

- the affected product, package, version, and runtime;
- a clear description of the issue and its potential impact;
- steps or a minimal example that reproduce it;
- relevant logs or diagnostics, with secrets and personal data removed; and
- a safe way to contact you about coordination.

Do not send private keys, production credentials, customer documents,
payment-card data, or unnecessary personal data. If the report itself is
sensitive, ask for an encrypted channel before sending the sensitive material.

The full policy, including what to expect and the scope of good-faith research,
is published at <https://www.bonniepdf.dev/legal/security>. Machine-readable
contact details are at <https://www.bonniepdf.dev/.well-known/security.txt>.

## What to expect

We assess reproducible reports and prioritise them by impact and
exploitability, then coordinate remediation and disclosure where appropriate.
Response and remediation times depend on severity, complexity, affected
versions, and whether third parties are involved. This policy does not create a
service-level commitment or a bug-bounty programme.

## Scope

In scope: the current BonniePDF packages and the `bonniepdf.dev` website.

Out of scope: third-party services, including GitHub, npm, NuGet, Paddle,
Cloudflare, certificate authorities, and timestamp authorities. Report those
under their own policies.

BonniePDF generates PDFs locally and adds no telemetry, activation, or
licence-checking network calls to its published packages. Applications that
integrate BonniePDF remain responsible for their own input handling,
authentication, authorization, storage, delivery, and operational security.
