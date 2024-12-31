using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public enum DungeonState { inactive, generating, cleanup, completed }

public class DungeonGenerator : MonoBehaviour
{
    [Header("Tile Prefabs")]
    [SerializeField] GameObject[] startRoomPrefabs;
    [SerializeField] GameObject[] roomPrefabs;
    [SerializeField] GameObject[] hallwayPrefabs;
    [SerializeField] GameObject[] doorPrefabs;
    [SerializeField] GameObject[] blockedPrefabs;

    [Header("Generation Settings")]
    [Range(10, 100)] public static int dungeonSize = 50;
    [Range(0.05f, 1f)] public static float constructionDelay = 0.05f; // Lowest value is 0.05 due to allowing physics to update in time.
    [Range(0, 1f)] public static float hallwayChance = 0.6f; // 60% Chance Default

    //public int DungeonSize { get => dungeonSize; set => dungeonSize = value; }
    //public float ConstructionDelay { get => constructionDelay; set => constructionDelay = value; }
    //public float HallwayChance { get => hallwayChance; set => hallwayChance = value; }

    [Header("References")]
    [SerializeField] GameObject topViewCamera;
    [SerializeField] GameObject player;
    [SerializeField] UIHandler UIHandler;

    List<Connector> connectorList = new List<Connector>();
    Transform tileFrom, tileTo, tileRoot;
    Transform container;
    int attempts = 0;
    const int maxAttempts = 10;
    Connector failedConnector;

    [Header("Available at Runtime")]
    public List<Tile> generatedTiles = new List<Tile>();

    void Start()
    {
        StartCoroutine(DungeonBuilder());
    }

    void OnEnable()
    {
        UIHandler.OnDungeonSizeChanged.AddListener(UpdateDungeonSize);
        UIHandler.OnConstructionDelayChanged.AddListener(UpdateConstructionDelay);
        UIHandler.OnHallwayChanceChanged.AddListener(UpdateHallwayChance);
    }

    void OnDisable()
    {
        UIHandler.OnDungeonSizeChanged.RemoveListener(UpdateDungeonSize);
        UIHandler.OnConstructionDelayChanged.RemoveListener(UpdateConstructionDelay);
        UIHandler.OnHallwayChanceChanged.RemoveListener(UpdateHallwayChance);
    }

    void Update()
    {
        // Reload scene
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(0);
        }
    }

    void UpdateDungeonSize(float value) { dungeonSize = Mathf.RoundToInt(value); }
    void UpdateConstructionDelay(float value) { constructionDelay = value; }
    void UpdateHallwayChance(float value) { hallwayChance = value; }

    IEnumerator DungeonBuilder()
    {
        GameObject goContainer = new GameObject("Generated Tiles");
        container = goContainer.transform;
        container.SetParent(transform);

        tileRoot = CreateRoom(startRoomPrefabs, "Start Room: ");
        tileRoot.SetParent(container);
        tileTo = tileRoot;

        while (generatedTiles.Count < dungeonSize)
        {
            yield return new WaitForSeconds(constructionDelay);
            tileFrom = GetRandomTileTransform();
            CreateTile();
            ConnectTiles(null);
            CollisionCheck();
        }

        RemoveNonconnectedHallways();
        BlockPassages();
        CleanupBoxes();
        SpawnDoors();

        yield return new WaitForSeconds(1f); // Wait a bit before switching the cameras

        player.SetActive(true);
        FindFirstObjectByType<InputController>().InitializePlayer(player); // This is necessary so that InputController can find the Player's components PlayerMovement and PlayerLook
        topViewCamera.SetActive(false); // Disable top-down view camera and focus on Player's camera instead.       
        yield return null;
    }

    void SpawnDoors()
    {
        // Get all connected connectors and spawn doors at them.
        Connector[] allConnectors = transform.GetComponentsInChildren<Connector>();
        for (int i = 0; i < allConnectors.Length; i++)
        {
            Connector myConnector = allConnectors[i];

            if (myConnector.isConnected)
            {
                Vector3 halfExtents = new Vector3(myConnector.size.x, 1f, myConnector.size.x);
                Vector3 pos = myConnector.transform.position;
                Vector3 offset = Vector3.up * 0.5f;
                Collider[] hits = Physics.OverlapBox(pos + offset, halfExtents, Quaternion.identity, LayerMask.GetMask("Door"));

                if (hits.Length == 0) // Check if there isn't already a door there.
                {
                    int doorIndex = Random.Range(0, doorPrefabs.Length);
                    GameObject goDoor = Instantiate(doorPrefabs[doorIndex], pos, myConnector.transform.rotation, myConnector.transform) as GameObject;
                    goDoor.name = doorPrefabs[doorIndex].name;
                }
            }
        }
    }

    void CleanupBoxes() // Remove all box colliders that were used to check for collision between other tiles
    {
        foreach (Tile myTile in generatedTiles)
        {
            if (myTile.tile.TryGetComponent<BoxCollider>(out var box))
            {
                Destroy(box);
            }
        }
    }

    void RemoveNonconnectedHallways()
    {
        List<Tile> hallwayTiles = generatedTiles.Where(x => x.tile.CompareTag("Hallway")).ToList(); // Finds and deletes all hallways that are not linked/connected with at least two rooms.
        foreach (Tile hallway in hallwayTiles)
        {
            if (hallway.tile.GetComponentsInChildren<Connector>().Count(c => c.isConnected) < 2) // Does the hallway have less than 2 connected Connectors?
            {
                hallway.connector.isConnected = false;
                generatedTiles.Remove(hallway);
                DestroyImmediate(hallway.tile.gameObject);
            }
        }
    }

    void BlockPassages()
    {
        List<Connector> unconnectedConnectors = transform.GetComponentsInChildren<Connector>().Where(c => !c.isConnected).ToList();

        for (int i = 0; i < unconnectedConnectors.Count; i++)
        {
            Connector connector = unconnectedConnectors[i];
            bool overlappingConnectorsFound = false;

            for (int j = i + 1; j < unconnectedConnectors.Count; j++)
            {
                Connector otherConnector = unconnectedConnectors[j];

                if (connector != otherConnector && connector.transform.position == otherConnector.transform.position)
                {
                    // Two connectors are overlapping, marking them as 'isConnected'
                    connector.isConnected = true;
                    otherConnector.isConnected = true;
                    overlappingConnectorsFound = true;
                    break;
                }
            }

            if (!overlappingConnectorsFound) // Place wall
            {
                PlaceWall(connector.transform.position, connector.transform.rotation, connector);
            }
        }
    }

    void PlaceWall(Vector3 position, Quaternion rotation, Connector connector)
    {
        int wallIndex = Random.Range(0, blockedPrefabs.Length);
        GameObject goWall = Instantiate(blockedPrefabs[wallIndex], position, rotation, connector.transform) as GameObject;
        goWall.name = blockedPrefabs[wallIndex].name;
    }

    void CollisionCheck()
    {
        if (!tileTo.TryGetComponent<BoxCollider>(out var box)) // Adds a Box Collider to 'tileTo' if there is none
        {
            box = tileTo.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
        }

        Vector3 offset = tileTo.TransformPoint(box.center) - tileTo.position;
        Vector3 halfExtents = box.bounds.extents;
        List<Collider> hits = Physics.OverlapBox(tileTo.position + offset, halfExtents, Quaternion.identity, LayerMask.GetMask("Tile")).ToList();
        hits = hits.Where(hit => hit.transform != tileFrom && hit.transform != tileTo).ToList(); // Filter out tileFrom and tileTo

        if (hits.Count > 0)
        {
            attempts++;
            //Debug.Log("Collision Detected: " + attempts);

            int toIndex = generatedTiles.FindIndex(x => x.tile == tileTo);
            failedConnector = null;

            if (generatedTiles[toIndex].connector != null)
            {
                failedConnector = generatedTiles[toIndex].connector;
                failedConnector.name = "failedConnector, with attempts: " + attempts;
                generatedTiles[toIndex].connector.isConnected = false; // Set recently added tile's connector status to FALSE                

                if (attempts >= maxAttempts)
                {
                    generatedTiles[toIndex].connector.isUnavailable = true; // Set tileTo's connector to Unavailable if max attempts is reached. (Must be done here before destroying and removing the tile from the list)                 
                }
            }

            generatedTiles.RemoveAt(toIndex);
            DestroyImmediate(tileTo.gameObject);

            if (attempts >= maxAttempts) // If max attempts is reached we'll choose a different tile to spawn tiles from
            {
                //Debug.Log("MAX ATTEMPTS REACHED");
                attempts = 0;
                tileFrom = GetRandomTileTransform();
                CreateTile();
                ConnectTiles(null); // null, because we're choosing a new tileFrom                
                CollisionCheck();
            }
            else
            {
                //Debug.Log("Still trying to access the same connector.");
                CreateTile();
                ConnectTiles(failedConnector.transform); // Attempt to connect at the same connector
                CollisionCheck();
            }
        }
        else
        {
            //Debug.Log("No collision detected! Resetting attempts...");
            attempts = 0;
        }
    }

    void ConnectTiles(Transform storedConnector)
    {
        Transform connectFrom = storedConnector != null ? GetSameConnector(storedConnector.transform) : GetRandomConnector(tileFrom); // If storedConnector is not null, assign connectFrom to that connector. Else, find a new random connector.
        //Debug.Log("Grabbing connector " + connectFrom);
        if (connectFrom == null)
        {
            Debug.Log("connectFrom not found");
            return;
        }

        Transform connectTo = GetRandomConnector(tileTo); // Create a Transform called "connectTo" and assign it to the tileTo's connector.
        if (connectTo == null)
        {
            Debug.Log("connectTo not found");
            return;
        }

        connectTo.SetParent(connectFrom);
        tileTo.SetParent(connectTo);
        connectTo.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        connectTo.Rotate(0, 180f, 0);
        tileTo.SetParent(container);
        connectTo.SetParent(tileTo.Find("Connectors"));
        generatedTiles.Last().connector = connectFrom.GetComponent<Connector>();
    }

    Transform GetSameConnector(Transform tile)
    {
        if (tile == null)
            return null;

        failedConnector.isConnected = true;

        return failedConnector.transform;
    }

    Transform GetRandomConnector(Transform tile)
    {
        if (tile == null)
            return null;

        // Create a list and store all found connectors in the chosen Tile, make sure it only looks for the non-connected ones.
        connectorList = tile.GetComponentsInChildren<Connector>().ToList().FindAll(x => x.isConnected == false && x.isUnavailable == false); // Lambda Expression

        // Check if there's any of the non-connected connectors available.
        if (connectorList.Count > 0)
        {
            int connectorIndex = Random.Range(0, connectorList.Count); // Choose a random available connector (that's not already connected)
            connectorList[connectorIndex].isConnected = true; // Set the chosen connector to "isConnected = true"

            if (tile == tileFrom)
            {
                if (!tile.TryGetComponent<BoxCollider>(out var box))
                {
                    box = tile.gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                }
            }

            return connectorList[connectorIndex].transform; // Return the chosen connector's transform property.
        }
        else
        {
            Debug.Log("NO AVAILABLE CONNECTORS FOUND!");
            // Move to another tile with available connectors?
        }

        return null;
    }

    Transform GetRandomTileTransform()
    {
        if (generatedTiles.Count > 0)
        {
            List<Tile> tilesWithConnectors = generatedTiles.Where(x => x.tile.GetComponentsInChildren<Connector>().Any(c => !c.isConnected && !c.isUnavailable)).ToList();

            if (tilesWithConnectors.Count > 0)
            {
                Tile closestTile = tilesWithConnectors.OrderBy(y => Vector3.Distance(y.tile.position, Vector3.zero)).First();
                //Debug.Log("Getting closest tile");
                return closestTile.tile;
            }
        }

        return null;
    }

    Transform CreateRoom(GameObject[] prefabs, string goName = "")
    {
        int index = Random.Range(0, prefabs.Length);

        GameObject go = Instantiate(prefabs[index], Vector3.zero, Quaternion.identity) as GameObject;
        go.name = $"{goName}{prefabs[index].name}";

        Transform origin = tileFrom != null ? generatedTiles[generatedTiles.FindIndex(x => x.tile == tileFrom)].tile : null;
        generatedTiles.Add(new Tile(go.transform, origin));

        return go.transform;
    }

    void CreateTile()
    {
        if (tileFrom.CompareTag("Room"))
        {
            tileTo = Random.value < hallwayChance ? CreateRoom(hallwayPrefabs) : CreateRoom(roomPrefabs);
        }
        if (tileFrom.CompareTag("Hallway"))
        {
            tileTo = CreateRoom(roomPrefabs);
        }
    }

    void OnDrawGizmos()
    {
        // TileFrom
        if (tileFrom == null)
            return;

        if (!tileFrom.TryGetComponent<MeshCollider>(out var meshCollider))
            return;

        Vector3 offset = meshCollider.bounds.center;
        Vector3 meshSize = meshCollider.bounds.size;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(offset, meshSize);

        // TileTo
        if (tileTo == null)
            return;

        if (!tileTo.TryGetComponent<MeshCollider>(out var tileToCollider))
            return;

        Vector3 tileToOffset = tileToCollider.bounds.center;
        Vector3 tileToMeshSize = tileToCollider.bounds.size;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(tileToOffset, tileToMeshSize);
    }
}

