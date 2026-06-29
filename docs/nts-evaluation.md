# Avaliação NTS × TurfJS (Fase 2)

> Entregável da Fase 2 do plano `plans/turfjs-parity-redesign.md`. Decide, operação por
> operação, entre **portar** o algoritmo do TurfJS, **manter o NTS** interinamente, ou
> **aproximar**. Toda divergência foi **medida** rodando o TurfJS real (`reference/`,
> `@turf` via Bun) contra a saída do Turfano — nada presumido.

**Método**: harness Bun (`reference/_eval_turf.mjs`, efêmero) emite as saídas do `@turf`
para fixtures canônicas; um *file-based app* .NET 10 emite as saídas do Turfano para as
mesmas fixtures; comparam-se conforme o critério da `research.md` (escalar `>1e-6` /
booleano qualquer diferença / geometria por área-vértices). Reproduzível.

**Resumo executivo**: a divergência do NTS em relação ao TurfJS é **muito menor do que o
receio sugeria**. As funções de medição já foram reimplementadas no Turfano para
espelhar o Turf (e batem); o overlay do NTS dá **área idêntica** ao `polyclip-ts` nos
casos testados; e o `buffer` do Turf é, literalmente, um porte do JTS (= NTS). O risco
real concentra-se em (a) os **algoritmos ingênuos** (`tin/voronoi/concave/tesselate/
isolines/isobands`) e (b) **duas divergências pontuais** (`centroid` conta o vértice de
fechamento; `booleanOverlap` diverge em aresta compartilhada).

---

## 1. Classificação de funções

`own` = implementação própria (não delega geometria ao NTS). `nts-wrapper` = delega ao
NTS. `naive` = algoritmo próprio reconhecidamente incompleto/incorreto.

### Medição / conversão (todas `own`)
| Função | Classificação | Diverge do Turf? |
|---|---|---|
| Area, Bbox, BboxPolygon, Square, Distance, Bearing, GetLength | own | não (medido: Area, Distance, Bearing batem) |
| RhumbBearing, RhumbDistance, RhumbDestination, Destination | own | não (corrigido na Fase 1) |
| WalkAlong (Along), Midpoint, Center | own | não (medido: Midpoint, Along batem) |
| Centroid | own | **SIM** — conta o vértice de fechamento (ver §2) |
| PointOnFeature | own + NTS (Centroid/Area p/ seleção) | n/d (heurística) |
| PointToLineDistance, PointToPolygonDistance, PointToSegmentDistance | own | não |
| NearestPoint, NearestPointOnLine | own | não |
| GetAngle | own | **SIM** — convenção de bearing (~0.18°, ver Fase 1) |
| BearingToAzimuth, ToLength, ToRadians | own | não |
| ToMercator, ToWgs84 | own (filtro NTS) | não (medido na Fase 1: bate) |
| Envelope | own (usa `EnvelopeInternal`) | não |

### Predicados booleanos
| Função | Classificação | Diverge do Turf? |
|---|---|---|
| BooleanContains, BooleanWithin, BooleanTouches, BooleanCrosses, BooleanDisjoint, BooleanIntersects, BooleanEqual | nts-wrapper (DE-9IM) | não (medido: batem nos casos testados) |
| **BooleanOverlap** | nts-wrapper (`Overlaps`) | **SIM** — aresta compartilhada (ver §2) |
| BooleanPointInPolygon | own (bbox) + NTS `Contains` | não (medido: fronteira/ignoreBoundary batem) |
| BooleanClockwise, BooleanParallel, BooleanPointOnLine | own | não |

### Transformação / mutação
| Função | Classificação | Diverge do Turf? |
|---|---|---|
| TransformRotate, TransformScale, TransformTranslate | own (**cartesiano**) | **SIM** — Turf é geodésico (ver Fase 1) |
| Flip, Truncate | own (filtro NTS) | não |
| Rewind | nts-wrapper (`GeometryEditor`) | provavelmente não |
| Clone | nts-wrapper (`Copy`) | não |
| CleanCoords, PolygonSmooth, BezierSpline, Circle | own | a validar (amostragem) |

### Overlay / clipping / hulls
| Função | Classificação | Diverge do Turf? |
|---|---|---|
| Difference, Intersect, Union | nts-wrapper (overlay NTS) | **não** nos casos testados (área idêntica vs `polyclip-ts`) |
| Buffer | nts-wrapper (`Geometry.Buffer`) | não (Turf usa `@turf/jsts` = JTS = NTS) |
| Convex | nts-wrapper (`ConvexHull`) | não (medido: bate) |
| Simplify | nts-wrapper (DP/TPS) | não no caso testado (validar mais) |
| BBoxClip | own (Cohen-Sutherland) | a validar |
| LineOffset | own (via buffer NTS) | a validar |
| LineIntersect | NTS `Intersection` + própria | a validar |

### Linhas / features (todas `own`)
| Função | Classificação |
|---|---|
| LineSlice, LineSliceAlong, LineChunk, Explode, Kinks | own |
| Meta (CoordEach/FeatureEach/…, `internal`) | own |

### Algoritmos ingênuos (`naive`)
| Função | Classificação | Diverge do Turf? |
|---|---|---|
| Tin, Voronoi (`internal`), Concave, Tesselate | naive | **SIM** (algoritmo incorreto p/ entradas gerais) |
| Isolines (`internal`), Isobands (`internal`) | naive | **SIM** (marching-squares heurístico) |

---

## 2. Divergências validadas (Turfano vs TurfJS)

Fixtures em `reference/_eval_turf.mjs` + dump Turfano (scratch). Reproduzir: rodar o
harness Bun e o file-based app `eval_turfano.cs` (ver §Quickstart).

| Caso | TurfJS | Turfano | Veredito |
|---|---|---|---|
| `area` poly1 (m²) | 32819945055.137505 | 32819945055.137505 | **igual** |
| `distance` (0,0)-(1,1) km | 157.249598 | 157.249598 | **igual** |
| `bearing` (0,0)-(1,1) | 44.995636 | 44.995636 | **igual** |
| `midpoint` (0,0)-(10,0) | [5,0] | [5,0] | **igual** |
| `along` 2km | [0, 0.017986] | [0, 0.017986] | **igual** |
| `union` área | 566970009695.9088 | 566970009695.9088 | **igual** |
| `intersect` área | 49334402996.70673 | 49334402996.70674 | **igual** (~1e-5) |
| `difference` área | 259382060242.38522 | 259382060242.38522 | **igual** |
| `simplify` nº vértices | 2 | 2 | **igual** |
| `convex` nº vértices | 5 | 5 | **igual** |
| `booleanPointInPolygon` fronteira (default/ignore) | true / false | true / false | **igual** |
| `contains/within/touches/crosses/equal` | true | true | **igual** |
| **`centroid`** polígono irregular | **[1, 1]** | **[0.833, 0.833]** | **DIVERGE** |
| **`booleanOverlap`** aresta compartilhada | **true** | **false** | **DIVERGE** |

**Divergência 1 — `Centroid`**: o Turf calcula a média dos vértices **excluindo** o
vértice de fechamento (divide por 5); o Turfano inclui o fechamento (divide por 6),
dando `0.833` em vez de `1`. O teste `CentroidTest` do Turfano cravou o valor errado
(`0.8333`) — mesmo padrão do `RhumbBearing`/`9.71°` da Fase 1. **Correção barata** na
onda Measurement: pular o último coord em anéis fechados.

**Divergência 2 — `BooleanOverlap`**: para dois polígonos que compartilham só uma
aresta, o Turf retorna `true` e o `Overlaps` do NTS retorna `false` (interiores não se
sobrepõem). Semântica Turf-específica. **Tratar** na onda Booleans (regra própria, não
`Overlaps` puro).

---

## 3. Matriz de decisão op-a-op

`portCost`: P (pequeno) / M (médio) / G (grande). Decisão para o redesign.

| Operação | Algoritmo do TurfJS | Diverg. vs NTS | Custo porte | **Decisão** | Justificativa |
|---|---|---|---|---|---|
| union | `polyclip-ts` | nenhuma (medido) | G | **NTS-interino** | área idêntica nos testes; portar polyclip é caro e sem ganho imediato |
| difference | `polyclip-ts` | nenhuma (medido) | G | **NTS-interino** | idem |
| intersect | `polyclip-ts` | nenhuma (medido) | G | **NTS-interino** | idem |
| dissolve | `polyclip-ts` (+flatten) | n/d (não impl. no Turfano) | M | **NTS-interino** | implementar via overlay NTS quando necessário |
| buffer | `@turf/jsts` (= JTS/NTS) | nenhuma | — | **NTS-interino** | o Turf **é** JTS; manter NTS é fidelidade máxima |
| convex | `concaveman` | nenhuma (medido) | M | **NTS-interino** | hull idêntico; `ConvexHull` do NTS basta |
| simplify | simplify-js (DP+radial) | nenhuma no teste | M | **NTS-interino** | validar mais casos; DP do NTS é equivalente |
| bboxClip | `lineclip` (Cohen-Sutherland) | a validar | P | **aproximar→portar** | Turfano já tem Cohen-Sutherland próprio; alinhar ao `lineclip` |
| bezierSpline | spline próprio do Turf | a validar | P | **portar** | portar o algoritmo exato p/ fidelidade |
| **tin** | Delaunay incremental próprio | **grande** (naive) | M | **PORTAR** | Turfano atual é falso-Delaunay (incorreto) |
| **voronoi** | `d3-voronoi` | **grande** (naive) | M-G | **PORTAR** | Turfano atual é aproximação por bissetores (incorreto) |
| **concave** | `@turf/tin` + `topojson` | **grande** (naive) | M | **PORTAR** | depende do tin correto |
| **tesselate** | `earcut` | **grande** (naive) | M | **PORTAR** | Turfano faz leque de centroide (incorreto p/ côncavos) |
| **isolines** | marching-squares | **grande** (naive) | M | **PORTAR** | heurístico atual; portar marching-squares |
| **isobands** | marching-squares | **grande** (naive) | M-G | **PORTAR** | idem |

---

## 4. Benchmark: tipos próprios vs NTS

Protótipo descartável `benchmark/TimeAndMemoryUsage/NtsVsOwnBench.cs` (`readonly record
struct Pos` + `double` vs `Coordinate`/`Geometry` + UnitsNet), BenchmarkDotNet
`[ShortRunJob]` `[MemoryDiagnoser]`, .NET 10 Release. **Reproduzir**: apontar
`Program.cs` para `NtsVsOwnBench` e `dotnet run -c Release --project
benchmark/TimeAndMemoryUsage`.

| Método | Mean | Alocado |
|---|---|---|
| DistanceNts | 114.85 ns | 0 B |
| DistanceOwn | ~0 ns\* | 0 B |
| AreaNts | 104.87 ns | **352 B** |
| AreaOwn | 31.02 ns | **104 B** |

\* `DistanceOwn` deu ~0 ns por **constant-folding** (entradas literais dobradas em
constante pelo JIT) — artefato, não medida real. O sinal confiável é **Area**: tipos
próprios alocam **~3,4× menos** (352→104 B) e rodam **~3,4× mais rápido** (105→31 ns),
porque construir geometria NTS (`Coordinate`×5 + `LinearRing` + `Polygon`) aloca no heap,
enquanto o `Pos[]` é um único array de structs. Direção coerente com a indicação do
README (~25% tempo / ~40% memória no `WalkAlong`).

**Ressalvas**: microbenchmark (ShortRun, 3 iterações); ordem de grandeza, não prova de
produção; `WalkAlong` não foi re-prototipado aqui (porte mais extenso) — fica a indicação
do README + o resultado de Area.

---

## 5. Inventário de UnitsNet

Uso real em `src/Turfano` (de `grep`): conjunto **fechado** em 3 quantidades.

| Tipo UnitsNet | Ocorrências | Onde / observação |
|---|---|---|
| `Length` (+ `LengthUnit`) | 176 (+25) | distância/comprimento em quase toda a lib |
| `Angle` (+ `AngleUnit`) | 59 (+12) | bearing/ângulos (`Angles.cs`, RhumbBearing, GetAngle, Destination) |
| `Area` (+ `UnitsNet.Area`) | 11 (+5) | só em `Turf.Area.cs` (retorno e desambiguação) |

**Conclusão p/ Fase 3**: bastam **3 structs de valor próprios** (`Length`/`Distance`,
`Angle`/`Bearing`, `Area`), cada um com `enum` de unidades alinhado ao Turf
(`kilometers`/`meters`/`miles`/`degrees`/`radians`...). UnitsNet pode sair sem perda de
cobertura.

---

## 6. Recomendação final

**Tipos próprios: SIM.** O ganho de alocação/tempo é real (~3,4× em Area) e o inventário
mostra que só 3 quantidades do UnitsNet são usadas — a Fase 3 pode substituir NTS+UnitsNet
por tipos de valor GeoJSON próprios + 3 structs de unidade, sem perda de cobertura e com
ganho de performance e de serialização (STJ/AOT).

**NTS por operação:**
- **Manter (NTS-interino)**: overlay (`union`/`difference`/`intersect`/`dissolve`),
  `buffer`, `convex`, `simplify` e os predicados booleanos DE-9IM — todos batem com o Turf
  nos testes (e o `buffer` do Turf **é** o JTS/NTS). Mantê-los como motor interino reduz
  risco; reavaliar se surgirem casos de robustez divergentes.
- **Portar (prioridade — é onde está o ganho de correção)**: os algoritmos hoje
  **incorretos** — `tin`, `voronoi`, `concave`, `tesselate`, `isolines`, `isobands` —
  com os algoritmos reais do Turf (Delaunay, d3-voronoi, earcut, marching-squares).
- **Corrigir (barato, nas ondas de paridade)**: `Centroid` (excluir o vértice de
  fechamento), `BooleanOverlap` (regra do Turf p/ aresta compartilhada), `GetAngle`
  (convenção de bearing), `TransformScale/Rotate/Translate` (geodésico vs cartesiano).

**Conclusão p/ a Fase 3**: prosseguir com os tipos próprios; manter o NTS como dependência
**interina** só para overlay/buffer/convex/simplify; priorizar o porte dos 6 algoritmos
ingênuos nas ondas de paridade. A **remoção total** do NTS é viável, porém só compensa
**depois** que as operações pesadas tiverem substitutos portados — não no dia 1.

---

## Apêndice — scripts de reprodução

> O diretório `reference/` é `.gitignore`d, então os scripts abaixo ficam embutidos aqui
> para o documento ser autossuficiente (FR-008). O benchmark é versionado em
> `benchmark/TimeAndMemoryUsage/NtsVsOwnBench.cs` (apontar `Program.cs` para ele).

### A. Ground-truth do TurfJS (`reference/_eval_turf.mjs`, Bun)

```js
import * as turf from "@turf/turf";
const f = (n) => (typeof n === "number" ? Number(n.toFixed(6)) : n);
const sq = turf.polygon([[[0,0],[0,5],[5,5],[5,0],[0,0]]]);
const out = {};
out.bpip_boundary_default = turf.booleanPointInPolygon(turf.point([0,2.5]), sq);
out.bpip_boundary_ignore  = turf.booleanPointInPolygon(turf.point([0,2.5]), sq, { ignoreBoundary: true });
const shA = turf.polygon([[[0,0],[0,5],[5,5],[5,0],[0,0]]]);
const shB = turf.polygon([[[5,0],[5,5],[10,5],[10,0],[5,0]]]);
out.touches_sharededge = turf.booleanTouches(shA, shB);
out.overlap_sharededge = turf.booleanOverlap(shA, shB);     // => true (NTS: false)
const ovA = turf.polygon([[[0,0],[0,5],[5,5],[5,0],[0,0]]]);
const ovB = turf.polygon([[[3,3],[3,8],[8,8],[8,3],[3,3]]]);
out.union_area = f(turf.area(turf.union(turf.featureCollection([ovA, ovB]))));
out.intersect_area = f(turf.area(turf.intersect(turf.featureCollection([ovA, ovB]))));
const irr = turf.polygon([[[0,0],[0,2],[1,1],[2,2],[2,0],[0,0]]]);
out.centroid_irr = turf.centroid(irr).geometry.coordinates.map(f);  // => [1,1] (Turfano: [0.833,0.833])
console.log(JSON.stringify(out, null, 2));
// rodar: (cd reference && bun run _eval_turf.mjs)
```

### B. Saídas do Turfano (file-based app .NET 10)

```csharp
#:project /caminho/para/src/Turfano/Turfano.csproj
using NetTopologySuite.Geometries; using Turfano;
static Polygon P(params (double x,double y)[] c) =>
    new Polygon(new LinearRing(c.Select(p => new Coordinate(p.x,p.y)).ToArray()));
var shA = P((0,0),(0,5),(5,5),(5,0),(0,0));
var shB = P((5,0),(5,5),(10,5),(10,0),(5,0));
Console.WriteLine("overlap_sharededge=" + Turf.BooleanOverlap(shA, shB)); // False
var irr = P((0,0),(0,2),(1,1),(2,2),(2,0),(0,0));
var c = Turf.Centroid(irr);
Console.WriteLine($"centroid={c.X},{c.Y}");   // 0.833,0.833
// rodar: dotnet run eval_turfano.cs
```
