using TMPro;
using UnityEngine;

namespace SupKonQuest
{
    public class CampGoldDisplay : MonoBehaviour
    {
        public Camp camp;
        public TextMeshPro textMesh;

        private void Update()
        {
            if (camp == null || textMesh == null) return;

            if (camp.owner == null)
            {
                textMesh.text = "Neutral";
            }
            else
            {
                textMesh.text = camp.owner.money.ToString() + " G";
            }
        }
    }
}