using UnityEditor;
using UnityEngine;

public class AssetBundleCreate
{
    [MenuItem("Build/Build Asset Bundles")]
    static void BuildAssetBundles()
    {
        var assetBundlePathAndroid = $"{Application.dataPath}/../AssetBundle/Android";
        BuildPipeline.BuildAssetBundles(assetBundlePathAndroid, BuildAssetBundleOptions.None, BuildTarget.Android);
        var assetBundlePathIos = $"{Application.dataPath}/../AssetBundle/Ios";
        BuildPipeline.BuildAssetBundles(assetBundlePathIos, BuildAssetBundleOptions.None, BuildTarget.iOS);
    }
}
