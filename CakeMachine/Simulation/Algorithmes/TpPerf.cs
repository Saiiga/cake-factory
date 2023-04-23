using CakeMachine.Fabrication.ContexteProduction;
using CakeMachine.Fabrication.Elements;
using CakeMachine.Fabrication.Opérations;
using CakeMachine.Utils;

namespace CakeMachine.Simulation.Algorithmes;

internal class TpPerf : Algorithme
{
    /// <inheritdoc />
    public override bool SupportsAsync => true;
    private int _nbPlatsARefaire = 0;

    public override void ConfigurerUsine(IConfigurationUsine builder)
    {
        builder.NombrePréparateurs = 10;
        builder.NombreFours = 6;
        builder.NombreEmballeuses = 15;
        
        base.ConfigurerUsine(builder);
    }

    public override async IAsyncEnumerable<GâteauEmballé> ProduireAsync(Usine usine, CancellationToken token)
    {
        
        var postePréparation = usine.Préparateurs.PoolTogether();
        var posteCuisson = usine.Fours.PoolTogether();
        var postesEmballages = usine.Emballeuses.PoolTogether();

        while (!token.IsCancellationRequested)
        {
            var plats = this._nbPlatsARefaire == 0
                ? usine.StockInfiniPlats.Take(usine.OrganisationUsine.NombreFours * usine.OrganisationUsine.ParamètresCuisson.NombrePlaces)
                : usine.StockInfiniPlats.Take(this._nbPlatsARefaire);
            
            this._nbPlatsARefaire = 0;

            var gateauxCrus = plats.AsParallel().Select(plat => postePréparation.ProduireAsync(plat, token)).EnumerateCompleted();
            var gateauCuits = CuireAsync(gateauxCrus, posteCuisson,  usine.OrganisationUsine.ParamètresCuisson.NombrePlaces, token);
            
            var tâchesEmballage = new List<Task<GâteauEmballé>>();
            
            await foreach (var gâteauCuit in gateauCuits.WithCancellation(token))
            {
                if(gâteauCuit.EstConforme)
                    tâchesEmballage.Add(postesEmballages.ProduireAsync(gâteauCuit, token));
                else
                    this._nbPlatsARefaire++;
            }

            await foreach (var gâteauEmballé in tâchesEmballage.EnumerateCompleted().WithCancellation(token))
                if(gâteauEmballé.EstConforme)
                    yield return gâteauEmballé;
                else
                    this._nbPlatsARefaire++;
        }
    }
    private async IAsyncEnumerable<GâteauCuit> CuireAsync(IAsyncEnumerable<GâteauCru> gateauxCru, IMachine<GâteauCru[], GâteauCuit[]> posteCuisson, ushort nbPlaces, CancellationToken token)
    {
        List<GâteauCru> plaqueGateauxCru = new();
        var enumator = gateauxCru.GetAsyncEnumerator();
        while (await enumator.MoveNextAsync())
        {
            var gateauCru = enumator.Current;
            if (!gateauCru.EstConforme)
            {
                this._nbPlatsARefaire++;
                continue;
            }
            plaqueGateauxCru.Add(gateauCru);
            
            if (plaqueGateauxCru.Count == nbPlaces)
            {
                foreach (var gateau in await posteCuisson.ProduireAsync(plaqueGateauxCru.ToArray(), token))
                {
                    yield return gateau;
                }
                plaqueGateauxCru.Clear();
            }
        }

        if (plaqueGateauxCru.Count > 0)
        {
            foreach (var gateau in await posteCuisson.ProduireAsync(plaqueGateauxCru.ToArray(), token))
            {
                yield return gateau;
            }
        }
    }

    private async IAsyncEnumerable<GâteauEmballé> EmballerAsync(IAsyncEnumerable<GâteauCuit> gateauxCuit,
        IMachine<GâteauCuit, GâteauEmballé> posteEmballages, CancellationToken token)
    {
        await foreach (var gateauCuit in gateauxCuit)
        {
            if (!gateauCuit.EstConforme)
            {
                this._nbPlatsARefaire++;
                continue;
            }
            var gateauEmballé = await posteEmballages.ProduireAsync(gateauCuit, token);
            
            yield return gateauEmballé;

        }
    }
}