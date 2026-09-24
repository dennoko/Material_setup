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
        private static void CloneMaterials(MenuCommand command)
        {
            if (!ShouldExecute(command)) return;

            GameObject[] selected = Selection.gameObjects;
            if (selected != null && selected.Length > 0)
            {
                MaterialCloner.CloneMaterials(selected, asVariant: false);
            }
        }

        [MenuItem(MenuPath + "複製+入れ替え", true)]
        private static bool ValidateCloneMaterials()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        [MenuItem(MenuPath + "複製+入れ替え（Material Variantとして作成）", false, 1)]
        private static void CloneMaterialsAsVariant(MenuCommand command)
        {
            if (!ShouldExecute(command)) return;

            GameObject[] selected = Selection.gameObjects;
            if (selected != null && selected.Length > 0)
            {
                MaterialCloner.CloneMaterials(selected, asVariant: true);
            }
        }

        [MenuItem(MenuPath + "複製+入れ替え（Material Variantとして作成）", true)]
        private static bool ValidateCloneMaterialsAsVariant()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        [MenuItem(MenuPath + "バーサーカーモード（初期値で新規作成）", false, 20)]
        private static void BerserkerSetup(MenuCommand command)
        {
            if (!ShouldExecute(command)) return;

            GameObject[] selected = Selection.gameObjects;
            if (selected != null && selected.Length > 0)
            {
                MaterialCloner.BerserkerSetup(selected);
            }
        }

        [MenuItem(MenuPath + "バーサーカーモード（初期値で新規作成）", true)]
        private static bool ValidateBerserkerSetup()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        /// <summary>
        /// 複数選択時にUnityがMenuItemコールバックを選択オブジェクト数分連続して呼び出すのを防ぎ、
        /// バッチ全体で1度だけ実行するように制御します。
        /// </summary>
        private static bool ShouldExecute(MenuCommand command)
        {
            // ヒエラルキーのコンテキストメニューから実行された場合、
            // command.context に各オブジェクトが渡されてオブジェクト数分呼び出されるため、
            // 選択配列の先頭オブジェクトの呼び出し時のみ実行し、残りはスキップする。
            if (command != null && command.context != null)
            {
                if (Selection.objects.Length > 0 && command.context != Selection.objects[0])
                {
                    return false;
                }
            }

            return true;
        }
    }
}

