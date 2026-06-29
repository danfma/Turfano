# Quickstart: construir e verificar a fundação de tipos

## 0. Spike primeiro (derrubar o risco)

Validar num teste mínimo a combinação STJ source-gen + polimorfismo multinível
(`GeoJsonObject`/`Geometry`, discriminador `type`) + converter de `Position` como array.
Se o source-gen não suportar o polimorfismo multinível, adotar o converter polimórfico
manual (fallback da `research.md`) antes de seguir.

## 1. Round-trip GeoJSON (SC-001)

```bash
# Gerar fixtures do @turf (reference/, Bun) e comparar com o round-trip dos novos tipos.
# Teste TUnit: desserializa fixture -> reserializa -> compara forma (type/coordinates/
# properties/bbox) com a saída do @turf.
dotnet run --project tests/Turfano.Tests -c Debug
```

## 2. Conversões de unidade = @turf (SC-003)

```bash
# Harness Bun emite convertLength/convertArea/lengthToRadians/... ; teste C# compara.
# (cd reference && bun run _units.mjs)  # efêmero
dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/UnitsTests/*"
```

## 3. Smoke de AOT/trimming (SC-002)

```bash
# App de teste mínimo que (de)serializa os tipos do Turfano:
dotnet publish <app-aot-smoke> -c Release -p:PublishAot=true 2>&1 | grep -i 'IL2|IL3|trim' || echo "sem warnings de trimming/reflexão"
```

## 4. Não-regressão (SC-004)

```bash
dotnet build Turfano.slnx -c Debug              # 0 erros, net8/9/10
dotnet run --project tests/Turfano.Tests -c Debug  # 156 + novos, 0 falhas
```

## 5. Ponte interna NTS (SC-005)

```bash
# Teste: novo-tipo Polygon -> NtsBridge.ToNts -> FromNts -> coordenadas preservadas.
```

## 6. Encerramento

- Marcar a Fase 3 em `plans/turfjs-parity-redesign.md` como `Complete` + Phase Summary.
- Remover/parametrizar os harnesses efêmeros de `reference/`.
