using System.Collections;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public Rigidbody2D RB;
    public bool isMoving;

    [Header("Anti Infinite Bounce")]
    public float minBounceY = 0.2f;   // 너무 수평으로 가는 걸 막기 위한 최소 Y 성분
    public float minSpeed = 0.1f;

    GameManager GM;

    public void Setup(GameManager gameManager)
    {
        GM = gameManager;
    }

    void Start()
    {
        if (GM == null)
            GM = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
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

        RB.linearVelocity = Vector2.zero;
        RB.angularVelocity = 0f;
        RB.AddForce(dir.normalized * 7000f, ForceMode2D.Force);
    }

    public void StopBall()
    {
        isMoving = false;

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

        // Ground 처리 전용
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

        // ===== 무한 튕김 방지용 최소 수정 로직 =====
        if (RB != null)
        {
            Vector2 dir = RB.linearVelocity.normalized;

            if (dir.magnitude > 0f && Mathf.Abs(dir.y) < 0.15f)
            {
                float speed = RB.linearVelocity.magnitude;
                if (speed < minSpeed) speed = 7000f * Time.fixedDeltaTime;

                float forcedY;

                // 현재 y 성분이 조금이라도 있으면 그 방향 유지
                if (dir.y > 0.001f)
                {
                    forcedY = minBounceY;
                }
                else if (dir.y < -0.001f)
                {
                    forcedY = -minBounceY;
                }
                else
                {
                    // 완전히 수평에 가까우면 충돌 위치 기준으로 위/아래 판단
                    // 공이 블록 중심보다 위에 있으면 위로, 아래에 있으면 아래로
                    forcedY = (transform.position.y >= hitObj.transform.position.y) ? minBounceY : -minBounceY;
                }

                Vector2 fixedDir = new Vector2(dir.x, forcedY).normalized;
                RB.linearVelocity = fixedDir * speed;
            }
        }
        // ===== 여기까지 최소 수정 =====

        Block block = hitObj.GetComponent<Block>();
        if (block != null)
        {
            block.OnHit(GM.attackPower);
        }
    }
}