# Mini LLM Local (C# + .NET 10)

Fine-tuning de um modelo open-source pequeno (**Gemma 2 2B**, **Llama 3.2 1B/3B**,
**Phi-3 mini**) para um **domínio específico** (ex.: suporte técnico de um produto)
e uma **aplicação de chat 100% local** escrita em C#.

A ideia tem duas metades, com responsabilidades bem separadas:

| Parte | Linguagem | Onde roda | Papel |
|---|---|---|---|
| **Treino / Fine-tuning** | Python (QLoRA) | GPU consumer | Adapta o modelo base ao domínio |
| **Aplicação de chat** | C# / .NET 10 | CPU ou GPU local | Conversa, baixa modelos, inspeciona dados |

A fronteira entre as duas é o arquivo **`.gguf`** (modelo quantizado). O Python
treina e exporta; o C# carrega e conversa — sem depender de nenhuma API externa.

---

## ✨ O que o app de teste faz

App de **console** (terminal), interativo, com três funções:

- **💬 Conversar com a IA** — chat com streaming token a token sobre um modelo `.gguf` local.
- **📦 Modelos** — lista os modelos baixados e baixa novos do catálogo (Gemma/Llama/Phi) com barra de progresso.
- **📊 Ver dados de treino** — inspeciona os datasets `.jsonl` em `data/` (contagem + prévia em tabela).

> Uma análise sobre evoluir para uma **UI web em Blazor** está em
> [`docs/blazor-analysis.md`](docs/blazor-analysis.md).

---

## 🧱 Estrutura do repositório

```
Mini-LLM-Local-C-Sharp/
├── MiniLlmLocal.sln
├── src/
│   ├── MiniLlmLocal.Core/         # Biblioteca: motor de chat, catálogo/download, leitura de dataset
│   │   ├── Chat/                  # IChatEngine, LlamaChatEngine (LLamaSharp), opções
│   │   ├── Models/                # ModelInfo, ModelCatalog, ModelDownloader, LocalModelStore
│   │   ├── Data/                  # TrainingExample, DatasetReader (JSONL)
│   │   └── AppPaths.cs            # Descobre models/ e data/ a partir da raiz do repo
│   └── MiniLlmLocal.Console/      # App de teste (Spectre.Console)
│       └── Menus/                 # ChatMenu, ModelsMenu, DataMenu
├── tests/
│   └── MiniLlmLocal.Tests/        # xUnit (catálogo, dataset, store, progresso)
├── training/                      # Python: QLoRA + conversão para GGUF
│   ├── requirements.txt
│   ├── finetune.py
│   └── convert_to_gguf.py
├── data/
│   └── support-dataset.jsonl      # Dataset de exemplo (suporte técnico, pt-BR)
├── models/                        # (.gguf ficam aqui — ignorado pelo Git)
└── docs/
    └── blazor-analysis.md
```

---

## 🚀 Começando rápido (só o app C#)

Pré-requisitos: **.NET 10 SDK**.

```bash
# 1. Compilar e testar
dotnet build
dotnet test

# 2. Rodar o app de teste
dotnet run --project src/MiniLlmLocal.Console
```

No app:

1. Vá em **📦 Modelos → Baixar modelo do catálogo** e baixe, por exemplo, o **Gemma 2 2B Instruct** (~1,7 GB).
2. Volte e escolha **💬 Conversar com a IA**, selecione o modelo e converse.
3. Em **📊 Ver dados de treino**, abra o `support-dataset.jsonl` para ver os exemplos.

Dentro do chat: `/reset` limpa o histórico, `/sair` volta ao menu.

> O primeiro download e o primeiro carregamento podem levar alguns segundos/minutos.
> A inferência em CPU funciona, mas é mais lenta — veja **GPU** abaixo.

---

## 🖥️ CPU vs. GPU (inferência no C#)

Por padrão o `MiniLlmLocal.Console` referencia o **backend de CPU** do LLamaSharp
(`LLamaSharp.Backend.Cpu`), que funciona em qualquer máquina.

Para acelerar com **GPU NVIDIA**, troque o backend no
`src/MiniLlmLocal.Console/MiniLlmLocal.Console.csproj`:

```xml
<!-- Remova o backend de CPU e adicione o de CUDA 12: -->
<PackageReference Include="LLamaSharp.Backend.Cuda12.Windows" Version="0.27.0" />
```

E aumente `GpuLayerCount` (em `ChatEngineOptions`, padrão `0` = só CPU) para um
valor alto (ex.: `999`) para descarregar o modelo inteiro na GPU. Há também
backends para Vulkan/OpenCL no NuGet do LLamaSharp.

---

## 🎓 Fine-tuning (Python, GPU)

O fluxo completo: **dataset → QLoRA → merge → GGUF → app C#**.

### 1. Ambiente

```bash
cd training
python -m venv .venv
source .venv/bin/activate            # Windows: .venv\Scripts\activate
# Instale o PyTorch com CUDA conforme sua GPU (https://pytorch.org), depois:
pip install -r requirements.txt
```

Para modelos *gated* (Gemma, Llama oficiais): `huggingface-cli login`.

### 2. Treinar (QLoRA 4-bit)

```bash
python finetune.py \
    --base-model google/gemma-2-2b-it \
    --dataset ../data/support-dataset.jsonl \
    --output-dir ./output/gemma-2-2b-suporte \
    --merge
```

Isso treina apenas adaptadores LoRA (cabe em ~6–8 GB de VRAM) e, com `--merge`,
gera o modelo mesclado em `output/.../merged`.

### 3. Converter para GGUF

Requer o [llama.cpp](https://github.com/ggerganov/llama.cpp) compilado (instruções
no topo de `convert_to_gguf.py`):

```bash
export LLAMA_CPP_DIR=/caminho/para/llama.cpp
python convert_to_gguf.py \
    --model-dir ./output/gemma-2-2b-suporte/merged \
    --outfile ../models/gemma-2-2b-suporte-Q4_K_M.gguf \
    --quantize Q4_K_M
```

### 4. Usar no app

O `.gguf` cai em `models/` e aparece automaticamente na lista de modelos do app C#.

---

## 📄 Formato do dataset

JSONL (um objeto JSON por linha), estilo *instruction tuning*:

```json
{"instruction": "Você é o suporte técnico... Responda à dúvida.", "input": "Minha internet está lenta.", "output": "Vamos diagnosticar: 1. Reinicie..."}
```

- `instruction` — a tarefa / persona.
- `input` — contexto opcional (a mensagem do cliente). Pode ser `""`.
- `output` — a resposta desejada.

O mesmo formato é lido pelo `DatasetReader` (C#, para inspeção) e pelo
`finetune.py` (Python, para treino).

---

## ⚠️ Notas e limitações

- **Template de prompt:** o `LlamaChatEngine` usa a `ChatSession` padrão do
  LLamaSharp. Para máxima fidelidade por família de modelo, considere aplicar o
  *chat template* oficial. Os `AntiPrompts` já cobrem os marcadores de fim de
  turno de Gemma (`<end_of_turn>`), Llama 3 (`<|eot_id|>`) e Phi-3 (`<|end|>`).
- **Licenças:** Gemma e Llama têm termos próprios de uso; Phi-3 é MIT. Revise
  antes de uso comercial. Os modelos do catálogo apontam para mirrors GGUF
  públicos no Hugging Face.
- **Modelos não versionados:** arquivos `.gguf`/`.safetensors` são grandes e
  ficam fora do Git (veja `.gitignore`).

---

## 🛠️ Stack

- .NET 10 · C#
- [LLamaSharp](https://github.com/SciSharp/LLamaSharp) 0.27 (binding .NET do llama.cpp)
- [Spectre.Console](https://spectreconsole.net/) (UI de terminal)
- xUnit (testes)
- Python: `transformers`, `peft`, `trl`, `bitsandbytes` (QLoRA)
