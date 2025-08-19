using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionVisualizer : MonoBehaviour
{
    [Header("表示設定")]
    [SerializeField] private float pointSize = 0.05f;
    [SerializeField] private Color collisionColor = Color.red;
    [SerializeField] private float pointLifetime = 2.0f;
    [SerializeField] private bool showDebugInfo = false;
    
    [Header("パフォーマンス設定")]
    [SerializeField] private int maxPoints = 50;
    [SerializeField] private float updateInterval = 0.1f;
    
    [Header("Collider設定")]
    [SerializeField] private bool detectChildColliders = true;
    [SerializeField] private bool useColorPerCollider = true;
    [SerializeField] private bool ignoreParentChildCollisions = true;
    
    // オブジェクトプール
    private Queue<GameObject> pointPool = new Queue<GameObject>();
    private List<GameObject> activePoints = new List<GameObject>();
    
    // 複数Collider管理
    private Dictionary<Collider, Dictionary<Collider, float>> lastUpdateTime = new Dictionary<Collider, Dictionary<Collider, float>>();
    private List<Collider> myColliders = new List<Collider>();
    
    // 各Colliderごとの色設定
    private Dictionary<Collider, Color> colliderColors = new Dictionary<Collider, Color>();
    private Color[] availableColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan };
    private int colorIndex = 0;
    
    // 親子関係管理
    private HashSet<Transform> myTransforms = new HashSet<Transform>();
    
    void Start()
    {
        // ColliderとTransformを収集
        CollectColliders();
        
        if (myColliders.Count == 0)
        {
            Debug.LogError("CollisionVisualizer: Colliderが見つかりません。Colliderを追加してください。");
            enabled = false;
            return;
        }
        
        Debug.Log($"CollisionVisualizer: {myColliders.Count}個のColliderを検知しました。");
        
        // 各Colliderに色を割り当て
        if (useColorPerCollider)
        {
            AssignColorsToColliders();
        }
        
        // オブジェクトプールを初期化
        InitializePool();
    }
    
    void CollectColliders()
    {
        if (detectChildColliders)
        {
            // 自身と子オブジェクトのColliderを取得
            Collider[] allColliders = GetComponentsInChildren<Collider>();
            
            foreach (var col in allColliders)
            {
                myColliders.Add(col);
                lastUpdateTime[col] = new Dictionary<Collider, float>();
                myTransforms.Add(col.transform);
                
                // ColliderがTriggerでない場合は警告
                if (!col.isTrigger && showDebugInfo)
                {
                    Debug.LogWarning($"CollisionVisualizer: {col.gameObject.name}のColliderのIs Triggerをオンにすることを推奨します。");
                }
            }
        }
        else
        {
            // 自身のColliderのみ取得
            Collider[] colliders = GetComponents<Collider>();
            foreach (var col in colliders)
            {
                myColliders.Add(col);
                lastUpdateTime[col] = new Dictionary<Collider, float>();
                myTransforms.Add(col.transform);
            }
        }
    }
    
    void AssignColorsToColliders()
    {
        foreach (var col in myColliders)
        {
            // 各Colliderに異なる色を割り当て
            colliderColors[col] = availableColors[colorIndex % availableColors.Length];
            colorIndex++;
            
            if (showDebugInfo)
            {
                Debug.Log($"Collider '{col.gameObject.name}' に色 {colliderColors[col]} を割り当て");
            }
        }
    }
    
    void InitializePool()
    {
        for (int i = 0; i < maxPoints; i++)
        {
            GameObject point = CreateCollisionPoint();
            point.SetActive(false);
            point.hideFlags = HideFlags.DontSave;
            pointPool.Enqueue(point);
        }
    }
    
    GameObject CreateCollisionPoint()
    {
        GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        point.name = "CollisionPoint";
        point.transform.localScale = Vector3.one * pointSize;
        
        // エディタでの保存を防ぐ
        point.hideFlags = HideFlags.DontSave;
        
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
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
            }
            mat.color = collisionColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", collisionColor * 0.5f);
            renderer.material = mat;
            mat.hideFlags = HideFlags.DontSave;
        }
        
        return point;
    }
    
    void OnTriggerStay(Collider other)
    {
        // 各自分のColliderについて衝突をチェック
        foreach (var myCol in myColliders)
        {
            // 無効なColliderはスキップ
            if (myCol == null || !myCol.enabled || !myCol.gameObject.activeInHierarchy)
            {
                continue;
            }
            
            // 自分自身との衝突は無視
            if (other == myCol)
            {
                continue;
            }
            
            // 同じGameObjectグループの他のColliderとの衝突も無視
            if (myColliders.Contains(other))
            {
                continue;
            }
            
            // 親子関係の衝突を無視する設定がオンの場合
            if (ignoreParentChildCollisions && IsParentChildRelation(myCol.transform, other.transform))
            {
                continue;
            }
            
            // 更新間隔をチェック
            if (lastUpdateTime[myCol].ContainsKey(other))
            {
                if (Time.time - lastUpdateTime[myCol][other] < updateInterval)
                {
                    continue;
                }
            }
            lastUpdateTime[myCol][other] = Time.time;
            
            // 衝突判定を行う
            CheckCollisionBetween(myCol, other);
        }
    }
    
    bool IsParentChildRelation(Transform t1, Transform t2)
    {
        // t1がt2の親、またはt2がt1の親の場合はtrue
        return t1.IsChildOf(t2) || t2.IsChildOf(t1);
    }
    
    void CheckCollisionBetween(Collider myCol, Collider other)
    {
        Vector3 direction;
        float distance;
        
        // ColliderのTransformを使用して正確な位置を取得
        bool isPenetrating = Physics.ComputePenetration(
            myCol, myCol.transform.position, myCol.transform.rotation,
            other, other.transform.position, other.transform.rotation,
            out direction, out distance
        );
        
        if (isPenetrating)
        {
            Vector3 contactPoint = CalculateContactPoint(myCol, other, direction, distance);
            
            // このColliderに割り当てられた色を使用
            Color pointColor = (useColorPerCollider && colliderColors.ContainsKey(myCol)) ? 
                colliderColors[myCol] : collisionColor;
            
            ShowCollisionPoint(contactPoint, direction, pointColor, myCol.gameObject.name, other.gameObject.name);
            
            if (showDebugInfo)
            {
                Debug.Log($"衝突検出: {myCol.gameObject.name} <-> {other.gameObject.name}, 距離: {distance:F3}");
            }
        }
    }
    
    Vector3 CalculateContactPoint(Collider colliderA, Collider colliderB, Vector3 direction, float distance)
    {
        // 両Colliderの最近接点を計算
        Vector3 pointA = colliderA.ClosestPoint(colliderB.transform.position);
        Vector3 pointB = colliderB.ClosestPoint(colliderA.transform.position);
        
        // 中点を計算
        Vector3 midPoint = (pointA + pointB) * 0.5f;
        
        // 貫通方向と距離から計算した点
        Vector3 centerA = colliderA.bounds.center;
        Vector3 penetrationPoint = centerA - direction * (distance * 0.5f);
        
        // 両方法の平均を取る
        return (midPoint + penetrationPoint) * 0.5f;
    }
    
    void ShowCollisionPoint(Vector3 position, Vector3 normal, Color color, string colA, string colB)
    {
        GameObject point = GetPointFromPool();
        
        if (point != null)
        {
            point.transform.position = position;
            
            // 法線方向を向かせる
            if (normal != Vector3.zero)
            {
                point.transform.rotation = Quaternion.LookRotation(normal);
            }
            
            // 色を設定
            Renderer renderer = point.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
                renderer.material.SetColor("_EmissionColor", color * 0.5f);
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
            newPoint.hideFlags = HideFlags.DontSave;
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
        // 各Colliderから離れた場合の処理
        foreach (var myCol in myColliders)
        {
            if (lastUpdateTime.ContainsKey(myCol) && lastUpdateTime[myCol].ContainsKey(other))
            {
                lastUpdateTime[myCol].Remove(other);
            }
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
        myColliders.Clear();
        colliderColors.Clear();
        myTransforms.Clear();
    }
    
    void OnDisable()
    {
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
        if (!showDebugInfo) return;
        
        // 各Colliderの範囲を異なる色で表示
        if (Application.isPlaying && myColliders != null)
        {
            foreach (var col in myColliders)
            {
                if (col != null)
                {
                    Color gizmoColor = (useColorPerCollider && colliderColors.ContainsKey(col)) ? 
                        colliderColors[col] : Color.yellow;
                    gizmoColor.a = 0.3f;
                    Gizmos.color = gizmoColor;
                    Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
                    
                    // Collider名を表示（Unity Editor上でのみ）
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(col.bounds.center, col.gameObject.name);
                    #endif
                }
            }
        }
        else if (!Application.isPlaying)
        {
            // エディタモードでの簡易表示
            Collider[] cols = detectChildColliders ? 
                GetComponentsInChildren<Collider>() : 
                GetComponents<Collider>();
                
            foreach (var col in cols)
            {
                if (col != null)
                {
                    Gizmos.color = new Color(1, 1, 0, 0.3f);
                    Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
                    
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(col.bounds.center, col.gameObject.name);
                    #endif
                }
            }
        }
    }
}