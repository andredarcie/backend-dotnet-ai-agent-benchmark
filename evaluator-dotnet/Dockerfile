# Backend Evaluator image: the .NET SDK (Roslyn in-process + dotnet build/test/format/list) plus the two
# static binaries the rubric still invokes — gitleaks (secrets) and hadolint (Dockerfile lint).
#
# Deliberately small. The image used to bundle Node 20, Python, Semgrep, Trivy, Schemathesis, Spectral,
# markdownlint, sqlfluff, lychee and dotnet-outdated (~3-4 GB, several minutes of network per grading
# run). Those tools were retired: they dominated the wall-clock and made the score depend on remote rule
# sets / CVE databases that change over time, so the same source could be graded differently on two days.
#
# NOT fully air-gapped, and don't claim otherwise: `dotnet restore` (implied by build/test) and the
# vulnerability audit behind `dotnet list package --vulnerable` still reach nuget.org. No *tool* here
# downloads a rule set, but the NuGet vulnerability data does move as CVEs are disclosed — so the SCA
# metric is the one check that is not a pure function of the source. Everything else is.

# ---- build the evaluator ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Evaluator.csproj -c Release -o /out

# ---- runtime: SDK + the two bundled linters ----
FROM mcr.microsoft.com/dotnet/sdk:10.0

# curl/ca-certificates are only needed to fetch the two static binaries below.
# --fail makes curl exit non-zero on an HTTP error (instead of piping a 403/HTML body into tar, which
# fails opaquely as "not in gzip format"); --retry rides out GitHub's transient rate-limits during builds.
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates \
 && CURL="curl -sSL --fail --retry 5 --retry-delay 3 --retry-all-errors" \
 && $CURL https://github.com/gitleaks/gitleaks/releases/download/v8.18.4/gitleaks_8.18.4_linux_x64.tar.gz \
        | tar -xz -C /usr/local/bin gitleaks \
 && $CURL -o /usr/local/bin/hadolint https://github.com/hadolint/hadolint/releases/download/v2.12.0/hadolint-Linux-x86_64 \
 && chmod +x /usr/local/bin/hadolint \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /out /evaluator
WORKDIR /work
ENTRYPOINT ["dotnet", "/evaluator/Evaluator.dll"]
