# Backend Evaluator image: .NET SDK (for Roslyn + dotnet test/format/build) plus all the local support
# tools cited in EVALUATION-CRITERIA.md, so a single container can run every criterion in --deep mode.

# ---- build the evaluator ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Evaluator.csproj -c Release -o /out

# ---- runtime: SDK + bundled tools ----
FROM mcr.microsoft.com/dotnet/sdk:10.0

# OS packages + Node + Python (toolchain hosts).
RUN apt-get update && apt-get install -y --no-install-recommends \
        curl ca-certificates git jq python3 python3-pip nodejs npm \
    && rm -rf /var/lib/apt/lists/*

# Node CLIs: Spectral (OpenAPI lint), swagger-cli (validate), markdownlint (docs).
RUN npm install -g @stoplight/spectral-cli @apidevtools/swagger-cli markdownlint-cli

# Python CLIs: sqlfluff (SQL lint), Schemathesis (contract conformance), Semgrep (SAST).
# Schemathesis is pinned to 3.x: v4 renamed the CLI flags the evaluator passes (--checks all,
# --hypothesis-max-examples), so an unpinned install would break the contract check at runtime.
RUN pip3 install --no-cache-dir --break-system-packages sqlfluff 'schemathesis<4' semgrep

# Static binaries: gitleaks (secrets), trivy (SCA), hadolint (Dockerfile), k6 (load), lychee (doc links).
# --fail makes curl exit non-zero on an HTTP error (instead of piping a 403/HTML body into tar, which
# fails opaquely as "not in gzip format"); --retry rides out GitHub's transient rate-limits during builds.
RUN CURL="curl -sSL --fail --retry 5 --retry-delay 3 --retry-all-errors" \
 && $CURL https://github.com/gitleaks/gitleaks/releases/download/v8.18.4/gitleaks_8.18.4_linux_x64.tar.gz \
        | tar -xz -C /usr/local/bin gitleaks \
 && $CURL https://github.com/aquasecurity/trivy/releases/download/v0.53.0/trivy_0.53.0_Linux-64bit.tar.gz \
        | tar -xz -C /usr/local/bin trivy \
 && $CURL -o /usr/local/bin/hadolint https://github.com/hadolint/hadolint/releases/download/v2.12.0/hadolint-Linux-x86_64 \
 && chmod +x /usr/local/bin/hadolint \
 && $CURL https://github.com/grafana/k6/releases/download/v0.52.0/k6-v0.52.0-linux-amd64.tar.gz \
        | tar -xz --strip-components=1 -C /usr/local/bin k6-v0.52.0-linux-amd64/k6 \
 && $CURL https://github.com/lycheeverse/lychee/releases/download/lychee-v0.24.2/lychee-x86_64-unknown-linux-gnu.tar.gz \
        | tar -xz -C /usr/local/bin lychee

# dotnet-outdated: the dependency-freshness tool invoked by the Portability evaluator (cat 12).
# RollForward=Major lets this net8-targeted tool run on the .NET 10 runtime that ships in this image,
# and ~/.dotnet/tools must be on PATH to invoke it. (Stryker/ReportGenerator are intentionally not
# bundled: the evaluator never invokes them, so detecting them on the host would be a false positive.)
ENV DOTNET_ROLL_FORWARD=Major
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-outdated-tool

COPY --from=build /out /evaluator
WORKDIR /work
ENTRYPOINT ["dotnet", "/evaluator/Evaluator.dll"]
