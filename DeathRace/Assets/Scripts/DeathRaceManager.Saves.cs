using Gamebox;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Named saves of the career (YAML files, BaseGame's saved games window). The career saves itself (the autosave)
    /// before every race, after every race and when the player leaves the garage; the player saves it by name from the
    /// menu or the garage. A save is the whole <see cref="CareerState"/>; loading one checks and mends it
    /// (<see cref="Career.Validate"/>) and opens its garage. Continue on the new career window takes up the latest.
    /// </summary>
    public partial class DeathRaceManager
    {
        protected override bool OffersNamedSaves => true;

        /// <summary>A career can be saved between races only (a race is never saved halfway).</summary>
        public override string SaveRefusal
        {
            get
            {
                if (career == null)
                {
                    return Portfolio.DeathRace.L.T("There is no career to save.");
                }
                return IsRacing ? Portfolio.DeathRace.L.T("A career cannot be saved during a race.") : null;
            }
        }

        protected override SaveContent CaptureSave()
        {
            return new SaveContent { Data = career, Level = career.tracksOpen - 1, Summary = SummaryOf(career) };
        }

        /// <summary>"Ada, day 12: #7, $12,300", what tells the saves apart in the list.</summary>
        public static string SummaryOf(CareerState state)
        {
            string line = Portfolio.DeathRace.L.F("{0}, day {1}: {2}, {3}", state.driverName, state.day, RaceFormat.Rank(Ladder.Rank(state)),
                RaceFormat.Money(state.wallet.Money));
            return state.champion ? line + " " + Portfolio.DeathRace.L.T("(champion)") : line;
        }

        protected override string RestoreSave(SavedGame save)
        {
            if (IsRacing || InSession || InLocalMatch)
            {
                return Portfolio.DeathRace.L.T("A career cannot be loaded during a race.");
            }
            CareerState state = save != null ? save.Read<CareerState>() : null;
            string problem = Career.Validate(state);
            if (problem != null)
            {
                return Portfolio.DeathRace.L.T(problem);
            }
            CloseCareerWindows();
            if (State == null || State.BaseState != BaseGameState.Initialization)
            {
                TransitionState(BaseGameState.Initialization);
            }
            TakeCareer(state);
            GarageWindow.For(this).Show(GarageWindow.RacePage);
            return null;
        }

        /// <summary>The windows of a career that a loaded one replaces (the garage is opened again for it).</summary>
        private void CloseCareerWindows()
        {
            NewCareerWindow.For(this).Hide();
            ResultsWindow.For(this).Hide();
            ChampionWindow.For(this).Hide();
            QuickRaceWindow.For(this).Hide();
            GarageWindow.For(this).Hide();
        }
    }
}
