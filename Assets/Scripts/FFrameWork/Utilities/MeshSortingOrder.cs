using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
// MeshRenderer に 2D 表示用の Sorting Layer と描画順を適用する補助コンポーネント。
public class MeshSortingOrder : MonoBehaviour
{
    [Header("2D Sorting Order")]
    public string sortingLayerName = "Default"; 
    public int sortingOrder = -100;              

    void Awake()
    {
        ApplySortingOrder();
    }

    void OnValidate()
    {
        ApplySortingOrder();
    }

    // 実行開始時と Inspector 更新時に、指定したレイヤー名と順序を MeshRenderer へ反映する。
    private void ApplySortingOrder()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;
        }
    }
}
