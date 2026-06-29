# Quickstart: conduzir e verificar a avaliação

Comandos autônomos para executar a Fase 2 (`002-nts-evaluation`).

## 1. Ground-truth do TurfJS (harness Bun)

```bash
# Em reference/ há @turf instalado e bun 1.3.x. Escrever um script efêmero que importa
# de @turf/turf e imprime as saídas para as fixtures canônicas, e rodá-lo:
#   (exemplo desta sessão: bun run reference/_tmp_refvals.mjs)
# Saída -> JSON com as saídas do TurfJS por função/fixture.
```

## 2. Saídas do Turfano para as mesmas fixtures

```bash
# Pequeno runner C# (ou testes descartáveis) que chama Turf.<fn> para as mesmas fixtures
# e imprime as saídas. Diferenciar contra o JSON do passo 1 conforme o critério da
# research.md (Decisão 2).
```

## 3. Inventário de UnitsNet (SC-005)

```bash
grep -rhoE 'UnitsNet\.[A-Za-z]+|\b(Length|Angle|Area)\b' src/Turfano --include='*.cs' \
  | sort | uniq -c | sort -rn
# Conferir que o conjunto é fechado (esperado: Length, Angle, Area).
```

## 4. Benchmark próprios vs NTS (SC-004)

```bash
# Após adicionar o protótipo (struct Position + benches Distance/Area/WalkAlong):
dotnet run -c Release --project benchmark/TimeAndMemoryUsage
# Esperado: tabela do BenchmarkDotNet com Mean + Allocated para cada rota, próprios vs NTS.
```

## 5. Verificação final (critérios de sucesso)

```bash
# SC-006: nenhuma mudança em src/ de produção
git diff --stat main -- src/Turfano   # → vazio

# Suíte permanece verde
dotnet run --project tests/Turfano.Tests -c Debug   # → 156, 0 falhas

# SC-001/002/003/005: o documento existe e cobre o exigido
test -f docs/nts-evaluation.md && grep -c '^|' docs/nts-evaluation.md
```

## 6. Encerramento

- Marcar a Fase 2 como `Complete` em `plans/turfjs-parity-redesign.md` e escrever o
  Phase Summary com a **recomendação consolidada** (insumo da Fase 3).
- Remover (ou marcar como fixtures de avaliação) os scripts efêmeros em `reference/` e os
  protótipos do `benchmark/`, conforme a decisão registrada.
