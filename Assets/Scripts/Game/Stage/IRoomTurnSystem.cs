namespace ChronowalkGame.Game.Stage
{
    public interface IRoomTurnSystem
    {
        // Called once after all ghost and player actions for this turn have been applied.
        void ResolveTurn(int turnNumber);
    }
}
