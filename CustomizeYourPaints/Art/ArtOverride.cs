using KSerialization;
using System.Collections.Generic;
using UnityEngine;

namespace CustomizeYourPaints
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class ArtOverride : KMonoBehaviour
    {
        public bool IsCustomPaintStage(string id)
        {
            return id != null && CustomizeYourPaints.myOverrides.Contains(id);
        }

        public void UpdateOverride(string newId)
        {
            overrideStage = IsCustomPaintStage(newId) ? newId : null;
        }

        [Serialize]
        public string overrideStage;

        [SerializeField]
        public List<string> customExtraStages;
    }
}
