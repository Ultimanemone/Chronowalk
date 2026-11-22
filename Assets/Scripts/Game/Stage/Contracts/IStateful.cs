namespace ChronowalkGame.Game.Stage.Contracts
{
    public interface IStateful
    {
        // Return an independent snapshot so later mutations do not change the captured state.
        object CaptureRoomState();
        void RestoreRoomState(object state);
    }
}
