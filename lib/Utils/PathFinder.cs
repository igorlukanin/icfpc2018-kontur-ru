using System;
using System.Collections.Generic;
using System.Linq;

using lib.Commands;
using lib.Models;
using lib.Primitives;

namespace lib.Utils
{
    public class PathFinder
    {
        private readonly bool[,,] state;
        private readonly Vec source;
        private readonly Vec target;
        private readonly Predicate<Vec> isAllowedPosition;
        private readonly int R;

        public PathFinder(bool[,,] state, Vec source, Vec target, Predicate<Vec> isAllowedPosition)
        {
            R = state.GetLength(0);
            this.state = state;
            this.source = source;
            this.target = target;
            this.isAllowedPosition = isAllowedPosition;
        }

        public PathFinder(DeluxeState state, Bot bot, Vec target)
            : this(state.Matrix.Voxels, bot.Position, target, vec => !state.IsVolatile(bot, vec))
        {
        }

        public PathFinder(bool[,,] state, Vec source, Vec target, HashSet<Vec> volatiles, Region region)
            : this(state, source, target, vec => (volatiles == null || !volatiles.Contains(vec)) && (region == null || vec.IsInRegion(region)))
        {
        }

        public List<ICommand> TryFindPath()
        {
            var used = new Dictionary<Vec, (Vec prev, ICommand command)>();
            if (source == target)
                return new List<ICommand>();

            if (!isAllowedPosition(target) || state.Get(target))
                return null;

            var queue = new SortedSet<Vec>(Comparer<Vec>.Create((a, b) =>
                {
                    var compareTo = a.MDistTo(target).CompareTo(b.MDistTo(target));
                    if (compareTo != 0)
                        return compareTo;
                    return Comparer<int>.Default.Compare(a.GetHashCode(), b.GetHashCode());
                }));
            queue.Add(source);
            used.Add(source, (null, null));
            while (queue.Any())
            {
                var current = queue.First();
                if (current == target)
                {
                    var result = new List<ICommand>();
                    for (var v = target; v != null; v = used[v].prev)
                    {
                        if (used[v].command != null)
                            result.Add(used[v].command);
                    }
                    result.Reverse();
                    return result;
                }
                queue.Remove(current);
                foreach (var (command, next) in IteratePossibleCommands(current))
                {
                    if (!used.ContainsKey(next))
                    {
                        used.Add(next, (current, command));
                        queue.Add(next);
                    }
                }
            }
            //var s = "";
            //for (int y = 0; y < R; y++)
            //{
            //    for (int z = 0; z < R; z++)
            //    {
            //        for (int x = R - 1; x >= 0; x--)
            //            s += used.ContainsKey(new Vec(x, y, z)) ? "X" : ".";
            //        s += "\r\n";
            //    }
            //    s += "===r\n";
            //}
            //File.WriteAllText(@"c:\2.txt", s);
            return null;
        }

        private static readonly Vec[] neighbors =
            {
                new Vec(1, 0, 0),
                new Vec(0, 1, 0),
                new Vec(0, 0, 1),
                new Vec(-1, 0, 0),
                new Vec(0, -1, 0),
                new Vec(0, 0, -1)
            };

        private IEnumerable<(ICommand, Vec)> IteratePossibleCommands(Vec current)
        {
            foreach (var n in neighbors)
            {
                var shift = Vec.Zero;
                for (int len = 1; len <= 15; len++)
                {
                    shift += n;
                    var res = current + shift;
                    if (!res.IsInCuboid(R) || !isAllowedPosition(res) || state.Get(res))
                        break;
                    yield return (new SMove(new LongLinearDifference(shift)), res);
                }
            }

            foreach (var fn in neighbors)
            {
                var fshift = Vec.Zero;
                for (int flen = 1; flen <= 5; flen++)
                {
                    fshift += fn;
                    var fres = current + fshift;
                    if (!fres.IsInCuboid(R) || !isAllowedPosition(fres) || state.Get(fres))
                        break;
                    foreach (var sn in neighbors)
                    {
                        if (fn * sn == 0)
                        {
                            var sshift = Vec.Zero;
                            for (int slen = 1; slen <= 5; slen++)
                            {
                                sshift += sn;
                                var res = fres + sshift;
                                if (!res.IsInCuboid(R) || !isAllowedPosition(res) || state.Get(res))
                                    break;
                                yield return (new LMove(new ShortLinearDifference(fshift), new ShortLinearDifference(sshift)), res);
                            }
                        }
                    }
                }
            }
        }
    }
}