using System.Collections.Generic;
using DG.Tweening;
using R3;
using TMPro;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity1Week_Ura.Actor
{
    public class PostView : MonoBehaviour
    {
        public Post post { get; private set; }
        public float Width => viewArranger.Width;
        public float Height => viewArranger.Height;
        public Observable<Post> OnPostClicked => onPostClicked;
        public Observable<Post> OnLikedByPlayer => onLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => onRepostedByPlayer;
        public Observable<PointerEventData> OnScrolled => onScrolled;

        [Header("Layout")]
        [SerializeField] ViewArranger viewArranger;
        [SerializeField] float defaultContentHeight = 0.6f;

        [Header("Main Visual")]
        [SerializeField] SpriteRenderer frameImage;
        [SerializeField] SpriteRenderer iconImage;

        [Header("Top Edge Object")]
        [SerializeField] Transform topEdgeObject;

        [Header("Repost Header")]
        [SerializeField] GameObject repostedHeaderIcon;

        [Header("Text")]
        [SerializeField] TMP_Text headerText;
        [SerializeField] TMP_Text repostedByText;
        [SerializeField] TMP_Text advertisementText;
        [SerializeField] TMP_Text contentText;
        [SerializeField] TMP_Text repostCountText;
        [SerializeField] TMP_Text likeCountText;

        [Header("Attached Image")]
        [SerializeField] SpriteRenderer attachedImageRenderer;
        [SerializeField] float attachedImageMaxWidth = 8.5f;
        [SerializeField] float attachedImageTopMargin = 0.2f;

        [Header("Text Style")]
        [SerializeField] Color subTextColor;
        [SerializeField] int subTextFontSizeOffset = 3;
        [SerializeField] Color repltTextColor = new Color32(0xEE, 0x5A, 0x7F, 0xFF);

        [Header("Action Buttons")]
        [SerializeField] ButtonView postButtonView;
        [SerializeField] ButtonView repostButtonView;
        [SerializeField] ButtonView likeButtonView;
        [SerializeField] SpriteRenderer repostIconImage;
        [SerializeField] SpriteRenderer likeIconImage;

        [Header("Action Colors")]
        [SerializeField] Color actionIconDefaultColor = new Color32(0x53, 0x64, 0x71, 0xFF);
        [SerializeField] Color repostActiveColor = new Color32(0x00, 0xBA, 0x7C, 0xFF);
        [SerializeField] Color likeActiveColor = new Color32(0xF9, 0x18, 0x80, 0xFF);

        readonly CompositeDisposable disposables = new();
        readonly Subject<Post> onPostClicked = new();
        readonly Subject<Post> onLikedByPlayer = new();
        readonly Subject<Post> onRepostedByPlayer = new();
        readonly Subject<UnityEngine.EventSystems.PointerEventData> onScrolled = new();
        readonly List<ButtonColliderClipTarget> buttonColliderClipTargets = new();
        bool hasAttachedImage;
        float currentAttachedImageHeight;
        Vector3 attachedImageBaseLocalScale = Vector3.one;
        bool hasAttachedImageBaseLocalScale;
        bool isInteractable = true;
        float interactionClipTopY = float.PositiveInfinity;
        float interactionClipBottomY = float.NegativeInfinity;

        struct ButtonColliderClipTarget
        {
            public BoxCollider2D Collider;
            public Vector2 BaseSize;
            public Vector2 BaseOffset;
        }

        public void Initialize(Post post)
        {
            disposables.Clear();
            this.post = post;

            var property = post.Property;
            var subTextColorHex = $"#{ColorUtility.ToHtmlStringRGBA(subTextColor)}";
            var repltTextColorHex = $"#{ColorUtility.ToHtmlStringRGBA(repltTextColor)}";

            iconImage.sprite = property.Author.Icon;
            var publishDateText = post.PublishDateTime.ToString("M月d日");
            headerText.text = $"{property.Author.Name}<size={subTextFontSizeOffset}><color={subTextColorHex}>　@{property.Author.Id}　{publishDateText}</color></size>";

            advertisementText.gameObject.SetActive(property.Author.Type == AccountType.Advertise);

            if (string.IsNullOrEmpty(property.ParentPostId))
            {
                contentText.text = property.Text;
            }
            else
            {
                var parentAccountId = property.ParentPostAuthor?.Id;
                if (string.IsNullOrEmpty(parentAccountId))
                {
                    parentAccountId = property.ParentPostId;
                }

                var replyText = $"返信先: @{parentAccountId} さん";
                contentText.text = $"<color={repltTextColorHex}>{replyText}</color>\n{property.Text}";
            }
            SetupAttachedImage(property.AttachedImage);
            RefreshRepostedByText();
            SubscribePostState();
            SubscribeActions();

            AdjustLayout();
            InitializeButtonColliderClipTargets();
            ApplyButtonInteractionState();
        }

        public void SetPosition(float x, float y, bool useAnimation = true)
        {
            viewArranger.SetPosition(x, y, useAnimation);
        }

        public void SetInteractable(bool isInteractable)
        {
            this.isInteractable = isInteractable;
            ApplyButtonInteractionState();
        }

        public void SetInteractionClip(float topY, float bottomY)
        {
            interactionClipTopY = topY;
            interactionClipBottomY = bottomY;
            ApplyButtonInteractionState();
        }

        public void StopAnimations()
        {
            if (viewArranger != null)
            {
                viewArranger.StopAnimations();
            }

            var buttonAnimators = GetComponentsInChildren<ButtonAnimator>(true);
            for (var i = 0; i < buttonAnimators.Length; i++)
            {
                var buttonAnimator = buttonAnimators[i];
                if (buttonAnimator == null)
                {
                    continue;
                }

                buttonAnimator.StopAnimations();
            }

            var transforms = GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var target = transforms[i];
                if (target == null)
                {
                    continue;
                }

                DOTween.Kill(target);
            }

            var spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                var target = spriteRenderers[i];
                if (target == null)
                {
                    continue;
                }

                DOTween.Kill(target);
            }

            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                var target = texts[i];
                if (target == null)
                {
                    continue;
                }

                DOTween.Kill(target);
            }
        }

        void SubscribePostState()
        {
            post.IsRepostedByPlayer.Subscribe(_ => RefreshActionViews()).AddTo(disposables);
            post.IsLikedByPlayer.Subscribe(_ => RefreshActionViews()).AddTo(disposables);
        }

        void AdjustLayout()
        {
            // テキストメッシュを強制更新して実際の描画サイズを取得
            contentText.ForceMeshUpdate();
            float renderedHeight = contentText.renderedHeight;

            // 1行時のデフォルト高さとの差分を計算
            float extraHeight = Mathf.Max(0f, renderedHeight - defaultContentHeight);
            if (hasAttachedImage)
            {
                extraHeight += attachedImageTopMargin + currentAttachedImageHeight;
            }
            
            float topExtraHeight = 0f;
            if (repostedByText.gameObject.activeSelf)
            {
                repostedByText.ForceMeshUpdate();
                float frameTop = frameImage.transform.localPosition.y + frameImage.transform.localScale.y * 0.5f;
                float textTop = repostedByText.transform.localPosition.y + repostedByText.rectTransform.rect.yMax;
                topExtraHeight = textTop - frameTop;
                if (topExtraHeight < 0f) topExtraHeight = 0f;
            }

            if (extraHeight <= 0f && topExtraHeight <= 0f)
            {
                return;
            }

            float halfExtra = extraHeight * 0.5f;
            float halfTopExtra = topExtraHeight * 0.5f;
            var attachedImageTransform = attachedImageRenderer != null ? attachedImageRenderer.transform : null;

            // 全ての子オブジェクトを自動的に判別してオフセット
            foreach (Transform child in transform)
            {
                if (child == frameImage.transform || child == attachedImageTransform)
                {
                    continue; // Frameはスケール変更で対応するため除外
                }

                bool isTopEdge = (topEdgeObject != null && topEdgeObject == child);

                var pos = child.localPosition;

                if (isTopEdge)
                {
                    // 上端追従オブジェクトは、伸びたフレームの上端に合わせて上へ移動する
                    pos.y += halfExtra;
                    pos.y += halfTopExtra;
                }
                else
                {
                    // 中心(Y=0)以上のオブジェクトは上へ、未満のオブジェクトは下へ広げる
                    if (pos.y >= 0f)
                    {
                        pos.y += halfExtra;
                    }
                    else
                    {
                        pos.y -= halfExtra;
                    }
                    
                    pos.y -= halfTopExtra;
                }
                
                child.localPosition = pos;

                if (child.TryGetComponent<ButtonAnimator>(out var buttonAnimator))
                {
                    buttonAnimator.RefreshBaseTransformFromCurrent();
                }
            }

            // Frameは原点に固定したまま、スケールYのみ拡大（上下対称に広がる）
            var frameTransform = frameImage.transform;
            var frameScale = frameTransform.localScale;
            frameScale.y += (extraHeight + topExtraHeight);
            frameTransform.localScale = frameScale;

            LayoutAttachedImage(renderedHeight);
        }

        void SetupAttachedImage(Sprite attachedImage)
        {
            hasAttachedImage = attachedImage != null;
            currentAttachedImageHeight = 0f;

            if (!hasAttachedImage)
            {
                if (attachedImageRenderer != null)
                {
                    attachedImageRenderer.sprite = null;
                    attachedImageRenderer.gameObject.SetActive(false);
                }

                return;
            }

            var renderer = EnsureAttachedImageRenderer();
            if (renderer == null)
            {
                hasAttachedImage = false;
                return;
            }

            renderer.gameObject.SetActive(true);
            renderer.sprite = attachedImage;
            CacheAttachedImageBaseScale();

            var spriteBounds = attachedImage.bounds.size;
            float sourceWidth = spriteBounds.x * Mathf.Abs(attachedImageBaseLocalScale.x);
            if (sourceWidth <= Mathf.Epsilon)
            {
                renderer.transform.localScale = attachedImageBaseLocalScale;
                return;
            }

            float targetWidth = attachedImageMaxWidth;
            if (targetWidth <= Mathf.Epsilon && contentText != null)
            {
                targetWidth = contentText.rectTransform.rect.width;
            }

            if (targetWidth <= Mathf.Epsilon)
            {
                targetWidth = sourceWidth;
            }

            float fitScale = targetWidth / sourceWidth;
            if (float.IsNaN(fitScale) || float.IsInfinity(fitScale) || fitScale <= Mathf.Epsilon)
            {
                fitScale = 1f;
            }

            renderer.transform.localScale = new Vector3(
                attachedImageBaseLocalScale.x * fitScale,
                attachedImageBaseLocalScale.y * fitScale,
                attachedImageBaseLocalScale.z);
            currentAttachedImageHeight = spriteBounds.y * Mathf.Abs(renderer.transform.localScale.y);
        }

        SpriteRenderer EnsureAttachedImageRenderer()
        {
            if (attachedImageRenderer != null)
            {
                return attachedImageRenderer;
            }

            var attachedImageObject = new GameObject("AttachedImage");
            attachedImageObject.transform.SetParent(transform, false);
            attachedImageRenderer = attachedImageObject.AddComponent<SpriteRenderer>();

            if (frameImage != null)
            {
                attachedImageRenderer.sharedMaterial = frameImage.sharedMaterial;
                attachedImageRenderer.sortingLayerID = frameImage.sortingLayerID;
                attachedImageRenderer.sortingOrder = frameImage.sortingOrder + 1;
                attachedImageRenderer.maskInteraction = frameImage.maskInteraction;
            }

            return attachedImageRenderer;
        }

        void CacheAttachedImageBaseScale()
        {
            if (hasAttachedImageBaseLocalScale || attachedImageRenderer == null)
            {
                return;
            }

            attachedImageBaseLocalScale = attachedImageRenderer.transform.localScale;
            hasAttachedImageBaseLocalScale = true;
        }

        void LayoutAttachedImage(float renderedTextHeight)
        {
            if (!hasAttachedImage || attachedImageRenderer == null || contentText == null)
            {
                return;
            }

            var contentRectTransform = contentText.rectTransform;
            float contentTopY = contentRectTransform.localPosition.y;
            float contentBottomY = contentTopY - renderedTextHeight;
            float imageY = contentBottomY - attachedImageTopMargin - currentAttachedImageHeight * 0.5f;
            float imageX = frameImage != null ? frameImage.transform.localPosition.x : 0f;

            var position = attachedImageRenderer.transform.localPosition;
            position.x = imageX;
            position.y = imageY;
            attachedImageRenderer.transform.localPosition = position;
        }

        void SubscribeActions()
        {
            if (postButtonView != null)
            {
                postButtonView.OnClicked.Subscribe(_ => OnPostClickedEvent()).AddTo(disposables);
                postButtonView.OnScrolled.Subscribe(onScrolled.OnNext).AddTo(disposables);
            }

            if (repostButtonView != null)
            {
                repostButtonView.OnClicked.Subscribe(_ => OnRepostClicked()).AddTo(disposables);
                repostButtonView.OnScrolled.Subscribe(onScrolled.OnNext).AddTo(disposables);
            }

            if (likeButtonView != null)
            {
                likeButtonView.OnClicked.Subscribe(_ => OnLikeClicked()).AddTo(disposables);
                likeButtonView.OnScrolled.Subscribe(onScrolled.OnNext).AddTo(disposables);
            }
        }

        void InitializeButtonColliderClipTargets()
        {
            buttonColliderClipTargets.Clear();
            AddButtonColliderClipTarget(postButtonView);
            AddButtonColliderClipTarget(repostButtonView);
            AddButtonColliderClipTarget(likeButtonView);
        }

        void AddButtonColliderClipTarget(ButtonView buttonView)
        {
            if (buttonView == null)
            {
                return;
            }

            if (!buttonView.TryGetComponent<BoxCollider2D>(out var boxCollider) || boxCollider == null)
            {
                return;
            }

            buttonColliderClipTargets.Add(new ButtonColliderClipTarget
            {
                Collider = boxCollider,
                BaseSize = boxCollider.size,
                BaseOffset = boxCollider.offset
            });
        }

        void ApplyButtonInteractionState()
        {
            postButtonView?.SetInteractable(isInteractable);
            repostButtonView?.SetInteractable(isInteractable);
            likeButtonView?.SetInteractable(isInteractable);

            if (!isInteractable)
            {
                return;
            }

            for (int i = 0; i < buttonColliderClipTargets.Count; i++)
            {
                var target = buttonColliderClipTargets[i];
                if (target.Collider == null)
                {
                    continue;
                }

                // 毎回基準形状に戻してから可視範囲で再クリップし、ドリフトを防ぐ。
                target.Collider.size = target.BaseSize;
                target.Collider.offset = target.BaseOffset;
                target.Collider.enabled = true;

                ClipButtonColliderVertically(target);
            }
        }

        void ClipButtonColliderVertically(ButtonColliderClipTarget target)
        {
            if (target.Collider == null || !target.Collider.enabled)
            {
                return;
            }

            var bounds = target.Collider.bounds;
            float visibleTopY = Mathf.Min(bounds.max.y, interactionClipTopY);
            float visibleBottomY = Mathf.Max(bounds.min.y, interactionClipBottomY);
            float visibleHeightWorld = visibleTopY - visibleBottomY;

            if (visibleHeightWorld <= Mathf.Epsilon)
            {
                target.Collider.enabled = false;
                return;
            }

            if (visibleHeightWorld >= bounds.size.y - Mathf.Epsilon)
            {
                return;
            }

            var baseCenter = bounds.center;
            var visibleCenterWorld = new Vector3(baseCenter.x, (visibleTopY + visibleBottomY) * 0.5f, baseCenter.z);
            var visibleTopWorld = new Vector3(baseCenter.x, visibleTopY, baseCenter.z);
            var visibleBottomWorld = new Vector3(baseCenter.x, visibleBottomY, baseCenter.z);

            var targetTransform = target.Collider.transform;
            var visibleCenterLocal = targetTransform.InverseTransformPoint(visibleCenterWorld);
            var visibleTopLocal = targetTransform.InverseTransformPoint(visibleTopWorld);
            var visibleBottomLocal = targetTransform.InverseTransformPoint(visibleBottomWorld);
            float visibleHeightLocal = Mathf.Abs(visibleTopLocal.y - visibleBottomLocal.y);

            var size = target.BaseSize;
            size.y = Mathf.Max(0f, visibleHeightLocal);

            var offset = target.BaseOffset;
            offset.y = visibleCenterLocal.y;

            target.Collider.size = size;
            target.Collider.offset = offset;
        }

        void OnPostClickedEvent()
        {
            onPostClicked.OnNext(post);
        }

        void OnRepostClicked()
        {
            onRepostedByPlayer.OnNext(post);
        }

        void OnLikeClicked()
        {
            onLikedByPlayer.OnNext(post);
        }

        void RefreshActionViews()
        {
            bool isRepostedByPlayer = post.IsRepostedByPlayer.CurrentValue;
            bool isLikedByPlayer = post.IsLikedByPlayer.CurrentValue;

            RefreshRepostedByText();
            UpdateCountText(repostCountText, post.RepostCount, isRepostedByPlayer, repostActiveColor);
            UpdateCountText(likeCountText, post.LikeCount, isLikedByPlayer, likeActiveColor);
            ApplyIconColor(repostIconImage, isRepostedByPlayer, repostActiveColor);
            ApplyIconColor(likeIconImage, isLikedByPlayer, likeActiveColor);
        }

        void RefreshRepostedByText()
        {
            var repostedByAccount = post.RepostedByAccount;
            bool hasRepostedByAccount = repostedByAccount != null && !string.IsNullOrEmpty(repostedByAccount.Name);

            repostedByText.gameObject.SetActive(hasRepostedByAccount);
            if (repostedHeaderIcon != null)
            {
                repostedHeaderIcon.SetActive(hasRepostedByAccount);
            }
            if (!hasRepostedByAccount)
            {
                return;
            }

            repostedByText.text = $"{repostedByAccount.Name} さんがリポストしました";
        }

        void UpdateCountText(TMP_Text countText, int count, bool isActive, Color activeColor)
        {
            if (countText == null)
            {
                return;
            }

            countText.text = count.ToString();
            countText.color = isActive ? activeColor : actionIconDefaultColor;
        }

        void ApplyIconColor(SpriteRenderer icon, bool isActive, Color activeColor)
        {
            if (icon == null)
            {
                return;
            }

            icon.color = isActive ? activeColor : actionIconDefaultColor;

            if (icon.TryGetComponent<ButtonAnimator>(out var buttonAnimator))
            {
                buttonAnimator.RefreshBaseColorsFromCurrent();
            }
        }

        void LateUpdate()
        {
            if (!isInteractable || buttonColliderClipTargets.Count == 0)
            {
                return;
            }

            // クリップ範囲が無制限なら毎フレーム更新は不要。
            if (float.IsPositiveInfinity(interactionClipTopY) && float.IsNegativeInfinity(interactionClipBottomY))
            {
                return;
            }

            for (int i = 0; i < buttonColliderClipTargets.Count; i++)
            {
                var target = buttonColliderClipTargets[i];
                if (target.Collider == null)
                {
                    continue;
                }

                target.Collider.size = target.BaseSize;
                target.Collider.offset = target.BaseOffset;
                target.Collider.enabled = true;
                ClipButtonColliderVertically(target);
            }
        }

        void OnDestroy()
        {
            disposables.Dispose();
            onPostClicked.OnCompleted();
            onPostClicked.Dispose();
            onLikedByPlayer.OnCompleted();
            onLikedByPlayer.Dispose();
            onRepostedByPlayer.OnCompleted();
            onRepostedByPlayer.Dispose();
            onScrolled.OnCompleted();
            onScrolled.Dispose();
        }
    }
}
