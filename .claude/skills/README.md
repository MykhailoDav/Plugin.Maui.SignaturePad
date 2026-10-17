# Agent skills

These are skills for AI coding agents, such as Claude Code, that work on this repository. Each skill is a folder with a `SKILL.md`. The folders are unmodified copies of the upstream skills, pinned to a commit. Each one keeps its upstream `LICENSE.txt` and a `SOURCE.txt` with the exact source URL.

| Skill | Source | License |
| --- | --- | --- |
| `maui-custom-handlers` | [dotnet/maui-labs](https://github.com/dotnet/maui-labs) `plugins/dotnet-maui` @ `6fc5fbc` | MIT |
| `maui-current-apis` | dotnet/maui-labs `plugins/dotnet-maui` @ `6fc5fbc` | MIT |
| `maui-performance` | dotnet/maui-labs `plugins/dotnet-maui` @ `6fc5fbc` | MIT |
| `maui-accessibility` | dotnet/maui-labs `plugins/dotnet-maui` @ `6fc5fbc` | MIT |
| `maui-unit-testing` | dotnet/maui-labs `plugins/dotnet-maui` @ `6fc5fbc` | MIT |
| `maui-project-structure` | dotnet/maui-labs `plugins/dotnet-maui` @ `6fc5fbc` | MIT |
| `maui-devflow-debug` | dotnet/maui-labs `plugins/dotnet-maui-tooling` @ `6fc5fbc` | MIT |
| `maui-devflow-onboard` | dotnet/maui-labs `plugins/dotnet-maui-tooling` @ `6fc5fbc` | MIT |
| `maui-graphics-drawing` | [davidortinau/maui-skills](https://github.com/davidortinau/maui-skills) @ `79bb4bf` | MIT |
| `nuget-trusted-publishing` | [dotnet/skills](https://github.com/dotnet/skills) @ `10d85e2` | MIT |
| `run-tests` | dotnet/skills @ `10d85e2` | MIT |
| `directory-build-organization` | dotnet/skills @ `10d85e2` | MIT |

To update a skill, replace its folder with the upstream version and update `SOURCE.txt` and this table. Do not edit the copies in place; put project-specific guidance in [AGENTS.md](../../AGENTS.md).

Some upstream guidance is out of date for this repository:
- `maui-devflow-*` install global .NET tools and change Debug builds of an app. Use them only with the sample app or the UI test host app, never with the library.
- `nuget-trusted-publishing` shows a Linux runner and .NET 9. Publishing this package needs .NET 10 with the MAUI workload.

Maintainers can keep more skills from their other projects in this folder. Those skills are listed in `.gitignore` and are not part of the repository.
