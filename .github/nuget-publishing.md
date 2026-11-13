# Publish BonniePDF to NuGet

Set up [Trusted Publishing on NuGet.org](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing#github-actions-setup) once:

| NuGet policy field | Value |
| --- | --- |
| Publisher | GitHub Actions |
| Repository owner | `bonniepdf` |
| Repository | `BonniePDF-NET` |
| Workflow file | `publish.yml` |
| Environment | Leave blank |
| Package owner | The NuGet user or organization that should own the package |
| Package pattern | `BonniePDF` |
| Scopes | Publish new packages and publish new versions |

In the GitHub repository, go to **Settings → Secrets and variables → Actions →
Variables** and set `NUGET_USER` to the individual NuGet.org **profile name** that
created the policy. If the policy belongs to an organization, use the member's
profile name. No long-lived API key is needed.

Pushing a version tag starts publishing. To retry an existing tag, go to
**Actions → Publish to NuGet → Run workflow**, select the default branch, and
enter the existing tag. From the CLI:

```sh
gh workflow run publish.yml --repo bonniepdf/BonniePDF-NET --ref master -f tag=v2026.11.13
```

The workflow checks the package version, validates the tagged commit against
the gallery, publishes to NuGet, and creates the GitHub release or fills in its
missing package asset. An existing GitHub release and asset are preserved.
