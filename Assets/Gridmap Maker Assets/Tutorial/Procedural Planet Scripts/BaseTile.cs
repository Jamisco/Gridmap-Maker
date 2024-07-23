using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class BaseTile : Tile
{
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = sprite;
    }

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        // please dont call base.refresh method in this method,
        // your just creating a stack overflow
    }

    [MenuItem("Assets/Create/2D/Custom Tiles/BaseTile")]
    public static void CreateAsset()
    {
        string path = "Assets/Tiles/BaseTile.asset";
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<BaseTile>(), path);
    }

}
