using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("World")]
    public float groundY = -55.489f;

    [Header("Prefabs")]
    public GameObject P_Ball;
    public GameObject P_Block;
    public GameObject P_BuffCard;
    public GameObject P_InfiniteBlock;
    public GameObject P_ParticleBlue;
    public GameObject P_ParticleRed;

    [Header("Block Sprites")]
    public Sprite normalBlockSprite;
    public Sprite coreBlockSprite;
    public Sprite infiniteBlockSprite;

    [Header("Ball Sprites")]
    public Sprite normalBallSprite;
    public Sprite penetrateBallSprite;
    public Sprite explosiveBallSprite;


    [Header("Scene Objects")]
    public GameObject BallPreview;
    public GameObject Arrow;
    public GameObject GameResultPanel;
    public GameObject BigResultText;

    public Transform BlockGroup;
    public Transform BallGroup;

    public LineRenderer MouseLR;
    public LineRenderer BallLR;

    [Header("UI")]
    public Text RemainShotText;
    public Text BallCountText;
    public Text ResultText;
    public Text StatInfoText;

    [Header("Audio")]
    public AudioSource S_GameOver;
    public AudioSource[] S_Block;

    [Header("Colors")]
    public Color[] blockColor;
    public Color coreColor = Color.yellow;
    public Color infiniteBlockColor = Color.gray;
    public Color buffCardColor = Color.cyan;
    public Color debuffBlockColor = new Color(1f, 0.45f, 0.8f, 1f);

    [Header("Base Stat")]
    public int cost = 10;
    public int maxShotCount = 10;
    public int supportRowCount = 6;
    public int attackPower = 1;

    [Header("Stage Progress")]
    public int currentStage = 1;
    public int maxMiniStage = 3;
    public int miniStageClearBonusShot = 10;

    [Header("Default Random Stage")]
    public bool spawnBuffCard = true;
    public bool spawnInfiniteBlock = true;
    public int infiniteBlockCount = 1;

    [Header("Stage Data Table")]
    public StageData[] stageTable;

    [Header("Aim Preview")]
    public float aimRayDistance = 300f;
    public LayerMask aimHitMask = Physics2D.DefaultRaycastLayers;

    [HideInInspector] public bool shotTrigger;
    [HideInInspector] public bool shotable;
    [HideInInspector] public Vector3 currentLaunchPos;
    [HideInInspector] public Vector3 nextLaunchPos;

    Vector3 firstPos;
    Vector3 secondPos;
    Vector3 gap;
    Vector3 initialLaunchPos;

    int timerCount;
    int launchIndex;
    int remainShotCount;
    int currentMiniStage = 1;

    bool timerStart;
    bool isDie;
    bool isStageClear;
    bool isBuffSelecting;
    bool firstBallLandedThisTurn;

    readonly Quaternion QI = Quaternion.identity;

    [Header("Buff UI")]
    public BuffSelectUI buffSelectUI;

    [Header("Ball Type UI")]
    public Button normalBallButton;
    public Button penetrateBallButton;
    public Button explosiveBallButton;

    [Header("Grid")]
    public int columnCount = 8;
    public float leftX = -49.3f;
    public float rightX = 49.3f;
    public float topY = 51.2f;
    public float rowGap = 12.8f;

    Ball.BallType selectedBallType = Ball.BallType.Normal;
    readonly List<Ball.BallType> currentTurnBallTypes = new List<Ball.BallType>();
    int currentTurnLaunchCount;

    float XGap
    {
        get { return (rightX - leftX) / (columnCount - 1); }
    }


    public class BuffOption
    {
        public BuffType type;
        public int value;

        public BuffOption(BuffType type, int value)
        {
            this.type = type;
            this.value = value;
        }

        public string GetText()
        {
            switch (type)
            {
                case BuffType.ExtraShot:
                    return "잔여 공격 횟수 +" + value;
                case BuffType.ExtraCost:
                    return "코스트 +" + value;
                case BuffType.ExtraAttackPower:
                    return "공격력 +" + value;
            }

            return "";
        }
    }

    public enum BuffType
    {
        ExtraShot,
        ExtraCost,
        ExtraAttackPower
    }

    void Awake()
    {
        Time.timeScale = 1f;
        SetupCamera916();

        remainShotCount = maxShotCount;
        UpdateRemainShotText();

        initialLaunchPos = new Vector3(0f, groundY, 0f);
        currentLaunchPos = initialLaunchPos;
        nextLaunchPos = initialLaunchPos;

        SetupBallTypeButtons();
        InitializeBalls();
        SetSelectedBallType(Ball.BallType.Normal);

        UpdateBallCountText();
        UpdateStatInfoText();

        if (buffSelectUI != null)
            buffSelectUI.Hide();

        if (GameResultPanel != null)
            GameResultPanel.SetActive(false);

        SyncBallPreviewWithSelectedBall();
        StageGenerator();
        HideAimObjects();
    }

    void Update()
    {
        UpdateGame();
    }

    void FixedUpdate()
    {
        FixedUpdateGame();
    }

    void SetupCamera916()
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        Rect rect = camera.rect;
        float scaleheight = ((float)Screen.width / Screen.height) / ((float)9 / 16);
        float scalewidth = 1f / scaleheight;

        if (scaleheight < 1)
        {
            rect.height = scaleheight;
            rect.y = (1f - scaleheight) / 2f;
        }
        else
        {
            rect.width = scalewidth;
            rect.x = (1f - scalewidth) / 2f;
        }

        camera.rect = rect;
    }

    public void Restart()
    {
        Time.timeScale = 1f;

        if (buffSelectUI != null)
            buffSelectUI.Hide();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdateRemainShotText()
    {
        if (RemainShotText != null)
            RemainShotText.text = "남은 발사 : " + remainShotCount;
    }

    public void UpdateBallCountText()
    {
        if (BallCountText != null)
            BallCountText.text = "Cost: " + BallGroup.childCount;
    }

    void UpdateStatInfoText()
    {
        if (StatInfoText != null)
            StatInfoText.text = "스테이지 : " + currentStage + " / 미니 스테이지 : " + currentMiniStage + "\n코스트 : " + cost + " / 공격력 : " + attackPower;
    }

    void InitializeBalls()
    {
        for (int i = BallGroup.childCount - 1; i >= 0; i--)
            Destroy(BallGroup.GetChild(i).gameObject);

        for (int i = 0; i < cost; i++)
        {
            GameObject ballObj = Instantiate(P_Ball, currentLaunchPos, QI, BallGroup);
            Ball ball = ballObj.GetComponent<Ball>();
            if (ball != null)
            {
                ball.Setup(this);
                ApplyBallTypeToBall(ball, selectedBallType);
            }
        }
        RefreshReadyBallDisplay();
    }

    public void AddBalls(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject ballObj = Instantiate(P_Ball, currentLaunchPos, QI, BallGroup);
            Ball ball = ballObj.GetComponent<Ball>();
            if (ball != null)
            {
                ball.Setup(this);
                ApplyBallTypeToBall(ball, selectedBallType);
            }
        }

        UpdateBallCountText();
    }

    public void RebuildBallsToMatchCost()
    {
        for (int i = BallGroup.childCount - 1; i >= cost; i--)
            Destroy(BallGroup.GetChild(i).gameObject);

        while (BallGroup.childCount < cost)
        {
            GameObject ballObj = Instantiate(P_Ball, currentLaunchPos, QI, BallGroup);
            Ball ball = ballObj.GetComponent<Ball>();
            if (ball != null)
                ball.Setup(this);
        }

        RefreshReadyBallDisplay();
        UpdateBallCountText();
    }

    public void ModifyStat(StatType statType, int amount)
    {
        switch (statType)
        {
            case StatType.Cost:
                cost = Mathf.Max(1, cost + amount);
                RebuildBallsToMatchCost();
                break;

            case StatType.AttackPower:
                attackPower = Mathf.Max(1, attackPower + amount);
                break;

            case StatType.RemainShotCount:
                remainShotCount = Mathf.Max(0, remainShotCount + amount);
                UpdateRemainShotText();
                break;
        }

        UpdateStatInfoText();
        BlockColorRefresh();
    }

    float GetActualBallRadius()
    {
        if (P_Ball == null)
            return 0.5f;

        CircleCollider2D circle = P_Ball.GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            float scaleX = Mathf.Abs(P_Ball.transform.localScale.x);
            float scaleY = Mathf.Abs(P_Ball.transform.localScale.y);
            float maxScale = Mathf.Max(scaleX, scaleY);
            return circle.radius * maxScale;
        }

        SpriteRenderer sr = P_Ball.GetComponent<SpriteRenderer>();
        if (sr != null)
            return sr.bounds.extents.x;

        return 0.5f;
    }

    void SetupBallTypeButtons()
    {
        if (normalBallButton != null)
        {
            normalBallButton.onClick.RemoveAllListeners();
            normalBallButton.onClick.AddListener(() => SetSelectedBallType(Ball.BallType.Normal));
        }

        if (penetrateBallButton != null)
        {
            penetrateBallButton.onClick.RemoveAllListeners();
            penetrateBallButton.onClick.AddListener(() => SetSelectedBallType(Ball.BallType.Penetrate));
        }

        if (explosiveBallButton != null)
        {
            explosiveBallButton.onClick.RemoveAllListeners();
            explosiveBallButton.onClick.AddListener(() => SetSelectedBallType(Ball.BallType.Explosive));
        }
    }

    Sprite GetBallSprite(Ball.BallType type)
    {
        switch (type)
        {
            case Ball.BallType.Penetrate:
                return penetrateBallSprite != null ? penetrateBallSprite : normalBallSprite;

            case Ball.BallType.Explosive:
                return explosiveBallSprite != null ? explosiveBallSprite : normalBallSprite;

            default:
                return normalBallSprite;
        }
    }

    int GetBallCost(Ball.BallType type)
    {
        return type == Ball.BallType.Normal ? 1 : 2;
    }

    public void SetSelectedBallType(Ball.BallType type)
    {
        selectedBallType = type;
        SyncBallPreviewWithSelectedBall();
        RefreshReadyBallDisplay();
        RefreshBallTypeButtonState();
    }

    void RefreshBallTypeButtonState()
    {
        Color selectedColor = Color.white;
        Color unselectedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        if (normalBallButton != null && normalBallButton.image != null)
            normalBallButton.image.color = selectedBallType == Ball.BallType.Normal ? selectedColor : unselectedColor;

        if (penetrateBallButton != null && penetrateBallButton.image != null)
            penetrateBallButton.image.color = selectedBallType == Ball.BallType.Penetrate ? selectedColor : unselectedColor;

        if (explosiveBallButton != null && explosiveBallButton.image != null)
            explosiveBallButton.image.color = selectedBallType == Ball.BallType.Explosive ? selectedColor : unselectedColor;
    }

    void RefreshIdleBallVisuals()
    {
        Sprite sprite = GetBallSprite(selectedBallType);

        for (int i = 0; i < BallGroup.childCount; i++)
        {
            Ball ball = BallGroup.GetChild(i).GetComponent<Ball>();
            if (ball == null) continue;
            if (ball.isMoving) continue;

            ball.SetBallType(selectedBallType, sprite);
        }
    }

    void ApplyBallTypeToBall(Ball ball, Ball.BallType type)
    {
        if (ball == null) return;
        ball.SetBallType(type, GetBallSprite(type));
    }

    void RefreshReadyBallDisplay()
    {
        for (int i = 0; i < BallGroup.childCount; i++)
        {
            Ball ball = BallGroup.GetChild(i).GetComponent<Ball>();
            if (ball == null) continue;

            ball.StopBall();
            ball.transform.position = currentLaunchPos;

            // 발사 준비 상태에서는 첫 번째 공만 보이게
            bool shouldShow = (i == 0);
            ball.SetVisible(shouldShow);

            if (shouldShow)
                ApplyBallTypeToBall(ball, selectedBallType);
        }
    }

    void SyncBallPreviewWithSelectedBall()
    {
        if (BallPreview == null)
            return;

        SpriteRenderer previewSR = BallPreview.GetComponent<SpriteRenderer>();
        if (previewSR == null)
            return;

        previewSR.sprite = GetBallSprite(selectedBallType);
        previewSR.color = Color.white;
        previewSR.sortingLayerName = "Default";
        previewSR.sortingOrder = 999;

        if (P_Ball != null)
            BallPreview.transform.localScale = P_Ball.transform.localScale;
    }

    void BuildCurrentTurnBallPlan()
    {
        currentTurnBallTypes.Clear();

        int remainCost = cost;

        while (remainCost > 0)
        {
            int selectedCost = GetBallCost(selectedBallType);

            if (selectedBallType != Ball.BallType.Normal && remainCost >= selectedCost)
            {
                currentTurnBallTypes.Add(selectedBallType);
                remainCost -= selectedCost;
            }
            else
            {
                currentTurnBallTypes.Add(Ball.BallType.Normal);
                remainCost -= 1;
            }
        }

        currentTurnLaunchCount = currentTurnBallTypes.Count;
    }

    public void SetNextLaunchPos(Vector3 pos)
    {
        if (firstBallLandedThisTurn) return;

        firstBallLandedThisTurn = true;
        nextLaunchPos = new Vector3(pos.x, groundY, 0f);
    }

    StageData GetCurrentStageData()
    {
        if (stageTable == null || stageTable.Length == 0)
            return null;

        for (int i = 0; i < stageTable.Length; i++)
        {
            if (stageTable[i] != null && stageTable[i].stageId == currentStage)
                return stageTable[i];
        }

        int fallbackIndex = currentStage - 1;
        if (fallbackIndex >= 0 && fallbackIndex < stageTable.Length)
            return stageTable[fallbackIndex];

        return null;
    }

    MiniStageData GetCurrentMiniStageData()
    {
        StageData stageData = GetCurrentStageData();
        if (stageData == null || stageData.miniStages == null)
            return null;

        int index = currentMiniStage - 1;
        if (index < 0 || index >= stageData.miniStages.Count)
            return null;

        return stageData.miniStages[index];
    }

    public void StageGenerator()
    {
        ClearBlocks();

        MiniStageData miniData = GetCurrentMiniStageData();

        if (miniData != null)
        {
            if (miniData.useFixedPattern)
                GenerateFixedMiniStage(miniData);
            else
                GenerateRandomMiniStage(miniData.supportRowCount, miniData.spawnBuffCard, miniData.spawnInfiniteBlock, miniData.infiniteBlockCount);

            ApplyGimmicks(miniData, StageGimmickTriggerType.OnStageStart);
        }
        else
        {
            GenerateRandomMiniStage(supportRowCount, spawnBuffCard, spawnInfiniteBlock, infiniteBlockCount);
        }

        BlockColorRefresh();
        UpdateStatInfoText();
    }

    void ClearBlocks()
    {
        for (int i = BlockGroup.childCount - 1; i >= 0; i--)
            Destroy(BlockGroup.GetChild(i).gameObject);
    }

    void GenerateRandomMiniStage(int rowCount, bool allowBuffCard, bool allowInfiniteBlock, int randomInfiniteBlockCount)
    {
        bool[,] occupied = new bool[rowCount + 1, columnCount];

        int coreIndex = Random.Range(1, columnCount - 1);
        Vector3 corePos = new Vector3(leftX + coreIndex * XGap, topY, 0f);
        CreateCoreBlock(corePos);

        for (int row = 1; row <= rowCount; row++)
        {
            float y = topY - (rowGap * row);
            HashSet<int> usedCols = new HashSet<int> { coreIndex };

            int randomExtraCount = Random.Range(3, 7);
            while (usedCols.Count < randomExtraCount + 1)
                usedCols.Add(Random.Range(0, columnCount));

            foreach (int col in usedCols)
            {
                float x = leftX + col * XGap;
                Vector3 spawnPos = new Vector3(x, y, 0f);

                int hp = Random.Range(cost - 3, cost + 4);
                if (hp < 1) hp = 1;

                CreateNormalBlock(spawnPos, hp);
                occupied[row, col] = true;
            }
        }

        List<Vector2Int> emptySlots = new List<Vector2Int>();
        for (int row = 1; row <= rowCount; row++)
        {
            for (int col = 0; col < columnCount; col++)
            {
                if (!occupied[row, col])
                    emptySlots.Add(new Vector2Int(row, col));
            }
        }

        if (allowBuffCard && emptySlots.Count > 0)
        {
            int idx = Random.Range(0, emptySlots.Count);
            Vector2Int slot = emptySlots[idx];
            emptySlots.RemoveAt(idx);

            Vector3 pos = new Vector3(leftX + slot.y * XGap, topY - (rowGap * slot.x), 0f);
            CreateBuffCardBlock(pos);
        }

        if (allowInfiniteBlock)
        {
            for (int i = 0; i < randomInfiniteBlockCount; i++)
            {
                if (emptySlots.Count <= 0) break;

                int idx = Random.Range(0, emptySlots.Count);
                Vector2Int slot = emptySlots[idx];
                emptySlots.RemoveAt(idx);

                Vector3 pos = new Vector3(leftX + slot.y * XGap, topY - (rowGap * slot.x), 0f);
                CreateInfiniteBlock(pos);
            }
        }
    }

    void GenerateFixedMiniStage(MiniStageData data)
    {
        if (data == null)
            return;

        for (int i = 0; i < data.fixedBlocks.Count; i++)
        {
            StageBlockData blockData = data.fixedBlocks[i];
            Vector3 pos = GetGridWorldPosition(blockData.row, blockData.col);
            CreateBlockFromData(pos, blockData);
        }
    }

    Vector3 GetGridWorldPosition(int row, int col)
    {
        col = Mathf.Clamp(col, 0, columnCount - 1);
        row = Mathf.Max(0, row);

        return new Vector3(leftX + col * XGap, topY - rowGap * row, 0f);
    }

    void CreateBlockFromData(Vector3 pos, StageBlockData data)
    {
        switch (data.blockType)
        {
            case Block.BlockType.Core:
                CreateCoreBlock(pos);
                break;

            case Block.BlockType.Infinite:
                CreateInfiniteBlock(pos);
                break;

            case Block.BlockType.BuffCard:
                CreateBuffCardBlock(pos);
                break;

            case Block.BlockType.Debuff:
                CreateDebuffBlock(pos, Mathf.Max(1, data.hp), data.debuffStatType, data.debuffAmount);
                break;

            default:
                CreateNormalBlock(pos, Mathf.Max(1, data.hp));
                break;
        }
    }

    void CreateCoreBlock(Vector3 pos)
    {
        GameObject obj = Instantiate(P_Block, pos, QI, BlockGroup);
        obj.name = "CoreBlock";

        Block block = obj.GetComponent<Block>();
        if (block != null)
            block.Setup(this, Block.BlockType.Core, 0);
    }

    void CreateNormalBlock(Vector3 pos, int hp)
    {
        GameObject obj = Instantiate(P_Block, pos, QI, BlockGroup);
        obj.name = "Block";

        Block block = obj.GetComponent<Block>();
        if (block != null)
            block.Setup(this, Block.BlockType.Normal, hp);
    }

    void CreateBuffCardBlock(Vector3 pos)
    {
        GameObject prefab = P_BuffCard != null ? P_BuffCard : P_Block;
        GameObject obj = Instantiate(prefab, pos, QI, BlockGroup);
        obj.name = "BuffCard";

        Block block = obj.GetComponent<Block>();
        if (block != null)
            block.Setup(this, Block.BlockType.BuffCard, 0);
    }

    void CreateInfiniteBlock(Vector3 pos)
    {
        GameObject prefab = P_InfiniteBlock != null ? P_InfiniteBlock : P_Block;
        GameObject obj = Instantiate(prefab, pos, QI, BlockGroup);
        obj.name = "InfiniteBlock";

        Block block = obj.GetComponent<Block>();
        if (block != null)
            block.Setup(this, Block.BlockType.Infinite, 0);
    }

    void CreateDebuffBlock(Vector3 pos, int hp, StatType statType, int amount)
    {
        GameObject obj = Instantiate(P_Block, pos, QI, BlockGroup);
        obj.name = "DebuffBlock";

        Block block = obj.GetComponent<Block>();
        if (block != null)
            block.SetupDebuff(this, hp, statType, amount);
    }

    public void ProcessBallHit(Ball ball, Block hitBlock)
    {
        if (ball == null || hitBlock == null)
            return;

        DealDamage(hitBlock);

        switch (ball.ballType)
        {
            case Ball.BallType.Penetrate:
                {
                    Block upperBlock = FindAdjacentBlock(hitBlock, 0, 1);
                    if (upperBlock != null && upperBlock != hitBlock)
                        DealDamage(upperBlock);
                    break;
                }

            case Ball.BallType.Explosive:
                {
                    Block leftBlock = FindAdjacentBlock(hitBlock, -1, 0);
                    Block rightBlock = FindAdjacentBlock(hitBlock, 1, 0);

                    if (leftBlock != null && leftBlock != hitBlock)
                        DealDamage(leftBlock);

                    if (rightBlock != null && rightBlock != hitBlock)
                        DealDamage(rightBlock);
                    break;
                }
        }
    }

    void DealDamage(Block block)
    {
        if (block == null) return;
        block.OnHit(attackPower);
    }

    Block FindAdjacentBlock(Block origin, int colOffset, int rowOffset)
    {
        if (origin == null) return null;

        Vector3 targetPos = origin.transform.position + new Vector3(colOffset * XGap, rowOffset * rowGap, 0f);
        return FindBlockAtPosition(targetPos, 0.4f);
    }

    Block FindBlockAtPosition(Vector3 targetPos, float tolerance)
    {
        for (int i = 0; i < BlockGroup.childCount; i++)
        {
            Block block = BlockGroup.GetChild(i).GetComponent<Block>();
            if (block == null) continue;

            if (Vector2.Distance(block.transform.position, targetPos) <= tolerance)
                return block;
        }

        return null;
    }

    void ApplyGimmicks(MiniStageData data, StageGimmickTriggerType triggerType)
    {
        if (data == null || data.gimmicks == null)
            return;

        for (int i = 0; i < data.gimmicks.Count; i++)
        {
            StageGimmickData gimmick = data.gimmicks[i];
            if (gimmick == null) continue;
            if (gimmick.triggerType != triggerType) continue;

            ModifyStat(gimmick.statType, -Mathf.Abs(gimmick.amount));
        }
    }

    public void BlockColorRefresh()
    {
        for (int i = 0; i < BlockGroup.childCount; i++)
        {
            Block block = BlockGroup.GetChild(i).GetComponent<Block>();
            if (block != null)
                block.RefreshView();
        }
    }

    public void OpenBuffSelection()
    {
        if (isBuffSelecting) return;

        isBuffSelecting = true;
        Time.timeScale = 0f;

        List<BuffOption> options = new List<BuffOption>
        {
            new BuffOption(BuffType.ExtraShot, Random.Range(3, 8)),
            new BuffOption(BuffType.ExtraCost, Random.Range(3, 8)),
            new BuffOption(BuffType.ExtraAttackPower, Random.Range(1, 4))
        };

        for (int i = 0; i < options.Count; i++)
        {
            int rand = Random.Range(i, options.Count);
            BuffOption temp = options[i];
            options[i] = options[rand];
            options[rand] = temp;
        }

        if (buffSelectUI == null)
        {
            Debug.LogError("buffSelectUI가 GameManager에 연결되지 않았습니다.");
            return;
        }

        buffSelectUI.Show(this, options);
    }

    public void ApplyBuff(BuffOption option)
    {
        switch (option.type)
        {
            case BuffType.ExtraShot:
                ModifyStat(StatType.RemainShotCount, option.value);
                break;

            case BuffType.ExtraCost:
                ModifyStat(StatType.Cost, option.value);
                break;

            case BuffType.ExtraAttackPower:
                ModifyStat(StatType.AttackPower, option.value);
                break;
        }
    }

    public void CloseBuffSelection()
    {
        isBuffSelecting = false;
        Time.timeScale = 1f;

        if (buffSelectUI != null)
            buffSelectUI.Hide();
    }

    public void GameClear()
    {
        if (isDie) return;

        if (currentMiniStage < maxMiniStage)
        {
            currentMiniStage++;
            ModifyStat(StatType.RemainShotCount, miniStageClearBonusShot);

            ClearBlocks();
            ResetBallsToInitialPosition();
            StageGenerator();
            return;
        }

        if (isStageClear) return;

        Time.timeScale = 1f;
        isStageClear = true;
        timerStart = false;
        shotable = false;
        isBuffSelecting = false;

        if (buffSelectUI != null)
            buffSelectUI.Hide();

        HideAimObjects();
        StopAllBalls();

        if (GameResultPanel != null)
            GameResultPanel.SetActive(true);

        if (BigResultText != null)
        {
            BigResultText.SetActive(true);
            Text titleText = BigResultText.GetComponent<Text>();
            if (titleText != null)
                titleText.text = "스테이지 클리어";
        }

        if (ResultText != null)
            ResultText.text = "모든 미니 스테이지를 클리어했습니다!";
    }

    public void GameFail()
    {
        if (isStageClear || isDie) return;

        Time.timeScale = 1f;
        isDie = true;
        timerStart = false;
        shotable = false;
        isBuffSelecting = false;

        if (buffSelectUI != null)
            buffSelectUI.Hide();

        HideAimObjects();
        StopAllBalls();

        if (P_ParticleBlue != null)
            Destroy(Instantiate(P_ParticleBlue, currentLaunchPos, QI), 1f);

        if (GameResultPanel != null)
            GameResultPanel.SetActive(true);

        if (BigResultText != null)
        {
            BigResultText.SetActive(true);
            Text titleText = BigResultText.GetComponent<Text>();
            if (titleText == null)
                titleText = BigResultText.GetComponentInChildren<Text>();

            if (titleText != null)
                titleText.text = "게임 오버";
        }

        if (ResultText != null)
            ResultText.text = "실패! 발사 횟수를 모두 사용했습니다.";

        if (S_GameOver != null)
            S_GameOver.Play();
    }

    void HideAimObjects()
    {
        if (BallPreview != null)
            BallPreview.SetActive(false);

        if (Arrow != null)
            Arrow.SetActive(false);

        if (MouseLR != null)
        {
            MouseLR.positionCount = 2;
            MouseLR.SetPosition(0, Vector3.zero);
            MouseLR.SetPosition(1, Vector3.zero);
        }

        if (BallLR != null)
        {
            BallLR.positionCount = 2;
            BallLR.SetPosition(0, Vector3.zero);
            BallLR.SetPosition(1, Vector3.zero);
        }
    }

    void StopAllBalls()
    {
        for (int i = 0; i < BallGroup.childCount; i++)
        {
            Ball ball = BallGroup.GetChild(i).GetComponent<Ball>();
            if (ball != null)
                ball.StopBall();
        }
    }

    void ResetBallsToInitialPosition()
    {
        currentLaunchPos = initialLaunchPos;
        nextLaunchPos = initialLaunchPos;

        timerStart = false;
        shotTrigger = false;
        shotable = true;
        firstBallLandedThisTurn = false;
        currentTurnLaunchCount = 0;
        currentTurnBallTypes.Clear();

        RebuildBallsToMatchCost();

        for (int i = 0; i < BallGroup.childCount; i++)
        {
            Transform ballTr = BallGroup.GetChild(i);
            Ball ball = ballTr.GetComponent<Ball>();

            if (ball != null)
            {
                ball.StopBall();
                ball.Setup(this);
                ApplyBallTypeToBall(ball, selectedBallType);
            }

            ballTr.position = currentLaunchPos;
        }

        SyncBallPreviewWithSelectedBall();
        RefreshReadyBallDisplay();
        HideAimObjects();
        UpdateStatInfoText(); ;
    }

    Vector3 GetAimEndPoint(Vector3 origin, Vector3 dir)
    {
        float ballRadius = GetActualBallRadius();
        Vector3 castOrigin = origin + dir.normalized * (ballRadius + 0.05f);
        RaycastHit2D[] hits = Physics2D.CircleCastAll(castOrigin, ballRadius, dir, aimRayDistance, aimHitMask);

        if (hits != null && hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i].collider;
                if (col == null) continue;
                if (hits[i].distance < 0.05f) continue;

                Block block = col.GetComponent<Block>();
                if (block != null && block.blockType == Block.BlockType.BuffCard)
                    continue;

                Vector2 endPoint = hits[i].point + hits[i].normal * ballRadius;
                return new Vector3(endPoint.x, endPoint.y, -1f);
            }
        }

        Vector3 fallback = castOrigin + dir.normalized * aimRayDistance;
        fallback.z = -1f;
        return fallback;
    }

    void UpdateGame()
    {
        if (isDie || isStageClear || isBuffSelecting) return;

        shotable = true;
        for (int i = 0; i < BallGroup.childCount; i++)
        {
            Ball ball = BallGroup.GetChild(i).GetComponent<Ball>();
            if (ball != null && ball.isMoving)
            {
                shotable = false;
                break;
            }
        }

        if (shotable)
        {
            currentLaunchPos = nextLaunchPos;
            RefreshReadyBallDisplay();
        }

        if (shotTrigger && shotable)
        {
            shotTrigger = false;

            if (remainShotCount <= 0)
            {
                GameFail();
                return;
            }
        }

        if (!shotable) return;
        if (remainShotCount <= 0) return;

        if (Input.GetMouseButtonDown(0))
        {
            firstPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
            firstPos.z = 0f;
            secondPos = firstPos;
        }

        if (Input.GetMouseButton(0))
        {
            secondPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
            secondPos.z = 0f;

            Vector3 drag = secondPos - firstPos;

            if (drag.magnitude >= 0.5f)
            {
                gap = drag.normalized;

                if (gap.y < 0.2f)
                    gap.y = 0.2f;

                gap = gap.normalized;
                SyncBallPreviewWithSelectedBall();
                Vector3 aimEndPos = GetAimEndPoint(currentLaunchPos, gap);

                if (Arrow != null)
                {
                    Arrow.transform.position = currentLaunchPos;
                    Arrow.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(gap.y, gap.x) * Mathf.Rad2Deg);
                    Arrow.SetActive(true);
                }

                if (MouseLR != null)
                {
                    MouseLR.positionCount = 2;
                    MouseLR.SetPosition(0, firstPos);
                    MouseLR.SetPosition(1, secondPos);
                }

                if (BallLR != null)
                {
                    BallLR.positionCount = 2;
                    BallLR.SetPosition(0, currentLaunchPos);
                    BallLR.SetPosition(1, aimEndPos);
                }

                if (BallPreview != null)
                {
                    SyncBallPreviewWithSelectedBall();
                    BallPreview.transform.position = new Vector3(aimEndPos.x, aimEndPos.y, -1f);
                    BallPreview.transform.rotation = Quaternion.identity;
                    BallPreview.SetActive(true);
                }
            }
            else
            {
                HideAimObjects();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector3 drag = secondPos - firstPos;

            if (drag.magnitude >= 0.5f)
            {
                MiniStageData miniData = GetCurrentMiniStageData();
                if (miniData != null)
                    ApplyGimmicks(miniData, StageGimmickTriggerType.OnTurnStart);

                if (remainShotCount <= 0)
                {
                    UpdateRemainShotText();
                    GameFail();
                    HideAimObjects();
                    return;
                }

                timerStart = true;
                timerCount = 0;
                launchIndex = 0;

                BuildCurrentTurnBallPlan();

                firstBallLandedThisTurn = false;
                nextLaunchPos = currentLaunchPos;

                remainShotCount--;
                UpdateRemainShotText();

                if (remainShotCount < 0)
                {
                    remainShotCount = 0;
                    UpdateRemainShotText();
                }

                HideAimObjects();
            }
            else
            {
                HideAimObjects();
            }

            firstPos = Vector3.zero;
            secondPos = Vector3.zero;
        }
    }

    void FixedUpdateGame()
    {
        if (isBuffSelecting) return;
        if (!timerStart) return;

        if (++timerCount >= 3)
        {
            timerCount = 0;

            if (launchIndex < currentTurnLaunchCount)
            {
                Ball ball = BallGroup.GetChild(launchIndex).GetComponent<Ball>();
                if (ball != null)
                {
                    ApplyBallTypeToBall(ball, currentTurnBallTypes[launchIndex]);
                    ball.transform.position = currentLaunchPos;
                    ball.SetVisible(true);
                    ball.Launch(gap);
                }

                launchIndex++;
            }

            if (launchIndex >= currentTurnLaunchCount)
            {
                timerStart = false;
                launchIndex = 0;
            }
        }
    }
}