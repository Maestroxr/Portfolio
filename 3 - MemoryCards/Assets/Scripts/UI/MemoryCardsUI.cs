using Gamebox;
using Gamebox.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Menu of the Memory Cards module. Identical to the shared menu except that after a victory or a game over
    /// the menu only comes back once the cards finished their animation (the manager shows it).
    /// </summary>
    public class MemoryCardsUI : GameUI
    {
        public override void UpdateGameState(GameState state)
        {
            switch (state.BaseState)
            {
                case BaseGameState.GameOver:
                case BaseGameState.Victory:
                    SetInteractable(ReturnToGame, false);
                    SetInteractable(SaveGame, false);
                    break;
                default:
                    base.UpdateGameState(state);
                    break;
            }
        }
    }
}
