using System;

namespace Portfolio.Checkers
{
    /// <summary>
    /// A game as it is written to a named save (and the autosave): the table it was set up with, the position with its
    /// history counters, and the campaign level it belongs to (-1 for a quick game, a two-player game or an online one).
    /// Serialized to YAML by the base <c>SavedGame</c>; the same shape travels to the server for online saved games.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public TableSetup setup;
        public CheckersGame game;
        public int campaignLevel = -1;
    }
}
