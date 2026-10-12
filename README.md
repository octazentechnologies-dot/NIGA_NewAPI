# Homeocentrum.Niga.API

ASP.NET Core 8 API solution.

| Project | Role |
|---------|------|
| `Homeocentrum.Niga.API.sln` | Solution |
| `Homeocentrum.Niga.API` | Web host (port 5002) |
| `Homeocentrum.Niga.API.Domain` | Domain / services / EF |
| `Homeocentrum.Niga.API.Domain.Tests` | xUnit tests |

```bash
dotnet build Homeocentrum.Niga.API.sln
dotnet run --project Homeocentrum.Niga.API/Homeocentrum.Niga.API.csproj --urls http://127.0.0.1:5002
```

Namespaces: host `Homeocentrum.Niga.API*`, domain `Homeocentrum.Niga.API.Domain*`. Week SQL/docs: `ScriptsAndFiles/`.
