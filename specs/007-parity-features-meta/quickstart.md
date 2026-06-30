# Quickstart: Onda D — Feature Conversion, Joins & Meta

## 1. Ground-truth do @turf (harness Bun)

```bash
# reference/_features.mjs (efêmero): por função, imprime a saída do @turf (geometrias/
# coleções serializadas p/ comparação estrutural; números p/ distâncias; sequências de
# coord/índices para coordEach/segmentEach/flattenEach).
cd reference && bun run _features.mjs
```

## 2. Implementar (re-tipar reshape + portar onde preciso)

- `Geo.*` em `src/Turfano/Parity/{Convert,Join,Misc,Meta}.cs`, reusando helpers das Ondas
  A/B/C (`EachPosition`/`EachSegment`/`FlattenGeometry`/`BooleanPointInPolygon`/`Along`/
  `NearestPointOnLine`/`Distance`/`SegmentsIntersect`).
- Meta: expor `CoordEach`/`SegmentEach`/... com a assinatura/índices do `@turf`.

## 3. Testar vs @turf (SC-001/002/003)

```bash
dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/*Convert*/*"
dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/*Meta*/*"
# meta provam ordem/índices; pointsWithinPolygon prova a fronteira.
```

## 4. Não-regressão + AOT (SC-005)

```bash
dotnet build Turfano.slnx -c Debug              # 0 erros, net8/9/10
dotnet run --project tests/Turfano.Tests -c Debug  # 215 + novos, 0 falhas
dotnet build tests/Turfano.AotSmoke -c Release 2>&1 | grep -c "warning IL"  # 0
git diff --stat main -- 'src/Turfano/Turf.*.cs'   # vazio (NTS intocado)
```

## 5. Encerramento

- Marcar a Onda D em `plans/turfjs-parity-redesign.md` (Fase 7) como `Complete` + Phase
  Summary; remover o harness efêmero.
