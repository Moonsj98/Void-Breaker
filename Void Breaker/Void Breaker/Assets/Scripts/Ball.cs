using System.Collections;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public Rigidbody2D RB;
    public bool isMoving;

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

        Vector2 dir = RB.linearVelocity.normalized;
        if (dir.magnitude != 0 && dir.y < 0.15f && dir.y > -0.15f)
        {
            RB.linearVelocity = Vector2.zero;
            RB.AddForce(new Vector2(dir.x > 0 ? 1 : -1, -0.2f).normalized * 7000f);
        }

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
}