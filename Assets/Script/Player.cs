using UnityEngine;

public class Player : MonoBehaviour
{
    private CollisionVisualizer collisionVisualizer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // CollisionVisualizerコンポーネントを確認/追加
        collisionVisualizer = GetComponent<CollisionVisualizer>();
        if (collisionVisualizer == null)
        {
            collisionVisualizer = gameObject.AddComponent<CollisionVisualizer>();
            Debug.Log("CollisionVisualizerを自動追加しました");
        }
        
        // Colliderの確認
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("Player: Colliderが見つかりません。衝突検出にはColliderが必要です。");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("Player: ColliderのIs Triggerをオンにすることを推奨します。");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"OnTriggerEnter: {other.gameObject.name}と衝突");
    }
    
    void OnTriggerStay(Collider other)
    {
        // CollisionVisualizerが処理するため、ここでは特別な処理は不要
        // 必要に応じて追加の処理を記述可能
    }
    
    void OnTriggerExit(Collider other)
    {
        Debug.Log($"OnTriggerExit: {other.gameObject.name}から離脱");
    }
}
