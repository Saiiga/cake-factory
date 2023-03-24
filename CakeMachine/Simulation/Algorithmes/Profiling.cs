using System.Runtime.CompilerServices;
using CakeMachine.Fabrication.ContexteProduction;
using CakeMachine.Fabrication.Elements;
using CakeMachine.Fabrication.Opérations;
using CakeMachine.Utils;

namespace CakeMachine.Simulation.Algorithmes;

internal class Profiling : Algorithme
{
    /// <inheritdoc />
    public override bool SupportsSync => true;

    /// <inheritdoc />
    public override bool SupportsAsync => true;

    public override void ConfigurerUsine(IConfigurationUsine builder)
    {
        builder.NombreEmballeuses = 2;
        base.ConfigurerUsine(builder);
        
    }

    /// <inheritdoc />
    public override IEnumerable<GâteauEmballé> Produire(Usine usine, CancellationToken token)
    {
        var postePréparation = usine.Préparateurs.Single();
        var posteCuisson = usine.Fours.Single();
        var posteEmballage = usine.Emballeuses.Single();

        while (!token.IsCancellationRequested)
        {
            var plat = usine.StockInfiniPlats.First();

            var gâteauCru = postePréparation.Préparer(plat);
            var gâteauCuit = posteCuisson.Cuire(gâteauCru).Single();
            var gâteauEmballé = posteEmballage.Emballer(gâteauCuit);
                
            yield return gâteauEmballé;
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<GâteauEmballé> ProduireAsync(
        Usine usine,
        [EnumeratorCancellation] CancellationToken token)
    {
        var postePréparation = usine.Préparateurs.Single();
        var posteCuisson = usine.Fours.Single();
        var postesEmballages = usine.Emballeuses.Take(2);

        while (!token.IsCancellationRequested)
        {
            var plats = usine.StockInfiniPlats.Take(5);

            var gâteauxCrus = await Task.WhenAll(plats.Select(postePréparation.PréparerAsync));
            var gâteauxCuits = (await posteCuisson.CuireAsync(gâteauxCrus));

            var gâteauxEmballés = await Task.WhenAll(gâteauxCuits.Select(postesEmballages.First(poste => poste.PlacesRestantes != 0).EmballerAsync));

           
            foreach (var gateauEmballe in gâteauxEmballés)
                yield return gateauEmballe;
        }
    }

    private static async IAsyncEnumerable<GâteauCuit> CuireAsync(IAsyncEnumerable<GâteauCru> gateauxCru, List<GâteauCru> bufferGateauxCru, Cuisson posteCuisson)
    {
        await foreach (var gateauCru in gateauxCru)
        {
            bufferGateauxCru.Add(gateauCru);
            if (bufferGateauxCru.Count == 5)
            {
                var gateauxCuit =  await posteCuisson.CuireAsync(bufferGateauxCru.ToArray());

                foreach (var gateau in gateauxCuit)
                {
                    yield return gateau;
                }
                bufferGateauxCru.Clear();
            }
        }
    }
}