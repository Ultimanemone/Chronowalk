namespace ChronowalkGame.Game.Stage.Contracts
{
    public interface IRuntimeInitializable<in TInitializationData>
    {
        void Initialize(TInitializationData initializationData);
    }
}
