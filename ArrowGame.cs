using System.Reflection;
using System.Collections;
using GlobalEnums;
using Modding;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StubbornKnight;

public enum ArrowDirection
{
    Up,
    Down,
    Left,
    Right
}

public class ArrowGame : MonoBehaviour
{
    private const int ArrowCount = 4;
    private const float ArrowSpacing = 0.6f;
    private const float HeightOffset = 1.0f;

    private SpriteRenderer[] _arrowRenderers;
    private ArrowDirection[] _currentArrows;
    private Sprite[] _arrowSprites;
    private GameObject _container;
    private bool _isAnimating = false;
    private bool _isEnabled = true;
    private const float AnimationDuration = 0.2f;

    public ArrowDirection CurrentTargetArrow => _currentArrows[3];
    public bool IsAnimating => _isAnimating;

    public bool IsAttackAllowed(AttackDirection dir)
    {
        ArrowDirection target = _currentArrows[3];
        switch (dir)
        {
            case AttackDirection.upward:
                return target == ArrowDirection.Up;
            case AttackDirection.downward:
                return target == ArrowDirection.Down;
            case AttackDirection.normal:
                if (HeroController.instance == null) return false;
                return HeroController.instance.cState.facingRight 
                    ? target == ArrowDirection.Right 
                    : target == ArrowDirection.Left;
            default:
                return false;
        }
    }

    public bool IsSpellAllowed(ArrowDirection dir)
    {
        return _currentArrows[3] == dir;
    }

    public void OnSuccessfulAction()
    {
        if (!_isAnimating)
        {
            RollArrows();
        }
    }

    private void Start()
    {
        CleanupOldArrows();
        LoadArrowSprites();
        CreateArrowDisplay();
        GenerateNewArrows();
        SetModEnabled(_isEnabled);
    }

    private void CleanupOldArrows()
    {
        var existingContainers = GameObject.FindObjectsOfType<GameObject>();
        foreach (var go in existingContainers)
        {
            if (go.name == "ArrowContainer" && go.transform.parent == HeroController.instance?.transform)
            {
                Destroy(go);
            }
        }
    }

    private void Update()
    {
    }

    private void LateUpdate()
    {
        if (_container != null && HeroController.instance != null)
        {
            float playerScaleX = HeroController.instance.transform.lossyScale.x;
            _container.transform.localScale = new Vector3(1f / playerScaleX, 1f, 1f);
        }
    }

    private void LoadArrowSprites()
    {
        _arrowSprites = new Sprite[4];

        try
        {
            Assembly modAssembly = typeof(StubbornKnight).Assembly;
            string resourceName = "StubbornKnight.assets.right-arrow.png";

            using (Stream stream = modAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Log($"Resource not found: {resourceName}");
                    return;
                }

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(buffer);
                tex.Apply();

                Sprite baseSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1024f);

                for (int i = 0; i < 4; i++)
                {
                    _arrowSprites[i] = baseSprite;
                }
            }

            Log("Arrow sprites loaded successfully");
        }
        catch (Exception e)
        {
            Log("Error loading arrow sprites: " + e.Message);
        }
    }

    private void SetArrowRotation(SpriteRenderer sr, ArrowDirection dir)
    {
        switch (dir)
        {
            case ArrowDirection.Right:
                sr.transform.localRotation = Quaternion.identity;
                break;
            case ArrowDirection.Left:
                sr.transform.localRotation = Quaternion.Euler(0, 180, 0);
                break;
            case ArrowDirection.Up:
                sr.transform.localRotation = Quaternion.Euler(0, 0, 90);
                break;
            case ArrowDirection.Down:
                sr.transform.localRotation = Quaternion.Euler(0, 0, -90);
                break;
        }
    }

    private void Log(string message)
    {
        StubbornKnight.instance.Log($"[ArrowGame] {message}");
    }

    public void SetModEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (_container != null)
        {
            _container.SetActive(enabled);
        }
    }

    private void CreateArrowDisplay()
    {
        _container = new GameObject("ArrowContainer");
        _container.transform.SetParent(HeroController.instance.transform);
        _container.transform.localPosition = Vector3.zero;

        _arrowRenderers = new SpriteRenderer[ArrowCount];
        _currentArrows = new ArrowDirection[ArrowCount];

        for (int i = 0; i < ArrowCount; i++)
        {
            GameObject arrowObj = new GameObject($"Arrow_{i}");
            arrowObj.transform.SetParent(_container.transform);
            arrowObj.transform.localPosition = new Vector3(0, HeightOffset + i * ArrowSpacing, 0);

            SpriteRenderer sr = arrowObj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Effects";
            sr.sortingOrder = 100;
            sr.color = Color.white;

            _arrowRenderers[i] = sr;
        }
    }

    private void GenerateNewArrows()
    {
        for (int i = 0; i < ArrowCount; i++)
        {
            _currentArrows[i] = (ArrowDirection)Random.Range(0, 4);
        }
        UpdateArrowDisplay();
        SetTopArrowTransparent();
    }

    private void UpdateArrowDisplay()
    {
        for (int i = 0; i < ArrowCount; i++)
        {
            if (_arrowRenderers[i] == null) continue;
            int arrowIndex = (ArrowCount - 1) - i;
            _arrowRenderers[i].sprite = _arrowSprites[(int)_currentArrows[arrowIndex]];
            SetArrowRotation(_arrowRenderers[i], _currentArrows[arrowIndex]);
        }
    }

    private void SetTopArrowTransparent()
    {
        if (_arrowRenderers[3] != null)
        {
            Color c = _arrowRenderers[3].color;
            c.a = 0f;
            _arrowRenderers[3].color = c;
        }
    }

    private void RollArrows()
    {
        StartCoroutine(RollArrowsWithAnimation());
    }

    private IEnumerator RollArrowsWithAnimation()
    {
        _isAnimating = true;

        float elapsed = 0f;

        Vector3[] startPositions = new Vector3[ArrowCount];
        Vector3[] targetPositions = new Vector3[ArrowCount];

        for (int i = 0; i < ArrowCount; i++)
        {
            if (_arrowRenderers[i] == null) continue;
            startPositions[i] = _arrowRenderers[i].transform.localPosition;
            targetPositions[i] = startPositions[i] + new Vector3(0, -ArrowSpacing, 0);
        }

        while (elapsed < AnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / AnimationDuration);

            for (int i = 0; i < ArrowCount; i++)
            {
                if (_arrowRenderers[i] != null)
                    _arrowRenderers[i].transform.localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], t);
            }

            if (_arrowRenderers[0] != null)
            {
                Color c0 = _arrowRenderers[0].color;
                c0.a = Mathf.Lerp(1f, 0f, t);
                _arrowRenderers[0].color = c0;
            }

            if (_arrowRenderers[3] != null)
            {
                Color c3 = _arrowRenderers[3].color;
                c3.a = Mathf.Lerp(0f, 1f, t);
                _arrowRenderers[3].color = c3;
            }

            yield return null;
        }

        for (int i = ArrowCount - 1; i > 0; i--)
        {
            _currentArrows[i] = _currentArrows[i - 1];
        }
        _currentArrows[0] = (ArrowDirection)Random.Range(0, 4);

        ResetArrowPositions();
        UpdateArrowDisplay();

        for (int i = 0; i < ArrowCount - 1; i++)
        {
            if (_arrowRenderers[i] != null)
            {
                Color c = _arrowRenderers[i].color;
                c.a = 1f;
                _arrowRenderers[i].color = c;
            }
        }
        SetTopArrowTransparent();

        _isAnimating = false;
    }

    private void ResetArrowPositions()
    {
        for (int i = 0; i < ArrowCount; i++)
        {
            _arrowRenderers[i].transform.localPosition = new Vector3(0, HeightOffset + i * ArrowSpacing, 0);
        }
    }
}
