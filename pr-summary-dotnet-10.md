# Upgrade reporting listener to .NET 10

This repository deploys an SQS-triggered AWS Lambda, rather than publishing a reusable NuGet package. Its CI packages a Lambda ZIP and deploys it through Serverless. No package release version or NuGet publishing workflow was found, so no major package version is introduced. A framework-only change to a published library would require separate consideration of consumer compatibility; that is not the deployment model here.

## Changed

- Target both application and test projects at `net10.0`.
- Use the .NET 10 SDK in both Dockerfiles and the CircleCI .NET executor.
- Select Lambda runtime `dotnet10` and framework `net10.0` in deployment defaults and Serverless configuration.
- Update ZIP paths in Serverless and both packaging scripts to `bin/release/net10.0`.
- Update the local Windows mock Lambda test profile to the .NET 10 output directory and test tool executable. Developers using that profile need `Amazon.Lambda.TestTool-10.0` installed.
- Set `NuGetAuditMode` to `direct` in both projects to preserve .NET 8's audit scope. The first .NET 10 restore failed on transitive audit findings promoted to errors. This preserves the previous policy without changing dependencies or disabling auditing altogether.
- Replace three outdated/template READMEs with the actual listener behavior, setup, configuration, external interfaces, testing and packaging instructions. This service exposes no HTTP endpoints and does not directly access a database.

The Makefile already uses `docker compose` throughout. It is intentionally retained unchanged; no `docker-compose` replacement was needed. There is no `global.json`, and Docker supplies the required SDK without adding one. Business logic and test code are unchanged.

## Package changes

None. All existing PackageReference versions are preserved. No NuGet package is published by this change.

## Validation

| Check | Result |
| --- | --- |
| Initial .NET 8 `make test` | After creating an empty local `.env`, restore and complete solution build succeeded, with two existing analyzer-version warnings. Tests could not start because downloading the Confluent Schema Registry image failed with `no route to host`. |
| First .NET 10 `make -s test` | Restore failed on the changed transitive-audit default. Fixed by explicitly retaining the prior `direct` audit mode. |
| Final .NET 10 `make -s test` | Exit 0. Restore and complete solution build succeeded; 28 tests passed, 1 skipped, 0 failed. Dependency images downloaded successfully on retry. |
| Additional .NET 10 tests without infrastructure | 20 use-case and Tenure API gateway tests passed using the built test image. |
| Baseline retry from an original-revision temporary checkout | Could not rebuild: private NuGet credential was no longer available (`password` null or empty). The original baseline image was not available for reuse. A complete before/after test-result comparison remains unavailable. |
| `git diff --check` | Passed. |

The skipped test is the existing `TenureCreatedTests.ListenerSavesTheCreatedTenureToKafka` test, already marked skipped in the repository. No tests were changed to obtain the passing result.

The two build warnings occur on both frameworks: the pinned `Microsoft.CodeAnalysis.NetAnalyzers` 5.0.3 package is older than the analyzers provided by the SDK. These remain unchanged.

For local testing, the Compose test service's `SONAR_TOKEN` build argument was temporarily commented out and has been restored exactly. The test Dockerfile has no SonarScanner, Java installation or token instructions to adjust. No SonarCloud changes remain in the diff. The temporary empty `.env` created for validation was removed.

The host has only the .NET 8 SDK; .NET 10 restore/build/tests were validated inside Docker. AWS deployment, Lambda ZIP packaging and the local Windows mock test tool were not executed.

## Out of scope and release considerations

- Existing transitive audit findings for log4net 2.0.8 and Newtonsoft.Json 9.0.1 are not remediated in this framework migration. No separate security scan was run.
- The existing analyzer package, Kafka dependencies, application Docker command and unrelated tooling are unchanged.
- CircleCI installs Serverless `^3`. That version's runtime schema may warn about the new `dotnet10` identifier. Validate packaging/deployment in the deployment environment before release; a major Serverless upgrade is not included without evidence it blocks deployment.
- No application deployment or package publication was performed.

## References

- [AWS .NET Lambda runtimes](https://docs.aws.amazon.com/lambda/latest/dg/lambda-csharp.html): managed runtime identifier `dotnet10`.
- [Microsoft .NET 10 restore compatibility change](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/nugetaudit-transitive-packages): new default includes transitive dependencies in auditing.
- [AWS mock Lambda test tool](https://github.com/aws/aws-lambda-dotnet/blob/master/Tools/LambdaTestTool/README.md): runtime-specific tool executable names.
