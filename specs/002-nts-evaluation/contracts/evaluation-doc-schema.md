# Contrato: estrutura obrigatória de `docs/nts-evaluation.md`

Esta feature não expõe API pública (é pesquisa). O "contrato" é a **estrutura do
documento entregável**, para que os critérios de sucesso sejam objetivamente
verificáveis e as fases seguintes saibam onde ler cada decisão.

`docs/nts-evaluation.md` MUST conter, nesta ordem, as seções:

## 1. Classificação de funções

Tabela cobrindo **100%** das funções de `Turf` (`public`/`internal`), colunas:
`function | visibility | classification (nts-wrapper/own/naive) | divergesFromTurf
(yes/no/n-a) | evidenceRef`. (Satisfaz SC-001.)

## 2. Divergências validadas (Turfano vs TurfJS)

Para cada função wrapper-NTS com `divergesFromTurf = yes`, ≥1 entrada com a fixture e o
par de saídas (`turfOutput` vs `turfanoOutput`) + categoria + veredito. Cada entrada cita
o comando/script que a reproduz. (Satisfaz SC-003 e FR-008.)

## 3. Matriz de decisão op-a-op

Tabela cobrindo **100%** das operações pesadas/ingênuas (`union, difference, intersect,
dissolve, buffer, convex, simplify, bboxClip, tin, voronoi, concave, tesselate, isobands,
isolines, bezierSpline`), colunas: `operation | turfAlgorithm | divergenceMagnitude |
portCost (P/M/G) | decision (portar/nts-interino/aproximar) | rationale`. (Satisfaz
SC-002.)

## 4. Benchmark: tipos próprios vs NTS

Tabela com `route (Distance/Area/WalkAlong) | timeOwn | timeNts | allocOwn | allocNts`,
mais o comando de reprodução (`dotnet run -c Release --project
benchmark/TimeAndMemoryUsage`) e as ressalvas de microbenchmark. (Satisfaz SC-004.)

## 5. Inventário de UnitsNet

Lista fechada `unitsNetType | usedIn`, com o `grep` que a gera. (Satisfaz SC-005.)

## 6. Recomendação final

Por operação: manter ou remover o NTS (consolidando a Seção 3) + conclusão sobre adotar
tipos de valor próprios (consolidando a Seção 4). É o insumo direto da Fase 3.

---

**Invariantes de verificação**
- Zero alterações em `src/` de produção; suíte permanece 156/0 (SC-006, FR-007).
- Toda divergência/ganho é reproduzível por um comando registrado (FR-008).
