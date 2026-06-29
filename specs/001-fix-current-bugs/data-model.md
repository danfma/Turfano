# Data Model: Correção de bugs do Turfano (Fase 1)

**Não há entidades de dados novas ou alteradas.** Esta feature é correção de
comportamento de funções existentes; não cria nem modifica modelos persistidos,
DTOs ou contratos de dados.

## Tipos/símbolos tocados (referência)

| Símbolo | Arquivo | Natureza | Mudança |
|---|---|---|---|
| `Angles.TwoPi` | `src/Turfano/Angles.cs` | constante estática `Angle` | valor corrigido para `2π` (era `π`); assinatura inalterada |
| `Turf.TransformScale(...)` | `src/Turfano/Turf.TransformScale.cs` | método público | correção interna de cálculo do eixo Y; assinatura inalterada |
| `Turf.RhumbBearing(...)` | `src/Turfano/Turf.RhumbBearing.cs` | método público | sem edição; comportamento corrigido via `TwoPi` |
| `Turf.GetAngle(...)` | `src/Turfano/Turf.Angle.cs` | método público | sem edição; caminho `Explementary` corrigido via `TwoPi` |
| `TransformScaleOptions` | `src/Turfano/Turf.TransformScale.cs` | `readonly record struct` | inalterado (`FactorY`/`FactorZ`/`Origin`/`OriginZ`/`MutateZ`) |
| `GetAngleOptions` | `src/Turfano/Turf.Angle.cs` | `readonly record struct` | inalterado (`Explementary`/`Mercator`) |

## Regras / invariantes reforçadas

- `Angles.TwoPi == 2 × Angles.Pi` (invariante que estava quebrada).
- `TransformScale(g, k)` com `Origin`/`FactorY` ausentes ⇒ escala uniforme: a extensão
  em X e em Y é multiplicada por `k`.
- `GetAngle(..., Explementary: true) == 360° − GetAngle(..., Explementary: false)`.
