internal class Group (string Id, string Name, int Start, int End);
internal class Section (string Id, string Name, Group[] Groups);
internal class Game (string Id, string Name, Section[] Sections);