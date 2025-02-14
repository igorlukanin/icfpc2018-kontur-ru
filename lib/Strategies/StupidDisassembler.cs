using System;
using System.Collections.Generic;
using System.Linq;

using lib.Commands;
using lib.Models;
using lib.Primitives;
using lib.Utils;

using MoreLinq;

namespace lib.Strategies
{
    public class StupidDisassembler : IAmSolver
    {
        private readonly Matrix source;
        private DeluxeState state;

        public StupidDisassembler(Matrix source)
        {
            this.source = source;
        }
        public IEnumerable<ICommand> Solve()
        {
            state = new DeluxeState(source, new Matrix(source.R));
            var bot = state.Bots.First();
            while (true)
            {
                var shift = Math.Min(15, source.R - bot.Position.Y - 1);
                yield return SimulateCommand(new SMove(new LongLinearDifference(new Vec(0, shift, 0))));
                if (shift < 15) break;
            }
            while (bot.Position.Y > 0)
            {
                var layer = GetLayer(state.Matrix, bot.Position.Y - 1).ToList();
                if (layer.Count == 0) yield return SimulateCommand(new SMove(new LongLinearDifference(new Vec(0, -1, 0))));
                else
                {
                    var botPos = bot.Position;
                    var target = layer.MinBy(p => p.GetMNeighbours(source).Count(n => state.Matrix[n])).First();
                    if (target.GetNears().Contains(botPos))
                        yield return SimulateCommand(new Voidd(new NearDifference(target-botPos)));
                    else
                        yield return SimulateMoveToTarget(target, botPos);
                }
            }
            while (!bot.Position.Equals(Vec.Zero))
            {
                yield return SimulateMoveToTarget(Vec.Zero, bot.Position);
            }
            yield return SimulateCommand(new Halt());
        }

        private ICommand SimulateMoveToTarget(Vec target, Vec botPos)
        {
            if (target.X != botPos.X)
            {
                var dx = Math.Min(15, Math.Max(-15, (target - botPos).X));
                return SimulateCommand(new SMove(new LongLinearDifference(new Vec(dx, 0, 0))));
            }
            else
            {
                var dz = Math.Min(15, Math.Max(-15, (target - botPos).Z));
                return SimulateCommand(new SMove(new LongLinearDifference(new Vec(0, 0, dz))));
            }
        }

        private ICommand SimulateCommand(ICommand command)
        {
            new Interpreter(state).Tick(new Queue<ICommand>(new[] { command }));
            return command;
        }

        private IEnumerable<Vec> GetLayer(IMatrix matrix, int y)
        {
            for (int x = 0; x < matrix.R; x++)
            for (int z = 0; z < matrix.R; z++)
            {
                if (matrix[x,y,z]) yield return new Vec(x,y,z);
            }
        }
    }
}