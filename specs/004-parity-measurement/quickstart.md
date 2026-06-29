# Quickstart: Onda A — Measurement

## 1. Ground-truth do @turf (harness Bun)

```bash
# reference/_measure.mjs (efêmero): importa @turf e imprime, por função/fixture, os valores
# de referência (area, distance, bearing, centroid, destination, along, rhumb*, ...).
cd reference && bun run _measure.mjs   # -> JSON com os valores do @turf
```

## 2. Implementar (re-tipar o que já é correto + consertar centroid)

- Para cada função, partir do algoritmo já fiel ao @turf nas `Turf.*.cs` atuais e
  re-tipá-lo sobre `Turfano.GeoJson`/`Turfano.Units` em `src/Turfano/Parity/`.
- `Centroid`: excluir o vértice de fechamento dos anéis.

## 3. Testar vs @turf (SC-001/002)

```bash
dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/Measure*/*"
# Cada função bate com o @turf (tolerância apertada). Centroid prova [1,1].
```

## 4. Não-regressão + AOT (SC-004/005)

```bash
dotnet build Turfano.slnx -c Debug              # 0 erros, net8/9/10
dotnet run --project tests/Turfano.Tests -c Debug  # 177 + novos, 0 falhas
dotnet build tests/Turfano.AotSmoke -c Release 2>&1 | grep -c "warning IL"  # 0
git diff --stat main -- 'src/Turfano/Turf.*.cs'   # vazio (NTS-based intocado)
```

## 5. Encerramento

- Marcar a Onda A em `plans/turfjs-parity-redesign.md` (Fase 4) como `Complete` + Phase
  Summary; remover o harness efêmero.
