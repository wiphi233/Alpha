using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MazeGenerator : MonoBehaviour
{
    public static MazeGenerator Instance;
    public static int playCount = 0;

    [Header("Maze Settings 迷宫设置")]
    public GameObject WallPerfab;
    public GameObject FloorPerfab;
    public GameObject PointLightPerfab;

    private readonly int[][] move =
    {
        new int[] { -1, 0, -1, 0 },
        new int[] { 1, 0, 1, 0 },
        new int[] { 0, -1, 0, -1 },
        new int[] { 0, 1, 0, 1 }
    };
    private int n;
    private int m;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            playCount = 0;
        }
        else
        {
            Debug.LogWarning("重复的 MazeGenerator 实例！");
            Destroy(this);
        }
    }

    public List<List<bool>> GenerateMaze(int _n, int _m, int startX, int startY, int? seed = null)
    {
        n = _n;
        m = _m;
        if (startX <= 0 || startX >= n - 1 || startY <= 0 || startY >= m - 1)
        {
            return null;
        }
        if (startX % 2 == 0)
        {
            startX--;
        }
        if (startY % 2 == 0)
        {
            startY--;
        }
        System.Random rnd;
        if (seed.HasValue)
        {
            rnd = new System.Random(seed.Value);
        }
        else
        {
            rnd = new System.Random();
        }
        List<List<bool>> maze = new List<List<bool>>();
        for (int i = 0; i < n; i++)
        {
            maze.Add(new List<bool>());
            for (int j = 0; j < m; j++)
            {
                maze[i].Add(true);
            }
        }
        maze[startX][startY] = false;
        List<Wall> walls = new List<Wall>();
        AddWalls(maze, walls, startX, startY);
        while (walls.Count > 0)
        {
            int idx = rnd.Next(walls.Count);
            Wall wall = walls[idx];
            walls.RemoveAt(idx);
            int nextX = wall.x + wall.dx;
            int nextY = wall.y + wall.dy;
            if (Inside(nextX, nextY) && maze[nextX][nextY])
            {
                maze[wall.x][wall.y] = false;
                maze[nextX][nextY] = false;
                AddWalls(maze, walls, nextX, nextY);
            }
        }
        return maze;
    }

    private void AddWalls(List<List<bool>> maze, List<Wall> walls, int x, int y)
    {
        foreach (var v in move)
        {
            int wallX = x + v[0];
            int wallY = y + v[1];
            if (Inside(wallX, wallY) && maze[wallX][wallY])
            {
                bool flag = true;
                foreach (var w in walls)
                {
                    if (w.x == wallX && w.y == wallY)
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    walls.Add(new Wall(wallX, wallY, v[2], v[3]));
                }
            }
        }
    }

    private bool Inside(int x, int y)
    {
        return x >= 0 && x < n && y >= 0 && y < m;
    }

    private class Wall
    {
        public int x;
        public int y;
        public int dx;
        public int dy;
        public Wall(int x, int y, int dx, int dy)
        {
            this.x = x;
            this.y = y;
            this.dx = dx;
            this.dy = dy;
        }
    }

    public void CreateMaze(Vector3 mazePosition, Vector2Int startPosition, Vector2Int size, out Vector3 endPosition, out GameObject mazeObject, float step = 1, float yRotation = 0f, int? seed = null)
    {
        endPosition = Vector3.zero;
        mazeObject = null;
        if (seed == null)
        {
            seed = startPosition.x * 2 + startPosition.y * 3 + size.x * 5 + size.y * 7;
        }
        List<List<bool>> mazeData = GenerateMaze(size.x, size.y, startPosition.x, startPosition.y, seed);
        if (mazeData == null)
        {
            Debug.LogWarning("mazeData is null.");
            return;
        }

        // 计算迷宫在世界空间中的包围盒中心
        Vector3 mazeCenter = new Vector3(
            mazePosition.x + size.x * step * 0.5f,
            mazePosition.y,
            mazePosition.z + size.y * step * 0.5f
        );

        // 创建旋转容器
        GameObject mazeGroup = new GameObject("MazeGroup");
        mazeGroup.transform.position = mazeCenter;
        mazeGroup.transform.rotation = Quaternion.Euler(0, yRotation, 0);

        // ---- 地板 ----
        Vector3 floorWorldPos = new Vector3(mazePosition.x + size.x * step / 2, mazePosition.y + 0.1f, mazePosition.z + size.y * step / 2);
        GameObject floorObj = Instantiate(FloorPerfab, floorWorldPos, Quaternion.identity);
        floorObj.transform.SetParent(mazeGroup.transform);
        floorObj.transform.localRotation = Quaternion.identity;
        floorObj.transform.localScale = new Vector3((size.x + 2) * step, 0.2f, (size.y + 2) * step);
        floorObj.SetActive(true);

        // ---- 内部墙壁 ----
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                if (i == size.x - 1 && j == size.y - 2)
                {
                    float x = mazePosition.x + i * step;
                    float z = mazePosition.z + j * step;
                    Vector3 wallWorldPos = new Vector3(x, mazePosition.y + 0.1f + (step / 2), z);
                    GameObject obj = Instantiate(PointLightPerfab, wallWorldPos, Quaternion.identity);
                    obj.transform.SetParent(mazeGroup.transform);
                    obj.transform.localPosition = wallWorldPos - mazeCenter;
                    obj.transform.localRotation = Quaternion.identity;
                    obj.SetActive(true);
                    endPosition = obj.transform.position;
                    continue;
                }
                if (mazeData[i][j])
                {
                    float x = mazePosition.x + i * step;
                    float z = mazePosition.z + j * step;
                    Vector3 wallWorldPos = new Vector3(x, mazePosition.y + 0.1f + (step / 2), z);
                    GameObject obj = Instantiate(WallPerfab, wallWorldPos, Quaternion.identity);
                    obj.transform.SetParent(mazeGroup.transform);
                    obj.transform.localPosition = wallWorldPos - mazeCenter;
                    obj.transform.localRotation = Quaternion.identity;
                    obj.SetActive(true);
                }
            }
        }
        mazeObject = mazeGroup;
    }
}