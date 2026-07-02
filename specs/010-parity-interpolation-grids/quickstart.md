# Quickstart: Onda F — Interpolation, Grids & Triangulation

## Validar (depois de implementado)

```bash
dotnet build Turfano.slnx -c Debug
dotnet run --project tests/Turfano.Tests -c Debug
dotnet run --project tests/Turfano.Tests -- --treenode-filter "/*/*/GridTests/*"
dotnet run --project tests/Turfano.Tests -- --treenode-filter "/*/*/InterpolateTests/*"
dotnet run --project tests/Turfano.Tests -- --treenode-filter "/*/*/ContourTests/*"
dotnet run --project tests/Turfano.Tests -- --treenode-filter "/*/*/HullTessellationTests/*"
```

## Ground truth (@turf real)

```bash
cd reference && cat > _wavef.mjs <<'EOF'
import * as turf from "@turf/turf";
const bbox = [-1, -1, 1, 1];
console.log("pointGrid:", turf.pointGrid(bbox, 50, {units:"kilometers"}).features.length);
console.log("hexGrid:", turf.hexGrid(bbox, 50, {units:"kilometers"}).features.length);
const pts = turf.featureCollection([/* pontos com z */]);
// tin/planepoint/interpolate/isolines/isobands/convex/concave/voronoi/tesselate...
EOF
bun run _wavef.mjs   # (efêmero; remover antes do commit final)
```

## Critérios (spec)

- SC-001: cada função bate com o `@turf` (estrutural/numérico).
- SC-002: os 6 ingênuos do legado (tin/voronoi/concave/tesselate/isolines/isobands) com
  versão fiel provada.
- SC-003: `grep -rn "NetTopologySuite" src/Turfano/Parity/` → vazio.
- SC-004: build net8/9/10 limpo; suíte 245 + novos verde; AOT smoke 0 warnings IL.
