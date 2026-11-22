using ChronowalkGame.Game.Stage.Data;

namespace ChronowalkGame.Game.Stage.Tiles.Definitions
{
    public abstract class TileObjectDefinition : StageElement
    {
        public abstract TileType TileType { get; }
        public virtual bool AllowsActors => true;
        public virtual bool AllowsObjects => true;
        public virtual bool BlocksLaser => false;
    }
}