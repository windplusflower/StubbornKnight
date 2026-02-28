/*
 * 空洞骑士Mod入门到进阶指南/配套模版
 * 作者：近环（https://space.bilibili.com/1224243724）
 */

using System.Reflection;
using System.Collections;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
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
    private GameObject _container;
    private bool _isAnimating = false;
    private bool _isEnabled = true;
    private const float AnimationDuration = 0.2f;

    // 公共接口：获取当前底部箭头
    public ArrowDirection CurrentTargetArrow => _currentArrows[3];
    public bool IsAnimating => _isAnimating;

    // 检查攻击方向是否匹配
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
                // 普通攻击根据朝向判断左/右
                if (HeroController.instance == null) return false;
                return HeroController.instance.cState.facingRight 
                    ? target == ArrowDirection.Right 
                    : target == ArrowDirection.Left;
            default:
                return false;
        }
    }

    // 检查法术方向是否匹配（传入 ArrowDirection 而不是 SpellType）
    public bool IsSpellAllowed(ArrowDirection dir)
    {
        return _currentArrows[3] == dir;
    }

    // 成功执行动作后调用
    public void OnSuccessfulAction()
    {
        if (!_isAnimating)
        {
            RollArrows();
        }
    }

    private void Start()
    {
        // 清理旧的箭头容器（避免重复创建）
        CleanupOldArrows();
        
        LoadArrowSprites();
        CreateArrowDisplay();
        GenerateNewArrows();
        
        // 应用初始启用状态
        SetModEnabled(_isEnabled);
    }
    
    private void CleanupOldArrows()
    {
        // 查找并销毁场景中已存在的 ArrowContainer
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
        // 不再检测方向键输入，改为通过 Hook 拦截攻击和法术
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
            // 使用白色，让图片自带颜色显示
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
        // 日志已移除，改为在攻击/法术时打印
    }

    private void UpdateArrowDisplay()
    {
        for (int i = 0; i < ArrowCount; i++)
        {
            if (_arrowRenderers[i] == null) continue;
            int arrowIndex = (ArrowCount - 1) - i; // 3, 2, 1, 0
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

// 法术拦截 Action
public class SpellInterceptAction : FsmStateAction
{
    public override void OnEnter()
    {
        // 检查 mod 开关
        if (!StubbornKnight.IsModEnabled)
        {
            Finish();
            return;
        }

        // 获取 ArrowGame 组件
        var arrowGame = HeroController.instance?.GetComponent<ArrowGame>();
        if (arrowGame == null)
        {
            Finish();
            return;
        }

        // 直接读取输入轴判断法术方向和类型
        float verticalInput = UnityEngine.Input.GetAxisRaw("Vertical");
        float horizontalInput = UnityEngine.Input.GetAxisRaw("Horizontal");

        ArrowDirection spellDir;
        string spellName;

        // 优先检查垂直输入（吼/砸）
        if (verticalInput > 0.1f)
        {
            spellDir = ArrowDirection.Up;
            spellName = "Shriek(吼)";
        }
        else if (verticalInput < -0.1f)
        {
            spellDir = ArrowDirection.Down;
            spellName = "Quake(砸)";
        }
        // 然后检查水平输入（波）
        else if (horizontalInput > 0.1f)
        {
            spellDir = ArrowDirection.Right;
            spellName = "Fireball(波右)";
        }
        else if (horizontalInput < -0.1f)
        {
            spellDir = ArrowDirection.Left;
            spellName = "Fireball(波左)";
        }
        // 默认：根据朝向判断波的方向
        else
        {
            bool facingRight = HeroController.instance.cState.facingRight;
            spellDir = facingRight ? ArrowDirection.Right : ArrowDirection.Left;
            spellName = facingRight ? "Fireball(波右-默认)" : "Fireball(波左-默认)";
        }

        // 获取期望方向
        ArrowDirection expected = arrowGame.CurrentTargetArrow;
        // 检查是否匹配
        bool isSuccess = (spellDir == expected);

        if (!isSuccess)
        {
            // 取消法术
            Fsm.Event("FSM CANCEL");
            StubbornKnight.instance.Log($"[Spell] Expected: {expected}, Actual: {spellName}({spellDir}), Result: FAILED");
        }
        else
        {
            // 允许释放，触发滚动
            arrowGame.OnSuccessfulAction();
            StubbornKnight.instance.Log($"[Spell] Expected: {expected}, Actual: {spellName}({spellDir}), Result: SUCCESS");
        }

        Finish();
    }
}

// Mod配置类
[Serializable]
public class Settings
{
    public bool on = true;
}

public class StubbornKnight : Mod, IGlobalSettings<Settings>, IMenuMod
{
    public static StubbornKnight instance;
    private Settings mySettings = new();

    public static bool IsModEnabled => instance != null && instance.mySettings.on;

    public StubbornKnight() : base("StubbornKnight")
    {
        instance = this;
    }

    public override string GetVersion() => "1.0";

    public override List<(string, string)> GetPreloadNames()
    {
        return new List<(string, string)>();
    }

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        On.HeroController.Start += HeroController_Start;
        On.HeroController.Attack += HeroController_Attack;
        On.HeroController.CanNailArt += HeroController_CanNailArt;
        On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;

        ModHooks.LanguageGetHook += changeName;
    }

    private void HeroController_Start(On.HeroController.orig_Start orig, HeroController self)
    {
        orig(self);
        
        var arrowGame = self.gameObject.AddComponent<ArrowGame>();
        arrowGame.SetModEnabled(mySettings.on);
    }

    private void HeroController_Attack(On.HeroController.orig_Attack orig, HeroController self, AttackDirection dir)
    {
        if (!mySettings.on)
        {
            orig(self, dir);
            return;
        }

        var arrowGame = self.GetComponent<ArrowGame>();
        if (arrowGame == null)
        {
            orig(self, dir);
            return;
        }

        // 获取期望方向
        ArrowDirection expected = arrowGame.CurrentTargetArrow;
        // 将 AttackDirection 转换为可读的字符串
        string actualDir = dir.ToString();
        if (dir == AttackDirection.normal)
        {
            actualDir = HeroController.instance.cState.facingRight ? "Right" : "Left";
        }
        
        // 检查攻击方向是否匹配
        bool isSuccess = arrowGame.IsAttackAllowed(dir);
        
        if (isSuccess)
        {
            // 允许攻击
            orig(self, dir);
            // 触发滚动
            arrowGame.OnSuccessfulAction();
            Log($"[Attack] Expected: {expected}, Actual: {actualDir}, Result: SUCCESS");
        }
        else
        {
            // 取消攻击
            Log($"[Attack] Expected: {expected}, Actual: {actualDir}, Result: FAILED");
        }
    }

    private bool HeroController_CanNailArt(On.HeroController.orig_CanNailArt orig, HeroController self)
    {
        bool result = orig(self);
        
        if (!mySettings.on || !result)
        {
            return result;
        }

        var arrowGame = self.GetComponent<ArrowGame>();
        if (arrowGame == null)
        {
            return result;
        }

        // 读取垂直输入判断剑技方向
        float verticalInput = Input.GetAxisRaw("Vertical");
        ArrowDirection arrowDir;
        string nailArtName;

        if (verticalInput > 0.1f)
        {
            arrowDir = ArrowDirection.Up;
            nailArtName = "Cyclone Slash";
        }
        else if (verticalInput < -0.1f)
        {
            arrowDir = ArrowDirection.Down;
            nailArtName = "Dash Slash";
        }
        else
        {
            arrowDir = self.cState.facingRight ? ArrowDirection.Right : ArrowDirection.Left;
            nailArtName = "Great Slash";
        }

        // 获取期望方向
        ArrowDirection expected = arrowGame.CurrentTargetArrow;
        // 检查是否匹配
        bool isSuccess = arrowGame.IsSpellAllowed(arrowDir);

        if (!isSuccess)
        {
            // 拦截剑技
            Log($"[NailArt] Expected: {expected}, Actual: {nailArtName}({arrowDir}), Result: FAILED");
            return false;
        }
        else
        {
            // 允许释放，触发滚动
            arrowGame.OnSuccessfulAction();
            Log($"[NailArt] Expected: {expected}, Actual: {nailArtName}({arrowDir}), Result: SUCCESS");
            return result;
        }
    }

    private void PlayMakerFSM_OnEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
    {
        orig(self);

        // 检测 Spell Control FSM
        if (self.FsmName == "Spell Control" && self.gameObject == HeroController.instance?.gameObject)
        {
            ModifySpellControlFSM(self);
        }
    }

    private void ModifySpellControlFSM(PlayMakerFSM fsm)
    {
        // 注入到 QC (Quick Cast) 状态
        InjectSpellAction(fsm, "QC");
        // 注入到 Spell Choice (普通施法) 状态
        InjectSpellAction(fsm, "Spell Choice");
        
        Log("Spell Control FSM modified");
    }

    private void InjectSpellAction(PlayMakerFSM fsm, string stateName)
    {
        var state = fsm.Fsm.GetState(stateName);
        if (state == null) return;

        // 检查是否已注入（避免重复）
        if (state.Actions.Length > 0 && state.Actions[0] is SpellInterceptAction)
        {
            return;
        }

        var action = new SpellInterceptAction();

        // 在开头插入自定义 Action
        var newActions = new FsmStateAction[state.Actions.Length + 1];
        newActions[0] = action;
        for (int i = 0; i < state.Actions.Length; i++)
        {
            newActions[i + 1] = state.Actions[i];
        }
        state.Actions = newActions;
    }

    private string changeName(string key, string title, string orig)
    {
        return orig;
    }

    public bool ToggleButtonInsideMenu => true;

    public void OnLoadGlobal(Settings settings) => mySettings = settings;

    public Settings OnSaveGlobal() => mySettings;

    private void ToggleModSetting(bool enabled)
    {
        if (HeroController.instance != null)
        {
            var arrowGame = HeroController.instance.GetComponent<ArrowGame>();
            arrowGame?.SetModEnabled(enabled);
        }
    }

    public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? menu)
    {
        List<IMenuMod.MenuEntry> menus = new();
        menus.Add(
            new()
            {
                Values = new string[]
                {
                    Language.Language.Get("MOH_ON", "MainMenu"),
                    Language.Language.Get("MOH_OFF", "MainMenu"),
                },
                Saver = i => {
                    mySettings.on = i == 0;
                    ToggleModSetting(mySettings.on);
                },
                Loader = () => mySettings.on ? 0 : 1,
                Name = "StubbornKnight"
            }
        );
        return menus;
    }
}
