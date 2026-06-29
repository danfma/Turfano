# Quickstart: validar a correção de bugs

Comandos autônomos para implementar e verificar a feature `001-fix-current-bugs`.

## Pré-condição (TDD — demonstrar os bugs primeiro)

Antes de aplicar os patches, escrever os testes e confirmar que **falham**:

```bash
# Build atual
dotnet build Turfano.slnx -c Debug

# Após adicionar TransformScaleTests.cs e RhumbBearingTests.cs, rodar só essas classes:
dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/TransformScaleTests/*"
dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/RhumbBearingTests/*"
# Esperado ANTES dos patches: FALHAS (SC-005 para o caso padrão de TransformScale;
# RhumbBearing divergindo de 9.71° / para rumos > 180°).
```

## Aplicar os patches

1. `src/Turfano/Angles.cs`: `TwoPi = Angle.FromRadians(2 * Math.PI)`.
2. `src/Turfano/Turf.TransformScale.cs`: `var scaledY = dy * (options.FactorY ?? factor);`.
3. Varrer outros usos do padrão de precedência:
   ```bash
   grep -rn '\* options\.' src/Turfano --include='*.cs' | grep '??'
   ```

## Verificação final (critérios de sucesso)

```bash
# SC-004: build limpo em todos os TFMs
dotnet build Turfano.slnx -c Debug   # → 0 Error(s)

# SC-001/002/003/005: suíte completa verde, incluindo os novos testes
dotnet run --project tests/Turfano.Tests -c Debug
# → Passed!  failed: 0  (total = 146 + novos)
```

### Âncoras esperadas

- `RhumbBearing((-75.343, 39.984), (-75.534, 39.123))` → `≈ 9.71°` (`.Within(0.01)`).
- `TransformScale(polígono, 2)` sem `FactorY` → largura e altura da bbox = 2× as
  originais (Y não colapsa).
- `GetAngle(start, mid, end, o => o with { Explementary = true })` → `360° − θ`.

## Rollback

Como é comportamento-apenas em 2 arquivos + testes novos, o rollback é reverter os 2
patches; os testes novos podem permanecer (passam a falhar, documentando o bug).
