using System;
using System.Collections.Generic;
using System.Threading;

namespace TetrisConsole
{
    internal class Program
    {
        private const int Rows = 20;
        private const int Cols = 10;

        private static readonly GameField Field = new GameField(Rows, Cols);
        private static Tetromino current = Tetromino.Random(Cols);

        private static readonly int FallIntervalMs = 500; // базовая скорость падения

        static void Main()
        {
            Console.Title = "Tetris – Золотая кираса";
            Console.CursorVisible = false;
            DateTime lastFall = DateTime.Now;

            while (true)
            {
                HandleInput();

                // автоматическое опускание фигуры
                if ((DateTime.Now - lastFall).TotalMilliseconds >= FallIntervalMs)
                {
                    if (!TryMove(0, 1))
                    {
                        Field.LockPiece(current);
                        Field.ClearLines();

                        if (Field.IsGameOver())
                        {
                            Draw();
                            Console.SetCursorPosition(0, Rows + 1);
                            Console.WriteLine("Игра окончена. R – рестарт, Esc – выход.");
                            WaitForRestart();
                            Field.Clear();
                        }

                        current = Tetromino.Random(Cols);
                    }

                    lastFall = DateTime.Now;
                }

                Draw();
                Thread.Sleep(16); // ~60 FPS
            }
        }

        private static void WaitForRestart()
        {
            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.R) return;
                if (key == ConsoleKey.Escape) Environment.Exit(0);
            }
        }

        private static void HandleInput()
        {
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                        TryMove(-1, 0);
                        break;
                    case ConsoleKey.RightArrow:
                        TryMove(1, 0);
                        break;
                    case ConsoleKey.DownArrow:
                        TryMove(0, 1);
                        break;
                    case ConsoleKey.UpArrow:
                        TryRotate();
                        break;
                    case ConsoleKey.Spacebar: // hard-drop
                        while (TryMove(0, 1)) { }
                        break;
                    case ConsoleKey.Escape:
                        Environment.Exit(0);
                        break;
                }
            }
        }

        private static bool TryMove(int dx, int dy)
        {
            if (!Field.IsCollision(current, dx, dy))
            {
                current.Move(dx, dy);
                return true;
            }
            return false;
        }

        private static void TryRotate()
        {
            current.Rotate();
            if (Field.IsCollision(current, 0, 0))
            {
                current.RotateBack(); // откат, если столкновение после поворота
            }
        }

        private static void Draw()
        {
            Console.SetCursorPosition(0, 0);
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    bool filled = Field[r, c] || current.IsAt(r, c);
                    Console.Write(filled ? '█' : ' ');
                }
                Console.WriteLine();
            }
        }
    }

    internal class GameField
    {
        private readonly int rows;
        private readonly int cols;
        private readonly bool[,] blocks;

        public GameField(int rows, int cols)
        {
            this.rows = rows;
            this.cols = cols;
            blocks = new bool[rows, cols];
        }

        public bool this[int r, int c] => blocks[r, c];

        public bool IsCollision(Tetromino t, int dx, int dy)
        {
            foreach (var (r, c) in t.Cells(dx, dy))
            {
                if (r < 0 || r >= rows || c < 0 || c >= cols) return true;
                if (blocks[r, c]) return true;
            }
            return false;
        }

        public void LockPiece(Tetromino t)
        {
            foreach (var (r, c) in t.Cells())
            {
                if (r >= 0 && r < rows)
                    blocks[r, c] = true;
            }
        }

        public void ClearLines()
        {
            for (int r = rows - 1; r >= 0; r--)
            {
                bool full = true;
                for (int c = 0; c < cols; c++)
                    if (!blocks[r, c]) { full = false; break; }

                if (full)
                {
                    for (int rr = r; rr > 0; rr--)
                        for (int c = 0; c < cols; c++)
                            blocks[rr, c] = blocks[rr - 1, c];
                    for (int c = 0; c < cols; c++) blocks[0, c] = false;
                    r++; // пересчитываем эту же строку ещё раз
                }
            }
        }

        public bool IsGameOver()
        {
            for (int c = 0; c < cols; c++)
                if (blocks[0, c]) return true;
            return false;
        }

        public void Clear() => Array.Clear(blocks, 0, blocks.Length);
    }

    internal class Tetromino
    {
        private static readonly bool[][,] Shapes =
        {
            new bool[,] { { true, true, true, true } },                            // I
            new bool[,] { { true, true }, { true, true } },                        // O
            new bool[,] { { false, true, false }, { true,  true, true  } },        // T
            new bool[,] { { true,  false, false }, { true,  true,  true  } },      // J
            new bool[,] { { false, false, true },  { true,  true,  true  } },      // L
            new bool[,] { { true,  true,  false }, { false, true,  true  } },      // S
            new bool[,] { { false, true,  true  }, { true,  true,  false } }       // Z
        };

        private int row;
        private int col;
        private bool[,] shape;
        private static readonly Random Rnd = new Random();

        private Tetromino(int shapeIdx, int startCol)
        {
            shape = Shapes[shapeIdx];
            row = 0;
            col = startCol;
        }

        public static Tetromino Random(int fieldCols)
        {
            int idx = Rnd.Next(Shapes.Length);
            int start = (fieldCols - Shapes[idx].GetLength(1)) / 2;
            return new Tetromino(idx, start);
        }

        public void Move(int dx, int dy)
        {
            col += dx;
            row += dy;
        }

        public void Rotate()     => shape = RotateMatrix(shape, true);
        public void RotateBack() => shape = RotateMatrix(shape, false);

        private static bool[,] RotateMatrix(bool[,] m, bool cw)
        {
            int r = m.GetLength(0), c = m.GetLength(1);
            var res = new bool[c, r];
            for (int i = 0; i < r; i++)
                for (int j = 0; j < c; j++)
                    res[cw ? j : c - 1 - j, cw ? r - 1 - i : i] = m[i, j];
            return res;
        }

        public IEnumerable<(int r, int c)> Cells(int dx = 0, int dy = 0)
        {
            for (int r = 0; r < shape.GetLength(0); r++)
                for (int c = 0; c < shape.GetLength(1); c++)
                    if (shape[r, c]) yield return (row + dy + r, col + dx + c);
        }

        public bool IsAt(int absR, int absC)
        {
            int lr = absR - row, lc = absC - col;
            return lr >= 0 && lr < shape.GetLength(0) &&
                   lc >= 0 && lc < shape.GetLength(1) &&
                   shape[lr, lc];
        }
    }
}