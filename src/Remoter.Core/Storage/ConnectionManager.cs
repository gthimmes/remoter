using Remoter.Core.Models;
using Remoter.Core.Security;

namespace Remoter.Core.Storage;

/// <summary>
/// The single source of truth for the saved connection list and their passwords.
/// Keeps the in-memory list, the JSON file and Credential Manager in step.
/// </summary>
public sealed class ConnectionManager
{
    private readonly ProfileStore _store;
    private readonly ICredentialStore _credentials;
    private readonly List<ConnectionProfile> _profiles = new();

    public ConnectionManager(ProfileStore store, ICredentialStore credentials)
    {
        _store = store;
        _credentials = credentials;
    }

    public IReadOnlyList<ConnectionProfile> Profiles => _profiles;

    public event EventHandler? Changed;

    public string? LoadWarning { get; private set; }

    public void Load()
    {
        _profiles.Clear();
        _profiles.AddRange(_store.Load(out var warning));
        LoadWarning = warning;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Adds a new profile and optionally saves its password.</summary>
    public void Add(ConnectionProfile profile, string? password)
    {
        _profiles.Add(profile);
        ApplyPassword(profile, password);
        Persist();
    }

    /// <summary>Copies edited values into the tracked profile with the same id and saves.</summary>
    public void Update(ConnectionProfile edited, string? password)
    {
        var existing = _profiles.FirstOrDefault(p => p.Id == edited.Id);
        if (existing is null)
        {
            Add(edited, password);
            return;
        }
        existing.CopyFrom(edited);
        ApplyPassword(existing, password);
        Persist();
    }

    public void Remove(ConnectionProfile profile)
    {
        _profiles.RemoveAll(p => p.Id == profile.Id);
        TryDeleteCredential(profile.CredentialTarget);
        Persist();
    }

    public void MarkConnected(ConnectionProfile profile)
    {
        var existing = _profiles.FirstOrDefault(p => p.Id == profile.Id);
        if (existing is null)
            return;
        existing.LastConnected = DateTimeOffset.Now;
        Persist();
    }

    public string? GetPassword(ConnectionProfile profile) =>
        profile.SavePassword ? _credentials.Read(profile.CredentialTarget)?.Secret : null;

    private void ApplyPassword(ConnectionProfile profile, string? password)
    {
        if (profile.SavePassword && !string.IsNullOrEmpty(password))
            _credentials.Save(profile.CredentialTarget, profile.QualifiedUsername, password);
        else if (!profile.SavePassword)
            TryDeleteCredential(profile.CredentialTarget);
    }

    private void TryDeleteCredential(string target)
    {
        try { _credentials.Delete(target); }
        catch { /* a missing or locked credential must not block editing the list */ }
    }

    private void Persist()
    {
        _store.Save(_profiles);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
