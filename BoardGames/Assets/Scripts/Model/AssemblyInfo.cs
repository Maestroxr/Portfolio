using System.Runtime.CompilerServices;

// The rules of Checkers know nothing of Unity: this assembly references only Gamebox.Lockstep, so the same files are
// compiled into the server module (Server/StdbModule.csproj) and into the model tests (Tests/) outside Unity.
[assembly: InternalsVisibleTo("Skinnerboxes.BoardGames")]
[assembly: InternalsVisibleTo("Skinnerboxes.BoardGames.Editor")]
[assembly: InternalsVisibleTo("Skinnerboxes.BoardGames.Tests")]
