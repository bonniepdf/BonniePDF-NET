## What does this PR do?

<!-- One or two sentences describing the change. -->

## Type of change

- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Refactor / internal improvement
- [ ] Documentation / tooling

## Checklist

- [ ] `dotnet build BonniePDF.Tests/BonniePDF.Tests.csproj -c Release` passes
- [ ] `dotnet test BonniePDF.Tests/BonniePDF.Tests.csproj -c Release` passes
- [ ] `dotnet pack BonniePDF/BonniePDF.csproj -c Release` produces a valid package
- [ ] Fluent (`Pdf.Create`) and traditional (`new Document`) APIs both updated — they must produce byte-identical output
- [ ] Public API changes are reflected in the README or docs
- [ ] Breaking changes are clearly described above
- [ ] If this touches the TypeScript port's behaviour, the matching change is tracked or made in `BonniePDF-TypeScript`
