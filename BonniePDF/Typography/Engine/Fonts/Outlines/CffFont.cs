// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
namespace BonniePDF.Typography.Engine
{
    // CFF 1 reader from Adobe Technical Notes 5176 and 5177.
    internal sealed class CffFont
    {
        private readonly BigEndianData data;
        private bool embeddingChecked;
        private readonly byte[][] chars, globals;
        private readonly List<byte[][]> locals = new List<byte[][]>();
        private readonly List<byte[]> privates = new List<byte[]>();
        private readonly List<int> privateSizes = new List<int>();
        private readonly List<double[]> fdMatrices = new List<double[]>();
        private readonly int[] selection;
        private readonly double[] matrix, bbox;
        private readonly int glyphCount, unitsPerEm;
        private readonly OutlineReadLimits limits;
        private sealed class IndexData
        {
            internal byte[][] Items; internal int End;
        }
        internal sealed class EmbeddingData
        {
            internal byte[][] Chars, Globals, Privates;
            internal int[] PrivateSizes, Selection;
            internal double[] Matrix, BBox;
            internal double[][] FdMatrices;
        }
        internal CffFont(byte[] bytes, int glyphCount, int unitsPerEm, OutlineReadLimits limits)
        {
            data = new BigEndianData((byte[])bytes.Clone());
            this.glyphCount = glyphCount;
            this.unitsPerEm = unitsPerEm;
            this.limits = limits;
            if (data.U8(0) != 1 || data.U8(1) != 0)
                throw new NotSupportedException("Only CFF 1.0 is supported.");
            int header = data.U8(2);
            if (header < 4 || data.U8(3) < 1 || data.U8(3) > 4)
                Fail("Invalid CFF header.");
            var names = Index(header);
            var tops = Index(names.End);
            var strings = Index(tops.End);
            var globalIndex = Index(strings.End);
            if (names.Items.Length != 1 || tops.Items.Length != 1)
                throw new NotSupportedException("CFF FontSets are unsupported.");
            globals = globalIndex.Items;
            var top = Dict(tops.Items[0]);
            if (Value(top, 1205, 0) != 0)
                throw new NotSupportedException("Only filled CFF outlines are supported.");
            if (Value(top, 1206, 2) != 2)
                throw new NotSupportedException("Only Type 2 charstrings are supported.");
            matrix = ArrayValue(top, 1207, new double[] { .001, 0, 0, .001, 0, 0 });
            CheckMatrix(matrix);
            bbox = ArrayValue(top, 5, new double[] { 0, 0, 0, 0 });
            if (bbox.Length != 4)
                Fail("Invalid CFF FontBBox.");
            chars = Index(Integer(Value(top, 17, -1))).Items;
            if (chars.Length != glyphCount)
                Fail("CFF glyph count disagrees with maxp.");
            selection = new int[glyphCount];
            if (top.ContainsKey(1230))
            {
                var fds = Index(Integer(Value(top, 1236, -1))).Items;
                if (fds.Length == 0 || fds.Length > 256)
                    Fail("Invalid CFF FDArray.");
                foreach (var fd in fds)
                    AddPrivate(Dict(fd), true);
                int p = Integer(Value(top, 1237, -1)), format = data.U8(p++);
                if (format == 0)
                for (int i = 0; i < glyphCount; i++)
                    selection[i] = data.U8(p++);
                else if (format == 3)
                {
                    int count = data.U16(p);
                    p += 2;
                    if (count == 0 || count > glyphCount)
                        Fail("Invalid CFF FDSelect range count.");
                    int previous = 0;
                    for (int i = 0; i < count; i++)
                    {
                        int first = data.U16(p), fd = data.U8(p + 2), next = data.U16(p + 3);
                        p += 3;
                        if (first != previous || next <= first || next > glyphCount)
                            Fail("Invalid CFF FDSelect ranges.");
                        for (int g = first; g < next; g++)
                            selection[g] = fd;
                        previous = next;
                    }
                    if (previous != glyphCount)
                        Fail("Invalid CFF FDSelect sentinel.");
                }
                else
                    throw new NotSupportedException("Unsupported CFF FDSelect format.");
            }
            else
                AddPrivate(top, false);
            if (selection.Any(fd => fd >= locals.Count))
                Fail("Invalid CFF font dictionary index.");
        }
        private static void Fail(string message = "Invalid CFF data.")
        {
            throw new InvalidDataException(message);
        }
        private static int Integer(double n)
        {
            if (double.IsNaN(n) || double.IsInfinity(n) || n < 0 || n > int.MaxValue || n != Math.Floor(n))
                Fail("Invalid CFF offset or index.");
            return (int)n;
        }
        private static double Value(Dictionary<int, double[]> dict, int op, double fallback)
        {
            double[] values;
            if (!dict.TryGetValue(op, out values))
                return fallback;
            if (values.Length != 1)
                Fail("Invalid CFF DICT operand count.");
            return values[0];
        }
        private static double[] ArrayValue(Dictionary<int, double[]> dict, int op, double[] fallback)
        {
            double[] value;
            return dict.TryGetValue(op, out value) ? value : fallback;
        }
        private static void CheckMatrix(double[] m)
        {
            if (m.Length != 6 || m.Any(n => double.IsNaN(n) || double.IsInfinity(n)) || m[0] * m[3] == m[1] * m[2])
                Fail("Invalid CFF FontMatrix.");
        }
        private IndexData Index(int p)
        {
            int count = data.U16(p);
            p += 2;
            if (count == 0)
                return new IndexData { Items = Array.Empty<byte[]>(), End = p };
            int size = data.U8(p++);
            if (size < 1 || size > 4)
                Fail("Invalid CFF INDEX offSize.");
            data.Check(p, (count + 1) * size);
            int origin = p + (count + 1) * size;
            Func<int, int> offset = i => { long value = 0; for (int j = 0; j < size; j++) value = value * 256 + data.U8(p + i * size + j); if (value > int.MaxValue) Fail("CFF INDEX offset too large."); return (int)value; };
            int previous = offset(0);
            if (previous != 1)
                Fail("Invalid CFF INDEX origin.");
            var items = new byte[count][];
            for (int i = 0; i < count; i++)
            {
                int next = offset(i + 1);
                if (next < previous)
                    Fail("Invalid CFF INDEX order.");
                items[i] = data.Copy(checked(origin + previous - 1), next - previous);
                previous = next;
            }
            data.Check(origin, previous - 1);
            return new IndexData { Items = items, End = checked(origin + previous - 1) };
        }
        private static Dictionary<int, double[]> Dict(byte[] bytes)
        {
            var d = new BigEndianData(bytes);
            var result = new Dictionary<int, double[]>();
            var stack = new List<double>();
            int p = 0;
            while (p < bytes.Length)
            {
                int b = d.U8(p++);
                double n;
                if (b >= 32 && b <= 246)
                    n = b - 139;
                else if (b >= 247 && b <= 250)
                    n = (b - 247) * 256 + d.U8(p++) + 108;
                else if (b >= 251 && b <= 254)
                    n = -(b - 251) * 256 - d.U8(p++) - 108;
                else if (b == 28)
                {
                    n = d.I16(p);
                    p += 2;
                }
                else if (b == 29)
                {
                    n = unchecked((int)d.U32(p));
                    p += 4;
                }
                else if (b == 30)
                {
                    var value = new StringBuilder();
                    bool done = false;
                    while (!done)
                    {
                        int v = d.U8(p++);
                        foreach (int digit in new[] { v >> 4, v & 15 })
                        {
                            if (digit == 15)
                            {
                                done = true;
                                break;
                            }
                            if (digit == 13)
                                Fail("Invalid CFF real.");
                            value.Append(digit < 10 ? digit.ToString(CultureInfo.InvariantCulture) : digit == 10 ? "." : digit == 11 ? "E" : digit == 12 ? "E-" : "-");
                            if (value.Length > 64)
                                Fail("CFF real too long.");
                        }
                    }
                    if (!double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out n))
                        Fail("Invalid CFF real.");
                }
                else
                {
                    int op = b == 12 ? 1200 + d.U8(p++) : b;
                    if (b > 21 && b != 12)
                        Fail("Invalid CFF DICT operator.");
                    if (result.ContainsKey(op))
                        Fail("Duplicate CFF DICT operator.");
                    result.Add(op, stack.ToArray());
                    stack.Clear();
                    continue;
                }
                if (double.IsNaN(n) || double.IsInfinity(n) || stack.Count >= 48)
                    Fail("Invalid CFF DICT operands.");
                stack.Add(n);
            }
            if (stack.Count != 0)
                Fail("Unterminated CFF DICT operands.");
            return result;
        }
        private void AddPrivate(Dictionary<int, double[]> dict, bool cid)
        {
            var pair = ArrayValue(dict, 18, new double[] { 0, 0 });
            if (pair.Length != 2)
                Fail("Invalid CFF Private DICT.");
            int size = Integer(pair[0]), offset = Integer(pair[1]);
            var raw = data.Copy(offset, size);
            var priv = Dict(raw);
            int end = checked(offset + size);
            byte[][] subs = Array.Empty<byte[]>();
            if (priv.ContainsKey(19))
            {
                int at = checked(offset + Integer(Value(priv, 19, -1)));
                if (at < end)
                    Fail("CFF Subrs overlaps Private DICT.");
                var index = Index(at);
                subs = index.Items;
                end = index.End;
            }
            locals.Add(subs);
            privates.Add(data.Copy(offset, end - offset));
            privateSizes.Add(size);
            var m = cid ? ArrayValue(dict, 1207, new double[] { 1, 0, 0, 1, 0, 0 }) : new double[] { 1, 0, 0, 1, 0, 0 };
            CheckMatrix(m);
            fdMatrices.Add(m);
        }
        internal EmbeddingData GetEmbeddingData()
        {
            if (!embeddingChecked)
            {
                long cost = 0;
                for (int id = 0; id < glyphCount; id++)
                {
                    var t = new Type2(chars[id], locals[selection[id]], globals, limits, matrix, fdMatrices[selection[id]], unitsPerEm);
                    t.Read();
                    cost += t.OperationCount;
                    if (cost > 16000000)
                        Fail("CFF whole-program operation limit exceeded.");
                }
                embeddingChecked = true;
            }
            return new EmbeddingData { Chars = chars.Select(x => (byte[])x.Clone()).ToArray(), Globals = globals.Select(x => (byte[])x.Clone()).ToArray(), Privates = privates.Select(x => (byte[])x.Clone()).ToArray(), PrivateSizes = privateSizes.ToArray(), Selection = (int[])selection.Clone(), Matrix = (double[])matrix.Clone(), FdMatrices = fdMatrices.Select(x => (double[])x.Clone()).ToArray(), BBox = (double[])bbox.Clone() };
        }
        internal Glyph ReadGlyph(int id)
        {
            if (id < 0 || id >= glyphCount)
                Fail("Invalid glyph ID.");
            return new Type2(chars[id], locals[selection[id]], globals, limits, matrix, fdMatrices[selection[id]], unitsPerEm).Read();
        }
        private sealed class Type2
        {
            private List<double> stack = new List<double>(); private readonly double[] transient = new double[32];
            private double x, y; private int stems, operations, points, contours; private bool width, active, ended; private uint seed = 1;
            private readonly Glyph glyph = new Glyph(); private readonly byte[] bytes; private readonly byte[][] locals, globals; private readonly OutlineReadLimits limits; private readonly double[] top, fd; private readonly int upem;
            internal Type2(byte[] bytes, byte[][] locals, byte[][] globals, OutlineReadLimits limits, double[] top, double[] fd, int upem)
            {
                this.bytes = bytes;
                this.locals = locals;
                this.globals = globals;
                this.limits = limits;
                this.top = top;
                this.fd = fd;
                this.upem = upem;
            }
            internal int OperationCount
            {
                get
                {
                    return operations;
                }
            }
            private void Charge()
            {
                if (++operations > limits.MaxOperations)
                    Fail("CFF operation limit exceeded.");
            }
            private void Push(double n)
            {
                if (double.IsNaN(n) || double.IsInfinity(n) || Math.Abs(n) > int.MaxValue || stack.Count >= 48)
                    Fail("CFF operand limit exceeded.");
                stack.Add(n);
            }
            private double Pop()
            {
                if (stack.Count == 0)
                    Fail("CFF stack underflow.");
                double n = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                return n;
            }
            private static int Int(double n, int min, int max)
            {
                if (double.IsNaN(n) || n != Math.Floor(n) || n < min || n > max)
                    Fail("Invalid CFF operand index.");
                return (int)n;
            }
            private void Arity(int n)
            {
                if (stack.Count != n)
                    Fail("Invalid Type 2 charstring.");
            }
            private void Command(int kind, params double[] pairs)
            {
                points += pairs.Length / 2;
                if (points > limits.MaxPoints)
                    Fail("CFF point limit exceeded.");
                var c = new double[pairs.Length + 1];
                c[0] = kind;
                for (int i = 0; i < pairs.Length; i += 2)
                {
                    double a = fd[0] * pairs[i] + fd[2] * pairs[i + 1] + fd[4], b = fd[1] * pairs[i] + fd[3] * pairs[i + 1] + fd[5];
                    c[i + 1] = (top[0] * a + top[2] * b + top[4]) * upem;
                    c[i + 2] = (top[1] * a + top[3] * b + top[5]) * upem;
                    if (double.IsNaN(c[i + 1]) || double.IsInfinity(c[i + 1]) || double.IsNaN(c[i + 2]) || double.IsInfinity(c[i + 2]) || Math.Abs(c[i + 1]) > int.MaxValue || Math.Abs(c[i + 2]) > int.MaxValue)
                        Fail("CFF coordinate limit exceeded.");
                }
                glyph.Commands.Add(c);
            }
            private void Close()
            {
                if (active)
                    Command(3);
                active = false;
            }
            private void Move(double dx, double dy)
            {
                Close();
                x += dx;
                y += dy;
                if (++contours > limits.MaxContours)
                    Fail("CFF contour limit exceeded.");
                Command(0, x, y);
                active = true;
            }
            private void Line(double dx, double dy)
            {
                if (!active)
                    Fail("CFF path must begin with moveto.");
                x += dx;
                y += dy;
                Command(1, x, y);
            }
            private void Curve(double a, double b, double c, double d, double e, double f)
            {
                if (!active)
                    Fail("CFF path must begin with moveto.");
                double x1 = x + a, y1 = y + b, x2 = x1 + c, y2 = y1 + d;
                x = x2 + e;
                y = y2 + f;
                Command(2, x1, y1, x2, y2, x, y);
            }
            private void Curve(double[] s, int i)
            {
                Curve(s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);
            }
            private void Width(int expected, bool stem = false)
            {
                if (!width)
                {
                    if (stem ? stack.Count % 2 == 1 : stack.Count > expected)
                        stack.RemoveAt(0);
                    width = true;
                }
            }
            private void Execute(byte[] bytes, int depth, bool sub = false)
            {
                if (depth > Math.Min(10, limits.MaxDepth))
                    Fail("CFF subroutine depth limit exceeded.");
                var d = new BigEndianData(bytes);
                int p = 0;
                while (p < bytes.Length && !ended)
                {
                    Charge();
                    int op = d.U8(p++);
                    if (op >= 32)
                    {
                        double n;
                        if (op <= 246)
                            n = op - 139;
                        else if (op <= 250)
                            n = (op - 247) * 256 + d.U8(p++) + 108;
                        else if (op <= 254)
                            n = -(op - 251) * 256 - d.U8(p++) - 108;
                        else
                        {
                            n = unchecked((int)d.U32(p)) / 65536.0;
                            p += 4;
                        }
                        Push(n);
                        continue;
                    }
                    if (op == 28)
                    {
                        Push(d.I16(p));
                        p += 2;
                        continue;
                    }
                    if (op == 10 || op == 29)
                    {
                        var list = op == 10 ? locals : globals;
                        int bias = list.Length < 1240 ? 107 : list.Length < 33900 ? 1131 : 32768;
                        int index = Int(Pop() + bias, 0, list.Length - 1);
                        Execute(list[index], depth + 1, true);
                        continue;
                    }
                    if (op == 11)
                    {
                        if (!sub)
                            Fail("Type 2 return outside subroutine.");
                        return;
                    }
                    if (op == 12)
                    {
                        Escape(d.U8(p++));
                        continue;
                    }
                    if (op == 1 || op == 3 || op == 18 || op == 23 || op == 19 || op == 20)
                    {
                        Width(0, true);
                        if (stack.Count % 2 != 0)
                            Fail();
                        stems += stack.Count / 2;
                        if (stems > 96)
                            Fail("CFF stem limit exceeded.");
                        stack.Clear();
                        if (op == 19 || op == 20)
                        {
                            int n = (stems + 7) / 8;
                            d.Check(p, n);
                            p += n;
                        }
                        continue;
                    }
                    if (op == 4 || op == 21 || op == 22)
                    {
                        int n = op == 21 ? 2 : 1;
                        Width(n);
                        Arity(n);
                        Move(op == 4 ? 0 : stack[0], op == 22 ? 0 : stack[n - 1]);
                    }
                    else if (op == 5)
                    {
                        if (stack.Count < 2 || stack.Count % 2 != 0)
                            Fail();
                        for (int i = 0; i < stack.Count; i += 2)
                            Line(stack[i], stack[i + 1]);
                    }
                    else if (op == 6 || op == 7)
                    {
                        if (stack.Count == 0)
                            Fail();
                        bool h = op == 6;
                        foreach (double v in stack)
                        {
                            Line(h ? v : 0, h ? 0 : v);
                            h = !h;
                        }
                    }
                    else if (op == 8)
                    {
                        if (stack.Count == 0 || stack.Count % 6 != 0)
                            Fail();
                        var s = stack.ToArray();
                        for (int i = 0; i < s.Length; i += 6)
                            Curve(s, i);
                    }
                    else if (op == 24)
                    {
                        var s = stack.ToArray();
                        if (s.Length < 8 || (s.Length - 2) % 6 != 0)
                            Fail();
                        int i = 0;
                        for (; i < s.Length - 2; i += 6)
                            Curve(s, i);
                        Line(s[i], s[i + 1]);
                    }
                    else if (op == 25)
                    {
                        var s = stack.ToArray();
                        if (s.Length < 8 || (s.Length - 6) % 2 != 0)
                            Fail();
                        int i = 0;
                        for (; i < s.Length - 6; i += 2)
                            Line(s[i], s[i + 1]);
                        Curve(s, i);
                    }
                    else if (op == 26 || op == 27)
                    {
                        var s = stack.ToArray();
                        if (s.Length < 4 || s.Length % 4 > 1)
                            Fail();
                        int i = s.Length % 4;
                        double first = i != 0 ? s[0] : 0;
                        for (; i < s.Length; i += 4)
                        {
                            if (op == 26)
                                Curve(first, s[i], s[i + 1], s[i + 2], 0, s[i + 3]);
                            else
                                Curve(s[i], first, s[i + 1], s[i + 2], s[i + 3], 0);
                            first = 0;
                        }
                    }
                    else if (op == 30 || op == 31)
                    {
                        var s = stack.ToArray();
                        if (s.Length < 4 || s.Length % 4 > 1)
                            Fail();
                        int i = 0;
                        bool h = op == 31;
                        while (i < s.Length)
                        {
                            double a = s[i++], b = s[i++], c = s[i++], e = s[i++], last = s.Length - i == 1 ? s[i++] : 0;
                            if (h)
                                Curve(a, 0, b, c, last, e);
                            else
                                Curve(0, a, b, c, e, last);
                            h = !h;
                        }
                    }
                    else if (op == 14)
                    {
                        if (!width)
                        {
                            if (stack.Count == 1 || stack.Count == 5)
                                stack.RemoveAt(0);
                            width = true;
                        }
                        if (stack.Count == 4)
                            throw new NotSupportedException("Deprecated Type 2 seac composites are unsupported.");
                        Arity(0);
                        Close();
                        ended = true;
                    }
                    else
                        Fail("Unsupported or reserved Type 2 operator.");
                    stack.Clear();
                }
                if (!ended)
                    Fail(sub ? "CFF subroutine missing return." : "CFF charstring missing endchar.");
            }
            private void Escape(int op)
            {
                double a, b;
                if (op == 34)
                {
                    Arity(7);
                    Curve(stack[0], 0, stack[1], stack[2], stack[3], 0);
                    Curve(stack[4], 0, stack[5], -stack[2], stack[6], 0);
                    stack.Clear();
                }
                else if (op == 35)
                {
                    Arity(13);
                    var s = stack.ToArray();
                    Curve(s, 0);
                    Curve(s, 6);
                    stack.Clear();
                }
                else if (op == 36)
                {
                    Arity(9);
                    Curve(stack[0], stack[1], stack[2], stack[3], stack[4], 0);
                    Curve(stack[5], 0, stack[6], stack[7], stack[8], -stack[1] - stack[3] - stack[7]);
                    stack.Clear();
                }
                else if (op == 37)
                {
                    Arity(11);
                    var s = stack.ToArray();
                    double dx = s[0] + s[2] + s[4] + s[6] + s[8], dy = s[1] + s[3] + s[5] + s[7] + s[9];
                    Curve(s, 0);
                    Curve(s[6], s[7], s[8], s[9], Math.Abs(dx) > Math.Abs(dy) ? s[10] : -dx, Math.Abs(dx) > Math.Abs(dy) ? -dy : s[10]);
                    stack.Clear();
                }
                else if (op == 3 || op == 4 || op == 10 || op == 11 || op == 12 || op == 15 || op == 24)
                {
                    b = Pop();
                    a = Pop();
                    Push(op == 3 ? (a != 0 && b != 0 ? 1 : 0) : op == 4 ? (a != 0 || b != 0 ? 1 : 0) : op == 10 ? a + b : op == 11 ? a - b : op == 12 ? a / b : op == 15 ? (a == b ? 1 : 0) : a * b);
                }
                else if (op == 5 || op == 9 || op == 14 || op == 26)
                {
                    a = Pop();
                    Push(op == 5 ? (a != 0 ? 0 : 1) : op == 9 ? Math.Abs(a) : op == 14 ? -a : Math.Sqrt(a));
                }
                else if (op == 18)
                    Pop();
                else if (op == 20)
                {
                    int i = Int(Pop(), 0, 31);
                    transient[i] = Pop();
                }
                else if (op == 21)
                    Push(transient[Int(Pop(), 0, 31)]);
                else if (op == 22)
                {
                    double v2 = Pop(), v1 = Pop(), s2 = Pop(), s1 = Pop();
                    Push(v1 <= v2 ? s1 : s2);
                }
                else if (op == 23)
                {
                    seed = unchecked(seed * 1664525 + 1013904223);
                    Push((seed + 1.0) / 4294967296.0);
                }
                else if (op == 27)
                {
                    a = Pop();
                    Push(a);
                    Push(a);
                }
                else if (op == 28)
                {
                    a = Pop();
                    b = Pop();
                    Push(a);
                    Push(b);
                }
                else if (op == 29)
                {
                    a = Math.Max(0, Pop());
                    int i = Int(a, 0, stack.Count - 1);
                    Push(stack[stack.Count - 1 - i]);
                }
                else if (op == 30)
                {
                    double j = Pop();
                    int n = Int(Pop(), 0, stack.Count);
                    if (j != Math.Floor(j))
                        Fail();
                    if (n != 0)
                    {
                        int rotation = (int)((j % n + n) % n);
                        var part = stack.GetRange(stack.Count - n, n);
                        stack.RemoveRange(stack.Count - n, n);
                        stack.AddRange(part.Skip(n - rotation));
                        stack.AddRange(part.Take(n - rotation));
                    }
                }
                else
                    Fail("Unsupported or reserved Type 2 escaped operator.");
            }
            internal Glyph Read()
            {
                Execute(bytes, 0);
                bool any = false;
                double minX = 0, minY = 0, maxX = 0, maxY = 0;
                foreach (var c in glyph.Commands)
                for (int i = 1; i < c.Length; i += 2)
                {
                    if (!any)
                    {
                        minX = maxX = c[i];
                        minY = maxY = c[i + 1];
                        any = true;
                    }
                    else
                    {
                        minX = Math.Min(minX, c[i]);
                        minY = Math.Min(minY, c[i + 1]);
                        maxX = Math.Max(maxX, c[i]);
                        maxY = Math.Max(maxY, c[i + 1]);
                    }
                }
                if (any)
                {
                    glyph.XMin = (int)Math.Floor(minX);
                    glyph.YMin = (int)Math.Floor(minY);
                    glyph.XMax = (int)Math.Ceiling(maxX);
                    glyph.YMax = (int)Math.Ceiling(maxY);
                }
                return glyph;
            }
        }
    }
}
