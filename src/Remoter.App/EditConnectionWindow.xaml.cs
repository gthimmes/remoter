using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Remoter.Core.Models;

namespace Remoter.App;

public partial class EditConnectionWindow : Window
{
    private readonly ConnectionProfile _profile;

    private EditConnectionWindow(ConnectionProfile profile, string? existingPassword)
    {
        InitializeComponent();
        _profile = profile;
        LoadFrom(profile, existingPassword);
        SizeModeBox.SelectionChanged += (_, _) => UpdateSizePanel();
        NameBox.TextChanged += (_, _) => UpdateSaveEnabled();
        HostBox.TextChanged += (_, _) => UpdateSaveEnabled();
        UpdateSaveEnabled();
        ShowSection(0);
    }

    /// <summary>Save stays off until the two fields a connection cannot do without are filled in.</summary>
    private void UpdateSaveEnabled()
    {
        var ready = !string.IsNullOrWhiteSpace(NameBox.Text) && !string.IsNullOrWhiteSpace(HostBox.Text);
        SaveButton.IsEnabled = ready;
        SaveButton.ToolTip = ready ? null : "Enter a name and a computer to save.";
    }

    /// <summary>
    /// Every section is built up front and shown one at a time. Swapping them through a
    /// ContentControl would take the hidden ones out of the tree and with them the field names
    /// the rest of this class works through.
    /// </summary>
    private void ShowSection(int index)
    {
        var sections = new[]
        {
            GeneralPanel, OrganizePanel, DisplayPanel, LocalResourcesPanel,
            ExperiencePanel, SecurityPanel, GatewayPanel,
        };
        for (var i = 0; i < sections.Length; i++)
            sections[i].Visibility = i == index ? Visibility.Visible : Visibility.Collapsed;
        ContentScroller.ScrollToTop();
    }

    private void Rail_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded || GeneralPanel is not null)
            ShowSection(Rail.SelectedIndex);
    }

    /// <summary>
    /// Opens the editor for <paramref name="profile"/> (edited in place). Returns true when saved,
    /// with the entered password in <paramref name="password"/> (null if none / not saving).
    /// </summary>
    public static bool ShowDialog(Window owner, ConnectionProfile profile, out string? password, string? existingPassword = null)
    {
        var window = new EditConnectionWindow(profile, existingPassword) { Owner = owner };
        var ok = window.ShowDialog() == true;
        password = ok && window.SavePasswordBox.IsChecked == true ? window.PasswordBox.Password : null;
        return ok;
    }

    private void LoadFrom(ConnectionProfile p, string? existingPassword)
    {
        // A saved connection with no name showed its computer in the list. Now that Save needs a
        // name, start from that same text so editing an old connection is not blocked on it.
        NameBox.Text = string.IsNullOrWhiteSpace(p.Name) ? p.Host : p.Name;
        HostBox.Text = p.Host;
        PortBox.Text = p.Port.ToString(CultureInfo.InvariantCulture);
        UserBox.Text = p.QualifiedUsername;
        PasswordBox.Password = existingPassword ?? "";
        SavePasswordBox.IsChecked = p.SavePassword;

        FolderBox.Text = p.Folder;
        FavoriteBox.IsChecked = p.Favorite;
        TagsBox.Text = p.TagList;
        NotesBox.Text = p.Notes;

        SizeModeBox.SelectedIndex = (int)p.Display.SizeMode;
        WidthBox.Text = p.Display.Width.ToString(CultureInfo.InvariantCulture);
        HeightBox.Text = p.Display.Height.ToString(CultureInfo.InvariantCulture);
        ColorDepthBox.SelectedIndex = p.Display.ColorDepth switch { 15 => 0, 16 => 1, 24 => 2, _ => 3 };
        ScaleBox.Text = p.Display.DesktopScalePercent?.ToString(CultureInfo.InvariantCulture) ?? "";
        FullScreenBox.IsChecked = p.Display.FullScreen;
        MultiMonBox.IsChecked = p.Display.UseAllMonitors;
        ConnectionBarBox.IsChecked = p.Display.ShowConnectionBar;
        PinBarBox.IsChecked = p.Display.PinConnectionBar;
        UpdateSizePanel();

        AudioBox.SelectedIndex = (int)p.LocalResources.AudioPlayback;
        AudioCaptureBox.IsChecked = p.LocalResources.AudioCapture;
        KeyboardBox.SelectedIndex = (int)p.LocalResources.Keyboard;
        ClipboardBox.IsChecked = p.LocalResources.Clipboard;
        PrintersBox.IsChecked = p.LocalResources.Printers;
        SmartCardBox.IsChecked = p.LocalResources.SmartCards;
        PortsBox.IsChecked = p.LocalResources.Ports;
        DevicesBox.IsChecked = p.LocalResources.Devices;
        AllDrivesBox.IsChecked = p.LocalResources.AllDrives;
        DynamicDrivesBox.IsChecked = p.LocalResources.DynamicDrives;

        SelectByTag(SpeedBox, ((int)p.Experience.Speed).ToString());
        WallpaperBox.IsChecked = p.Experience.DesktopBackground;
        FontSmoothingBox.IsChecked = p.Experience.FontSmoothing;
        CompositionBox.IsChecked = p.Experience.DesktopComposition;
        WindowDragBox.IsChecked = p.Experience.ShowWindowContentsWhileDragging;
        AnimationBox.IsChecked = p.Experience.MenuAndWindowAnimation;
        ThemesBox.IsChecked = p.Experience.VisualStyles;
        BitmapCacheBox.IsChecked = p.Experience.PersistentBitmapCaching;
        AutoReconnectBox.IsChecked = p.Experience.AutoReconnect;

        SelectByTag(ServerAuthBox, ((int)p.Security.ServerAuthentication).ToString());
        NlaBox.IsChecked = p.Security.NetworkLevelAuthentication;
        RestrictedAdminBox.IsChecked = p.Security.RestrictedAdmin;
        CredGuardBox.IsChecked = p.Security.RemoteCredentialGuard;
        AdminSessionBox.IsChecked = p.Security.AdministrativeSession;

        GatewayUsageBox.SelectedIndex = (int)p.Gateway.Usage;
        GatewayHostBox.Text = p.Gateway.Hostname ?? "";
        GatewaySameCredsBox.IsChecked = p.Gateway.UseSameCredentials;
        UpdateGatewayPanel();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var p = _profile;
        p.Name = NameBox.Text.Trim();
        p.Host = HostBox.Text.Trim();
        p.Port = int.TryParse(PortBox.Text.Trim(), out var port) ? port : 3389;

        var user = UserBox.Text.Trim();
        var slash = user.IndexOf('\\');
        if (slash > 0) { p.Domain = user[..slash]; p.Username = user[(slash + 1)..]; }
        else { p.Domain = null; p.Username = string.IsNullOrEmpty(user) ? null : user; }
        p.SavePassword = SavePasswordBox.IsChecked == true;

        p.Folder = ConnectionQuery.NormalizeFolder(FolderBox.Text);
        p.Favorite = FavoriteBox.IsChecked == true;
        p.Tags = TagsBox.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        p.Notes = NotesBox.Text.Trim();

        p.Display.SizeMode = (SizeMode)Math.Max(0, SizeModeBox.SelectedIndex);
        p.Display.Width = ParseInt(WidthBox.Text, p.Display.Width);
        p.Display.Height = ParseInt(HeightBox.Text, p.Display.Height);
        p.Display.ColorDepth = int.Parse((string)((ComboBoxItem)ColorDepthBox.SelectedItem).Tag);
        p.Display.DesktopScalePercent = int.TryParse(ScaleBox.Text.Trim(), out var scale) ? Math.Clamp(scale, 100, 500) : null;
        p.Display.FullScreen = FullScreenBox.IsChecked == true;
        p.Display.UseAllMonitors = MultiMonBox.IsChecked == true;
        p.Display.ShowConnectionBar = ConnectionBarBox.IsChecked == true;
        p.Display.PinConnectionBar = PinBarBox.IsChecked == true;

        p.LocalResources.AudioPlayback = (AudioPlaybackMode)Math.Max(0, AudioBox.SelectedIndex);
        p.LocalResources.AudioCapture = AudioCaptureBox.IsChecked == true;
        p.LocalResources.Keyboard = (KeyboardMode)Math.Max(0, KeyboardBox.SelectedIndex);
        p.LocalResources.Clipboard = ClipboardBox.IsChecked == true;
        p.LocalResources.Printers = PrintersBox.IsChecked == true;
        p.LocalResources.SmartCards = SmartCardBox.IsChecked == true;
        p.LocalResources.Ports = PortsBox.IsChecked == true;
        p.LocalResources.Devices = DevicesBox.IsChecked == true;
        p.LocalResources.AllDrives = AllDrivesBox.IsChecked == true;
        p.LocalResources.DynamicDrives = DynamicDrivesBox.IsChecked == true;

        p.Experience.Speed = (ConnectionSpeed)TagInt(SpeedBox, (int)ConnectionSpeed.AutoDetect);
        p.Experience.DesktopBackground = WallpaperBox.IsChecked == true;
        p.Experience.FontSmoothing = FontSmoothingBox.IsChecked == true;
        p.Experience.DesktopComposition = CompositionBox.IsChecked == true;
        p.Experience.ShowWindowContentsWhileDragging = WindowDragBox.IsChecked == true;
        p.Experience.MenuAndWindowAnimation = AnimationBox.IsChecked == true;
        p.Experience.VisualStyles = ThemesBox.IsChecked == true;
        p.Experience.PersistentBitmapCaching = BitmapCacheBox.IsChecked == true;
        p.Experience.AutoReconnect = AutoReconnectBox.IsChecked == true;

        p.Security.ServerAuthentication = (ServerAuthenticationPolicy)TagInt(ServerAuthBox, (int)ServerAuthenticationPolicy.Warn);
        p.Security.NetworkLevelAuthentication = NlaBox.IsChecked == true;
        p.Security.RestrictedAdmin = RestrictedAdminBox.IsChecked == true;
        p.Security.RemoteCredentialGuard = CredGuardBox.IsChecked == true;
        p.Security.AdministrativeSession = AdminSessionBox.IsChecked == true;

        p.Gateway.Usage = (GatewayUsage)Math.Max(0, GatewayUsageBox.SelectedIndex);
        p.Gateway.Hostname = string.IsNullOrWhiteSpace(GatewayHostBox.Text) ? null : GatewayHostBox.Text.Trim();
        p.Gateway.UseSameCredentials = GatewaySameCredsBox.IsChecked == true;

        var problems = p.Validate();
        if (problems.Count > 0)
        {
            MessageBox.Show(this, string.Join("\n", problems), "Please fix these settings",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void UpdateSizePanel()
    {
        var fixedSize = SizeModeBox.SelectedIndex != (int)SizeMode.FitToWindow;
        if (FixedSizePanel != null)
            FixedSizePanel.IsEnabled = fixedSize;
    }

    private void UpdateGatewayPanel()
    {
        if (GatewayDetails != null)
            GatewayDetails.IsEnabled = GatewayUsageBox.SelectedIndex != (int)GatewayUsage.Never;
    }

    private void GatewayUsageBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateGatewayPanel();

    private void SpeedBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Apply the classic mstsc experience defaults for the chosen speed, so the checkboxes
        // start somewhere sensible. The user can still override any of them.
        if (!IsLoaded)
            return;
        var speed = (ConnectionSpeed)TagInt(SpeedBox, (int)ConnectionSpeed.AutoDetect);
        var preset = new ExperienceSettings();
        switch (speed)
        {
            case ConnectionSpeed.Modem:
            case ConnectionSpeed.LowSpeedBroadband:
                preset = new ExperienceSettings
                {
                    DesktopBackground = false, FontSmoothing = false, DesktopComposition = false,
                    ShowWindowContentsWhileDragging = false, MenuAndWindowAnimation = false, VisualStyles = true,
                };
                break;
            case ConnectionSpeed.Satellite:
            case ConnectionSpeed.HighSpeedBroadband:
            case ConnectionSpeed.Wan:
                preset = new ExperienceSettings
                {
                    DesktopBackground = false, ShowWindowContentsWhileDragging = false, MenuAndWindowAnimation = false,
                };
                break;
        }
        WallpaperBox.IsChecked = preset.DesktopBackground;
        FontSmoothingBox.IsChecked = preset.FontSmoothing;
        CompositionBox.IsChecked = preset.DesktopComposition;
        WindowDragBox.IsChecked = preset.ShowWindowContentsWhileDragging;
        AnimationBox.IsChecked = preset.MenuAndWindowAnimation;
        ThemesBox.IsChecked = preset.VisualStyles;
    }

    private static int ParseInt(string text, int fallback) =>
        int.TryParse(text.Trim(), out var value) ? value : fallback;

    private static void SelectByTag(ComboBox box, string tag)
    {
        foreach (ComboBoxItem item in box.Items)
        {
            if ((string)item.Tag == tag)
            {
                box.SelectedItem = item;
                return;
            }
        }
        box.SelectedIndex = 0;
    }

    private static int TagInt(ComboBox box, int fallback) =>
        box.SelectedItem is ComboBoxItem { Tag: string tag } && int.TryParse(tag, out var value) ? value : fallback;
}
