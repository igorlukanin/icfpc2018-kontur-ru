using System;
using System.Linq;

using lib.Commands;
using lib.Models;
using lib.Primitives;
using lib.Strategies.Features.Async;
using lib.Utils;

namespace lib.Strategies.Features
{
    public class Drill : BotStrategy
    {
        private readonly Vec target;

        public Drill(DeluxeState state, Bot bot, Vec target)
            : base(state, bot)
        {
            this.target = target;
        }

        protected override async StrategyTask<bool> Run()
        {
            while (true)
            {
                var hasPath = new PathFinderNeighbours(state.Matrix, bot.Position, target, x => !state.IsVolatile(bot, x)).TryFindPath(out var used);
                if (hasPath)
                {
                    var commands = new PathFinder(state, bot, target).TryFindPath();
                    if (commands == null)
                        throw new InvalidOperationException("WTF??");
                    if (await Move(bot, target))
                        return true;
                    continue;
                }

                var moveTarget = used
                                     .Where(v => new Region(v, target).Dim == 1 
                                                 && !new Region(v, target).Any(x => x != bot.Position && state.IsVolatile(bot, x)))
                                     .OrderBy(v => v.MDistTo(target)).FirstOrDefault();
                if (moveTarget == null)
                {
                    await WhenNextTurn();
                    continue;
                }
                if (!await Move(bot, moveTarget))
                    continue;

                var drillTarget = bot.Position.GetMNeighbours(state.Matrix).OrderBy(n => n.MDistTo(target)).First();
                if (state.IsVolatile(bot, drillTarget))
                {
                    await WhenNextTurn();
                    continue;
                }
                if (state.Matrix[drillTarget])
                    await Do(new Voidd(new NearDifference(drillTarget - bot.Position)));
            }
        }
    }
}