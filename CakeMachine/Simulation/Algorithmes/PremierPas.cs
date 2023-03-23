using System.Data.SqlTypes;
using System.Runtime.CompilerServices;
using CakeMachine.Fabrication.ContexteProduction;
using CakeMachine.Fabrication.Elements;
using CakeMachine.Fabrication.Opérations;

namespace CakeMachine.Simulation.Algorithmes;

internal class PremierPas : Algorithme
{
    /// <inheritdoc />
    public override bool SupportsSync => true;
    public override bool SupportsAsync => true;

    /// <inheritdoc />
    public override IEnumerable<GâteauEmballé> Produire(Usine usine, CancellationToken token)
    {
        var postePréparation = usine.Préparateurs.Single();
        var posteCuisson = usine.Fours.Single();
        var posteEmballage = usine.Emballeuses.Single();

        var gateauCrus = new List<GâteauCru>();
        var gateauEmballes = new List<GâteauEmballé>();

        int nombreGateaux = 2;

        while (!token.IsCancellationRequested)
        {
            gateauCrus.Clear();
            gateauEmballes.Clear();

            var plat = usine.StockInfiniPlats.First();
            
            //foreach (var gâteauEmballé in Test2(nombreGateaux, gateauCrus, postePréparation, plat, posteCuisson, gateauEmballes, posteEmballage)) yield return gâteauEmballé;
            foreach (var gâteauEmballé in Test1(nombreGateaux, gateauCrus, postePréparation, plat, posteCuisson, gateauEmballes, posteEmballage)) yield return gâteauEmballé;
        }
    }

    private static IEnumerable<GâteauEmballé> Test1(int nombreGateaux, List<GâteauCru> gateauCrus,
        Préparation postePréparation, Plat plat,
        Cuisson posteCuisson, List<GâteauEmballé> gateauEmballes, Emballage posteEmballage)
    {
        for (int nombreGateauxCru = 0; nombreGateauxCru < nombreGateaux; nombreGateauxCru++)
        {
            gateauCrus.Add(postePréparation.Préparer(plat));
        }

        var gateauCuit = posteCuisson.Cuire(gateauCrus.ToArray()).Single();

        for (int nombreGateauxCuit = 0; nombreGateauxCuit < nombreGateaux; nombreGateauxCuit++)
        {
            gateauEmballes.Add(posteEmballage.Emballer(gateauCuit));
        }

        foreach (var gateau in gateauEmballes)
            yield return gateau;
    }

    public override async IAsyncEnumerable<GâteauEmballé> ProduireAsync(Usine usine, [EnumeratorCancellation] CancellationToken token)
    {
        var postePréparation = usine.Préparateurs.Single();
        var posteCuisson = usine.Fours.Single();
        var posteEmballage = usine.Emballeuses.Single();

        while (!token.IsCancellationRequested)
        {
            // yield return await unParUnAsync(usine, postePréparation, posteCuisson, posteEmballage);
            // await foreach (var p in deuxParDeuxAsync(usine, postePréparation, posteCuisson, posteEmballage)) yield return p;

            var plats = usine.StockInfiniPlats.Take(10).ToArray();
            var gateauxCru = await Task.WhenAll(plats.Select(postePréparation.PréparerAsync));
            
            var gateauxCuit = await posteCuisson.CuireAsync(gateauxCru.Take(5).ToArray());
            var gateauxCuit2 = await posteCuisson.CuireAsync(gateauxCru.Skip(5).Take(5).ToArray());
            
            gateauxCuit = gateauxCuit.Concat(gateauxCuit2).ToArray();
            
            var gateauEmballer = await Task.WhenAll(gateauxCuit.Select(posteEmballage.EmballerAsync));

            foreach (var gateau in gateauEmballer)
                yield return gateau;
        }
    }

    private static async IAsyncEnumerable<GâteauEmballé> deuxParDeuxAsync(Usine usine, Préparation postePréparation, Cuisson posteCuisson,
        Emballage posteEmballage)
    {
        var plats = usine.StockInfiniPlats.Take(2).ToArray();
        var gateauxCru = await Task.WhenAll(plats.Select(postePréparation.PréparerAsync));
        var gateauxCuit = await posteCuisson.CuireAsync(gateauxCru);
        var gateauEmballer = await Task.WhenAll(gateauxCuit.Select(posteEmballage.EmballerAsync));

        foreach (var gateau in gateauEmballer)
            yield return gateau;
    }

    private static async Task<GâteauEmballé> unParUnAsync(Usine usine, Préparation postePréparation, Cuisson posteCuisson,
        Emballage posteEmballage)
    {
        var plat = usine.StockInfiniPlats.First();

        var gateauPreparer = await postePréparation.PréparerAsync(plat);
        var gateauCuisson = await posteCuisson.CuireAsync(gateauPreparer);
        var gateauEmballer = await posteEmballage.EmballerAsync(gateauCuisson.First());

        return gateauEmballer;
    }
}