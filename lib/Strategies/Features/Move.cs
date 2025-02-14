using lib.Models;
using lib.Strategies.Features.Async;
using lib.Utils;

namespace lib.Strategies.Features
{
    public class Move : BotStrategy
    {
        private readonly Vec target;
        
        public Move(DeluxeState state, Bot bot, Vec target)
            : base(state, bot)
        {
            this.target = target;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(target)}: {target}";
        }

        protected override async StrategyTask<bool> Run()
        {
            var commands = new PathFinder(state, bot, target).TryFindPath();
            commands?.Reverse();
            if (commands == null)
                return false;

            var first = true;
            while (commands.Count > 0)
            {
                if (first)
                    first = false;
                else
                {
                    // todo (kungurtsev, 22.07.2018): в каких-то стратах возможно лучше сразу вернуть управление и пересчитать глобальные цели

                    if (commands[commands.Count - 1].HasVolatileConflicts(bot, state) ||
                        !commands[commands.Count - 1].AllPositionsAreValid(state.Matrix, bot))
                    {
                        commands = new PathFinder(state, bot, target).TryFindPath();
                        commands?.Reverse();
                        if (commands == null)
                            return false;
                    }
                }

                await Do(commands[commands.Count - 1]);
                commands.RemoveAt(commands.Count - 1);
            }

            return true;
        }
    }
}