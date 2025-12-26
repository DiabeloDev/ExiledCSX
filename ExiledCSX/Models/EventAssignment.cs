using System;

namespace ExiledCSX.Models
{
    public class EventAssignment
    {
        public Type ArgsType { get; }
        public Delegate Handler { get; }

        public EventAssignment(Type argsType, Delegate handler)
        {
            ArgsType = argsType;
            Handler = handler;
        }
    }
}