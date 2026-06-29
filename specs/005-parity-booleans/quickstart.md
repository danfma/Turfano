# Quickstart: Onda B — Booleans / Assertions

## 1. Ground-truth + fixtures do @turf (harness Bun)

```bash
# reference/_boolean.mjs (efêmero): para cada boolean*, (a) casos-âncora com resultado do
# @turf e (b) varredura das fixtures test/true e test/false dos pacotes @turf/boolean-*.
cd reference && bun run _boolean.mjs   # -> JSON (nome, args, esperado)
```

## 2. Implementar (portar o @turf, NÃO re-tipar o NTS)

- `Geo.Boolean*` em `src/Turfano/Parity/Boolean.*.cs`, portando o algoritmo do `@turf` e
  reusando helpers da Onda A (`InRing`/`PointInPolygon`, `NearestPointOnLine`, `IsLeft`).
- `booleanPointInPolygon`: porte do `inRing` com tratamento de borda + `ignoreBoundary`.

## 3. Testar vs @turf + fixtures (SC-001/002)

```bash
dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/Boolean*/*"
# Cada predicado bate com o @turf; o de point-in-polygon prova a borda (ignoreBoundary).
```

## 4. Não-regressão + AOT (SC-004/005)

```bash
dotnet build Turfano.slnx -c Debug              # 0 erros, net8/9/10
dotnet run --project tests/Turfano.Tests -c Debug  # 193 + novos, 0 falhas
dotnet build tests/Turfano.AotSmoke -c Release 2>&1 | grep -c "warning IL"  # 0
git diff --stat main -- 'src/Turfano/Turf.*.cs'   # vazio (NTS intocado)
```

## 5. Encerramento

- Marcar a Onda B em `plans/turfjs-parity-redesign.md` (Fase 5) como `Complete` + Phase
  Summary; remover o harness efêmero.
