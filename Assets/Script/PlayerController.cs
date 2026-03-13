using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    float speed = 1.0f; // 플레이어의 이동 속도
    public int lifeNumber = 3;
    public Rigidbody playerRigidbody;
    public MeshRenderer mesh;
    public Material mat;

    void Start()
    {
        mesh = GetComponent<MeshRenderer>();
        mat = mesh.material;
        playerRigidbody = GetComponent<Rigidbody>(); // Get the Rigidbody component attached to the player GameObject
    }

    void Update()
    {

    }

    void FixedUpdate()
    {
        //수평 축과 수직 축의 입력값을 감지하여 벡터라는 변수에 저장
        Vector3 vec = new Vector3(Input.GetAxisRaw("Horizontal") * 10.0f, 0, Input.GetAxisRaw("Vertical") * 10.0f);
        playerRigidbody.linearVelocity = vec;
    }

    public void Die()
    {
        // 자신의 플레이어 오브젝트를 비활성화
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == ("Bullet"))
        {
            mat.color = new Color(1, 0, 0);
        }
    }
    public IEnumerator DamageFlash()
    {
        Color originalColor = mat.color;

        mat.color = Color.red;

        yield return new WaitForSeconds(1f);

        mat.color = originalColor;
    }
}
