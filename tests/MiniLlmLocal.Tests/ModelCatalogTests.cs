using MiniLlmLocal.Core.Models;

namespace MiniLlmLocal.Tests;

public class ModelCatalogTests
{
    [Fact]
    public void Catalog_IsNotEmpty()
    {
        Assert.NotEmpty(ModelCatalog.All);
    }

    [Fact]
    public void Catalog_HasUniqueIds()
    {
        var ids = ModelCatalog.All.Select(m => m.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Catalog_AllUrlsAreHttpsGguf()
    {
        Assert.All(ModelCatalog.All, m =>
        {
            Assert.StartsWith("https://", m.DownloadUrl);
            Assert.EndsWith(".gguf", m.FileName);
        });
    }

    [Fact]
    public void Catalog_IncludesGemma()
    {
        Assert.Contains(ModelCatalog.All, m => m.Family == ModelFamily.Gemma);
    }

    [Theory]
    [InlineData("gemma-2-2b-it")]
    [InlineData("GEMMA-2-2B-IT")]
    public void FindById_IsCaseInsensitive(string id)
    {
        Assert.NotNull(ModelCatalog.FindById(id));
    }

    [Fact]
    public void FindById_UnknownReturnsNull()
    {
        Assert.Null(ModelCatalog.FindById("inexistente"));
    }
}
