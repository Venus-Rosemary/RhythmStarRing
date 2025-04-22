using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;
using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;
using TMPro;
using System.Linq;

public class LightSphereGeneration : Singleton<LightSphereGeneration>
{
    [Header("基础设置")]
    public Transform targetEnd;
    public List<Transform> allCreatPoint=new List<Transform>();

    [Header("关卡设置")]
    public bool isFirstPass = false;
    public bool isSecondPass = false;
    public List<GameObject> FirstPassObject=new List<GameObject>();//第一关生成光球
    public List<GameObject> SecondPassObject = new List<GameObject>();//第二关生成光球

    [Header("光球设置")]
    private float LightSphereSpeed = 0;//光球速度
    public float minSpeed = 0;
    public float maxSpeed = 0;
    public float GenerationInterval = 0;
    private float GenInt = 0;

    [Header("设置检测范围")]
    public float missRadius = 5f;
    public float goodRadius = 4f;
    public float perfectRadius = 2f;
    [SerializeField] private List<GameObject> allActiveSphere = new List<GameObject>();//所有活跃的光球
    [SerializeField] private GameObject nearestPoint;//最近的一个光球

    [Header("液体设置")]
    public Material liquidMaterial; // 液体材质
    private readonly float minFillAmount = 7.3f;
    private readonly float maxFillAmount = -6.3f;

    [Header("分数设置")]
    private readonly int maxScore = 100;//最大总分值
    [SerializeField] private int currentScore = 0;
    public int CurrentScore => currentScore; // 只读属性，供其他脚本获取分数
    public TMP_Text numText;

    [Header("特效设置")]
    public GameObject PerfectVFX;
    public GameObject GoodVFX;
    public GameObject MissVFX;
    public int poolSize = 5; // 每种特效的对象池大小
    public Transform poolParent;//对象存放位置
    private readonly float vfxDuration = 2f;

    private Queue<GameObject> perfectPool = new Queue<GameObject>();
    private Queue<GameObject> goodPool = new Queue<GameObject>();
    private Queue<GameObject> missPool = new Queue<GameObject>();

    [Header("UI设置")]
    public GameObject scoreUIPrefab;  // 3D评分UI预制体
    public Transform uiSpawnPoint;    // UI生成位置
    public float uiMoveSpeed = 1f;    // UI上移速度
    public float uiFadeTime = 1f;     // UI渐隐时间
    private Queue<GameObject> uiPool = new Queue<GameObject>();

    [Header("音效设置")]
    public AudioClip highToneSound;    // 高音效
    public AudioClip middleToneSound;  // 中音效
    public AudioClip lowToneSound;     // 低音效
    private AudioSource audioSource;

    [Header("游戏流程设置")]
    public GameObject startUI;        // 开始界面
    public GameObject gameUI;         // 游戏界面
    public GameObject endUI;          // 结束界面
    public float gameTime = 120f;     // 游戏时间（秒）
    private float currentTime;        // 当前剩余时间
    public TMP_Text timeText;         // 时间显示
    private bool isPlaying = false;   // 游戏是否进行中
    public List<GameObject> AllButton=new List<GameObject>();//场景中的按钮
    private bool hasActivatedSecondPhase = false; // 是否已激活第二阶段按钮

    void Start()
    {
        GenInt = GenerationInterval;
        InitializeVFXPools();
        InitializeUIPool();
        audioSource = gameObject.AddComponent<AudioSource>();

        InitializeGame();
    }

    private void InitializeGame()
    {
        // 初始显示开始界面
        startUI.SetActive(true);
        gameUI.SetActive(false);
        endUI.SetActive(false);

        // 确保所有按钮都处于关闭状态
        foreach (var button in AllButton)
        {
            button.SetActive(false);
        }
        hasActivatedSecondPhase = false;

        // 重置游戏数据
        currentScore = 0;
        currentTime = gameTime;
        isPlaying = false;

        // 清理场景
        ClearAllSpheres();

        // 重置液体高度
        liquidMaterial.SetFloat("_FillAmount", minFillAmount);

        // 更新UI显示
        numText.text = $"分值：{currentScore}";
        timeText.text = $"时间：{Mathf.CeilToInt(currentTime)}";
    }

    void Update()
    {
        if (isPlaying)
        {
            UpdateGameTime();
        }
    }

    private void UpdateGameTime()
    {
        currentTime -= Time.deltaTime;
        timeText.text = $"时间：{Mathf.CeilToInt(currentTime)}";

        // 第二关按钮控制
        if (isSecondPass)
        {
            if (currentTime <= gameTime / 2 && !hasActivatedSecondPhase)
            {
                for (int i = 0; i < AllButton.Count; i++)
                {
                    AllButton[i].SetActive(i>5);
                }
                hasActivatedSecondPhase = true;
            }
        }

        if (currentTime <= 0 || currentScore >= maxScore)
        {
            GameOver();
        }
    }

    private void OnEnable()
    {

    }

    #region 游戏流程

    //关卡设置
    public void LevelSetting()//开始游戏
    {
        if (isFirstPass)
        {
            GenerateSphere(FirstPassObject);
        }

        if (isSecondPass)
        {
            GenerateSphere(SecondPassObject);
        }
    }

    // 开始游戏（由UI按钮调用）
    public void StartFirstLevel()
    {
        isFirstPass = true;
        isSecondPass = false;
        // 激活前三个按钮
        for (int i = 0; i < AllButton.Count; i++)
        {
            AllButton[i].SetActive(i < 3);
        }
        StartGame();
    }

    public void StartSecondLevel()
    {
        isFirstPass = false;
        isSecondPass = true;
        // 初始只激活中间三个按钮
        for (int i = 0; i < AllButton.Count; i++)
        {
            AllButton[i].SetActive(i >= 3 && i < 6);
        }
        StartGame();
    }

    private void StartGame()
    {
        startUI.SetActive(false);
        gameUI.SetActive(true);
        endUI.SetActive(false);
        isPlaying = true;
        LevelSetting(); // 开始生成光球
    }

    private void GameOver()
    {
        isPlaying = false;

        // 停止生成光球
        //CancelInvoke();

        // 清理场景
        ClearAllSpheres();

        // 显示结束界面
        gameUI.SetActive(false);
        endUI.SetActive(true);

        // 更新结算界面分数显示
        TMP_Text finalScoreText = endUI.GetComponentInChildren<TMP_Text>();
        if (finalScoreText != null)
        {
            finalScoreText.text = $"最终得分：{currentScore}";
        }
    }

    // UI按钮回调方法
    public void ReturnToMenu()
    {
        InitializeGame();
    }

    public void RestartGame()
    {
        if (isFirstPass)
        {
            // 重置游戏数据
            currentScore = 0;
            currentTime = gameTime;

            // 重置液体高度
            liquidMaterial.SetFloat("_FillAmount", minFillAmount);

            // 更新UI显示
            numText.text = $"分值：{currentScore}";
            timeText.text = $"时间：{Mathf.CeilToInt(currentTime)}";
        
            StartFirstLevel();
        }
        else
        {            
            // 重置游戏数据
            currentScore = 0;
            currentTime = gameTime;

            // 重置液体高度
            liquidMaterial.SetFloat("_FillAmount", minFillAmount);

            // 更新UI显示
            numText.text = $"分值：{currentScore}";
            timeText.text = $"时间：{Mathf.CeilToInt(currentTime)}";

            StartSecondLevel();
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

    //清理
    private void ClearAllSpheres()
    {
        // 清理所有活跃的光球
        foreach (var sphere in allActiveSphere.ToList())
        {
            if (sphere != null)
            {
                DestroyCurrent(sphere);
            }
        }
        allActiveSphere.Clear();
    }



    #region 光球生成设置
    //生成光球
    private void GenerateSphere(List<GameObject> productObject)
    {
        if (!isPlaying) return;

        if (allCreatPoint!=null)
        {
            int randomPoint = Random.Range(0, allCreatPoint.Count);
            if (productObject != null)
            {
                int randomObject = Random.Range(0, productObject.Count);
                GameObject SphereObj = Instantiate(productObject[randomObject], allCreatPoint[randomPoint].position,Quaternion.identity);
                SphereObj.transform.SetParent(transform);

                allActiveSphere.Add(SphereObj);

                if (isFirstPass)
                {
                    LightSphereSpeed = 2f + ((float)currentScore / maxScore) * 2f;//速度范围 2 - 3.99f
                    GenInt = GenerationInterval;
                }

                if (isSecondPass)
                {
                    LightSphereSpeed = 2f;//第二关匀速
                    GenInt = GenerationInterval * 1.5f;
                }

                LightSphereControl lightSphereControl= SphereObj.GetComponent<LightSphereControl>();
                lightSphereControl.SetEndPointTarget(targetEnd, LightSphereSpeed, true);

                // 根据音色类型播放对应音效
                switch (lightSphereControl.tone)
                {
                    case ToneType.High:
                        PlaySound(highToneSound);
                        break;
                    case ToneType.Middle:
                        PlaySound(middleToneSound);
                        break;
                    case ToneType.Low:
                        PlaySound(lowToneSound);
                        break;
                }
            }

            DOVirtual.DelayedCall(GenInt, ()=> GenerateSphere(productObject));
        }
    }

    //光球销毁
    public void DestroyCurrent(GameObject gameObject)
    {
        if (allActiveSphere.Contains(gameObject))
        {
            allActiveSphere.Remove(gameObject);
            gameObject.GetComponent<LightSphereControl>().DoTweenKill();
            //可以添加特效
            Destroy(gameObject);
        }
    }
    #endregion

    #region 找寻所有活跃光中离得最近的一个
    //判定检测
    public void CheckScore(ButtonState buttonState, ColorType colorType, ToneType toneType)
    {
        //找寻所有活跃光中离得最近的一个
        if (allActiveSphere!=null)
        {
            if (allActiveSphere.Count==0)
            {
                Debug.Log("场景中还没有活跃的光球");
                return;
            }
            float distanceMax = 1000;
            foreach (var item in allActiveSphere)
            {
                float distanceMin = Vector3.Distance(item.transform.position, targetEnd.position);
                if (distanceMin< distanceMax)
                {
                    distanceMax=distanceMin;
                    nearestPoint=item;
                }
            }
        }
        switch (buttonState)
        {
            case ButtonState.both:
                //两个都检测
                FirstPassCheck(nearestPoint, colorType, toneType);
                break;
            case ButtonState.color:
                // 根据声音判断颜色
                ToneOnlyCheck(nearestPoint, toneType);
                break;
            case ButtonState.tone:
                // 根据颜色判断声音
                ColorOnlyCheck(nearestPoint, colorType);
                break;
        }
    }

    #endregion

    #region 特效对象池设置
    private void InitializeVFXPools()
    {
        // 初始化Perfect特效池
        for (int i = 0; i < poolSize; i++)
        {
            GameObject perfect = Instantiate(PerfectVFX, poolParent);
            perfect.SetActive(false);
            perfectPool.Enqueue(perfect);
        }

        // 初始化Good特效池
        for (int i = 0; i < poolSize; i++)
        {
            GameObject good = Instantiate(GoodVFX, poolParent);
            good.SetActive(false);
            goodPool.Enqueue(good);
        }

        // 初始化Miss特效池
        for (int i = 0; i < poolSize; i++)
        {
            GameObject miss = Instantiate(MissVFX, poolParent);
            miss.SetActive(false);
            missPool.Enqueue(miss);
        }
    }

    private void PlayVFX(Queue<GameObject> pool, Vector3 position)
    {
        if (pool.Count == 0) return;

        GameObject vfx = pool.Dequeue();
        vfx.transform.position = position;
        vfx.SetActive(true);

        DOVirtual.DelayedCall(vfxDuration, () =>
        {
            vfx.SetActive(false);
            pool.Enqueue(vfx);
        });
    }
    #endregion

    #region UI设置
    private void InitializeUIPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject ui = Instantiate(scoreUIPrefab, poolParent);
            ui.SetActive(false);
            uiPool.Enqueue(ui);
        }
    }

    private void ShowScoreUI(string scoreText, Vector3 position)
    {
        if (uiPool.Count == 0) return;

        GameObject ui = uiPool.Dequeue();
        ui.transform.position = position;
        ui.SetActive(true);

        // 获取UI组件
        TMP_Text text = ui.GetComponentInChildren<TMP_Text>();
        CanvasGroup canvasGroup = ui.GetComponent<CanvasGroup>();

        // 设置文本
        text.text = scoreText;
        canvasGroup.alpha = 1f;

        // 创建动画序列
        DG.Tweening.Sequence sequence = DOTween.Sequence();

        // 向上移动
        sequence.Join(ui.transform.DOMoveY(ui.transform.position.y + 1f, uiMoveSpeed));
        // 渐隐
        sequence.Join(canvasGroup.DOFade(0, uiFadeTime));

        // 动画完成后回收到对象池
        sequence.OnComplete(() =>
        {
            ui.SetActive(false);
            uiPool.Enqueue(ui);
        });
    }
    #endregion

    #region 音效设置
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private AudioClip ObtainObjectTone(GameObject nearObject)
    {
        LightSphereControl lightBall = nearObject.GetComponent<LightSphereControl>();
        AudioClip toneSound = null;

        // 获取对应音色的音效
        switch (lightBall.tone)
        {
            case ToneType.High:
                toneSound = highToneSound;
                break;
            case ToneType.Middle:
                toneSound = middleToneSound;
                break;
            case ToneType.Low:
                toneSound = lowToneSound;
                break;
        }
        return toneSound;
    }

    #endregion

    #region 得分部分
    private void UpdateLiquidLevel()
    {
        if (liquidMaterial != null)
        {
            // 将当前分数映射到液面高度范围
            float fillAmount = Mathf.Lerp(minFillAmount, maxFillAmount, (float)currentScore / maxScore);
            liquidMaterial.SetFloat("_FillAmount", fillAmount);
        }
    }
    private void AddScore(int score)
    {
        currentScore += score;
        currentScore = Mathf.Clamp(currentScore, 0, maxScore);
        numText.text = $"分值： {currentScore}";
        Debug.Log($"当前分数: {currentScore}");
        UpdateLiquidLevel(); // 更新液面高度
    }

    //得分部分
    private void HandleScore(GameObject nearObject, float distance)
    {
        Vector3 effectPosition = nearObject.transform.position;

        //AudioClip toneSound = ObtainObjectTone(nearObject);

        if (distance <= perfectRadius)
        {
            Debug.Log("Perfect");
            PlayVFX(perfectPool, effectPosition);
            ShowScoreUI("Perfect", uiSpawnPoint.position);
            //PlaySound(toneSound);
            AddScore(3);
            DestroyCurrent(nearObject);
            return;
        }
        else if (distance <= goodRadius)
        {
            Debug.Log("Good");
            PlayVFX(goodPool, effectPosition);
            ShowScoreUI("Good", uiSpawnPoint.position);
            //PlaySound(toneSound);
            AddScore(1);
            DestroyCurrent(nearObject);
            return;
        }
        else if (distance <= missRadius)
        {
            Debug.Log("Miss");
            PlayVFX(missPool, effectPosition);
            ShowScoreUI("Miss", uiSpawnPoint.position);
            //PlaySound(toneSound);
            AddScore(0);
            DestroyCurrent(nearObject);
            return;
        }
    }
    #endregion

    #region 第一关的检测---1对1


    public void FirstPassCheck(GameObject nearObject, ColorType colorType, ToneType toneType)
    {
        if (nearObject != null)
        {
            LightSphereControl lightBall = nearObject.GetComponent<LightSphereControl>();

            //AudioClip toneSound = ObtainObjectTone(nearObject);

            if (lightBall.color == colorType && lightBall.tone == toneType)
            {
                Debug.Log($"匹配正确!");
                float distance = Vector3.Distance(nearObject.transform.position, targetEnd.position);

                if (distance > missRadius) return;
                HandleScore(nearObject, distance);
            }
            else
            {
                float distance = Vector3.Distance(nearObject.transform.position, targetEnd.position);
                if (distance > missRadius) return;
                if (distance <= missRadius)
                {
                    Debug.Log("类型不匹配!   Miss");

                    PlayVFX(missPool, nearObject.transform.position);
                    ShowScoreUI("Miss", uiSpawnPoint.position);
                    //PlaySound(toneSound);
                    AddScore(0);
                    DestroyCurrent(nearObject);
                    return;
                }
            }
        }
    }
    #endregion

    #region 第二关---只检测颜色
    public void ColorOnlyCheck(GameObject nearObject, ColorType colorType)
    {
        if (nearObject != null)
        {
            LightSphereControl lightBall = nearObject.GetComponent<LightSphereControl>();

            //AudioClip toneSound = ObtainObjectTone(nearObject);

            if (lightBall.color == colorType)
            {
                Debug.Log($"颜色匹配正确!");
                float distance = Vector3.Distance(nearObject.transform.position, targetEnd.position);

                if (distance > missRadius) return;

                HandleScore(nearObject, distance);
            }
            else
            {
                float distance = Vector3.Distance(nearObject.transform.position, targetEnd.position);
                if (distance > missRadius) return;
                if (distance <= missRadius)
                {
                    Debug.Log("颜色不匹配!   Miss");
                    PlayVFX(missPool, nearObject.transform.position);
                    ShowScoreUI("Miss", uiSpawnPoint.position);
                    //PlaySound(toneSound);
                    AddScore(0);
                    DestroyCurrent(nearObject);
                    return;
                }
            }
        }
    }
    #endregion

    #region 第二关---只检测音色
    public void ToneOnlyCheck(GameObject nearObject, ToneType toneType)
    {
        if (nearObject != null)
        {
            LightSphereControl lightBall = nearObject.GetComponent<LightSphereControl>();

            //AudioClip toneSound = ObtainObjectTone(nearObject);

            if (lightBall.tone == toneType)
            {
                Debug.Log($"音色匹配正确!");
                float distance = Vector3.Distance(nearObject.transform.position, targetEnd.position);

                if (distance > missRadius) return;

                HandleScore(nearObject, distance);
            }
            else
            {
                float distance = Vector3.Distance(nearObject.transform.position, targetEnd.position);
                if (distance > missRadius) return;
                if (distance <= missRadius)
                {
                    Debug.Log("音色不匹配!   Miss");
                    PlayVFX(missPool, nearObject.transform.position);
                    ShowScoreUI("Miss", uiSpawnPoint.position);
                    //PlaySound(toneSound);
                    AddScore(0);
                    DestroyCurrent(nearObject);
                    return;
                }
            }
        }
    }
    #endregion



#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        DrawCircleGizmo(targetEnd.position, missRadius, 36, Color.red);
        DrawCircleGizmo(targetEnd.position, goodRadius, 36, Color.yellow);
        DrawCircleGizmo(targetEnd.position, perfectRadius, 36, Color.green);
    }

    /// <summary>
    ///绘制圆形范围
    /// </summary>
    /// <param name="center">中心点</param>
    /// <param name="radius">半径</param>
    /// <param name="segments">段数，段数越多圆越圆滑，建议36以上</param>
    /// <param name="gizmoColor">颜色</param>
    private void DrawCircleGizmo(Vector3 center, float radius, int segments,Color gizmoColor)
    {
        // 设置 Gizmos 的颜色
        Gizmos.color = gizmoColor;

        // 计算每个分段的角度增量（弧度制）
        float angleIncrement = (2f * Mathf.PI) / segments;

        // 计算圆上的点，并用线段连接它们
        Vector3 previousPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            // 当前角度
            float angle = i * angleIncrement;

            // 计算当前点的位置（假设圆在 XZ 平面上）
            Vector3 currentPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

            // 如果不是第一个点，则连接上一个点和当前点
            if (i > 0)
            {
                Gizmos.DrawLine(previousPoint, currentPoint);
            }

            // 更新上一个点
            previousPoint = currentPoint;
        }
    }
#endif
}
