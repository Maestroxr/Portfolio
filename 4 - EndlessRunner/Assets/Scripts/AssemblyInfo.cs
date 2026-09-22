using System.Runtime.CompilerServices;

// The editor tooling (Assets/Editor) builds the prefabs, levels and scene and wires the internal fields directly; the
// edit mode tests lay out tracks with the internal layout builder.
[assembly: InternalsVisibleTo("Skinnerboxes.EndlessRunner.Editor")]
[assembly: InternalsVisibleTo("Skinnerboxes.EndlessRunner.Tests")]
