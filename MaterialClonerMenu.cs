using UnityEditor;
using UnityEngine;

namespace MaterialSetup
{
    /// <summary>
    /// ヒエラルキーコンテキストメニューへのマテリアル複製機能の統合
    /// </summary>
    public static class MaterialClonerMenu
    {
        [MenuItem("GameObject/マテリアルを複製して差し替え", false, 0)]
        private static void CloneMaterials()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected != null)
            {
                MaterialCloner.CloneMaterials(selected);
            }
        }

        [MenuItem("GameObject/マテリアルを複製して差し替え", true)]
        private static bool ValidateCloneMaterials()
        {
            return Selection.activeGameObject != null;
        }
    }
}
