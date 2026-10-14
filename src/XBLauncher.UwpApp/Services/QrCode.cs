using System;
using System.Collections.Generic;

namespace XBLauncher.UwpApp.Services
{
    /// <summary>
    /// Minimal QR generator (byte mode, ECC level M) for the device-code URL.
    /// Self-contained; draws into a bool matrix for the caller to render.
    /// Supports versions 1-10 (enough for a login URL).
    /// </summary>
    public static class QrCode
    {
        private static readonly int[] DataCw = { 0,19,34,55,80,108,136,156,194,232,274 };
        private static readonly int[] EccCw = { 0,10,16,26,18,24,16,18,22,22,26 };
        private static readonly int[] Blocks = { 0,1,1,1,2,2,4,4,4,5,5 };

        public static bool[,] Generate(string text)
        {
            var data = System.Text.Encoding.UTF8.GetBytes(text);
            int v = 1; while (v < 10 && data.Length * 8 + 12 > DataCw[v] * 8) v++;
            int dcw = DataCw[v];
            var bits = new List<bool>();
            Append(bits, 0b0100, 4); Append(bits, data.Length, 8);
            foreach (var b in data) Append(bits, b, 8);
            int cap = dcw * 8;
            for (int i = 0; i < 4 && bits.Count < cap; i++) bits.Add(false);
            while (bits.Count % 8 != 0) bits.Add(false);
            byte[] pad = { 0xEC, 0x11 }; int pi = 0;
            while (bits.Count < cap) { Append(bits, pad[pi], 8); pi ^= 1; }
            var cw = new byte[dcw];
            for (int i = 0; i < bits.Count; i++) if (bits[i]) cw[i / 8] |= (byte)(0x80 >> (i % 8));
            var final = Interleave(cw, Blocks[v], EccCw[v]);

            int size = v * 4 + 17;
            var m = new bool[size, size]; var res = new bool[size, size];
            DrawFunc(m, res, v);
            DrawCw(m, res, final, size);
            int best = 0, bestPen = int.MaxValue;
            for (int mask = 0; mask < 8; mask++)
            {
                ApplyMask(m, res, mask); DrawFormat(m, mask);
                int pen = Penalty(m, size);
                if (pen < bestPen) { bestPen = pen; best = mask; }
                ApplyMask(m, res, mask);
            }
            ApplyMask(m, res, best); DrawFormat(m, best);
            return m;
        }

        private static void Append(List<bool> bits, int val, int n)
        { for (int i = n - 1; i >= 0; i--) bits.Add(((val >> i) & 1) != 0); }

        private static byte GfMul(byte a, byte b)
        { int r = 0; for (int i = 7; i >= 0; i--) { r = (r << 1) ^ ((r >> 7) * 0x11D); r ^= ((b >> i) & 1) * a; } return (byte)r; }

        private static byte[] Rs(byte[] data, int eccLen)
        {
            var gen = new byte[eccLen + 1]; gen[eccLen] = 1;
            var root = (byte)1;
            for (int i = 0; i < eccLen; i++)
            {
                for (int j = 0; j <= eccLen; j++)
                { gen[j] = GfMul(gen[j], root); if (j + 1 <= eccLen) gen[j] ^= gen[j + 1]; }
                root = GfMul(root, 2);
            }
            var res = new byte[eccLen];
            foreach (var b in data)
            {
                int factor = b ^ res[0];
                Array.Copy(res, 1, res, 0, eccLen - 1); res[eccLen - 1] = 0;
                for (int i = 0; i < eccLen; i++) res[i] ^= GfMul(gen[i + 1], (byte)factor);
            }
            return res;
        }

        private static byte[] Interleave(byte[] data, int blocks, int eccLen)
        {
            int dpb = data.Length / blocks;
            var db = new byte[blocks][]; var eb = new byte[blocks][];
            for (int b = 0; b < blocks; b++)
            { var s = new byte[dpb]; Array.Copy(data, b * dpb, s, 0, dpb); db[b] = s; eb[b] = Rs(s, eccLen); }
            var o = new List<byte>();
            for (int i = 0; i < dpb; i++) for (int b = 0; b < blocks; b++) o.Add(db[b][i]);
            for (int i = 0; i < eccLen; i++) for (int b = 0; b < blocks; b++) o.Add(eb[b][i]);
            return o.ToArray();
        }
        private static void DrawFunc(bool[,] m, bool[,] res, int v)
        {
            int size = m.GetLength(0);
            Finder(m, res, 0, 0); Finder(m, res, size - 7, 0); Finder(m, res, 0, size - 7);
            for (int i = 8; i < size - 8; i++)
            {
                bool dark = i % 2 == 0;
                if (!res[6, i]) { m[6, i] = dark; res[6, i] = true; }
                if (!res[i, 6]) { m[i, 6] = dark; res[i, 6] = true; }
            }
            if (v >= 2) Align(m, res, size - 7, size - 7);
            for (int i = 0; i < 9; i++) { res[8, i] = true; res[i, 8] = true; }
            for (int i = 0; i < 8; i++) { res[8, size - 1 - i] = true; res[size - 1 - i, 8] = true; }
            m[size - 8, 8] = true; res[size - 8, 8] = true;
        }

        private static void Finder(bool[,] m, bool[,] res, int x, int y)
        {
            for (int dy = -1; dy <= 7; dy++)
                for (int dx = -1; dx <= 7; dx++)
                {
                    int xx = x + dx, yy = y + dy;
                    if (xx < 0 || yy < 0 || xx >= m.GetLength(0) || yy >= m.GetLength(0)) continue;
                    bool dark = (dx >= 0 && dx <= 6 && (dy == 0 || dy == 6)) ||
                                (dy >= 0 && dy <= 6 && (dx == 0 || dx == 6)) ||
                                (dx >= 2 && dx <= 4 && dy >= 2 && dy <= 4);
                    m[yy, xx] = dark; res[yy, xx] = true;
                }
        }

        private static void Align(bool[,] m, bool[,] res, int x, int y)
        {
            for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                { bool dark = Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1; m[y + dy, x + dx] = dark; res[y + dy, x + dx] = true; }
        }

        private static void DrawCw(bool[,] m, bool[,] res, byte[] data, int size)
        {
            int bit = 0;
            for (int right = size - 1; right >= 1; right -= 2)
            {
                if (right == 6) right = 5;
                for (int vert = 0; vert < size; vert++)
                    for (int j = 0; j < 2; j++)
                    {
                        int x = right - j;
                        bool up = ((right + 1) & 2) == 0;
                        int y = up ? size - 1 - vert : vert;
                        if (res[y, x]) continue;
                        bool dark = bit < data.Length * 8 && ((data[bit / 8] >> (7 - (bit % 8))) & 1) != 0;
                        bit++; m[y, x] = dark;
                    }
            }
        }

        private static void ApplyMask(bool[,] m, bool[,] res, int mask)
        {
            int size = m.GetLength(0);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    if (res[y, x]) continue;
                    bool inv;
                    switch (mask)
                    {
                        case 0: inv = (x + y) % 2 == 0; break;
                        case 1: inv = y % 2 == 0; break;
                        case 2: inv = x % 3 == 0; break;
                        case 3: inv = (x + y) % 3 == 0; break;
                        case 4: inv = (y / 2 + x / 3) % 2 == 0; break;
                        case 5: inv = (x * y) % 2 + (x * y) % 3 == 0; break;
                        case 6: inv = ((x * y) % 2 + (x * y) % 3) % 2 == 0; break;
                        default: inv = ((x + y) % 2 + (x * y) % 3) % 2 == 0; break;
                    }
                    if (inv) m[y, x] = !m[y, x];
                }
        }

        private static void DrawFormat(bool[,] m, int mask)
        {
            int fmt = mask; // ECC level M = 00
            int rem = fmt;
            for (int i = 0; i < 10; i++) rem = (rem << 1) ^ ((rem >> 9) * 0x537);
            int bits = ((fmt << 10) | rem) ^ 0x5412;
            int size = m.GetLength(0);
            for (int i = 0; i <= 5; i++) m[8, i] = Bit(bits, i);
            m[8, 7] = Bit(bits, 6); m[8, 8] = Bit(bits, 7); m[7, 8] = Bit(bits, 8);
            for (int i = 9; i < 15; i++) m[14 - i, 8] = Bit(bits, i);
            for (int i = 0; i < 8; i++) m[size - 1 - i, 8] = Bit(bits, i);
            for (int i = 8; i < 15; i++) m[8, size - 15 + i] = Bit(bits, i);
        }

        private static bool Bit(int v, int i) => ((v >> i) & 1) != 0;

        private static int Penalty(bool[,] m, int size)
        {
            int pen = 0;
            for (int y = 0; y < size; y++)
            { int run = 1; for (int x = 1; x < size; x++) { if (m[y, x] == m[y, x - 1]) { run++; if (run == 5) pen += 3; else if (run > 5) pen++; } else run = 1; } }
            for (int x = 0; x < size; x++)
            { int run = 1; for (int y = 1; y < size; y++) { if (m[y, x] == m[y - 1, x]) { run++; if (run == 5) pen += 3; else if (run > 5) pen++; } else run = 1; } }
            for (int y = 0; y < size - 1; y++)
                for (int x = 0; x < size - 1; x++)
                { bool c = m[y, x]; if (c == m[y, x + 1] && c == m[y + 1, x] && c == m[y + 1, x + 1]) pen += 3; }
            int dark = 0; for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) if (m[y, x]) dark++;
            pen += Math.Abs(dark * 20 - size * size * 10) / (size * size) * 10;
            return pen;
        }
    }
}