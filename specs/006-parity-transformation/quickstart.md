# Quickstart: Onda C — Transformation & Mutation

## 1. Ground-truth do @turf (harness Bun)

```bash
# reference/_transform.mjs (efêmero): por função, imprime a saída do @turf (geometrias
# serializadas p/ comparação estrutural; números p/ tolerância). Inclui transformScale
# (geodésico), circle, simplify, rewind, etc.
cd reference && bun run _transform.mjs
```

## 2. Implementar (re-tipar onde fiel, portar onde diverge)

- `Geo.*` em `src/Turfano/Parity/{Mutate,Transform,Generate}.*.cs`, reusando helpers das
  Ondas A/B (`Distance`/`Rhumb*`/`Destination`/`BooleanClockwise`/`EachSegment`).
- `TransformScale`: **geodésico** (rhumbDistance/rhumbBearing/rhumbDestination da origem).

## 3. Testar vs @turf (SC-001/002)

```bash
dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/*Transform*/*"
dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/*Mutate*/*"
# transformScale prova a semântica geodésica.
```

## 4. Não-regressão + AOT (SC-004/005)

```bash
dotnet build Turfano.slnx -c Debug              # 0 erros, net8/9/10
dotnet run --project tests/Turfano.Tests -c Debug  # 203 + novos, 0 falhas
dotnet build tests/Turfano.AotSmoke -c Release 2>&1 | grep -c "warning IL"  # 0
git diff --stat main -- 'src/Turfano/Turf.*.cs'   # vazio (NTS intocado)
```

## 5. Encerramento

- Marcar a Onda C em `plans/turfjs-parity-redesign.md` (Fase 6) como `Complete` + Phase
  Summary; remover o harness efêmero.
