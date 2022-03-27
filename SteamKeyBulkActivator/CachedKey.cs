using SteamKit2;

namespace SteamKeyBulkActivator;

public class CachedKey : IEquatable<CachedKey>
{
    public string Key { get; set; }
    public EPurchaseResultDetail LastResultDetails { get; set; }
    public DateTime LastResultDateTime { get; set; }

    public bool Equals(CachedKey? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Key == other.Key;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((CachedKey) obj);
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}