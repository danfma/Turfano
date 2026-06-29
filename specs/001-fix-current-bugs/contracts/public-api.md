# Contratos da API pública afetada (Fase 1)

A feature é **comportamento-apenas**: nenhuma assinatura pública muda. Este documento
fixa o contrato (assinatura + contrato comportamental corrigido) das funções afetadas,
para servir de base aos testes de regressão.

## `Turf.RhumbBearing`

```csharp
public static Angle RhumbBearing(
    Coordinate startPoint,
    Coordinate endPoint,
    Func<RhumbBearingOptions, RhumbBearingOptions>? configure = null);
```

**Contrato (corrigido)**:
- Retorna o rumo de linha de rumo em graus, no intervalo `[-180°, +180°]` (positivo no
  sentido horário a partir do norte), equivalente ao `@turf/rhumb-bearing`.
- Correto para rumos cujo `bearing360 > 180°` (antes retornava sinal/magnitude errados).
- `configure(o => o with { Final = true })` retorna o rumo final (do destino).
- **Âncora**: `RhumbBearing((-75.343, 39.984), (-75.534, 39.123)) ≈ -170.29°` (TurfJS
  real; `9.71°` é o sentido inverso `to→start` e era a saída do bug).

## `Turf.TransformScale` (sobrecargas Geometry/Point/LineString/Polygon)

```csharp
public static Geometry   TransformScale(Geometry geometry, double factor, TransformScaleOptions options = default);
public static Point      TransformScale(Point point,       double factor, TransformScaleOptions options = default);
public static LineString TransformScale(LineString line,   double factor, TransformScaleOptions options = default);
public static Polygon    TransformScale(Polygon polygon,   double factor, TransformScaleOptions options = default);
```

**Contrato (corrigido)**:
- Sem `FactorY` (caso padrão): escala **uniforme** — X e Y multiplicados por `factor` a
  partir da `Origin` (ou do centro da bbox se `Origin == null`).
- Com `FactorY`/`FactorZ`: cada eixo escala pelo seu fator.
- `TransformScaleOptions`: `Origin`, `FactorY`, `FactorZ`, `OriginZ`, `MutateZ`
  (inalterado).

## `Turf.GetAngle`

```csharp
public static Angle GetAngle(
    Coordinate startPoint,
    Coordinate midPoint,
    Coordinate endPoint,
    Func<GetAngleOptions, GetAngleOptions>? configure = null);
```

**Contrato (corrigido)**:
- Padrão: ângulo (positivo horário) entre os segmentos `start-mid` e `mid-end`.
- `configure(o => o with { Explementary = true })`: retorna `360° − ângulo`.
- `Mercator = true`: cálculo sobre projeção Mercator (usa `RhumbBearing`).

## Garantia transversal

- Nenhuma outra assinatura pública muda.
- A suíte de testes existente (146 testes) permanece verde; os novos testes apenas
  acrescentam cobertura.
