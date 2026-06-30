# Quickstart: Onda E — Overlay / Clipping

## 1. Ground-truth do @turf (harness Bun)

```bash
# reference/_overlay.mjs (efêmero): por função, imprime a ÁREA do resultado do @turf
# (union/difference/intersect/buffer/dissolve) e a estrutura (bboxClip).
cd reference && bun run _overlay.mjs
```

## 2. Implementar (overlay/buffer via NtsBridge; bboxClip portado)

- `Geo.Union/Difference/Intersect/Dissolve/Buffer` em `src/Turfano/Parity/Overlay*.cs`:
  `FromNts(ToNts(a).Union(ToNts(b)))` etc. (NTS escondido). Vazio → `null`.
- `Geo.BBoxClip` em `src/Turfano/Parity/Clip.BBoxClip.cs`: Cohen-Sutherland portado.

## 3. Testar vs @turf (SC-001/002)

```bash
dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/*Overlay*/*"
# union/intersect/difference: ÁREA bate com o @turf (~1e-5). bboxClip: estrutura.
```

## 4. Não-regressão + AOT (SC-004/005)

```bash
dotnet build Turfano.slnx -c Debug              # 0 erros, net8/9/10
dotnet run --project tests/Turfano.Tests -c Debug  # 226 + novos, 0 falhas
dotnet build tests/Turfano.AotSmoke -c Release 2>&1 | grep -c "warning IL"  # 0 (serialização)
git diff --stat main -- 'src/Turfano/Turf.*.cs'   # vazio (NTS-based intocado)
```

## 5. Encerramento

- Marcar a Onda E em `plans/turfjs-parity-redesign.md` (Fase 8) como `Complete` + Phase
  Summary; remover o harness efêmero.
