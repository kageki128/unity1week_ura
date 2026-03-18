using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Unity1Week_Ura.Infrastructure
{
    public class AddressableSpriteLabelLoader
    {
        static readonly string[] imageExtensions = { ".png", ".jpg", ".jpeg" };

        readonly Dictionary<string, Dictionary<string, Sprite>> spritesByLabelKey = new(StringComparer.Ordinal);
        readonly SemaphoreSlim loadGate = new(1, 1);

        public async UniTask<Dictionary<string, Sprite>> LoadAllByLabelAsync(AssetLabelReference labelReference, CancellationToken ct)
        {
            if (labelReference == null || !labelReference.RuntimeKeyIsValid())
            {
                throw new InvalidOperationException("Addressable label is not assigned.");
            }

            var labelKey = labelReference.RuntimeKey.ToString();
            if (string.IsNullOrEmpty(labelKey))
            {
                throw new InvalidOperationException("Addressable label key is empty.");
            }

            if (spritesByLabelKey.TryGetValue(labelKey, out var cached))
            {
                return cached;
            }

            await loadGate.WaitAsync(ct).AsUniTask();
            try
            {
                if (spritesByLabelKey.TryGetValue(labelKey, out cached))
                {
                    return cached;
                }

                var loaded = await LoadByReflectionAsync(labelReference, ct);
                spritesByLabelKey[labelKey] = loaded;
                return loaded;
            }
            finally
            {
                loadGate.Release();
            }
        }

        async UniTask<Dictionary<string, Sprite>> LoadByReflectionAsync(AssetLabelReference labelReference, CancellationToken ct)
        {
            var loadMethod = FindLoadAssetsByLabelMethod();
            if (loadMethod == null)
            {
                throw new MissingMethodException(nameof(Addressables), nameof(Addressables.LoadAssetsAsync));
            }

            var genericLoadMethod = loadMethod.MakeGenericMethod(typeof(Sprite));
            var operation = genericLoadMethod.GetParameters().Length == 3
                ? genericLoadMethod.Invoke(null, new object[] { labelReference, null, true })
                : genericLoadMethod.Invoke(null, new object[] { labelReference, null });
            if (operation == null)
            {
                throw new InvalidOperationException("Failed to start Addressables sprite load operation.");
            }

            var taskProperty = operation.GetType().GetProperty("Task");
            if (taskProperty == null)
            {
                throw new InvalidOperationException("Addressables sprite operation does not provide Task property.");
            }

            var loadTask = taskProperty.GetValue(operation) as Task;
            if (loadTask == null)
            {
                throw new InvalidOperationException("Addressables sprite load task is null.");
            }

            await loadTask.AsUniTask().AttachExternalCancellation(ct);

            var resultProperty = loadTask.GetType().GetProperty("Result");
            if (resultProperty == null)
            {
                throw new InvalidOperationException("Addressables sprite load task does not have Result property.");
            }

            var result = resultProperty.GetValue(loadTask) as IEnumerable;
            if (result == null)
            {
                throw new InvalidOperationException("Addressables sprite load result is null.");
            }

            var spritesByFileName = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in result)
            {
                if (entry is not Sprite sprite || string.IsNullOrEmpty(sprite.name))
                {
                    continue;
                }

                spritesByFileName[sprite.name] = sprite;
                foreach (var extension in imageExtensions)
                {
                    spritesByFileName[$"{sprite.name}{extension}"] = sprite;
                }
            }

            return spritesByFileName;
        }

        static MethodInfo FindLoadAssetsByLabelMethod()
        {
            var methods = typeof(Addressables).GetMethods();
            foreach (var method in methods)
            {
                if (!method.IsGenericMethodDefinition || method.Name != nameof(Addressables.LoadAssetsAsync))
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length == 2
                    && parameters[0].ParameterType == typeof(object)
                    && parameters[1].ParameterType.IsGenericType)
                {
                    return method;
                }

                if (parameters.Length == 3
                    && parameters[0].ParameterType == typeof(object)
                    && parameters[1].ParameterType.IsGenericType
                    && parameters[2].ParameterType == typeof(bool))
                {
                    return method;
                }
            }

            return null;
        }

        /// <summary>
        /// ファイル名をキーにSprite辞書から対応するSpriteを引く。見つからない場合はnullを返す。
        /// </summary>
        public static Sprite ResolveSprite(string fileName, IReadOnlyDictionary<string, Sprite> spritesByFileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            if (!spritesByFileName.TryGetValue(fileName, out var sprite))
            {
                return null;
            }

            return sprite;
        }
    }
}
