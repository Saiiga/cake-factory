using System.Runtime.CompilerServices;
using CakeMachine.Fabrication.ContexteProduction;
using CakeMachine.Fabrication.Elements;
using CakeMachine.Fabrication.Opérations;

namespace CakeMachine.Simulation.Algorithmes;

internal class PremierPas : Algorithme
{
    /// <inheritdoc />
    public override bool SupportsSync => true;

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

    private static IEnumerable<GâteauEmballé> Test2(int nombreGateaux, List<GâteauCru> gateauCrus, Préparation postePréparation, Plat plat,
        Cuisson posteCuisson, List<GâteauEmballé> gateauEmballes, Emballage posteEmballage)
    {
        GâteauCuit gateauCuit = null;
        for (int nombreGateau = 0; nombreGateau < nombreGateaux; nombreGateau++)
        {
            gateauCrus.Add(postePréparation.Préparer(plat));

            if (nombreGateau % nombreGateaux == 1)
                gateauCuit = posteCuisson.Cuire(gateauCrus.ToArray()).Single();

            if (gateauCuit != null)
                gateauEmballes.Add(posteEmballage.Emballer(gateauCuit));
        }

        foreach (var gateau in gateauEmballes)
            yield return gateau;
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
}