# Remoter roadmap — toward an enterprise connection manager

This maps the feature set of the leading enterprise remote-desktop managers onto Remoter,
and sequences what we build. The reference product is **Devolutions Remote Desktop
Manager (RDM)**; we also draw from **Microsoft RDCMan**, **mRemoteNG**, and **Royal TS**.

## The reference products and their editions

**Devolutions Remote Desktop Manager** ships in two editions:

- **Free (Solo)** — single user, local storage. Includes most of what makes it a
  "manager": many connection types, nested folders, credential inheritance, quick
  connect, a tabbed session interface, import/export, VPN integration, macros and
  PowerShell, and offline use.
- **Enterprise (Team)** — everything in Free plus team features: shared data sources
  (SQL Server, MariaDB, Devolutions Server), role-based access control, a full audit
  trail, session recording (RDP/SSH/VNC), two-factor authentication, password-manager
  integrations (1Password, KeePass, Bitwarden, LastPass, Keeper…), reporting, and a
  custom installer.

RDM supports 150+ connection types (RDP, SSH, VNC, Telnet, Citrix, VMware, web, cloud
consoles, FTP/SFTP) and 60+ add-ons.

**Others:** RDCMan (free, Microsoft, RDP-only, grouped tree). mRemoteNG (free, open
source, tabbed multi-protocol: RDP/VNC/SSH/Telnet/rlogin/HTTP). Royal TS (paid,
multi-protocol, teams, cross-platform).

Sources: Devolutions "Compare editions" and product pages, ActiveDirectory Pro and
ITPRC manager round-ups (captured September 2026).

## What Remoter has today

Parity with the built-in Windows client: saved connections, quick connect, recent
hosts, `.rdp` import/export, Credential Manager passwords, the full RDP option set
(display, local resources, experience, security, gateway), full-screen, Ctrl+Alt+Del,
fit-to-window, and clear connection errors. Single user, local, RDP only.

## Where we are going

Remoter targets the **Free/Solo experience first** — a great single-user local manager —
then selectively adds team features. Everything below is achievable on the current
WPF + RDP-ActiveX architecture unless noted.

### Tier 1 — Organize (in progress)

The jump from "a list" to "a manager."

- **Folders / groups** for connections, nested.
- **Search and filter** across name, host, user, tags.
- **Favorites** pinned for one-click access.
- **Tags and notes** per connection.
- **Credential / setting inheritance** from a folder to its connections.

### Tier 2 — Session experience

- **Tabbed sessions**: many live sessions docked as tabs in one window (the signature
  RDM/mRemoteNG feature).
- **Connection status**: reachability/ping indicator per connection.
- **Wake-on-LAN** before connecting.
- **Thumbnails / live previews** of open sessions.

### Tier 3 — More protocols

Requires new engines beyond the RDP control; the `IRemoteSession` abstraction already
isolates the UI from the engine.

- **SSH / Telnet** via an embedded terminal.
- **VNC** via a VNC client library.
- **Web / cloud consoles** via an embedded browser.
- **RDCMan `.rdg`** and bulk `.rdp` import; Active Directory computer import.

### Tier 4 — Team / enterprise

- **Named / shared credentials** reused across connections (start local, single-user).
- **Session recording** and an **audit log**.
- **Shared data source** (SQL Server / a small sync server), **RBAC**, **2FA**.
- **Password-manager integrations** (KeePass, Bitwarden, 1Password).

## Principles

- **Local and private by default.** Secrets stay in Windows Credential Manager; team
  storage is opt-in.
- **Engine-agnostic UI.** New protocols implement `IRemoteSession`; the shell does not
  change.
- **Keep parity working.** Every increment keeps the built-in-client feature set intact.
