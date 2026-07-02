# Contrato: API pública — engine exit (Fase 1)

## Core (`Turfano`) — assinaturas INALTERADAS, motor trocado

```csharp
namespace Turfano.GeoJson;

public static partial class Geo
{
    // agora computados pelo motor polyclip portado (internal), SEM NTS:
    public static Geometry? Union(Geometry a, Geometry b);
    public static Geometry? Difference(Geometry a, Geometry b);
    public static Geometry? Intersect(Geometry a, Geometry b);
    public static Geometry Dissolve(FeatureCollection polygons);
    public static FeatureCollection Polygonize(FeatureCollection lines); // grafo do @turf

    // REMOVIDO do core (movido ao satélite como extensão): Geo.Buffer
}
```

## Satélite (`Turfano.NetTopologySuite`) — NOVO

```csharp
namespace Turfano.NetTopologySuite;

/// Conversão na borda com o ecossistema NTS (EF Core spatial etc.). Fronteira empacotada.
public static class NtsConvert
{
    public static global::NetTopologySuite.Geometries.Geometry ToNts(Turfano.GeoJson.Geometry geometry);
    public static Turfano.GeoJson.Geometry FromNts(global::NetTopologySuite.Geometries.Geometry geometry);
    public static global::NetTopologySuite.Geometries.Coordinate ToNts(Turfano.GeoJson.Position position);
    public static Turfano.GeoJson.Position FromNts(global::NetTopologySuite.Geometries.Coordinate coordinate);
}

public static class NtsGeometryExtensions
{
    /// Buffer geodésico (AEQD → NTS Buffer → desprojeção), como na Onda E.
    public static Turfano.GeoJson.Geometry? Buffer(
        this Turfano.GeoJson.Geometry geometry,
        Turfano.Units.Length radius,
        int steps = 8);
}
```

## Invariantes de verificação
- Âncoras de área das Ondas D/E inalteradas, sem NTS no caminho (SC-001).
- `grep NetTopologySuite src/Turfano/Parity/` vazio (SC-002).
- Satélite: buffer = `@turf` (~3.12e10) + sequência do resultado empacotada (SC-003);
  ida-e-volta da bridge preserva furos e Z (SC-004).
- `NOTICE` presente; build net8/9/10; suíte verde; AOT serialização 0 warnings (SC-005).
