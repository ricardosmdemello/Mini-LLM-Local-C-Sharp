"""
Fine-tuning QLoRA de um modelo pequeno (Gemma 2 / Llama 3.2 / Phi-3) para um
domínio específico (ex.: suporte técnico), rodável em GPU consumer (6–12 GB).

Fluxo geral:
    1. Carrega o modelo base em 4-bit (NF4) com bitsandbytes.
    2. Aplica adaptadores LoRA (peft) — só eles são treinados.
    3. Treina sobre um dataset JSONL no formato de instrução (instruction/input/output).
    4. Salva os adaptadores LoRA e, opcionalmente, o modelo já "mesclado" (merge).

O modelo mesclado é o que será convertido para GGUF (veja convert_to_gguf.py)
e consumido pelo app C#.

Exemplos de uso
---------------
    # Gemma 2 2B (gated no HF — faça `huggingface-cli login` antes):
    python finetune.py \
        --base-model google/gemma-2-2b-it \
        --dataset ../data/support-dataset.jsonl \
        --output-dir ./output/gemma-2-2b-suporte \
        --merge

    # Llama 3.2 1B:
    python finetune.py --base-model meta-llama/Llama-3.2-1B-Instruct \
        --dataset ../data/support-dataset.jsonl --output-dir ./output/llama-1b-suporte --merge

    # Phi-3 mini:
    python finetune.py --base-model microsoft/Phi-3-mini-4k-instruct \
        --dataset ../data/support-dataset.jsonl --output-dir ./output/phi3-suporte --merge
"""

import argparse
import os

import torch
from datasets import load_dataset
from peft import LoraConfig, PeftModel
from transformers import (
    AutoModelForCausalLM,
    AutoTokenizer,
    BitsAndBytesConfig,
    TrainingArguments,
)
from trl import SFTTrainer


def parse_args():
    p = argparse.ArgumentParser(description="Fine-tuning QLoRA para um domínio específico.")
    p.add_argument("--base-model", required=True,
                   help="ID do modelo base no Hugging Face (ex.: google/gemma-2-2b-it).")
    p.add_argument("--dataset", required=True,
                   help="Caminho para o .jsonl no formato instruction/input/output.")
    p.add_argument("--output-dir", required=True, help="Diretório de saída dos adaptadores/treino.")
    p.add_argument("--epochs", type=float, default=3.0)
    p.add_argument("--batch-size", type=int, default=1)
    p.add_argument("--grad-accum", type=int, default=8, help="Passos de acumulação de gradiente.")
    p.add_argument("--lr", type=float, default=2e-4)
    p.add_argument("--max-seq-len", type=int, default=1024)
    p.add_argument("--lora-r", type=int, default=16)
    p.add_argument("--lora-alpha", type=int, default=32)
    p.add_argument("--lora-dropout", type=float, default=0.05)
    p.add_argument("--merge", action="store_true",
                   help="Após treinar, mescla o LoRA no modelo base e salva em <output-dir>/merged.")
    return p.parse_args()


def format_example(example):
    """Monta um texto de treino a partir de instruction/input/output.

    Usa um formato genérico de instrução. Funciona bem na prática para as três
    famílias; para máxima fidelidade você pode trocar pelo chat template oficial
    de cada modelo via tokenizer.apply_chat_template.
    """
    instruction = (example.get("instruction") or "").strip()
    user_input = (example.get("input") or "").strip()
    output = (example.get("output") or "").strip()

    if user_input:
        prompt = f"### Instrução:\n{instruction}\n\n### Entrada:\n{user_input}\n\n### Resposta:\n"
    else:
        prompt = f"### Instrução:\n{instruction}\n\n### Resposta:\n"
    return {"text": prompt + output}


def main():
    args = parse_args()
    os.makedirs(args.output_dir, exist_ok=True)

    if not torch.cuda.is_available():
        print("[AVISO] Nenhuma GPU CUDA detectada. QLoRA praticamente exige GPU. "
              "Prossiga apenas para um teste de fumaça.")

    print(f"==> Carregando dataset: {args.dataset}")
    dataset = load_dataset("json", data_files=args.dataset, split="train")
    dataset = dataset.map(format_example, remove_columns=dataset.column_names)
    print(f"    {len(dataset)} exemplos.")

    print(f"==> Carregando modelo base em 4-bit: {args.base_model}")
    bnb_config = BitsAndBytesConfig(
        load_in_4bit=True,
        bnb_4bit_quant_type="nf4",
        bnb_4bit_compute_dtype=torch.bfloat16,
        bnb_4bit_use_double_quant=True,
    )

    tokenizer = AutoTokenizer.from_pretrained(args.base_model, trust_remote_code=True)
    if tokenizer.pad_token is None:
        tokenizer.pad_token = tokenizer.eos_token

    model = AutoModelForCausalLM.from_pretrained(
        args.base_model,
        quantization_config=bnb_config,
        device_map="auto",
        trust_remote_code=True,
        torch_dtype=torch.bfloat16,
    )
    model.config.use_cache = False

    lora_config = LoraConfig(
        r=args.lora_r,
        lora_alpha=args.lora_alpha,
        lora_dropout=args.lora_dropout,
        bias="none",
        task_type="CAUSAL_LM",
        target_modules=["q_proj", "k_proj", "v_proj", "o_proj",
                        "gate_proj", "up_proj", "down_proj"],
    )

    training_args = TrainingArguments(
        output_dir=args.output_dir,
        num_train_epochs=args.epochs,
        per_device_train_batch_size=args.batch_size,
        gradient_accumulation_steps=args.grad_accum,
        learning_rate=args.lr,
        bf16=True,
        logging_steps=5,
        save_strategy="epoch",
        optim="paged_adamw_8bit",
        lr_scheduler_type="cosine",
        warmup_ratio=0.03,
        report_to="none",
    )

    print("==> Iniciando treino (QLoRA)...")
    trainer = SFTTrainer(
        model=model,
        train_dataset=dataset,
        peft_config=lora_config,
        dataset_text_field="text",
        max_seq_length=args.max_seq_len,
        tokenizer=tokenizer,
        args=training_args,
    )
    trainer.train()

    adapter_dir = os.path.join(args.output_dir, "adapter")
    trainer.model.save_pretrained(adapter_dir)
    tokenizer.save_pretrained(adapter_dir)
    print(f"==> Adaptadores LoRA salvos em: {adapter_dir}")

    if args.merge:
        print("==> Mesclando LoRA no modelo base (fp16)...")
        del model, trainer
        torch.cuda.empty_cache()

        base = AutoModelForCausalLM.from_pretrained(
            args.base_model,
            torch_dtype=torch.float16,
            device_map="auto",
            trust_remote_code=True,
        )
        merged = PeftModel.from_pretrained(base, adapter_dir)
        merged = merged.merge_and_unload()

        merged_dir = os.path.join(args.output_dir, "merged")
        merged.save_pretrained(merged_dir, safe_serialization=True)
        tokenizer.save_pretrained(merged_dir)
        print(f"==> Modelo mesclado salvo em: {merged_dir}")
        print("    Converta para GGUF com: python convert_to_gguf.py --model-dir "
              f"{merged_dir} --outfile ../models/meu-modelo.gguf")


if __name__ == "__main__":
    main()
