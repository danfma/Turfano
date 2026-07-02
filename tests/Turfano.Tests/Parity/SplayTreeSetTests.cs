using Turfano.GeoJson.Polyclip;

namespace Turfano.Tests;

// T003 — fundação do porte do polyclip: semântica do splaytree-ts.
public class SplayTreeSetTests
{
    private sealed record Box(int Key, string Tag);

    private static SplayTreeSet<Box> NewSet() => new((a, b) => a.Key.CompareTo(b.Key));

    [Test]
    public async Task Add_First_Delete_KeepOrderedSemantics()
    {
        var set = NewSet();
        foreach (var key in new[] { 5, 1, 9, 3, 7 })
            await Assert.That(set.Add(new Box(key, "a"))).IsTrue();

        await Assert.That(set.Count).IsEqualTo(5);
        await Assert.That(set.Add(new Box(3, "duplicado"))).IsFalse(); // empate não insere
        await Assert.That(set.Count).IsEqualTo(5);
        await Assert.That(set.First().Key).IsEqualTo(1);

        await Assert.That(set.Delete(new Box(1, "x"))).IsTrue();
        await Assert.That(set.First().Key).IsEqualTo(3);
        await Assert.That(set.Delete(new Box(42, "x"))).IsFalse();
        await Assert.That(set.Count).IsEqualTo(4);
    }

    [Test]
    public async Task AddAndReturn_ReturnsExistingInstanceOnTie()
    {
        var set = NewSet();
        var original = new Box(10, "original");
        await Assert.That(ReferenceEquals(set.AddAndReturn(original), original)).IsTrue();

        var duplicate = new Box(10, "novo");
        var returned = set.AddAndReturn(duplicate);
        await Assert.That(ReferenceEquals(returned, original)).IsTrue(); // devolve o EXISTENTE
        await Assert.That(set.Count).IsEqualTo(1);
    }

    [Test]
    public async Task LastBefore_And_FirstAfter_AreStrict()
    {
        var set = NewSet();
        foreach (var key in new[] { 10, 20, 30, 40 })
            set.Add(new Box(key, "a"));

        // estritamente menor/maior — um elemento IGUAL não conta
        await Assert.That(set.LastBefore(new Box(30, "x"))!.Key).IsEqualTo(20);
        await Assert.That(set.FirstAfter(new Box(30, "x"))!.Key).IsEqualTo(40);

        // chave inexistente entre elementos
        await Assert.That(set.LastBefore(new Box(25, "x"))!.Key).IsEqualTo(20);
        await Assert.That(set.FirstAfter(new Box(25, "x"))!.Key).IsEqualTo(30);

        // fora das pontas
        await Assert.That(set.LastBefore(new Box(10, "x"))).IsNull();
        await Assert.That(set.FirstAfter(new Box(40, "x"))).IsNull();
    }

    [Test]
    public async Task StressAgainstSortedSetSemantics()
    {
        var set = NewSet();
        var reference = new SortedDictionary<int, Box>();
        var random = new Random(42);

        for (var i = 0; i < 2000; i++)
        {
            var key = random.Next(0, 300);
            var box = new Box(key, i.ToString());
            switch (random.Next(3))
            {
                case 0:
                    var added = set.Add(box);
                    await Assert.That(added).IsEqualTo(reference.TryAdd(key, box));
                    break;
                case 1:
                    var deleted = set.Delete(new Box(key, "probe"));
                    await Assert.That(deleted).IsEqualTo(reference.Remove(key));
                    break;
                default:
                    var probe = new Box(key, "probe");
                    var expectedBefore = reference.Keys.Where(k => k < key).Cast<int?>().LastOrDefault();
                    var expectedAfter = reference.Keys.Where(k => k > key).Cast<int?>().FirstOrDefault();
                    await Assert.That(set.LastBefore(probe)?.Key).IsEqualTo(expectedBefore);
                    await Assert.That(set.FirstAfter(probe)?.Key).IsEqualTo(expectedAfter);
                    break;
            }
            await Assert.That(set.Count).IsEqualTo(reference.Count);
        }
    }
}
