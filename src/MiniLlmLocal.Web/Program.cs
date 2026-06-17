using MiniLlmLocal.Core;
using MiniLlmLocal.Core.Models;
using MiniLlmLocal.Web.Components;
using MiniLlmLocal.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- Serviços do MiniLlmLocal ---------------------------------------------
// Catálogo/armazenamento são stateless: singletons.
builder.Services.AddSingleton(_ => new LocalModelStore(AppPaths.ModelsDirectory));
builder.Services.AddSingleton<ModelDownloader>();
// Sessão de chat: 1 por circuito (aba), pois o LLamaContext tem estado e não é thread-safe.
builder.Services.AddScoped<ChatSessionState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
