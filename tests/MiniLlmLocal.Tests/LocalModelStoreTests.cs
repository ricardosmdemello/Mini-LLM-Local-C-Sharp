using MiniLlmLocal.Core.Models;

namespace MiniLlmLocal.Tests;

public class LocalModelStoreTests : IDisposable
{
    private readonly string _dir;

    public LocalModelStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "minillm-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public void List_EmptyWhenNoModels()
    {
        var store = new LocalModelStore(_dir);
        Assert.Empty(store.List());
    }

    [Fact]
    public void List_MissingDirectory_ReturnsEmpty()
    {
        var store = new LocalModelStore(Path.Combine(_dir, "nope"));
        Assert.Empty(store.List());
    }

    [Fact]
    public void List_FindsGgufFilesOnly()
    {
        File.WriteAllText(Path.Combine(_dir, "model-a.gguf"), "x");
        File.WriteAllText(Path.Combine(_dir, "model-b.gguf"), "yy");
        File.WriteAllText(Path.Combine(_dir, "readme.txt"), "ignore");

        var store = new LocalModelStore(_dir);
        var models = store.List();

        Assert.Equal(2, models.Count);
        Assert.All(models, m => Assert.EndsWith(".gguf", m.FileName));
    }

    [Fact]
    public void IsDownloaded_TrueWhenFilePresent()
    {
        var model = ModelCatalog.All[0];
        File.WriteAllText(Path.Combine(_dir, model.FileName), "x");

        var store = new LocalModelStore(_dir);
        Assert.True(store.IsDownloaded(model));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }
}
