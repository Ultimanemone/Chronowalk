using ChronowalkGame.Data.Levels;
using System;

namespace ChronowalkGame.Data
{
    public class ActionType
    {
        public class Move : ActionType
        {
            public Direction direction { get; private set; }

            public Move(Direction direction)
            {
                this.direction = direction;
            }
        }

        public class Interact : ActionType
        {
            public Object obj { get; private set; }

            public Interact(Object obj)
            {
                this.obj = obj;
            }
        }

        public class Wait : ActionType { }
    }
}