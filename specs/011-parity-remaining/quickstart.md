# Quickstart: Onda G — Paridade total

## Validar

```bash
dotnet build Turfano.slnx -c Debug
dotnet run --project tests/Turfano.Tests -c Debug
dotnet run --project tests/Turfano.Tests -- --treenode-filter "/*/*/LineOpsTests/*"
dotnet run --project tests/Turfano.Tests -- --treenode-filter "/*/*/ShapeProjectionTests/*"
dotnet run --project tests/Turfano.Tests -- --treenode-filter "/*/*/StatTests/*"
dotnet run --project tests/Turfano.Tests -- --treenode-filter "/*/*/RandomClusterTests/*"
```

## Ground truth

`reference/_waveg.mjs` (efêmero, bun) — GT por US antes de cada porte; aleatórios só
estruturais (R6). Kmeans/dbscan/collect têm GT EXATO (R3).

## Critérios (spec)

- SC-001: cruzamento final = 100% (exclusões: buffer→satélite, geojson-rbush→infra).
- SC-002: cada função nova com teste ancorado no `@turf`.
- SC-003: `ToMercator`/`ToWgs84` na fachada (pré-requisito leva 2).
- SC-004: grep NTS vazio; suíte 258+ verde; AOT 0 warnings.
