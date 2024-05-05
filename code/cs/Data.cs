using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum GroupCounting
{
    Continuous,
    Reset,
    Reset2
}

internal record class IdNameRecord(string Id, string Name);
internal record class Group(string Id, string Name, int Start, int End) : IdNameRecord(Id, Name);
internal record class Pack(string Id, string Name, GroupCounting Counting, string[] Groups): IdNameRecord(Id, Name);
internal record class Section(string Id, string Name, Pack[] Packs) : IdNameRecord(Id, Name);
internal record class Game(string Id, string Name, Section[] Sections) : IdNameRecord(Id, Name);
