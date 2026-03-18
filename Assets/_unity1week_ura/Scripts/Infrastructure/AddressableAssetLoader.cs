using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Unity1Week_Ura.Infrastructure
{
    /// <summary>
    /// AssetReferenceからアセットをリフレクション経由で読み込む共通ユーティリティ。
    /// </summary>
    public static class AddressableAssetLoader
    {
        /// <summary>
        /// <see cref="AssetReference"/> から指定した型 <typeparamref name="T"/> のアセットを読み込む。
        /// 読み込み後は呼び出し側で <see cref="AssetReference.ReleaseAsset"/> を適切に呼ぶこと。
        /// </summary>
        public static async UniTask<T> LoadAsync<T>(AssetReference assetReference) where T : UnityEngine.Object
        {
            if (assetReference == null)
            {
                throw new ArgumentNullException(nameof(assetReference));
            }

            var loadMethod = typeof(AssetReference).GetMethod(nameof(AssetReference.LoadAssetAsync), Type.EmptyTypes);
            if (loadMethod == null)
            {
                throw new MissingMethodException(nameof(AssetReference), nameof(AssetReference.LoadAssetAsync));
            }

            var genericLoadMethod = loadMethod.MakeGenericMethod(typeof(T));
            var operation = genericLoadMethod.Invoke(assetReference, null);
            if (operation == null)
            {
                throw new InvalidOperationException($"Failed to start Addressable load operation for {typeof(T).Name}.");
            }

            var taskProperty = operation.GetType().GetProperty("Task");
            if (taskProperty == null)
            {
                throw new InvalidOperationException("Addressable operation does not provide Task property.");
            }

            var loadTask = taskProperty.GetValue(operation) as Task;
            if (loadTask == null)
            {
                throw new InvalidOperationException("Addressable load task is null.");
            }

            await loadTask.AsUniTask();

            var resultProperty = loadTask.GetType().GetProperty("Result");
            if (resultProperty == null)
            {
                throw new InvalidOperationException("Addressable load task does not have Result property.");
            }

            var result = resultProperty.GetValue(loadTask) as T;
            if (result == null)
            {
                throw new InvalidOperationException($"Addressable returned null {typeof(T).Name}.");
            }

            return result;
        }
    }
}
