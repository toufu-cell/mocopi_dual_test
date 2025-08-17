using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Triggerの重なりを検出して、交差点に赤い点を表示するコンポーネント
/// Physics.ComputePenetrationを使用して正確な貫通情報を取得
/// </summary>
public class CollisionVisualizer : MonoBehaviour
{
    [Header("表示設定")]
    [SerializeField] private float pointSize = 0.05f;
    [SerializeField] private Color collisionColor = Color.red;
    [SerializeField] private float pointLifetime = 2.0f;
    [SerializeField] private bool showDebugInfo = false;
    
    [Header("パフォーマンス設定")]
    [SerializeField] private int maxPoints = 30;
    [SerializeField] private float updateInterval = 0.1f; // 更新間隔（秒）
    
    // オブジェクトプール
    private Queue<GameObject> pointPool = new Queue<GameObject>();
    private List<GameObject> activePoints = new List<GameObject>();
    private Dictionary<Collider, float> lastUpdateTime = new Dictionary<Collider, float>();
    
    // 自身のCollider参照
    private Collider myCollider;
    
    void Start()
    {
        // 自身のColliderを取得
        myCollider = GetComponent<Collider>();
        if (myCollider == null)
        {
            Debug.LogError("CollisionVisualizer: Colliderが見つかりません。Colliderを追加してください。");
            enabled = false;
            return;
        }
        
        // Is Triggerがオンになっているか確認
        if (!myCollider.isTrigger)
        {
            Debug.LogWarning("CollisionVisualizer: ColliderのIs Triggerをオンにすることを推奨します。");
        }
        
        // オブジェクトプールを初期化
        InitializePool();
    }
    
    void InitializePool()
    {
        for (int i = 0; i < maxPoints; i++)
        {
            GameObject point = CreateCollisionPoint();
            point.SetActive(false);
            // プール内のオブジェクトもフラグを設定
            point.hideFlags = HideFlags.HideAndDontSave;
            pointPool.Enqueue(point);
        }
    }
    
    GameObject CreateCollisionPoint()
    {
        // 赤い球体を作成
        GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        point.name = "CollisionPoint";
        point.transform.localScale = Vector3.one * pointSize;
        
        // エディタでの保存を防ぐ
        point.hideFlags = HideFlags.HideAndDontSave;
        
        // コライダーを削除（視覚的な表示のみ）
        Collider pointCollider = point.GetComponent<Collider>();
        if (pointCollider != null)
        {
            Destroy(pointCollider);
        }
        
        // マテリアルを設定
        Renderer renderer = point.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Universal Render Pipelineに対応したマテリアル作成
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat == null)
            {
                // URPが見つからない場合は標準シェーダーを使用
                mat = new Material(Shader.Find("Standard"));
            }
            mat.color = collisionColor;
            // エミッシブカラーを設定して光らせる（オプション）
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", collisionColor * 0.5f);
            renderer.material = mat;
            
            // マテリアルも保存しないようにする
            mat.hideFlags = HideFlags.HideAndDontSave;
        }
        
        return point;
    }
    
    void OnTriggerStay(Collider other)
    {
        // 自分自身との衝突は無視
        if (other == myCollider)
        {
            return;
        }
        
        // 更新間隔をチェック（パフォーマンス最適化）
        if (lastUpdateTime.ContainsKey(other))
        {
            if (Time.time - lastUpdateTime[other] < updateInterval)
            {
                return;
            }
        }
        lastUpdateTime[other] = Time.time;
        
        // Physics.ComputePenetrationで貫通情報を計算
        Vector3 direction;
        float distance;
        bool isPenetrating = Physics.ComputePenetration(
            myCollider, transform.position, transform.rotation,
            other, other.transform.position, other.transform.rotation,
            out direction, out distance
        );
        
        if (isPenetrating)
        {
            // 接触点を計算（複数の方法を試す）
            Vector3 contactPoint = CalculateContactPoint(myCollider, other, direction, distance);
            
            // 赤い点を表示
            ShowCollisionPoint(contactPoint, direction);
            
            if (showDebugInfo)
            {
                Debug.Log($"衝突検出: {gameObject.name} <-> {other.gameObject.name}, 距離: {distance:F3}");
            }
        }
    }
    
    Vector3 CalculateContactPoint(Collider colliderA, Collider colliderB, Vector3 direction, float distance)
    {
        // 方法1: ClosestPointを使用した計算
        Vector3 pointA = colliderA.ClosestPoint(colliderB.transform.position);
        Vector3 pointB = colliderB.ClosestPoint(colliderA.transform.position);
        Vector3 midPoint = (pointA + pointB) * 0.5f;
        
        // 方法2: 貫通方向と距離から計算
        Vector3 centerA = colliderA.bounds.center;
        Vector3 penetrationPoint = centerA - direction * (distance * 0.5f);
        
        // 両方法の平均を取る（より正確な位置）
        return (midPoint + penetrationPoint) * 0.5f;
    }
    
    void ShowCollisionPoint(Vector3 position, Vector3 normal)
    {
        GameObject point = GetPointFromPool();
        
        if (point != null)
        {
            point.transform.position = position;
            
            // 法線方向を向かせる（オプション）
            if (normal != Vector3.zero)
            {
                point.transform.rotation = Quaternion.LookRotation(normal);
            }
            
            point.SetActive(true);
            activePoints.Add(point);
            
            // 一定時間後に非表示
            StartCoroutine(HidePointAfterDelay(point, pointLifetime));
        }
    }
    
    GameObject GetPointFromPool()
    {
        // アクティブな点が最大数に達している場合
        if (activePoints.Count >= maxPoints)
        {
            // 最も古い点を再利用
            if (activePoints.Count > 0)
            {
                GameObject oldestPoint = activePoints[0];
                activePoints.RemoveAt(0);
                StopCoroutine(HidePointAfterDelay(oldestPoint, 0));
                return oldestPoint;
            }
            return null;
        }
        
        if (pointPool.Count > 0)
        {
            return pointPool.Dequeue();
        }
        else
        {
            // プールが空の場合は新しく作成
            GameObject newPoint = CreateCollisionPoint();
            newPoint.hideFlags = HideFlags.HideAndDontSave;
            return newPoint;
        }
    }
    
    IEnumerator HidePointAfterDelay(GameObject point, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (point != null && point.activeSelf)
        {
            point.SetActive(false);
            activePoints.Remove(point);
            pointPool.Enqueue(point);
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        // Colliderが離れたら更新時間の記録を削除
        if (lastUpdateTime.ContainsKey(other))
        {
            lastUpdateTime.Remove(other);
        }
    }
    
    void OnDestroy()
    {
        // クリーンアップ
        foreach (var point in activePoints)
        {
            if (point != null)
            {
                DestroyImmediate(point);
            }
        }
        
        while (pointPool.Count > 0)
        {
            var point = pointPool.Dequeue();
            if (point != null)
            {
                DestroyImmediate(point);
            }
        }
        
        activePoints.Clear();
        lastUpdateTime.Clear();
    }
    
    void OnDisable()
    {
        // 無効化時にもクリーンアップ
        StopAllCoroutines();
        
        // アクティブな点を全て非表示にしてプールに戻す
        for (int i = activePoints.Count - 1; i >= 0; i--)
        {
            GameObject point = activePoints[i];
            if (point != null)
            {
                point.SetActive(false);
                pointPool.Enqueue(point);
            }
        }
        activePoints.Clear();
    }
    
    // デバッグ用のGizmo表示
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo || myCollider == null) return;
        
        // Colliderの範囲を表示
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(myCollider.bounds.center, myCollider.bounds.size);
    }
}