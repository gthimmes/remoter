namespace Remoter.Core.Security;

public sealed record StoredCredential(string Username, string Secret);

/// <summary>Secret storage abstraction. The production implementation is Windows Credential Manager.</summary>
public interface ICredentialStore
{
    void Save(string target, string username, string secret);

    StoredCredential? Read(string target);

    void Delete(string target);
}

/// <summary>Non-persistent store for tests.</summary>
public sealed class InMemoryCredentialStore : ICredentialStore
{
    private readonly Dictionary<string, StoredCredential> _items = new(StringComparer.OrdinalIgnoreCase);

    public void Save(string target, string username, string secret) => _items[target] = new StoredCredential(username, secret);

    public StoredCredential? Read(string target) => _items.TryGetValue(target, out var c) ? c : null;

    public void Delete(string target) => _items.Remove(target);
}
