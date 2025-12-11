using System;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;


namespace sequential
{
   
        class SequentialSim
        {
            const byte S = 0, I = 1, R = 2, D = 3;
            static Random rng = new Random(12345);

            static int WIDTH = 1000;
            static int HEIGHT = 1000;
            static int DAYS = 365;

            static double beta = 0.25;
            static double gamma = 0.05;
            static double mu = 0.005;

            static void Main(string[] args)
            {
                string outDir = "outputs/sequential";
                Directory.CreateDirectory(outDir);
                ParseArgs(args, ref WIDTH, ref HEIGHT, ref DAYS, outDir);

                var grid = InitializeGrid(WIDTH, HEIGHT, 0.0001);
                var next = new byte[HEIGHT, WIDTH];

                var sw = Stopwatch.StartNew();
                for (int d = 0; d < DAYS; d++)
                {
                    int newInf = 0;
                    for (int r = 0; r < HEIGHT; r++)
                        for (int c = 0; c < WIDTH; c++)
                        {
                            byte s = grid[r, c];
                            if (s == S)
                            {
                                int k = CountInfectedNeighbors(grid, r, c);
                                if (k > 0)
                                {
                                    double p = 1.0 - Math.Pow(1.0 - beta, k);
                                    if (rng.NextDouble() < p) { next[r, c] = I; newInf++; continue; }
                                }
                                next[r, c] = S;
                            }
                            else if (s == I)
                            {
                                double rv = rng.NextDouble();
                                if (rv < mu) next[r, c] = D;
                                else if (rv < mu + gamma) next[r, c] = R;
                                else next[r, c] = I;
                            }
                            else next[r, c] = s;
                        }
                    // swap
                    var tmp = grid; grid = next; next = tmp;

                    // optional: save frame every N days (e.g., daily)
                    if ((d % 1) == 0) SaveFrame(grid, WIDTH, HEIGHT, Path.Combine(outDir, $"frame_{d:D4}.png"));
                }
                sw.Stop();
                File.AppendAllText("timings.csv", $"sequential,{WIDTH}x{HEIGHT},{DAYS},1,{sw.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
        
                Console.WriteLine($"Done sequential in {sw.Elapsed}");
            }

            static void ParseArgs(string[] args, ref int w, ref int h, ref int days, string outDir)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--width") w = int.Parse(args[++i]);
                    else if (args[i] == "--height") h = int.Parse(args[++i]);
                    else if (args[i] == "--days") days = int.Parse(args[++i]);
                    else if (args[i] == "--out") outDir = args[++i];
                }
            }

            static byte[,] InitializeGrid(int w, int h, double frac)
            {
                var g = new byte[h, w];
                int total = w * h; int n = Math.Max(1, (int)(total * frac));
                for (int r = 0; r < h; r++) for (int c = 0; c < w; c++) g[r, c] = S;
                for (int i = 0; i < n; i++) g[rng.Next(0, h), rng.Next(0, w)] = I;
                return g;
            }

            static int CountInfectedNeighbors(byte[,] g, int r, int c)
            {
                int h = g.GetLength(0), w = g.GetLength(1), cnt = 0;
                for (int dr = -1; dr <= 1; dr++) for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        int rr = r + dr, cc = c + dc;
                        if (rr < 0 || rr >= h || cc < 0 || cc >= w) continue;
                        if (g[rr, cc] == I) cnt++;
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
