using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Remoter.Core.Models;
using Remoter.Core.Rdp;
using Remoter.Core.Storage;

namespace Remoter.App;

public partial class MainWindow : Window
{
    private readonly ConnectionManager _connections = App.Current.Connections;
    private readonly ObservableCollection<ConnectionProfile> _items = new();

    public MainWindow()
    {
        InitializeComponent();
        List.ItemsSource = _items;
        _connections.Changed += (_, _) => Refresh();
        Refresh();

        if (_connections.LoadWarning is { } warning)
            Status.Text = warning;
    }

    private ConnectionProfile? Selected => List.SelectedItem as ConnectionProfile;

    private void Refresh()
    {
        var previous = Selected?.Id;
        _items.Clear();
        foreach (var p in _connections.Profiles.OrderBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            _items.Add(p);
        if (previous is { } id)
            List.SelectedItem = _items.FirstOrDefault(p => p.Id == id);
        Status.Text = _items.Count == 0
            ? "No saved connections yet. Use New, or type a computer name above."
            : $"{_items.Count} saved connection{(_items.Count == 1 ? "" : "s")}.";
    }

    private void List_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
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

        // Quick connections are not saved unless the user chooses to later.
        StartSession(profile, saveOnConnect: false);
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
        copy.SavePassword = false; // don't silently copy a stored secret
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
            StartSession(Selected.Clone(), saveOnConnect: true);
    }

    private void StartSession(ConnectionProfile profile, bool saveOnConnect)
    {
        var password = saveOnConnect && profile.Id != Guid.Empty ? _connections.GetPassword(profile) : null;

        // If no username or (saved) password, ask before we open the session window.
        if (string.IsNullOrEmpty(profile.Username) || (profile.SavePassword && password is null) || !profile.SavePassword)
        {
            if (!CredentialPromptWindow.TryGetCredentials(this, profile, ref password))
                return;
        }

        if (saveOnConnect)
        {
            var known = _connections.Profiles.FirstOrDefault(p => p.Id == profile.Id);
            if (known is not null)
            {
                _connections.MarkConnected(known);
                if (profile.SavePassword && !string.IsNullOrEmpty(password))
                    _connections.Update(profile, password);
            }
        }

        var window = new SessionWindow(profile, password);
        window.Show();
    }

    private static string SafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
