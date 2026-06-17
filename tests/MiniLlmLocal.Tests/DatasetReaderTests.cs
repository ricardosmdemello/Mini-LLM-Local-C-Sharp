using System.Text.Json;
using MiniLlmLocal.Core.Data;

namespace MiniLlmLocal.Tests;

public class DatasetReaderTests
{
    [Fact]
    public void ParseJsonl_ParsesValidLines()
    {
        var lines = new[]
        {
            """{"instruction": "Como reinicio o roteador?", "input": "", "output": "Segure o botão por 10s."}""",
            """{"instruction": "Resolva o problema", "input": "Wi-Fi caindo", "output": "Atualize o firmware."}"""
        };

        var result = DatasetReader.ParseJsonl(lines);

        Assert.Equal(2, result.Count);
        Assert.Equal("Como reinicio o roteador?", result[0].Instruction);
        Assert.Equal("Wi-Fi caindo", result[1].Input);
        Assert.Equal("Atualize o firmware.", result[1].Output);
    }

    [Fact]
    public void ParseJsonl_SkipsBlankLines()
    {
        var lines = new[]
        {
            "",
            "   ",
            """{"instruction": "x", "input": "", "output": "y"}""",
            ""
        };

        var result = DatasetReader.ParseJsonl(lines);

        Assert.Single(result);
    }

    [Fact]
    public void ParseJsonl_InvalidLine_ThrowsWithLineNumber()
    {
        var lines = new[]
        {
            """{"instruction": "ok", "output": "ok"}""",
            "{ isto não é json válido"
        };

        var ex = Assert.Throws<JsonException>(() => DatasetReader.ParseJsonl(lines));
        Assert.Contains("linha 2", ex.Message);
    }

    [Fact]
    public void ReadJsonl_MissingFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => DatasetReader.ReadJsonl("nao_existe.jsonl"));
    }
}
