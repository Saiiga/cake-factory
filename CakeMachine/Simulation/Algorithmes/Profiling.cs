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
        var postePréparation = usine.Préparateurs;
        var posteCuisson = usine.Fours.Single();
        var postesEmballages = usine.Emballeuses;

        while (!token.IsCancellationRequested)
        {
            var plats = usine.StockInfiniPlats.Take(5);

            var gâteauxCrus = plats.Select(postePréparation.First(poste => poste.PlacesRestantes != 0).PréparerAsync).EnumerateCompleted();
            var gâteauxCuits = CuireAsync(gâteauxCrus, posteCuisson, usine.OrganisationUsine.ParamètresCuisson.NombrePlaces);

            var gâteauxEmballés = EmballerAsync(gâteauxCuits, postesEmballages, usine.OrganisationUsine.ParamètresEmballage.NombrePlaces);

            await foreach (var gateauEmballe in gâteauxEmballés)
                yield return gateauEmballe;
        }
    }

    private static async IAsyncEnumerable<GâteauCuit> CuireAsync(IAsyncEnumerable<GâteauCru> gateauxCru, Cuisson posteCuisson, ushort nbPlaces)
    {
        List<GâteauCru> plaqueGateauxCru = new();
        var enumator = gateauxCru.GetAsyncEnumerator();
        while (await enumator.MoveNextAsync())
        {
            var gateauCru = enumator.Current;
            plaqueGateauxCru.Add(gateauCru);
            
            if (plaqueGateauxCru.Count == nbPlaces)
            {
                foreach (var gateau in await posteCuisson.CuireAsync(plaqueGateauxCru.ToArray()))
                {
                    yield return gateau;
                }
                plaqueGateauxCru.Clear();
            }
        }

        if (plaqueGateauxCru.Count > 0)
        {
            foreach (var gateau in await posteCuisson.CuireAsync(plaqueGateauxCru.ToArray()))
            {
                yield return gateau;
            }
        }
    }
    
    private static async IAsyncEnumerable<GâteauEmballé> EmballerAsync(IAsyncEnumerable<GâteauCuit> gateauxCuit, IEnumerable<Emballage> posteEmballages, ushort nbPlaces)
    {
        await foreach (var gateauCuit in gateauxCuit)
        {
            var gateauxEmballés =  await Task.WhenAll(posteEmballages.First(poste => poste.PlacesRestantes != 0).EmballerAsync(gateauCuit));

            foreach (var gateau in gateauxEmballés)
            {
                yield return gateau;
            }
        }
    }
}