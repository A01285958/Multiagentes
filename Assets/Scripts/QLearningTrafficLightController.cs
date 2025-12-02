using System.Collections.Generic;
using UnityEngine;

public struct StateKey
{
    public int n;
    public int s;
    public int e;
    public int w;
    public int phase;

    public StateKey(int n, int s, int e, int w, int phase)
    {
        this.n = n;
        this.s = s;
        this.e = e;
        this.w = w;
        this.phase = phase;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is StateKey)) return false;
        StateKey other = (StateKey)obj;
        return n == other.n && s == other.s && e == other.e && w == other.w && phase == other.phase;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + n;
            hash = hash * 31 + s;
            hash = hash * 31 + e;
            hash = hash * 31 + w;
            hash = hash * 31 + phase;
            return hash;
        }
    }
}

public class QLearningTrafficLightController : MonoBehaviour
{
    [Header("Parámetros de aprendizaje")]
    public float alpha = 0.1f;
    public float gamma = 0.9f;
    public float epsilon = 0.1f;

    [Header("Tiempo de decisión")]
    public float decisionInterval = 1f;
    private float decisionTimer = 0f;

    [Header("Límites de colas")]
    public int queueCap = 4;
    public BoxCollider intersectionArea;

    [Header("Semáforos visuales")]
    public TrafficLightVisual northLight;
    public TrafficLightVisual southLight;
    public TrafficLightVisual eastLight;
    public TrafficLightVisual westLight;

    private int currentPhase = 0;   // 0 N, 1 S, 2 E, 3 W
    private int phaseTimer = 0;     // pasos que lleva en la fase actual

    private Dictionary<StateKey, float[]> Q = new Dictionary<StateKey, float[]>();
    public enum LightColor {Red, Yellow, Green}

    public static QLearningTrafficLightController Instance {get; private set;}

    [Header("Referencias para medir colas")]
    public Transform northStop;   // stop line de norte
    public Transform southStop;   // stop line de sur
    public Transform eastStop;    // stop line de este
    public Transform westStop;    // stop line de oeste

    public float maxQueueDistance = 20f;   // hasta dónde hacia atrás mides la fila
    public float laneHalfWidth = 2f;      // ancho aprox del carril

    bool IsInQueueRegion(char approach, CarAgent car)
{
    Vector3 pos = car.transform.position;

    switch (approach)
    {
        // Coches que vienen de arriba (N→S): dirección NorthToSouth
        case 'N':
        {
            if (car.direction != CarAgent.MoveDirection.NorthToSouth) return false;

            float zStop = northStop.position.z;
            float xStop = northStop.position.x;

            // Están formados "antes" del alto: z mayor que la línea y no demasiado lejos
            float dz = pos.z - zStop;   // hacia atrás del alto
            if (dz < 0 || dz > maxQueueDistance) return false;

            // Que no estén demasiado desviados del carril
            if (Mathf.Abs(pos.x - xStop) > laneHalfWidth) return false;

            return true;
        }

        // Coches que vienen de abajo (S→N)
        case 'S':
        {
            if (car.direction != CarAgent.MoveDirection.SouthToNorth) return false;

            float zStop = southStop.position.z;
            float xStop = southStop.position.x;

            float dz = zStop - pos.z;   // hacia atrás del alto
            if (dz < 0 || dz > maxQueueDistance) return false;
            if (Mathf.Abs(pos.x - xStop) > laneHalfWidth) return false;

            return true;
        }

        // Coches que vienen de la derecha (E→W)
        case 'E':
        {
            if (car.direction != CarAgent.MoveDirection.EastToWest) return false;

            float xStop = eastStop.position.x;
            float zStop = eastStop.position.z;

            float dx = pos.x - xStop;   // hacia atrás del alto
            if (dx < 0 || dx > maxQueueDistance) return false;
            if (Mathf.Abs(pos.z - zStop) > laneHalfWidth) return false;

            return true;
        }

        // Coches que vienen de la izquierda (W→E)
        case 'W':
        {
            if (car.direction != CarAgent.MoveDirection.WestToEast) return false;

            float xStop = westStop.position.x;
            float zStop = westStop.position.z;

            float dx = westStop.position.x - pos.x;   // hacia atrás del alto
            if (dx < 0 || dx > maxQueueDistance) return false;
            if (Mathf.Abs(pos.z - zStop) > laneHalfWidth) return false;

            return true;
        }
    }

    return false;
}

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        currentPhase = 0;
        phaseTimer = 0;
        decisionTimer = 0f;
        ApplyLights();
    }

    void Update()
    {
        decisionTimer += Time.deltaTime;
        if (decisionTimer >= decisionInterval)
        {
            decisionTimer = 0f;
            StepLearning();
        }
    }

    void StepLearning()
    {
        StateKey state = GetState();
        int action = ChooseAction(state);           // 0 mantener, 1 siguiente fase
        ApplyAction(action);
        float reward = GetReward();
        StateKey newState = GetState();
        UpdateQ(state, action, reward, newState);
        ApplyLights();
    }

    StateKey GetState()
    {
        int n = GetQueueLengthForApproach('N');
        int s = GetQueueLengthForApproach('S');
        int e = GetQueueLengthForApproach('E');
        int w = GetQueueLengthForApproach('W');

        int n_d = Mathf.Min(n, queueCap);
        int s_d = Mathf.Min(s, queueCap);
        int e_d = Mathf.Min(e, queueCap);
        int w_d = Mathf.Min(w, queueCap);

        return new StateKey(n_d, s_d, e_d, w_d, currentPhase);
    }

    int ChooseAction(StateKey state)
    {
        if (Random.value < epsilon)
            return Random.Range(0, 2);   // 0 o 1

        if (!Q.ContainsKey(state))
            return Random.Range(0, 2);

        float[] qValues = Q[state];
        return (qValues[0] >= qValues[1]) ? 0 : 1;
    }

    float GetReward()
    {
        int n = GetQueueLengthForApproach('N');
        int s = GetQueueLengthForApproach('S');
        int e = GetQueueLengthForApproach('E');
        int w = GetQueueLengthForApproach('W');

        // penaliza más una cola muy larga en un solo lado
        return -(n * n + s * s + e * e + w * w);
    }

    void UpdateQ(StateKey state, int action, float reward, StateKey newState)
    {
        if (!Q.ContainsKey(state))
            Q[state] = new float[2];

        if (!Q.ContainsKey(newState))
            Q[newState] = new float[2];

        float qOld = Q[state][action];
        float qMaxNext = Mathf.Max(Q[newState][0], Q[newState][1]);

        Q[state][action] = qOld + alpha * (reward + gamma * qMaxNext - qOld);
    }

    void ApplyAction(int action)
    {
        if (action == 1)
        {
            currentPhase = (currentPhase + 1) % 4;   // 0→1→2→3→0
            phaseTimer = 0;
        }
        else
        {
            phaseTimer += 1;
        }
    }

    void ApplyLights()
    {
        Debug.Log($"Fase actual: {currentPhase} (N={currentPhase==0} S={currentPhase==1} E={currentPhase==2} W={currentPhase==3})");

        if (northLight != null) northLight.SetGreen(currentPhase == 0);
        if (southLight != null) southLight.SetGreen(currentPhase == 1);
        if (eastLight  != null) eastLight.SetGreen(currentPhase == 2);
        if (westLight  != null) westLight.SetGreen(currentPhase == 3);
    }
 
    int GetQueueLengthForApproach(char approach)
    {
        int count = 0;
        CarAgent[] cars = Object.FindObjectsByType<CarAgent>(FindObjectsSortMode.None);

        foreach (var car in cars)
        {
            // Solo me interesan coches parados
            if (!car.IsStopped) continue;

            // Y que estén en la región de fila de ese lado
            if (!IsInQueueRegion(approach, car)) continue;

            count++;
        }

        return count;
    }

    public LightColor GetLightForDirection(CarAgent.MoveDirection dir)
{
    // usamos tu currentPhase: 0 N, 1 S, 2 E, 3 W
    switch (dir)
    {
        case CarAgent.MoveDirection.NorthToSouth:
            return (currentPhase == 0) ? LightColor.Green : LightColor.Red;

        case CarAgent.MoveDirection.SouthToNorth:
            return (currentPhase == 1) ? LightColor.Green : LightColor.Red;

        case CarAgent.MoveDirection.WestToEast:
            return (currentPhase == 3) ? LightColor.Green : LightColor.Red;

        case CarAgent.MoveDirection.EastToWest:
            return (currentPhase == 2) ? LightColor.Green : LightColor.Red;
    }
    return LightColor.Red;
}

}