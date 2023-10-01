using System;
using System.Collections.Generic;
using YARG.Core.Chart;
using YARG.Core.Input;

namespace YARG.Core.Engine
{
    // This is a hack lol
    public abstract class BaseEngine
    {
        public bool IsInputQueued => InputQueue.Count > 0;

        public int BaseScore { get; protected set; }

        public int[] StarScoreThresholds { get; protected set; }

        protected bool IsInputUpdate { get; private set; }
        protected bool IsBotUpdate   { get; private set; }

        protected readonly SyncTrack SyncTrack;

        protected readonly Queue<GameInput> InputQueue;

        protected readonly uint Resolution;
        protected readonly uint TicksPerSustainPoint;

        protected GameInput CurrentInput;

        /// <summary>
        /// Whether or not the specified engine should treat a note as a chord, or separately.
        /// For example, guitars would treat each note as a chord, where as drums would treat them
        /// as singular pieces.
        /// </summary>
        public abstract bool TreatChordAsSeparate { get; }

        protected BaseEngine(BaseEngineParameters parameters, SyncTrack syncTrack)
        {
            SyncTrack = syncTrack;
            Resolution = syncTrack.Resolution;

            TicksPerSustainPoint = Resolution / 25;

            float[] multiplierThresholds = parameters.StarMultiplierThresholds;
            StarScoreThresholds = new int[multiplierThresholds.Length];
            for (int i = 0; i < multiplierThresholds.Length; i++)
            {
                StarScoreThresholds[i] = (int)(BaseScore * multiplierThresholds[i]);
            }

            InputQueue = new Queue<GameInput>();
            CurrentInput = new GameInput(-9999, -9999, -9999);
        }

        /// <summary>
        /// Gets the number of notes the engine recognizes in a specific note parent.
        /// This number is determined by <see cref="TreatChordAsSeparate"/>.
        /// </summary>
        public int GetNumberOfNotes<T>(T type) where T : Note<T>
        {
            return TreatChordAsSeparate ? type.ChildNotes.Count + 1 : 1;
        }

        /// <summary>
        /// Queue an input to be processed by the engine.
        /// </summary>
        /// <param name="input">The input to queue into the engine.</param>
        public void QueueInput(GameInput input)
        {
            InputQueue.Enqueue(input);
        }

        /// <summary>
        /// Updates the engine and processes all inputs currently queued.
        /// </summary>
        public void UpdateEngine()
        {
            if (!IsInputQueued)
            {
                return;
            }

            IsInputUpdate = true;
            ProcessInputs();
        }

        /// <summary>
        /// Updates the engine with no input processing.
        /// </summary>
        /// <param name="time">The time to simulate hit logic at.</param>
        public void UpdateEngine(double time)
        {
            IsInputUpdate = false;
            bool noteUpdated;
            do
            {
                noteUpdated = UpdateHitLogic(time);
            } while (noteUpdated);
        }

        /// <summary>
        /// Loops through the input queue and processes each input. Invokes engine logic for each input.
        /// </summary>
        protected void ProcessInputs()
        {
            // Start to process inputs in queue.
            while (InputQueue.TryDequeue(out var input))
            {
                // Execute a non-input update using the input 's time.
                // This will update the engine to the time of the first input, missing notes before the input is processed
                UpdateEngine(input.Time);

                CurrentInput = input;
                IsInputUpdate = true;
                bool noteUpdated;
                do
                {
                    noteUpdated = UpdateHitLogic(input.Time);
                    IsInputUpdate = false;
                } while (noteUpdated);
            }
        }

        protected uint GetCurrentTick(double time)
        {
            return SyncTrack.TimeToTick(time);
        }

        public virtual void UpdateBot(double songTime)
        {
            IsInputUpdate = false;
            IsBotUpdate = true;
        }

        public abstract void Reset(bool keepCurrentButtons = false);

        /// <summary>
        /// Executes engine logic with respect to the given time.
        /// </summary>
        /// <param name="time">The time in which to simulate hit logic at.</param>
        /// <returns>True if a note was updated (hit or missed). False if no changes.</returns>
        protected abstract bool UpdateHitLogic(double time);

        /// <summary>
        /// Resets the engine's state back to default and then processes the list of inputs up to the given time.
        /// </summary>
        /// <param name="time">Time to process up to.</param>
        /// <param name="inputs">List of inputs to execute against.</param>
        /// <returns>The input index that was processed up to.</returns>
        public abstract int ProcessUpToTime(double time, IEnumerable<GameInput> inputs);

        /// <summary>
        /// Processes the list of inputs from the given start time to the given end time. Does not reset the engine's state.
        /// </summary>
        /// <param name="startTime">Time to begin processing from.</param>
        /// <param name="endTime">Time to process up to.</param>
        /// <param name="inputs">List of inputs to execute against.</param>
        public abstract void ProcessFromTimeToTime(double startTime, double endTime, IEnumerable<GameInput> inputs);
    }

    public abstract class BaseEngine<TEngineParams, TEngineStats, TEngineState> : BaseEngine
        where TEngineParams : BaseEngineParameters
        where TEngineStats : BaseStats, new()
        where TEngineState : BaseEngineState, new()
    {
        protected const double STAR_POWER_PHRASE_AMOUNT = 0.25;

        public delegate void StarPowerStatusEvent(bool active);
        public StarPowerStatusEvent? OnStarPowerStatus;

        public readonly TEngineStats EngineStats;

        protected readonly TEngineParams EngineParameters;

        public TEngineState State;

        protected BaseEngine(SyncTrack syncTrack, TEngineParams engineParameters) : base(engineParameters, syncTrack)
        {
            EngineParameters = engineParameters;

            EngineStats = new TEngineStats();
            State = new TEngineState();
            State.Reset();

            EngineStats.ScoreMultiplier = 1;
        }

        protected void UpdateTimeVariables(double time)
        {
            State.LastUpdateTime = State.CurrentTime;
            State.CurrentTime = time;

            State.LastTick = State.CurrentTick;
            State.CurrentTick = GetCurrentTick(time);

            var timeSigs = SyncTrack.TimeSignatures;
            while (State.NextTimeSigIndex < timeSigs.Count && timeSigs[State.NextTimeSigIndex].Time < time)
            {
                State.CurrentTimeSigIndex++;
                State.NextTimeSigIndex++;
            }

            var currentTimeSig = timeSigs[State.CurrentTimeSigIndex];

            State.TicksEveryEightMeasures =
                (uint) (Resolution * ((double) 4 / currentTimeSig.Denominator) * currentTimeSig.Numerator * 8);
        }

        public override void Reset(bool keepCurrentButtons = false)
        {
            CurrentInput = new GameInput(-9999, -9999, -9999);
            InputQueue.Clear();

            State.Reset();
            EngineStats.Reset();
        }

        protected abstract void UpdateMultiplier();

        protected void UpdateStars()
        {
            if (State.CurrentStarIndex >= StarScoreThresholds.Length || StarScoreThresholds[0] == 0)
            {
                return;
            }

            if (EngineStats.Score >= StarScoreThresholds[State.CurrentStarIndex])
            {
                EngineStats.Stars++;
                State.CurrentStarIndex++;
            }
        }

        protected void DepleteStarPower(double amount)
        {
            if (!EngineStats.IsStarPowerActive)
            {
                return;
            }

            EngineStats.StarPowerAmount -= amount;
            if (EngineStats.StarPowerAmount <= 0)
            {
                EngineStats.StarPowerAmount = 0;
                EngineStats.IsStarPowerActive = false;
                OnStarPowerStatus?.Invoke(false);
            }
        }

        protected void ActivateStarPower()
        {
            if (EngineStats.IsStarPowerActive)
            {
                return;
            }

            EngineStats.IsStarPowerActive = true;
            OnStarPowerStatus?.Invoke(true);
        }

        protected double GetUsedStarPower()
        {
            return (State.CurrentTick - State.LastTick) / (double) State.TicksEveryEightMeasures;
        }

        public override int ProcessUpToTime(double time, IEnumerable<GameInput> inputs)
        {
            Reset();

            var inputIndex = 0;
            foreach (var input in inputs)
            {
                if (input.Time > time)
                {
                    break;
                }

                InputQueue.Enqueue(input);
                inputIndex++;
            }

            ProcessInputs();

            return inputIndex;
        }

        public override void ProcessFromTimeToTime(double startTime, double endTime, IEnumerable<GameInput> inputs)
        {
            throw new NotImplementedException();
        }

        protected abstract int CalculateBaseScore();
    }
}