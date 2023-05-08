using System;
using System.Text.Json.Serialization;

namespace BaseBuilding.Scripts.Util.objects;

public readonly struct Eid : IEquatable<Eid>, IComparable<Eid>
{
    public ulong Id { get; init; }

    public Eid() => Id = 0UL;

    public Eid(ulong id) => Id = id;

    [JsonIgnore] public bool IsValid => Id > 0UL;

    public static bool operator ==(Eid left, Eid right) => left.Equals(right);

    public static bool operator !=(Eid left, Eid right) => !(left == right);

    public bool Equals(Eid other) => Id == other.Id;

    public override bool Equals(object? obj) => obj is Eid other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id);

    public int CompareTo(Eid other) => Id.CompareTo(other.Id);
}