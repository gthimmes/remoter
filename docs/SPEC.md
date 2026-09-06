# Remoter — a Windows Remote Desktop client

## Goal

A desktop Remote Desktop (RDP) client for Windows that first matches the everyday
features of the built-in client (`mstsc.exe`), then grows beyond it. The immediate
target is connecting to another computer on the same home network safely and securely.

This document is the plan. It records what we are building, the technology choices
and why, the feature scope for the first version, and what comes next.

## Technology choice

**.NET 10, C#, WPF for the shell, hosting the Microsoft RDP ActiveX control for the
session surface.**

Why this stack:

- **Reuse the real RDP protocol engine.** Windows ships the Remote Desktop protocol
  as an ActiveX control, `mstscax.dll` (the same engine `mstsc.exe` uses). Hosting it
  gives us a correct, maintained, secure RDP implementation — TLS, CredSSP / Network
  Level Authentication, NLA, gateway support, device redirection — instead of
  reimplementing the protocol. Reimplementing RDP from scratch would be a multi-year
  effort and a security liability.
- **WPF for the shell** gives a modern, themable UI for the connection list and the
  settings dialogs. The live session is a heavy child window, so it is hosted in a
  `WindowsFormsHost` wrapping a WinForms `AxHost` around the control. WPF content and
  the session surface never overlap, so there are no airspace problems.
- **Windows Credential Manager** stores passwords, encrypted by Windows with the
  user's key (DPAPI). We never write secrets to our own files.

Considered and rejected for now:

- **A from-scratch RDP implementation** (e.g. porting FreeRDP): far more work and
  risk than the goal justifies. Revisit only if we need to run on non-Windows or need
  protocol behavior the control will not expose.
- **MAUI / WinUI 3**: no advantage here and worse interop with the ActiveX control.

### The control version

`mstscax.dll` registers several versioned CLSIDs. On current Windows 11 (build 26100)
the newest control, version 13, exists in the registry but its class factory returns
`CLASS_E_CLASSNOTAVAILABLE`. The newest control that actually instantiates is
**version 12** (`MsRdpClient11NotSafeForScripting`,
`{1DF7C823-B2D4-4B54-975A-F2AC5D7CF8B8}`), which exposes `IMsRdpClient10`,
`IMsRdpClientNonScriptable8` and `IMsRdpExtendedSettings`. We host that one.

Because the .NET SDK build cannot run COM interop generation (`ResolveComReference`),
the interop assembly is generated once with `TlbImp.exe` and committed to `lib/`. Run
`tools/Generate-Interop.ps1` to regenerate it from a newer Windows build.

## Architecture

Four projects:

| Project | Kind | Responsibility |
|---|---|---|
| `Remoter.Core` | class library | Connection model, JSON store, Credential Manager wrapper, `.rdp` import/export, session abstractions and disconnect-reason text. No UI, no COM. |
| `Remoter.Rdp` | WinForms library | Hosts the ActiveX control (`AxMsRdpClient`) and maps a profile onto its settings (`RdpSessionControl`), exposing the engine-agnostic `IRemoteSession`. |
| `Remoter.App` | WPF app | The connection list, the settings editor, the credential prompt, and the session window. |
| `Remoter.Core.Tests` | xUnit | Unit tests for the model, store, credential logic, `.rdp` round-tripping and disconnect mapping. |

`Remoter.App` depends on both libraries; `Remoter.Rdp` depends on `Remoter.Core`;
`Remoter.Core` depends on nothing. The UI talks to sessions only through
`IRemoteSession`, so the engine can be swapped later without touching the app.

## First-version scope (parity with the built-in client)

- **Connections**: save named connections; quick-connect by typing a computer name;
  edit, duplicate, delete; import and export `.rdp` files.
- **Credentials**: user name and domain per connection; optional saved password in
  Windows Credential Manager; prompt for credentials when not saved.
- **Display**: fit-to-window (dynamic resolution), fixed size with smart-sizing scale,
  or fixed size with scrollbars; color depth; DPI scale; full screen; span all
  monitors; connection bar.
- **Local resources**: audio playback location and microphone capture; where Windows
  key combinations apply; redirect clipboard, printers, smart cards, ports, plug-and-
  play devices and drives (all / specific / drives plugged in later).
- **Experience**: connection-speed presets plus individual toggles for wallpaper,
  font smoothing, desktop composition, window drag contents, animation, visual styles,
  persistent bitmap cache, and automatic reconnection.
- **Security**: server-authentication policy; Network Level Authentication (on by
  default); Restricted Admin and Remote Credential Guard; console/admin session.
- **Gateway**: Remote Desktop Gateway host and credential sharing.
- **In session**: disconnect, full-screen toggle, send Ctrl+Alt+Del, match remote
  resolution to the window; clear, actionable messages when a connection fails.

## Security posture

- NLA is on by default; the safe path for a home network.
- Passwords live only in Windows Credential Manager, never in our JSON or in exported
  `.rdp` files.
- Server-authentication policy is surfaced so the user decides what happens when a
  host's identity cannot be verified.

## What comes next (beyond the built-in client)

These are the reasons to build our own client rather than use `mstsc.exe`. Not in the
first version; recorded so the architecture leaves room for them.

- **Tabbed / multi-session** management in one window.
- **Connection folders and search** for many machines.
- **Wake-on-LAN** before connecting.
- **Per-connection notes and quick actions**.
- **Session thumbnails / live previews**.
- **Scriptable / CLI** connect.
- **Cross-machine settings sync**.
