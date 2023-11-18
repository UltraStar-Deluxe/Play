// using System;
// using Unity.Netcode;
//
// public class PlayerMicPitchTrackerNetworkBehavior : NetworkBehaviour
// {
//     public NetworkVariable<int> totalScore = new();
//
//     private void Start()
//     {
//         totalScore.OnValueChanged += OnTotalScoreChanged;
//     }
//
//     // public override void OnDestroy()
//     // {
//     //     base.OnDestroy();
//     //     totalScore.OnValueChanged -= OnTotalScoreChanged;
//     // }
//
//     private void OnTotalScoreChanged(int oldValue, int newValue)
//     {
//
//     }
// }
