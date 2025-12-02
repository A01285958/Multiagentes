using UnityEngine;

public class TrafficSimulator : MonoBehaviour
{
    [Header("Prefab de carros")]
    public GameObject[] carPrefabs; //bus, ambulancia, coche, etc.

    [Header("Puntos de aparición")]
    public Transform northSpawn; //Arriba (Norte -> Sur)
    public Transform southSpawn; //Abajo (Sur -> Norte)
    public Transform eastSpawn; //Derecha (Este -> Oeste)
    public Transform westSpawn; //Izquierda (Oeste -> Este)

    [Header("Parametros de spawn")]
    public float spawnRate = 0.4f; //Coches por segund aprox.
    public LayerMask carLayer; //Capa carro para que no aparezca encima de otro
    public float spawnCheckRadius = 2f;

    void Update()
    {
        // proceso tipo Poisson: probabilidad spawnRate * dt cada frame
        if (Random.value < spawnRate * Time.deltaTime)
        {
            SpawnRandomCar();
        }
    }

    void SpawnRandomCar()
    {
        if (carPrefabs == null || carPrefabs.Length == 0) return;

        //0 -N, 1 - S, 2 - E, 3 - W
        int dirIndex = Random.Range(0, 4);

        Transform spawnPoint = null;
        CarAgent.MoveDirection moveDir = CarAgent.MoveDirection.NorthToSouth;

        switch (dirIndex)
        {
            case 0:
                spawnPoint = northSpawn;
                moveDir = CarAgent.MoveDirection.NorthToSouth;
                break;
            case 1:
                spawnPoint = southSpawn;
                moveDir = CarAgent.MoveDirection.SouthToNorth;
                break;
            case 2:
                spawnPoint = eastSpawn;
                moveDir = CarAgent.MoveDirection.EastToWest;
                break;
            case 3:
                spawnPoint = westSpawn;
                moveDir = CarAgent.MoveDirection.WestToEast;
                break;
        }
        if (spawnPoint == null) return;

        //No nuevo coche si ya hay un coche muy cerca del spawn
        if(Physics.CheckSphere(spawnPoint.position, spawnCheckRadius, carLayer))
            return;
        
        //Elegir prefab aleatorio
        GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];

        //Instanciar
        GameObject carObj = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        //Asegurar de que tenga CarAgent y configurar direccion
        CarAgent agent = carObj.GetComponent<CarAgent>() ?? carObj.GetComponentInChildren<CarAgent>();
        if (agent != null)
        {
            agent.direction = moveDir;
        }
    }

    void OnDrawGizmosSelected()
    {
         // para ver en la escena la zona donde se checa si ya hay carro
        Gizmos.color = Color.cyan;
        if (northSpawn != null) Gizmos.DrawWireSphere(northSpawn.position, spawnCheckRadius);
        if (southSpawn != null) Gizmos.DrawWireSphere(southSpawn.position, spawnCheckRadius);
        if (eastSpawn  != null) Gizmos.DrawWireSphere(eastSpawn.position,  spawnCheckRadius);
        if (westSpawn  != null) Gizmos.DrawWireSphere(westSpawn.position,  spawnCheckRadius);
    }
}
