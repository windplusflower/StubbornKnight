using System;
using System.IO;
using System.Reflection;
using System.Collections;
using GlobalEnums;
using Modding;
using UnityEngine;
using UnityEngine.UI;
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
    private int _arrowCount = 3;
    private float _arrowOpacity = 1.0f;
    private const float ArrowSpacing = 0.6f;
    private const float HeightOffset = 1.0f;

    private SpriteRenderer[] _arrowRenderers;
    private ArrowDirection[] _currentArrows;
    private Sprite[] _arrowSprites;
    private Sprite _redArrowSprite;
    private Sprite _greenArrowSprite;
    private GameObject _container;
    private bool _isAnimating = false;
    private bool _isEnabled = true;
    private const float AnimationDuration = 0.2f;

    public ArrowDirection CurrentTargetArrow => _currentArrows[_arrowCount - 1];
    public bool IsAnimating => _isAnimating;

    public bool IsAttackAllowed(AttackDirection dir)
    {
        ArrowDirection target = _currentArrows[_arrowCount - 1];
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
        return _currentArrows[_arrowCount - 1] == dir;
    }

    public void OnSuccessfulAction()
    {
        if (!_isAnimating)
        {
            RollArrows();
            StartCoroutine(PlaySuccessEffectCoroutine());
        }
    }

    private IEnumerator PlaySuccessEffectCoroutine()
    {
        SpriteRenderer targetArrow = _arrowRenderers[0];
        if (targetArrow == null || _greenArrowSprite == null) yield break;

        Sprite originalSprite = targetArrow.sprite;
        targetArrow.sprite = _greenArrowSprite;

        yield return new WaitForSeconds(0.15f);

        targetArrow.sprite = originalSprite;
    }

    private bool _isPlayingErrorEffect = false;

    public void TriggerErrorEffect()
    {
        if (_arrowRenderers[0] == null || _isPlayingErrorEffect) return;
        _isPlayingErrorEffect = true;
        StartCoroutine(PlayErrorEffectCoroutine());
    }

    private IEnumerator PlayErrorEffectCoroutine()
    {
        SpriteRenderer targetArrow = _arrowRenderers[0];
        if (targetArrow == null) 
        {
            _isPlayingErrorEffect = false;
            yield break;
        }

        Sprite originalSprite = targetArrow.sprite;
        if (_redArrowSprite != null)
        {
            targetArrow.sprite = _redArrowSprite;
        }
        
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 originalPos = targetArrow.transform.localPosition;
        float shakeAmount = 0.08f;
        int shakeFrequency = 15;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - (elapsed / duration);
            float shake = Mathf.Sin(elapsed * shakeFrequency * Mathf.PI * 2f) * shakeAmount * decay;
            targetArrow.transform.localPosition = new Vector3(originalPos.x + shake, originalPos.y, 0f);
            yield return null;
        }

        targetArrow.transform.localPosition = originalPos;
        targetArrow.sprite = originalSprite;
        _isPlayingErrorEffect = false;
    }

    private void Start()
    {
        CleanupOldArrows();
        StartCoroutine(LoadCursorSpriteWithActivation());
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

    private IEnumerator LoadCursorSpriteWithActivation()
    {
        _arrowSprites = new Sprite[4];
        
        yield return null;
        
        LoadFallbackSprites();
        UpdateArrowDisplay();
    }

    private IEnumerator ActivateAndGetCursorSpriteCoroutine(Transform cursorRightTransform, Action<Sprite> callback)
    {
        GameObject cursorObj = cursorRightTransform.gameObject;
        Image image = cursorObj.GetComponent<Image>();
        
        if (image != null && image.sprite != null && image.sprite.name != "blank_frame")
        {
            callback(image.sprite);
            yield break;
        }
        
        GameObject tempInstance = Instantiate(cursorObj);
        tempInstance.name = "TempCursorRight";
        tempInstance.SetActive(true);
        
        tempInstance.transform.SetParent(null);
        tempInstance.transform.position = new Vector3(10000, 10000, 0);
        
        Image tempImage = tempInstance.GetComponent<Image>();
        Animator tempAnimator = tempInstance.GetComponent<Animator>();
        
        if (tempAnimator != null)
        {
            tempAnimator.enabled = true;
            tempAnimator.Rebind();
            tempAnimator.Update(0f);
            
            if (HasAnimatorParameter(tempAnimator, "show"))
            {
                tempAnimator.ResetTrigger("hide");
                tempAnimator.SetTrigger("show");
            }
        }
        
        Sprite result = null;
        for (int i = 0; i < 30; i++)
        {
            if (tempImage != null && tempImage.sprite != null && tempImage.sprite.name != "blank_frame")
            {
                result = tempImage.sprite;
                break;
            }
            
            if (tempAnimator != null)
            {
                tempAnimator.Update(0.016f);
            }
            
            yield return null;
        }
        
        Destroy(tempInstance);
        
        callback(result);
    }

    private bool HasAnimatorParameter(Animator animator, string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
            {
                return true;
            }
        }
        return false;
    }

    private void LoadFallbackSprites()
    {
        try
        {
            Assembly modAssembly = typeof(StubbornKnight).Assembly;
            string resourceName = "StubbornKnight.assets.left-arrow.png";

            using (Stream stream = modAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
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

            _redArrowSprite = LoadSpriteFromResources("StubbornKnight.assets.red-arrow.png");
            _greenArrowSprite = LoadSpriteFromResources("StubbornKnight.assets.green-arrow.png");
        }
        catch
        {
        }
    }

    private Sprite LoadSpriteFromResources(string resourceName)
    {
        try
        {
            Assembly modAssembly = typeof(StubbornKnight).Assembly;
            using (Stream stream = modAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(buffer);
                tex.Apply();

                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1024f);
            }
        }
        catch
        {
            return null;
        }
    }

    private void SetArrowRotation(SpriteRenderer sr, ArrowDirection dir)
    {
        switch (dir)
        {
            case ArrowDirection.Right:
                sr.transform.localRotation = Quaternion.Euler(0, 180, 0);
                break;
            case ArrowDirection.Left:
                sr.transform.localRotation = Quaternion.identity;
                break;
            case ArrowDirection.Up:
                sr.transform.localRotation = Quaternion.Euler(0, 0, -90);
                break;
            case ArrowDirection.Down:
                sr.transform.localRotation = Quaternion.Euler(0, 0, 90);
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

    public void SetConfig(int arrowCount, float arrowOpacity)
    {
        _arrowCount = arrowCount;
        _arrowOpacity = arrowOpacity;
        ApplyOpacity(arrowOpacity);
    }

    private void ApplyOpacity(float opacity)
    {
        if (_arrowRenderers == null) return;
        for (int i = 0; i < _arrowRenderers.Length; i++)
        {
            if (_arrowRenderers[i] != null)
            {
                Color c = _arrowRenderers[i].color;
                c.a = opacity;
                _arrowRenderers[i].color = c;
            }
        }
    }

    public void UpdateConfig(int arrowCount, float arrowOpacity)
    {
        bool needRecreate = arrowCount != _arrowCount;
        _arrowCount = arrowCount;
        _arrowOpacity = arrowOpacity;

        if (needRecreate && _container != null)
        {
            Destroy(_container);
            CreateArrowDisplay();
            GenerateNewArrows();
        }
        else
        {
            ApplyOpacity(arrowOpacity);
        }
    }

    private void CreateArrowDisplay()
    {
        _container = new GameObject("ArrowContainer");
        _container.transform.SetParent(HeroController.instance.transform);
        _container.transform.localPosition = Vector3.zero;

        _arrowRenderers = new SpriteRenderer[_arrowCount];
        _currentArrows = new ArrowDirection[_arrowCount];

        Color arrowColor = new Color(1f, 1f, 1f, _arrowOpacity);

        for (int i = 0; i < _arrowCount; i++)
        {
            GameObject arrowObj = new GameObject($"Arrow_{i}");
            arrowObj.transform.SetParent(_container.transform);
            arrowObj.transform.localPosition = new Vector3(0, HeightOffset + i * ArrowSpacing, 0);

            SpriteRenderer sr = arrowObj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Effects";
            sr.sortingOrder = 100;
            sr.color = arrowColor;
            sr.transform.localScale = new Vector3(7.5f, 7.5f, 1f);

            _arrowRenderers[i] = sr;
        }
    }

    private void GenerateNewArrows()
    {
        for (int i = 0; i < _arrowCount; i++)
        {
            _currentArrows[i] = (ArrowDirection)Random.Range(0, 4);
        }
        UpdateArrowDisplay();
        SetTopArrowTransparent();
    }

    private void UpdateArrowDisplay()
    {
        for (int i = 0; i < _arrowCount; i++)
        {
            if (_arrowRenderers[i] == null) continue;
            int arrowIndex = (_arrowCount - 1) - i;
            _arrowRenderers[i].sprite = _arrowSprites[(int)_currentArrows[arrowIndex]];
            SetArrowRotation(_arrowRenderers[i], _currentArrows[arrowIndex]);
            
            float xOffset = 0f;
            if (_currentArrows[arrowIndex] == ArrowDirection.Right)
                xOffset = 0f;
            else if (_currentArrows[arrowIndex] == ArrowDirection.Left)
                xOffset = -0f;
            
            _arrowRenderers[i].transform.localPosition = new Vector3(xOffset, HeightOffset + i * ArrowSpacing, 0);
        }
    }

    private void SetTopArrowTransparent()
    {
        int topIndex = _arrowCount - 1;
        if (_arrowRenderers[topIndex] != null)
        {
            Color c = _arrowRenderers[topIndex].color;
            c.a = 0f;
            _arrowRenderers[topIndex].color = c;
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

        Vector3[] startPositions = new Vector3[_arrowCount];
        Vector3[] targetPositions = new Vector3[_arrowCount];

        for (int i = 0; i < _arrowCount; i++)
        {
            if (_arrowRenderers[i] == null) continue;
            startPositions[i] = _arrowRenderers[i].transform.localPosition;
            targetPositions[i] = startPositions[i] + new Vector3(0, -ArrowSpacing, 0);
        }

        while (elapsed < AnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / AnimationDuration);

            for (int i = 0; i < _arrowCount; i++)
            {
                if (_arrowRenderers[i] != null)
                    _arrowRenderers[i].transform.localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], t);
            }

            if (_arrowRenderers[0] != null)
            {
                Color c0 = _arrowRenderers[0].color;
                c0.a = Mathf.Lerp(_arrowOpacity, 0f, t);
                _arrowRenderers[0].color = c0;
            }

            int topIndex = _arrowCount - 1;
            if (_arrowRenderers[topIndex] != null)
            {
                Color c3 = _arrowRenderers[topIndex].color;
                c3.a = Mathf.Lerp(0f, _arrowOpacity, t);
                _arrowRenderers[topIndex].color = c3;
            }

            yield return null;
        }

        for (int i = _arrowCount - 1; i > 0; i--)
        {
            _currentArrows[i] = _currentArrows[i - 1];
        }
        _currentArrows[0] = (ArrowDirection)Random.Range(0, 4);

        ResetArrowPositions();
        UpdateArrowDisplay();

        for (int i = 0; i < _arrowCount - 1; i++)
        {
            if (_arrowRenderers[i] != null)
            {
                Color c = _arrowRenderers[i].color;
                c.a = _arrowOpacity;
                _arrowRenderers[i].color = c;
            }
        }
        SetTopArrowTransparent();

        _isAnimating = false;
    }

    private void ResetArrowPositions()
    {
        for (int i = 0; i < _arrowCount; i++)
        {
            float xOffset = 0f;
            if (_currentArrows[i] == ArrowDirection.Right)
                xOffset = 0f;
            else if (_currentArrows[i] == ArrowDirection.Left)
                xOffset = -0f;
            
            _arrowRenderers[i].transform.localPosition = new Vector3(xOffset, HeightOffset + i * ArrowSpacing, 0);
        }
    }
}