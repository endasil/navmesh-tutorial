using System;

using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public GameObject cubeWall;
    public GameObject cubeFloor;
    public GameObject cubeWater;
    public GameObject cubeObstacle;
    public GameObject player;
    public GameObject npcPrefab;
    public GameObject EnvironmentParent;
    public GameObject groundParent;
    public GameObject waterParent;
    public GameObject wallParent;

    private readonly string[] layout =
    {
        "█████████████████████████████",
        "█  ██████ ~~                █",
        "█  █N         O      ~~~    █",
        "█  █    █ ~~~        █████  █",
        "█  ██ ███            █   █  █",
        "█            ████    █      █",
        "█  █    █    █  █    █  ███ █",
        "█      O     █  █    █  █   █",
        "█  ███ ██    █  ██ ███  █   █",
        "█   O       ~~~         █   █",
        "█            █       ██████ █",
        "█ ~~~~~~~~ ~~~~~~~~~~~~~~~~~█",
        "█ ~~~~~~~~ ~~~~~~~~~~~~~~~~~█",
        "█                    █████  █",
        "█    █O ██                  █",
        "█    █     p                █",
        "█████████████████████████████"
    };

    public float cubeSize = 1f;

    private float floorHeight;

    private void Start()
    {
        floorHeight = GetRendererHeight(cubeFloor);

        int rows = layout.Length;

        for (int y = 0; y < rows; y++)
        {
            string line = layout[y];

            for (int x = 0; x < line.Length; x++)
            {
                char c = char.ToUpper(line[x]);
                Vector3 basePosition = new Vector3(x * cubeSize, 0f, (rows - 1 - y) * cubeSize);

                GameObject prefab = c switch
                {
                    '█' => cubeWall,
                    ' ' => cubeFloor,
                    '~' => cubeWater,
                    'O' => cubeObstacle,
                    'P' or 'N' => cubeFloor,
                    _ => null
                };

                if (prefab != null && c != '~')
                {
                    Instantiate(cubeFloor, basePosition, Quaternion.identity, groundParent.transform);
                }

                switch (c)
                {
                    case '~':
                        Instantiate(cubeWater, basePosition, Quaternion.identity, waterParent.transform);
                        break;

                    case '█':
                        InstantiateOnTop(cubeWall, basePosition, wallParent.transform);
                        break;

                    case 'O':
                        InstantiateOnTop(cubeObstacle, basePosition, EnvironmentParent.transform);
                        break;

                    case 'P':
                        InstantiateOnTop(player, basePosition, EnvironmentParent.transform);
                        // player.transform.position = GetPositionOnTop(player, basePosition);
                        break;

                    case 'N':
                        Instantiate(npcPrefab, GetPositionOnTop(npcPrefab, basePosition), Quaternion.identity, transform);
                        break;
                }
            }
        }
    }

    private void InstantiateOnTop(GameObject prefab, Vector3 basePosition, Transform parent)
    {
        Vector3 pos = GetPositionOnTop(prefab, basePosition);
        Instantiate(prefab, pos, Quaternion.identity, parent);
    }

    private Vector3 GetPositionOnTop(GameObject prefab, Vector3 basePosition)
    {
        float objectHeight = GetRendererHeight(prefab);
        Debug.Log("Render height" + prefab.name +  $" {objectHeight}");
        Debug.Log($"{prefab.name} floor {cubeFloor.transform.localScale.y} offset {objectHeight * 0.5f -0.5f}  ");
        return basePosition + Vector3.up * (cubeFloor.transform.localScale.y + objectHeight * 0.5f -0.5f);
    }



    private float GetRendererHeight(GameObject prefab)
    {
        if (prefab == null) return 1f;

        Renderer renderer = prefab.GetComponentInChildren<Renderer>();
        if (renderer != null)
            return renderer.bounds.size.y;

        return prefab.transform.localScale.y; // fallback
    }
}
