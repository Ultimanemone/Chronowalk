namespace ChronowalkGame.Game.Stage.Contracts
{
    public interface IRoomTurnSystem
    {
        // Called once after all ghost and player actions for this turn have been applied.
        void ResolveTurn(int turnNumber);
    }
}
