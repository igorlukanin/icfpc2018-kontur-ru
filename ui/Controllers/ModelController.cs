using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using lib.Commands;
using lib.Models;
using lib.Utils;

using Microsoft.AspNetCore.Mvc;

namespace ui.Controllers
{
    [Route("api/[controller]")]
    public class MatrixController : Controller
    {
        [HttpGet("[action]")]
        public string[,,] Index(int i)
        {
            const string problemsDir = "../data/problemsF";
            var filename = Directory.EnumerateFiles(problemsDir, "*.mdl").ToList()[i];

            if (filename == null) return null;

            var content = System.IO.File.ReadAllBytes(filename);

            var voxels = Matrix.Load(content).Voxels;
            var strings = new string[voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2)];

            for (var x = 0; x < voxels.GetLength(0); x++)
                for (var y = 0; y < voxels.GetLength(1); y++)
                    for (var z = 0; z < voxels.GetLength(2); z++)
                        strings[x, y, z] = voxels[x, y, z] ? "" : "0";

            return strings;
        }

        [HttpGet("[action]")]
        public IEnumerable<string> Solutions()
        {
            return Directory
                .EnumerateFiles("../data/solutions/")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(x => x);
        }

        [HttpGet("[action]")]
        public TraceResult Trace(string file, int startTick = 0, int count = 2000)
        {
            var problemName = file.Split("-")[0];
            var problem = ProblemSolutionFactory.CreateProblem($"../data/problemsF/{problemName}_tgt.mdl");
            var solution = CommandSerializer.Load(System.IO.File.ReadAllBytes($"../data/solutions/{problemName}.nbt"));

            var state = new DeluxeState(problem.SourceMatrix, problem.TargetMatrix);
            var queue = new Queue<ICommand>(solution);
            var results = new List<TickResult>();
            var filledVoxels = new HashSet<Vec>(state.GetFilledVoxels());
            var tickIndex = 0;
            try
            {
                var newFilledVoxels = new List<Vec>(state.GetFilledVoxels());
                var newClearedVoxels = new List<Vec>();
                var interpreter = new Interpreter(state);
                while (queue.Any() && tickIndex < startTick + count)
                {
                    interpreter.Tick(queue);

                    foreach (var vec in interpreter.LastChangedCells)
                        if (state.Matrix[vec])
                        {
                            if (!filledVoxels.Contains(vec))
                            {
                                newFilledVoxels.Add(vec);
                                filledVoxels.Add(vec);
                            }
                        }
                        else if (filledVoxels.Contains(vec))
                        {
                            newClearedVoxels.Add(vec);
                            filledVoxels.Remove(vec);
                        }
                    if (tickIndex >= startTick)
                    {
                        results.Add(new TickResult
                            {
                                filled = newFilledVoxels.Select(v => new[] {v.X, v.Y, v.Z}).ToArray(),
                                cleared = newClearedVoxels.Select(v => new[] {v.X, v.Y, v.Z}).ToArray(),
                                bots = state.Bots
                                            .Select(x => new[] {x.Position.X, x.Position.Y, x.Position.Z})
                                            .ToArray(),
                                tickIndex = tickIndex
                            });
                        newFilledVoxels.Clear();
                    }
                    tickIndex++;
                }
            }
            catch (Exception e)
            {
                var arr = results.ToArray();
                return new TraceResult
                    {
                        Exception = e.ToString(),
                        R = problem.R,
                        startTick = startTick,
                        totalTicks = arr.Length - 1,
                        Ticks = arr
                };
            }

            var ticks = results.ToArray();
            return new TraceResult
                {
                    R = problem.R,
                    startTick = startTick,
                    totalTicks = ticks.Length - 1,
                    Ticks = ticks
                };
        }
    }

    public struct TraceResult
    {
        public string Exception;
        public int startTick;
        public int totalTicks;
        public int R;
        public TickResult[] Ticks;
    }

    public struct TickResult
    {
        public int[][] filled;
        public int[][] cleared;
        public int[][] bots;
        public int tickIndex;
    }
}