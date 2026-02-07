using UnityEditor;
using UnityEngine;

namespace MaterialSetup
{
    /// <summary>
    /// ヒエラルキーコンテキストメニューへのマテリアル複製機能の統合
    /// </summary>
    public static class MaterialClonerMenu
    {
        private const string MenuPath = "GameObject/マテリアルを複製/";

        [MenuItem(MenuPath + "複製+入れ替え", false, 0)]
        private static void CloneMaterials()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected != null)
            {
                MaterialCloner.CloneMaterials(selected, asVariant: false);
            }
        }

        [MenuItem(MenuPath + "複製+入れ替え", true)]
        private static bool ValidateCloneMaterials()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem(MenuPath + "複製+入れ替え（Material Variantとして作成）", false, 1)]
        private static void CloneMaterialsAsVariant()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected != null)
            {
                MaterialCloner.CloneMaterials(selected, asVariant: true);
            }
        }

        [MenuItem(MenuPath + "複製+入れ替え（Material Variantとして作成）", true)]
        private static bool ValidateCloneMaterialsAsVariant()
        {
            return Selection.activeGameObject != null;
        }
    }
}
