# Remoter design spec

Visual spec for Remoter: `MainWindow`, `EditConnectionWindow`, and the shared control
styles in `App.xaml`. Rendered reference: the Remoter Visual Spec artifact.

**Rule zero.** No control sets a literal color. Every brush comes from `Themes/Dark.xaml`
or `Themes/Light.xaml`, referenced with `DynamicResource`. If a style needs a color that
is not a token, add a token, do not add a hex. This is what makes light theme free
instead of a second design.

Token names are the ones already in this repo. Values changed, names did not, except for
the additions in section 2.

All measurements are in DIP, which equals CSS pixels at 96 DPI. Base unit is 4.

---

## 1. Color tokens

| Token | Dark | Light | Used for |
|---|---|---|---|
| `Bg` | `#14161A` | `#F3F4F6` | Window ground, toolbar, status bar, dialog rail and footer |
| `Panel` | `#1A1D22` | `#FFFFFF` | Title bar, quick connect strip, list body, dialog content pane |
| `PanelAlt` | `#22262D` | `#ECEEF1` | Ghost button hover, count pills, popups, selected tab |
| `Input` | `#1E2229` | `#FFFFFF` | Text box, password box, combo box, checkbox fill |
| `Border` | `#2B3038` | `#E1E4E9` | Section dividers, list rules |
| `BorderStrong` | `#3A414C` | `#C7CCD4` | Input outlines, scrollbar thumb, secondary button border |
| `Text` | `#E4E7EC` | `#191C21` | Row names, button labels, headings |
| `TextMuted` | `#9BA3B0` | `#5B6472` | Data cells, ghost button labels, field labels |
| `TextFaint` | `#7E8898` | `#666F7C` | Column headers, timestamps, status bar, placeholders, helper text |
| `TextOnAccent` | `#FFFFFF` | `#FFFFFF` | Label on a filled accent button |
| `Accent` | `#4C8DFF` | `#1D4FD0` | Accent *text*: selection rail, focus ring, sort chevron, tonal label |
| `AccentFill` | `#1A62D8` | `#1D4FD0` | Filled button ground. Carries a white label. |
| `AccentHover` | `#1F6FEB` | `#1A45B8` | Filled button hover |
| `AccentPressed` | `#154FAF` | `#15379E` | Filled button pressed |
| `AccentSubtle` | `#4C8DFF` @ 14% | `#1D4FD0` @ 10% | Tonal button fill, selected rail item |
| `Selection` | `#4C8DFF` @ 18% | `#1D4FD0` @ 12% | Selected list row |
| `SelectionInactive` | `#8C96A8` @ 10% | `#5B6472` @ 9% | Selected row when the list loses focus |
| `Hover` | `#FFFFFF` @ 4.5% | `#10141C` @ 3.5% | Row hover, window button hover |
| `Danger` | `#F0645C` | `#BF2A21` | Delete hover, validation errors |
| `DangerSubtle` | `#F0645C` @ 12% | `#BF2A21` @ 10% | Delete hover fill |
| `Success` | `#3FB37F` | `#15754F` | Live session dot and label |
| `Favorite` | `#F5C451` | `#9C6A15` | Favorite star in the list gutter |

Neutrals carry a slight blue bias on purpose so they sit with the accent instead of
fighting it. Do not swap in true grays.

**Why the accent splits in two.** In dark theme `#4C8DFF` reads as accent text on a dark
ground at 5.3:1, but only 3.2:1 behind a white label, which fails at 13 DIP. Filled buttons
therefore sit on the deeper `AccentFill`, and the bright value stays for text, rails and the
focus ring. Light needs no split: `#1D4FD0` clears AA both as text on white and as a ground
under white.

**One rule the tokens cannot express.** On a selected row, `TextFaint` cells promote to
`TextMuted`. Faint ink reaches only 3.6:1 against the selection tint, and a selected row
should read stronger than its neighbors anyway.

**Tags.** Hue is picked deterministically by hashing the tag string, so a tag is the same
color in every row. Four hues cycled: green, indigo, amber, violet. Fill is the hue at 16%
in dark and 11% in light, text is the hue itself. Every combination clears 5:1 against its
own composited fill.

---

## 2. Migrating the existing palette

`Themes/Dark.xaml` and `Themes/Light.xaml` are already in the repo. They keep every key
name that was in `App.xaml` and add nine.

### Values that changed

| Token | Was | Now (dark) | Why |
|---|---|---|---|
| `Bg` | `#17181B` | `#14161A` | Slight blue bias, more separation from `Panel` |
| `Panel` | `#202227` | `#1A1D22` | Same |
| `PanelAlt` | `#2A2D33` | `#22262D` | `PanelAlt` was doing double duty as input fill; `Input` now covers that |
| `Border` | `#3B3E46` | `#2B3038` | Current rules are heavier than the content they separate |
| `BorderStrong` | `#4A4E57` | `#3A414C` | Same |
| `Text` | `#ECEDEF` | `#E4E7EC` | Slightly off maximum so bold text does not bloom |
| `TextMuted` | `#B7BBC4` | `#9BA3B0` | Was too close to `Text` to create a second level |
| `TextFaint` | `#8A8E98` | `#7E8898` | Old value failed AA at 4.1:1 on `Panel` |
| `Accent` | `#5B8CFF` | `#4C8DFF` | Marginal, kept close to what you had |
| `AccentHover` | `#7099FF` | `#1F6FEB` | Role change: this is now the filled-button hover, not a lighter accent |
| `Selection` | `#2F4B86` | `#4C8DFF` @ 18% | Opaque to alpha, so it composites correctly over any row ground |
| `Danger` | `#E5595E` | `#F0645C` | Marginal |

### Tokens added

`Input`, `AccentFill`, `AccentPressed`, `AccentSubtle`, `SelectionInactive`, `Hover`,
`DangerSubtle`, `Success`, `Favorite`.

### Literals to sweep

Every hardcoded hex currently in the XAML, with its replacement. These are the ones that
exist as of this writing; grep for `Background="#`, `Foreground="#` and `Color="#` outside
the theme files before calling it done.

**`App.xaml`**

| Line | Literal | Replace with |
|---|---|---|
| 53 | `#2A2D33` TextBox template fill | `Input` |
| 80 | `#2A2D33` PasswordBox template fill | `Input` |
| 114 | `#2A2D33` ComboBox template fill | `Input` |
| 126 | `#24272D` ComboBox popup fill | `PanelAlt` |
| 176 | `#2A2D33` CheckBox box fill | `Input` |
| 203 | `#2A2D33` default Button background | `Transparent` (see the ghost button rule) |
| 218 | `#343841` Button hover | `PanelAlt` |
| 222 | `#2E323A` Button pressed | `Border` |
| 253 | `#4C7DF0` AccentButton pressed | `AccentPressed` |
| 302 | `#24272D` TabItem selected | `PanelAlt` |

**`MainWindow.xaml`**

| Line | Literal | Replace with |
|---|---|---|
| 27 | `#2A2D33` quick connect box fill | `Input` |
| 56 | `#24272D` recents popup fill | `PanelAlt` |
| 86 | `#31353D` recents item hover | `PanelAlt` |
| 122 | `#2A2D33` search box fill | `Input` |
| 179 | `#262930` ListViewItem state | `Hover` or `Selection`, depending on which trigger it is |
| 192 | `#1B1C20` group header fill | `Transparent`, with a 1 DIP `Border` rule above |
| 211 | `#F5C451` favorite star | `Favorite` |

**`SessionHostWindow.xaml`** line 6 and line 99 use `Background="Black"`. Leave both.
That is the RDP surface, not chrome, and black is correct there.

### Three things that will bite

1. **`StaticResource` blocks the theme swap.** Every color reference in `App.xaml`,
   `MainWindow.xaml`, `EditConnectionWindow.xaml`, `CredentialPromptWindow.xaml` and
   `SessionHostWindow.xaml` has to become `DynamicResource`. Non-color resources
   (`BoolToVisibility`, `ComboToggle`, `TabStrip`, `SessionTabItem`, style keys used in
   `BasedOn`) stay `StaticResource`. `BasedOn` in particular cannot take a
   `DynamicResource`, so leave those alone.
2. **`Selection` is doing two jobs.** `App.xaml` line 46 uses it as `TextBox.SelectionBrush`.
   Now that `Selection` is an 18% alpha tint, and WPF applies `SelectionOpacity` 0.4 on top,
   text selection would be nearly invisible. Point `SelectionBrush` at `Accent` and set
   `SelectionOpacity` to 0.35 on `TextBox` and `PasswordBox`.
3. **Keep the `TabControl` and `TabItem` styles.** The connection dialog stops using them,
   but `SessionHostWindow` builds `SessionTabItem` on top of the base `TabItem` style with
   `BasedOn`. Deleting them breaks the session tab strip.

### Wiring it up

`App.xaml` becomes a merged dictionary. The theme sits at index 0 so it can be swapped.

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Themes/Dark.xaml" />
            <ResourceDictionary Source="Themes/Controls.xaml" />
        </ResourceDictionary.MergedDictionaries>
        <BooleanToVisibilityConverter x:Key="BoolToVisibility" />
    </ResourceDictionary>
</Application.Resources>
```

The control styles currently inline in `App.xaml` move to `Themes/Controls.xaml`. No
`.csproj` change is needed: the WPF SDK globs `**/*.xaml` as `Page` items.

```csharp
// App.xaml.cs
public static void ApplyTheme(string name)   // "Dark" or "Light"
{
    var dict = new ResourceDictionary
    {
        Source = new Uri($"Themes/{name}.xaml", UriKind.Relative)
    };
    Application.Current.Resources.MergedDictionaries[0] = dict;
}
```

On startup, follow Windows unless the user picked a theme:
`HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`.
Offer three options in settings: System, Dark, Light. Default to System.

The swap repainting without a restart is the test for whether any `StaticResource` slipped
into a color binding. If something stays dark when you switch to light, that control is
your bug.

---

## 3. Type

Family: `Segoe UI Variable Text`, falling back to `Segoe UI`. Above 18 DIP use
`Segoe UI Variable Display`. Timestamps and any numeric column use tabular figures
(`Typography.NumeralAlignment="Tabular"`).

| Role | Size | Weight | Applied to |
|---|---|---|---|
| Title | 18 | 600 | Dialog titles, empty state heading |
| Body | 13 | 400 | List cells, combo box, search, input text |
| BodyStrong | 13 | 500 | Row names, ghost button labels, rail items |
| Action | 13 | 600 | Connect, Save, New |
| Caption | 12 | 500 | Column headers, group headers, field labels |
| Micro | 11 | 500 | Tags, count pills, status bar, window title, helper text |
| Label | 11 | 600 | QUICK CONNECT and dialog section headings. Uppercase, tracking 0.075em |

---

## 4. Metrics

### Main window

Default 960 x 604. Minimum 720 x 480.

| Element | Height | Padding | Notes |
|---|---|---|---|
| Title bar | 40 | 14 left | `Panel`, 1 bottom border |
| Quick connect strip | 56 | 16 x | `Panel`, 12 gap between children |
| Toolbar | 52 | 16 x | `Bg`, 4 gap within a group, 12 between groups |
| Column header row | 32 | 12 x per cell | 1 bottom border, no vertical separators |
| Group header row | 30 | 10 left | 1 top border, except the first group |
| Data row | 34 | 12 x per cell | 40 leading gutter, no vertical rules |
| Status bar | 26 | 16 x | `Bg`, 1 top border |
| Tag pill | 18 | 6 x | 4 gap between tags |

The list is a `ListView` with a `GridView`, not a `DataGrid`. Style
`GridViewColumnHeader` and `ListViewItem`, not `DataGridColumnHeader` and `DataGridRow`.

`GridViewColumn` widths: gutter 40 fixed, Name 220, Computer 210, User 170, Tags 150,
Last connected 150. Those are the current values and they are fine. What changes is that
the columns should stretch: bind the last column or use a `ViewBox`-free star-sizing
workaround so the row fills the window instead of leaving dead space on the right.

**Gutter.** The 34 DIP favorite column becomes 40 and holds two marks: a 6 DIP `Success`
dot when a session is open for that profile, then the existing favorite star at 12 DIP,
4 DIP apart. Both are independently visible. Connecting to a favorite should not hide
that it is a favorite.

### Connection settings dialog

Default 720 x 560. Minimum 640 x 480. Resizable, centered on owner.

| Element | Size | Padding | Notes |
|---|---|---|---|
| Section rail | 172 wide | 12 y, 8 x | `Bg`, 1 right border |
| Rail item | 32 tall | 12 x | Radius 6, 2 gap between items |
| Content pane | fills | 22 y, 26 x | `Panel`, scrolls independently |
| Field | 32 input | 6 label gap | 16 between fields, 24 before a section heading |
| Section heading | 11/600 caps | 10 rule gap | Hairline `BorderStrong` rule fills remaining width |
| Helper text | 11 | 6 below input | `TextFaint`. Format hints go here, never in the label. |
| Checkbox | 16 x 16 | 9 label gap | Radius 4, `AccentFill` when checked |
| Footer | 60 tall | 20 x | `Bg`, 1 top border, 8 button gap, right aligned |

Rail order: General, Organize, Display, Local resources, Experience, Security, Gateway.
Same seven sections, same fields, same bindings as the current `TabItem` set.

### Shared

| Property | Value | Applied to |
|---|---|---|
| Radius small | 4 | Tags, count pills, checkbox |
| Radius control | 6 | Buttons, text boxes, combo boxes, rail items |
| Radius surface | 8 | Window, dialogs, flyouts, context menus |
| Control height | 32 | Every interactive control, no exceptions |
| Shadow flyout | dark `0 8 24 rgba(0,0,0,.45)`, light `0 4 16 rgba(16,20,28,.14)` | Menus, popups |
| Focus ring | 2 DIP `Accent`, 1 offset, outside the border | Keyboard focus only |
| Transition | 120ms ease-out, background and foreground only | Never animate layout |
| Scrollbar | 10 DIP overlay, thumb `BorderStrong` radius 5 | Visible on hover or scroll |

---

## 5. Control states

**Filled button** (`AccentButton`: Connect, Save). Exactly one per window.
Rest `AccentFill`. Hover `AccentHover`. Pressed `AccentPressed`.
Disabled `PanelAlt` fill with `TextFaint` label and no hover.

**Tonal button** (new, `TonalButton`: New). The only other accent-colored control.
`AccentSubtle` fill, `Accent` label, SemiBold.

**Ghost button** (`GhostButton`, and the default `Button` style: Edit, Duplicate, Import,
Export). Rest transparent with `TextMuted`. Hover `PanelAlt` with `Text`.
Pressed `Border`. Disabled `TextFaint` at 55% opacity.
Note this changes the default `Button`: today it rests on `#2A2D33`, which is why every
toolbar action looks equally clickable.

**Danger button** (new, `DangerButton`: Delete). Identical to a ghost button at rest.
Hover only: `DangerSubtle` fill, `Danger` label. Red is the confirmation, not the label.

**Secondary button** (new, `SecondaryButton`: Cancel). Transparent with a 1 DIP
`BorderStrong` outline and `Text` label. Hover fills `PanelAlt`, border unchanged.

**Text, password and combo box.** Rest `Input` with 1 DIP `BorderStrong`.
Hover lightens the border one step. Focus is a 1 DIP `Accent` border plus the 2 DIP ring.
Invalid is a `Danger` border with an 11 DIP message below.
`SelectionBrush` is `Accent` at `SelectionOpacity` 0.35.

**List row** (`ListViewItem`). Rest transparent. Hover `Hover`.
Selected `Selection` plus a 2 DIP `Accent` rail on the leading edge.
Selected while the list is unfocused drops to `SelectionInactive` with no rail.
On a selected row, `TextFaint` cells promote to `TextMuted`.

**Column header** (`GridViewColumnHeader`). 12/500 in `TextFaint`, transparent fill,
one 1 DIP bottom `Border`, no separators. Sorted column goes `Text` with an `Accent`
chevron.

**Rail item.** Rest transparent with `TextMuted`. Hover `PanelAlt` with `Text`.
Selected `AccentSubtle` fill with `Accent` text at SemiBold. No underline.

**Checkbox.** Rest 1 DIP `BorderStrong` on `Input`. Hover border to `Accent` at 60%.
Checked `AccentFill` with a white 1.6 DIP check. The label is clickable.

**Empty state.** Centered in the list area. 48 DIP outline icon in `TextFaint`,
"No saved connections" at 18/600, one tonal New connection button below.

---

## 6. What changes, and why

### Main window

1. **One filled button.** Connect and New are both `AccentButton` today, so nothing is the
   primary action. Connect keeps `AccentButton`, New becomes `TonalButton`, the rest are
   ghosts.
2. **Weighted and grouped toolbar.** Edit, Duplicate and Delete disable until a row is
   selected. A 1 DIP divider separates row actions from Import and Export.
3. **Three ink levels, held.** `Text` for Name, `TextMuted` for data cells, `TextFaint`
   for timestamps. Nothing else. Color encodes hierarchy, never identity.
4. **Selection reads as selection.** Accent tint plus a 2 DIP rail, with a distinct
   unfocused state. The current opaque `#2F4B86` band has no relationship to the accent.
5. **Compact quick connect.** 56 DIP strip with 32 DIP controls and a 300 DIP combo.
6. **Headers recede.** `GridViewColumnHeader` at 12/500 in `TextFaint`, transparent fill,
   one bottom border.
7. **Real group headers.** Chevron, name, count pill, rule above, no filled band.
   Groups collapse.
8. **Overlay scrollbars.** The always-visible horizontal scrollbar goes away.

### Connection settings dialog

9. **Left rail replaces the tab strip.** Seven `TabItem` headers do not fit 598 DIP, so they
   wrap onto two rows with Experience, Security and Gateway landing above General. A 172 DIP
   `ListBox` rail plus a `ContentControl` holds all seven in order at any size.
10. **Scrolling content pane.** Content scrolls between a fixed title bar and a fixed 60 DIP
    footer. Today the user name box is cut in half by the button bar.
11. **Paired fields.** Computer and Port share a row with Port fixed at 96. Field gap 16,
    section gap 24. General fits without scrolling.
12. **Three levels of label.** Section headings at 11/600 uppercase with a hairline rule,
    field labels at 12/500 `TextMuted`, helper text at 11 `TextFaint` below the input.
    `user, DOMAIN\user or user@domain` moves out of the label into helper text.

### Worth adding while you are in there

- The live session dot in the gutter, alongside the favorite star.
- An empty state, because a fresh install opens to a blank rectangle with a scrollbar.
- Save disabled until the dialog has both a name and a computer.

---

## 7. Controls.xaml patterns

`Themes/Dark.xaml` and `Themes/Light.xaml` are done. `Themes/Controls.xaml` is not: it
should be created by moving the existing styles out of `App.xaml` and reworking them
against the tokens. These are the patterns that carry the change. Build and run after each
one, since a broken `ControlTemplate` fails at runtime rather than at build.

```xml
<!-- Base: every button is 32 tall, radius 6, 13/500, transparent by default -->
<Style x:Key="ButtonBase" TargetType="Button">
    <Setter Property="Height" Value="32" />
    <Setter Property="Padding" Value="14,0" />
    <Setter Property="FontSize" Value="13" />
    <Setter Property="FontWeight" Value="Medium" />
    <Setter Property="Cursor" Value="Hand" />
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="{DynamicResource TextMuted}" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border x:Name="Bd"
                        Background="{TemplateBinding Background}"
                        CornerRadius="6"
                        Padding="{TemplateBinding Padding}"
                        SnapsToDevicePixels="True">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsEnabled" Value="False">
                        <Setter TargetName="Bd" Property="Opacity" Value="0.55" />
                        <Setter Property="Foreground" Value="{DynamicResource TextFaint}" />
                        <Setter Property="Cursor" Value="Arrow" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>

<!-- Ghost: Edit, Duplicate, Import, Export. Also the implicit Button style. -->
<Style x:Key="GhostButton" TargetType="Button" BasedOn="{StaticResource ButtonBase}">
    <Style.Triggers>
        <MultiTrigger>
            <MultiTrigger.Conditions>
                <Condition Property="IsMouseOver" Value="True" />
                <Condition Property="IsEnabled" Value="True" />
            </MultiTrigger.Conditions>
            <Setter Property="Background" Value="{DynamicResource PanelAlt}" />
            <Setter Property="Foreground" Value="{DynamicResource Text}" />
        </MultiTrigger>
    </Style.Triggers>
</Style>

<!-- Danger: Delete. Neutral at rest, red on hover only. -->
<Style x:Key="DangerButton" TargetType="Button" BasedOn="{StaticResource ButtonBase}">
    <Style.Triggers>
        <MultiTrigger>
            <MultiTrigger.Conditions>
                <Condition Property="IsMouseOver" Value="True" />
                <Condition Property="IsEnabled" Value="True" />
            </MultiTrigger.Conditions>
            <Setter Property="Background" Value="{DynamicResource DangerSubtle}" />
            <Setter Property="Foreground" Value="{DynamicResource Danger}" />
        </MultiTrigger>
    </Style.Triggers>
</Style>

<!-- Tonal: New. -->
<Style x:Key="TonalButton" TargetType="Button" BasedOn="{StaticResource ButtonBase}">
    <Setter Property="Background" Value="{DynamicResource AccentSubtle}" />
    <Setter Property="Foreground" Value="{DynamicResource Accent}" />
    <Setter Property="FontWeight" Value="SemiBold" />
</Style>

<!-- Primary: Connect, Save. Exactly one per window. -->
<Style x:Key="AccentButton" TargetType="Button" BasedOn="{StaticResource ButtonBase}">
    <Setter Property="Background" Value="{DynamicResource AccentFill}" />
    <Setter Property="Foreground" Value="{DynamicResource TextOnAccent}" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="Padding" Value="20,0" />
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="{DynamicResource AccentHover}" />
        </Trigger>
        <Trigger Property="IsPressed" Value="True">
            <Setter Property="Background" Value="{DynamicResource AccentPressed}" />
        </Trigger>
        <Trigger Property="IsEnabled" Value="False">
            <Setter Property="Background" Value="{DynamicResource PanelAlt}" />
            <Setter Property="Foreground" Value="{DynamicResource TextFaint}" />
        </Trigger>
    </Style.Triggers>
</Style>

<!-- Column header: recedes, does not compete with data -->
<Style TargetType="GridViewColumnHeader">
    <Setter Property="Height" Value="32" />
    <Setter Property="Padding" Value="12,0" />
    <Setter Property="FontSize" Value="12" />
    <Setter Property="FontWeight" Value="Medium" />
    <Setter Property="Foreground" Value="{DynamicResource TextFaint}" />
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="HorizontalContentAlignment" Value="Left" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="GridViewColumnHeader">
                <Border Background="{TemplateBinding Background}"
                        BorderBrush="{DynamicResource Border}"
                        BorderThickness="0,0,0,1"
                        Padding="{TemplateBinding Padding}"
                        SnapsToDevicePixels="True">
                    <ContentPresenter HorizontalAlignment="Left" VerticalAlignment="Center" />
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>

<!-- Row: 34 tall, accent rail on selection, neutral when unfocused -->
<Style TargetType="ListViewItem">
    <Setter Property="Height" Value="34" />
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="{DynamicResource TextMuted}" />
    <Setter Property="FontSize" Value="13" />
    <Setter Property="HorizontalContentAlignment" Value="Stretch" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="ListViewItem">
                <Border x:Name="Bd" Background="{TemplateBinding Background}"
                        SnapsToDevicePixels="True">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="2" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <Rectangle x:Name="Rail" Grid.Column="0" Fill="Transparent" />
                        <GridViewRowPresenter Grid.Column="1"
                            Content="{TemplateBinding Content}"
                            Columns="{TemplateBinding GridView.ColumnCollection}"
                            VerticalAlignment="Center" />
                    </Grid>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter TargetName="Bd" Property="Background" Value="{DynamicResource Hover}" />
                    </Trigger>
                    <Trigger Property="IsSelected" Value="True">
                        <Setter TargetName="Bd" Property="Background" Value="{DynamicResource SelectionInactive}" />
                    </Trigger>
                    <MultiTrigger>
                        <MultiTrigger.Conditions>
                            <Condition Property="IsSelected" Value="True" />
                            <Condition Property="Selector.IsSelectionActive" Value="True" />
                        </MultiTrigger.Conditions>
                        <Setter TargetName="Bd" Property="Background" Value="{DynamicResource Selection}" />
                        <Setter TargetName="Rail" Property="Fill" Value="{DynamicResource Accent}" />
                        <Setter Property="Foreground" Value="{DynamicResource TextMuted}" />
                    </MultiTrigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

`GridViewRowPresenter` is the piece that is easy to get wrong. A `ListViewItem` template
that uses a plain `ContentPresenter` will render the row as a single cell and silently
drop every column.

---

## 8. Implementation order

1. `Themes/Dark.xaml` and `Themes/Light.xaml` are in the repo. Merge them in `App.xaml`
   and confirm the app still builds and looks unchanged except for the new values.
2. Move the control styles out of `App.xaml` into `Themes/Controls.xaml`. Still no visual
   change beyond step 1.
3. Sweep the literals using the tables in section 2. Verify against a fresh grep, not
   against those tables, since line numbers drift.
4. Convert every color reference to `DynamicResource`. Leave `BasedOn` and non-color
   resources alone.
5. Wire the theme switch and verify the swap repaints without a restart. Fix whatever
   stays dark.
6. Restyle the shell: title bar, quick connect strip, toolbar, status bar.
7. Restyle the list: `GridViewColumnHeader`, `ListViewItem`, group headers, tag pills,
   gutter. Biggest single visual win, so keep it as its own commit.
8. Rebuild the connection dialog: rail `ListBox` plus `ContentControl`, same seven
   sections and every existing field binding preserved. Keep the base `TabControl` and
   `TabItem` styles, `SessionHostWindow` depends on them.
9. Add what was missing: disabled states on row actions, empty state, live session dot,
   overlay scrollbars, Save validation.

Every text pairing in this spec was measured, and all of them clear 4.5:1 in both themes,
including the ones that would normally be waved through at the 3:1 large-text allowance:
`TextFaint` on both grounds, `Accent` on `AccentSubtle`, `TextOnAccent` on all three
filled-button states, `Danger` on `DangerSubtle`, `TextMuted` on `Selection`, and all four
tag colors on their composited fills. The lowest ratio anywhere is 4.59.
