using UnityEngine;

public class Bullet : MonoBehaviour
{
    //탄알 이동 속도
    public float speed = 8.0f;
    Rigidbody bulletRigidbody;

    void Start()
    {
        //게임 오브젝트에서 Rigidbody 컴포넌트를 찾아 bulletRigidboy에 할당
        bulletRigidbody = GetComponent<Rigidbody>();

        // 탄알 리지드바디의 속도 = 앞쪽 방향(Z축) * 이동 속도(speed)
        //transform.forward는 게임 오브젝트의 앞쪽 방향(Z축)을 나타내는 벡터 변수입니다.
        // 자신의 오브젝트에 대한 Transform 컴포넌트에 접근할 때는, GetComponent<>를 사용하지 않고, transform.forward로 바로 접근할 수 있습니다.
        bulletRigidbody.linearVelocity = transform.forward * speed;
    }

    void Update()
    {
    }
    // OnCollisionEnter: 오브젝트가 충돌하는 순간 호출되는 메서드
    // OnCollisionStay: 오브젝트가 충돌한 상태로 계속 유지되는 동안 매 프레임마다 호출되는 메서드
    // OnCollisionExit: 오브젝트가 충돌에서 벗어나는 순간 호출되는 메서드
    // Collider의 IsTrigger 옵션을 체크한 경우, 충돌이 발생해도 OnCollisionEnter, OnCollisionStay, OnCollisionExit 메서드가 호출되지 않습니다.
    // 대신, OnTriggerEnter, OnTriggerStay, OnTriggerExit 메서드가 호출됩니다.
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == ("Player"))
        {
            // PlayerController 스크립트가 붙어있는 게임 오브젝트를 찾아 player 변수에 할당
            PlayerController player = other.GetComponent<PlayerController>();
            // PlayerController의 player 컴포넌트를 가져왔다면, 플레이어를 죽이는 로직
            if(player != null)
            {
                Destroy(gameObject);
                player.lifeNumber -= 1;
                player.StartCoroutine(player.DamageFlash());
                if (player.lifeNumber == 0 )
                {
                    player.Die();
                }
            }
        }
        if(other.gameObject.name == ("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
