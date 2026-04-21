using UnityEngine;

public class CharacterSelect : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private GameObject[] characters;   // 11 characters
    [SerializeField] private GameObject[] indicators;   // 11 indicators

    private void Start()
    {
        // Optional: select the first character by default
        SelectCharacter(0);
    }

    public void SelectCharacter(int index)
    {
        if (characters == null || indicators == null)
        {
            Debug.LogWarning("Characters or Indicators array is not assigned.");
            return;
        }

        if (characters.Length != indicators.Length)
        {
            Debug.LogWarning("Characters and Indicators arrays must be the same length.");
            return;
        }

        if (index < 0 || index >= characters.Length)
        {
            Debug.LogWarning("Selected index is out of range: " + index);
            return;
        }

        for (int i = 0; i < characters.Length; i++)
        {
            bool isSelected = (i == index);

            if (characters[i] != null)
                characters[i].SetActive(isSelected);

            if (indicators[i] != null)
                indicators[i].SetActive(isSelected);
        }

        CharacterSelectSingleton.Instance.skin = index;
    }
}