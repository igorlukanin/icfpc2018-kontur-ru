using JetBrains.Annotations;

using lib.Models;
using lib.Primitives;
using lib.Utils;

namespace lib.Commands
{
    public class Voidd : BaseCommand
    {
        public NearDifference Shift { get; }

        public Voidd(NearDifference shift)
        {
            Shift = shift;
        }

        public override string ToString()
        {
            return $"Void({Shift})";
        }

        [NotNull]
        public override byte[] Encode()
        {
            return new [] {(byte)((Shift.GetParameter() << 3) | 0b010)};
        }

        public override bool AllPositionsAreValid([NotNull] IMatrix matrix, Bot bot)
        {
            return matrix.IsInside(GetPosition(bot));
        }

        public override void Apply(DeluxeState state, Bot bot)
        {
            var pos = GetPosition(bot);
            if (state.Matrix.IsFilledVoxel(pos))
            {
                state.Energy -= 12;
                state.Matrix.Void(pos);
            }
            else
            {
                state.Energy += 3;
            }
        }

        [NotNull]
        public override Vec[] GetVolatileCells([NotNull] Bot bot)
        {
            return new[] {bot.Position, GetPosition(bot)};
        }

        [NotNull]
        private Vec GetPosition([NotNull] Bot bot)
        {
            return bot.Position + Shift;
        }
    }
}