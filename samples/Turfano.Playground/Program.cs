using Turfano.GeoJson;

// Exemplo mínimo sobre a fachada Geo (tipos GeoJSON próprios, zero NTS/UnitsNet).

var origin = new Position(-122.4194, 37.7749);
var destination = new Position(-122.4094, 37.7849);

var square = Geo.Polygon([
    new Position(0, 0),
    new Position(1, 0),
    new Position(1, 1),
    new Position(0, 1),
    new Position(0, 0),
]);

var overlappingSquare = Geo.Polygon([
    new Position(0.5, 0.5),
    new Position(1.5, 0.5),
    new Position(1.5, 1.5),
    new Position(0.5, 1.5),
    new Position(0.5, 0.5),
]);

var distance = Geo.Distance(origin, destination);
var area = Geo.Area(square);
var union = Geo.Union(square, overlappingSquare);

Console.WriteLine($"Distância entre os pontos: {distance.Kilometers:F3} km");
Console.WriteLine($"Área do quadrado unitário: {area.SquareMeters:F1} m²");
Console.WriteLine($"União dos dois quadrados: {Geo.GetGeoJsonType(union!)}");
