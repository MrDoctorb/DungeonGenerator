using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] int width, height;

    [SerializeField] int maxRoomWidth, maxRoomHeight;
    [SerializeField] int minRoomWidth, minRoomHeight;

    [SerializeField] int maxRoomAttempts;
    [SerializeField] bool roomsAreSeperate;

    [SerializeField] int minPathLength;

    [SerializeField] GameObject tile;
    DungeonTile[,] dungeonTiles;
    List<Connector> connectors = new List<Connector>();
    List<Vector2Int> endPoints = new List<Vector2Int>();
    List<Vector2Int> idLinks = new List<Vector2Int>();
    int currentid = 0;

    void Start()
    {
        dungeonTiles = new DungeonTile[width, height];
        MakeAllRooms();
        FullFloodFill();
        FindConnectors();
        Connect();
        Trim();

        foreach (DungeonTile tile in dungeonTiles)
        {
            if (tile != null)
            {
                Instantiate(this.tile, (Vector2)tile.pos, Quaternion.identity);
            }
        }
    }

    void MakeAllRooms()
    {
        for (int i = 0; i < maxRoomAttempts; ++i)
        {
            int x = Random.Range(0, width - maxRoomWidth);
            int y = Random.Range(0, height - maxRoomHeight);
            Vector2Int randPos = new Vector2Int(x, y);
            int newWidth = Random.Range(minRoomWidth, maxRoomWidth + 1);
            int newHeight = Random.Range(minRoomHeight, maxRoomHeight + 1);
            if (!Overlap(randPos, newWidth, newHeight))
            {
                MakeRoom(randPos, newWidth, newHeight);
            }
        }
    }

    void MakeRoom(Vector2Int startPos, int width, int height)
    {
        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < height; ++j)
            {
                dungeonTiles[startPos.x + i, startPos.y + j] = new DungeonTile(startPos + new Vector2Int(i, j), currentid);
                //Instantiate(tile, (Vector2)startPos + new Vector2Int(i, j), Quaternion.identity).GetComponent<SpriteRenderer>().color = Color.red;
            }
        }
        ++currentid;
    }

    bool Overlap(Vector2Int startPos, int width, int height)
    {
        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < height; ++j)
            {
                if (roomsAreSeperate)
                {
                    Vector2Int[] test = AdjacentTiles(new Vector2Int(startPos.x + i, startPos.y + j));

                    if (dungeonTiles[startPos.x + i, startPos.y + j] != null || test.Length != 0)
                    {

                        return true;
                    }
                }
                else if (dungeonTiles[startPos.x + i, startPos.y + j] != null) 
                {
                    return true;
                }
            }
        }
        print("C");
        return false;
    }

    void FullFloodFill()
    {
        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < height; ++j)
            {
                Vector2Int pos = new Vector2Int(i, j);
                if (AdjacentTiles(pos).Length == 0)
                {
                    Vector2Int[] path = FloodFill(pos);
                    if (path.Length >= minPathLength)
                    {
                        /*foreach (Vector2Int newPos in path)
                        {
                            //dungeonTiles[newPos.x, newPos.y] = new DungeonTile(newPos, currentid);
                            Instantiate(tile, (Vector2)newPos, Quaternion.identity).GetComponent<SpriteRenderer>().color = Color.blue;
                        }*/
                        endPoints.Add(path[0]);
                        endPoints.Add(path[path.Length - 1]);

                        ++currentid;
                    }
                    else
                    {
                        foreach (Vector2Int newPos in path)
                        {
                            dungeonTiles[newPos.x, newPos.y] = null;
                        }
                    }
                }
            }
        }
    }

    Vector2Int[] FloodFill(Vector2Int pos)
    {
        dungeonTiles[pos.x, pos.y] = new DungeonTile(pos, currentid);
        //Instantiate(tile, (Vector2)pos, Quaternion.identity).GetComponent<SpriteRenderer>().color = Color.blue;

        List<Vector2Int> attempts = new List<Vector2Int>();
        for (int i = 0; i < 4; ++i)
        {
            Vector2Int dir = DirectionToVector((Direction)Random.Range(0, 4));
            while (attempts.Contains(dir))
            {
                dir = DirectionToVector((Direction)Random.Range(0, 4));
            }
            attempts.Add(dir);
            dir += pos;
            if (!OutOfBounds(dir) && dungeonTiles[dir.x, dir.y] == null && AdjacentTiles(dir).Length == 1)
            {
                List<Vector2Int> path = new List<Vector2Int>(FloodFill(dir));
                path.Add(pos);
                return path.ToArray();
            }
        }
        return new Vector2Int[] { pos };
    }

    void FindConnectors()
    {
        foreach (DungeonTile tile in dungeonTiles)
        {
            if (tile != null)
            {
                for (int i = 0; i < 4; ++i)
                {
                    //Check two spaces away in each direction

                    Vector2Int testPos = tile.pos + DirectionToVector((Direction)i);
                    if (!OutOfBounds(testPos) && dungeonTiles[testPos.x, testPos.y] == null)
                    {
                        testPos += DirectionToVector((Direction)i);
                        if (!OutOfBounds(testPos) && dungeonTiles[testPos.x, testPos.y] != null)
                        {
                            int testId = dungeonTiles[testPos.x, testPos.y].id;
                            if (testId != tile.id)
                            {
                                Vector2Int connectPos = testPos - DirectionToVector((Direction)i);
                                connectors.Add(new Connector(connectPos, testId, tile.id));
                                // Instantiate(this.tile, (Vector2)connectPos, Quaternion.identity).transform.localScale *= .25f;
                            }
                        }
                    }
                }
            }
        }
    }

    void Trim()
    {
        while (endPoints.Count != 0)
        {
            Vector2Int pos = endPoints[0];
            Vector2Int[] adjacent = AdjacentTiles(pos);
            if (adjacent.Length == 1)
            {
                dungeonTiles[pos.x, pos.y] = null;
                endPoints.Add(adjacent[0]);
            }
            endPoints.RemoveAt(0);
        }
    }

    void Connect()
    {
        System.Random rand = new System.Random();
        connectors = (List<Connector>)connectors.OrderBy(item => rand.Next()).ToList();

        foreach (Connector connector in connectors)
        {
            if (!IDsInUse(connector.id1, connector.id2))
            {
                AddIDUsage(connector.id1, connector.id2);
                dungeonTiles[connector.pos.x, connector.pos.y] = new DungeonTile(connector.pos, connector.id1);
                // Instantiate(tile, (Vector2)connector.pos, Quaternion.identity);
            }
        }
    }

    bool IDsInUse(int id1, int id2)
    {
        Vector2Int idLink = new Vector2Int(id1, id2);
        if (id1 > id2)
        {
            idLink = new Vector2Int(id2, id1);
        }

        return idLinks.Contains(idLink);
    }

    void AddIDUsage(int id1, int id2)
    {
        if (id1 < id2)
        {
            idLinks.Add(new Vector2Int(id1, id2));
        }
        else
        {
            idLinks.Add(new Vector2Int(id2, id1));
        }
    }

    Vector2Int[] AdjacentTiles(Vector2Int pos)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        for (int i = 0; i < 4; ++i)
        {
            Vector2Int tryPos = pos + DirectionToVector((Direction)i);
            if (!OutOfBounds(tryPos))
            {
                if (dungeonTiles[tryPos.x, tryPos.y] != null)
                {
                    tiles.Add(tryPos);
                }
            }
        }

        return tiles.ToArray();
    }

    Vector2Int DirectionToVector(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up:
                return Vector2Int.up;
            case Direction.Right:
                return Vector2Int.right;
            case Direction.Down:
                return Vector2Int.down;
            case Direction.Left:
                return Vector2Int.left;
            default:
                return new Vector2Int();
        }
    }


    bool OutOfBounds(Vector2Int pos)
    {
        return pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height;
    }
}

public class DungeonTile
{
    public Vector2Int pos;
    public int id;

    public DungeonTile(Vector2Int pos, int id)
    {
        this.pos = pos;
        this.id = id;
    }
}

public class Connector
{
    public Vector2Int pos;
    public int id1, id2;

    public Connector(Vector2Int pos, int id1, int id2)
    {
        this.pos = pos;
        this.id1 = id1;
        this.id2 = id2;
    }
}

public enum Direction
{
    Up,
    Right,
    Down,
    Left
}