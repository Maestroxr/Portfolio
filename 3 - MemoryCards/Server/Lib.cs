using SpacetimeDB;

/// <summary>
/// The server module of Memory Cards: the Gamebox base server (users and login, connecting and disconnecting,
/// the player profile; see BaseServer/Lib.cs in this project) plus what this file adds. Both files declare the
/// same partial class, so the tables, reducers and helpers of the base server are in reach here:
/// <c>ctx.Db.User</c>, <c>RequireUser(ctx)</c>, <c>FindUser</c>, <c>IsOnline</c>.
///
/// After a change: publish the module, then generate the client bindings (Gamebox > Server in the Unity editor,
/// or <c>spacetime publish</c> and <c>spacetime generate</c> in the project folder, which read spacetime.json).
/// </summary>
public static partial class Module
{
    // Tables, reducers and views of the game. Keep what belongs to a player in a table keyed by the identity:
    //
    //     [SpacetimeDB.Table(Accessor = "HighScore", Public = true)]
    //     public partial struct HighScore
    //     {
    //         [SpacetimeDB.PrimaryKey]
    //         public Identity Player;
    //         public long Best;
    //     }
    //
    //     [SpacetimeDB.Reducer]
    //     public static void SubmitScore(ReducerContext ctx, long score)
    //     {
    //         var user = RequireUser(ctx);
    //         if (ctx.Db.HighScore.Player.Find(user.Identity) is not { } row)
    //         {
    //             ctx.Db.HighScore.Insert(new HighScore { Player = user.Identity, Best = score });
    //         }
    //         else if (score > row.Best)
    //         {
    //             row.Best = score;
    //             ctx.Db.HighScore.Player.Update(row);
    //         }
    //     }

    // The lifecycle reducers exist once per module, in the base server. React to them by implementing the
    // extension points you need; the others cost nothing:
    //
    //     static partial void OnInit(ReducerContext ctx)
    //     static partial void OnUserCreated(ReducerContext ctx, User user)
    //     static partial void OnUserConnected(ReducerContext ctx, User user, bool cameOnline)
    //     static partial void OnUserDisconnected(ReducerContext ctx, User user, bool wentOffline)
    //     static partial void OnUserDeleted(ReducerContext ctx, User user)
    //     static partial void ValidateUserName(ReducerContext ctx, string name, ref string? error)
}
