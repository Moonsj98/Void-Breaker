using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Header("Stage Setting")]
    public int cost = 10;
    public int maxShotCount = 10;
    public int supportRowCount = 6;
    public int attackPower = 1;

    [Header("Mini Stage")]
    public int maxMiniStage = 3;
    int currentMiniStage = 1;

    [Header("Special Block Spawn")]
    public bool spawnBuffCard = true;
    public bool spawnInfiniteBlock = true;
    public int infiniteBlockCount = 1;

    [Header("Aim Preview")]
    public float aimRayDistance = 300f;
    // BallPreview가 블록과 딱 붙어 보이도록 조정하는 반지름
    public float aimPreviewRadius = 1.3f;
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
    float cachedBallRadius = -1f;

    bool timerStart;
    bool isDie;
    bool isStageClear;
    bool isBuffSelecting;
    bool firstBallLandedThisTurn;

    Quaternion QI = Quaternion.identity;

    [Header("Buff UI")]
    public BuffSelectUI buffSelectUI;

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
        nextLaunchPos = initialLaunchPos; ;

        InitializeBalls();

        UpdateBallCountText();
        UpdateStatInfoText();

        if (buffSelectUI != null)
            buffSelectUI.Hide();

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
            BallCountText.text = "x" + BallGroup.childCount;
    }

    void UpdateStatInfoText()
    {
        if (StatInfoText != null)
            StatInfoText.text = "코스트 : " + cost + " / 공격력 : " + attackPower;
    }

    void InitializeBalls()
    {
        Vector3 spawnPos = currentLaunchPos;

        for (int i = BallGroup.childCount - 1; i >= 0; i--)
            Destroy(BallGroup.GetChild(i).gameObject);

        for (int i = 0; i < cost; i++)
        {
            GameObject ballObj = Instantiate(P_Ball, spawnPos, QI, BallGroup);
            Ball ball = ballObj.GetComponent<Ball>();
            if (ball != null)
                ball.Setup(this);
        }
    }

    public void AddBalls(int count)
    {
        Vector3 spawnPos = currentLaunchPos;

        for (int i = 0; i < count; i++)
        {
            GameObject ballObj = Instantiate(P_Ball, spawnPos, QI, BallGroup);
            Ball ball = ballObj.GetComponent<Ball>();
            if (ball != null)
                ball.Setup(this);
        }

        UpdateBallCountText();
    }

    float GetActualBallRadius()
    {
        if (cachedBallRadius > 0f)
            return cachedBallRadius;

        if (P_Ball == null)
        {
            cachedBallRadius = 0.5f;
            return cachedBallRadius;
        }

        CircleCollider2D circle = P_Ball.GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            float scaleX = P_Ball.transform.localScale.x;
            float scaleY = P_Ball.transform.localScale.y;
            float maxScale = Mathf.Max(Mathf.Abs(scaleX), Mathf.Abs(scaleY));

            cachedBallRadius = circle.radius * maxScale;
            return cachedBallRadius;
        }

        SpriteRenderer sr = P_Ball.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            cachedBallRadius = sr.bounds.extents.x;
            return cachedBallRadius;
        }

        cachedBallRadius = 0.5f;
        return cachedBallRadius;
    }

    public void SetNextLaunchPos(Vector3 pos)
    {
        if (firstBallLandedThisTurn) return;

        firstBallLandedThisTurn = true;
        nextLaunchPos = new Vector3(pos.x, groundY, 0f);
    }

    public void StageGenerator()
    {
        for (int i = BlockGroup.childCount - 1; i >= 0; i--)
            Destroy(BlockGroup.GetChild(i).gameObject);

        int columnCount = 8;
        float leftX = -49.3f;
        float rightX = 49.3f;
        float topY = 51.2f;
        float rowGap = 12.8f;
        float xGap = (rightX - leftX) / (columnCount - 1);

        bool[,] occupied = new bool[supportRowCount + 1, columnCount];

        int coreIndex = Random.Range(1, columnCount - 1);
        Vector3 corePos = new Vector3(leftX + coreIndex * xGap, topY, 0f);
        CreateCoreBlock(corePos);

        for (int row = 1; row <= supportRowCount; row++)
        {
            float y = topY - (rowGap * row);

            HashSet<int> usedCols = new HashSet<int>();
            usedCols.Add(coreIndex);

            int randomExtraCount = Random.Range(3, 7);
            while (usedCols.Count < randomExtraCount + 1)
                usedCols.Add(Random.Range(0, columnCount));

            foreach (int col in usedCols)
            {
                float x = leftX + col * xGap;
                Vector3 spawnPos = new Vector3(x, y, 0f);

                int hp = Random.Range(cost - 3, cost + 4);
                if (hp < 1) hp = 1;

                CreateNormalBlock(spawnPos, hp);
                occupied[row, col] = true;
            }
        }

        List<Vector2Int> emptySlots = new List<Vector2Int>();
        for (int row = 1; row <= supportRowCount; row++)
        {
            for (int col = 0; col < columnCount; col++)
            {
                if (!occupied[row, col])
                    emptySlots.Add(new Vector2Int(row, col));
            }
        }

        if (spawnBuffCard && emptySlots.Count > 0)
        {
            int idx = Random.Range(0, emptySlots.Count);
            Vector2Int slot = emptySlots[idx];
            emptySlots.RemoveAt(idx);

            Vector3 pos = new Vector3(leftX + slot.y * xGap, topY - (rowGap * slot.x), 0f);
            CreateBuffCardBlock(pos);
        }

        if (spawnInfiniteBlock)
        {
            for (int i = 0; i < infiniteBlockCount; i++)
            {
                if (emptySlots.Count <= 0) break;

                int idx = Random.Range(0, emptySlots.Count);
                Vector2Int slot = emptySlots[idx];
                emptySlots.RemoveAt(idx);

                Vector3 pos = new Vector3(leftX + slot.y * xGap, topY - (rowGap * slot.x), 0f);
                CreateInfiniteBlock(pos);
            }
        }

        BlockColorRefresh();
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

        List<BuffOption> options = new List<BuffOption>();
        options.Add(new BuffOption(BuffType.ExtraShot, Random.Range(3, 8)));
        options.Add(new BuffOption(BuffType.ExtraCost, Random.Range(3, 8)));
        options.Add(new BuffOption(BuffType.ExtraAttackPower, Random.Range(1, 4)));

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
                remainShotCount += option.value;
                UpdateRemainShotText();
                break;

            case BuffType.ExtraCost:
                cost += option.value;
                AddBalls(option.value);
                break;

            case BuffType.ExtraAttackPower:
                attackPower += option.value;
                break;
        }

        UpdateStatInfoText();
        BlockColorRefresh();
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

        // 미니 스테이지 진행
        if (currentMiniStage < maxMiniStage)
        {
            currentMiniStage++;

            remainShotCount += 10;
            UpdateRemainShotText();

            for (int i = BlockGroup.childCount - 1; i >= 0; i--)
                Destroy(BlockGroup.GetChild(i).gameObject);

            ResetBallsToInitialPosition();
            StageGenerator();

            return;
        }

        // 마지막 미니 스테이지 클리어
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

        for (int i = 0; i < BallGroup.childCount; i++)
        {
            Transform ballTr = BallGroup.GetChild(i);
            Ball ball = ballTr.GetComponent<Ball>();

            if (ball != null)
            {
                ball.StopBall();
                ball.Setup(this);
            }

            ballTr.position = currentLaunchPos;
        }

        UpdateBallCountText();
        HideAimObjects();
    }

    Vector3 GetAimEndPoint(Vector3 origin, Vector3 dir)
    {
        float ballRadius = GetActualBallRadius();

        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, ballRadius, dir, aimRayDistance, aimHitMask);

        if (hits != null && hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i].collider;
                if (col == null) continue;

                if (BallPreview != null && col.gameObject == BallPreview)
                    continue;

                Block block = col.GetComponent<Block>();
                if (block != null && block.blockType == Block.BlockType.BuffCard)
                    continue;

                Vector2 endPoint = hits[i].point + hits[i].normal * ballRadius;
                return new Vector3(endPoint.x, endPoint.y, 0f);
            }
        }

        return origin + dir.normalized * aimRayDistance;
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
            currentLaunchPos = nextLaunchPos;

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
            firstPos = Camera.main.ScreenToWorldPoint(new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                10f
            ));
            firstPos.z = 0f;

            secondPos = firstPos;
        }

        if (Input.GetMouseButton(0))
        {
            secondPos = Camera.main.ScreenToWorldPoint(new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                10f
            ));
            secondPos.z = 0f;

            Vector3 drag = secondPos - firstPos;

            if (drag.magnitude >= 0.5f)
            {
                gap = drag.normalized;

                if (gap.y < 0.2f)
                    gap.y = 0.2f;

                gap = gap.normalized;

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
                    BallPreview.transform.position = aimEndPos;
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
                timerStart = true;
                timerCount = 0;
                launchIndex = 0;

                firstBallLandedThisTurn = false;
                nextLaunchPos = currentLaunchPos;

                remainShotCount--;
                UpdateRemainShotText();

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

            if (launchIndex < BallGroup.childCount)
            {
                Ball ball = BallGroup.GetChild(launchIndex).GetComponent<Ball>();
                if (ball != null)
                    ball.Launch(gap);

                launchIndex++;
            }

            if (launchIndex >= BallGroup.childCount)
            {
                timerStart = false;
                launchIndex = 0;
            }
        }
    }
}