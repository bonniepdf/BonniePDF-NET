using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BonniePDF.Rendering.Pdf;
/// <summary>Encrypts string tokens in a generated PDF fragment, preserving its syntax.</summary>
internal static class PdfStringEncryption
{
    public static string EncryptStrings(string fragment, Func<byte[], byte[]> encrypt)
    {
        StringBuilder result = new();
        int i = 0;
        string Encrypted(List<byte> bytes) => "<" + Convert.ToHexString(encrypt(bytes.ToArray())) + ">";
        while (i < fragment.Length)
        {
            int start = i;
            char c = fragment[i++];
            if (c == '%')
            {
                while (i < fragment.Length && fragment[i] != '\n' && fragment[i] != '\r')
                    i++;
                result.Append(fragment, start, i - start);
            }
            else if (c == '(')
            {
                List<byte> bytes = [];
                int depth = 1;
                while (i < fragment.Length && depth > 0)
                {
                    char ch = fragment[i++];
                    if (ch == '\\')
                    {
                        if (i == fragment.Length)
                            throw new FormatException("Unterminated PDF string");
                        ch = fragment[i++];
                        if (ch is '\r' or '\n')
                        {
                            if (ch == '\r' && i < fragment.Length && fragment[i] == '\n')
                                i++;
                            continue;
                        }

                        if (ch is >= '0' and <= '7')
                        {
                            int octal = ch - '0';
                            int digits = 1;
                            while (digits < 3 && i < fragment.Length && fragment[i] is >= '0' and <= '7')
                            {
                                octal = octal * 8 + fragment[i++] - '0';
                                digits++;
                            }

                            bytes.Add((byte)(octal & 255));
                        }
                        else
                            bytes.Add(ch switch
                            {
                                'n' => 10,
                                'r' => 13,
                                't' => 9,
                                'b' => 8,
                                'f' => 12,
                                _ => (byte)ch
                            });
                    }
                    else if (ch == ')')
                    {
                        if (--depth > 0)
                            bytes.Add(41);
                    }
                    else
                    {
                        if (ch == '(')
                            depth++;
                        if (ch == '\r')
                        {
                            if (i < fragment.Length && fragment[i] == '\n')
                                i++;
                            ch = '\n';
                        }

                        bytes.Add((byte)ch);
                    }
                }

                if (depth != 0)
                    throw new FormatException("Unterminated PDF string");
                result.Append(Encrypted(bytes));
            }
            else if (c == '<' && i < fragment.Length && fragment[i] == '<')
            {
                result.Append("<<");
                i++;
            }
            else if (c == '<')
            {
                int end = fragment.IndexOf('>', i);
                if (end < 0)
                    throw new FormatException("Unterminated PDF hex string");
                string hex = string.Concat(fragment.AsSpan(i, end - i).ToString().Where(ch => ch is not ('\0' or '\t' or '\n' or '\f' or '\r' or ' ')));
                if (hex.Length % 2 != 0)
                    hex += "0";
                List<byte> bytes = [];
                for (int index = 0; index < hex.Length; index += 2)
                    bytes.Add(byte.Parse(hex.AsSpan(index, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                result.Append(Encrypted(bytes));
                i = end + 1;
            }
            else
                result.Append(c);
        }

        return result.ToString();
    }
}
