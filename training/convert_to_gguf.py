"""
Converte um modelo Hugging Face (já mesclado pelo finetune.py) para o formato
GGUF e, opcionalmente, quantiza — produzindo o arquivo .gguf que o app C#
(LLamaSharp) carrega.

Este script é um *wrapper* fino sobre as ferramentas do llama.cpp:
    - convert_hf_to_gguf.py  (converte HF -> GGUF em fp16)
    - llama-quantize         (binário que quantiza, ex.: Q4_K_M)

Pré-requisitos
--------------
Clone e compile o llama.cpp (uma vez):

    git clone https://github.com/ggerganov/llama.cpp
    cd llama.cpp
    cmake -B build && cmake --build build --config Release
    pip install -r requirements.txt   # deps do convert_hf_to_gguf.py

Depois aponte a variável de ambiente LLAMA_CPP_DIR para a pasta do llama.cpp,
ou passe --llama-cpp-dir.

Exemplo
-------
    python convert_to_gguf.py \
        --model-dir ./output/gemma-2-2b-suporte/merged \
        --outfile ../models/gemma-2-2b-suporte-Q4_K_M.gguf \
        --quantize Q4_K_M
"""

import argparse
import os
import subprocess
import sys


def parse_args():
    p = argparse.ArgumentParser(description="Converte um modelo HF mesclado para GGUF.")
    p.add_argument("--model-dir", required=True,
                   help="Diretório do modelo mesclado (saída de finetune.py --merge).")
    p.add_argument("--outfile", required=True, help="Caminho do .gguf final.")
    p.add_argument("--quantize", default="Q4_K_M",
                   help="Tipo de quantização (ex.: Q4_K_M, Q5_K_M, Q8_0). 'none' para manter fp16.")
    p.add_argument("--llama-cpp-dir", default=os.environ.get("LLAMA_CPP_DIR"),
                   help="Caminho do clone do llama.cpp (ou defina LLAMA_CPP_DIR).")
    return p.parse_args()


def find_quantize_binary(llama_cpp_dir):
    """Localiza o binário llama-quantize (varia conforme o build/OS)."""
    candidates = [
        os.path.join(llama_cpp_dir, "build", "bin", "llama-quantize"),
        os.path.join(llama_cpp_dir, "build", "bin", "llama-quantize.exe"),
        os.path.join(llama_cpp_dir, "build", "bin", "Release", "llama-quantize.exe"),
        os.path.join(llama_cpp_dir, "llama-quantize"),
        os.path.join(llama_cpp_dir, "llama-quantize.exe"),
    ]
    for c in candidates:
        if os.path.isfile(c):
            return c
    return None


def main():
    args = parse_args()

    if not args.llama_cpp_dir:
        sys.exit("Erro: informe --llama-cpp-dir ou defina a variável LLAMA_CPP_DIR.")
    if not os.path.isdir(args.model_dir):
        sys.exit(f"Erro: diretório do modelo não encontrado: {args.model_dir}")

    convert_script = os.path.join(args.llama_cpp_dir, "convert_hf_to_gguf.py")
    if not os.path.isfile(convert_script):
        sys.exit(f"Erro: convert_hf_to_gguf.py não encontrado em {args.llama_cpp_dir}")

    os.makedirs(os.path.dirname(os.path.abspath(args.outfile)) or ".", exist_ok=True)

    quantizing = args.quantize.lower() != "none"
    # Se vamos quantizar, primeiro geramos um fp16 intermediário.
    fp16_path = args.outfile + (".fp16.gguf" if quantizing else "")
    target = fp16_path if quantizing else args.outfile

    print(f"==> Convertendo {args.model_dir} -> {target} (fp16)")
    subprocess.run(
        [sys.executable, convert_script, args.model_dir,
         "--outfile", target, "--outtype", "f16"],
        check=True,
    )

    if quantizing:
        quant_bin = find_quantize_binary(args.llama_cpp_dir)
        if not quant_bin:
            sys.exit("Erro: binário llama-quantize não encontrado. Compile o llama.cpp.")
        print(f"==> Quantizando -> {args.outfile} ({args.quantize})")
        subprocess.run([quant_bin, fp16_path, args.outfile, args.quantize], check=True)
        os.remove(fp16_path)

    print(f"\n✓ Pronto! GGUF gerado em: {args.outfile}")
    print("  Coloque-o na pasta models/ do projeto e selecione no app C#.")


if __name__ == "__main__":
    main()
