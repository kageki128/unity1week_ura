using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Unity1Week_Ura.Actor
{
    public class BackgroundView : MonoBehaviour
    {
        enum ScrollDirection
        {
            Left,
            Right,
            Up,
            Down,
        }

        [Header("Tile")]
        [SerializeField] Sprite tileSprite;
        [SerializeField] Color tileColor = Color.white;
        [SerializeField, Min(0f)] float tileScale = 1f;

        [Header("Layout")]
        [SerializeField] float tiltAngle;

        [Header("Scroll")]
        [SerializeField] ScrollDirection direction = ScrollDirection.Left;
        [SerializeField, Min(0f)] float speed = 8f;

        readonly List<Image> tileImages = new();

        RectTransform rectTransform;
        RectTransform scrollRoot;
        RectTransform rotationRoot;
        Tween scrollTween;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            BuildTiles();
            StartScroll();
        }

        void OnRectTransformDimensionsChange()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (rectTransform == null)
            {
                return;
            }

            BuildTiles();
            StartScroll();
        }

        void OnDestroy()
        {
            KillScrollTween();
        }

        void BuildTiles()
        {
            if (tileSprite == null)
            {
                Debug.LogWarning("BackgroundView: tileSprite is not assigned.", this);
                return;
            }

            EnsureRoots();
            ClearTiles();

            var tileSizeValue = ResolveTileSize();
            if (tileSizeValue.x <= 0f || tileSizeValue.y <= 0f)
            {
                Debug.LogWarning("BackgroundView: tileSize must be positive.", this);
                return;
            }

            var viewportSize = rectTransform.rect.size;
            var diagonal = Mathf.Sqrt(viewportSize.x * viewportSize.x + viewportSize.y * viewportSize.y);
            var requiredWidth = diagonal + tileSizeValue.x * 2f;
            var requiredHeight = diagonal + tileSizeValue.y * 2f;

            var columns = Mathf.CeilToInt(requiredWidth / tileSizeValue.x) + 1;
            var rows = Mathf.CeilToInt(requiredHeight / tileSizeValue.y) + 1;

            var startX = -((columns - 1) * tileSizeValue.x) * 0.5f;
            var startY = ((rows - 1) * tileSizeValue.y) * 0.5f;

            for (var y = 0; y < rows; y++)
            {
                for (var x = 0; x < columns; x++)
                {
                    var tile = CreateTileImage();
                    var tileRect = tile.rectTransform;
                    tileRect.anchoredPosition = new Vector2(startX + x * tileSizeValue.x, startY - y * tileSizeValue.y);
                    tileRect.sizeDelta = tileSizeValue;
                    tileImages.Add(tile);
                }
            }
        }

        Image CreateTileImage()
        {
            var tileObject = new GameObject("Tile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tileObject.transform.SetParent(rotationRoot, false);

            var image = tileObject.GetComponent<Image>();
            image.sprite = tileSprite;
            image.color = tileColor;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            return image;
        }

        void EnsureRoots()
        {
            if (scrollRoot == null)
            {
                scrollRoot = FindOrCreateRoot("ScrollRoot", rectTransform);
            }

            if (rotationRoot == null)
            {
                rotationRoot = FindOrCreateRoot("RotationRoot", scrollRoot);
            }

            scrollRoot.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRoot.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRoot.pivot = new Vector2(0.5f, 0.5f);
            scrollRoot.anchoredPosition = Vector2.zero;
            scrollRoot.localRotation = Quaternion.identity;
            scrollRoot.localScale = Vector3.one;

            rotationRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rotationRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rotationRoot.pivot = new Vector2(0.5f, 0.5f);
            rotationRoot.anchoredPosition = Vector2.zero;
            rotationRoot.localRotation = Quaternion.Euler(0f, 0f, tiltAngle);
            rotationRoot.localScale = Vector3.one;
        }

        static RectTransform FindOrCreateRoot(string objectName, Transform parent)
        {
            var child = parent.Find(objectName);
            if (child != null)
            {
                return child.GetComponent<RectTransform>();
            }

            var rootObject = new GameObject(objectName, typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            return rootObject.GetComponent<RectTransform>();
        }

        void ClearTiles()
        {
            foreach (var image in tileImages)
            {
                if (image == null)
                {
                    continue;
                }

                Destroy(image.gameObject);
            }

            tileImages.Clear();
        }

        void StartScroll()
        {
            KillScrollTween();

            if (tileSprite == null || speed <= 0f)
            {
                if (scrollRoot != null)
                {
                    scrollRoot.anchoredPosition = Vector2.zero;
                }

                return;
            }

            var tileSizeValue = ResolveTileSize();
            var step = Mathf.Max(tileSizeValue.x, tileSizeValue.y);
            if (step <= 0f)
            {
                return;
            }

            var duration = step / speed;
            scrollTween = DOVirtual.Float(0f, step, duration, ApplyScrollOffset)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }

        void ApplyScrollOffset(float distance)
        {
            if (scrollRoot == null)
            {
                return;
            }

            var tileSizeValue = ResolveTileSize();
            if (tileSizeValue.x <= 0f || tileSizeValue.y <= 0f)
            {
                scrollRoot.anchoredPosition = Vector2.zero;
                return;
            }

            var axis = GetDirectionAxis();
            var screenOffset = axis * distance;

            var localOffset = Rotate(screenOffset, -tiltAngle);
            localOffset.x = RepeatSigned(localOffset.x, tileSizeValue.x);
            localOffset.y = RepeatSigned(localOffset.y, tileSizeValue.y);

            scrollRoot.anchoredPosition = Rotate(localOffset, tiltAngle);
        }

        Vector2 GetDirectionAxis()
        {
            return direction switch
            {
                ScrollDirection.Left => Vector2.left,
                ScrollDirection.Right => Vector2.right,
                ScrollDirection.Up => Vector2.up,
                ScrollDirection.Down => Vector2.down,
                _ => Vector2.left,
            };
        }

        static Vector2 Rotate(Vector2 vector, float angleDegrees)
        {
            var radians = angleDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);

            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }

        static float RepeatSigned(float value, float length)
        {
            if (length <= 0f)
            {
                return 0f;
            }

            var half = length * 0.5f;
            return Mathf.Repeat(value + half, length) - half;
        }

        Vector2 ResolveTileSize()
        {
            if (tileSprite == null)
            {
                return Vector2.zero;
            }

            return tileSprite.rect.size * tileScale;
        }

        void KillScrollTween()
        {
            if (scrollTween == null)
            {
                return;
            }

            if (scrollTween.IsActive())
            {
                scrollTween.Kill();
            }

            scrollTween = null;
        }
    }
}