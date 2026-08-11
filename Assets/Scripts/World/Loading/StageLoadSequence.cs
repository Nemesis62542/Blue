using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Blue.World.Loading
{
    /// <summary>
    /// 1フェーズの実測結果。
    /// </summary>
    public struct StageLoadPhaseReport
    {
        public string label;
        public float weight;
        public float seconds;

        /// <summary>このフェーズでの Total Reserved Memory の増分(byte)</summary>
        // 予約プールは大きな単位で確保されるため、実際の確保量が増えても
        // 予約済みの余りに収まると増分が出ない。粗い指標として扱うこと。
        public long reservedMemoryDelta;

        /// <summary>このフェーズでの Total Allocated Memory の増分(byte)</summary>
        // 実際に確保された量。予約プールの粒度に影響されないので、
        // 「このフェーズが何MB積んだか」はこちらを見る。
        // どちらも Development Build でのみ意味のある値が取れる（リリースビルドでは0）。
        public long allocatedMemoryDelta;
    }

    /// <summary>
    /// ステージロードのフェーズを順に実行し、重み付けした全体進捗を報告する。
    /// </summary>
    // Garage からフィールドへ遷移する際のロード画面はこれを購読するだけでよく、
    // 個々のフェーズの実装を知る必要はない。
    public class StageLoadSequence
    {
        #region Fields

        private readonly List<IStageLoadPhase> phases = new List<IStageLoadPhase>();
        private readonly List<StageLoadPhaseReport> reports = new List<StageLoadPhaseReport>();

        // 1フェーズがこの秒数を超えたら異常とみなす。ロードが完了しない不具合は
        // 「待ち続けて何も起きない」形で出るため、黙って止まらないようにする。
        private readonly float phaseTimeoutSeconds;

        #endregion

        #region Properties

        public IReadOnlyList<IStageLoadPhase> Phases => phases;

        /// <summary>各フェーズの実測結果</summary>
        // Weight を勘で決めないための材料になる。
        public IReadOnlyList<StageLoadPhaseReport> Reports => reports;

        public bool IsRunning { get; private set; }

        #endregion

        #region Construction

        public StageLoadSequence(float phaseTimeoutSeconds = 120f)
        {
            this.phaseTimeoutSeconds = phaseTimeoutSeconds;
        }

        public StageLoadSequence Add(IStageLoadPhase phase)
        {
            if (phase != null)
            {
                phases.Add(phase);
            }

            return this;
        }

        #endregion

        #region Run

        /// <summary>
        /// 全フェーズを順に実行する。
        /// </summary>
        public async UniTask RunAsync(IProgress<StageLoadStatus> progress, CancellationToken cancellationToken = default)
        {
            IsRunning = true;
            reports.Clear();

            try
            {
                float totalWeight = 0f;
                foreach (IStageLoadPhase phase in phases)
                {
                    totalWeight += Mathf.Max(0f, phase.Weight);
                }

                if (totalWeight <= 0f)
                {
                    totalWeight = 1f;
                }

                float completedWeight = 0f;

                for (int i = 0; i < phases.Count; i++)
                {
                    IStageLoadPhase phase = phases[i];
                    float startTime = Time.realtimeSinceStartup;
                    long startReserved = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
                    long startAllocated = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();

                    phase.Begin();
                    Report(progress, i, phase, completedWeight, totalWeight);

                    while (!phase.IsDone)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        phase.Tick();
                        Report(progress, i, phase, completedWeight, totalWeight);

                        if (Time.realtimeSinceStartup - startTime > phaseTimeoutSeconds)
                        {
                            throw new TimeoutException(
                                $"[StageLoadSequence] フェーズ '{phase.Label}' が {phaseTimeoutSeconds} 秒で完了しませんでした" +
                                $"（進捗 {phase.Progress:P0} で停止）。");
                        }

                        await UniTask.Yield(cancellationToken);
                    }

                    completedWeight += Mathf.Max(0f, phase.Weight);
                    ReportCompleted(progress, i, phase, completedWeight, totalWeight);

                    reports.Add(new StageLoadPhaseReport
                    {
                        label = phase.Label,
                        weight = phase.Weight,
                        seconds = Time.realtimeSinceStartup - startTime,
                        reservedMemoryDelta = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() - startReserved,
                        allocatedMemoryDelta = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() - startAllocated,
                    });
                }
            }
            finally
            {
                IsRunning = false;
            }
        }

        #endregion

        #region Report

        private void Report(IProgress<StageLoadStatus> progress, int index, IStageLoadPhase phase,
                            float completedWeight, float totalWeight)
        {
            if (progress == null)
            {
                return;
            }

            float phaseProgress = Mathf.Clamp01(phase.Progress);

            progress.Report(new StageLoadStatus
            {
                phaseLabel = phase.Label,
                phaseIndex = index,
                phaseCount = phases.Count,
                phaseProgress = phaseProgress,
                totalProgress = Mathf.Clamp01((completedWeight + Mathf.Max(0f, phase.Weight) * phaseProgress) / totalWeight),
            });
        }

        private void ReportCompleted(IProgress<StageLoadStatus> progress, int index, IStageLoadPhase phase,
                                     float completedWeight, float totalWeight)
        {
            if (progress == null)
            {
                return;
            }

            progress.Report(new StageLoadStatus
            {
                phaseLabel = phase.Label,
                phaseIndex = index,
                phaseCount = phases.Count,
                phaseProgress = 1f,
                totalProgress = Mathf.Clamp01(completedWeight / totalWeight),
            });
        }

        #endregion
    }
}
