// BulletSpawner는 주기적으로 Bullet을 생성한다.
using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    // 생성할 Bullet의 원본 프리팹
    public GameObject bulletPrefab;

    // Bullet의 최소 생성 주기
    public float spawnRateMin = 0.1f;

    // Bullet의 최대 생성 주기
    public float spawnRateMax = 0.5f;

    // Bullet의 생성 주기
    private float spawnRate;

    // Bullet을 발사할 대상(target)
    private Transform target;

    // 최근 생성 시점에서 지난 시간 (딜레이)
    // 마지막 탄알 생성 시점부터 흐른 시간을 표시하는 타이머
    private float timeAfterSpawn;

    void Start()
    {
        //최근 생성 이후의 누적 시간을 0으로 초기화
        timeAfterSpawn = 0.0f;

        //탄알 생성 간격을 spawnRateMin과 spawnRateMax 사이의 랜덤한 값으로 지정
        spawnRate = Random.Range(spawnRateMin, spawnRateMax);

        // PlayerController 스크립트가 붙어있는 게임 오브젝트를 찾아 target 변수에 할당
        // FindFirstObjectByType<T>() 메서드는 씬에 존재하는 모든 오브젝트를 검색하여 원하는 타입의 오브젝트를 찾아낸다.
        target = FindFirstObjectByType<PlayerController>().transform;


    }

    void Update()
    {
        //timeAfterSpawn 갱신
        // Time.deltaTime는 마지막 프레임이 끝나고 현재 프레임이 시작될 때까지의 시간 간격을 나타내는 값입니다.
        // 이 값을 timeAfterSpawn에 더해주면, timeAfterSpawn은 최근 생성 이후로 흐른 누적 시간을 나타내게 됩니다.
        timeAfterSpawn += Time.deltaTime; 

        // 최근 생성 시점에서부터 누적된 시간이 생성 주기보다 크거나 같다면
        if(timeAfterSpawn >= spawnRate)
        {
            timeAfterSpawn = 0.0f; // 탄알 생성 이후, 누적된 시간을 초기화

            //bulletPrefab의 복제본을 transform.position 위치에 transform.rotation 회전으로 생성
            GameObject bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
            bullet.transform.LookAt(target); // bullet이 target을 향하도록 회전

            spawnRate = Random.Range(spawnRateMin, spawnRateMax); // 다음 탄알 생성 주기를 랜덤값으로 초기화
        }
    }
}
