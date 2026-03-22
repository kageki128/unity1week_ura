using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class SmartPhoneView : AnimationViewBase
    {
        // Title
        public Observable<Unit> OnStartButtonClicked => titlePhoneScreenView.OnStartButtonClicked;

        // Select
        public Observable<GameRuleSO> OnDifficultyButtonClicked => selectPhoneScreenView.OnDifficultyButtonClicked;
        public Observable<Unit> OnBackToTitleButtonClicked => selectPhoneScreenView.OnBackToTitleButtonClicked;

        // Game
        public Observable<Post> OnDraftDroppedToPublish => gamePhoneScreenView.OnDraftDroppedToPublish;
        public Observable<Account> OnPlayerAccountClicked => gamePhoneScreenView.OnPlayerAccountClicked;
        public Observable<Post> OnLikedByPlayer => gamePhoneScreenView.OnLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => gamePhoneScreenView.OnRepostedByPlayer;
        public Observable<Unit> OnSelectSceneButtonClicked => gamePhoneScreenView.OnSelectSceneButtonClicked;
        public Observable<Unit> OnRestartButtonClicked => gamePhoneScreenView.OnRestartButtonClicked;

        // Result
        public Observable<Unit> OnRetryButtonClicked => resultPhoneScreenView.OnRetryButtonClicked;
        public Observable<Unit> OnBackToSelectButtonClicked => resultPhoneScreenView.OnBackToSelectButtonClicked;
        public Observable<Unit> OnShareButtonClicked => resultPhoneScreenView.OnShareButtonClicked;


        [SerializeField] TitlePhoneScreenView titlePhoneScreenView;
        [SerializeField] SelectPhoneScreenView selectPhoneScreenView;
        [SerializeField] GamePhoneScreenView gamePhoneScreenView;
        [SerializeField] ResultPhoneScreenView resultPhoneScreenView;

        [SerializeField] ScreenTransitionViewHub screenTransitionViewHub;

        [SerializeField] Transform titleShowTransform;
        [SerializeField] Transform titleHideTransform;
        [SerializeField] Transform selectShowTransform;
        [SerializeField] Transform selectHideTransform;
        [SerializeField] Transform gameShowTransform;
        [SerializeField] Transform gameHideTransform;
        [SerializeField] Transform resultShowTransform;
        [SerializeField] Transform resultHideTransform;

        [SerializeField, Min(0f)] float AnimationDuration = 0.5f;
        [SerializeField] Ease AnimationEase = Ease.OutExpo;

        readonly Dictionary<SceneType, PhoneScreenViewBase> screenViews = new();
        readonly Dictionary<SceneType, Transform> showSceneTargets = new();
        readonly Dictionary<SceneType, Transform> hideSceneTargets = new();

        Tween currentTween;

        public override void Initialize()
        {
            screenViews.Clear();
            screenViews.Add(SceneType.Title, titlePhoneScreenView);
            screenViews.Add(SceneType.Select, selectPhoneScreenView);
            screenViews.Add(SceneType.Game, gamePhoneScreenView);
            screenViews.Add(SceneType.Result, resultPhoneScreenView);

            showSceneTargets.Clear();
            showSceneTargets.Add(SceneType.Title, titleShowTransform);
            showSceneTargets.Add(SceneType.Select, selectShowTransform);
            showSceneTargets.Add(SceneType.Game, gameShowTransform);
            showSceneTargets.Add(SceneType.Result, resultShowTransform);

            hideSceneTargets.Clear();
            hideSceneTargets.Add(SceneType.Title, titleHideTransform);
            hideSceneTargets.Add(SceneType.Select, selectHideTransform);
            hideSceneTargets.Add(SceneType.Game, gameHideTransform);
            hideSceneTargets.Add(SceneType.Result, resultHideTransform);

            foreach (var screenView in screenViews.Values)
            {
                screenView.Initialize(screenTransitionViewHub);
            }

            screenTransitionViewHub.Initialize();
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            await UniTask.CompletedTask;
        }
        public override async UniTask HideAsync(CancellationToken ct)
        {
            gameObject.SetActive(false);
            await UniTask.CompletedTask;
        }

        public async UniTask ShowSceneAsync(SceneType sceneType, CancellationToken ct)
        {
            var screenView = GetScreenView(sceneType);
            var targetTransform = GetShowTargetTransform(sceneType);

            var animationTask = targetTransform != null
                ? AnimateToTargetAsync(targetTransform, AnimationDuration, AnimationEase, ct)
                : UniTask.CompletedTask;

            await UniTask.WhenAll(
                screenView.ShowAsync(ct),
                animationTask);
        }

        public async UniTask HideSceneAsync(SceneType sceneType, CancellationToken ct)
        {
            var screenView = GetScreenView(sceneType);
            var targetTransform = GetHideTargetTransform(sceneType);

            var animationTask = targetTransform != null
                ? AnimateToTargetAsync(targetTransform, AnimationDuration, AnimationEase, ct)
                : UniTask.CompletedTask;

            await UniTask.WhenAll(
                screenView.HideAsync(ct),
                animationTask);
        }

        public void AddPostToTimeline(Post post) => gamePhoneScreenView.AddPost(post);
        public void ClearTimeline() => gamePhoneScreenView.ClearPosts();
        public void SetPlayerAccounts(IReadOnlyList<Account> accounts) => gamePhoneScreenView.SetPlayerAccounts(accounts);
        public void SetSelectedPlayerAccount(Account account) => gamePhoneScreenView.SetSelectedPlayerAccount(account);

        PhoneScreenViewBase GetScreenView(SceneType sceneType)
        {
            if (screenViews.TryGetValue(sceneType, out var screenView))
            {
                return screenView;
            }

            throw new KeyNotFoundException($"Screen view for scene type {sceneType} not found.");
        }

        Transform GetShowTargetTransform(SceneType sceneType)
        {
            if (showSceneTargets.TryGetValue(sceneType, out var targetTransform))
            {
                return targetTransform;
            }

            throw new KeyNotFoundException($"Show target transform for scene type {sceneType} not found.");
        }

        Transform GetHideTargetTransform(SceneType sceneType)
        {
            if (hideSceneTargets.TryGetValue(sceneType, out var targetTransform))
            {
                return targetTransform;
            }

            throw new KeyNotFoundException($"Hide target transform for scene type {sceneType} not found.");
        }

        async UniTask AnimateToTargetAsync(Transform targetTransform, float duration, Ease ease, CancellationToken ct)
        {
            KillCurrentTween();

            if (duration <= 0f)
            {
                transform.localPosition = targetTransform.localPosition;
                transform.localRotation = targetTransform.localRotation;
                transform.localScale = targetTransform.localScale;
                return;
            }

            var sequence = DOTween.Sequence()
                .Join(transform.DOLocalMove(targetTransform.localPosition, duration))
                .Join(transform.DOLocalRotateQuaternion(targetTransform.localRotation, duration))
                .Join(transform.DOScale(targetTransform.localScale, duration))
                .SetEase(ease);

            currentTween = sequence;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            try
            {
                await sequence.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                KillCurrentTween();
                throw;
            }
            finally
            {
                if (currentTween == sequence)
                {
                    currentTween = null;
                }
            }
        }

        void KillCurrentTween()
        {
            if (currentTween == null)
            {
                return;
            }

            if (currentTween.IsActive())
            {
                currentTween.Kill();
            }

            currentTween = null;
        }

        void OnDestroy()
        {
            KillCurrentTween();
        }
    }
}