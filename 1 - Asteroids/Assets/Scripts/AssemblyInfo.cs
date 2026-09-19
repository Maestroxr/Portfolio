using System.Runtime.CompilerServices;

// The editor tooling (Assets/Editor) builds the prefabs, levels and scene and wires the internal fields directly; the
// tests drive the same internals.
[assembly: InternalsVisibleTo("Skinnerboxes.Asteroids.Editor")]
[assembly: InternalsVisibleTo("Skinnerboxes.Asteroids.Tests.Editor")]
[assembly: InternalsVisibleTo("Skinnerboxes.Asteroids.Tests.Runtime")]
