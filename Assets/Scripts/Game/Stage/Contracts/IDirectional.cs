using ChronowalkGame.Game.Data;
using UnityEngine;

namespace ChronowalkGame.Game.Stage.Contracts
{
    public interface IDirectional
    {
        bool HasDirection { get; }
        Direction Direction { get; }
    }
}
