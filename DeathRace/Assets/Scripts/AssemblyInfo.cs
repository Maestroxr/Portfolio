using System.Runtime.CompilerServices;

// The editor tooling (Assets/Editor) builds the assets and the scene and wires the internal fields directly; the edit
// mode tests set races and careers up through them.
[assembly: InternalsVisibleTo("Skinnerboxes.DeathRace.Editor")]
[assembly: InternalsVisibleTo("Skinnerboxes.DeathRace.Tests")]
