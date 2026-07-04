using System.Text;

namespace Helpdesk.Application.Reports;

public static class SimplePdfReport
{
    private const int MaxLines = 38;
    private const int MaxLineLength = 96;

    public static byte[] Create(string title, IReadOnlyCollection<string> lines)
    {
        var content = BuildPageContent(title, lines);
        var contentBytes = Encoding.ASCII.GetBytes(content);
        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n",
            "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n",
            $"5 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream\nendobj\n"
        };

        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n");

        var offsets = new List<long> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, obj);
        }

        var xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {offsets.Count}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(stream, $"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static string BuildPageContent(string title, IReadOnlyCollection<string> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("BT");
        builder.AppendLine("/F1 18 Tf");
        builder.AppendLine("50 790 Td");
        builder.AppendLine($"({EscapePdfText(Clip(title))}) Tj");
        builder.AppendLine("/F1 10 Tf");
        builder.AppendLine("0 -26 Td");
        builder.AppendLine($"(Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC) Tj");

        var visibleLines = lines.Take(MaxLines).ToList();
        foreach (var line in visibleLines)
        {
            builder.AppendLine("0 -17 Td");
            builder.AppendLine($"({EscapePdfText(Clip(line))}) Tj");
        }

        if (lines.Count > MaxLines)
        {
            builder.AppendLine("0 -17 Td");
            builder.AppendLine("(Report truncated to fit a single portfolio preview page.) Tj");
        }

        builder.AppendLine("ET");
        return builder.ToString();
    }

    private static string Clip(string value)
    {
        return value.Length <= MaxLineLength
            ? value
            : string.Concat(value.AsSpan(0, MaxLineLength - 3), "...");
    }

    private static string EscapePdfText(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            switch (character)
            {
                case '\\':
                case '(':
                case ')':
                    builder.Append('\\').Append(character);
                    break;
                case '\r':
                case '\n':
                    builder.Append(' ');
                    break;
                default:
                    builder.Append(character is < ' ' or > '~' ? '?' : character);
                    break;
            }
        }

        return builder.ToString();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
