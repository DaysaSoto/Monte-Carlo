using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using System.Globalization;
using System.Collections.Generic;

namespace parallel
{
    
    class ParallelSim
    {
        const byte S = 0, I = 1, R = 2, D = 3;
        static ThreadLocal<Random> rng = new ThreadLocal<Random>(() => new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)));

        static int WIDTH = 1000; static int HEIGHT = 1000; static int DAYS = 365;
        static double beta = 0.25, gamma = 0.05, mu = 0.005;

        static void Main(string[] args)
        {
            int cores = Environment.ProcessorCount;
            string outDir = "outputs/parallel";
            ParseArgs(args, ref WIDTH, ref HEIGHT, ref DAYS, ref cores, ref outDir);
            Directory.CreateDirectory(outDir);

            var grid = InitializeGrid(WIDTH, HEIGHT, 0.0001);
            var next = new byte[HEIGHT, WIDTH];

            var sw = Stopwatch.StartNew();

            for (int d = 0; d < DAYS; d++)
            {
                long totalNew = 0; long totalInfectious = 0;
                int blocks = Math.Min(cores, HEIGHT); // at most one row per block
                int rowsPer = HEIGHT / blocks;

                Parallel.For(0, blocks, new ParallelOptions { MaxDegreeOfParallelism = blocks }, bi =>
                {
                    int r0 = bi * rowsPer; int r1 = (bi == blocks - 1) ? HEIGHT : r0 + rowsPer;
                    int localH = (r1 - r0) + 2; // ghost top/bottom
                    var local = new byte[localH, WIDTH];

                    // copy global -> local with ghost rows
                    for (int lr = 0; lr < localH; lr++)
                    {
                        int gr = r0 + lr - 1;
                        if (gr < 0 || gr >= HEIGHT) for (int c = 0; c < WIDTH; c++) local[lr, c] = S;
                        else for (int c = 0; c < WIDTH; c++) local[lr, c] = grid[gr, c];
                    }

                    // process inner rows
                    for (int lr = 1; lr < localH - 1; lr++)
                    {
                        int gr = r0 + lr - 1;
                        for (int c = 0; c < WIDTH; c++)
                        {
                            byte s = local[lr, c];
                            if (s == S)
                            {
                                int k = CountInfectedNeighborsLocal(local, lr, c);
                                if (k > 0)
                                {
                                    double p = 1.0 - Math.Pow(1.0 - beta, k);
                                    if (rng.Value.NextDouble() < p) { next[gr, c] = I; Interlocked.Increment(ref _newInf); continue; }
                                }
                                next[gr, c] = S;
                            }
                            else if (s == I)
                            {
                                Interlocked.Increment(ref _curInfCount);
                                double rv = rng.Value.NextDouble();
                                if (rv < mu) next[gr, c] = D;
                                else if (rv < mu + gamma) next[gr, c] = R;
                                else next[gr, c] = I;
                            }
                            else next[gr, c] = s;
                        }
                    }
                });

                totalNew = Interlocked.Exchange(ref _newInf, 0);
                totalInfectious = Interlocked.Exchange(ref _curInfCount, 0);

                // swap grids
                var tmp = grid; grid = next; next = tmp;

                // optional frame save
                SaveFrame(grid, WIDTH, HEIGHT, Path.Combine(outDir, $"frame_{d:D4}.png"));

                // you can collect per-day stats here
            }

            sw.Stop();
            File.AppendAllText("timings.csv", $"parallel,{WIDTH}x{HEIGHT},{DAYS},{cores},{sw.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
    
            Console.WriteLine($"Done parallel in {sw.Elapsed}");
        }

        static int _newInf = 0, _curInfCount = 0;

        static void ParseArgs(string[] args, ref int w, ref int h, ref int days, ref int cores, ref string outDir)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--width") w = int.Parse(args[++i]);
                else if (args[i] == "--height") h = int.Parse(args[++i]);
                else if (args[i] == "--days") days = int.Parse(args[++i]);
                else if (args[i] == "--cores") cores = int.Parse(args[++i]);
                else if (args[i] == "--out") outDir = args[++i];
            }
        }

        static byte[,] InitializeGrid(int w, int h, double frac)
        {
            var g = new byte[h, w];
            var rnd = rng.Value;
            int total = w * h; int n = Math.Max(1, (int)(total * frac));
            for (int r = 0; r < h; r++) for (int c = 0; c < w; c++) g[r, c] = S;
            for (int i = 0; i < n; i++) g[rnd.Next(0, h), rnd.Next(0, w)] = I;
            return g;
        }

        static int CountInfectedNeighborsLocal(byte[,] local, int lr, int c)
        {
            int H = local.GetLength(0), W = local.GetLength(1), cnt = 0;
            for (int dr = -1; dr <= 1; dr++) for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int rr = lr + dr, cc = c + dc;
                    if (rr < 0 || rr >= H || cc < 0 || cc >= W) continue;
                    if (local[rr, cc] == I) cnt++;
                }
            return cnt;
        }

        static void SaveFrame(byte[,] g, int w, int h, string path)
        {
            using (var bmp = new Bitmap(w, h))
            {
                for (int r = 0; r < h; r++) for (int c = 0; c < w; c++)
                    {
                        byte s = g[r, c];
                        Color col = s == S ? Color.White : s == I ? Color.Red : s == R ? Color.Green : Color.Black;
                        bmp.SetPixel(c, r, col);
                    }
                bmp.Save(path);
            }
        }
    }
}
