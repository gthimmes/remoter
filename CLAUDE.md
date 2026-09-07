# Remoter

WPF Remote Desktop client for Windows, .NET 10. Hosts the Microsoft RDP ActiveX control
behind a connection manager. See `README.md` for the product summary and `docs/SPEC.md`
for the plan.

## Commands

```powershell
dotnet build
dotnet run --project src/Remoter.App
dotnet test
```

## Layout

- `src/Remoter.Core` connection model, storage, credentials, `.rdp` import/export. No UI.
- `src/Remoter.Rdp` hosts the RDP ActiveX control.
- `src/Remoter.App` the WPF app. `App.xaml`, `MainWindow`, `EditConnectionWindow`,
  `SessionHostWindow`, `CredentialPromptWindow`, `Views/`.
- `tests/Remoter.Core.Tests` xUnit.

## UI work

**Read `docs/design/REMOTER-DESIGN.md` before changing any XAML.** It is the source of
truth for color, type, spacing and control states.

- Never introduce a color that is not a token in `Themes/Dark.xaml`. If a style needs a
  color that is not there, add a token to both theme files, do not add a hex.
- Reference color tokens with `DynamicResource`. `StaticResource` breaks the runtime theme
  swap. Non-color resources and anything used in `BasedOn` stay `StaticResource`, because
  `BasedOn` cannot take a `DynamicResource`.
- `Themes/Dark.xaml` and `Themes/Light.xaml` must hold the same keys. A key in one and not
  the other throws at swap time.
- The connection list is a `ListView` with a `GridView`. Style `ListViewItem` and
  `GridViewColumnHeader`. A `ListViewItem` template needs a `GridViewRowPresenter`; a plain
  `ContentPresenter` silently collapses every column into one.
- `SessionHostWindow` builds `SessionTabItem` on the base `TabItem` style with `BasedOn`.
  Do not delete the base `TabControl` or `TabItem` styles when the connection dialog stops
  using tabs.

## Conventions

- `.editorconfig` is authoritative: 4-space C#, 2-space XAML and project files, CRLF,
  final newline. Existing `App.xaml` uses 4-space XAML and predates this rule. Reformat it
  when you next touch it substantially, not as a drive-by.
- Nullable and implicit usings are on solution-wide via `Directory.Build.props`.

## Brand mark

Source of truth is `assets/logo/mark.svg`. `Remoter.ico` is built from it, and
`src/Remoter.App/Themes/Logo.xaml` restates the same geometry as WPF vector shapes for
in-app use. Change the SVG and all three have to move together.

- The 16, 20 and 24px icon layers are hand-drawn (`mark-16.svg`, `mark-20.svg`,
  `mark-24.svg`), not downsampled, because the corner radii turn to mush below 32px.
  Never regenerate the .ico by scaling `mark.svg` alone; you will lose them.
- The .ico carries 16, 20, 24, 32, 48, 64, 128 and 256. Small layers are 32-bit BGRA
  DIB entries, the 256 is a PNG entry.
- Brand gradient is `#4E8FFF` to `#1745B8`, taken from the UI's `Accent` and `AccentFill`.
  Flat `#2F6BE0` is the substitute at 24px and below, where a gradient reads as noise.

## Gotchas

- `lib/Interop.MSTSCLib.dll` is committed on purpose. The SDK build cannot generate COM
  interop. Regenerate with `pwsh tools/Generate-Interop.ps1`, do not try to make MSBuild
  do it.
- Passwords live only in Windows Credential Manager. Never write one to the connection
  list, to an exported `.rdp`, or to a log.
- `Background="Black"` in `SessionHostWindow` is the RDP surface, not chrome. Leave it.
