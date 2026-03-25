using System;
using System.Threading;
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
        public static async UniTask<T> LoadAsync<T>(AssetReference assetReference, CancellationToken ct) where T : UnityEngine.Object
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

            Debug.Log($"[U1W-DIAG][AA-010] AddressableAssetLoader start type={typeof(T).Name} key={assetReference.RuntimeKey}");

            await WaitForOperationAsync(operation, ct);
            Debug.Log($"[U1W-DIAG][AA-011] AddressableAssetLoader operation done type={typeof(T).Name}");

            var resultProperty = operation.GetType().GetProperty("Result");
            if (resultProperty == null)
            {
                throw new InvalidOperationException("Addressable operation does not have Result property.");
            }

            var result = resultProperty.GetValue(operation) as T;
            if (result == null)
            {
                throw new InvalidOperationException($"Addressable returned null {typeof(T).Name}.");
            }

            Debug.Log($"[U1W-DIAG][AA-012] AddressableAssetLoader result ready type={typeof(T).Name}");

            return result;
        }

        static async UniTask WaitForOperationAsync(object operation, CancellationToken ct)
        {
            var operationType = operation.GetType();
            var isDoneProperty = operationType.GetProperty("IsDone");
            if (isDoneProperty == null)
            {
                throw new InvalidOperationException("Addressable operation does not have IsDone property.");
            }

            while (!(bool)isDoneProperty.GetValue(operation))
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }
    }
}
