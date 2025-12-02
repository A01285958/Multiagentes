using System.Collections.Generic;
using System.IO;             
using UnityEngine;

[System.Serializable]
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

// Clases auxiliares para serializar la Q-table
[System.Serializable]
public class QEntry
{
    public int n;
    public int s;
    public int e;
    public int w;
    public int phase;
    public float q0;
    public float q1;
}

[System.Serializable]
public class QTableData
{
    public List<QEntry> entries = new List<QEntry>();
}

public class QLearningTrafficLightController : MonoBehaviour
{
    [Header("Parámetros de aprendizaje")]
    public float alpha = 0.1f;
    public float gamma = 0.9f;
    public float epsilon = 0.1f;

    [Header("Persistencia Q-Learning")]
    public bool loadFromFile = true;
    public bool saveToFile = true;
    public string qTableFileName = "traffic_qtable.json";

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
    private int phaseTimer = 0;

    // Diccionario Q(s,a)
    private Dictionary<StateKey, float[]> Q = new Dictionary<StateKey, float[]>();

    public enum LightColor { Red, Yellow, Green }

    public static QLearningTrafficLightController Instance { get; private set; }

    [Header("Referencias para medir colas")]
    public Transform northStop;
    public Transform southStop;
    public Transform eastStop;
    public Transform westStop;

    public float maxQueueDistance = 20f;
    public float laneHalfWidth = 2f;

    string QTablePath
    {
        get { return Path.Combine(Application.persistentDataPath, qTableFileName); }
    }

    bool IsInQueueRegion(char approach, CarAgent car)
    {
        Vector3 pos = car.transform.position;

        switch (approach)
        {
            case 'N':
            {
                if (car.direction != CarAgent.MoveDirection.NorthToSouth) return false;

                float zStop = northStop.position.z;
                float xStop = northStop.position.x;

                float dz = pos.z - zStop;   // hacia atrás del alto
                if (dz < 0 || dz > maxQueueDistance) return false;
                if (Mathf.Abs(pos.x - xStop) > laneHalfWidth) return false;

                return true;
            }

            case 'S':
            {
                if (car.direction != CarAgent.MoveDirection.SouthToNorth) return false;

                float zStop = southStop.position.z;
                float xStop = southStop.position.x;

                float dz = zStop - pos.z;
                if (dz < 0 || dz > maxQueueDistance) return false;
                if (Mathf.Abs(pos.x - xStop) > laneHalfWidth) return false;

                return true;
            }

            case 'E':
            {
                if (car.direction != CarAgent.MoveDirection.EastToWest) return false;

                float xStop = eastStop.position.x;
                float zStop = eastStop.position.z;

                float dx = pos.x - xStop;
                if (dx < 0 || dx > maxQueueDistance) return false;
                if (Mathf.Abs(pos.z - zStop) > laneHalfWidth) return false;

                return true;
            }

            case 'W':
            {
                if (car.direction != CarAgent.MoveDirection.WestToEast) return false;

                float xStop = westStop.position.x;
                float zStop = westStop.position.z;

                float dx = westStop.position.x - pos.x;
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
        if (loadFromFile)
        {
            LoadQTable();
        }

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

    void OnApplicationQuit()
    {
        if (saveToFile)
        {
            SaveQTable();
        }
    }

    void OnDestroy()
    {
        if (saveToFile)
        {
            SaveQTable();
        }
    }

    void StepLearning()
    {
        StateKey state = GetState();
        int action = ChooseAction(state);
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
            return Random.Range(0, 2);

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
            currentPhase = (currentPhase + 1) % 4;
            phaseTimer = 0;
        }
        else
        {
            phaseTimer += 1;
        }
    }

    void ApplyLights()
    {
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
            if (!car.IsStopped) continue;
            if (!IsInQueueRegion(approach, car)) continue;
            count++;
        }

        return count;
    }

    public LightColor GetLightForDirection(CarAgent.MoveDirection dir)
    {
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

    void SaveQTable()
    {
        QTableData data = new QTableData();
        foreach (var kvp in Q)
        {
            StateKey k = kvp.Key;
            float[] q = kvp.Value ?? new float[2];

            QEntry e = new QEntry
            {
                n = k.n,
                s = k.s,
                e = k.e,
                w = k.w,
                phase = k.phase,
                q0 = q.Length > 0 ? q[0] : 0f,
                q1 = q.Length > 1 ? q[1] : 0f
            };
            data.entries.Add(e);
        }

        string json = JsonUtility.ToJson(data, true);
        try
        {
            File.WriteAllText(QTablePath, json);
            Debug.Log("Q-table guardada en: " + QTablePath);
        }
        catch (IOException ex)
        {
            Debug.LogError("Error guardando Q-table: " + ex.Message);
        }
    }

    void LoadQTable()
    {
        if (!File.Exists(QTablePath))
        {
            Debug.Log("No se encontró Q-table previa, se inicia desde cero");
            return;
        }

        try
        {
            string json = File.ReadAllText(QTablePath);
            QTableData data = JsonUtility.FromJson<QTableData>(json);
            Q.Clear();

            if (data != null && data.entries != null)
            {
                foreach (var e in data.entries)
                {
                    StateKey k = new StateKey(e.n, e.s, e.e, e.w, e.phase);
                    float[] q = new float[2] { e.q0, e.q1 };
                    Q[k] = q;
                }
            }

            Debug.Log("Q-table cargada desde: " + QTablePath);
        }
        catch (IOException ex)
        {
            Debug.LogError("Error cargando Q-table: " + ex.Message);
        }
    }
}