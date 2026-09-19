using System.Runtime.CompilerServices;

// The editor tooling (Assets/Editor) builds the prefabs, levels and scene and wires the internal fields directly; the
// edit mode tests set up campaigns and rounds through them.
[assembly: InternalsVisibleTo("Skinnerboxes.MemoryCards.Editor")]
[assembly: InternalsVisibleTo("Skinnerboxes.MemoryCards.Tests")]
