using System.Globalization;
using System.Text;
using FrierenHR.Application.Common;

namespace FrierenHR.Tests.Common;

public class CsvBuilderTests
{
    [Fact]
    public void A_header_and_a_row_are_written_in_order()
    {
        var csv = new CsvBuilder("Name", "Days").AppendRow("Ana", 3).ToString();

        Assert.Equal($"Name,Days{Environment.NewLine}Ana,3{Environment.NewLine}", csv);
    }

    [Theory]
    [InlineData("O'Brien, Maria", "\"O'Brien, Maria\"")]   // a comma would split the row
    [InlineData("She said \"yes\"", "\"She said \"\"yes\"\"\"")] // quotes are doubled
    [InlineData("line one\nline two", "\"line one\nline two\"")] // a newline would end the row
    [InlineData("Ana", "Ana")]                            // nothing to quote
    public void Values_are_quoted_only_when_they_need_it(string value, string expected)
    {
        var csv = new CsvBuilder("Name").AppendRow(value).ToString();

        Assert.Contains(expected, csv);
    }

    [Fact]
    public void Decimals_are_written_invariant_so_a_comma_locale_cannot_split_a_row()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // writes 1,25 by default
        try
        {
            var csv = new CsvBuilder("Hours").AppendRow(1.25m).ToString();

            Assert.Contains("1.25", csv);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void A_null_becomes_an_empty_field_rather_than_the_word_null()
    {
        var csv = new CsvBuilder("Name", "Department").AppendRow("Ana", null).ToString();

        Assert.Contains("Ana,", csv);
        Assert.DoesNotContain("null", csv);
    }

    [Fact]
    public void A_row_with_the_wrong_number_of_values_is_rejected()
    {
        var builder = new CsvBuilder("Name", "Days");

        // Caught here rather than producing a file payroll silently misreads by one column.
        Assert.Throws<InvalidOperationException>(() => builder.AppendRow("Ana"));
    }

    [Fact]
    public void The_download_carries_a_byte_order_mark_so_Excel_reads_it_as_utf8()
    {
        var bytes = new CsvBuilder("Name").AppendRow("Niño").ToUtf8WithBom();

        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes.Take(3).ToArray());
        Assert.Contains("Niño", Encoding.UTF8.GetString(bytes));
    }
}
