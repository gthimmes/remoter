using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using Remoter.App.Theming;
using Remoter.Core.Models;
using Remoter.Core.Rdp;
using Remoter.Core.Storage;

namespace Remoter.App;

public partial class MainWindow : Window
{
    private readonly ConnectionManager _connections = App.Current.Connections;
    private readonly ThemeManager _theme = App.Current.Theme;
    private readonly ObservableCollection<ConnectionProfile> _items = new();
    private readonly ObservableCollection<RecentConnection> _recentItems = new();

    private readonly ICollectionView _view;

    public MainWindow()
    {
        InitializeComponent();

        _view = CollectionViewSource.GetDefaultView(_items);
        _view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ConnectionProfile.GroupKey)));
        _view.SortDescriptions.Add(new SortDescription(nameof(ConnectionProfile.GroupSort), ListSortDirection.Ascending));
        _view.SortDescriptions.Add(new SortDescription(nameof(ConnectionProfile.DisplayName), ListSortDirection.Ascending));
        _view.Filter = o => o is ConnectionProfile p && ConnectionQuery.Matches(p, SearchBox.Text);
        List.ItemsSource = _view;
        RecentsList.ItemsSource = _recentItems;

        _connections.Changed += (_, _) => Refresh();
        _connections.RecentsChanged += (_, _) => RefreshRecents();
        Refresh();
        RefreshRecents();

        if (_connections.LoadWarning is { } warning)
            Status.Text = warning;

        _theme.Changed += (_, _) => RefreshThemeMenu();
        RefreshThemeMenu();
    }

    private ConnectionProfile? Selected => List.SelectedItem as ConnectionProfile;

    // ----- Appearance -----

    private void ThemeButton_Click(object sender, RoutedEventArgs e) => ThemePopup.IsOpen = !ThemePopup.IsOpen;

    private void ThemeChoice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string tag } && Enum.TryParse<AppTheme>(tag, out var mode))
            _theme.SetMode(mode);
        ThemePopup.IsOpen = false;
    }

    private void RefreshThemeMenu()
    {
        // Hidden rather than collapsed, so the labels stay lined up.
        TickSystem.Visibility = _theme.Mode == AppTheme.System ? Visibility.Visible : Visibility.Hidden;
        TickDark.Visibility = _theme.Mode == AppTheme.Dark ? Visibility.Visible : Visibility.Hidden;
        TickLight.Visibility = _theme.Mode == AppTheme.Light ? Visibility.Visible : Visibility.Hidden;
        SystemHint.Text = _theme.Mode == AppTheme.System ? $"({_theme.Resolved.ToLowerInvariant()})" : "";
    }

    private void Refresh()
    {
        var previous = Selected?.Id;
        _items.Clear();
        foreach (var p in _connections.Profiles)
            _items.Add(p);
        if (previous is { } id)
            List.SelectedItem = _items.FirstOrDefault(p => p.Id == id);
        Status.Text = _items.Count == 0
            ? "No saved connections yet. Use New, or type a computer name above."
            : $"{_items.Count} saved connection{(_items.Count == 1 ? "" : "s")}.";
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        SearchHint.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        _view.Refresh();
    }

    private void RefreshRecents()
    {
        _recentItems.Clear();
        foreach (var r in _connections.Recents)
            _recentItems.Add(r);
        RecentsEmpty.Visibility = _recentItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RecentsList.Visibility = _recentItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    // ----- Recents dropdown -----

    private void RecentsButton_Click(object sender, RoutedEventArgs e)
    {
        RecentsPopup.IsOpen = !RecentsPopup.IsOpen;
    }

    private void RecentsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RecentsList.SelectedItem is RecentConnection r)
        {
            RecentsPopup.IsOpen = false;
            ConnectToRecent(r);
        }
    }

    private void ClearRecents_Click(object sender, RoutedEventArgs e)
    {
        _connections.ClearRecents();
        RecentsPopup.IsOpen = false;
    }

    private void ConnectToRecent(RecentConnection r)
    {
        var profile = new ConnectionProfile
        {
            Host = r.Host,
            Port = r.Port,
            Username = r.Username,
            Domain = r.Domain,
            Name = r.Host,
        };
        StartSession(profile, tracked: null);
    }

    // ----- Quick connect -----

    private void QuickHost_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            QuickConnect_Click(sender, e);
    }

    private void QuickConnect_Click(object sender, RoutedEventArgs e)
    {
        var text = QuickHost.Text.Trim();
        if (text.Length == 0)
            return;

        var (host, port) = RdpFile.SplitHostPort(text);
        var profile = new ConnectionProfile { Host = host, Name = host };
        if (port is not null)
            profile.Port = port.Value;

        // Prefill from a recent entry for the same host, so we can reuse the last username.
        var recent = _connections.Recents.FirstOrDefault(r =>
            string.Equals(r.Host, host, StringComparison.OrdinalIgnoreCase) && r.Port == profile.Port);
        if (recent is not null)
        {
            profile.Username = recent.Username;
            profile.Domain = recent.Domain;
        }

        StartSession(profile, tracked: null);
    }

    // ----- Toolbar -----

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var profile = new ConnectionProfile();
        if (EditConnectionWindow.ShowDialog(this, profile, out var password))
        {
            _connections.Add(profile, password);
            List.SelectedItem = _items.FirstOrDefault(p => p.Id == profile.Id);
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null)
            return;
        var clone = Selected.Clone();
        var existingPassword = _connections.GetPassword(Selected);
        if (EditConnectionWindow.ShowDialog(this, clone, out var password, existingPassword))
            _connections.Update(clone, password);
    }

    private void Duplicate_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null)
            return;
        var copy = Selected.Clone();
        copy.Id = Guid.NewGuid();
        copy.Name = Selected.DisplayName + " (copy)";
        copy.SavePassword = false;
        copy.LastConnected = null;
        _connections.Add(copy, null);
        List.SelectedItem = _items.FirstOrDefault(p => p.Id == copy.Id);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null)
            return;
        var result = MessageBox.Show(
            $"Delete \"{Selected.DisplayName}\"? Any saved password will be removed too.",
            "Remoter", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
            _connections.Remove(Selected);
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import a Remote Desktop file",
            Filter = "Remote Desktop files (*.rdp)|*.rdp|All files (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var profile = RdpFile.Parse(File.ReadAllText(dialog.FileName));
            if (string.IsNullOrWhiteSpace(profile.Name))
                profile.Name = Path.GetFileNameWithoutExtension(dialog.FileName);
            if (EditConnectionWindow.ShowDialog(this, profile, out var password))
                _connections.Add(profile, password);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not import that file:\n\n" + ex.Message, "Remoter",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null)
            return;
        var dialog = new SaveFileDialog
        {
            Title = "Export to a Remote Desktop file",
            Filter = "Remote Desktop files (*.rdp)|*.rdp",
            FileName = SafeFileName(Selected.DisplayName) + ".rdp",
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            File.WriteAllText(dialog.FileName, RdpFile.Write(Selected));
            Status.Text = "Exported to " + dialog.FileName + " (the password is not written to the file).";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not export that file:\n\n" + ex.Message, "Remoter",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ----- Connect -----

    private void List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Selected is not null)
            StartSession(Selected.Clone(), tracked: Selected);
    }

    /// <summary>
    /// Opens a session. <paramref name="tracked"/> is the saved profile this came from (null for
    /// quick/recent connections), used to stamp its last-connected time on success.
    /// </summary>
    private void StartSession(ConnectionProfile profile, ConnectionProfile? tracked)
    {
        var password = tracked is not null && tracked.SavePassword ? _connections.GetPassword(tracked) : null;

        var needPrompt = string.IsNullOrEmpty(profile.Username)
            || (profile.SavePassword && password is null)
            || tracked is null
            || !profile.SavePassword;

        if (needPrompt)
        {
            if (!CredentialPromptWindow.TryGetCredentials(this, profile, ref password))
                return;

            // If the user chose to save on a tracked profile, persist the choice and secret.
            if (tracked is not null && profile.SavePassword && !string.IsNullOrEmpty(password))
            {
                tracked.Username = profile.Username;
                tracked.Domain = profile.Domain;
                tracked.SavePassword = true;
                _connections.Update(tracked, password);
            }
        }

        // Sessions dock as tabs in the host window you used last; hold Shift to get a new window.
        var newWindow = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        var view = SessionHostWindow.OpenSession(profile, password, newWindow);
        view.FirstConnected += (_, _) =>
        {
            _connections.RecordRecent(profile.Host, profile.Port, profile.Username, profile.Domain);
            if (tracked is not null)
                _connections.MarkConnected(tracked);
        };
    }

    private static string SafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
