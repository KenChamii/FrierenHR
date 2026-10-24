using System.Globalization;
using System.Text;

namespace FrierenHR.Application.Common;

/// <summary>Builds RFC 4180 CSV.</summary>
public sealed class CsvBuilder
{
    private readonly StringBuilder _builder = new();
    private readonly int _columnCount;

    public CsvBuilder(params string[] headers)
    {
        _columnCount = headers.Length;
        AppendRow(headers);
    }

    public CsvBuilder AppendRow(params object?[] values)
    {
        if (values.Length != _columnCount)
            throw new InvalidOperationException(
                $"Row has {values.Length} values but the header declared {_columnCount}.");

        _builder.AppendLine(string.Join(',', values.Select(Escape)));
        return this;
    }

    public override string ToString() => _builder.ToString();

    /// <summary>Excel reads a bare UTF-8 file as the system code page.</summary>
    public byte[] ToUtf8WithBom() =>
        Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(ToString())).ToArray();

    private static string Escape(object? value)
    {
        var text = value switch
        {
            null => string.Empty,
            // Invariant, so a decimal does not become "1,25" and split the row in two.
            decimal d => d.ToString("0.##", CultureInfo.InvariantCulture),
            double d => d.ToString("0.##", CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        var needsQuoting = text.Contains(',') || text.Contains('"')
                           || text.Contains('\n') || text.Contains('\r');

        return needsQuoting ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
    }
}
