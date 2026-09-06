# Remoter

A Windows Remote Desktop (RDP) client. It hosts the same Remote Desktop protocol
engine the built-in client uses, wrapped in a modern connection manager, with the
goal of matching `mstsc.exe` first and then going further.

See [docs/SPEC.md](docs/SPEC.md) for the plan, the technology choices, and the roadmap.

## Status

First version. Connects to a Windows host over RDP with the standard set of options
(display, local resources, experience, security, gateway), saves named connections,
stores passwords in Windows Credential Manager, and imports/exports `.rdp` files.

## Requirements

- Windows 10 or 11
- .NET 10 SDK

## Build and run

```powershell
dotnet build
dotnet run --project src/Remoter.App
```

Run the tests:

```powershell
dotnet test
```

## Projects

- `src/Remoter.Core` — connection model, storage, credentials, `.rdp` import/export.
- `src/Remoter.Rdp` — hosts the Microsoft RDP ActiveX control.
- `src/Remoter.App` — the WPF application.
- `tests/Remoter.Core.Tests` — unit tests.

## COM interop

The app hosts the Remote Desktop ActiveX control from `mstscax.dll`. The .NET SDK
build cannot generate COM interop itself, so the interop assembly is generated once
and committed to `lib/Interop.MSTSCLib.dll`. To regenerate it (for example on a newer
Windows build), run:

```powershell
pwsh tools/Generate-Interop.ps1
```

## Security

Passwords are stored only in Windows Credential Manager (encrypted by Windows with
your account key). They are never written to the connection list or to exported
`.rdp` files. Network Level Authentication is on by default.
