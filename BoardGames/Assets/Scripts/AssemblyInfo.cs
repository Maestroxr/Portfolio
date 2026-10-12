using System.Runtime.CompilerServices;

// The editor tooling (Assets/Editor) builds the assets and the scene and wires the internal fields directly; the edit
// mode tests set games up through them.
[assembly: InternalsVisibleTo("Skinnerboxes.Checkers.Editor")]
[assembly: InternalsVisibleTo("Skinnerboxes.Checkers.Tests")]
