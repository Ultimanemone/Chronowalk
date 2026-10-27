namespace ChronowalkGame.Game.Stage
{
    public interface IRoomStateful
    {
        // Return an independent snapshot so later mutations do not change the captured state.
        object CaptureRoomState();
        void RestoreRoomState(object state);
    }
}
