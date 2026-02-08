using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SeaOfFallenStars.WorldData;
public class VassalInfoPanelManager : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button closeButton;

    [SerializeField] private Button openSettlementButton;

    private string _vassalSettlementId;

    private void Awake()
    {
        if (GetComponent<MapInputBlocker>() == null)
            gameObject.AddComponent<MapInputBlocker>();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => Destroy(gameObject));
        }

        if (openSettlementButton != null)
        {
            openSettlementButton.onClick.RemoveAllListeners();
            openSettlementButton.onClick.AddListener(() => MapNavigationUtil.OpenSettlementById(_vassalSettlementId));
        }
    }

    // Called by InfoWindowManager via reflection
    public void Initialize(SettlementInfoData liege, SettlementStatsCache.VassalComputedSummary vassal)
    {
        if (vassal == null) return;

        _vassalSettlementId = vassal.vassalSettlementId;

        if (titleText != null)
            titleText.text = vassal.vassalDisplayName;

        string ruler = "";
        var data = JsonDataLoader.TryLoadFromEitherPath<SettlementInfoData>(
            DataPaths.Runtime_MapDataPath,
            DataPaths.Editor_MapDataPath,
            vassal.vassalSettlementId
        );

        if (data != null)
        {
            if (!string.IsNullOrWhiteSpace(data.main?.rulerDisplayName)) ruler = data.main.rulerDisplayName;
            else if (!string.IsNullOrWhiteSpace(data.main?.rulerDisplayName)) ruler = data.main.rulerDisplayName;
            else if (!string.IsNullOrWhiteSpace(data.rulerCharacterId)) ruler = CharacterNameResolver.Resolve(data.rulerCharacterId);
        }

        string liegeName = liege != null
            ? (!string.IsNullOrWhiteSpace(liege.displayName) ? liege.displayName : liege.settlementId)
            : "";

        if (bodyText != null)
        {
            bodyText.text =
                $"Liege: {liegeName}\n" +
                $"Ruler: {ruler}\n\n" +
                $"Contract Terms:\n" +
                $"- Income Tax Rate: {(vassal.incomeTaxRate * 100f):0.#}%\n" +
                $"- Troop Tax Rate: {(vassal.troopTaxRate * 100f):0.#}%\n" +
                $"- Notes: {Safe(vassal.terms)}\n\n" +
                $"Totals:\n" +
                $"- Population: {vassal.vassalTotalPopulation:N0}\n" +
                $"- Income: Gross {vassal.vassalGrossIncome:N2}/mo | Pays {vassal.incomePaidUp:N2}/mo | Net {vassal.vassalNetIncome:N2}/mo\n" +
                $"- Troops: Gross {vassal.vassalGrossTroops:N0} | Pays {vassal.troopsPaidUp:N0} | Net {vassal.vassalNetTroops:N0}\n";
        }
    }

    private static string Safe(string s) => string.IsNullOrWhiteSpace(s) ? "" : s;
}
