using System.Windows;
using Remoter.Core.Models;

namespace Remoter.App;

public partial class CredentialPromptWindow : Window
{
    private CredentialPromptWindow(ConnectionProfile profile, string? password)
    {
        InitializeComponent();
        Prompt.Text = $"Enter your credentials for {profile.Host}.";
        UserBox.Text = profile.QualifiedUsername;
        PasswordBox.Password = password ?? "";
        SaveBox.IsChecked = profile.SavePassword;

        Loaded += (_, _) =>
        {
            if (string.IsNullOrEmpty(UserBox.Text))
                UserBox.Focus();
            else
                PasswordBox.Focus();
        };

        // Borderless window: let the user drag it by its body.
        MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        };
    }

    /// <summary>
    /// Prompts for credentials, updating <paramref name="profile"/>'s username, domain and SavePassword
    /// and returning the entered password via <paramref name="password"/>. Returns false if cancelled.
    /// </summary>
    public static bool TryGetCredentials(Window owner, ConnectionProfile profile, ref string? password)
    {
        var window = new CredentialPromptWindow(profile, password) { Owner = owner };
        if (window.ShowDialog() != true)
            return false;

        var entered = window.UserBox.Text.Trim();
        SplitUser(entered, out var domain, out var user);
        profile.Username = user;
        profile.Domain = domain;
        profile.SavePassword = window.SaveBox.IsChecked == true;
        password = window.PasswordBox.Password;
        return true;
    }

    private static void SplitUser(string entered, out string? domain, out string user)
    {
        domain = null;
        user = entered;
        var slash = entered.IndexOf('\\');
        if (slash > 0)
        {
            domain = entered[..slash];
            user = entered[(slash + 1)..];
            return;
        }
        var at = entered.IndexOf('@');
        if (at > 0)
        {
            // user@domain: keep as UPN in the username, leave domain empty.
            user = entered;
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UserBox.Text))
        {
            MessageBox.Show(this, "Please enter a user name.", "Remoter", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
