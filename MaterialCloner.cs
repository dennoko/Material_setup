using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MaterialSetup
{
    /// <summary>
    /// マテリアルの複製と参照差し替えを行うユーティリティクラス
    /// バーサーカーモード: 同一シェーダーで初期値のマテリアルを新規作成して差し替え
    /// </summary>
    public static class MaterialCloner
    {
        private const string CloneFolderName = "clone";
        private static readonly Regex TrailingNumberPattern = new Regex(@"^(.+?)( \d+)?$", RegexOptions.Compiled);

        /// <summary>
        /// 指定したGameObjectとその子のメッシュに含まれるマテリアルを複製し、参照を差し替える
        /// </summary>
        /// <param name="target">対象のGameObject</param>
        /// <param name="asVariant">trueの場合、Material Variantとして作成</param>
        public static void CloneMaterials(GameObject target, bool asVariant = false)
        {
            if (target == null)
            {
                Debug.LogWarning("対象のGameObjectが指定されていません。");
                return;
            }

            // マテリアルとそれを使用しているRendererを収集
            var materialRenderers = CollectMaterials(target);

            if (materialRenderers.Count == 0)
            {
                Debug.Log("複製するマテリアルが見つかりませんでした。");
                return;
            }

            string operationName = asVariant ? "Material Variantを作成して差し替え" : "マテリアルを複製して差し替え";
            Undo.SetCurrentGroupName(operationName);
            int undoGroup = Undo.GetCurrentGroup();

            int clonedCount = 0;
            int skippedCount = 0;

            foreach (var kvp in materialRenderers)
            {
                try
                {
                    Material original = kvp.Key;
                    List<RendererMaterialSlot> slots = kvp.Value;

                    Material cloned = asVariant 
                        ? CreateAndSaveMaterialVariant(original) 
                        : CloneAndSaveMaterial(original);
                        
                    if (cloned != null)
                    {
                        ReplaceMaterialReferences(slots, original, cloned);
                        clonedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    skippedCount++;
                    Debug.LogWarning($"マテリアル '{kvp.Key?.name}' の処理中にエラーが発生しました。スキップします: {ex.Message}");
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            string operationType = asVariant ? "Material Variant作成" : "マテリアル複製";
            string resultMessage = $"{operationType}: {clonedCount}個成功";
            if (skippedCount > 0)
            {
                resultMessage += $", {skippedCount}個スキップ";
                Debug.LogWarning(resultMessage);
            }
            else
            {
                Debug.Log(resultMessage);
            }
        }

        /// <summary>
        /// バーサーカーモード: 対象マテリアルと同じシェーダーで初期値のマテリアルを新規作成し、差し替える
        /// </summary>
        /// <param name="target">対象のGameObject</param>
        public static void BerserkerSetup(GameObject target)
        {
            if (target == null)
            {
                Debug.LogWarning("対象のGameObjectが指定されていません。");
                return;
            }

            // マテリアルとそれを使用しているRendererを収集
            var materialRenderers = CollectMaterials(target);

            if (materialRenderers.Count == 0)
            {
                Debug.Log("セットアップするマテリアルが見つかりませんでした。");
                return;
            }

            // 確認ダイアログ
            int materialCount = materialRenderers.Count;
            if (!EditorUtility.DisplayDialog(
                "バーサーカーモード",
                $"{materialCount}個のマテリアルを初期値でセットアップします。\n" +
                "元のマテリアルのプロパティは引き継がれません。\n" +
                "実行しますか？",
                "実行", "キャンセル"))
            {
                return;
            }

            Undo.SetCurrentGroupName("バーサーカーモード: 初期値マテリアルで差し替え");
            int undoGroup = Undo.GetCurrentGroup();

            int createdCount = 0;
            int skippedCount = 0;

            foreach (var kvp in materialRenderers)
            {
                try
                {
                    Material original = kvp.Key;
                    List<RendererMaterialSlot> slots = kvp.Value;

                    Material fresh = CreateFreshMaterial(original);

                    if (fresh != null)
                    {
                        ReplaceMaterialReferences(slots, original, fresh);
                        createdCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    skippedCount++;
                    Debug.LogWarning($"マテリアル '{kvp.Key?.name}' の処理中にエラーが発生しました。スキップします: {ex.Message}");
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            string resultMessage = $"バーサーカーモード: {createdCount}個作成";
            if (skippedCount > 0)
            {
                resultMessage += $", {skippedCount}個スキップ";
                Debug.LogWarning(resultMessage);
            }
            else
            {
                Debug.Log(resultMessage);
            }
        }

        /// <summary>
        /// GameObjectとその子から全てのマテリアルとそれを使用しているRendererを収集する
        /// </summary>
        private static Dictionary<Material, List<RendererMaterialSlot>> CollectMaterials(GameObject target)
        {
            var result = new Dictionary<Material, List<RendererMaterialSlot>>();
            var renderers = target.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material mat = materials[i];
                    if (mat == null) continue;

                    // アセットとして保存されているマテリアルのみ対象
                    if (!AssetDatabase.Contains(mat)) continue;

                    if (!result.ContainsKey(mat))
                    {
                        result[mat] = new List<RendererMaterialSlot>();
                    }
                    result[mat].Add(new RendererMaterialSlot(renderer, i));
                }
            }

            return result;
        }

        /// <summary>
        /// マテリアルを複製して保存する
        /// </summary>
        private static Material CloneAndSaveMaterial(Material original)
        {
            string originalPath = AssetDatabase.GetAssetPath(original);
            if (string.IsNullOrEmpty(originalPath))
            {
                Debug.LogWarning($"マテリアル '{original.name}' のパスを取得できませんでした。");
                return null;
            }

            string destFolder = GetCloneFolderPath(originalPath);
            EnsureFolderExists(destFolder);

            // ユニークな名前を生成
            string baseName = GetBaseName(original.name);
            string uniqueName = GetUniqueMaterialName(baseName, destFolder);

            // マテリアルを複製
            Material cloned = Object.Instantiate(original);
            cloned.name = uniqueName;

            string destPath = $"{destFolder}/{uniqueName}.mat";
            AssetDatabase.CreateAsset(cloned, destPath);

            return AssetDatabase.LoadAssetAtPath<Material>(destPath);
        }

        /// <summary>
        /// Material Variantを作成して保存する
        /// </summary>
        private static Material CreateAndSaveMaterialVariant(Material original)
        {
            string originalPath = AssetDatabase.GetAssetPath(original);
            if (string.IsNullOrEmpty(originalPath))
            {
                Debug.LogWarning($"マテリアル '{original.name}' のパスを取得できませんでした。");
                return null;
            }

            string destFolder = GetCloneFolderPath(originalPath);
            EnsureFolderExists(destFolder);

            // ユニークな名前を生成
            string baseName = GetBaseName(original.name);
            string uniqueName = GetUniqueMaterialName(baseName, destFolder);

            // Material Variantを作成
            Material variant = new Material(original);
            variant.name = uniqueName;
            variant.parent = original;

            string destPath = $"{destFolder}/{uniqueName}.mat";
            AssetDatabase.CreateAsset(variant, destPath);

            return AssetDatabase.LoadAssetAtPath<Material>(destPath);
        }

        /// <summary>
        /// 同一シェーダーで初期値のマテリアルを新規作成して保存する
        /// </summary>
        private static Material CreateFreshMaterial(Material original)
        {
            string originalPath = AssetDatabase.GetAssetPath(original);
            if (string.IsNullOrEmpty(originalPath))
            {
                Debug.LogWarning($"マテリアル '{original.name}' のパスを取得できませんでした。");
                return null;
            }

            string destFolder = GetCloneFolderPath(originalPath);
            EnsureFolderExists(destFolder);

            // ユニークな名前を生成
            string baseName = GetBaseName(original.name);
            string uniqueName = GetUniqueMaterialName(baseName, destFolder);

            // 同じシェーダーで新規マテリアルを作成（プロパティは全てデフォルト値）
            Material fresh = new Material(original.shader);
            fresh.name = uniqueName;

            // RenderQueueはシェーダーのデフォルト値を使用
            fresh.renderQueue = -1;

            string destPath = $"{destFolder}/{uniqueName}.mat";
            AssetDatabase.CreateAsset(fresh, destPath);

            return AssetDatabase.LoadAssetAtPath<Material>(destPath);
        }

        /// <summary>
        /// フォルダが存在しない場合は作成する
        /// </summary>
        private static void EnsureFolderExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = Path.GetDirectoryName(folderPath).Replace("\\", "/");
                string newFolderName = Path.GetFileName(folderPath);
                AssetDatabase.CreateFolder(parentFolder, newFolderName);
            }
        }

        /// <summary>
        /// 複製先のフォルダパスを取得する
        /// 元マテリアルがcloneフォルダ内にある場合は同じフォルダ、そうでなければcloneサブフォルダ
        /// </summary>
        private static string GetCloneFolderPath(string originalPath)
        {
            string folder = Path.GetDirectoryName(originalPath).Replace("\\", "/");
            string folderName = Path.GetFileName(folder);

            // 既にcloneフォルダ内にある場合は同じフォルダを使用
            if (folderName.Equals(CloneFolderName, System.StringComparison.OrdinalIgnoreCase))
            {
                return folder;
            }

            return $"{folder}/{CloneFolderName}";
        }

        /// <summary>
        /// 名前の末尾にある「スペース+数字」パターンを除去してベース名を取得
        /// </summary>
        private static string GetBaseName(string name)
        {
            Match match = TrailingNumberPattern.Match(name);
            return match.Success ? match.Groups[1].Value : name;
        }

        /// <summary>
        /// 指定フォルダ内でユニークなマテリアル名を生成する
        /// </summary>
        private static string GetUniqueMaterialName(string baseName, string folderPath)
        {
            // フォルダ内の既存マテリアルを取得
            var existingNames = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string existingName = Path.GetFileNameWithoutExtension(path);
                string existingBase = GetBaseName(existingName);

                // ベース名が同じものを記録
                if (existingBase.Equals(baseName, System.StringComparison.OrdinalIgnoreCase))
                {
                    existingNames.Add(existingName);
                }
            }

            // ベース名そのものが使われていなければそれを使用
            if (!existingNames.Contains(baseName))
            {
                return baseName;
            }

            // 使用可能な番号を探す
            int number = 1;
            while (true)
            {
                string candidate = $"{baseName} {number}";
                if (!existingNames.Contains(candidate))
                {
                    return candidate;
                }
                number++;
            }
        }

        /// <summary>
        /// Rendererのマテリアル参照を差し替える
        /// </summary>
        private static void ReplaceMaterialReferences(List<RendererMaterialSlot> slots, Material original, Material replacement)
        {
            foreach (var slot in slots)
            {
                Undo.RecordObject(slot.Renderer, "マテリアル参照の差し替え");

                var materials = slot.Renderer.sharedMaterials;
                if (slot.SlotIndex < materials.Length && materials[slot.SlotIndex] == original)
                {
                    materials[slot.SlotIndex] = replacement;
                    slot.Renderer.sharedMaterials = materials;
                }
            }
        }

        /// <summary>
        /// Rendererとマテリアルスロットのインデックスを保持する構造体
        /// </summary>
        private struct RendererMaterialSlot
        {
            public Renderer Renderer { get; }
            public int SlotIndex { get; }

            public RendererMaterialSlot(Renderer renderer, int slotIndex)
            {
                Renderer = renderer;
                SlotIndex = slotIndex;
            }
        }
    }
}
