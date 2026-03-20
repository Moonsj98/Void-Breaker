using System.Collections;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public enum BallType
    {
        Normal,
        Penetrate,
        Explosive
    }

    public Rigidbody2D RB;
    public SpriteRenderer spriteRenderer;
    public bool isMoving;
    public BallType ballType = BallType.Normal;

    [Header("Anti-Stuck")]
    public float stuckCheckInterval = 0.12f;
    public float stuckMoveThreshold = 0.35f;
    public int stuckCollisionThreshold = 4;
    public float escapeAngleY = 0.35f;
    public float escapeForce = 1700f;

    GameManager GM;

    Vector3 lastCheckPos;
    float lastCheckTime;
    int rapidCollisionCount;
    float lastCollisionTime;

    public void Setup(GameManager gameManager)
    {
        GM = gameManager;
        isMoving = false;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (RB != null)
        {
            RB.linearVelocity = Vector2.zero;
            RB.angularVelocity = 0f;
        }

        lastCheckPos = transform.position;
        lastCheckTime = Time.time;
        rapidCollisionCount = 0;
        lastCollisionTime = -999f;
    }

    void Start()
    {
        if (GM == null)
            GM = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        lastCheckPos = transform.position;
        lastCheckTime = Time.time;
    }

    void FixedUpdate()
    {
        if (!isMoving || RB == null)
            return;

        if (Time.time - lastCheckTime >= stuckCheckInterval)
        {
            float movedDist = Vector2.Distance(transform.position, lastCheckPos);

            if (movedDist < stuckMoveThreshold && rapidCollisionCount >= stuckCollisionThreshold)
            {
                ApplyEscapeCorrection();
                rapidCollisionCount = 0;
            }
            else
            {
                if (movedDist >= stuckMoveThreshold)
                    rapidCollisionCount = 0;
            }

            lastCheckPos = transform.position;
            lastCheckTime = Time.time;
        }
    }

    public void SetBallType(BallType type, Sprite sprite)
    {
        ballType = type;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && sprite != null)
            spriteRenderer.sprite = sprite;
    }

    public void SetVisible(bool visible)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;
    }

    public void Launch(Vector3 dir)
    {
        if (GM == null) return;

        GM.shotTrigger = true;
        isMoving = true;

        if (RB == null)
        {
            Debug.LogError("Ball.cs의 RB가 연결되지 않았습니다.");
            return;
        }

        SetVisible(true);

        rapidCollisionCount = 0;
        lastCollisionTime = -999f;
        lastCheckPos = transform.position;
        lastCheckTime = Time.time;

        RB.linearVelocity = Vector2.zero;
        RB.angularVelocity = 0f;
        RB.AddForce(dir.normalized * 7000f, ForceMode2D.Force);
    }

    public void StopBall()
    {
        isMoving = false;
        rapidCollisionCount = 0;

        if (RB != null)
            RB.linearVelocity = Vector2.zero;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        StartCoroutine(OnCollisionEnter2D_BALL(col));
    }

    IEnumerator OnCollisionEnter2D_BALL(Collision2D col)
    {
        GameObject hitObj = col.gameObject;

        Physics2D.IgnoreLayerCollision(2, 2);

        CountRapidCollision();

        if (hitObj.CompareTag("Ground"))
        {
            RB.linearVelocity = Vector2.zero;

            Vector3 landedPos = new Vector3(col.contacts[0].point.x, GM.groundY, 0f);
            transform.position = landedPos;

            GM.SetNextLaunchPos(landedPos);

            while (true)
            {
                yield return null;
                transform.position = Vector3.MoveTowards(transform.position, GM.nextLaunchPos, 4f);

                if (transform.position == GM.nextLaunchPos)
                {
                    isMoving = false;
                    yield break;
                }
            }
        }

        Block block = hitObj.GetComponent<Block>();
        if (block != null)
        {
            GM.ProcessBallHit(this, block);
        }
    }

    void CountRapidCollision()
    {
        if (Time.time - lastCollisionTime <= 0.08f)
            rapidCollisionCount++;
        else
            rapidCollisionCount = 1;

        lastCollisionTime = Time.time;
    }

    void ApplyEscapeCorrection()
    {
        if (RB == null)
            return;

        Vector2 curVel = RB.linearVelocity;

        if (curVel.sqrMagnitude <= 0.001f)
            return;

        Vector2 dir = curVel.normalized;

        float ySign;
        if (Mathf.Abs(dir.y) > 0.05f)
            ySign = Mathf.Sign(dir.y);
        else
            ySign = (transform.position.y >= 0f) ? 1f : -1f;

        Vector2 escapeDir = new Vector2(dir.x, dir.y + (escapeAngleY * ySign)).normalized;
        RB.AddForce(escapeDir * escapeForce, ForceMode2D.Impulse);
    }
}