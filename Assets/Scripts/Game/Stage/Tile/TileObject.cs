using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tile
{
    public abstract class TileObject : ScriptableObject
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private GameObject prefab;

        public abstract TileType TileType { get; }
        public Sprite Sprite => sprite;
        public GameObject Prefab => prefab;
    }
}