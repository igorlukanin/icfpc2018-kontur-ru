using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using lib.Primitives;
using lib.Utils;

namespace lib.Models
{
    public class Matrix : IMatrix
    {
        private readonly bool[,,] voxels;

        public Matrix(int n)
            : this(new bool[n, n, n], 0)
        {
        }

        public IEnumerable<Vec> GetFilledVoxels()
        {
            return new Cuboid(R).AllPoints().Where(v => this[v]);
        }

        public Matrix([NotNull] bool[,,] voxels, int weight)
        {
            this.voxels = voxels;
            R = voxels.GetLength(0);
            Weight = weight;
        }

        public Matrix([NotNull] params string[] zLayers)
        {
            R = zLayers.Length;
            voxels = new bool[R, R, R];
            for (int x = 0; x < R; x++)
                for (int y = 0; y < R; y++)
                    for (int z = 0; z < R; z++)
                    {
                        var yLayers = zLayers[z].Split('|');
                        voxels[x, y, z] = yLayers[y][x] == '1';
                        if (voxels[x, y, z]) Weight++;
                    }
        }

        public int R { get; }

        [NotNull]
        public static Matrix Load([NotNull] byte[] content)
        {
            var r = content[0];
            var voxels = new bool[r, r, r];
            var bit = 0;
            var weight = 0;
            for (int x = 0; x < r; x++)
                for (int y = 0; y < r; y++)
                    for (int z = 0; z < r; z++)
                    {
                        byte b = content[1 + bit / 8];
                        bool isFull = (b >> (bit % 8) & 1) == 1;
                        voxels[x, y, z] = isFull;
                        if (isFull) weight++;
                        bit++;
                    }
            return new Matrix(voxels, weight);
        }

        [NotNull]
        public byte[] Save()
        {
            var content = new byte[1 + (R * R * R + 7) / 8];
            content[0] = (byte)R;
            var bit = 0;
            for (int x = 0; x < R; x++)
                for (int y = 0; y < R; y++)
                    for (int z = 0; z < R; z++)
                    {
                        bool isFull = voxels[x, y, z];
                        if (isFull)
                            content[1 + bit / 8] |= (byte)(1 << (bit % 8));
                        bit++;
                    }
            return content;
        }

        public bool this[[NotNull] Vec coord] { get => voxels[coord.X, coord.Y, coord.Z]; set => voxels[coord.X, coord.Y, coord.Z] = value; }

        public bool this[int x, int y, int z] { get => voxels[x, y, z]; set => voxels[x, y, z] = value; }
        
        public bool[,,] Voxels => this.voxels;

        public int Weight { get; }

        public Matrix Clone()
        {
            return new Matrix((bool[,,])voxels.Clone(), Weight);
        }

        public Matrix Intersect(Matrix target)
        {
            var r = target.R;
            var newVoxels = new bool[r,r,r];
            var weight = 0;
            for (int x = 0; x < r; x++)
            for (int y = 0; y < r; y++)
            for (int z = 0; z < r; z++)
            {
                var isFull = voxels[x, y, z] && target.voxels[x, y, z];
                newVoxels[x, y, z] = isFull;
                if (isFull) weight++;
            }
            return new Matrix(newVoxels, weight);
        }
    }
}