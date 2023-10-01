using System.Collections.Generic;
using YARG.Core.Chart;

namespace YARG.Core.Engine.Track
{
    public abstract class TrackEngine<TNoteType, TEngineParams, TEngineStats, TEngineState> :
        BaseEngine<TEngineParams, TEngineStats, TEngineState>
        where TNoteType : Note<TNoteType>
        where TEngineParams : TrackEngineParameters
        where TEngineStats : BaseStats, new()
        where TEngineState : TrackEngineState, new()
    {
        protected const int POINTS_PER_NOTE = 50;
        protected const int POINTS_PER_BEAT = 25;

        public delegate void StarPowerPhraseHitEvent(TNoteType note);
        public delegate void StarPowerPhraseMissEvent(TNoteType note);

        public delegate void NoteHitEvent(int noteIndex, TNoteType note);
        public delegate void NoteMissedEvent(int noteIndex, TNoteType note);

        public delegate void SoloStartEvent(SoloSection soloSection);
        public delegate void SoloEndEvent(SoloSection soloSection);

        public StarPowerPhraseHitEvent?  OnStarPowerPhraseHit;
        public StarPowerPhraseMissEvent? OnStarPowerPhraseMissed;

        public NoteHitEvent?    OnNoteHit;
        public NoteMissedEvent? OnNoteMissed;

        public SoloStartEvent? OnSoloStart;
        public SoloEndEvent?   OnSoloEnd;

        protected readonly InstrumentDifficulty<TNoteType> Chart;
        protected readonly List<TNoteType> Notes;

        protected List<SoloSection> Solos;

        protected TrackEngine(InstrumentDifficulty<TNoteType> chart,
            SyncTrack syncTrack, TEngineParams engineParameters) : base(syncTrack, engineParameters)
        {
            Chart = chart;
            Notes = Chart.Notes;

            Solos = GetSoloSections();
        }

        public override void Reset(bool keepCurrentButtons = false)
        {
            base.Reset(keepCurrentButtons);

            foreach (var note in Notes)
            {
                note.ResetNoteState();
            }

            foreach (var solo in Solos)
            {
                solo.NotesHit = 0;
            }
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

        protected virtual void StripStarPower(TNoteType? note)
        {
            if (note is null || !note.IsStarPower)
            {
                return;
            }

            EngineStats.PhrasesMissed++;

            // Strip star power from the note and all its children
            note.Flags &= ~NoteFlags.StarPower;
            foreach (var childNote in note.ChildNotes)
            {
                childNote.Flags &= ~NoteFlags.StarPower;
            }

            // Look back until finding the start of the phrase
            var prevNote = note.PreviousNote;
            while (prevNote is not null && prevNote.IsStarPower)
            {
                prevNote.Flags &= ~NoteFlags.StarPower;
                foreach (var childNote in prevNote.ChildNotes)
                {
                    childNote.Flags &= ~NoteFlags.StarPower;
                }

                if (prevNote.IsStarPowerStart)
                {
                    break;
                }

                prevNote = prevNote.PreviousNote;
            }

            // Do this to warn of a null reference if its used below
            prevNote = null;

            // Look forward until finding the end of the phrase
            var nextNote = note.NextNote;
            while (nextNote is not null && nextNote.IsStarPower)
            {
                nextNote.Flags &= ~NoteFlags.StarPower;
                foreach (var childNote in nextNote.ChildNotes)
                {
                    childNote.Flags &= ~NoteFlags.StarPower;
                }

                if (nextNote.IsStarPowerEnd)
                {
                    break;
                }

                nextNote = nextNote.NextNote;
            }

            OnStarPowerPhraseMissed?.Invoke(note);
        }

        protected void AwardStarPower(TNoteType note)
        {
            EngineStats.StarPowerAmount += STAR_POWER_PHRASE_AMOUNT;
            if (EngineStats.StarPowerAmount >= 1)
            {
                EngineStats.StarPowerAmount = 1;
            }

            OnStarPowerPhraseHit?.Invoke(note);
        }

        protected void StartSolo()
        {
            if (State.CurrentSoloIndex >= Solos.Count)
            {
                return;
            }

            State.IsSoloActive = true;
            OnSoloStart?.Invoke(Solos[State.CurrentSoloIndex]);
        }

        protected void EndSolo()
        {
            if (!State.IsSoloActive)
            {
                return;
            }

            State.IsSoloActive = false;
            OnSoloEnd?.Invoke(Solos[State.CurrentSoloIndex]);
            State.CurrentSoloIndex++;
        }

        private List<SoloSection> GetSoloSections()
        {
            var soloSections = new List<SoloSection>();
            for (int i = 0; i < Notes.Count; i++)
            {
                var start = Notes[i];
                if (!start.IsSoloStart)
                {
                    continue;
                }

                // note is a SoloStart

                // Try to find a solo end
                int soloNoteCount = GetNumberOfNotes(start);
                for (int j = i + 1; j < Notes.Count; j++)
                {
                    var end = Notes[j];

                    soloNoteCount += GetNumberOfNotes(end);

                    if (!end.IsSoloEnd) continue;

                    soloSections.Add(new SoloSection(soloNoteCount));

                    // Move i to the end of the solo section
                    i = j + 1;
                    break;
                }
            }

            return soloSections;
        }
    }
}