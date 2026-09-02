# Contributing to BonniePDF for .NET

Thanks for your interest. This file covers two things: the legal basis on which
contributions are accepted, and the practical steps for getting a change in.

## Before you open a pull request

Open an issue first for anything beyond a typo or an obvious bug fix. BonniePDF
is a specification-driven project, and a change that looks small locally can
have consequences for output conformance or for the other language
implementation. Agreeing the approach in an issue saves rework.

## Licensing of contributions

This repository is published under the **BonniePDF Community License 1.0** (see
[LICENSE](LICENSE)). It is a source-available licence, not an open-source
licence: the same code is also distributed to customers under the BonniePDF
Commercial License Agreement.

By submitting a contribution you confirm that:

1. You wrote the contribution yourself, or you have the right to submit it under
   the terms below — including any clearance you need from an employer or client.
2. You grant CodeFocus Limited a perpetual, worldwide, non-exclusive,
   royalty-free, irrevocable licence to use, reproduce, modify, prepare
   derivative works of, publicly display, sublicense, and distribute your
   contribution and such derivative works, **including as part of commercially
   licensed distributions of BonniePDF under different licence terms**.
3. You understand that you retain copyright in your contribution, and that this
   grant does not transfer ownership of it.
4. Your contribution is provided without warranty of any kind.

This is what allows a contribution to ship in both the source-available and the
paid distributions of BonniePDF. Without it, an accepted change could not be
included in the commercial product.

## Signing off your commits

We use the [Developer Certificate of Origin](https://developercertificate.org/)
rather than a separate contributor licence agreement. Add a sign-off line to
each commit:

```
Signed-off-by: Your Name <your.email@example.com>
```

`git commit -s` adds it for you. Use your real name and an email address you
can be reached at. A pull request whose commits are not signed off cannot be
merged.

## Security issues

Do not open a public issue or pull request for a suspected vulnerability. See
[SECURITY.md](SECURITY.md).

## Building and testing

```bash
dotnet build BonniePDF.slnx
dotnet test BonniePDF.Tests/BonniePDF.Tests.csproj
```

Run the test suite before opening a pull request. `BonniePDF.Validation/` runs
output through veraPDF for PDF/A conformance — do not disable validation cases
to make a change pass.

The library has no native dependencies and must keep it that way: no SkiaSharp,
`System.Drawing`, ImageMagick, or libpdfium.


## Parity is the contract

BonniePDF has two co-equal implementations, TypeScript and .NET, both conforming
to one language-neutral specification. A behavioural change belongs in the
specification and the shared conformance fixtures first, then in both
implementations. A pull request that changes behaviour in one language only is a
parity break and will not be merged on its own.

Differences in *spelling* between the two languages are expected and correct:
each surface is idiomatic to its language. Differences in *behaviour* are bugs.
See `AGENTS.md` for how to classify one before acting on it.

## Code of conduct

Be civil and assume good faith. Discussion stays on the technical merits of the
change. CodeFocus Limited may decline or revert a contribution, or restrict
participation, at its discretion.

## Questions

Open an issue, or email support@bonniepdf.dev.
