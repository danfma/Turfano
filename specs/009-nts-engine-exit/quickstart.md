# Quickstart: Saída do motor NTS — leva 1

## 1. Fontes do porte (leitura obrigatória, linha a linha)

```bash
# polyclip core (módulos marcados com "// src/*.ts"):
less reference/node_modules/polyclip-ts/dist/esm/index.js      # 1137 linhas
less reference/node_modules/splaytree-ts/dist/esm/index.js     # 687 linhas
less reference/node_modules/@turf/polygonize/dist/esm/index.js # 635 linhas
```

## 2. Ordem de implementação (dependências)

1. `ExactDecimal` (+ testes de aritmética exata) → 2. `SplayTreeSet` (+ testes de
ordem/vizinhos) → 3. precision/vector/bbox → 4. sweep-event/segment → 5. sweep-line/
operation/geom-in/geom-out → 6. rewire `Geo.Union/...` → 7. polygonize nativo → 8. satélite.

## 3. Validação por regressão (o truque desta feature)

```bash
# As âncoras do @turf já estão pinadas nos testes das Ondas D/E — o motor novo tem que
# passar nos MESMOS testes:
dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/OverlayTests/*"
dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/ConvertTests/*"
# + casos extras com furos (GT via bun) no fechamento.
```

## 4. Verificação estrutural

```bash
grep -r "NetTopologySuite" src/Turfano/Parity/ || echo "Parity livre de NTS ✅"
dotnet build Turfano.slnx -c Debug        # core + satélite, net8/9/10
dotnet run --project tests/Turfano.Tests -c Debug   # 232 + novos, 0 falhas
dotnet build tests/Turfano.AotSmoke -c Release 2>&1 | grep -c "warning IL"  # 0
ls NOTICE                                  # atribuições MIT/BSD-3
```

## 5. Encerramento

- Marcar os itens da leva 1 na Fase 11 do plano-mãe; registrar a otimização futura
  (caminho double + fallback exato) e os itens da leva 2 (pós-F/G).
