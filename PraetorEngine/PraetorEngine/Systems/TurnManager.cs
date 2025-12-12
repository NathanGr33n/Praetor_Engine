using System;

namespace PraetorEngine.Systems
{
    /// <summary>
    /// Event arguments for turn-related events.
    /// </summary>
    public class TurnEventArgs : EventArgs
    {
        public int TurnNumber { get; }
        
        public TurnEventArgs(int turnNumber)
        {
            TurnNumber = turnNumber;
        }
    }

    /// <summary>
    /// Manages turn-based gameplay flow.
    /// Coordinates turn progression and notifies systems of turn changes.
    /// </summary>
    public class TurnManager
    {
        private int _currentTurn;
        private TurnPhase _currentPhase;

        public int CurrentTurn => _currentTurn;
        public TurnPhase CurrentPhase => _currentPhase;

        // Events for turn lifecycle
        public event EventHandler<TurnEventArgs>? TurnStarted;
        public event EventHandler<TurnEventArgs>? TurnEnded;
        public event EventHandler<TurnEventArgs>? PhaseChanged;

        public TurnManager(int startingTurn = 1)
        {
            _currentTurn = startingTurn;
            _currentPhase = TurnPhase.PlayerActions;
        }

        /// <summary>
        /// Advances to the next turn.
        /// Triggers turn end and turn start events.
        /// </summary>
        public void NextTurn()
        {
            // End current turn
            OnTurnEnded(new TurnEventArgs(_currentTurn));

            // Increment turn counter
            _currentTurn++;

            // Reset to initial phase
            _currentPhase = TurnPhase.PlayerActions;

            // Start new turn
            OnTurnStarted(new TurnEventArgs(_currentTurn));
        }

        /// <summary>
        /// Advances to the next phase within the current turn.
        /// If at the last phase, advances to the next turn.
        /// </summary>
        public void NextPhase()
        {
            switch (_currentPhase)
            {
                case TurnPhase.PlayerActions:
                    _currentPhase = TurnPhase.AIActions;
                    OnPhaseChanged(new TurnEventArgs(_currentTurn));
                    break;

                case TurnPhase.AIActions:
                    _currentPhase = TurnPhase.Resolution;
                    OnPhaseChanged(new TurnEventArgs(_currentTurn));
                    break;

                case TurnPhase.Resolution:
                    // End of turn cycle, advance to next turn
                    NextTurn();
                    break;
            }
        }

        /// <summary>
        /// Resets the turn manager to initial state.
        /// </summary>
        public void Reset(int startingTurn = 1)
        {
            _currentTurn = startingTurn;
            _currentPhase = TurnPhase.PlayerActions;
        }

        /// <summary>
        /// Gets a display string for the current turn and phase.
        /// </summary>
        public string GetTurnDisplay()
        {
            string phaseStr = _currentPhase switch
            {
                TurnPhase.PlayerActions => "Player Phase",
                TurnPhase.AIActions => "AI Phase",
                TurnPhase.Resolution => "Resolution Phase",
                _ => "Unknown"
            };

            return $"Turn {_currentTurn} - {phaseStr}";
        }

        // Event invocation methods
        protected virtual void OnTurnStarted(TurnEventArgs e)
        {
            TurnStarted?.Invoke(this, e);
        }

        protected virtual void OnTurnEnded(TurnEventArgs e)
        {
            TurnEnded?.Invoke(this, e);
        }

        protected virtual void OnPhaseChanged(TurnEventArgs e)
        {
            PhaseChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Phases within a single turn.
    /// Can be extended for more complex turn structures in Phase 3+.
    /// </summary>
    public enum TurnPhase
    {
        PlayerActions = 0,  // Player gives orders and moves units
        AIActions = 1,      // AI takes its actions
        Resolution = 2      // Resolve combat, update world state
    }
}
