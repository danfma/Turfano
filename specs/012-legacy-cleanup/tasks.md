# Tasks: Leva 2 — Limpeza da superfície legada

- [X] T001 [US1] Deletar do core: `src/Turfano/Turf.*.cs` (72), `TerritoryUtils.cs`, `GeometryExtensions.cs`, `Angles.cs`, `BBox.cs` (raiz, legado) e qualquer arquivo órfão que só sirva o legado.
- [X] T002 [US1] `Turfano.csproj`: remover PackageReferences NTS/NTS.Features/UnitsNet; limpar `GlobalUsings.cs` (usings NTS/UnitsNet); build do core limpo.
- [X] T003 [US2] Deletar os testes legados de `tests/Turfano.Tests/` (os que usam `Turf.`/NTS/UnitsNet), mantendo TODOS os da fachada (Parity/, serialização, bridge, factory, ExactDecimal/SplayTree); ajustar usings se preciso.
- [X] T004 [US2] Reescrever `samples/Turfano.Playground/Program.cs` e `benchmark/TimeAndMemoryUsage` mínimos sobre a fachada `Geo` (remover refs NTS/UnitsNet desses csproj se existirem); solution inteira compila.
- [X] T005 [US2] Suíte completa 0 falhas; AOT smoke 0 warnings.
- [X] T006 [US3] Versão `1.0.0-rc.1` + Title/Description novos nos dois csproj; `README.md` atualizado (fachada Geo, satélite, NOTICE); `dotnet pack` dos dois ok.
- [X] T007 Verificação SC-001 (greps vazios no core) e polish: plano-mãe Fase 11 Complete + Phase Summary + Final Recap; merge na main.
