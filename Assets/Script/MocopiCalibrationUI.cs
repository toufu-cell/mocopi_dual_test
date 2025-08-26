using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mocopi.Receiver;

public class MocopiCalibrationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button calibrateButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Mocopi Reference")]
    [SerializeField] private MocopiSimpleReceiver mocopiReceiver;
    
    void Start()
    {
        // MocopiSimpleReceiverが指定されていない場合、シーンから自動検索
        if (mocopiReceiver == null)
        {
            mocopiReceiver = FindObjectOfType<MocopiSimpleReceiver>();
        }
        
        // ボタンイベントを設定
        if (calibrateButton != null)
        {
            calibrateButton.onClick.AddListener(OnCalibrateButtonClicked);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetButtonClicked);
        }
        
        // 初期状態を更新
        UpdateStatusText();
    }
    
    void Update()
    {
        // ステータステキストを定期的に更新
        UpdateStatusText();
    }
    
    private void OnCalibrateButtonClicked()
    {
        if (mocopiReceiver == null)
        {
            Debug.LogError("MocopiCalibrationUI: MocopiSimpleReceiverが見つかりません");
            return;
        }
        
        mocopiReceiver.CalibrateAllAvatarsPosition();
        Debug.Log("MocopiCalibrationUI: 位置キャリブレーションを実行しました");
    }
    
    private void OnResetButtonClicked()
    {
        if (mocopiReceiver == null)
        {
            Debug.LogError("MocopiCalibrationUI: MocopiSimpleReceiverが見つかりません");
            return;
        }
        
        mocopiReceiver.ResetAllAvatarsPositionCalibration();
        Debug.Log("MocopiCalibrationUI: 位置キャリブレーションをリセットしました");
    }
    
    private void UpdateStatusText()
    {
        if (statusText == null || mocopiReceiver == null)
            return;
            
        bool anyCalibrated = false;
        int totalAvatars = mocopiReceiver.AvatarSettings.Count;
        int calibratedAvatars = 0;
        
        for (int i = 0; i < totalAvatars; i++)
        {
            if (mocopiReceiver.IsAvatarPositionCalibrated(i))
            {
                calibratedAvatars++;
                anyCalibrated = true;
            }
        }
        
        if (totalAvatars == 0)
        {
            statusText.text = "状態: アバターが設定されていません";
        }
        else if (calibratedAvatars == totalAvatars)
        {
            statusText.text = "状態: 全アバターキャリブレーション済み";
            statusText.color = Color.green;
        }
        else if (anyCalibrated)
        {
            statusText.text = $"状態: {calibratedAvatars}/{totalAvatars} アバターキャリブレーション済み";
            statusText.color = Color.yellow;
        }
        else
        {
            statusText.text = "状態: キャリブレーション未実行";
            statusText.color = Color.red;
        }
    }
    
    void OnDestroy()
    {
        // イベントリスナーをクリーンアップ
        if (calibrateButton != null)
        {
            calibrateButton.onClick.RemoveListener(OnCalibrateButtonClicked);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(OnResetButtonClicked);
        }
    }
}