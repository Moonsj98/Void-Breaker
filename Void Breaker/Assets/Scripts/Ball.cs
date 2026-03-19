using System.Collections;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public Rigidbody2D RB;
    public bool isMoving;

    [Header("Anti-Stuck")]
    public float stuckCheckInterval = 0.12f;     // 이동량 체크 간격
    public float stuckMoveThreshold = 0.35f;     // 이 값보다 덜 움직이면 갇힘 후보
    public int stuckCollisionThreshold = 4;      // 짧은 시간 내 충돌 횟수 기준
    public float escapeAngleY = 0.35f;           // 탈출 시 추가할 Y 성분
    public float escapeForce = 1700f;            // 탈출용 보정 힘

    GameManager GM;

    Vector3 lastCheckPos;
    float lastCheckTime;
    int rapidCollisionCount;
    float lastCollisionTime;

    public void Setup(GameManager gameManager)
    {
        GM = gameManager;
        isMoving = false;

        lastCheckPos = transform.position;
        lastCheckTime = Time.time;
        rapidCollisionCount = 0;
        lastCollisionTime = -999f;
    }

    void Start()
    {
        if (GM == null)
            GM = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();

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

            // 최근 구간 동안 거의 움직이지 않았고, 짧은 시간 내 충돌이 많이 발생했다면 갇힘으로 판단
            if (movedDist < stuckMoveThreshold && rapidCollisionCount >= stuckCollisionThreshold)
            {
                ApplyEscapeCorrection();
                rapidCollisionCount = 0;
            }
            else
            {
                // 충분히 이동했다면 충돌 누적 초기화
                if (movedDist >= stuckMoveThreshold)
                    rapidCollisionCount = 0;
            }

            lastCheckPos = transform.position;
            lastCheckTime = Time.time;
        }
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

        // 기존 얕은 각도 보정은 제거
        // 이 부분이 공을 항상 아래쪽으로 다시 밀어 무한 튕김을 악화시킬 수 있었음

        if (hitObj.CompareTag("Ground"))
        {
            RB.linearVelocity = Vector2.zero;

            Vector3 landedPos = new Vector3(col.contacts[0].point.x, GM.groundY, 0f);
            transform.position = landedPos;

            // 이번 턴에 가장 먼저 도착한 공만 다음 발사 위치를 결정
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
            block.OnHit(GM.attackPower);
        }
    }

    void CountRapidCollision()
    {
        // 매우 짧은 간격으로 연속 충돌하면 누적
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

        // 현재 진행 방향을 최대한 유지하면서 Y를 살짝 추가
        Vector2 dir = curVel.normalized;

        float ySign;
        if (Mathf.Abs(dir.y) > 0.05f)
            ySign = Mathf.Sign(dir.y);
        else
            ySign = (transform.position.y >= 0f) ? 1f : -1f;

        Vector2 escapeDir = new Vector2(dir.x, dir.y + (escapeAngleY * ySign)).normalized;

        // 기존 속도를 완전히 바꾸지 않고 아주 살짝만 탈출 보정
        RB.AddForce(escapeDir * escapeForce, ForceMode2D.Impulse);
    }
}