using System.Collections.Generic;
using HarmonyLib;
using TUNING;

namespace CraftableDataBanks
{
    // Craftable Data Banks (ORIGINAL mod, 2026-07-11). In the base game the ONLY
    // source of Research Data Banks (the currency of gamma/space research) is a
    // rocket Research Module landing: 10 banks per flight, against tech costs of
    // 200-1600 points. This adds a Crafting Station recipe that fabricates them
    // on the ground at rocket-parity cost: 40 kg Plastic + 40 kg Refined Metal
    // -> 10 Data Banks (1 bank = 1 kg = 1 gamma point at the Cosmic Research
    // Center). The recipe unlocks with the same tech that unlocks the Cosmic
    // Research Center itself, so it appears exactly when data banks become
    // useful. DatabankHelper.TAG picks the right databank item for the DLC
    // state (ResearchDatabank in the base game).
    public static class CraftableDataBanksPatches
    {
        private const float PlasticKg = 40f;
        private const float NiobiumKg = 40f;
        private const float DataBanksProduced = 10f;

        [HarmonyPatch(typeof(CraftingTableConfig), "ConfigureRecipes")]
        public static class CraftingTableConfig_ConfigureRecipes_Patch
        {
            public static void Postfix()
            {
                ComplexRecipe.RecipeElement[] ingredients = new[]
                {
                    new ComplexRecipe.RecipeElement(
                        SimHashes.Polypropylene.CreateTag(), PlasticKg,
                        ComplexRecipe.RecipeElement.TemperatureOperation.Heated),
                    new ComplexRecipe.RecipeElement(
                        SimHashes.Niobium.CreateTag(), NiobiumKg,
                        ComplexRecipe.RecipeElement.TemperatureOperation.Heated),
                };
                ComplexRecipe.RecipeElement[] results = new[]
                {
                    new ComplexRecipe.RecipeElement(
                        DatabankHelper.TAG, DataBanksProduced,
                        ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature),
                };

                _ = new ComplexRecipe(
                    ComplexRecipeManager.MakeRecipeID(CraftingTableConfig.ID, ingredients, results),
                    ingredients, results)
                {
                    time = INDUSTRIAL.RECIPES.STANDARD_FABRICATION_TIME * 2f,
                    description = string.Format(
                        "Encodes colony findings onto storage media, producing {0} without a rocket flight.",
                        DatabankHelper.NAME_PLURAL),
                    nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                    fabricators = new List<Tag> { CraftingTableConfig.ID },
                    requiredTech = TechIdThatUnlocks("CosmicResearchCenter"),
                    sortOrder = 5,
                };
            }
        }

        // NOTE: ComplexRecipe ingredients must be CONCRETE element tags — the
        // recipe manager's DeriveRecipiesFromSource keeps only materials that
        // resolve to a prefab, and a category tag like GameTags.RefinedMetal
        // resolves to none, silently deriving ZERO recipes (the v1 bug). An
        // any-refined-metal Tag[] worked but its material dropdown ran off the
        // top of the screen, so the metal is pinned to Niobium — the space
        // metal, which keeps data banks flavored as space research.

        // The tech gate is looked up at runtime off the building it should march
        // with (the Buildable Geysers / Insulated Door lesson: hardcoded tech ids
        // rot when Klei reshuffles the tree). Null = ungated, a safe fallback.
        private static string TechIdThatUnlocks(string buildingId)
        {
            foreach (Tech tech in Db.Get().Techs.resources)
            {
                if (tech.unlockedItemIDs.Contains(buildingId))
                {
                    return tech.Id;
                }
            }
            return null;
        }
    }
}
