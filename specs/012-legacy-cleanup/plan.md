# Implementation Plan: Leva 2 — Limpeza da superfície legada

**Branch**: `012-legacy-cleanup` | **Date**: 2026-07-02 | **Spec**: [spec.md](./spec.md)

## Summary

Deleção da superfície legada (72 `Turf.*.cs` + infra), zero dependências no core,
consumidores sobre a fachada `Geo`, pacotes `1.0.0-rc.1`. Sem pesquisa (decisões todas
fechadas no plano-mãe); sem entidades novas; sem contratos novos (a API pública é a
fachada existente — a mudança é REMOÇÃO).

## Technical Context

C# multi-target net8/9/10; core vira zero-dep (BCL apenas + SourceLink build-only);
satélite mantém NTS. Testes TUnit. Riscos: deletar arquivo da fachada por engano
(mitigação: greps de verificação + suíte); benchmark/playground referenciam legado
(mitigação: reescrever mínimo sobre `Geo`).

## Constitution Check

PASS (gates do projeto: sem regressão da fachada, AOT, NOTICE intacto).

## Estrutura da execução

1. **US1**: deletar legado do core; limpar csproj/GlobalUsings; build core.
2. **US2**: deletar testes legados (manter fachada); reescrever playground/benchmark
   mínimos sobre `Geo`; suíte verde; AOT smoke.
3. **US3**: versões/metadados `1.0.0-rc.1`; README.md; `dotnet pack` dos dois.
4. **Polish**: greps SC-001; plano-mãe Fase 11 Complete + Final Recap; merge.
