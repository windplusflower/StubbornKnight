/*
 * 空洞骑士Mod入门到进阶指南/配套模版
 * 作者：近环（https://space.bilibili.com/1224243724）
 */

using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Collections;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using Modding;
using Modding.Utils;
using Satchel;
using Satchel.Futils;
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
    private HeroActions _inputActions;
    private GameObject _container;
    private bool _isAnimating = false;
    private const float AnimationDuration = 0.2f;

    private void Start()
    {
        LoadArrowSprites();
        CreateArrowDisplay();
        GenerateNewArrows();
        
        _inputActions = InputHandler.Instance.inputActions;
    }

    private void Update()
    {
        if (_inputActions == null || _isAnimating) return;

        ArrowDirection targetArrow = _currentArrows[3];
        bool isCorrect = false;
        string inputDir = "None";

        if (_inputActions.left.WasPressed)
            inputDir = "Left";
        else if (_inputActions.right.WasPressed)
            inputDir = "Right";
        else if (_inputActions.up.WasPressed)
            inputDir = "Up";
        else if (_inputActions.down.WasPressed)
            inputDir = "Down";

        if (inputDir != "None")
        {
            if (targetArrow == ArrowDirection.Left && _inputActions.left.WasPressed)
                isCorrect = true;
            else if (targetArrow == ArrowDirection.Right && _inputActions.right.WasPressed)
                isCorrect = true;
            else if (targetArrow == ArrowDirection.Up && _inputActions.up.WasPressed)
                isCorrect = true;
            else if (targetArrow == ArrowDirection.Down && _inputActions.down.WasPressed)
                isCorrect = true;

            // Debug log
            string arrows = $"Bottom:{_currentArrows[2]}, Mid:{_currentArrows[1]}, Top:{_currentArrows[0]}, Input:{inputDir}, Correct:{isCorrect}";
            Log(arrows);
        }

        if (isCorrect)
        {
            RollArrows();
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
            // 使用白色，让图片自带颜色显示
            sr.color = Color.white;
            
            _arrowRenderers[i] = sr;
        }
    }

    private void LateUpdate()
    {
        if (_container != null && HeroController.instance != null)
        {
            // 强制设置scale为正值，防止随玩家翻转
            float playerScaleX = HeroController.instance.transform.lossyScale.x;
            _container.transform.localScale = new Vector3(1f / playerScaleX, 1f, 1f);
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
        Log($"Generated: 0:{_currentArrows[0]}, 1:{_currentArrows[1]}, 2:{_currentArrows[2]}, 3:{_currentArrows[3]}");
    }

    private void UpdateArrowDisplay()
    {
        _arrowRenderers[0].sprite = _arrowSprites[(int)_currentArrows[3]];
        _arrowRenderers[1].sprite = _arrowSprites[(int)_currentArrows[2]];
        _arrowRenderers[2].sprite = _arrowSprites[(int)_currentArrows[1]];
        _arrowRenderers[3].sprite = _arrowSprites[(int)_currentArrows[0]];
        
        SetArrowRotation(_arrowRenderers[0], _currentArrows[3]);
        SetArrowRotation(_arrowRenderers[1], _currentArrows[2]);
        SetArrowRotation(_arrowRenderers[2], _currentArrows[1]);
        SetArrowRotation(_arrowRenderers[3], _currentArrows[0]);
    }

    private void SetTopArrowTransparent()
    {
        Color c = _arrowRenderers[3].color;
        c.a = 0f;
        _arrowRenderers[3].color = c;
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
            startPositions[i] = _arrowRenderers[i].transform.localPosition;
            targetPositions[i] = startPositions[i] + new Vector3(0, -ArrowSpacing, 0);
        }

        while (elapsed < AnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / AnimationDuration);

            for (int i = 0; i < ArrowCount; i++)
            {
                _arrowRenderers[i].transform.localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], t);
            }

            Color c0 = _arrowRenderers[0].color;
            c0.a = Mathf.Lerp(1f, 0f, t);
            _arrowRenderers[0].color = c0;

            Color c3 = _arrowRenderers[3].color;
            c3.a = Mathf.Lerp(0f, 1f, t);
            _arrowRenderers[3].color = c3;

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
            Color c = _arrowRenderers[i].color;
            c.a = 1f;
            _arrowRenderers[i].color = c;
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



// Mod配置类，目前只有开关的配置。可以自行添加额外选项，并在GetMenuData里添加交互。
[Serializable]
public class Settings
{
    public bool on = true;
}

public class StubbornKnight : Mod, IGlobalSettings<Settings>, IMenuMod
{
    public static StubbornKnight instance;
    /*  
     * ******** Mod名字和版本号 ********
     */
    public StubbornKnight() : base("StubbornKnight")
    {
        instance = this;
    }
    public override string GetVersion() => "1.0";

    /* 
     * ******** 预加载和hook ********
     */
    public override List<(string, string)> GetPreloadNames()
    {
        // 预加载你想要的攻击特效或者敌人，具体请阅读教程。
        return new List<(string, string)>
        {
            // ("GG_Radiance", "Boss Control/Absolute Radiance")
        };
    }
    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        On.HeroController.Start += HeroController_Start;
        On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;

        ModHooks.LanguageGetHook += changeName;
    }

    private void HeroController_Start(On.HeroController.orig_Start orig, HeroController self)
    {
        orig(self);
        
        if (mySettings.on)
        {
            self.gameObject.AddComponent<ArrowGame>();
        }
    }
    private string changeName(string key, string title, string orig)
    {
        // if (key == "MEGA_MOSS_MAIN" || key == "NAME_MEGA_MOSS_CHARGER" && mySettings.on) {
        //     return "大型苔藓冲飞者";
        // }
        return orig;
    }

    /* 
     * ******** FSM相关改动，这个示例改动使得左特随机在空中多次假动作 ********
     */
    [Obsolete]
    private void PlayMakerFSM_OnEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
    {
        if (mySettings.on)
        {
            //FSM:MoveMent Attacking BroadcastDeath
            if (self.gameObject.scene.name == "GG_Ghost_Hu" && self.gameObject.name == "Ghost Warrior Hu")
            {
                if (self.FsmName == "Attacking")
                {
                    self.enabled = false;
                }
                if (self.FsmName == "MoveMent")
                {
                    self.enabled = false;
                }

            }
        }
        orig(self);
    }

    /* 
     * ******** 配置文件读取和菜单设置，如没有额外需求不需要改动 ********
     */
    private Settings mySettings = new();
    public bool ToggleButtonInsideMenu => true;
    // 读取配置文件
    public void OnLoadGlobal(Settings settings) => mySettings = settings;
    // 写入配置文件
    public Settings OnSaveGlobal() => mySettings;
    // 设置菜单格式
    public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? menu)
    {
        List<IMenuMod.MenuEntry> menus = new();
        menus.Add(
            new()
            {
                // 这是个单选菜单，这里提供开和关两种选择。
                Values = new string[]
                {
                    Language.Language.Get("MOH_ON", "MainMenu"),
                    Language.Language.Get("MOH_OFF", "MainMenu"),
                },
                // 把菜单的当前被选项更新到配置变量
                Saver = i => mySettings.on = i == 0,
                Loader = () => mySettings.on ? 0 : 1,
                // Name = "Moss Jumper",
            }
        );
        return menus;
    }
}
