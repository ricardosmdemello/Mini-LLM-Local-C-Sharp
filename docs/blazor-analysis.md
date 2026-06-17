# Análise: evoluir o app de teste para uma UI Web em Blazor

> Status: **proposta / análise**. A implementação atual é o app de **console**
> (`src/MiniLlmLocal.Console`). Este documento avalia trazer uma interface web.

## Contexto

A lógica de domínio (carregar modelo, chat com streaming, catálogo/download,
leitura de dataset) já vive em **`MiniLlmLocal.Core`** e **não depende do
console**. Isso é a peça-chave: uma UI Blazor consumiria exatamente as mesmas
classes (`LlamaChatEngine`, `ModelDownloader`, `LocalModelStore`,
`DatasetReader`), sem reescrita do núcleo.

## Qual sabor de Blazor?

| Modelo | Onde o modelo roda | Veredito |
|---|---|---|
| **Blazor Server** | No servidor (a mesma máquina local) | ✅ **Recomendado.** O `.gguf` carrega no processo .NET do servidor; o browser é só a tela. Streaming de tokens via SignalR é natural. |
| **Blazor WebAssembly** | No browser (sandbox WASM) | ❌ Inviável aqui. LLamaSharp depende de bibliotecas nativas (llama.cpp); não roda em WASM. Modelos de GB não cabem no browser. |
| **.NET MAUI Blazor Hybrid** | No processo nativo do app desktop | 🔸 Boa opção se o objetivo for um **app desktop** instalável (Windows/macOS) com UI web embutida. |

Para "rodar local com uma UI de chat mais bonita", o caminho natural é
**Blazor Server** rodando em `localhost`, aberto no navegador.

## O que mudaria na arquitetura

```
MiniLlmLocal.Core        (inalterado — toda a lógica)
        ▲
        ├── MiniLlmLocal.Console   (continua existindo)
        └── MiniLlmLocal.Web       (NOVO: Blazor Server)
```

- **Sessão de chat por usuário:** `LlamaContext` **não é thread-safe** e guarda
  estado de KV-cache. Numa app web é preciso 1 engine por sessão/aba, com
  serialização das chamadas de inferência. O ideal é um serviço *scoped* (ou um
  pool) em vez de um singleton compartilhado.
- **Memória:** cada modelo carregado consome RAM/VRAM. Numa app multiusuário
  isso vira gargalo — para uso local single-user é tranquilo.
- **Streaming:** trocar `await foreach` escrevendo no console por atualização de
  estado do componente (`StateHasChanged()`), ou um `IAsyncEnumerable` ligado a
  um componente de chat.
- **Download:** o `IProgress<DownloadProgress>` já existente alimenta uma
  `<progress>`/barra do MudBlazor com pouca cola.

## Esforço estimado

| Item | Esforço |
|---|---|
| Projeto `MiniLlmLocal.Web` (Blazor Server) + DI do Core | Baixo |
| Página de **Chat** com streaming + histórico | Médio (gestão de sessão/estado) |
| Página de **Modelos** (listar + baixar com progresso) | Baixo |
| Página de **Dados** (tabela de exemplos do `.jsonl`) | Baixo |
| Biblioteca de componentes (ex.: **MudBlazor**) para visual | Baixo |
| **Total** | **~1–2 dias** |

O baixo custo se deve inteiramente ao Core já estar desacoplado.

## Riscos / pontos de atenção

1. **Concorrência no `LLamaContext`** — o maior ponto técnico. Mitigar com
   engine por sessão + `SemaphoreSlim` por engine.
2. **Cancelamento** — a UI web deve permitir "parar geração"; o `IChatEngine`
   já aceita `CancellationToken`, então é só fiar até um botão.
3. **Segurança** — se exposto além de `localhost`, qualquer um na rede usaria
   sua GPU/CPU e leria os dados. Manter bindado em `127.0.0.1`.
4. **Hospedar em container/headless** — backend nativo do LLamaSharp precisa das
   libs certas (CPU/CUDA) na imagem.

## Recomendação

- **Curto prazo:** manter o console como a interface "de referência" e de testes.
- **Quando quiser UI:** criar **`MiniLlmLocal.Web` (Blazor Server)** + **MudBlazor**,
  reaproveitando 100% do `MiniLlmLocal.Core`. Começar pelas páginas de Modelos e
  Dados (triviais) e deixar o Chat por último, resolvendo a gestão de sessão do
  `LLamaContext`.
- **Se o alvo for desktop instalável:** considerar **MAUI Blazor Hybrid** em vez
  de Server, para empacotar tudo num executável.

> Próximo passo sugerido (não implementado): `dotnet new blazor -n MiniLlmLocal.Web`,
> adicionar referência ao Core, registrar os serviços no DI e portar uma página
> por vez.
