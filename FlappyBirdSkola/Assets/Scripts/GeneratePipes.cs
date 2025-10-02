using UnityEngine;
using UnityEngine.Serialization;

public class GeneratePipes : MonoBehaviour
{
    public GameObject pipe;
    public GameObject scoreAdder;
    public GameObject pipeMover; //moves pipes to the left
    public GameObject cloud;
    public GameObject building;
    
    [FormerlySerializedAs("pipeNumber")] public int pipeMaxNumber;
    private int pipeTotalNumber;
    
    public float timerInterval = 10f;
    public float minHeight = 1.7f;
    public float maxHeight = 7f;
    
    private float timer = 0f;
    
    private float cloudTimer = 0f;
    public float cloudInterval = 1.5f;
    public float cloudMaxSize = 1.2f;
    public float cloudMinSize = 0.8f;
    public float cloudMaxHeight = 4.2f;
    public float cloudMinHeight = 3.8f;
    
    private float buildingTimer = 0f;
    public float buildingInterval = 1.5f;
    public int buildingMaxSize = 6;
    public int buildingMinSize = 2;
    
    public float distance;

    private void Update()
    {
        timer += Time.deltaTime;
        cloudTimer += Time.deltaTime;
        buildingTimer += Time.deltaTime;
        if (timer >= timerInterval)
        {
            timer -= timerInterval;
            CreatePipe();
        }

        if (cloudTimer >= cloudInterval)
        {
            cloudTimer -= cloudInterval;
            CreateCloud();
        }

        if (buildingTimer >= buildingInterval)
        {
            buildingTimer -= buildingInterval;
            CreateBuilding();
        }
    }
    void Start()
    {
        /*
        CreatePipe();
    
        if (pipeMover != null && scoreAdder != null && pipe != null){
            for (int i = 0; i < 1; i++)
            {
                CreatePipe();
                distance += 8;
            }
            pipeMover.GetComponent<MovePipes>().canMove = true;
        }*/
        CreatePipe();
        CreateCloud();
        pipeMover.GetComponent<MovePipes>().canMove = true;
    }

    public void CreatePipe()
    {
        if (pipeMover != null && scoreAdder != null && pipe != null)
        {
            if (pipeMaxNumber > pipeTotalNumber || pipeMaxNumber == -1)
            {
                float pipeHeight = Random.Range(minHeight, maxHeight);
                Vector2 pipeUpPosition = new Vector2(transform.position.x, -pipeHeight);
                Instantiate(pipe, pipeUpPosition, new Quaternion(), pipeMover.transform);
                pipeHeight = maxHeight - pipeHeight + 2;
                Vector2 pipeDownPosition = new Vector2(transform.position.x, pipeHeight);
                Instantiate(pipe, pipeDownPosition, new Quaternion(0, 0, 180, 0), pipeMover.transform);
                Vector2 scoreAdderPosition = new Vector2(transform.position.x, 0);
                Instantiate(scoreAdder, scoreAdderPosition, new Quaternion(), pipeMover.transform);
            }
        }
    }

    public void CreateCloud()
    {
        float cloudHeight = Random.Range(cloudMinHeight, cloudMaxHeight);
        Vector2 cloudPosition = new Vector2(transform.position.x, cloudHeight);
        GameObject createdCloud = Instantiate(cloud, cloudPosition, new Quaternion(), pipeMover.transform);
        float cloudScale = Random.Range(cloudMinSize, cloudMaxSize);
        createdCloud.transform.localScale = new Vector3(cloudScale, cloudScale, 0);
    }

    public void CreateBuilding()
    {
        int scale = Random.Range(buildingMinSize, buildingMaxSize);
        float height = 0.5f * (scale - 2) - 4f;
        Vector2 position = new Vector2(transform.position.x, height);
        GameObject createdBuilding = Instantiate(building, position, new Quaternion(), pipeMover.transform);
        createdBuilding.transform.localScale = new Vector3(3, scale, 0);
    }
}
