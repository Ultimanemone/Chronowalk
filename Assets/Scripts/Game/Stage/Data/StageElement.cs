using UnityEngine;

namespace ChronowalkGame.Game.Stage.Data
{
    public abstract class StageElement : ScriptableObject
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private GameObject prefab;

        public Sprite Sprite => sprite;
        public GameObject Prefab => prefab;
    }
}
