# Homeocentrum.Niga.NewAPI

ASP.NET Core 8 New API solution.

| Project | Role |
|---------|------|
| `Homeocentrum.Niga.NewAPI.sln` | Solution |
| `Homeocentrum.Niga.NewAPI` | Web host (port 5038) |
| `Homeocentrum.Niga.NewAPI.Domain` | Domain / services / EF |
| `Homeocentrum.Niga.NewAPI.Domain.Tests` | xUnit tests |

```bash
dotnet build Homeocentrum.Niga.NewAPI.sln
dotnet run --project Homeocentrum.Niga.NewAPI/Homeocentrum.Niga.NewAPI.csproj --urls http://127.0.0.1:5038
```

Namespaces: host `Homeocentrum.Niga.NewAPI*`, domain `Homeocentrum.Niga.NewAPI.Domain*`. Week SQL/docs: `ScriptsAndFiles/`.
