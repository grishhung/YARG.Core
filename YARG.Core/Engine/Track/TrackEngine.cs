using System;
using YARG.Core.Chart;

namespace YARG.Core.Engine.Track
{
    public abstract class TrackEngine<TNoteType, TActionType, TEngineParams, TEngineStats, TEngineState> :
        BaseEngine<TNoteType, TActionType, TEngineParams, TEngineStats, TEngineState>
        where TNoteType : Note<TNoteType>
        where TActionType : unmanaged, Enum
        where TEngineParams : TrackEngineParameters
        where TEngineStats : BaseStats, new()
        where TEngineState : BaseEngineState, new()
    {
        protected const int POINTS_PER_NOTE = 50;
        protected const int POINTS_PER_BEAT = 25;

        public delegate void NoteHitEvent(int noteIndex, TNoteType note);

        public delegate void NoteMissedEvent(int noteIndex, TNoteType note);

        public NoteHitEvent?    OnNoteHit;
        public NoteMissedEvent? OnNoteMissed;

        protected TrackEngine(InstrumentDifficulty<TNoteType> chart, SyncTrack syncTrack,
            TEngineParams engineParameters) : base(chart, syncTrack, engineParameters)
        {
        }

        protected abstract bool CheckForNoteHit();

        /// <summary>
        /// Checks if the given note can be hit with the current input state.
        /// </summary>
        /// <param name="note">The Note to attempt to hit.</param>
        /// <returns>True if note can be hit. False otherwise.</returns>
        protected abstract bool CanNoteBeHit(TNoteType note);

        protected abstract bool HitNote(TNoteType note);

        protected abstract void MissNote(TNoteType note);

        protected abstract void AddScore(TNoteType note);

        protected bool IsNoteInWindow(TNoteType note)
        {
            return note.Time - State.CurrentTime < EngineParameters.BackEnd &&
                note.Time - State.CurrentTime > EngineParameters.FrontEnd;
        }
    }
}