using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GridMapMaker.Tutorial
{


    /// <summary>
    /// Base tile used to create a unity tile map
    /// </summary>
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

#if UNITY_EDITOR

        [MenuItem("Assets/Create/2D/Custom Tiles/BaseTile")]
        public static void CreateAsset()
        {
            string path = "Assets/Tiles/BaseTile.asset";
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<BaseTile>(), path);
        }
#endif

    }


}